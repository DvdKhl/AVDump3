using System;
using System.Collections.Generic;
using System.Text;

namespace AVDump3Lib.Processing.BlockBuffers {
    public interface IMirroredBufferPool {
        int BufferSize { get; }
        IMirroredBuffer Take();
        void Release(IMirroredBuffer buffer);
    }

    public class MirroredBufferPool : IMirroredBufferPool {
        public int BufferSize { get; }
        public int BlockSize { get; }

        private Stack<IMirroredBuffer> slots = new Stack<IMirroredBuffer>();

        public MirroredBufferPool(int bufferSize) { BufferSize = bufferSize; }

        public IMirroredBuffer Take() {
            lock(slots) {
                if(slots.Count != 0) {
                    return slots.Pop();
                } else {
                    return new MirroredBufferWindows(BufferSize);
                }
            }
        }
        public void Release(IMirroredBuffer buffer) { lock(slots) slots.Push(buffer); }
    }
}
