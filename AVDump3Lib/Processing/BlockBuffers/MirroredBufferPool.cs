using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockBuffers {
	public interface IMirroredBufferPool {
		int BufferSize { get; }
		IMirroredBuffer Take();
		void Release(IMirroredBuffer buffer);
	}

	public class MirroredBufferPool : IMirroredBufferPool {
		public int BufferSize { get; }

		private readonly Stack<IMirroredBuffer> slots = new();

		public MirroredBufferPool(int bufferSize) { BufferSize = bufferSize; }

		public IMirroredBuffer Take() {
			lock(slots) {
				if(slots.Count != 0) {
					return slots.Pop();
				} else {
					return new MirroredBuffer(BufferSize);
				}
			}
		}
		public void Release(IMirroredBuffer buffer) { lock(slots) slots.Push(buffer); }
	}
}
