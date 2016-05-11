using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump2Lib.BlockConsumers.Ogg.BitStreams {
    public abstract class OGGBitStream {
		public uint Id { get; private set; }
		public long Size { get; private set; }
		public long Duration { get; protected set; }
		public abstract string CodecName { get; }
		public abstract string CodecVersion { get; protected set; }

		public bool IsOfficiallySupported { get; private set; }

		public OGGBitStream(bool isOfficiallySupported) { IsOfficiallySupported = isOfficiallySupported; }

		internal static OGGBitStream ProcessBeginPage(Page page) {
			int offset;
			var data = page.GetData(out offset);

			OGGBitStream bitStream = null;
			if(data.Length - offset >= 0x39 && Encoding.ASCII.GetString(data, offset + 1, 5).Equals("video")) {
				bitStream = new OGMVideoOGGBitStream(data, offset);
			} else if(data.Length - offset >= 42 && Encoding.ASCII.GetString(data, offset + 1, 6).Equals("theora")) {
				bitStream = new TheoraOGGBitStream(data, offset);
			} else if(data.Length - offset >= 30 && Encoding.ASCII.GetString(data, offset + 1, 6).Equals("vorbis")) {
				bitStream = new VorbisOGGBitStream(data, offset);
			} else if(data.Length - offset >= 79 && Encoding.ASCII.GetString(data, offset + 1, 4).Equals("FLAC")) {
				bitStream = new FlacOGGBitStream(data, offset);
			} else if(data.Length - offset >= 46 && Encoding.ASCII.GetString(data, offset + 1, 5).Equals("audio")) {
				bitStream = new OGMAudioOGGBitStream(data, offset);
			} else if(data.Length - offset >= 0x39 && Encoding.ASCII.GetString(data, offset + 1, 4).Equals("text")) {
				bitStream = new OGMTextOGGBitStream(data, offset);
			}

			if(bitStream == null) bitStream = new UnknownOGGBitStream();
			bitStream.Id = page.StreamId;

			return bitStream;
		}

		internal virtual void ProcessPage(Page page) {
			Size += page.DataLength;
			
		}


		protected static T GetStruct<T>(byte[] b, int offset, int length) {
			var structBytes = new byte[length];
			Buffer.BlockCopy(b, offset, structBytes, 0, length);

			GCHandle hDataIn = GCHandle.Alloc(structBytes, GCHandleType.Pinned);
			T structure = (T)Marshal.PtrToStructure(hDataIn.AddrOfPinnedObject(), typeof(T));
			hDataIn.Free();
			return structure;
		}
	}
}
