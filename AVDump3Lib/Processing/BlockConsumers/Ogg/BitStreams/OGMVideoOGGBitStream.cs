using System;
using System.Runtime.InteropServices;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public class OGMVideoOGGBitStream : VideoOGGBitStream, IOGMStream, IVorbisComment {
		public override string CodecName { get { return "OGMVideo"; } }
		public override string CodecVersion { get; protected set; }
		public string ActualCodecName { get; private set; }

		public OGMVideoOGGBitStream(byte[] header, int offset)
			: base(false) {
			var codecInfo = GetStruct<OGMVideoHeader>(header, offset + 1, 0x38);
			ActualCodecName = new string(codecInfo.SubType);
			FrameRate = 10000000d / codecInfo.TimeUnit;
			Width = codecInfo.Width;
			Height = codecInfo.Height;
		}

		[StructLayout(LayoutKind.Sequential, Size = 52)]
		public struct OGMVideoHeader {
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
			public int Width;
			public int Height;
		}

		internal override void ProcessPage(Page page) {
			base.ProcessPage(page);
			commentParser.ParsePage(page);

			var frameIndex = BitConverter.ToInt64(page.GranulePosition,0);
			if(FrameCount < (int)frameIndex) FrameCount = (int)frameIndex;
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
