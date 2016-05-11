using System;
using System.Runtime.InteropServices;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
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
			public Int64 TimeUnit;
			public Int64 SamplesPerUnit;
			public Int32 DefaultLength;
			public Int32 BufferSize;
			public Int16 BitsPerSample;
			public Int16 Unknown;
			public Int16 ChannelCount;
			public Int16 BlockAlign;
			public Int32 Byterate;
		}

		internal override void ProcessPage(Page page) {
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
