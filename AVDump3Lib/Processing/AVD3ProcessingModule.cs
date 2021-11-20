using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.MP4;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.HashAlgorithms;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing {


	public class FilePathFilterEventArgs : EventArgs {
		public string FilePath { get; private set; }
		public bool Accepted { get; private set; }
		public void Decline() => Accepted = false;

		internal void Reset(string filePath) { FilePath = filePath; Accepted = true; }
	}
	public class AVD3ProcessingModule : IAVD3ProcessingModule {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern CPUInstructions RetrieveCPUInstructions();
		}



		public CPUInstructions AvailableSIMD { get; } = NativeMethods.RetrieveCPUInstructions();

		public event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter = delegate { };
		public event EventHandler<FilePathFilterEventArgs> FilePathFilter = delegate { };

		public ImmutableArray<IBlockConsumerFactory> BlockConsumerFactories { get; private set; }

		public void RegisterDefaultBlockConsumers(IDictionary<string, ImmutableArray<string>> arguments) {
			var factories = new Dictionary<string, IBlockConsumerFactory>();
			void addOrReplace(IBlockConsumerFactory factory) => factories[factory.Name] = factory;
			string? getArgumentAt(BlockConsumerSetup s, int index, string? defVal) {
				if(arguments == null) return defVal;
				return arguments.TryGetValue(s.Name, out var args) && index < args.Length ? args[index] ?? defVal : defVal;
			}

			addOrReplace(new BlockConsumerFactory("NULL", s => new HashCalculator(s.Name, s.Reader, new NullHashAlgorithm(getArgumentAt(s, 0, "4").ToInvInt32() << 20))));
			addOrReplace(new BlockConsumerFactory("CPY", s => new CopyToFileBlockConsumer(s.Name, s.Reader, Path.Combine(getArgumentAt(s, 0, null) ?? throw new Exception(), Path.GetFileName((string)s.Tag)))));
			addOrReplace(new BlockConsumerFactory("MD5", s => new HashCalculator(s.Name, s.Reader, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.MD5, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA1", s => new HashCalculator(s.Name, s.Reader, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA1, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA2-256", s => new HashCalculator(s.Name, s.Reader, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA256, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA2-384", s => new HashCalculator(s.Name, s.Reader, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA384, 1024))));
			addOrReplace(new BlockConsumerFactory("SHA2-512", s => new HashCalculator(s.Name, s.Reader, new AVDHashAlgorithmIncrmentalHashAdapter(HashAlgorithmName.SHA512, 1024))));
			addOrReplace(new BlockConsumerFactory("MD4", s => new HashCalculator(s.Name, s.Reader, new Md4HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("ED2K", s => new HashCalculator(s.Name, s.Reader, new Ed2kHashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("CRC32", s => new HashCalculator(s.Name, s.Reader, new Crc32HashAlgorithm())));
			addOrReplace(new BlockConsumerFactory("MKV", s => new MatroskaParser(s.Name, s.Reader)));
			addOrReplace(new BlockConsumerFactory("OGG", s => new OggParser(s.Name, s.Reader)));
			addOrReplace(new BlockConsumerFactory("MP4", s => new MP4Parser(s.Name, s.Reader)));


			try {
				addOrReplace(new BlockConsumerFactory("ED2K", s => new HashCalculator(s.Name, s.Reader, new Ed2kNativeHashAlgorithm())));
				addOrReplace(new BlockConsumerFactory("MD4", s => new HashCalculator(s.Name, s.Reader, new Md4NativeHashAlgorithm())));
				addOrReplace(new BlockConsumerFactory("CRC32", s => new HashCalculator(s.Name, s.Reader, new Crc32NativeHashAlgorithm())));
				addOrReplace(new BlockConsumerFactory("SHA3-224", s => new HashCalculator(s.Name, s.Reader, new SHA3NativeHashAlgorithm(224))));
				addOrReplace(new BlockConsumerFactory("SHA3-256", s => new HashCalculator(s.Name, s.Reader, new SHA3NativeHashAlgorithm(256))));
				addOrReplace(new BlockConsumerFactory("SHA3-384", s => new HashCalculator(s.Name, s.Reader, new SHA3NativeHashAlgorithm(384))));
				addOrReplace(new BlockConsumerFactory("SHA3-512", s => new HashCalculator(s.Name, s.Reader, new SHA3NativeHashAlgorithm(512))));
				addOrReplace(new BlockConsumerFactory("KECCAK-224", s => new HashCalculator(s.Name, s.Reader, new KeccakNativeHashAlgorithm(224))));
				addOrReplace(new BlockConsumerFactory("KECCAK-256", s => new HashCalculator(s.Name, s.Reader, new KeccakNativeHashAlgorithm(256))));
				addOrReplace(new BlockConsumerFactory("KECCAK-384", s => new HashCalculator(s.Name, s.Reader, new KeccakNativeHashAlgorithm(384))));
				addOrReplace(new BlockConsumerFactory("KECCAK-512", s => new HashCalculator(s.Name, s.Reader, new KeccakNativeHashAlgorithm(512))));

				if(AvailableSIMD.HasFlag(CPUInstructions.SSE2)) {
					addOrReplace(new BlockConsumerFactory("TIGER", s => new HashCalculator(s.Name, s.Reader, new TigerNativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("TTH", s => new HashCalculator(s.Name, s.Reader, new TigerTreeHashAlgorithm(getArgumentAt(s, 0, Math.Min(4, Environment.ProcessorCount).ToInvString()).ToInvInt32()))));
					addOrReplace(new BlockConsumerFactory("CRC32", s => new HashCalculator(s.Name, s.Reader, new Crc32NativeHashAlgorithm())));
				}
				if(AvailableSIMD.HasFlag(CPUInstructions.SSE42)) {
					addOrReplace(new BlockConsumerFactory("CRC32C", s => new HashCalculator(s.Name, s.Reader, new Crc32CIntelHashAlgorithm())));
				}
				if(AvailableSIMD.HasFlag(CPUInstructions.SHA) && false) { //Broken (Produces wrong hashes)
					addOrReplace(new BlockConsumerFactory("SHA1", s => new HashCalculator(s.Name, s.Reader, new SHA1NativeHashAlgorithm())));
					addOrReplace(new BlockConsumerFactory("SHA2-256", s => new HashCalculator(s.Name, s.Reader, new SHA256NativeHashAlgorithm())));
				}


			} catch(Exception) {
				//TODO Log
			}

			var blockConsumerFactories = factories.Values.ToList();
			blockConsumerFactories.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
			BlockConsumerFactories = ImmutableArray.CreateRange(blockConsumerFactories);
		}


		public IStreamProvider CreateFileStreamProvider(string[] paths, bool recursive, PathPartitions pathPartitions, Action<string> onPathAccepted, Action<Exception> onError) {
			var filePathFilterEventArgs = new FilePathFilterEventArgs();

			//var acceptedFileCountCursorTop = Console.CursorTop++;
			var fileDiscoveryOn = DateTimeOffset.UtcNow;
			var spp = new StreamFromPathsProvider(pathPartitions);
			spp.AddFiles(paths, recursive,
				fileInfo => {
					filePathFilterEventArgs.Reset(fileInfo.FullName);
					FilePathFilter(this, filePathFilterEventArgs);
					if(filePathFilterEventArgs.Accepted) onPathAccepted?.Invoke(fileInfo.FullName);
					return filePathFilterEventArgs.Accepted;
				},
				onError
			);


			return spp;
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
		public ModuleInitResult Initialized() => new(false);
		public void Shutdown() { }
	}
}
