using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams {
    public abstract class OGGBitStream {
        public uint Id { get; private set; }
        public long Size { get; private set; }
        public long Duration { get; protected set; }
        public abstract string CodecName { get; }
        public abstract string CodecVersion { get; protected set; }

        public bool IsOfficiallySupported { get; private set; }

        public OGGBitStream(bool isOfficiallySupported) { IsOfficiallySupported = isOfficiallySupported; }


        public static OGGBitStream ProcessBeginPage(OggPage page) {
            OGGBitStream bitStream = null;
            if(page.DataLength - page.DataOffset >= 29 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 5).Equals("video")) {
                bitStream = new OGMVideoOGGBitStream(page.Data, page.DataOffset);
            } else if(page.DataLength - page.DataOffset >= 42 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 6).Equals("theora")) {
                bitStream = new TheoraOGGBitStream(page.Data, page.DataOffset);
            } else if(page.DataLength - page.DataOffset >= 30 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 6).Equals("vorbis")) {
                bitStream = new VorbisOGGBitStream(page.Data, page.DataOffset);
            } else if(page.DataLength - page.DataOffset >= 79 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 4).Equals("FLAC")) {
                bitStream = new FlacOGGBitStream(page.Data, page.DataOffset);
            } else if(page.DataLength - page.DataOffset >= 46 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 5).Equals("audio")) {
                bitStream = new OGMAudioOGGBitStream(page.Data, page.DataOffset);
            } else if(page.DataLength - page.DataOffset >= 0x39 && Encoding.ASCII.GetString(page.Data, page.DataOffset + 1, 4).Equals("text")) {
                bitStream = new OGMTextOGGBitStream(page.Data, page.DataOffset);
            }

            if(bitStream == null) bitStream = new UnknownOGGBitStream();
            bitStream.Id = page.StreamId;

            return bitStream;
        }

        public virtual void ProcessPage(OggPage page) {
            Size += page.DataLength;
        }


        protected unsafe static T GetStruct<T>(byte[] b, int offset, int length) {
            var structBytes = new byte[length];
            Buffer.BlockCopy(b, offset, structBytes, 0, length);
            fixed (byte* structPtr = structBytes)
            {
                return (T)Marshal.PtrToStructure((IntPtr)structPtr, typeof(T));
            }
        }
    }
}
