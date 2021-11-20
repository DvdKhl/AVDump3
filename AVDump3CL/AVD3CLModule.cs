using AVDump3Lib;
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
using AVDump3UI;
using ExtKnot.StringInvariants;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AVDump3CL;

public class AVD3CLModuleExceptionEventArgs : EventArgs {
	public AVD3CLModuleExceptionEventArgs(XElement exception) {
		Exception = exception;
	}

	public XElement Exception { get; private set; }
}

public class AVD3CLFileProcessedEventArgs : EventArgs {
	public FileMetaInfo FileMetaInfo { get; }

	private readonly List<Task<bool>> processingTasks = new();
	private readonly Dictionary<string, string> fileMoveTokens = new();

	public IEnumerable<Task<bool>> ProcessingTasks => processingTasks;
	public IReadOnlyDictionary<string, string> FileMoveTokens => fileMoveTokens;

	public void AddProcessingTask(Task<bool> processingTask) => processingTasks.Add(processingTask);

	public void AddFileMoveToken(string key, string value) => fileMoveTokens.Add(key, value);

	public AVD3CLFileProcessedEventArgs(FileMetaInfo fileMetaInfo) => FileMetaInfo = fileMetaInfo;
}


public interface IAVD3CLModule : IAVD3Module {
	event EventHandler<AVD3CLModuleExceptionEventArgs> ExceptionThrown;
	event EventHandler<AVD3CLFileProcessedEventArgs> FileProcessed;
	event EventHandler ProcessingFinished;

	IAVD3Console Console { get; }

	void RegisterShutdownDelay(WaitHandle waitHandle);
	void WriteLine(string value);
}





public class AVD3CLModule : IAVD3CLModule, IFileMoveConfigure {
	public event EventHandler<AVD3CLModuleExceptionEventArgs>? ExceptionThrown = delegate { };
	public event EventHandler<AVD3CLFileProcessedEventArgs>? FileProcessed = delegate { };
	public event EventHandler ProcessingFinished = delegate { };

	public IAVD3Console Console => console;

	private IFileMoveScript fileMove;
	private HashSet<string> filePathsToSkip = new();
	//private IServiceProvider fileMoveServiceProvider;
	//private ScriptRunner<string> fileMoveScriptRunner;

	private AVD3CLSettings settings;
	private readonly object fileSystemLock = new();
	private readonly List<WaitHandle> shutdownDelayHandles = new();
	private readonly AppendLineManager lineWriter = new();

	private static readonly Regex placeholderPattern = new(@"\$\{(?<Key>[A-Za-z0-9\-\.]+)\}");
	private IReadOnlyCollection<IAVD3Module> modules;
	private IAVD3ProcessingModule processingModule;
	private IAVD3InformationModule informationModule;
	private IAVD3ReportingModule reportingModule;
	private readonly AVD3Console console = new();

	private AVD3ProgressDisplay? progressDisplay;


	public AVD3CLModule() {
		AppDomain.CurrentDomain.UnhandledException += UnhandleException;
	}



	private void UnhandleException(object sender, UnhandledExceptionEventArgs e) {
		var ex = e.ExceptionObject as Exception ?? new Exception("Non Exception Type: " + e.ExceptionObject.ToString());
		AVD3LibException libException;

		if(e.ExceptionObject is AggregateException aggEx && aggEx.InnerExceptions.Count == 1 && aggEx.InnerExceptions[0] is AVD3ForceMajeureException forceMajeureException) {
			libException = forceMajeureException;

		} else {
			libException = new AVD3CLException("Unhandled AppDomain wide Exception", ex);
		}

		OnException(libException);

	}

	private void OnException(AVD3LibException ex) {
		if(settings?.Diagnostics.IncludePersonalData ?? false) {
			string? GetPropertyValue(ISettingProperty x) {
				if(x.UserValueType == AVD3UISettings.PasswordType) {
					return "Hidden";
				} else {
					return x.ToString(settings.Store.GetPropertyValue(x));
				}
			}

			var effectiveArgs = settings.Store.SettingProperties.Where(x => settings.Store.ContainsProperty(x)).Select(x => new XElement("Argument", $"{x.Group.FullName}.{x.Name}={GetPropertyValue(x)}"));

			ex.Data.Add("EffectiveCommandLineArguments", effectiveArgs);
		}


		var exElem = ex.ToXElement(
			settings?.Diagnostics.SkipEnvironmentElement ?? false,
			settings?.Diagnostics.IncludePersonalData ?? false
		);

		if(settings?.Diagnostics.SaveErrors ?? false) {
			lock(fileSystemLock) {
				Directory.CreateDirectory(settings.Diagnostics.ErrorDirectory);
				var filePath = Path.Combine(settings.Diagnostics.ErrorDirectory, "AVD3Error" + ex.ThrownOn.ToString("yyyyMMdd HHmmssffff") + ".xml");

				using var fileStream = File.OpenWrite(filePath);
				using var xmlWriter = System.Xml.XmlWriter.Create(fileStream, new System.Xml.XmlWriterSettings { CheckCharacters = false, Encoding = Encoding.UTF8, Indent = true });
				exElem.WriteTo(xmlWriter);
			}
		}

		var exception = ex.GetBaseException() ?? ex;

		console.WriteLine("Error " + exception.GetType() + ": " + exception.Message);

		if(ex is AVD3ForceMajeureException forceMajeureException) {
			console.WriteLine(forceMajeureException.RemedyActionMessage);
		}

		ExceptionThrown?.Invoke(this, new AVD3CLModuleExceptionEventArgs(exElem));

	}

	public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		this.modules = modules;

		processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
		processingModule.BlockConsumerFilter += (s, e) => {
			if(settings?.Processing.Consumers.Value.Any(x => e.BlockConsumerName.InvEqualsOrdCI(x.Name)) ?? false) {
				e.Accept();
			}
		};

		processingModule.FilePathFilter += (s, e) => {
			if(settings == null) throw new InvalidOperationException("Called FilePathFilter when settings is null");

			var accept = settings.FileDiscovery.WithExtensions.Allow == (
				settings.FileDiscovery.WithExtensions.Items.Length == 0 ||
				settings.FileDiscovery.WithExtensions.Items.Any(
					fe => e.FilePath.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase)
				)
			) && !filePathsToSkip.Contains(e.FilePath);
			if(!accept) e.Decline();
		};

		informationModule = modules.OfType<IAVD3InformationModule>().Single();
		reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

		var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
		settingsgModule.RegisterSettings(AVD3CLSettings.GetProperties());
		settingsgModule.ConfigurationFinished += ConfigurationFinished;
	}
	public ModuleInitResult Initialized() => new(false);

	public void ConfigurationFinished(object? sender, SettingsModuleInitResult args) {
		settings = new AVD3CLSettings(args.Store);

		progressDisplay = new AVD3ProgressDisplay(settings.Display);
		console.WriteProgress += progressDisplay.WriteProgress; //TODO Don't use event (Call Order)

		processingModule.RegisterDefaultBlockConsumers((settings.Processing.Consumers ?? ImmutableArray<ConsumerSettings>.Empty).ToDictionary(x => x.Name, x => x.Arguments));

		if(settings.Processing.Consumers == null) {
			System.Console.WriteLine("Available Consumers: ");
			foreach(var blockConsumerFactory in processingModule.BlockConsumerFactories) {
				System.Console.WriteLine(blockConsumerFactory.Name.PadRight(14) + " - " + blockConsumerFactory.Description);
			}
			args.Cancel();

		} else if(settings.Processing.Consumers.Value.Any()) {
			var invalidBlockConsumerNames = settings.Processing.Consumers.Value.Where(x => processingModule.BlockConsumerFactories.All(y => !y.Name.InvEqualsOrdCI(x.Name))).ToArray();
			if(invalidBlockConsumerNames.Any()) {
				System.Console.WriteLine("Invalid BlockConsumer(s): " + string.Join(", ", invalidBlockConsumerNames.Select(x => x.Name)));
				args.Cancel();
			}
		}

		if(settings.Diagnostics.Version) {
			using var mi = new MediaInfoLibNativeMethods();
			var version = mi.Option("Info_Version");

			System.Console.WriteLine($"Program Version: {Assembly.GetEntryAssembly()?.GetName().Version?.Build.ToString() ?? "Unknown"}");
			System.Console.WriteLine(version);
			args.Cancel();
		}

		if(settings.Reporting.Reports == null) {
			System.Console.WriteLine("Available Reports: ");
			foreach(var reportFactory in reportingModule.ReportFactories) {
				System.Console.WriteLine(reportFactory.Name.PadRight(14) + " - " + reportFactory.Description);
			}
			args.Cancel();

		} else if(settings.Reporting.Reports?.Any() ?? false) {
			var invalidReportNames = settings.Reporting.Reports.Value.Where(x => reportingModule.ReportFactories.All(y => !y.Name.InvEqualsOrdCI(x))).ToArray();
			if(invalidReportNames.Any()) {
				System.Console.WriteLine("Invalid Report: " + string.Join(", ", invalidReportNames));
				args.Cancel();
			}
		}

		if(!string.IsNullOrEmpty(settings.Reporting.CRC32Error?.Path)) {
			processingModule.BlockConsumerFilter += (s, e) => {
				if(e.BlockConsumerName.InvEqualsOrd("CRC32")) e.Accept();
			};
		}

		if(settings.Processing.PrintAvailableSIMDs) {
			System.Console.WriteLine("Available SIMD Instructions: ");
			foreach(var flagValue in Enum.GetValues(typeof(CPUInstructions)).OfType<CPUInstructions>().Where(x => (x & processingModule.AvailableSIMD) != 0)) {
				System.Console.WriteLine(flagValue);
			}
			args.Cancel();
		}

		//Don't cancel startup when DoneLogPath doesn't exist yet
		if(!string.IsNullOrEmpty(settings.FileDiscovery.DoneLogPath) && !File.Exists(settings.FileDiscovery.DoneLogPath)) {
			File.Open(settings.FileDiscovery.DoneLogPath, FileMode.OpenOrCreate).Dispose();
		}

		var invalidFilePaths = settings.FileDiscovery.SkipLogPath.Where(p => !File.Exists(p));
		if(!invalidFilePaths.Any()) {
			filePathsToSkip = new HashSet<string>(settings.FileDiscovery.SkipLogPath.SelectMany(p => File.ReadLines(p)));

		} else if(settings.FileDiscovery.SkipLogPath.Any()) {
			System.Console.WriteLine("SkipLogPath contains file paths which do not exist: " + string.Join(", ", invalidFilePaths));
			args.Cancel();
		}

		static void CreateDirectoryChain(string? path, bool isDirectory = false) {
			if(!isDirectory) path = Path.GetDirectoryName(path);
			if(!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
		}

		if(settings.Diagnostics.NullStreamTest.StreamCount > 0 && settings.Reporting.Reports?.Length > 0) {
			System.Console.WriteLine("NullStreamTest cannot be used with reports");
			args.Cancel();
		}

		if(settings.FileMove.Mode != FileMoveMode.None) {
			var fileMoveExtensions = modules.OfType<IFileMoveConfigure>().ToArray();

			static string PlaceholderConvert(string pattern) => "return \"" + placeholderPattern.Replace(pattern.Replace("\\", "\\\\").Replace("\"", "\\\""), @""" + Get(""$1"") + """) + "\";";

			fileMove = settings.FileMove.Mode switch {
				FileMoveMode.PlaceholderInline => new FileMoveScriptByInlineScript(fileMoveExtensions, PlaceholderConvert(settings.FileMove.Pattern)),
				FileMoveMode.CSharpScriptInline => new FileMoveScriptByInlineScript(fileMoveExtensions, settings.FileMove.Pattern),
				FileMoveMode.PlaceholderFile => new FileMoveScriptByScriptFile(fileMoveExtensions, settings.FileMove.Pattern, x => PlaceholderConvert(x)),
				FileMoveMode.CSharpScriptFile => new FileMoveScriptByScriptFile(fileMoveExtensions, settings.FileMove.Pattern),
				FileMoveMode.DotNetAssembly => new FileMoveScriptByAssembly(fileMoveExtensions, settings.FileMove.Pattern),
				_ => throw new NotImplementedException()
			};

			if(!settings.FileMove.Test) {
				fileMove.Load();

			} else {
				if(!fileMove.CanReload) {
					System.Console.WriteLine("FileMove cannot enter test mode because the choosen --FileMove.Mode cannot be reloaded. It needs to be file based!");
					args.Cancel();
				}
			}
		}

		foreach(var processedLogPath in settings.FileDiscovery.ProcessedLogPath) CreateDirectoryChain(processedLogPath);
		foreach(var skipLogPath in settings.FileDiscovery.SkipLogPath) CreateDirectoryChain(skipLogPath);
		CreateDirectoryChain(settings.Reporting.CRC32Error?.Path);
		CreateDirectoryChain(settings.Reporting.ExtensionDifferencePath);
		CreateDirectoryChain(settings.Reporting.ReportDirectory, true);
		CreateDirectoryChain(settings.Diagnostics.ErrorDirectory, true);

	}


	public NullStreamProvider CreateNullStreamProvider() {
		if(settings.Diagnostics.NullStreamTest == null) throw new AVD3CLException("Called CreateNullStreamProvider where Diagnostics.NullStreamTest was null");

		var nsp = new NullStreamProvider(
			settings.Diagnostics.NullStreamTest.StreamCount,
			settings.Diagnostics.NullStreamTest.StreamLength,
			settings.Diagnostics.NullStreamTest.ParallelStreamCount
		);

		progressDisplay.TotalFiles = nsp.StreamCount;
		progressDisplay.TotalBytes = nsp.StreamCount * nsp.StreamLength;

		return nsp;
	}

	public void Process(string[] paths) {
		var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));

		var sp = settings.Diagnostics.NullStreamTest.StreamCount > 0 ? CreateNullStreamProvider() : CreateFileStreamProvider(paths);
		var streamConsumerCollection = processingModule.CreateStreamConsumerCollection(sp,
			settings.Processing.BufferLength,
			settings.Processing.ProducerMinReadLength,
			settings.Processing.ProducerMaxReadLength
		);
		streamConsumerCollection.ConsumingStream += ConsumingStream;

		using(console)
		using(sp as IDisposable)
		using(var cts = new CancellationTokenSource()) {
			progressDisplay.Initialize(bytesReadProgress.GetProgress);

			if(!settings.Display.ForwardConsoleCursorOnly) console.StartProgressDisplay();

			void cancelKeyHandler(object s, ConsoleCancelEventArgs e) {
				System.Console.CancelKeyPress -= cancelKeyHandler;
				e.Cancel = true;
				cts.Cancel();
			}
			System.Console.CancelKeyPress += cancelKeyHandler;
			try {
				streamConsumerCollection.ConsumeStreams(bytesReadProgress, cts.Token);
				if(console.ShowingProgress) console.StopProgressDisplay();

				ProcessingFinished?.Invoke(this, EventArgs.Empty);

				var shutdownDelayHandles = this.shutdownDelayHandles.ToArray();
				if(shutdownDelayHandles.Length > 0) WaitHandle.WaitAll(shutdownDelayHandles);

			} catch(OperationCanceledException) {

			} finally {
				if(console.ShowingProgress) console.StopProgressDisplay();
			}

			console.WriteDisplayProgress();
		}

		if(settings.Processing.PauseBeforeExit) {
			System.Console.WriteLine("Program execution has finished. Press any key to exit.");
			System.Console.Read();
		}

		lineWriter.Clear();
	}

	private IStreamProvider CreateFileStreamProvider(string[] paths) {
		var acceptedFiles = 0;
		var fileDiscoveryOn = DateTimeOffset.UtcNow;
		var sp = (StreamFromPathsProvider)processingModule.CreateFileStreamProvider(
			paths, settings.FileDiscovery.Recursive, settings.FileDiscovery.Concurrent,
			path => {
				if(settings.Diagnostics.PrintDiscoveredFiles) {
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

		if(progressDisplay != null) {
			progressDisplay.TotalFiles = sp.TotalFileCount;
			progressDisplay.TotalBytes = sp.TotalBytes;
		}

		return sp;
	}

	private async void ConsumingStream(object? sender, ConsumingStreamEventArgs e) {
		var filePath = (string)e.Tag;
		var fileName = Path.GetFileName(filePath);

		var hasProcessingError = false;
		e.OnException += (s, args) => {
			args.IsHandled = true;
			args.Retry = args.RetryCount < 2;
			hasProcessingError = !args.IsHandled;

			OnException(new AVD3CLException("ConsumingStream", args.Cause) { Data = { { "FileName", new SensitiveData(fileName) } } });
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

				if(settings.FileDiscovery.ProcessedLogPath.Any() && success) {
					foreach(var processedLogPath in settings.FileDiscovery.ProcessedLogPath) {
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
			var infoProviders = informationModule.InfoProviderFactories.Select(x => x.Create(infoSetup)).ToArray();
			return new FileMetaInfo(new FileInfo(filePath), infoProviders);

		} catch(Exception ex) {
			OnException(new AVD3CLException("CreatingInfoProviders", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
			return null;
		}
	}

	private async Task<bool> HandleReporting(FileMetaInfo fileMetaInfo) {
		var fileName = Path.GetFileName(fileMetaInfo.FileInfo.FullName);


		var linesToWrite = new List<string>(32);
		if(settings.Reporting.PrintHashes || settings.Reporting.PrintReports) {
			linesToWrite.Add(fileName);
		}

		if(settings.Reporting.PrintHashes) {
			foreach(var item in fileMetaInfo.Providers.OfType<HashProvider>().FirstOrDefault().Items.OfType<MetaInfoItem<ImmutableArray<byte>>>()) {
				linesToWrite.Add(item.Type.Key + " => " + BitConverter.ToString(item.Value.ToArray()).Replace("-", ""));
			}
			linesToWrite.Add("");
		}
		if(!string.IsNullOrEmpty(settings.Reporting.CRC32Error?.Path)) {
			var hashProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == HashProvider.HashProviderType).Single();
			var metaInfoItem = hashProvider.Items.FirstOrDefault(x => x.Type.Key.Equals("CRC32"));

			if(metaInfoItem != null) {
				var crc32Hash = (ImmutableArray<byte>)metaInfoItem.Value;
				var crc32HashStr = BitConverter.ToString(crc32Hash.ToArray(), 0).Replace("-", "");

				if(!Regex.IsMatch(fileMetaInfo.FileInfo.FullName, settings.Reporting.CRC32Error?.Pattern.Replace("${CRC32}", crc32HashStr))) {
					lineWriter.AppendLine(
						settings.Reporting.CRC32Error.Value.Path,
						crc32HashStr + " " + fileMetaInfo.FileInfo.FullName
					);
				}
			}
		}

		if(!string.IsNullOrEmpty(settings.Reporting.ExtensionDifferencePath)) {
			var metaDataProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == MediaProvider.MediaProviderType).Single();
			var detExts = metaDataProvider.Select(MediaProvider.SuggestedFileExtensionType)?.Value ?? ImmutableArray.Create<string>();
			var ext = fileMetaInfo.FileInfo.Extension.StartsWith('.') ? fileMetaInfo.FileInfo.Extension[1..] : fileMetaInfo.FileInfo.Extension;

			if(!detExts.Contains(ext, StringComparer.OrdinalIgnoreCase)) {
				if(detExts.Length == 0) detExts = ImmutableArray.Create("unknown");

				lineWriter.AppendLine(
					settings.Reporting.ExtensionDifferencePath,
					ext + " => " + string.Join(" ", detExts) + "\t" + fileMetaInfo.FileInfo.FullName
				);
			}
		}

		var success = true;
		var reportsFactories = reportingModule.ReportFactories.Where(x => settings.Reporting.Reports?.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase)) ?? false).ToArray();
		if(reportsFactories.Length != 0) {

			try {
				var reportItems = reportsFactories.Select(x => new { x.Name, Report = x.Create(fileMetaInfo) });
				var tokenValues = new Dictionary<string, string?>();
				foreach(var reportItem in reportItems) {
					if(settings.Reporting.PrintReports) {
						linesToWrite.Add(reportItem.Report.ReportToString(Utils.UTF8EncodingNoBOM) + "\n");
					}

					tokenValues["ReportName"] = reportItem.Name;
					tokenValues["ReportFileExtension"] = reportItem.Report.FileExtension;

					var reportFileName = settings.Reporting.ReportFileName;
					reportFileName = placeholderPattern.Replace(reportFileName, m => ReplaceToken(m.Groups["Key"].Value, fileMetaInfo, tokenValues));

					var reportContentPrefix = placeholderPattern.Replace(settings.Reporting.ReportContentPrefix, m => ReplaceToken(m.Groups["Key"].Value, fileMetaInfo, tokenValues));

					lock(fileSystemLock) {
						reportItem.Report.SaveToFile(Path.Combine(settings.Reporting.ReportDirectory, reportFileName), reportContentPrefix, Utils.UTF8EncodingNoBOM);
					}
				}

			} catch(PathTooLongException ex) {
				console.WriteLine("Error " + ex.GetType() + ": " + ex.Message);
				success = false;

			} catch(Exception ex) {
				OnException(new AVD3CLException("GeneratingReports", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
				success = false;
			}
		}
		console.WriteLine(linesToWrite);
		return success;
	}
	private async Task<bool> HandleEvent(FileMetaInfo fileMetaInfo) {
		var success = true;

		var fileProcessedEventArgs = new AVD3CLFileProcessedEventArgs(fileMetaInfo);
		try {
			FileProcessed?.Invoke(this, fileProcessedEventArgs);
		} catch(Exception ex) {
			OnException(new AVD3CLException("FileProcessedEvent", ex) { Data = { { "FilePath", new SensitiveData(fileMetaInfo.FileInfo.FullName) } } });
			success = false;
		}
		success &= (await Task.WhenAll(fileProcessedEventArgs.ProcessingTasks).ConfigureAwait(false)).All(x => x);

		return success;
	}
	private async Task<bool> HandleFileMove(FileMetaInfo fileMetaInfo) {
		var success = true;
		if(fileMove != null) {
			using var fileMoveScoped = fileMove.CreateScope();
			using var clLock = settings.FileMove.Test ? console.LockConsole() : null;

			try {
				var moveFile = true;
				var actionKey = ' ';
				var repeat = settings.FileMove.Test;
				do {

					string? destFilePath = null;
					try {
						if(settings.FileMove.Test) fileMoveScoped.Load();

						destFilePath = await fileMoveScoped.GetFilePathAsync(fileMetaInfo);


						foreach(var (Value, Replacement) in settings.FileMove.Replacements) {
							destFilePath = destFilePath.InvReplace(Value, Replacement);
						}

						if(settings.FileMove.DisableFileMove) {
							destFilePath = Path.Combine(Path.GetDirectoryName(fileMetaInfo.FileInfo.FullName) ?? "", Path.GetFileName(destFilePath));
						}
						if(settings.FileMove.DisableFileRename) {
							destFilePath = Path.Combine(Path.GetDirectoryName(destFilePath) ?? "", Path.GetFileName(fileMetaInfo.FileInfo.FullName));
						}



					} catch(Exception) {
						destFilePath = null;
					}


					if(settings.FileMove.Test) {
						System.Console.WriteLine();
						System.Console.WriteLine();
						System.Console.WriteLine("FileMove.Test Enabled" + (settings.FileMove.DisableFileMove ? " (DisableFileMove Enabled!)" : "") + (settings.FileMove.DisableFileRename ? " (DisableFileRename Enabled!)" : ""));
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

							if(!string.IsNullOrEmpty(settings.FileMove.LogPath)) {
								lineWriter.AppendLine(settings.FileMove.LogPath, originalPath + " => " + destFilePath);
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

	public void WriteLine(string value) => console.WriteLine(value);


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

	public void Shutdown() { }


	public static AVD3ModuleManagement Create(string moduleDirectory) {
		var moduleManagement = CreateModules(moduleDirectory);
		moduleManagement.RaiseIntialize();
		return moduleManagement;
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

	public static bool Run(AVD3ModuleManagement moduleManagement) {
		var moduleInitResult = moduleManagement.RaiseInitialized();
		if(moduleInitResult.CancelStartup) {
			if(!string.IsNullOrEmpty(moduleInitResult.Reason)) {
				System.Console.WriteLine("Startup Cancel: " + moduleInitResult.Reason);
			}
			return false;
		}
		return true;
	}

	private static AVD3ModuleManagement CreateModules(string moduleDirectory) {
		var moduleManagement = new AVD3ModuleManagement();
		moduleManagement.LoadModuleFromType(typeof(AVD3CLModule));
		if(!string.IsNullOrEmpty(moduleDirectory)) moduleManagement.LoadModules(moduleDirectory);
		moduleManagement.LoadModuleFromType(typeof(AVD3InformationModule));
		moduleManagement.LoadModuleFromType(typeof(AVD3ProcessingModule));
		moduleManagement.LoadModuleFromType(typeof(AVD3ReportingModule));
		moduleManagement.LoadModuleFromType(typeof(AVD3SettingsModule));
		return moduleManagement;
	}

}
