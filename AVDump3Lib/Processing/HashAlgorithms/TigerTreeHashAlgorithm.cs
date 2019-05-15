using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class TigerTreeHashAlgorithm : AVDHashAlgorithm {
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
		public override int BlockSize => BLOCKSIZE;

		public TigerTreeHashAlgorithm(int threadCount) {
			blockHashers = Enumerable.Range(0, threadCount).Select(x => new BlockHasher(x, threadCount)).ToArray();
			blockHashersSync = blockHashers.Select(x => x.WorkDoneSync).ToArray();
		}


		public override void Initialize() {
			leafCount = 0;
			nodeCount = 0;
			Array.Clear(nodes, 0, nodes.Length);
			Array.Clear(leaves, 0, leaves.Length);
		}

		public override ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data) => throw new NotImplementedException();


		private bool hasLastBlock;
		protected override void HashCore(ReadOnlySpan<byte> data) {
			if (hasLastBlock && data.Length != 0) throw new Exception();


			foreach (var blockHasher in blockHashers) {
				blockHasher.ProcessData(ref data);
			}
			WaitHandle.WaitAll(blockHashersSync);


			if ((data.Length & (BLOCKSIZE - 1)) != 0) {
				fixed (byte* leavesPtr = leaves)
				fixed (byte* dataPtr = data) {
					TigerNativeHashAlgorithm.TTHPartialBlockHash(
						dataPtr + (data.Length - data.Length % BLOCKSIZE), 
						data.Length % BLOCKSIZE,
						leavesPtr + data.Length / BLOCKSIZE + 1
					);
				}
				hasLastBlock = true;
			}

			Compress();
		}

		private void Compress() {
			var leafPairsProcessed = 0;

			fixed (byte* leavesPtr = leaves)
			fixed (byte* nodesPtr = nodes) {
				while (leafCount > 1) {
					var levelIsEmpty = (nodeCount & 1) == 0;
					TigerNativeHashAlgorithm.TTHNodeHash(leavesPtr + leafPairsProcessed * 48, nodesPtr + (levelIsEmpty ? 0 : 24));
					leafPairsProcessed++;
					leafCount -= 2;

					var currentLevel = 0;
					while (!levelIsEmpty) {
						levelIsEmpty = (nodeCount & (2 << currentLevel)) == 0;
						TigerNativeHashAlgorithm.TTHNodeHash(nodesPtr + currentLevel * 48, nodesPtr + (currentLevel + 1) * 48 + (levelIsEmpty ? 0 : 24));
					}
					nodeCount++;
				}
			}
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

			public BlockHasher(int index, int count) {
				this.index = index;

				nextBlockOffsetData = count * BLOCKSIZE;
				nextBlockOffsetLeaf = count * 24;

				doWorkSync = new AutoResetEvent(false);
				WorkDoneSync = new AutoResetEvent(false);

				hashThread = new Thread(DoWork);
				hashThread.Start();
			}

			public void ProcessData(ref ReadOnlySpan<byte> data) {
				fixed (byte* ptr = data) dataPtr = ptr;

				offsetLeaf = index * 24;
				offsetData = index * BLOCKSIZE;
				lengthData = data.Length - index * BLOCKSIZE;

				doWorkSync.Set();
			}

			public void Finish() {
				threadJoin = true;
				doWorkSync.Set();
				hashThread.Join();
			}

			void DoWork() {
				doWorkSync.WaitOne();
				while (!threadJoin) {
					fixed (byte* leavesPtr = leaves) {
						while (lengthData >= BLOCKSIZE) {
							TigerNativeHashAlgorithm.TTHBlockHash(dataPtr + offsetData, leavesPtr + offsetLeaf);

							offsetLeaf += nextBlockOffsetLeaf;
							offsetData += nextBlockOffsetData;
							lengthData -= nextBlockOffsetData;
						}
					}
					WorkDoneSync.Set();
					doWorkSync.WaitOne();
				}
			}

		}

	}
}


namespace AVDump2Lib.HashAlgorithms {



	public class TTH : HashAlgorithm {
		public const int BLOCKSIZE = 1024;
		private static byte[] zeroArray = new byte[] { 0 };
		private static byte[] oneArray = new byte[] { 1 };
		private static byte[] emptyArray = new byte[0];

		private Environment[] environments; private AutoResetEvent[] signals;

		private ITigerForTTH nodeHasher, blockHasher;
		private bool hasLastBlock;
		private bool hasStarted;
		private int threadCount;

		private Queue<byte[]> blocks;
		private LinkedList<byte[]> nods;
		private LinkedList<LinkedListNode<byte[]>> levels;
		private byte[] dataBlock;


		public TTH(int threadCount) {
			this.blocks = new Queue<byte[]>();
			this.nods = new LinkedList<byte[]>();
			this.levels = new LinkedList<LinkedListNode<byte[]>>();

			//nodeHasher = new TigerThex();
			//blockHasher = new TigerThex();
			nodeHasher = new TTHTiger();
			blockHasher = new TTHTiger();

			this.threadCount = threadCount /*= 1*/;

			signals = new AutoResetEvent[threadCount];
			environments = new Environment[threadCount];
			for (int i = 0; i < threadCount; i++) {
				environments[i] = new Environment(i);
				signals[i] = environments[i].WorkDone;
			}
		}

		protected override void HashCore(byte[] array, int ibStart, int cbSize) {
			if (!hasLastBlock && cbSize != 0) throw new Exception();
			if (!hasStarted) { foreach (var e in environments) e.HashThread.Start(e); hasStarted = true; }

			dataBlock = array;

			foreach (var e in environments) {
				e.Offset = ibStart;
				e.Length = cbSize;
				e.DoWork.Set();
			}
			WaitHandle.WaitAll(signals);


			for (int i = 0; i < cbSize / BLOCKSIZE; i++) blocks.Enqueue(environments[i % threadCount].Blocks.Dequeue());

			if ((cbSize & (BLOCKSIZE - 1)) != 0) {
				ibStart += cbSize - (cbSize & (BLOCKSIZE - 1));
				cbSize &= BLOCKSIZE - 1;

				blocks.Enqueue(blockHasher.TTHFinalBlockHash(array, ibStart, cbSize));
				hasLastBlock = false;
			}

			CompressBlocks();
		}
		private void DoWork(object obj) {
			var e = (Environment)obj;
			var envBlockSize = BLOCKSIZE * threadCount;
			var envOffset = BLOCKSIZE * e.Index;

			e.DoWork.WaitOne();
			while (!e.ThreadJoin) {
				e.Length -= envOffset;
				e.Offset += envOffset;
				while (e.Length >= BLOCKSIZE) {
					e.Blocks.Enqueue(e.BlockHasher.TTHBlockHash(dataBlock, e.Offset));
					e.Offset += envBlockSize;
					e.Length -= envBlockSize;
				}
				e.WorkDone.Set();
				e.DoWork.WaitOne();
			}
		}
		private void CompressBlocks() {
			if (levels.Last.Value == null && blocks.Count > 1) levels.Last.Value = nods.AddLast(nodeHasher.TTHNodeHash(blocks.Dequeue(), blocks.Dequeue()));
			while (blocks.Count > 1) nods.AddLast(nodeHasher.TTHNodeHash(blocks.Dequeue(), blocks.Dequeue()));

			var level = levels.Last;
			LinkedListNode<LinkedListNode<byte[]>> nextLevel;
			do {
				while (!(level.Value == null || //Level has no nods
				  (level.Value.Next == null) || //Level is at last node position (only one node available)
				  ((nextLevel = GetNextLevel(level)) != null && level.Value.Next == nextLevel.Value))) //Level has only one node
				{
					level.Value.Value = nodeHasher.TTHNodeHash(level.Value.Value, level.Value.Next.Value);
					nods.Remove(level.Value.Next);

					if (level.Previous == null) { //New level Node
						levels.AddFirst(level.Value);
					} else if (level.Previous.Value == null) { //First node in higher level
						level.Previous.Value = level.Value;
					}

					nextLevel = GetNextLevel(level);
					if (level.Value.Next == null || (nextLevel != null && level.Value.Next == nextLevel.Value)) {
						level.Value = null;
					} else {
						level.Value = level.Value.Next;
					}
				}

			} while ((level = level.Previous) != null);
		}
		private static LinkedListNode<LinkedListNode<byte[]>> GetNextLevel(LinkedListNode<LinkedListNode<byte[]>> level) {
			var nextLevel = level;
			while ((nextLevel = nextLevel.Next) != null) if (nextLevel.Value != null) return nextLevel;
			return null;
		}

		protected override byte[] HashFinal() {
			foreach (var e in environments) {
				e.ThreadJoin = true;
				e.DoWork.Set();
				e.HashThread.Join();
				e.HashThread = null;
			}

			foreach (var block in blocks) nods.AddLast(block);
			return nods.Count != 0 ? nods.Reverse().Aggregate((byte[] accumHash, byte[] hash) => nodeHasher.TTHNodeHash(hash, accumHash)) : blockHasher.ZeroArrayHash;
		}

		public override void Initialize() {
			//nodeHasher.TTHInitialize();
			//blockHasher.TTHInitialize();

			this.blocks.Clear();
			this.nods.Clear();
			this.levels.Clear();

			levels.AddFirst((LinkedListNode<byte[]>)null);

			hasStarted = false;
			hasLastBlock = true;

			foreach (var e in environments) {
				e.Blocks.Clear();
				e.DoWork.Reset();
				e.WorkDone.Reset();
				e.ThreadJoin = false;
				//e.BlockHasher.TTHInitialize();
				e.HashThread = new Thread(DoWork);
			}
		}

		private class Environment {
			public Thread HashThread;
			public bool ThreadJoin;
			public int Index;

			public ITigerForTTH BlockHasher = new TTHTiger();

			public Queue<byte[]> Blocks;
			public int Offset;
			public int Length;

			public AutoResetEvent WorkDone, DoWork;

			public Environment(int index) {
				DoWork = new AutoResetEvent(false);
				WorkDone = new AutoResetEvent(false);

				this.Index = index;

				Blocks = new Queue<byte[]>();
				//BlockHasher.TTHInitialize();
			}
		}

		public interface ITigerForTTH {
			//void TTHInitialize();
			byte[] TTHBlockHash(byte[] array, int ibStart);
			byte[] TTHFinalBlockHash(byte[] array, int ibStart, int cbSize);
			byte[] TTHNodeHash(byte[] l, byte[] r);

			byte[] ZeroArrayHash { get; }

		}
	}



}