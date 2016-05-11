namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public abstract class VideoOGGBitStream : OGGBitStream {
		public VideoOGGBitStream(bool isOfficiallySupported) : base(isOfficiallySupported) { }

		public int FrameCount { get; protected set; }
		public double FrameRate { get; protected set; }
		public int Width { get; protected set; }
		public int Height { get; protected set; }
	}
}
