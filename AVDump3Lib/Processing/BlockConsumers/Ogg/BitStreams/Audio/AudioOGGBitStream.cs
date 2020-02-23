using System;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public abstract class AudioOGGBitStream : OGGBitStream {
		public AudioOGGBitStream(bool isOfficiallySupported) : base(isOfficiallySupported) { }
		public abstract long SampleCount { get; }
		public abstract double SampleRate { get; }
		public virtual TimeSpan Duration => TimeSpan.FromSeconds(SampleCount / SampleRate);
		public int ChannelCount { get; protected set; }
	}
}
