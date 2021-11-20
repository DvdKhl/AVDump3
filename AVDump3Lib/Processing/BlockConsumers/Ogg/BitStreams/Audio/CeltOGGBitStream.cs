namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class CeltOGGBitStream : AudioOGGBitStream {
		public override string CodecName => "Celt";
		public override string CodecVersion { get; protected set; }
		public override long SampleCount => LastGranulePosition;
		public override double SampleRate { get; }

		public CeltOGGBitStream()
			: base(true) {

		}
	}
}
