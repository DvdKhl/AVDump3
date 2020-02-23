using System;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public abstract class VideoOGGBitStream : OGGBitStream {
		public VideoOGGBitStream(bool isOfficiallySupported) : base(isOfficiallySupported) { }

		public abstract long FrameCount { get; }
		public abstract double FrameRate { get; }
		public int Width { get; protected set; }
		public int Height { get; protected set; }
		public virtual TimeSpan Duration => TimeSpan.FromSeconds(FrameCount / FrameRate);
	}
}
