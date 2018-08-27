using System;
using System.IO;

namespace AVDump3Lib.Processing.BlockBuffers.Sources {
    public class StreamBlockSource : IBlockSource {
		private readonly Stream source;

		public StreamBlockSource(Stream source) { this.source = source; }
		public long Length => source.Length;
		public int Read(Span<byte> block) => source.Read(block);
	}
}
