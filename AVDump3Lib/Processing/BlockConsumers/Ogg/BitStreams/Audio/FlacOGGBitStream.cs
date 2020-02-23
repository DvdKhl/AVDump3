using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class FlacOGGBitStream : AudioOGGBitStream {
		public override string CodecName { get { return "Flac"; } }
		public override string CodecVersion { get; protected set; }
		public override long SampleCount => LastGranulePosition;
		public override double SampleRate { get; }

		public FlacOGGBitStream(ReadOnlySpan<byte> header)
			: base(true) {
			SampleRate = header[33] << 12 | header[34] << 4 | (header[35] & 0xF0) >> 4; //TODO: check offsets
			ChannelCount = ((header[35] & 0x0E) >> 1) + 1;
		}
	}
}
