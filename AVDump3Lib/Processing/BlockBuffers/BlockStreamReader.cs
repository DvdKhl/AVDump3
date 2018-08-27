using System;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Processing.BlockBuffers {
    public interface IBlockStreamReader {
        bool Advance(int length);
        ReadOnlySpan<byte> GetBlock();
        long Length { get; }
        long ReadBytes { get; }
        bool Completed { get; }

        void Complete();
    }

    public class BlockStreamReader : IBlockStreamReader {
        private readonly int readerIndex;
        private readonly IBlockStream blockStream;

        public long ReadBytes { get; private set; }

        public long Length => blockStream.Length;

        public bool Completed { get; private set; }

        public BlockStreamReader(IBlockStream blockStream, int readerIndex) {
            this.blockStream = blockStream;
            this.readerIndex = readerIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetBlock() => blockStream.GetBlock(readerIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Advance(int length) {
            ReadBytes += length;
            return blockStream.Advance(readerIndex, length);
        }

        public void Complete() {
            blockStream.CompleteConsumption(readerIndex);
            Completed = true;
        }
    }
}
