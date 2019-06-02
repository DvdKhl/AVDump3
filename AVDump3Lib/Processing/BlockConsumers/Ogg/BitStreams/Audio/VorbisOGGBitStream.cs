using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public sealed class VorbisOGGBitStream : AudioOGGBitStream, IVorbisComment {
		public override string CodecName { get { return "Vorbis"; } }
		public override string CodecVersion { get; protected set; }

		//public long SampleCount { get; private set; }
		//public long BitRate { get; private set; }

		public VorbisOGGBitStream(ReadOnlySpan<byte> header)
			: base(true) {
			var codecInfo = MemoryMarshal.Read<VorbisIdentHeader>(header.Slice(7, 23));
			ChannelCount = codecInfo.ChannelCount;
			SampleRate = codecInfo.SampleRate;
			CodecVersion = codecInfo.Version.ToString();
			//BitRate = codecInfo.NomBitrate;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct VorbisIdentHeader {
			public uint Version;
			public byte ChannelCount;
			public uint SampleRate;
			public int MaxBitrate;
			public int NomBitrate;
			public int MinBitrate;
			public byte BlockSizes;
			public bool Framing;
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
