using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
    public class OGMAudioOGGBitStream : AudioOGGBitStream, IOGMStream, IVorbisComment {
		public override string CodecName { get { return "OGMAudio"; } }
		public override string CodecVersion { get; protected set; }
		public string ActualCodecName { get; private set; }


		public OGMAudioOGGBitStream(byte[] header, int offset)
			: base(false) {
			var codecInfo = GetStruct<OGMAudioHeader>(header, offset + 1, 56);
			ChannelCount = codecInfo.ChannelCount;
			SampleRate = codecInfo.SamplesPerUnit;
			ActualCodecName = new string(codecInfo.SubType);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OGMAudioHeader {
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public Char[] StreamType;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public Char[] SubType;
			public Int32 Size;
			public long TimeUnit;
			public long SamplesPerUnit;
			public Int32 DefaultLength;
			public Int32 BufferSize;
			public short BitsPerSample;
			public short Unknown;
			public short ChannelCount;
			public short BlockAlign;
			public Int32 Byterate;
		}

        public override void ProcessPage(OggPage page) {
			base.ProcessPage(page);
			commentParser.ParsePage(page);

			var sampleIndex = BitConverter.ToInt64(page.GranulePosition, 0);
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
