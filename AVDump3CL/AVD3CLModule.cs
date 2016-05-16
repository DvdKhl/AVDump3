using AVDump2Lib.InfoProvider.Tools;
using AVDump3Lib.BlockBuffers;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3CL {
	public class AVD3CLModule : IAVD3Module {
		private IAVD3ProcessingModule processingModule;
		private IAVD3ReportingModule reportingModule;


		public int BlockCount { get; private set; }
		public int BlockLength { get; private set; }
		public int GlobalConcurrentCount { get; private set; }
		public IReadOnlyCollection<PathPartition> PathPartitions { get; private set; }
		public IReadOnlyCollection<string> UsedBlockConsumerNames { get; private set; }

		public bool UseNtfsAlternateStreams { get; private set; }

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterCommandlineArgs += CreateCommandlineArguments;

			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();
		}

		public void Process(string[] paths) {
			var bcs = new BlockConsumerSelector(processingModule.BlockConsumerFactories);
			bcs.Filter += BlockConsumerFilter;

			var bp = new BlockPool(BlockCount, BlockLength);

			var scf = new StreamConsumerFactory(bcs, bp);
			var sp = new StreamFromPathsProvider(GlobalConcurrentCount,
				PathPartitions, paths, true,
				path => path.EndsWith("mkv"), ex => { }
			);

			var streamConsumerCollection = new StreamConsumerCollection(scf, sp);

			var bytesReadProgress = new BytesReadProgress(processingModule.BlockConsumerFactories.Select(x => x.Name));

			var cl = new AVD3CL(bytesReadProgress.GetProgress);
			cl.TotalFiles = sp.TotalFileCount;
			cl.TotalBytes = sp.TotalBytes;

			Task.Run(() => cl.Display());

			streamConsumerCollection.ConsumingStream += ConsumingStream;
			streamConsumerCollection.ConsumeStreams(CancellationToken.None, bytesReadProgress);
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



			//var infoProvider = new CompositeMediaProvider(
			//	new HashProvider(blockConsumers.OfType<HashCalculator>().Select(b =>
			//		new HashProvider.HashResult(b.HashAlgorithmType, b.HashAlgorithm.Hash)
			//	)),
			//	new MediaInfoLibProvider((string)e.Tag),
			//	new MatroskaProvider(blockConsumers.OfType<MatroskaParser>().First().Info)
			//);

			//if(!Directory.Exists("Dumps")) Directory.CreateDirectory("Dumps");
			//GenerateAVDump3Report(infoProvider).Save("Dumps\\" + Path.GetFileName((string)e.Tag) + ".xml");
		}
		public static XElement GenerateAVDump3Report(MediaProvider provider) {
			var root = new XElement("File");
			GenerateAVDump3ReportSub(root, provider);
			return root;
		}
		public static void GenerateAVDump3ReportSub(XElement elem, MetaInfoContainer container) {
			foreach(var item in container.Items) {
				if(item.Value == null) continue;

				var subElem = new XElement(item.Type.Key);
				if(item.Value is MetaInfoContainer) {
					GenerateAVDump3ReportSub(subElem, (MetaInfoContainer)item.Value);

				} else if(item.Provider is HashProvider) {
					var bVal = (byte[])item.Value;
					subElem.Value = BitConverter.ToString(bVal).Replace("-", "");

				} else if(item.Value is byte[]) {
					var bVal = (byte[])item.Value;
					subElem.Value = bVal.Length <= 16 ? BitConverter.ToString(bVal) : "Byte[" + bVal.Length + "]";

				} else if(item.Value is IFormattable) subElem.Value = ((IFormattable)item.Value).ToString(null, CultureInfo.InvariantCulture);
				else subElem.Value = item.Value.ToString();

				subElem.Add(new XAttribute("p", item.Provider.Name));

				if(item.Type.Unit != null) {
					subElem.Add(new XAttribute("u", item.Type.Unit));
				}


				elem.Add(subElem);
			}
		}

		private IEnumerable<ArgGroup> CreateCommandlineArguments() {
			var blockCount = 16;
			var blockLength = 16 << 20;
			var globalConcurrentCount = 1;
			var partitions = Enumerable.Empty<PathPartition>();
			string[] usedBlockConsumers = new string[0];

			yield return new ArgGroup("Processing",
				"",
				() => {
					BlockCount = blockCount;
					BlockLength = blockLength;
					GlobalConcurrentCount = globalConcurrentCount;
					PathPartitions = Array.AsReadOnly(partitions.ToArray());
					UsedBlockConsumerNames = Array.AsReadOnly(usedBlockConsumers);
				},
				ArgStructure.Create(
					arg => {
						var raw = arg.Split(':').Select(ldArg => int.Parse(ldArg));
						return new { BlockSize = raw.ElementAt(0), BlockCount = raw.ElementAt(1) };
					},
					args => {
						blockCount = args.BlockCount;
						blockLength = args.BlockSize << 20;
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
						globalConcurrentCount = arg.MaxCount;
						partitions = arg.PerPath.Select(x => new PathPartition(x.Path, x.MaxCount));
					},
					"--Concurrent=<max>[:<path1>,<max1>;<path2>,<max2>;...]",
					"Sets the maximal number of files which will be processed concurrently.\n" +
					"First param (max) sets a global limit. (path,max) pairs sets limits per path.",
					"Concurrent", "Conc"
				),
				ArgStructure.Create(
					arg => arg.Split(',').Select(a => a.Trim()),
					hashNames => {
						usedBlockConsumers = hashNames.ToArray();
					},
					"--Consumers=<ConsumerName1>,<ConsumerName2,...>",
					"Select consumers to use (CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER, MKV)",
					"Consumers"
				)
			);


			bool useNtfsAlternateStreams = false;
			yield return new ArgGroup("Internal",
				"",
				() => {
					UseNtfsAlternateStreams = useNtfsAlternateStreams;
				},
				ArgStructure.Create(
					arg => useNtfsAlternateStreams = true,
					"--UseNtfsAlternateStreams",
					"Store Hashes in Ntfs Alternate Streams to avoid unecessary rehashing",
					"UseNtfsAlternateStreams"
				)
			);
		}
	}
}
