using System;

namespace AVDump3Lib.BlockBuffers {
    public interface IBlockStreamReader {
		bool Advance();
		byte[] GetBlock(out int blockLength);
		long Length { get; }
		long ReadBytes { get; }
        void DropOut();
	}

	public class BlockStreamReader : IBlockStreamReader {
		private int readerIndex;
		private IBlockStream blockStream;

		
		private int curBlockLength;

		public long ReadBytes { get; private set; }

		public long Length { get; } //Faster Access. Needed?

		public BlockStreamReader(IBlockStream blockStream, int readerIndex) {
			this.blockStream = blockStream;
			this.readerIndex = readerIndex;
			Length = blockStream.Length;
		}

		public byte[] GetBlock(out int blockLength) {
			var block = blockStream.GetBlock(readerIndex);

			curBlockLength = (int)Math.Min(block.Length, Length - ReadBytes);
			blockLength = curBlockLength;

			return block;
		}
		public bool Advance() {
			ReadBytes += curBlockLength;
			return blockStream.Advance(readerIndex);
		}

        public void DropOut() { blockStream.DropOut(readerIndex); }
    }
}
