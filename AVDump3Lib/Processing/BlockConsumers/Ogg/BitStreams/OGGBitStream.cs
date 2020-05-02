using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
	public abstract class OGGBitStream {
		public uint Id { get; private set; }
		public long Size { get; private set; }
		public long LastGranulePosition { get; private set; }
		public abstract string CodecName { get; }
		public abstract string CodecVersion { get; protected set; }

		public bool IsOfficiallySupported { get; private set; }

		public OGGBitStream(bool isOfficiallySupported) { IsOfficiallySupported = isOfficiallySupported; }


		public static OGGBitStream ProcessBeginPage(ref OggPage page) {
			OGGBitStream bitStream = null;
			if(page.Data.Length >= 29 && Encoding.ASCII.GetString(page.Data.Slice(1, 5)).Equals("video")) {
				bitStream = new OGMVideoOGGBitStream(page.Data);
			} else if(page.Data.Length >= 46 && Encoding.ASCII.GetString(page.Data.Slice(1, 5)).Equals("audio")) {
				bitStream = new OGMAudioOGGBitStream(page.Data);
			} else if(page.Data.Length >= 0x39 && Encoding.ASCII.GetString(page.Data.Slice(1, 4)).Equals("text")) {
				bitStream = new OGMTextOGGBitStream(page.Data);
			} else if(page.Data.Length >= 42 && Encoding.ASCII.GetString(page.Data.Slice(1, 6)).Equals("theora")) {
				bitStream = new TheoraOGGBitStream(page.Data);
			} else if(page.Data.Length >= 30 && Encoding.ASCII.GetString(page.Data.Slice(1, 6)).Equals("vorbis")) {
				bitStream = new VorbisOGGBitStream(page.Data);
			} else if(page.Data.Length >= 79 && Encoding.ASCII.GetString(page.Data.Slice(1, 4)).Equals("FLAC")) {
				bitStream = new FlacOGGBitStream(page.Data);
			}

			if(bitStream == null) bitStream = new UnknownOGGBitStream();
			bitStream.Id = page.StreamId;

			return bitStream;
		}

		public virtual void ProcessPage(ref OggPage page) {
			var granulePosition = MemoryMarshal.Read<long>(page.GranulePosition);
			LastGranulePosition = granulePosition > LastGranulePosition ? granulePosition : LastGranulePosition;

			Size += page.Data.Length;
		}
	}
}
