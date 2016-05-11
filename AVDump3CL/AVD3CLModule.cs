using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3CL {
	public class AVD3CLModule : IAVD3Module {
		private IAVD3ProcessingModule processingModule;
		private IAVD3ReportingModule reportingModule;


		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			var settingsgModule = modules.OfType<IAVD3SettingsModule>().Single();
			settingsgModule.RegisterCommandlineArgs += CreateCommandlineArguments;

			processingModule = modules.OfType<IAVD3ProcessingModule>().Single();
			reportingModule = modules.OfType<IAVD3ReportingModule>().Single();
		}

		public void Process() {

		}

		private IEnumerable<ArgGroup> CreateCommandlineArguments() {
			var blockCount = 16;
			var blockLength = 16 << 20;
			var globalConcurrentCount = 1;
			var partitions = Enumerable.Empty<PathPartition>();
			string[] usedBlockConsumers = new string[0];

			yield return new ArgGroup("Processing",
				"",
				() => { },
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
					"Sets the maximal number of files which will be processed concurrently.\nFirst param (max) sets a global limit. (path,max) pairs sets limits per path.",
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
