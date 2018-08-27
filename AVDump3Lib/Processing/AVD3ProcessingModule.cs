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
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Processing.BlockConsumers.MP4;

namespace AVDump3Lib.Processing {
	public class BlockConsumerFilterEventArgs : EventArgs {
		public IStreamConsumerCollection StreamConsumerCollection { get; }
		public string BlockConsumerName { get; }

		public bool Accepted { get; private set; }
		public void Accept() { Accepted = true; }

		public BlockConsumerFilterEventArgs(IStreamConsumerCollection streamConsumerCollection, string blockConsumerName) {
			StreamConsumerCollection = streamConsumerCollection;
			BlockConsumerName = blockConsumerName;
		}
	}

	public interface IAVD3ProcessingModule : IAVD3Module {
		IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter;
		IStreamConsumerCollection CreateStreamConsumerCollection(IStreamProvider streamProvider, int blockCount, int blockLength);
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

		public event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter;

		private List<IBlockConsumerFactory> blockConsumerFactories;

		public IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		public AVD3ProcessingModule() {
			var factories = new Dictionary<string, IBlockConsumerFactory>();
			Action<IBlockConsumerFactory> addOrReplace = factory => factories[factory.Name] = factory;

			addOrReplace(new BlockConsumerFactory("NULL", r => new HashCalculator("NULL", r, new NullHashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("SHA1", r => new HashCalculator("SHA1", r, SHA1.Create())));
			addOrReplace(new BlockConsumerFactory("SHA256", r => new HashCalculator("SHA256", r, SHA256.Create())));
			addOrReplace(new BlockConsumerFactory("SHA384", r => new HashCalculator("SHA384", r, SHA384.Create())));
			addOrReplace(new BlockConsumerFactory("SHA512", r => new HashCalculator("SHA512", r, SHA512.Create())));
			addOrReplace(new BlockConsumerFactory("MD4", r => new HashCalculator("MD4", r, new Md4HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("MD5", r => new HashCalculator("MD5", r, MD5.Create())));
			addOrReplace(new BlockConsumerFactory("ED2K", r => new HashCalculator("ED2K", r, new Ed2kHashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new TigerHashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("TTH", r => new HashCalculator("TTH", r, new TigerTreeHashAlgorithm(Environment.ProcessorCount))));
			addOrReplace(new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("MKV", r => new MatroskaParser("MKV", r)));
			addOrReplace(new BlockConsumerFactory("OGG", r => new OggParser("OGG", r)));
			addOrReplace(new BlockConsumerFactory("MP4", r => new MP4Parser("MP4", r)));

			try {
				var cpuInstructions = RetrieveCPUInstructions();
				if(cpuInstructions.HasFlag(CPUInstructions.SSE2)) {
					addOrReplace(new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new TigerNativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32NativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("SHA3", r => new HashCalculator("SHA3", r, new SHA3NativeHashAlgorithm())));
				}
				if(cpuInstructions.HasFlag(CPUInstructions.SSE4)) {
					addOrReplace(new BlockConsumerFactory("CRC32C", r => new HashCalculator("CRC32C", r, new Crc32CIntelHashAlgorithm())));
				}
			} catch(Exception) {
				//TODO Log
			}

			blockConsumerFactories = factories.Values.ToList();
			blockConsumerFactories.Sort((a, b) => a.Name.CompareTo(b.Name));
			BlockConsumerFactories = blockConsumerFactories.AsReadOnly();
		}


		private void OnBlockConsumerFilter(object sender, BlockConsumerSelectorEventArgs e, IStreamConsumerCollection scc) {
			var filterEvent = new BlockConsumerFilterEventArgs(scc, e.Name);
			BlockConsumerFilter?.Invoke(this, filterEvent);

			e.Select = filterEvent.Accepted;
		}

		public IStreamConsumerCollection CreateStreamConsumerCollection(IStreamProvider streamProvider, int blockCount, int blockLength) {
			var bcs = new BlockConsumerSelector(BlockConsumerFactories);
			var bp = new BlockPool(blockCount, blockLength);
			var scf = new StreamConsumerFactory(bcs, bp);
			var scc = new StreamConsumerCollection(scf, streamProvider);

			bcs.Filter += (s, e) => OnBlockConsumerFilter(s, e, scc);


			return scc;
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }
		public ModuleInitResult Initialized() => new ModuleInitResult(false);
	}
}
