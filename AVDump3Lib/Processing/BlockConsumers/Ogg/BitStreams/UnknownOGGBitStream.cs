namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;

public class UnknownOGGBitStream : OGGBitStream {
	public UnknownOGGBitStream() : base(false) { }

	public override string CodecName => "Unknown";
	public override string CodecVersion { get; protected set; }
}
