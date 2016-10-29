using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.HashAlgorithms;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing {
	public interface IAVD3ProcessingModule : IAVD3Module {
		IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }
		string GetBlockConsumerDescription(string name);
	}
	public class AVD3ProcessingModule : IAVD3ProcessingModule {
		[Flags]
		private enum CPUInstructions : long {
			MMX = 1 << 0,
			ISSE = 1 << 1,
			SSE2 = 1 << 2,
			SSSE3 = 1 << 3,
			SSE4 = 1 << 4,
			SHA = 1 << 5,
			RDRAND = 1 << 6,
			RDSEED = 1 << 7,
			PadlockRNG = 1 << 8,
			PadlockACE = 1 << 9,
			PadlockACE2 = 1 << 10,
			PadlockPHE = 1 << 11,
			PadlockPMM = 1 << 12
		}

		[DllImport("AVDump3NativeLib.dll")]
		private static extern CPUInstructions RetrieveCPUInstructions();


		private List<IBlockConsumerFactory> blockConsumerFactories;
		private ResourceManager resourceManager = Lang.ResourceManager;

		public IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		public AVD3ProcessingModule() {
			var nullHash = new BlockConsumerFactory("NULL", r => new HashCalculator("NULL", r, new NullHashAlgorithm()));
			var sha1Hash = new BlockConsumerFactory("SHA1", r => new HashCalculator("SHA1", r, SHA1.Create()));
			var sha256Hash = new BlockConsumerFactory("SHA256", r => new HashCalculator("SHA256", r, SHA256.Create()));
			var sha384Hash = new BlockConsumerFactory("SHA384", r => new HashCalculator("SHA384", r, SHA384.Create()));
			var sha512Hash = new BlockConsumerFactory("SHA512", r => new HashCalculator("SHA512", r, SHA512.Create()));
			var md4Hash = new BlockConsumerFactory("MD4", r => new HashCalculator("MD4", r, new Md4HashAlgorithm()));
			var md5Hash = new BlockConsumerFactory("MD5", r => new HashCalculator("MD5", r, MD5.Create()));
			var ed2kHash = new BlockConsumerFactory("ED2K", r => new HashCalculator("ED2K", r, new Ed2kHashAlgorithm()));
			var tigerHash = new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new TigerHashAlgorithm()));
			var tthHash = new BlockConsumerFactory("TTH", r => new HashCalculator("TTH", r, new TigerTreeHashAlgorithm(Environment.ProcessorCount)));
			var crc32Hash = new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32HashAlgorithm()));
			var mkvConsumer = new BlockConsumerFactory("MKV", r => new MatroskaParser("MKV", r));
			var oggConsumer = new BlockConsumerFactory("OGG", r => new OggParser("OGG", r));

			try {
				var cpuInstructions = RetrieveCPUInstructions();
				if(cpuInstructions.HasFlag(CPUInstructions.SSE2)) {
					tigerHash = new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new TigerNativeHashAlgorithm()));
					crc32Hash = new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32NativeHashAlgorithm()));
				}
				if(cpuInstructions.HasFlag(CPUInstructions.SSE4)) {
					blockConsumerFactories.Add(new BlockConsumerFactory("CRC32C", r => new HashCalculator("CRC32C", r, new Crc32CIntelHashAlgorithm())));
				}
			} catch(Exception ex) {
				//TODO Log
			}

			blockConsumerFactories = new List<IBlockConsumerFactory> {
				nullHash, sha1Hash, sha256Hash, sha384Hash, sha512Hash,
				md4Hash, md5Hash, ed2kHash, tigerHash, tthHash, crc32Hash,
				mkvConsumer, oggConsumer
			};
			blockConsumerFactories.Sort((a, b) => a.Name.CompareTo(b.Name));
			BlockConsumerFactories = blockConsumerFactories.AsReadOnly();
		}

		public string GetBlockConsumerDescription(string name) {
			var description = resourceManager.GetString(name.Replace("-", "") + "ConsumerDescription");
			return !string.IsNullOrEmpty(description) ? description : "<NoDescriptionGiven>";
		}


		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }
		public void BeforeConfiguration(ModuleConfigurationEventArgs args) { }
		public void AfterConfiguration(ModuleConfigurationEventArgs args) { }
	}
}
