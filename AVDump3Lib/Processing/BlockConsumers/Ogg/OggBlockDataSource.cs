using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
    public enum PageFlags { None = 0, SpanBefore = 1, Header = 2, Footer = 4, SpanAfter = 1 << 31 }

    public class OggPage {
        public long FilePosition;
        public long DataPosition;
        public PageFlags Flags;
        public byte Version;
        public byte[] GranulePosition;
        public uint StreamId;
        public uint PageIndex;
        public byte[] Checksum;
        public byte SegmentCount;
        public int[] PacketOffsets;

        public byte[] Data;
        public int DataLength;
        public int DataOffset;
    }

    public class OggBlockDataSource {
        private IBlockStreamReader reader;

        private int blockLength;
        private byte[] block;

        public OggBlockDataSource(IBlockStreamReader reader) {
            this.reader = reader;

            block = reader.GetBlock(out blockLength);
        }

        public long Length => reader.Length;

        public long Position {
            get { return reader.ReadBytes + LocalPosition; }
        }

        public int LocalPosition { get; set; }


        private static byte[] OggS = { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };
        public bool SeekPastSyncBytes(bool strict) {
            int syncIndex = 0;

            while(true) {
                while(syncIndex < 4 && LocalPosition < blockLength) {
                    if(block[LocalPosition] == OggS[syncIndex]) {
                        syncIndex++;
                    } else {
                        if(strict) return false;
                        syncIndex = block[LocalPosition] == OggS[0] ? 1 : 0;
                    }
                    LocalPosition++;
                }
                if(syncIndex == 4) return true;

                if(reader.Advance()) break;
                block = reader.GetBlock(out blockLength);
            }
            return false;
        }

        public bool ReadOggPage(OggPage page) {
            if(!SeekPastSyncBytes(false)) return false;

            int offset;
            var block = Read(23, out offset);

            page.FilePosition = Position;
            page.Version = block[offset++];
            page.Flags = (PageFlags)block[offset++];
            page.GranulePosition = new byte[] { block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++] };
            page.StreamId = BitConverter.ToUInt32(block, offset); offset += 4;
            page.PageIndex = BitConverter.ToUInt32(block, offset); offset += 4;
            page.Checksum = new byte[] { block[offset++], block[offset++], block[offset++], block[offset++] };

            var segmentCount = page.SegmentCount = block[offset++];
            block = Read(segmentCount, out offset);

            var packetOffsets = new List<int>();
            while(segmentCount != 0) {
                page.DataLength += block[offset];

                if(block[offset] != 255) packetOffsets.Add(page.DataLength);

                offset++;
                segmentCount--;
            }
            page.PacketOffsets = packetOffsets.ToArray();
            if(block[offset - 1] == 255) page.Flags |= PageFlags.SpanAfter;

            page.DataPosition = Position;
            page.Data = Read(page.DataLength, out page.DataOffset);

            return true;
        }

        public byte[] Read(int readLength, out int offset) {
            if(blockLength - LocalPosition > readLength) {
                offset = LocalPosition;
                LocalPosition += readLength;
                return block;

            } else {
                offset = 0;

                var data = new byte[readLength];

                var bytesRead = 0;
                do {
                    var bytesToRead = Math.Min(blockLength - LocalPosition, readLength - bytesRead);
                    Buffer.BlockCopy(block, LocalPosition, data, bytesRead, bytesToRead);
                    bytesRead += bytesToRead;
                    LocalPosition += bytesToRead;

                    if(bytesRead != readLength) {
                        if(!reader.Advance()) throw new InvalidOperationException("Tried to read past end of stream");
                        block = reader.GetBlock(out blockLength);
                        LocalPosition = 0;
                    }

                } while(bytesRead != readLength);

                return data;
            }

        }

    }
}
