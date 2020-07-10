using AVDump3Lib;
using AVDump3Lib.Information;
using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using ExtKnot.StringInvariants;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3CL {
	public class AVD3CLModuleExceptionEventArgs : EventArgs {
		public AVD3CLModuleExceptionEventArgs(XElement exception) {
			Exception = exception;
		}

		public XElement Exception { get; private set; }
	}

	public class AVD3CLFileProcessedEventArgs : EventArgs {
		public FileMetaInfo FileMetaInfo { get; }

		private readonly List<Task<bool>> processingTasks = new List<Task<bool>>();
		private readonly Dictionary<string, string> fileMoveTokens = new Dictionary<string, string>();

		public IEnumerable<Task<bool>> ProcessingTasks => processingTasks;
		public IReadOnlyDictionary<string, string> FileMoveTokens => fileMoveTokens;

		public void AddProcessingTask(Task<bool> processingTask) => processingTasks.Add(processingTask);

		public void AddFileMoveToken(string key, string value) => fileMoveTokens.Add(key, value);

		public AVD3CLFileProcessedEventArgs(FileMetaInfo fileMetaInfo) => FileMetaInfo = fileMetaInfo;
	}


	public interface IAVD3CLModule : IAVD3Module {
		event EventHandler<AVD3CLModuleExceptionEventArgs> ExceptionThrown;
		event EventHandler<AVD3CLFileProcessedEventArgs> FileProcessed;
		event EventHandler<StringBuilder> AdditionalLines;
		event EventHandler ProcessingFinished;

		void RegisterShutdownDelay(WaitHandle waitHandle);

		void WriteLine(params string[] values);
	}

	public interface IAVDMoveFileExtension {
		void BuildServiceCollection(IServiceCollection services);
	}

	public class AVDMoveFileScriptGlobal {
		public AVDMoveFileScriptGlobal(Func<string, string> getHandler, IServiceProvider serviceProvider) {
			Get = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
			ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public Func<string, string> Get { get; }
		public IServiceProvider ServiceProvider { get; }
	}


	public class AVD3CLModule : IAVD3CLModule {
		public event EventHandler<AVD3CLModuleExceptionEventArgs>? ExceptionThrown;
		public event EventHandler<AVD3CLFileProcessedEventArgs>? FileProcessed;
		public event EventHandler ProcessingFinished;

		public event EventHandler<StringBuilder> AdditionalLines { add { cl.AdditionalLines += value; } remove { cl.AdditionalLines -= value; } }

		private HashSet<string> filePathsToSkip = new HashSet<string>();
		private IServiceProvider fileMoveServiceProvider;
		private ScriptRunner<string> fileMoveScriptRunner;

		private readonly AVD3CLModuleSettings settings = new AVD3CLModuleSettings();
		private readonly object fileSystemLock = new object();
		private readonly List<WaitHandle> shutdownDelayHandles = new List<WaitHandle>();
		private IAVD3ProcessingModule processingModule;
		private IAVD3InformationModule informationModule;
		private IAVD3ReportingModule reportingModule;
		private AVD3CL cl;

		public AVD3CLModule() {
			AppDomain.CurrentDomain.UnhandledException += UnhandleException;
			cl = new AVD3CL(settings.Display);
		}

		private void UnhandleException(object sender, UnhandledExceptionEventArgs e) {
			var wrapEx = new AVD3CLException(
				"Unhandled AppDomain wide Exception",
				e.ExceptionObject as Exception ?? new Exception("Non Exception Type: " + e.ExceptionObject.ToString())
			);

			OnException(wrapEx);
		}

		private void OnException(AVD3CLException ex) {
			var exElem = ex.ToXElement(
				settings.Diagnostics.SkipEnvironmentElement,
				settings.Diagnostics.IncludePersonalData
			);

			if(settings.Diagnostics.SaveErrors) {
				lock(fileSystemLock) {
					Directory.CreateDirectory(settings.Diagnostics.ErrorDirectory);
					var filePath = Path.Combine(settings.Diagnostics.ErrorDirectory, "AVD3Error" + ex.ThrownOn.ToString("yyyyMMdd HHmmssffff") + ".xml");

					using var fileStream = File.OpenWrite(filePath);
					using var xmlWriter = System.Xml.XmlWriter.Create(fileStream, new System.Xml.XmlWriterSettings { CheckCharacters = false, Encoding = Encoding.UTF8 });
					exElem.WriteTo(xmlWriter);
				}
			}

			var exception = ex.GetBaseException() ?? ex;
			cl.Writeline("Error " + exception.GetType() + ": " + exception.Message);
			ExceptionThrown?.Invoke(this, new AVD3CLModuleExceptionEventArgs(exElem));
			//TODO Raise Event for modules to listen to
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			var services = new ServiceCollection();
			foreach(var module in modules.OfType<IAVDMoveFileExtension>()) {
				module.BuildServiceCollection(services);
			}
			fileMoveServiceProvider = services.BuildServiceProvider();


			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			processingModule.BlockConsumerFilter += (s, e) => {
				if(settings.Processing.Consumers.Any(x => e.BlockConsumerName.InvEqualsOrdCI(x.Name))) {
					e.Accept();
				}
			};

			processingModule.FilePathFilter += (s, e) => {
				var accept = settings.FileDiscovery.WithExtensions.Allow == (
					settings.FileDiscovery.WithExtensions.Items.Count == 0 ||
					settings.FileDiscovery.WithExtensions.Items.Any(
						fe => e.FilePath.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase)
					)
				) && !filePathsToSkip.Contains(e.FilePath);
				if(!accept) e.Decline();
			};

			informationModule = modules.OfType<IAVD3InformationModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterSettings(settings.Diagnostics);
			settingsgModule.RegisterSettings(settings.Display);
			settingsgModule.RegisterSettings(settings.FileDiscovery);
			settingsgModule.RegisterSettings(settings.Processing);
			settingsgModule.RegisterSettings(settings.Reporting);
			settingsgModule.RegisterSettings(settings.FileMove);

			settingsgModule.AfterConfiguration += AfterConfiguration;
		}
		public ModuleInitResult Initialized() => new ModuleInitResult(false);
		public void AfterConfiguration(object? sender, ModuleInitResult args) {
			processingModule.RegisterDefaultBlockConsumers((settings.Processing.Consumers ?? Array.Empty<ProcessingSettings.ConsumerSettings>()).ToDictionary(x => x.Name, x => x.Arguments));

			if(settings.Processing.Consumers == null) {
				Console.WriteLine("Available Consumers: ");
				foreach(var blockConsumerFactory in processingModule.BlockConsumerFactories) {
					Console.WriteLine(blockConsumerFactory.Name.PadRight(14) + " - " + blockConsumerFactory.Description);
				}
				args.Cancel();

			} else if(settings.Processing.Consumers.Any()) {
				var invalidBlockConsumerNames = settings.Processing.Consumers.Where(x => processingModule.BlockConsumerFactories.All(y => !y.Name.InvEqualsOrdCI(x.Name))).ToArray();
				if(invalidBlockConsumerNames.Any()) {
					Console.WriteLine("Invalid BlockConsumer(s): " + string.Join(", ", invalidBlockConsumerNames.Select(x => x.Name)));
					args.Cancel();
				}
			}


			if(settings.Reporting.Reports == null) {
				Console.WriteLine("Available Reports: ");
				foreach(var reportFactory in reportingModule.ReportFactories) {
					Console.WriteLine(reportFactory.Name.PadRight(14) + " - " + reportFactory.Description);
				}
				args.Cancel();

			} else if(settings.Reporting.Reports.Any()) {
				var invalidReportNames = settings.Reporting.Reports.Where(x => reportingModule.ReportFactories.All(y => !y.Name.InvEqualsOrdCI(x))).ToArray();
				if(invalidReportNames.Any()) {
					Console.WriteLine("Invalid Report: " + string.Join(", ", invalidReportNames));
					args.Cancel();
				}
			}


			if(settings.Processing.PrintAvailableSIMDs) {
				Console.WriteLine("Available SIMD Instructions: ");
				foreach(var flagValue in Enum.GetValues(typeof(CPUInstructions)).OfType<CPUInstructions>().Where(x => (x & processingModule.AvailableSIMD) != 0)) {
					Console.WriteLine(flagValue);
				}
				args.Cancel();
			}

			if(File.Exists(settings.FileDiscovery.SkipLogPath)) {
				filePathsToSkip = new HashSet<string>(File.ReadLines(settings.FileDiscovery.SkipLogPath));
			}

			static void CreateDirectoryChain(string? path, bool isDirectory = false) {
				if(!isDirectory) path = Path.GetDirectoryName(path);
				if(!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
			}

			if(settings.Diagnostics.NullStreamTest != null && settings.Reporting.Reports?.Count > 0) {
				Console.WriteLine("NullStreamTest cannot be used with reports");
				args.Cancel();
			}

			if(settings.FileMove.Mode != FileMoveMode.None) {
				var scriptString = settings.FileMove.Mode switch
				{
					FileMoveMode.Placeholder => "return \"" + Regex.Replace(settings.FileMove.Pattern.Replace("\\", "\\\\").Replace("\"", "\\\""), @"\{([A-Za-z0-9-]+)\}", @""" + Get(""$1"") + """) + "\";",
					FileMoveMode.CSharpScriptFile => File.ReadAllText(settings.FileMove.Pattern),
					FileMoveMode.CSharpScriptInline => throw new NotImplementedException(),
					_ => throw new InvalidOperationException(),
				};
				var fileMoveScript = CSharpScript.Create<string>(scriptString, ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies()), typeof(AVDMoveFileScriptGlobal));
				fileMoveScriptRunner = fileMoveScript.CreateDelegate();
			}


			CreateDirectoryChain(settings.FileDiscovery.ProcessedLogPath);
			CreateDirectoryChain(settings.FileDiscovery.SkipLogPath);
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

			cl.TotalFiles = nsp.StreamCount;
			cl.TotalBytes = nsp.StreamCount * nsp.StreamLength;

			return nsp;
		}

		public void Process(string[] paths) {
			var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));

			var sp = settings.Diagnostics.NullStreamTest != null ? CreateNullStreamProvider() : CreateFileStreamProvider(paths);
			var streamConsumerCollection = processingModule.CreateStreamConsumerCollection(sp,
				settings.Processing.BufferLength,
				settings.Processing.ProducerMinReadLength,
				settings.Processing.ProducerMaxReadLength
			);

			using(cl)
			using(sp as IDisposable)
			using(var cts = new CancellationTokenSource()) {
				cl.IsProcessing = true;
				cl.Display(bytesReadProgress.GetProgress);

				void cancelKeyHandler(object s, ConsoleCancelEventArgs e) {
					Console.CancelKeyPress -= cancelKeyHandler;
					e.Cancel = true;
					cts.Cancel();
				}
				Console.CancelKeyPress += cancelKeyHandler;
				Console.CursorVisible = false;
				try {
					streamConsumerCollection.ConsumeStreams(ConsumingStream, cts.Token, bytesReadProgress);
					cl.IsProcessing = false;
					ProcessingFinished?.Invoke(this, EventArgs.Empty);

					var shutdownDelayHandles = this.shutdownDelayHandles.ToArray();
					if(shutdownDelayHandles.Length > 0) WaitHandle.WaitAll(shutdownDelayHandles);

				} catch(OperationCanceledException) {

				} finally {
					cl.Stop();
					Console.CursorVisible = true;
				}
			}

			if(settings.Processing.PauseBeforeExit) {
				Console.WriteLine("Program execution has finished. Press any key to exit.");
				Console.Read();
			}
		}

		private IStreamProvider CreateFileStreamProvider(string[] paths) {
			var acceptedFiles = 0;
			var fileDiscoveryOn = DateTimeOffset.UtcNow;
			var sp = (StreamFromPathsProvider)processingModule.CreateFileStreamProvider(
				paths, settings.FileDiscovery.Recursive, settings.FileDiscovery.Concurrent,
				path => {
					if(fileDiscoveryOn.AddSeconds(1) < DateTimeOffset.UtcNow) {
						Console.WriteLine("Accepted files: " + acceptedFiles);
						fileDiscoveryOn = DateTimeOffset.UtcNow;
					}
					acceptedFiles++;
				},
				ex => {
					if(!(ex is UnauthorizedAccessException) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
						Console.WriteLine("Filediscovery: " + ex.Message);
					}
				}
			);
			Console.WriteLine("Accepted files: " + acceptedFiles);
			Console.WriteLine();

			cl.TotalFiles = sp.TotalFileCount;
			cl.TotalBytes = sp.TotalBytes;

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

				var success = fileMetaInfo != null;
				success = success && await HandleReporting(fileMetaInfo);
				success = success && await HandleEvent(fileMetaInfo);
				success = success && await HandleFileMove(fileMetaInfo);

				//if(UseNtfsAlternateStreams) {
				//	using(var altStreamHandle = NtfsAlternateStreams.SafeCreateFile(
				//		NtfsAlternateStreams.BuildStreamPath((string)e.Tag, "AVDump3.xml"),
				//		NtfsAlternateStreams.ToNative(FileAccess.ReadWrite), FileShare.None,
				//		IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero))
				//	using(var altStream = new FileStream(altStreamHandle, FileAccess.ReadWrite)) {
				//		var avd3Elem = new XElement("AVDump3",
				//		  new XElement("Revision",
				//			new XAttribute("Build", Assembly.GetExecutingAssembly().GetName().Version.Build),
				//			blockConsumers.OfType<HashCalculator>().Select(hc =>
				//			  new XElement(hc.HashAlgorithmType.Key, BitConverter.ToString(hc.HashAlgorithm.Hash).Replace("-", ""))
				//			)
				//		  )
				//		);
				//		avd3Elem.Save(altStream, SaveOptions.None);
				//	}
				//}

				if(!string.IsNullOrEmpty(settings.FileDiscovery.ProcessedLogPath) && success) {
					lock(settings.FileDiscovery) File.AppendAllText(settings.FileDiscovery.ProcessedLogPath, fileMetaInfo.FileInfo.FullName + "\n");
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
				var crc32Hash = (ReadOnlyMemory<byte>)hashProvider.Items.First(x => x.Type.Key.Equals("CRC32")).Value;
				var crc32HashStr = BitConverter.ToString(crc32Hash.ToArray(), 0).Replace("-", "");

				if(!Regex.IsMatch(fileMetaInfo.FileInfo.FullName, settings.Reporting.CRC32Error?.Pattern.Replace("<CRC32>", crc32HashStr))) {
					lock(settings.Reporting) {
						File.AppendAllText(
							settings.Reporting.CRC32Error?.Path,
							crc32HashStr + " " + fileMetaInfo.FileInfo.FullName + Environment.NewLine
						);
					}
				}
			}

			if(!string.IsNullOrEmpty(settings.Reporting.ExtensionDifferencePath)) {
				var metaDataProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == MediaProvider.MediaProviderType).Single();
				var detExts = metaDataProvider.Select(MediaProvider.SuggestedFileExtensionType)?.Value ?? ImmutableArray.Create<string>();
				var ext = fileMetaInfo.FileInfo.Extension.StartsWith('.') ? fileMetaInfo.FileInfo.Extension.Substring(1) : fileMetaInfo.FileInfo.Extension;

				if(!detExts.Contains(ext, StringComparer.OrdinalIgnoreCase)) {
					if(detExts.Length == 0) detExts = ImmutableArray.Create("unknown");

					lock(settings.Reporting) {
						File.AppendAllText(
							settings.Reporting.ExtensionDifferencePath,
							ext + " => " + string.Join(" ", detExts) + "\t" + fileMetaInfo.FileInfo.FullName + Environment.NewLine
						);
					}
				}
			}

			var success = true;
			var reportsFactories = reportingModule.ReportFactories.Where(x => settings.Reporting.Reports.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase))).ToArray();
			if(reportsFactories.Length != 0) {

				try {

					var reportItems = reportsFactories.Select(x => new { x.Name, Report = x.Create(fileMetaInfo) });

					foreach(var reportItem in reportItems) {
						if(settings.Reporting.PrintReports) {
							linesToWrite.Add(reportItem.Report.ReportToString(Utils.UTF8EncodingNoBOM) + "\n");
						}

						var reportFileName = settings.Reporting.ReportFileName;
						reportFileName = reportFileName.Replace("<FileName>", fileName);
						reportFileName = reportFileName.Replace("<FileNameWithoutExtension>", Path.GetFileNameWithoutExtension(fileName));
						reportFileName = reportFileName.Replace("<FileExtension>", Path.GetExtension(fileName).Replace(".", ""));
						reportFileName = reportFileName.Replace("<ReportName>", reportItem.Name);
						reportFileName = reportFileName.Replace("<ReportFileExtension>", reportItem.Report.FileExtension);

						lock(fileSystemLock) {
							reportItem.Report.SaveToFile(Path.Combine(settings.Reporting.ReportDirectory, reportFileName), Utils.UTF8EncodingNoBOM);
						}
					}

				} catch(Exception ex) {
					OnException(new AVD3CLException("GeneratingReports", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
					success = false;
				}
			}
			cl.Writeline(linesToWrite);
			return success;
		}
		private async Task<bool> HandleFileMove(FileMetaInfo fileMetaInfo) {
			var success = true;

			var destFilePath = fileMetaInfo.FileInfo.FullName;
			if(settings.FileMove.Mode != FileMoveMode.None) {
				try {
					string GetValue(string key) {
						var value = key switch
						{
							"FullName" => fileMetaInfo.FileInfo.FullName,
							"FileName" => fileMetaInfo.FileInfo.Name,
							"FileExtension" => fileMetaInfo.FileInfo.Extension,
							"FileNameWithoutExtension" => Path.GetFileNameWithoutExtension(fileMetaInfo.FileInfo.FullName),
							"DirectoryName" => fileMetaInfo.FileInfo.DirectoryName,
							"DetectedExtension" => fileMetaInfo.CondensedProviders.OfType<MediaProvider>().FirstOrDefault()?.Select(MediaProvider.SuggestedFileExtensionType)?.Value.FirstOrDefault() ?? fileMetaInfo.FileInfo.Extension,
							_ => "",
						};

						if(key.StartsWith("Hash")) {
							var m = Regex.Match(key, @"Hash-(?<Name>[^-]+)-(?<Base>\d+)-(?<Case>UC|LC)");
							if(m.Success) {
								var hashName = m.Groups["Name"].Value;
								var withBase = m.Groups["Base"].Value;
								var useUppercase = m.Groups["Case"].Value.InvEqualsOrd("UC");

								var hashData = fileMetaInfo.CondensedProviders.OfType<HashProvider>().FirstOrDefault()?.Select<HashInfoItemType, ReadOnlyMemory<byte>>(hashName).Value.ToArray();
								if(hashData != null) value = BitConverterEx.ToBase(hashData, BitConverterEx.Bases[withBase]).Transform(x => useUppercase ? x.ToInvUpper() : x.ToInvLower());
							}
						}
						return value;
					}

					destFilePath = await fileMoveScriptRunner(new AVDMoveFileScriptGlobal(GetValue, fileMoveServiceProvider));

					if(settings.FileMove.DisableFileMove) {
						destFilePath = Path.Combine(Path.GetDirectoryName(fileMetaInfo.FileInfo.FullName) ?? "", Path.GetFileName(destFilePath));
					}
					if(settings.FileMove.DisableFileRename) {
						destFilePath = Path.Combine(Path.GetDirectoryName(destFilePath) ?? "", Path.GetFileName(fileMetaInfo.FileInfo.FullName));
					}

					await Task.Run(() => {
						var originalPath = fileMetaInfo.FileInfo.FullName;
						fileMetaInfo.FileInfo.MoveTo(destFilePath);

						if(!string.IsNullOrEmpty(settings.FileMove.LogPath)) {
							lock(settings.FileMove.LogPathProperty) {
								File.AppendAllText(settings.FileMove.LogPath, originalPath + " => " + destFilePath + Environment.NewLine);
							}
						}
					}).ConfigureAwait(false);

				} catch(Exception) {
					success = false;
				}
			}
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


		public void WriteLine(params string[] values) => cl.Writeline(values);


		public void RegisterShutdownDelay(WaitHandle waitHandle) => shutdownDelayHandles.Add(waitHandle);
	}

	public class AVD3CLException : AVD3LibException {
		public AVD3CLException(string message, Exception innerException) : base(message, innerException) { }
		public AVD3CLException(string message) : base(message) { }
	}
}
