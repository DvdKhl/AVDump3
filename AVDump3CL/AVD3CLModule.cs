using AVDump3Lib;
using AVDump3Lib.Information;
using AVDump3Lib.Information.InfoProvider;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
	}


	public class AVD3CLModule : IAVD3CLModule {
		public event EventHandler<AVD3CLModuleExceptionEventArgs> ExceptionThrown;
		public event EventHandler<AVD3CLFileProcessedEventArgs> FileProcessed;

		private AVD3CLModuleSettings settings = new AVD3CLModuleSettings();

		private IAVD3ProcessingModule processingModule;
		private IAVD3InformationModule informationModule;
		private IAVD3ReportingModule reportingModule;
		private AVD3CL cl;

		public AVD3CLModule() {
			AppDomain.CurrentDomain.UnhandledException += UnhandleException;
		}

		private void UnhandleException(object sender, UnhandledExceptionEventArgs e) {
			var wrapEx = new AVD3CLException("Unhandled AppDomain wide Exception",
				e.ExceptionObject as Exception ?? new Exception("Non Exception Type: " + e.ExceptionObject.ToString()));
			OnException(wrapEx);
		}

		private void OnException(AVD3CLException ex) {
			var exElem = ex.ToXElement(
				settings.Diagnostics.SkipEnvironmentElement,
				settings.Diagnostics.IncludePersonalData
			);

			ExceptionThrown?.Invoke(this, new AVD3CLModuleExceptionEventArgs(exElem));
			//TODO Raise Event for modules to listen to

			if(settings.Diagnostics.SaveErrors) {
				Directory.CreateDirectory(settings.Diagnostics.ErrorDirectory);
				var filePath = Path.Combine(settings.Diagnostics.ErrorDirectory, "AVD3Error" + ex.ThrownOn.ToString("yyyyMMdd HHmmssffff") + ".xml");

				using(var safeXmlWriter = new SafeXmlWriter(filePath, Encoding.UTF8)) {
					exElem.WriteTo(safeXmlWriter);
				}
			}

		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			informationModule = modules.OfType<IAVD3InformationModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterSettings(settings.Diagnostics);
			settingsgModule.RegisterSettings(settings.Display);
			settingsgModule.RegisterSettings(settings.FileDiscovery);
			settingsgModule.RegisterSettings(settings.Processing);
			settingsgModule.RegisterSettings(settings.Reporting);

		}
		public void BeforeConfiguration() { }
		public void AfterConfiguration() { }


		public NullStreamProvider CreateNullStreamProvider() {
			return new NullStreamProvider(
				settings.Diagnostics.NullStreamTest.StreamCount,
				settings.Diagnostics.NullStreamTest.StreamLength,
				settings.Diagnostics.NullStreamTest.ParallelStreamCount);
		}

		public void Process(string[] paths) {
			//if(UsedBlockConsumerNames.Count == 0) {
			//	Console.WriteLine("No Blockconsumer chosen: Nothing to do");
			//	return;
			//}
			if(settings.Processing.Consumers.Count == 0) {
				Console.WriteLine("Available Consumers: ");
				foreach(var name in processingModule.BlockConsumerFactories.Select(x => x.Name)) {
					Console.WriteLine(name);
				}
				return;
			}


			var bcs = new BlockConsumerSelector(processingModule.BlockConsumerFactories);
			bcs.Filter += BlockConsumerFilter;

			var bp = new BlockPool(settings.Processing.BlockSize.BlockCount, settings.Processing.BlockSize.BlockLength);

			var fileDiscoveryOn = DateTimeOffset.UtcNow;
			var acceptedFiles = 0;

			var scf = new StreamConsumerFactory(bcs, bp);
			var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));
			cl = new AVD3CL(settings.Display, bytesReadProgress.GetProgress);

			IStreamProvider sp;
			if(settings.Diagnostics.NullStreamTest != null) {
				var nsp = CreateNullStreamProvider();
				sp = nsp;

				cl.TotalFiles = nsp.StreamCount;
				cl.TotalBytes = nsp.StreamCount * nsp.StreamLength;

			} else {
				var spp = new StreamFromPathsProvider(settings.FileDiscovery.Concurrent, paths, true,
					path => {
						if(fileDiscoveryOn.AddSeconds(1) < DateTimeOffset.UtcNow) {
							Console.WriteLine("Accepted files: " + acceptedFiles);
							Console.CursorTop--;
							fileDiscoveryOn = DateTimeOffset.UtcNow;
						}

						var accept = settings.FileDiscovery.WithExtensions.Allow == (
							settings.FileDiscovery.WithExtensions.Items.Count == 0 ||
							settings.FileDiscovery.WithExtensions.Items.Any(
								fe => path.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase)
							)
						);

						if(accept) acceptedFiles++;
						return accept;
					},
					ex => Console.Error.WriteLine("Filediscovery: " + ex.Message)
				);
				cl.TotalFiles = spp.TotalFileCount;
				cl.TotalBytes = spp.TotalBytes;

				sp = spp;
			}

			var streamConsumerCollection = new StreamConsumerCollection(scf, sp);

			using(sp as IDisposable)
			using(cl)
			using(var cts = new CancellationTokenSource()) {
				cl.Display();

				streamConsumerCollection.ConsumingStream += ConsumingStream;

				ConsoleCancelEventHandler cancelKeyHandler = (s, e) => {
					e.Cancel = true;
					cts.Cancel();
				};
				Console.CancelKeyPress += cancelKeyHandler;
				Console.CursorVisible = false;
				try {
					streamConsumerCollection.ConsumeStreams(cts.Token, bytesReadProgress);

				} catch(OperationCanceledException ex) {
					Console.WriteLine(ex.Message);

				} finally {
					cl.Stop();
					Console.CancelKeyPress -= cancelKeyHandler;
					Console.CursorVisible = true;
				}
			}

			if(settings.Processing.PauseBeforeExit) {
				Console.Read();
			}
		}

		private void BlockConsumerFilter(object sender, BlockConsumerSelectorEventArgs e) {
			e.Select = settings.Processing.Consumers.Any(x => e.Name.Equals(x, StringComparison.OrdinalIgnoreCase));
		}

		private async void ConsumingStream(object sender, ConsumingStreamEventArgs e) {
			var hasError = false;
			e.OnException += (s, args) => {
				args.IsHandled = true;
				args.Retry = args.RetryCount < 2;
				hasError = !args.IsHandled;

				OnException(new AVD3CLException("ConsumingStream", args.Cause));
			};

			var blockConsumers = await e.FinishedProcessing;

			var filePath = (string)e.Tag;
			var fileName = Path.GetFileName(filePath);

			if(hasError) {
				return;
			}
			if(settings.Display.PrintHashes || settings.Display.PrintReports) {
				cl.Writeline(fileName.Substring(0, Math.Min(fileName.Length, Console.WindowWidth - 1)));
			}

			if(settings.Display.PrintHashes) {
				foreach(var bc in blockConsumers.OfType<HashCalculator>()) {
					cl.Writeline(bc.Name + " => " + BitConverter.ToString(bc.HashAlgorithm.Hash).Replace("-", ""));
				}
				cl.Writeline("");
			}

			var reportsFactories = reportingModule.ReportFactories.Where(x => settings.Reporting.Reports.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase))).ToArray();
			if(reportsFactories.Length != 0) {
				var infoSetup = new InfoProviderSetup(filePath, blockConsumers);
				var infoProviders = informationModule.InfoProviderFactories.Select(x => x.Create(infoSetup));

				var fileMetaInfo = new FileMetaInfo(new FileInfo(filePath), infoProviders);
				var reports = reportsFactories.Select(x => x.Create(fileMetaInfo));

				foreach(var report in reports) {
					if(settings.Display.PrintReports) {
						cl.Writeline(report.ReportToString() + "\n");
					}

					report.SaveToFile(Path.Combine(settings.Reporting.ReportDirectory, fileName + "." + report.FileExtension));
				}
			}


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

			FileProcessed?.Invoke(this, new AVD3CLFileProcessedEventArgs(filePath, blockConsumers));
		}
	}

	public class AVD3CLException : AVD3LibException {
		public AVD3CLException(string message, Exception innerException) : base(message, innerException) {

		}
	}
}
