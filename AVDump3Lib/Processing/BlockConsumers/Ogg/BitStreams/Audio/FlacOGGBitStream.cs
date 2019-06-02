using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class FlacOGGBitStream : AudioOGGBitStream {
		public override string CodecName { get { return "Flac"; } }
		public override string CodecVersion { get; protected set; }

		public FlacOGGBitStream(ReadOnlySpan<byte> header)
			: base(true) {
			SampleRate = header[33] << 12 | header[34] << 4 | (header[35] & 0xF0) >> 4; //TODO: check offsets
			ChannelCount = ((header[35] & 0x0E) >> 1) + 1;
		}

		public override void ProcessPage(ref OggPage page) {
			base.ProcessPage(ref page);

			var sampleIndex = MemoryMarshal.Read<long>(page.GranulePosition);
			if(SampleCount < (int)sampleIndex) SampleCount = (int)sampleIndex;
		}
	}
}
