namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public abstract class AudioOGGBitStream : OGGBitStream {
		public AudioOGGBitStream(bool isOfficiallySupported) : base(isOfficiallySupported) { }
		public int SampleCount { get; protected set; }
		public double SampleRate { get; protected set; }
		public int ChannelCount { get; protected set; }
	}
}
