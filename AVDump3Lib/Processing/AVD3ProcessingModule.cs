using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.MP4;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.HashAlgorithms;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
	[Flags]
	public enum CPUInstructions : long {
		MMX = 1 << 0,
		x64 = 1 << 1,
		ABM = 1 << 2,
		RDRAND = 1 << 3,
		BMI1 = 1 << 4,
		BMI2 = 1 << 5,
		ADX = 1 << 6,
		PREFETCHWT1 = 1 << 7,
		SSE = 1 << 8,
		SSE2 = 1 << 9,
		SSE3 = 1 << 10,
		SSSE3 = 1 << 11,
		SSE41 = 1 << 12,
		SSE42 = 1 << 13,
		SSE4a = 1 << 14,
		AES = 1 << 15,
		SHA = 1 << 16,
		AVX = 1 << 17,
		XOP = 1 << 18,
		FMA3 = 1 << 19,
		FMA4 = 1 << 20,
		AVX2 = 1 << 21,
		AVX512F = 1 << 22,
		AVX512CD = 1 << 23,
		AVX512PF = 1 << 24,
		AVX512ER = 1 << 25,
		AVX512VL = 1 << 26,
		AVX512BW = 1 << 27,
		AVX512DQ = 1 << 28,
		AVX512IFMA = 1 << 29,
		AVX512VBMI = 1 << 30
	}

	public interface IAVD3ProcessingModule : IAVD3Module {
		CPUInstructions AvailableSIMD { get; }

		IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter;
		IStreamConsumerCollection CreateStreamConsumerCollection(IStreamProvider streamProvider, int bufferLength, int minProducerReadLength, int maxProducerReadLength);
	}
	public class AVD3ProcessingModule : IAVD3ProcessingModule {

		[DllImport("AVDump3NativeLib")]
		private static extern CPUInstructions RetrieveCPUInstructions();

		public CPUInstructions AvailableSIMD { get; } = RetrieveCPUInstructions();

		public event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter;

		private readonly List<IBlockConsumerFactory> blockConsumerFactories;

		public IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		public AVD3ProcessingModule() {
			var factories = new Dictionary<string, IBlockConsumerFactory>();
			void addOrReplace(IBlockConsumerFactory factory) => factories[factory.Name] = factory;

			addOrReplace(new BlockConsumerFactory("NULL", (n, r) => new HashCalculator(n, r, new NullHashAlgorithm(4 << 20))));
			addOrReplace(new BlockConsumerFactory("MD5", (n, r) => new HashCalculator(n, r, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.MD5, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA1", (n, r) => new HashCalculator(n, r, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA1, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA256", (n, r) => new HashCalculator(n, r, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA256, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA384", (n, r) => new HashCalculator(n, r, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA384, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA512", (n, r) => new HashCalculator(n, r, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA512, 1024))));
			addOrReplace(new BlockConsumerFactory("MD4", (n, r) => new HashCalculator(n, r, new Md4HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("ED2K", (n, r) => new HashCalculator(n, r, new Ed2kHashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("CRC32", (n, r) => new HashCalculator(n, r, new Crc32HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("MKV", (n, r) => new MatroskaParser(n, r)));
			addOrReplace(new BlockConsumerFactory("OGG", (n, r) => new OggParser(n, r)));
			addOrReplace(new BlockConsumerFactory("MP4", (n, r) => new MP4Parser(n, r)));
			//addOrReplace(new BlockConsumerFactory("COPY", (n, r) =>new CopyToFileBlockConsumer("COPY", r, @"D:\Projects\Visual Studio 2017\Projects\New\AVDump3\bla.bin")));

			try {
				var cpuInstructions = RetrieveCPUInstructions();

				addOrReplace(new BlockConsumerFactory("ED2K", (n, r) => new HashCalculator(n, r, new Ed2kNativeHashAlgorithm())));
				addOrReplace(new BlockConsumerFactory("MD4", (n, r) => new HashCalculator(n, r, new Md4NativeHashAlgorithm())));
				addOrReplace(new BlockConsumerFactory("CRC32", (n, r) => new HashCalculator(n, r, new Crc32NativeHashAlgorithm())));
				if(cpuInstructions.HasFlag(CPUInstructions.SSE2)) {
					addOrReplace(new BlockConsumerFactory("TIGER", (n, r) => new HashCalculator(n, r, new TigerNativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("TTH", (n, r) => new HashCalculator(n, r, new TigerTreeHashAlgorithm(Math.Min(4, Environment.ProcessorCount)))));
					addOrReplace(new BlockConsumerFactory("CRC32", (n, r) => new HashCalculator(n, r, new Crc32NativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("SHA3", (n, r) => new HashCalculator(n, r, new SHA3NativeHashAlgorithm())));
				}
				if(cpuInstructions.HasFlag(CPUInstructions.SSE42)) {
					addOrReplace(new BlockConsumerFactory("CRC32C", (n, r) => new HashCalculator(n, r, new Crc32CIntelHashAlgorithm())));
				}
			} catch(Exception) {
				//TODO Log
			}

			blockConsumerFactories = factories.Values.ToList();
			blockConsumerFactories.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
			BlockConsumerFactories = blockConsumerFactories.AsReadOnly();
		}

		public IStreamConsumerCollection CreateStreamConsumerCollection(IStreamProvider streamProvider, int bufferLength, int minProducerReadLength, int maxProducerReadLength) {
			var bcs = new BlockConsumerSelector(BlockConsumerFactories);
			var bp = new MirroredBufferPool(bufferLength);
			var scf = new StreamConsumerFactory(bcs, bp, minProducerReadLength, maxProducerReadLength);
			var scc = new StreamConsumerCollection(scf, streamProvider);

			bcs.Filter += (s, e) => {
				var filterEvent = new BlockConsumerFilterEventArgs(scc, e.Name);
				BlockConsumerFilter?.Invoke(this, filterEvent);

				e.Select = filterEvent.Accepted;
			};


			return scc;
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }
		public ModuleInitResult Initialized() => new ModuleInitResult(false);
	}
}
