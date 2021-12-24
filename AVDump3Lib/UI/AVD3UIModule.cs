using AVDump3Lib.Information;
using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.FileMove;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using ExtKnot.StringInvariants;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3Lib.UI;

public class AVD3UIModuleExceptionEventArgs : EventArgs {
	public AVD3UIModuleExceptionEventArgs(XElement exception) {
		Exception = exception;
	}

	public XElement Exception { get; private set; }
}

public class AVD3UIFileProcessedEventArgs : EventArgs {
	public FileMetaInfo FileMetaInfo { get; }

	private readonly List<Task<bool>> processingTasks = new();
	private readonly Dictionary<string, string> fileMoveTokens = new();

	public IEnumerable<Task<bool>> ProcessingTasks => processingTasks;
	public IReadOnlyDictionary<string, string> FileMoveTokens => fileMoveTokens;

	public void AddProcessingTask(Task<bool> processingTask) => processingTasks.Add(processingTask);

	public void AddFileMoveToken(string key, string value) => fileMoveTokens.Add(key, value);

	public AVD3UIFileProcessedEventArgs(FileMetaInfo fileMetaInfo) => FileMetaInfo = fileMetaInfo;
}

public interface IAVD3UIConsole {

	IDisposable LockConsole();
	void WriteLine(IEnumerable<string> values);
	void WriteLine(string value);
}

public interface IAVD3UIModule : IAVD3Module {
	event EventHandler<AVD3UIModuleExceptionEventArgs> ExceptionThrown;
	event EventHandler<AVD3UIFileProcessedEventArgs> FileProcessed;
	event EventHandler ProcessingFinished;

	IAVD3UIConsole Console { get; }

	void RegisterShutdownDelay(WaitHandle waitHandle);
	void WriteLine(string value);
}





public abstract class AVD3UIModule : IAVD3UIModule, IFileMoveConfigure {
	public event EventHandler<AVD3UIModuleExceptionEventArgs>? ExceptionThrown = delegate { };
	public event EventHandler<AVD3UIFileProcessedEventArgs>? FileProcessed = delegate { };
	public event EventHandler ProcessingFinished = delegate { };

	public abstract IAVD3UIConsole Console { get; } //=> console;

	private IFileMoveScript fileMove = null!;
	private HashSet<string> filePathsToSkip = new();
	//private IServiceProvider fileMoveServiceProvider;
	//private ScriptRunner<string> fileMoveScriptRunner;

	private readonly List<WaitHandle> shutdownDelayHandles = new();
	private readonly AppendLineManager lineWriter = new();
	private readonly object reportSaveLockObj = new object();

	private static readonly Regex placeholderPattern = new(@"\$\{(?<Key>[A-Za-z0-9\-\.]+)\}");
	private IReadOnlyCollection<IAVD3Module> modules = Array.Empty<IAVD3Module>();
	protected IAVD3ProcessingModule ProcessingModule { get; private set; } = null!;
	protected IAVD3InformationModule InformationModule { get; private set; } = null!;
	protected IAVD3ReportingModule ReportingModule { get; private set; } = null!;
	//private readonly AVD3Console console = new();

	protected abstract AVD3UISettings Settings { get; }


	protected abstract void OnException(AVD3LibException ex);

	protected void OnExceptionThrown(AVD3UIModuleExceptionEventArgs args) {
		ExceptionThrown?.Invoke(this, args);
	}

	public virtual void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		this.modules = modules;

		ProcessingModule = modules.OfType<IAVD3ProcessingModule>().Single();
		InformationModule = modules.OfType<IAVD3InformationModule>().Single();
		ReportingModule = modules.OfType<IAVD3ReportingModule>().Single();

		ProcessingModule.BlockConsumerFilter += (s, e) => {
			if(Settings?.Processing.Consumers.Value.Any(x => e.BlockConsumerName.InvEqualsOrdCI(x.Name)) ?? false) {
				e.Accept();
			}
		};

		ProcessingModule.FilePathFilter += (s, e) => {
			if(Settings == null) throw new InvalidOperationException("Called FilePathFilter when settings is null");

			var accept = Settings.FileDiscovery.WithExtensions.Allow == (
				Settings.FileDiscovery.WithExtensions.Items.Length == 0 ||
				Settings.FileDiscovery.WithExtensions.Items.Any(
					fe => e.FilePath.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase)
				)
			) && !filePathsToSkip.Contains(e.FilePath);
			if(!accept) e.Decline();
		};


	}
	public ModuleInitResult Initialized() => new(false);

	protected void InitializeSettings(SettingsModuleInitResult args) {

		ProcessingModule.RegisterDefaultBlockConsumers((Settings.Processing.Consumers ?? ImmutableArray<ConsumerSettings>.Empty).ToDictionary(x => x.Name, x => x.Arguments));

		if(Settings.Processing.Consumers == null) {
			System.Console.WriteLine("Available Consumers: ");
			foreach(var blockConsumerFactory in ProcessingModule.BlockConsumerFactories) {
				System.Console.WriteLine(blockConsumerFactory.Name.PadRight(14) + " - " + blockConsumerFactory.Description);
			}
			args.Cancel();

		} else if(Settings.Processing.Consumers.Value.Any()) {
			var invalidBlockConsumerNames = Settings.Processing.Consumers.Value.Where(x => ProcessingModule.BlockConsumerFactories.All(y => !y.Name.InvEqualsOrdCI(x.Name))).ToArray();
			if(invalidBlockConsumerNames.Any()) {
				System.Console.WriteLine("Invalid BlockConsumer(s): " + string.Join(", ", invalidBlockConsumerNames.Select(x => x.Name)));
				args.Cancel();
			}
		}

		if(Settings.Diagnostics.Version) {
			using var mi = new MediaInfoLibNativeMethods();
			var version = mi.Option("Info_Version");

			System.Console.WriteLine($"Program Version: {Assembly.GetEntryAssembly()?.GetName().Version?.Build.ToString() ?? "Unknown"}");
			System.Console.WriteLine(version);
			args.Cancel();
		}

		if(Settings.Reporting.Reports == null) {
			System.Console.WriteLine("Available Reports: ");
			foreach(var reportFactory in ReportingModule.ReportFactories) {
				System.Console.WriteLine(reportFactory.Name.PadRight(14) + " - " + reportFactory.Description);
			}
			args.Cancel();

		} else if(Settings.Reporting.Reports?.Any() ?? false) {
			var invalidReportNames = Settings.Reporting.Reports.Value.Where(x => ReportingModule.ReportFactories.All(y => !y.Name.InvEqualsOrdCI(x))).ToArray();
			if(invalidReportNames.Any()) {
				System.Console.WriteLine("Invalid Report: " + string.Join(", ", invalidReportNames));
				args.Cancel();
			}
		}

		if(!string.IsNullOrEmpty(Settings.Reporting.CRC32Error?.Path)) {
			ProcessingModule.BlockConsumerFilter += (s, e) => {
				if(e.BlockConsumerName.InvEqualsOrd("CRC32")) e.Accept();
			};
		}

		if(Settings.Processing.PrintAvailableSIMDs) {
			System.Console.WriteLine("Available SIMD Instructions: ");
			foreach(var flagValue in Enum.GetValues(typeof(CPUInstructions)).OfType<CPUInstructions>().Where(x => (x & ProcessingModule.AvailableSIMD) != 0)) {
				System.Console.WriteLine(flagValue);
			}
			args.Cancel();
		}

		//Don't cancel startup when DoneLogPath doesn't exist yet
		if(!string.IsNullOrEmpty(Settings.FileDiscovery.DoneLogPath) && !File.Exists(Settings.FileDiscovery.DoneLogPath)) {
			File.Open(Settings.FileDiscovery.DoneLogPath, FileMode.OpenOrCreate).Dispose();
		}

		var invalidFilePaths = Settings.FileDiscovery.SkipLogPath.Where(p => !File.Exists(p));
		if(!invalidFilePaths.Any()) {
			filePathsToSkip = new HashSet<string>(Settings.FileDiscovery.SkipLogPath.SelectMany(p => File.ReadLines(p)));

		} else if(Settings.FileDiscovery.SkipLogPath.Any()) {
			System.Console.WriteLine("SkipLogPath contains file paths which do not exist: " + string.Join(", ", invalidFilePaths));
			args.Cancel();
		}

		static void CreateDirectoryChain(string? path, bool isDirectory = false) {
			if(!isDirectory) path = Path.GetDirectoryName(path);
			if(!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
		}

		if(Settings.Diagnostics.NullStreamTest.StreamCount > 0 && Settings.Reporting.Reports?.Length > 0) {
			System.Console.WriteLine("NullStreamTest cannot be used with reports");
			args.Cancel();
		}

		if(Settings.FileMove.Mode != FileMoveMode.None) {
			var fileMoveExtensions = modules.OfType<IFileMoveConfigure>().ToArray();

			static string PlaceholderConvert(string pattern) => "return \"" + placeholderPattern.Replace(pattern.Replace("\\", "\\\\").Replace("\"", "\\\""), @""" + Get(""$1"") + """) + "\";";

			fileMove = Settings.FileMove.Mode switch {
				FileMoveMode.PlaceholderInline => new FileMoveScriptByInlineScript(fileMoveExtensions, PlaceholderConvert(Settings.FileMove.Pattern)),
				FileMoveMode.CSharpScriptInline => new FileMoveScriptByInlineScript(fileMoveExtensions, Settings.FileMove.Pattern),
				FileMoveMode.PlaceholderFile => new FileMoveScriptByScriptFile(fileMoveExtensions, Settings.FileMove.Pattern, x => PlaceholderConvert(x)),
				FileMoveMode.CSharpScriptFile => new FileMoveScriptByScriptFile(fileMoveExtensions, Settings.FileMove.Pattern),
				FileMoveMode.DotNetAssembly => new FileMoveScriptByAssembly(fileMoveExtensions, Settings.FileMove.Pattern),
				_ => throw new NotImplementedException()
			};

			if(!Settings.FileMove.Test) {
				fileMove.Load();

			} else {
				if(!fileMove.CanReload) {
					System.Console.WriteLine("FileMove cannot enter test mode because the choosen --FileMove.Mode cannot be reloaded. It needs to be file based!");
					args.Cancel();
				}
			}
		}

		foreach(var processedLogPath in Settings.FileDiscovery.ProcessedLogPath) CreateDirectoryChain(processedLogPath);
		foreach(var skipLogPath in Settings.FileDiscovery.SkipLogPath) CreateDirectoryChain(skipLogPath);
		CreateDirectoryChain(Settings.Reporting.CRC32Error?.Path);
		CreateDirectoryChain(Settings.Reporting.ExtensionDifferencePath);
		CreateDirectoryChain(Settings.Reporting.ReportDirectory, true);
		CreateDirectoryChain(Settings.Diagnostics.ErrorDirectory, true);

	}


	private NullStreamProvider CreateNullStreamProvider() {
		if(Settings.Diagnostics.NullStreamTest == null) throw new AVD3UIException("Called CreateNullStreamProvider where Diagnostics.NullStreamTest was null");

		var nsp = new NullStreamProvider(
			Settings.Diagnostics.NullStreamTest.StreamCount,
			Settings.Diagnostics.NullStreamTest.StreamLength,
			Settings.Diagnostics.NullStreamTest.ParallelStreamCount
		);


		return nsp;
	}
	private IStreamProvider CreateFileStreamProvider(string[] paths) {
		var acceptedFiles = 0;
		var fileDiscoveryOn = DateTimeOffset.UtcNow;
		var sp = (StreamFromPathsProvider)ProcessingModule.CreateFileStreamProvider(
			paths, Settings.FileDiscovery.Recursive, Settings.FileDiscovery.Concurrent,
			path => {
				if(Settings.Diagnostics.PrintDiscoveredFiles) {
					System.Console.WriteLine("Accepted file: " + path);
				} else {
					if(fileDiscoveryOn.AddSeconds(1) < DateTimeOffset.UtcNow) {
						System.Console.WriteLine("Accepted files: " + acceptedFiles);
						fileDiscoveryOn = DateTimeOffset.UtcNow;
					}
				}
				acceptedFiles++;
			},
			ex => {
				if(ex is not UnauthorizedAccessException || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					System.Console.WriteLine("Filediscovery: " + ex.Message);
				}
			}
		);
		System.Console.WriteLine("Accepted files: " + acceptedFiles);
		System.Console.WriteLine();


		return sp;
	}
	protected virtual IStreamProvider CreateFileStream(string[] paths) {
		var sp = Settings.Diagnostics.NullStreamTest.StreamCount > 0 ? CreateNullStreamProvider() : CreateFileStreamProvider(paths);
		return sp;
	}

	public abstract IBytesReadProgress CreateBytesReadProgress();
	protected abstract void ProcessException(Exception ex);
	protected virtual void OnProcessingStarting(CancellationTokenSource cts) { }
	protected virtual void OnProcessingFinished() {
		ProcessingFinished?.Invoke(this, EventArgs.Empty);
	}
	protected virtual void OnProcessingFullyFinished() { }

	public void Process(string[] paths) {
		try {
			var bytesReadProgress = CreateBytesReadProgress();
			var sp = CreateFileStream(paths);

			var streamConsumerCollection = ProcessingModule.CreateStreamConsumerCollection(sp,
				Settings.Processing.BufferLength,
				Settings.Processing.ProducerMinReadLength,
				Settings.Processing.ProducerMaxReadLength
			);
			streamConsumerCollection.ConsumingStream += ConsumingStream;

			using(Console as IDisposable)
			using(sp as IDisposable)
			using(var cts = new CancellationTokenSource()) {

				OnProcessingStarting(cts);

				try {
					streamConsumerCollection.ConsumeStreams(bytesReadProgress, cts.Token);

					OnProcessingFinished();

					var shutdownDelayHandles = this.shutdownDelayHandles.ToArray();
					if(shutdownDelayHandles.Length > 0) WaitHandle.WaitAll(shutdownDelayHandles);

				} catch(OperationCanceledException) {
				} catch(Exception ex) {
					ProcessException(ex);
					throw;
				}

				OnProcessingFullyFinished();

			}

			if(Settings.Processing.PauseBeforeExit) {
				System.Console.WriteLine("Program execution has finished. Press any key to exit.");
				System.Console.Read();
			}

		} finally {
			lineWriter.Clear();
			lineWriter.Dispose();
		}
	}

	private async void ConsumingStream(object? sender, ConsumingStreamEventArgs e) {
		var filePath = (string)e.Tag;
		var fileName = Path.GetFileName(filePath);

		var hasProcessingError = false;
		e.OnException += (s, args) => {
			args.IsHandled = true;
			args.Retry = args.RetryCount < 2;
			hasProcessingError = !args.IsHandled;

			OnException(new AVD3UIException("ConsumingStream", args.Cause) { Data = { { "FileName", new SensitiveData(fileName) } } });
		};


		try {
			var blockConsumers = await e.FinishedProcessing.ConfigureAwait(false);
			if(hasProcessingError) return;


			var fileMetaInfo = CreateFileMetaInfo(filePath, blockConsumers);
			if(fileMetaInfo != null) {
				var success = true;
				success = success && await HandleReporting(fileMetaInfo);
				success = success && await HandleEvent(fileMetaInfo);
				success = success && await HandleFileMove(fileMetaInfo);

				if(Settings.FileDiscovery.ProcessedLogPath.Any() && success) {
					foreach(var processedLogPath in Settings.FileDiscovery.ProcessedLogPath) {
						lineWriter.AppendLine(processedLogPath, fileMetaInfo.FileInfo.FullName);
					}
				}
			}

		} finally {
			e.ResumeNext.Set();
		}
	}

	private FileMetaInfo? CreateFileMetaInfo(string filePath, ImmutableArray<IBlockConsumer> blockConsumers) {
		var fileName = Path.GetFileName(filePath);

		try {
			var infoSetup = new InfoProviderSetup(filePath, blockConsumers);
			var infoProviders = InformationModule.InfoProviderFactories.Select(x => x.Create(infoSetup)).ToArray();
			return new FileMetaInfo(new FileInfo(filePath), infoProviders);

		} catch(Exception ex) {
			OnException(new AVD3UIException("CreatingInfoProviders", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
			return null;
		}
	}

	private async Task<bool> HandleReporting(FileMetaInfo fileMetaInfo) {
		var fileName = Path.GetFileName(fileMetaInfo.FileInfo.FullName);


		var linesToWrite = new List<string>(32);
		if(Settings.Reporting.PrintHashes || Settings.Reporting.PrintReports) {
			linesToWrite.Add(fileName);
		}

		if(Settings.Reporting.PrintHashes) {
			foreach(var item in fileMetaInfo.Providers.OfType<HashProvider>().FirstOrDefault().Items.OfType<MetaInfoItem<ImmutableArray<byte>>>()) {
				linesToWrite.Add(item.Type.Key + " => " + BitConverter.ToString(item.Value.ToArray()).Replace("-", ""));
			}
			linesToWrite.Add("");
		}
		if(!string.IsNullOrEmpty(Settings.Reporting.CRC32Error?.Path)) {
			var hashProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == HashProvider.HashProviderType).Single();
			var metaInfoItem = hashProvider.Items.FirstOrDefault(x => x.Type.Key.Equals("CRC32"));

			if(metaInfoItem != null) {
				var crc32Hash = (ImmutableArray<byte>)metaInfoItem.Value;
				var crc32HashStr = BitConverter.ToString(crc32Hash.ToArray(), 0).Replace("-", "");

				if(!Regex.IsMatch(fileMetaInfo.FileInfo.FullName, Settings.Reporting.CRC32Error?.Pattern.Replace("${CRC32}", crc32HashStr))) {
					lineWriter.AppendLine(
						Settings.Reporting.CRC32Error.Value.Path,
						crc32HashStr + " " + fileMetaInfo.FileInfo.FullName
					);
				}
			}
		}

		if(!string.IsNullOrEmpty(Settings.Reporting.ExtensionDifferencePath)) {
			var metaDataProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == MediaProvider.MediaProviderType).Single();
			var detExts = metaDataProvider.Select(MediaProvider.SuggestedFileExtensionType)?.Value ?? ImmutableArray.Create<string>();
			var ext = fileMetaInfo.FileInfo.Extension.StartsWith('.') ? fileMetaInfo.FileInfo.Extension[1..] : fileMetaInfo.FileInfo.Extension;

			if(!detExts.Contains(ext, StringComparer.OrdinalIgnoreCase)) {
				if(detExts.Length == 0) detExts = ImmutableArray.Create("unknown");

				lineWriter.AppendLine(
					Settings.Reporting.ExtensionDifferencePath,
					ext + " => " + string.Join(" ", detExts) + "\t" + fileMetaInfo.FileInfo.FullName
				);
			}
		}

		var success = true;
		var reportsFactories = ReportingModule.ReportFactories.Where(x => Settings.Reporting.Reports?.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase)) ?? false).ToArray();
		if(reportsFactories.Length != 0) {

			try {
				var reportItems = reportsFactories.Select(x => new { x.Name, Report = x.Create(fileMetaInfo) });
				var tokenValues = new Dictionary<string, string?>();
				foreach(var reportItem in reportItems) {
					if(Settings.Reporting.PrintReports) {
						linesToWrite.Add(reportItem.Report.ReportToString(Utils.UTF8EncodingNoBOM) + "\n");
					}

					tokenValues["ReportName"] = reportItem.Name;
					tokenValues["ReportFileExtension"] = reportItem.Report.FileExtension;

					var reportFileName = Settings.Reporting.ReportFileName;
					reportFileName = placeholderPattern.Replace(reportFileName, m => ReplaceToken(m.Groups["Key"].Value, fileMetaInfo, tokenValues));

					var reportContentPrefix = placeholderPattern.Replace(Settings.Reporting.ReportContentPrefix, m => ReplaceToken(m.Groups["Key"].Value, fileMetaInfo, tokenValues));

					
					lock(reportSaveLockObj) {
						reportItem.Report.SaveToFile(Path.Combine(Settings.Reporting.ReportDirectory, reportFileName), reportContentPrefix, Utils.UTF8EncodingNoBOM);
					}
				}

			} catch(PathTooLongException ex) {
				Console.WriteLine("Error " + ex.GetType() + ": " + ex.Message);
				success = false;

			} catch(Exception ex) {
				OnException(new AVD3UIException("GeneratingReports", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
				success = false;
			}
		}
		Console.WriteLine(linesToWrite);
		return success;
	}
	private async Task<bool> HandleEvent(FileMetaInfo fileMetaInfo) {
		var success = true;

		var fileProcessedEventArgs = new AVD3UIFileProcessedEventArgs(fileMetaInfo);
		try {
			FileProcessed?.Invoke(this, fileProcessedEventArgs);
		} catch(Exception ex) {
			OnException(new AVD3UIException("FileProcessedEvent", ex) { Data = { { "FilePath", new SensitiveData(fileMetaInfo.FileInfo.FullName) } } });
			success = false;
		}
		success &= (await Task.WhenAll(fileProcessedEventArgs.ProcessingTasks).ConfigureAwait(false)).All(x => x);

		return success;
	}
	private async Task<bool> HandleFileMove(FileMetaInfo fileMetaInfo) {
		var success = true;
		if(fileMove != null) {
			using var fileMoveScoped = fileMove.CreateScope();
			using var clLock = Settings.FileMove.Test ? Console.LockConsole() : null;

			try {
				var moveFile = true;
				var actionKey = ' ';
				var repeat = Settings.FileMove.Test;
				do {

					string? destFilePath = null;
					try {
						if(Settings.FileMove.Test) fileMoveScoped.Load();

						destFilePath = await fileMoveScoped.GetFilePathAsync(fileMetaInfo);


						foreach(var (Value, Replacement) in Settings.FileMove.Replacements) {
							destFilePath = destFilePath.InvReplace(Value, Replacement);
						}

						if(Settings.FileMove.DisableFileMove) {
							destFilePath = Path.Combine(Path.GetDirectoryName(fileMetaInfo.FileInfo.FullName) ?? "", Path.GetFileName(destFilePath));
						}
						if(Settings.FileMove.DisableFileRename) {
							destFilePath = Path.Combine(Path.GetDirectoryName(destFilePath) ?? "", Path.GetFileName(fileMetaInfo.FileInfo.FullName));
						}



					} catch(Exception) {
						destFilePath = null;
					}


					if(Settings.FileMove.Test) {
						System.Console.WriteLine();
						System.Console.WriteLine();
						System.Console.WriteLine("FileMove.Test Enabled" + (Settings.FileMove.DisableFileMove ? " (DisableFileMove Enabled!)" : "") + (Settings.FileMove.DisableFileRename ? " (DisableFileRename Enabled!)" : ""));
						System.Console.WriteLine("Directoryname: ");
						System.Console.WriteLine("Old: " + fileMetaInfo.FileInfo.DirectoryName);
						System.Console.WriteLine("New: " + Path.GetDirectoryName(destFilePath));
						System.Console.WriteLine("Filename: ");
						System.Console.WriteLine("Old: " + fileMetaInfo.FileInfo.Name);
						System.Console.WriteLine("New: " + Path.GetFileName(destFilePath));


						if(actionKey == 'A') {
							System.Console.WriteLine("Press any key to cancel automatic mode");


							while(!System.Console.KeyAvailable && !fileMoveScoped.SourceChanged()) {
								await Task.Delay(500);
							}

							if(System.Console.KeyAvailable) {
								actionKey = ' ';
							} else {
								continue;
							}
						}

						do {
							System.Console.WriteLine();
							System.Console.WriteLine("How do you wish to continue?");
							System.Console.WriteLine("(C) Continue without moving the file");
							System.Console.WriteLine("(R) Repeat script execution");
							System.Console.WriteLine("(A) Repeat script execution automatically on sourcefile change");
							System.Console.WriteLine("(M) Moving the file and continue");
							System.Console.Write("User Input: ");

							while(System.Console.KeyAvailable) System.Console.ReadKey(true);
							actionKey = char.ToUpperInvariant(System.Console.ReadKey().KeyChar);

							if(actionKey == -1) actionKey = 'C';
							System.Console.WriteLine();
							System.Console.WriteLine();

						} while(actionKey != 'C' && actionKey != 'R' && actionKey != 'A' && actionKey != 'M');

						moveFile = actionKey == 'M';
						repeat = actionKey == 'R' || actionKey == 'A';
					}

					if(moveFile && !string.IsNullOrEmpty(destFilePath) && !string.Equals(destFilePath, fileMetaInfo.FileInfo.FullName, StringComparison.Ordinal)) {
						await Task.Run(() => {
							var originalPath = fileMetaInfo.FileInfo.FullName;
							fileMetaInfo.FileInfo.MoveTo(destFilePath);

							if(!string.IsNullOrEmpty(Settings.FileMove.LogPath)) {
								lineWriter.AppendLine(Settings.FileMove.LogPath, originalPath + " => " + destFilePath);
							}
						}).ConfigureAwait(false);
					}

				} while(repeat);

			} catch(Exception) {
				success = false;
			}

		}

		return success;
	}

	public void WriteLine(string value) => Console.WriteLine(value);


	public void RegisterShutdownDelay(WaitHandle waitHandle) => shutdownDelayHandles.Add(waitHandle);


	void IFileMoveConfigure.ConfigureServiceCollection(IServiceCollection services) { }
	string? IFileMoveConfigure.ReplaceToken(string key, FileMoveContext ctx) {
		var fileMetaInfo = ctx.FileMetaInfo;
		return ReplaceToken(key, fileMetaInfo);
	}

	private static string? ReplaceToken(string key, FileMetaInfo fileMetaInfo, IDictionary<string, string?>? additionalTokenValues = null) {
		var value = key switch {
			"FileSize" => fileMetaInfo.FileInfo.Length.ToString(),
			"FullName" => fileMetaInfo.FileInfo.FullName,
			"FileName" => fileMetaInfo.FileInfo.Name,
			"FileExtension" => fileMetaInfo.FileInfo.Extension,
			"FileNameWithoutExtension" => Path.GetFileNameWithoutExtension(fileMetaInfo.FileInfo.FullName),
			"DirectoryName" => fileMetaInfo.FileInfo.DirectoryName,
			_ => null,
		};

		if(key.StartsWith("SuggestedExtension")) {
			var metaDataProvider = fileMetaInfo.CondensedProviders.FirstOrDefault(x => x.Type == MediaProvider.MediaProviderType);
			var detExts = metaDataProvider.Select(MediaProvider.SuggestedFileExtensionType)?.Value ?? ImmutableArray.Create<string>();
			value = detExts.FirstOrDefault()?.Transform(x => "." + x) ?? fileMetaInfo.FileInfo.Extension;
		}

		if(key.StartsWith("Hash")) {
			var m = Regex.Match(key, @"Hash-(?<Name>[^-]+)-(?<Base>\d+)-(?<Case>UC|LC|OC)");
			if(m.Success) {
				var hashName = m.Groups["Name"].Value;
				var withBase = m.Groups["Base"].Value;
				var letterCase = m.Groups["Case"].Value;

				var hashData = fileMetaInfo.CondensedProviders.FirstOrDefault(x => x.Type == HashProvider.HashProviderType)?.Select<HashInfoItemType, ImmutableArray<byte>>(hashName)?.Value.ToArray();
				if(hashData != null) value = BitConverterEx.ToBase(hashData, BitConverterEx.Bases[withBase]).Transform(x => letterCase switch { "UC" => x.ToInvUpper(), "LC" => x.ToInvLower(), "OC" => x, _ => x });
			}
		}

		if(additionalTokenValues != null) {
			if(additionalTokenValues.TryGetValue(key, out var additionalTokenValue)) {
				value = additionalTokenValue;
			}
		}

		return value;
	}

	public void Shutdown() {
	}


	public string[]? HandleArgs(string[] args) {
		string[] pathsToProcess;
		var settingsModule = modules.OfType<AVD3SettingsModule>().Single();
		try {
			var parseResult = CLSettingsHandler.ParseArgs(settingsModule.SettingProperties, args);
			if(args.Contains("PRINTARGS")) {
				foreach(var arg in parseResult.RawArgs) System.Console.WriteLine(arg);
				System.Console.WriteLine();

			}

			if(!parseResult.Success) {
				System.Console.WriteLine(parseResult.Message);
				if(Utils.UsingWindows) System.Console.Read();
				return null;
			}

			if(parseResult.PrintHelp) {
				CLSettingsHandler.PrintHelp(settingsModule.SettingProperties, parseResult.PrintHelpTopic, args.Length != 0);
				return null;
			}


			var settingsStore = settingsModule.BuildStore();
			foreach(var settingValue in parseResult.SettingValues) {
				settingsStore.SetPropertyValue(settingValue.Key, settingValue.Value);
			}

			pathsToProcess = parseResult.UnnamedArgs.ToArray();

		} catch(Exception ex) {
			System.Console.WriteLine("Error while parsing commandline arguments:");
			System.Console.WriteLine(ex.Message);
			return null;
		}

		return pathsToProcess;
	}


}
