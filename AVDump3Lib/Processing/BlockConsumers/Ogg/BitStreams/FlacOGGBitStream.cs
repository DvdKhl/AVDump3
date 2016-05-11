using System;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public class FlacOGGBitStream : AudioOGGBitStream {
		public override string CodecName { get { return "Flac"; } }
		public override string CodecVersion { get; protected set; }

		public FlacOGGBitStream(byte[] header, int offset)
			: base(true) {
			SampleRate = header[offset + 33] << 12 | header[offset + 34] << 4 | (header[offset + 35] & 0xF0) >> 4; //TODO: check offsets
			ChannelCount = ((header[offset + 35] & 0x0E) >> 1) + 1;
		}

		internal override void ProcessPage(Page page) {
			base.ProcessPage(page);

			var sampleIndex = BitConverter.ToInt64(page.GranulePosition, 0);
			if(SampleCount < (int)sampleIndex) SampleCount = (int)sampleIndex;
		}
	}
}
