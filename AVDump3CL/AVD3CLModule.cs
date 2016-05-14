using AVDump3Lib.BlockBuffers;
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			var bcf = new BlockConsumerSelector(processingModule.BlockConsumerFactories);
			var bp = new BlockPool(BlockCount, BlockLength);

			var scf = new StreamConsumerFactory(bcf, bp);
			var sp = new StreamFromPathsProvider(GlobalConcurrentCount,
				PathPartitions, paths, true,
				path => path.EndsWith("mkv"), ex => { }
			);

			var streamConsumerCollection = new StreamConsumerCollection(scf, sp);

			streamConsumerCollection.ConsumeStreams();

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
							MaxCount = int.Parse(raw.ElementAt(0)),
							PerPath =
							  from item in raw.ElementAt(1).Split(';')
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
