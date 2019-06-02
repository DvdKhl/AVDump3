using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public class OGMVideoOGGBitStream : VideoOGGBitStream, IOGMStream, IVorbisComment {
		public override string CodecName { get { return "OGMVideo"; } }
		public override string CodecVersion { get; protected set; }
		public string ActualCodecName { get; private set; }

		public OGMVideoOGGBitStream(ReadOnlySpan<byte> header)
			: base(false) {
			var codecInfo = MemoryMarshal.Read<OGMVideoHeader>(header.Slice(1, 0x38));
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

		public override void ProcessPage(ref OggPage page) {
			base.ProcessPage(ref page);
			commentParser.ParsePage(ref page);

			var frameIndex = MemoryMarshal.Read<long>(page.GranulePosition);
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
