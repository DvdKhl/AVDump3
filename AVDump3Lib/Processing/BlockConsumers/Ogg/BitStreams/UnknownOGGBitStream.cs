namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public class UnknownOGGBitStream : OGGBitStream {
		public UnknownOGGBitStream() : base(false) {}

		public override string CodecName { get { return "Unknown"; } }
		public override string CodecVersion { get; protected set; }
	}
}
