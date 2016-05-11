using System;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public class CeltOGGBitStream : AudioOGGBitStream {
		public override string CodecName { get { return "Celt"; } }
		public override string CodecVersion { get; protected set; }

		public CeltOGGBitStream()
			: base(true) {

		}


		internal override void ProcessPage(Page page) {
			base.ProcessPage(page);

			var sampleIndex = BitConverter.ToInt64(page.GranulePosition, 0);
			if(SampleCount < (int)sampleIndex) SampleCount = (int)sampleIndex;
		}
	}
}
