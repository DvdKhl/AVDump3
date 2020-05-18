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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace AVDump3CL {
	public class AVD3CLModuleExceptionEventArgs : EventArgs {
		public AVD3CLModuleExceptionEventArgs(XElement exception) {
			Exception = exception;
		}

		public XElement Exception { get; private set; }
	}

	public class AVD3CLFileProcessedEventArgs : EventArgs {
		public string FilePath { get; }
		public ReadOnlyCollection<IBlockConsumer> BlockConsumers { get; }

		public AVD3CLFileProcessedEventArgs(string filePath, IEnumerable<IBlockConsumer> blockConsumers) {
			FilePath = filePath;
			BlockConsumers = Array.AsReadOnly(blockConsumers.ToArray());
		}
	}


	public interface IAVD3CLModule : IAVD3Module {
		event EventHandler<AVD3CLModuleExceptionEventArgs> ExceptionThrown;
		event EventHandler<AVD3CLFileProcessedEventArgs> FileProcessed;

		void WriteLine(string value);
	}


	public class AVD3CLModule : IAVD3CLModule {
		public event EventHandler<AVD3CLModuleExceptionEventArgs>? ExceptionThrown;
		public event EventHandler<AVD3CLFileProcessedEventArgs>? FileProcessed;

		private HashSet<string> filePathsToSkip = new HashSet<string>();

		private readonly AVD3CLModuleSettings settings = new AVD3CLModuleSettings();
		private readonly object fileSystemLock = new object();
		private IAVD3ProcessingModule processingModule;
		private IAVD3InformationModule informationModule;
		private IAVD3ReportingModule reportingModule;
		private AVD3CL cl;

		public AVD3CLModule() {
			AppDomain.CurrentDomain.UnhandledException += UnhandleException;
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

					using var safeXmlWriter = new SafeXmlWriter(filePath, Encoding.UTF8);
					exElem.WriteTo(safeXmlWriter);
				}
			}

			var exception = ex.GetBaseException() ?? ex;
			cl.Writeline("Error " + exception.GetType() + ": " + exception.Message);
			ExceptionThrown?.Invoke(this, new AVD3CLModuleExceptionEventArgs(exElem));
			//TODO Raise Event for modules to listen to
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			processingModule.BlockConsumerFilter += (s, e) => {
				if(settings.Processing.Consumers.Any(x => e.BlockConsumerName.InvEqualsOrdCI(x.Name))) {
					e.Accept();
				}
			};

			informationModule = modules.OfType<IAVD3InformationModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterSettings(settings.Diagnostics);
			settingsgModule.RegisterSettings(settings.Display);
			settingsgModule.RegisterSettings(settings.FileDiscovery);
			settingsgModule.RegisterSettings(settings.Processing);
			settingsgModule.RegisterSettings(settings.Reporting);

			settingsgModule.AfterConfiguration += AfterConfiguration;
		}
		public ModuleInitResult Initialized() => new ModuleInitResult(false);
		public void AfterConfiguration(object sender, ModuleInitResult args) {
			processingModule.RegisterDefaultBlockConsumers(settings.Processing.Consumers?.ToDictionary(x => x.Name, x => x.Arguments));

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
				path = Path.GetDirectoryName(path);
				if(!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);
			}

			if(settings.Diagnostics.NullStreamTest != null && settings.Reporting.Reports.Count > 0) {
				Console.WriteLine("NullStreamTest cannot be used with reports");
				args.Cancel();
			}



			CreateDirectoryChain(settings.FileDiscovery.ProcessedLogPath);
			CreateDirectoryChain(settings.FileDiscovery.SkipLogPath);
			CreateDirectoryChain(settings.Reporting.CRC32Error.Path);
			CreateDirectoryChain(settings.Reporting.ExtensionDifferencePath);
			CreateDirectoryChain(settings.Reporting.ReportDirectory, true);
			CreateDirectoryChain(settings.Diagnostics.ErrorDirectory, true);
		}


		public NullStreamProvider CreateNullStreamProvider() {
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
			cl = new AVD3CL(settings.Display, bytesReadProgress.GetProgress);

			var sp = settings.Diagnostics.NullStreamTest != null ? CreateNullStreamProvider() : CreateFileStreamProvider(paths);
			var streamConsumerCollection = processingModule.CreateStreamConsumerCollection(sp,
				settings.Processing.BufferLength,
				settings.Processing.ProducerMinReadLength,
				settings.Processing.ProducerMaxReadLength
			);

			using(cl)
			using(sp as IDisposable)
			using(var cts = new CancellationTokenSource()) {
				cl.Display();

				streamConsumerCollection.ConsumingStream += ConsumingStream;

				void cancelKeyHandler(object s, ConsoleCancelEventArgs e) {
					Console.CancelKeyPress -= cancelKeyHandler;
					e.Cancel = true;
					cts.Cancel();
				}
				Console.CancelKeyPress += cancelKeyHandler;
				Console.CursorVisible = false;
				try {
					streamConsumerCollection.ConsumeStreams(cts.Token, bytesReadProgress);

				} catch(OperationCanceledException) {

				} finally {
					cl.Stop();
					Console.CursorVisible = true;
				}
			}

			if(settings.Processing.PauseBeforeExit) {
				Console.Read();
			}
		}

		private IStreamProvider CreateFileStreamProvider(string[] paths) {
			IStreamProvider sp;

			var acceptedFiles = 0;
			var acceptedFileCountCursorTop = Console.CursorTop++;
			var fileDiscoveryOn = DateTimeOffset.UtcNow;
			var spp = new StreamFromPathsProvider(settings.FileDiscovery.Concurrent, paths, true,
				path => {
					if(fileDiscoveryOn.AddSeconds(1) < DateTimeOffset.UtcNow) {
						//var currentCursorTop = Console.CursorTop;
						//Console.CursorTop = acceptedFileCountCursorTop;
						Console.WriteLine("Accepted files: " + acceptedFiles);
						//Console.CursorTop = currentCursorTop;
						fileDiscoveryOn = DateTimeOffset.UtcNow;
					}

					var accept = settings.FileDiscovery.WithExtensions.Allow == (
						settings.FileDiscovery.WithExtensions.Items.Count == 0 ||
						settings.FileDiscovery.WithExtensions.Items.Any(
							fe => path.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase)
						)
					) && !filePathsToSkip.Contains(path);

					if(accept) acceptedFiles++;
					return accept;
				},
				ex => Console.WriteLine("Filediscovery: " + ex.Message)
			);
			Console.WriteLine("Accepted files: " + acceptedFiles);
			Console.WriteLine();

			cl.TotalFiles = spp.TotalFileCount;
			cl.TotalBytes = spp.TotalBytes;

			sp = spp;
			return sp;
		}

		private async void ConsumingStream(object sender, ConsumingStreamEventArgs e) {
			var filePath = (string)e.Tag;
			var fileName = Path.GetFileName(filePath);

			var hasProcessingError = false;
			e.OnException += (s, args) => {
				args.IsHandled = true;
				args.Retry = args.RetryCount < 2;
				hasProcessingError = !args.IsHandled;

				OnException(new AVD3CLException("ConsumingStream", args.Cause) { Data = { { "FileName", new SensitiveData(fileName) } } });
			};

			var blockConsumers = await e.FinishedProcessing;

			if(hasProcessingError) return;

			var linesToWrite = new List<string>(32);
			if(settings.Reporting.PrintHashes || settings.Reporting.PrintReports) {
				linesToWrite.Add(fileName);
			}

			if(settings.Reporting.PrintHashes) {
				foreach(var bc in blockConsumers.OfType<HashCalculator>()) {
					linesToWrite.Add(bc.Name + " => " + BitConverter.ToString(bc.HashValue.ToArray()).Replace("-", ""));
				}
				linesToWrite.Add("");
			}

			MetaDataProvider[] infoProviders;
			try {
				var infoSetup = new InfoProviderSetup(filePath, blockConsumers);
				infoProviders = informationModule.InfoProviderFactories.Select(x => x.Create(infoSetup)).ToArray();

			} catch(Exception ex) {
				OnException(new AVD3CLException("CreatingInfoProviders", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
				return;
			}

			var fileMetaInfo = new FileMetaInfo(new FileInfo(filePath), infoProviders);

			if(!string.IsNullOrEmpty(settings.Reporting.CRC32Error.Path)) {
				var hashProvider = fileMetaInfo.CondensedProviders.Where(x => x.Type == HashProvider.HashProviderType).Single();
				var crc32Hash = (ReadOnlyMemory<byte>)hashProvider.Items.First(x => x.Type.Key.Equals("CRC32")).Value;
				var crc32HashStr = BitConverter.ToString(crc32Hash.ToArray(), 0).Replace("-", "");

				if(!Regex.IsMatch(fileMetaInfo.FileInfo.FullName, settings.Reporting.CRC32Error.Pattern.Replace("<CRC32>", crc32HashStr))) {
					lock(settings.Reporting) {
						File.AppendAllText(
							settings.Reporting.CRC32Error.Path,
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

			try {
				FileProcessed?.Invoke(this, new AVD3CLFileProcessedEventArgs(filePath, blockConsumers));
			} catch(Exception ex) {
				OnException(new AVD3CLException("FileProcessedEvent", ex) { Data = { { "FileName", new SensitiveData(fileName) } } });
				success = false;
			}

			if(settings.FileDiscovery.ProcessedLogPath != null && success) {
				lock(settings.FileDiscovery) File.AppendAllText(settings.FileDiscovery.ProcessedLogPath, filePath + "\n");
			}

		}

		public void WriteLine(string value) => cl.Writeline(value);
	}

	public class AVD3CLException : AVD3LibException {
		public AVD3CLException(string message, Exception innerException) : base(message, innerException) {

		}
	}
}
