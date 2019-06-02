using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class CeltOGGBitStream : AudioOGGBitStream {
		public override string CodecName { get { return "Celt"; } }
		public override string CodecVersion { get; protected set; }

		public CeltOGGBitStream()
			: base(true) {

		}


		public override void ProcessPage(ref OggPage page) {
			base.ProcessPage(ref page);

			var sampleIndex = MemoryMarshal.Read<long>(page.GranulePosition);
			if(SampleCount < (int)sampleIndex) SampleCount = (int)sampleIndex;
		}
	}
}
