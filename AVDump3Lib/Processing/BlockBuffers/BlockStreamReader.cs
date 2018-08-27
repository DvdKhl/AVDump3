using System;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Processing.BlockBuffers {
	public interface IBlockStreamReader {
		bool Advance();
		byte[] GetBlock(out int blockLength);
		long Length { get; }
		long ReadBytes { get; }
		bool DroppedOut { get; }

		void DropOut();
	}

	public class BlockStreamReader : IBlockStreamReader {
		private int readerIndex;
        private int curBlockLength;
		private IBlockStream blockStream;

		public long ReadBytes { get; private set; }

		public long Length { get; } //Faster Access. Needed?

		public bool DroppedOut { get; private set; }

		public BlockStreamReader(IBlockStream blockStream, int readerIndex) {
			this.blockStream = blockStream;
			this.readerIndex = readerIndex;
			Length = blockStream.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] GetBlock(out int blockLength) {
			var block = blockStream.GetBlock(readerIndex, out blockLength);
            curBlockLength = blockLength;

            return block;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Advance() {
            ReadBytes += curBlockLength;
            return blockStream.Advance(readerIndex);
		}

		public void DropOut() { blockStream.DropOut(readerIndex); DroppedOut = true; }
	}
}
