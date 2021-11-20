using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;

public class OGMVideoOGGBitStream : VideoOGGBitStream, IOGMStream, IVorbisComment {
	public override string CodecName => "OGMVideo";
	public override string CodecVersion { get; protected set; }
	public override long FrameCount => LastGranulePosition;
	public override double FrameRate { get; }

	public string ActualCodecName { get; private set; }

	public unsafe OGMVideoOGGBitStream(ReadOnlySpan<byte> header)
		: base(false) {
		var codecInfo = MemoryMarshal.Read<OGMVideoHeader>(header.Slice(1, 0x38));
		ActualCodecName = new string(codecInfo.SubType, 0, 4, Encoding.ASCII);
		FrameRate = 10000000d / codecInfo.TimeUnit;
		Width = codecInfo.Width;
		Height = codecInfo.Height;

	}

	[StructLayout(LayoutKind.Sequential, Size = 52)]
	public unsafe struct OGMVideoHeader {
		public fixed sbyte StreamType[8];
		public fixed sbyte SubType[4];
		public int Size;
		public long TimeUnit;
		public long SamplesPerUnit;
		public int DefaultLength;
		public int BufferSize;
		public short BitsPerSample;
		public int Width;
		public int Height;
	}

	public override void ProcessPage(ref OggPage page) {
		base.ProcessPage(ref page);
		commentParser.ParsePage(ref page);
	}

	private readonly VorbisCommentParser commentParser = new();
	public Comments Comments {
		get {
			try {
				return commentParser.RetrieveComments();
			} catch(Exception) {
				return null; //TODO: Log warning
			}
		}
	}

}
