using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class OGMAudioOGGBitStream : AudioOGGBitStream, IOGMStream, IVorbisComment {
		public override string CodecName { get { return "OGMAudio"; } }
		public override string CodecVersion { get; protected set; }
		public string ActualCodecName { get; private set; }


		public OGMAudioOGGBitStream(ReadOnlySpan<byte> header)
			: base(false) {
			var codecInfo = MemoryMarshal.Read<OGMAudioHeader>(header.Slice(1, 56));
			ChannelCount = codecInfo.ChannelCount;
			SampleRate = codecInfo.SamplesPerUnit;
			ActualCodecName = new string(codecInfo.SubType);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OGMAudioHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public char[] StreamType;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public char[] SubType;
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

			var sampleIndex = MemoryMarshal.Read<long>(page.GranulePosition);
			if(SampleCount < (int)sampleIndex) SampleCount = (int)sampleIndex;
		}

		private VorbisCommentParser commentParser = new VorbisCommentParser();
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
}
