using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class TigerTreeHashAlgorithm : IAVDHashAlgorithm {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern byte* TTHCreateBlock();
			[DllImport("AVDump3NativeLib")]
			internal static extern byte* TTHCreateNode();
			[DllImport("AVDump3NativeLib")]
			internal static extern void TTHNodeHash(byte* data, byte* buffer, byte* hash);
			[DllImport("AVDump3NativeLib")]
			internal static extern void TTHBlockHash(byte* data, byte* buffer, byte* hash);
			[DllImport("AVDump3NativeLib")]
			internal static extern void TTHPartialBlockHash(byte* data, int length, byte* buffer, byte* hash);
			[DllImport("AVDump3NativeLib")]
			internal static extern void FreeHashObject(IntPtr handle);
		}



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
		private readonly byte[] EmptyHash = new byte[] { 0x32, 0x93, 0xAC, 0x63, 0x0C, 0x13, 0xF0, 0x24, 0x5F, 0x92, 0xBB, 0xB1, 0x76, 0x6E, 0x16, 0x16, 0x7A, 0x4E, 0x58, 0x49, 0x2D, 0xDE, 0x73, 0xF3 };

		public const int BLOCKSIZE = 1024;
		public int BlockSize => BLOCKSIZE * 2; //Due to optimizations each passed data block needs to be twice the size of BLOCKSIZE (See Compress)

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

		public ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			foreach(var blockHasher in blockHashers) blockHasher.Finish();

			if(nodeCount == 0 && data.Length == 0) return EmptyHash;
			if(data.Length >= 2048 || leafCount != 0) throw new Exception("leafCount is not 0 or remaining data is larger than 2048 bytes");

			if(data.Length != 0) {
				fixed(byte* leavesPtr = leaves)
				fixed(byte* dataPtr = data) {
					var buffer = NativeMethods.TTHCreateBlock();
					if(data.Length > 1024) {
						NativeMethods.TTHBlockHash(dataPtr, buffer, leavesPtr);
						leafCount++;
					}

					NativeMethods.TTHPartialBlockHash(
						dataPtr + leafCount * 1024,
						data.Length - leafCount * 1024,
						buffer, leavesPtr + leafCount * 24
					);
					leafCount++;
					NativeMethods.FreeHashObject((IntPtr)buffer);

					//There needs to be at least one node
					if(leafCount > 1) {
						Compress();

					} else if(nodeCount == 0) {
						leafCount--;
						nodeCount++;
						((Span<byte>)leaves).Slice(0, 24).CopyTo(nodes);
					}
				}
			}

			if(nodeCount == 0) throw new Exception("nodeCount is 0");

			var nodesCopied = 0;
			Span<byte> finalHashesSpan = new byte[24 * 3];
			fixed(byte* finalHashesPtr = finalHashesSpan) {
				Span<byte> leavesSpan = leaves;
				Span<byte> nodeSpan = nodes;

				for(var i = 0; i <= 55; i++) {
					//for (int i = 55 - 1; i >= 0; i--) {
					if((nodeCount & (1L << i)) == 0) continue;

					if(leafCount < 2) {
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



		public int TransformFullBlocks(in ReadOnlySpan<byte> data) {
			var dataSlice = data.Slice(0, Math.Min(16 << 20, data.Length) & ~2047);

			fixed(byte* dataPtr = dataSlice) {
				foreach(var blockHasher in blockHashers) blockHasher.ProcessData(dataPtr, dataSlice.Length);
				WaitHandle.WaitAll(blockHashersSync);
			}
			leafCount += dataSlice.Length >> 10;

			Compress();

			return dataSlice.Length;
		}

		private byte* compressBuffer = NativeMethods.TTHCreateNode();
		private void Compress() {
			var leafPairsProcessed = 0;

			//Since we assume Only the last datablock may have an odd number of leaves
			fixed(byte* leavesPtr = leaves)
			fixed(byte* nodesPtr = nodes) {
				while(leafCount > 1) {
					var levelIsEmpty = (nodeCount & 1) == 0;
					NativeMethods.TTHNodeHash(leavesPtr + leafPairsProcessed * 48, compressBuffer, nodesPtr + (levelIsEmpty ? 0 : 24));
					leafPairsProcessed++;
					leafCount -= 2;

					var currentLevel = 0;
					while(!levelIsEmpty) {
						levelIsEmpty = (nodeCount & (2 << currentLevel)) == 0;
						NativeMethods.TTHNodeHash(nodesPtr + currentLevel * 48, compressBuffer, nodesPtr + (currentLevel + 1) * 48 + (levelIsEmpty ? 0 : 24));
						currentLevel++;
					}
					nodeCount++;
				}
			}
		}


		//public void Dispose() {
		//	NativeMethods.FreeHashObject((IntPtr)compressBuffer);
		//	foreach(var blockHasher in blockHashers) blockHasher.Dispose();
		//	compressBuffer = (byte*)0;
		//}
		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if(!disposedValue) {

				NativeMethods.FreeHashObject((IntPtr)compressBuffer);
				foreach(var blockHasher in blockHashers) blockHasher.Dispose();
				compressBuffer = (byte*)0;

				disposedValue = true;
			}
		}

		~TigerTreeHashAlgorithm() => Dispose(false);
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		private class BlockHasher : IDisposable {
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

			public void Dispose() {
				Finish();
				doWorkSync.Dispose();
			}

			void DoWork() {
				var buffer = NativeMethods.TTHCreateBlock();

				doWorkSync.WaitOne();
				while(!threadJoin) {
					fixed(byte* leavesPtr = leaves) {
						while(lengthData >= BLOCKSIZE) {
							NativeMethods.TTHBlockHash(dataPtr + offsetData, buffer, leavesPtr + offsetLeaf);

							offsetLeaf += nextBlockOffsetLeaf;
							offsetData += nextBlockOffsetData;
							lengthData -= nextBlockOffsetData;
						}
					}
					WorkDoneSync.Set();
					doWorkSync.WaitOne();
				}
				NativeMethods.FreeHashObject((IntPtr)buffer);
			}

		}


	}
}