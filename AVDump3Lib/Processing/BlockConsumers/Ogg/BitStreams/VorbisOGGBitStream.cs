using System;
using System.Runtime.InteropServices;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public class VorbisOGGBitStream : AudioOGGBitStream, IVorbisComment {
		public override string CodecName { get { return "Vorbis"; } }
		public override string CodecVersion { get; protected set; }

		//public long SampleCount { get; private set; }
		//public long BitRate { get; private set; }

		public VorbisOGGBitStream(byte[] header, int offset)
			: base(true) {
			var codecInfo = GetStruct<VorbisIdentHeader>(header, offset + 7, 23);
			ChannelCount = codecInfo.ChannelCount;
			SampleRate = codecInfo.SampleRate;
			CodecVersion = codecInfo.Version.ToString();
			//BitRate = codecInfo.NomBitrate;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct VorbisIdentHeader {
			public UInt32 Version;
			public Byte ChannelCount;
			public UInt32 SampleRate;
			public Int32 MaxBitrate;
			public Int32 NomBitrate;
			public Int32 MinBitrate;
			public Byte BlockSizes;
			public Boolean Framing;
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
