using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;

public class OGMAudioOGGBitStream : AudioOGGBitStream, IOGMStream, IVorbisComment {
	public override string CodecName => "OGMAudio";
	public override string CodecVersion { get; protected set; }
	public string ActualCodecName { get; private set; }
	public override long SampleCount => LastGranulePosition;
	public override double SampleRate { get; }


	public unsafe OGMAudioOGGBitStream(ReadOnlySpan<byte> header)
		: base(false) {
		var codecInfo = MemoryMarshal.Read<OGMAudioHeader>(header.Slice(1, 56));
		ChannelCount = codecInfo.ChannelCount;
		SampleRate = codecInfo.SamplesPerUnit;
		ActualCodecName = new string(codecInfo.SubType, 0, 4, Encoding.ASCII);
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct OGMAudioHeader {
		public fixed sbyte StreamType[8];
		public fixed sbyte SubType[4];
		public int Size;
		public long TimeUnit;
		public long SamplesPerUnit;
		public int DefaultLength;
		public int BufferSize;
		public short BitsPerSample;
		public short Unknown;
		public short ChannelCount;
		public short BlockAlign;
		public int Byterate;
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
