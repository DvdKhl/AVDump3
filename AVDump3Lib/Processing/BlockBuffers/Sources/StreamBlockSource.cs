using System;
using System.IO;

namespace AVDump3Lib.Processing.BlockBuffers.Sources {
    public class StreamBlockSource : IBlockSource {
		private readonly Stream source;
		public long Length { get; }

		public StreamBlockSource(Stream source) { this.source = source; Length = source.Length; }

		public int Read(Span<byte> block) => source.Read(block);


		
		private long position;
		//public int Read(Span<byte> block) {
		//	var bytesread = (int)Math.Min(block.Length, Length - position);
		//	position += bytesread;
		//	return bytesread;
		//}
		
	}
}
