using AVDump2Lib.InfoProvider.Tools;
using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockConsumers;
using AVDump3Lib.Information;
using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Media;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3CL {
	public class FileExtensions {
		public bool Allow { get; private set; }
		public IReadOnlyList<string> Extensions { get; private set; }

		public FileExtensions(bool allow, IEnumerable<string> extensions) {
			Extensions = Array.AsReadOnly(extensions.ToArray());
		}

		public static FileExtensions Default { get; } = new FileExtensions(false, new string[0]);
	}

	public class AVD3CLModule : IAVD3Module {
		private IAVD3ProcessingModule processingModule;
		private IAVD3InformationModule informationModule;
		private IAVD3ReportingModule reportingModule;
		private AVD3CL cl;

		public int BlockCount { get; private set; } = 8;
		public int BlockLength { get; private set; } = 8 << 20;
		public int GlobalConcurrentCount { get; private set; } = 1;
		public IReadOnlyList<PathPartition> PathPartitions { get; private set; } = Array.AsReadOnly(new PathPartition[0]);
		public IReadOnlyList<string> UsedBlockConsumerNames { get; private set; } = Array.AsReadOnly(new string[0]);
		public IReadOnlyList<string> UsedReportNames { get; private set; } = Array.AsReadOnly(new string[0]);
		public FileExtensions FileExtensions { get; private set; } = FileExtensions.Default;
		public string ReportDirectory { get; private set; } = Environment.CurrentDirectory;

		public bool UseNtfsAlternateStreams { get; private set; }

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			informationModule = modules.OfType<IAVD3InformationModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();

			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterCommandlineArgs += CreateCommandlineArguments;

		}

		public void Process(string[] paths) {
			//if(UsedBlockConsumerNames.Count == 0) {
			//	Console.WriteLine("No Blockconsumer chosen: Nothing to do");
			//	return;
			//}

			var bcs = new BlockConsumerSelector(processingModule.BlockConsumerFactories);
			bcs.Filter += BlockConsumerFilter;

			var bp = new BlockPool(BlockCount, BlockLength);

			var scf = new StreamConsumerFactory(bcs, bp);
			var sp = new StreamFromPathsProvider(GlobalConcurrentCount,
				PathPartitions, paths, true,
				path => {
					if(FileExtensions.Extensions.Count == 0) return true;
					return !FileExtensions.Allow ^ FileExtensions.Extensions.Any(fe => path.EndsWith(fe, StringComparison.InvariantCultureIgnoreCase));
				},
				ex => Console.Error.WriteLine("Filediscovery: " + ex.Message)
			);

			//sp = new NullStreamProvider();

			var streamConsumerCollection = new StreamConsumerCollection(scf, sp);

			var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));

			cl = new AVD3CL(bytesReadProgress.GetProgress);
			cl.TotalFiles = sp.TotalFileCount;
			cl.TotalBytes = sp.TotalBytes;

			cl.Display();

			streamConsumerCollection.ConsumingStream += ConsumingStream;

			Console.CursorVisible = false;
			try {
				streamConsumerCollection.ConsumeStreams(CancellationToken.None, bytesReadProgress);

			} catch(Exception ex) {
				Console.Error.WriteLine(ex);

			} finally {
				cl.Stop();
				Console.CursorVisible = true;
			}
		}

		private void BlockConsumerFilter(object sender, BlockConsumerSelectorEventArgs e) {
			e.Select = UsedBlockConsumerNames.Any(x => e.Name.Equals(x, StringComparison.OrdinalIgnoreCase));
		}

		private async void ConsumingStream(object sender, ConsumingStreamEventArgs e) {
			e.OnException += (s, args) => {
				args.IsHandled = true;
				args.Retry = args.RetryCount < 2;
			};

			var blockConsumers = await e.FinishedProcessing;

			var filePath = (string)e.Tag;
			var fileName = Path.GetFileName(filePath);
			cl.Writeline(fileName.Substring(0, Math.Min(fileName.Length, Console.WindowWidth - 1)));
			foreach(var bc in blockConsumers.OfType<HashCalculator>()) {
				cl.Writeline(bc.Name + " => " + BitConverter.ToString(bc.HashAlgorithm.Hash).Replace("-", ""));
			}
			cl.Writeline("");

			var infoSetup = new InfoProviderSetup(filePath, blockConsumers);
			var infoProviders = informationModule.InfoProviderFactories.Select(x => x.Create(infoSetup));

			var reportsFactories = reportingModule.ReportFactories.Where(x => UsedReportNames.Any(y => x.Name.Equals(y, StringComparison.OrdinalIgnoreCase))).ToArray();

			if(reportsFactories.Length != 0) {
				var fileMetaInfo = new FileMetaInfo(new FileInfo(filePath), infoProviders);
				var reports = reportsFactories.Select(x => x.Create(fileMetaInfo));

				foreach(var report in reports) {
					cl.Writeline(report.ReportToString() + "\n");
					report.SaveToFile(Path.Combine(ReportDirectory, fileName + "." + report.FileExtension));
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


		}

		private IEnumerable<ArgGroup> CreateCommandlineArguments() {
			var availableBlockConsumerNames = processingModule.BlockConsumerFactories.Select(x => x.Name).ToArray();
			var availableReportNames = reportingModule.ReportFactories.Select(x => x.Name).ToArray();

			yield return new ArgGroup("Processing",
				"",
				ArgStructure.Create(
					arg => {
						var raw = arg.Split(':').Select(ldArg => int.Parse(ldArg));
						return new { BlockSize = raw.ElementAt(0), BlockCount = raw.ElementAt(1) };
					},
					args => {
						BlockCount = args.BlockCount;
						BlockLength = args.BlockSize << 20;
					},
					"--BSize=<blocksize in kb>:<block count>",
					"Circular buffer size for hashing",
					"BlockSize", "BSize"
				),
				ArgStructure.Create(
					arg => {
						var raw = arg.Split(new char[] { ':' }, 2);
						return new {
							MaxCount = int.Parse(raw[0]),
							PerPath =
							  from item in (raw.Length > 1 ? raw[1].Split(';') : new string[0])
							  let parts = item.Split(',')
							  select new { Path = parts[0], MaxCount = int.Parse(parts[1]) }
						};
					},
					arg => {
						GlobalConcurrentCount = arg.MaxCount;
						PathPartitions = Array.AsReadOnly(arg.PerPath.Select(x => new PathPartition(x.Path, x.MaxCount)).ToArray());
					},
					"--Concurrent=<max>[:<path1>,<max1>;<path2>,<max2>;...]",
					"Sets the maximal number of files which will be processed concurrently.\n" +
					"First param (max) sets a global limit. (path,max) pairs sets limits per path.",
					"Concurrent", "Conc"
				),
				ArgStructure.Create(
					arg => arg.Split(',').Select(a => a.Trim()),
					hashNames => UsedBlockConsumerNames = Array.AsReadOnly(hashNames.ToArray()),
					"--Consumers=<ConsumerName1>[,<ConsumerName2>,...]",
					"Select consumers to use (" + string.Join(", ", availableBlockConsumerNames) + ")",
					"Consumers", "Cons"
				)
			);


			yield return new ArgGroup("Reporting",
				"",
				ArgStructure.Create(
					arg => arg.Split(',').Select(a => a.Trim()),
					reportNames => UsedReportNames = Array.AsReadOnly(reportNames.ToArray()),
					"--Reports=<ReportName1>[,<ReportName2>,...]",
					string.Join(", ", availableReportNames),
					"Reports"
				),
				ArgStructure.Create(
					arg => ReportDirectory = arg,
					"--ReportDirectory=<DirectoryPath>",
					"",
					"ReportDirectory, RDir"
				)
			);

			yield return new ArgGroup("FileDiscovery",
				"",
				ArgStructure.Create(
					arg => { },
					"--Recursive",
					"",
					"Recursive"
				),
				ArgStructure.Create(
					arg => { },
					"--WithExtensions",
					"",
					"WithExtensions=[-]<Extension1>[,<Extension2>,...]"
				)
			);

			yield return new ArgGroup("Display",
				"",
				ArgStructure.Create(
					arg => { },
					"--HideBuffers",
					"",
					"HideBuffers"
				),
				ArgStructure.Create(
					arg => { },
					"--HideFileProgress",
					"",
					"HideFileProgress"
				),
				ArgStructure.Create(
					arg => { },
					"--HideTotalProgress",
					"",
					"HideTotalProgress"
				),
				ArgStructure.Create(
					arg => { },
					"--HideUI",
					"",
					"HideUI"
				),
				ArgStructure.Create(
					arg => { },
					"--PrintHashes",
					"",
					"PrintHashes"
				),
				ArgStructure.Create(
					arg => { },
					"--PrintReports",
					"",
					"PrintReports"
				)
			);

			//bool useNtfsAlternateStreams = false;
			//yield return new ArgGroup("Internal",
			//	"",
			//	() => {
			//		UseNtfsAlternateStreams = useNtfsAlternateStreams;
			//	},
			//	ArgStructure.Create(
			//		arg => useNtfsAlternateStreams = true,
			//		"--UseNtfsAlternateStreams",
			//		"Store Hashes in Ntfs Alternate Streams to avoid unecessary rehashing",
			//		"UseNtfsAlternateStreams"
			//	)
			//);
		}
	}
}
