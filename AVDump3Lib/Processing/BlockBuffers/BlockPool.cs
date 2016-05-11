using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.BlockBuffers {
    public interface IBlockPool {
        int BlockCount { get; }
        int BlockSize { get; }
        byte[][] Take();
        void Release(byte[][] blocks);
    }
    public class BlockPool : IBlockPool {
        public int BlockCount { get; }
        public int BlockSize { get; }

        private Stack<byte[][]> slots = new Stack<byte[][]>();

        public BlockPool(int blockCount, int blockSize) {
            BlockCount = blockCount;
            BlockSize = blockSize;

            //slots = new Stack<byte[][]>(
            //	Enumerable.Range(0, slotCount).Select(i =>
            //	Enumerable.Range(0, BlockCount).Select(j =>
            //	new byte[BlockSize]).ToArray()));
        }
        public byte[][] Take() {
            lock(slots) {
                if(slots.Count != 0) {
                    return slots.Pop();
                } else {
                    return Enumerable.Range(0, BlockCount).Select(j => new byte[BlockSize]).ToArray();
                }
            }
        }
        public void Release(byte[][] blocks) { lock(slots) slots.Push(blocks); }
    }
}
