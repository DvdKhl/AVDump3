using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class TigerTreeHashAlgorithm : IAVDHashAlgorithm {
		//Assumptions: We will never hash more than 2^64 bytes.
		// => Max Tree Depth: TD = log_2(2^64 / 1024) + 1 = 55
		// We only ever need to store two tiger hashes per level.
		// => Internal Node Data Length: Length(ND) = TD * 24 * 2 = 2640

		private int leafCount;
		private long nodeCount; //Used as a map to check if a level already contains a hash
		private readonly byte[] nodes = new byte[2640];
		private readonly byte[] leaves = new byte[(16 * 1024 * 1024) / 1024 * 24]; //Space for leaves calculated by 16MiB worth of data
		private readonly BlockHasher[] blockHashers;
		private readonly AutoResetEvent[] blockHashersSync;

		public const int BLOCKSIZE = 1024;
		public int BlockSize => BLOCKSIZE * 2; //Due to optimizations the each passed data block needs to be twice the size of BLOCKSIZE (See Compress)

		public TigerTreeHashAlgorithm(int threadCount) {
			blockHashers = Enumerable.Range(0, threadCount).Select(x => new BlockHasher(leaves, x, threadCount)).ToArray();
			blockHashersSync = blockHashers.Select(x => x.WorkDoneSync).ToArray();
		}

		public void Initialize() {
			leafCount = 0;
			nodeCount = 0;
			Array.Clear(nodes, 0, nodes.Length);
			Array.Clear(leaves, 0, leaves.Length);
		}

		public ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data) {
			foreach (var blockHasher in blockHashers) {
				blockHasher.Finish();
			}

			if (data.Length >= 2048 || leafCount != 0) throw new Exception();

			if (data.Length != 0) {
				fixed (byte* leavesPtr = leaves)
				fixed (byte* dataPtr = data) {
					var buffer = TigerNativeHashAlgorithm.TTHCreateBlock();
					if (data.Length > 1024) {
						TigerNativeHashAlgorithm.TTHBlockHash(dataPtr, buffer, leavesPtr);
						leafCount++;
					}

					TigerNativeHashAlgorithm.TTHPartialBlockHash(
						dataPtr + leafCount * 1024,
						data.Length - leafCount * 1024,
						buffer, leavesPtr + leafCount * 24
					);
					leafCount++;
					AVDNativeHashAlgorithm.FreeHashObject((IntPtr)buffer);

					if (leafCount > 1) Compress();
				}
			}


			int nodesCopied = 0;
			Span<byte> finalHashesSpan = new byte[24 * 3];
			fixed (byte* finalHashesPtr = finalHashesSpan) {
				Span<byte> leavesSpan = leaves;
				Span<byte> nodeSpan = nodes;

				for (int i = 0; i <= 55; i++) {
					//for (int i = 55 - 1; i >= 0; i--) {
					if ((nodeCount & (1L << i)) == 0) continue;

					if (leafCount < 2) {
						leavesSpan.Slice(0, 24).CopyTo(leavesSpan.Slice(24));
						nodeSpan.Slice(i * 48, 24).CopyTo(leavesSpan);
						leafCount++;

					} else {
						nodeSpan.Slice(i * 48, 24).CopyTo(nodeSpan.Slice((leafCount + nodesCopied - 2) * 48, 24));
						nodesCopied++;
					}
				}
				nodeCount = ~(-1 << nodesCopied);
				Compress();

				return nodeSpan.Slice(nodesCopied * 48, 24);
			}
		}



		public int TransformFullBlocks(ReadOnlySpan<byte> data) {
			data = data.Slice(0, Math.Min(16 << 20, data.Length) & ~2047);

			fixed (byte* dataPtr = data) {
				foreach (var blockHasher in blockHashers) blockHasher.ProcessData(dataPtr, data.Length);
				WaitHandle.WaitAll(blockHashersSync);
			}
			leafCount += data.Length >> 10;

			Compress();

			return data.Length;
		}

		private byte* compressBuffer = TigerNativeHashAlgorithm.TTHCreateNode();
		private void Compress() {
			var leafPairsProcessed = 0;

			//Since we assume Only the last datablock may have an odd number of leaves
			fixed (byte* leavesPtr = leaves)
			fixed (byte* nodesPtr = nodes) {
				while (leafCount > 1) {
					var levelIsEmpty = (nodeCount & 1) == 0;
					TigerNativeHashAlgorithm.TTHNodeHash(leavesPtr + leafPairsProcessed * 48, compressBuffer, nodesPtr + (levelIsEmpty ? 0 : 24));
					leafPairsProcessed++;
					leafCount -= 2;

					var currentLevel = 0;
					while (!levelIsEmpty) {
						levelIsEmpty = (nodeCount & (2 << currentLevel)) == 0;
						TigerNativeHashAlgorithm.TTHNodeHash(nodesPtr + currentLevel * 48, compressBuffer, nodesPtr + (currentLevel + 1) * 48 + (levelIsEmpty ? 0 : 24));
						currentLevel++;
					}
					nodeCount++;
				}
			}
		}

		public void Dispose() {
			AVDNativeHashAlgorithm.FreeHashObject((IntPtr)compressBuffer);
			compressBuffer = (byte*)0;
		}

		private class BlockHasher {
			private readonly Thread hashThread;
			private bool threadJoin;

			private readonly byte[] leaves;
			private byte* dataPtr;

			private int offsetLeaf;
			private int offsetData;
			private int lengthData;

			private readonly int index;
			private readonly int nextBlockOffsetLeaf;
			private readonly int nextBlockOffsetData;
			private readonly AutoResetEvent doWorkSync;

			public AutoResetEvent WorkDoneSync { get; }

			public BlockHasher(byte[] leaves, int index, int count) {
				this.leaves = leaves;
				this.index = index;

				nextBlockOffsetData = count * BLOCKSIZE;
				nextBlockOffsetLeaf = count * 24;

				doWorkSync = new AutoResetEvent(false);
				WorkDoneSync = new AutoResetEvent(false);

				hashThread = new Thread(DoWork);
				hashThread.Start();
			}

			public void ProcessData(byte* dataPtr, int length) {
				this.dataPtr = dataPtr;

				offsetLeaf = index * 24;
				offsetData = index * BLOCKSIZE;
				lengthData = length - index * BLOCKSIZE;

				doWorkSync.Set();
			}

			public void Finish() {
				threadJoin = true;
				doWorkSync.Set();
				hashThread.Join();
			}

			void DoWork() {
				var buffer = TigerNativeHashAlgorithm.TTHCreateBlock();

				doWorkSync.WaitOne();
				while (!threadJoin) {
					fixed (byte* leavesPtr = leaves) {
						while (lengthData >= BLOCKSIZE) {
							TigerNativeHashAlgorithm.TTHBlockHash(dataPtr + offsetData, buffer, leavesPtr + offsetLeaf);

							offsetLeaf += nextBlockOffsetLeaf;
							offsetData += nextBlockOffsetData;
							lengthData -= nextBlockOffsetData;
						}
					}
					WorkDoneSync.Set();
					doWorkSync.WaitOne();
				}
				AVDNativeHashAlgorithm.FreeHashObject((IntPtr)buffer);
			}

		}

	}
}