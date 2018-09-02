using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
    public enum PageFlags { None = 0, SpanBefore = 1, Header = 2, Footer = 4, SpanAfter = 1 << 31 }

    public ref struct OggPage {
        //public long FilePosition;
        //public long DataPosition;
        public PageFlags Flags;
        public byte Version;
        public ReadOnlySpan<byte> GranulePosition;
        public uint StreamId;
        public uint PageIndex;
        public ReadOnlySpan<byte> Checksum;
        public byte SegmentCount;
        public ReadOnlySpan<int> PacketOffsets;

        public ReadOnlySpan<byte> Data;
    }

    public class OggBlockDataSource {
        private IBlockStreamReader reader;


        public OggBlockDataSource(IBlockStreamReader reader) {
            this.reader = reader;

        }

        public long Length => reader.Length;

        private static uint OggS = BitConverter.ToUInt32(new[]{ (byte)'O', (byte)'g', (byte)'g', (byte)'S' });
        public bool SeekPastSyncBytes(bool strict) {
            while(true) {
                var offset = 0;//TODO rewrite
                var block = MemoryMarshal.Cast<byte, uint>(reader.GetBlock(reader.SuggestedReadLength & ~0xF)); //Max Page size    
                while( offset < block.Length) {
                    if(block[offset++] == OggS) {
                        reader.Advance(offset * 4);
                        return true;
                    }
                }
                if(reader.Advance(block.Length * 4 - 8)) break;
            }
            return false;
        }

        public bool ReadOggPage(ref OggPage page) {
            if(!SeekPastSyncBytes(false)) return false;

            var block = reader.GetBlock(23 + 256 * 256);

            //page.FilePosition = Position;
            page.Version = block[0];
            page.Flags = (PageFlags)block[1];
            page.GranulePosition = block.Slice(2, 8);
            page.StreamId = MemoryMarshal.Read<uint>(block.Slice(10));
            page.PageIndex = MemoryMarshal.Read<uint>(block.Slice(14));
            page.Checksum = block.Slice(18, 4);

            var segmentCount = page.SegmentCount = block[22];

            var offset = 0;
            var dataLength = 0;
            var packetOffsets = new List<int>();
            while(segmentCount != 0) {
                dataLength += block[23 + offset];

                if(block[23 + offset] != 255) packetOffsets.Add(dataLength);

                offset++;
                segmentCount--;
            }
            page.PacketOffsets = packetOffsets.ToArray();
            if(block[23 + offset - 1] == 255) page.Flags |= PageFlags.SpanAfter;

            //reader.BytesRead + 23 + offset;
            page.Data = block.Slice(23 + offset, dataLength);

            return true;
        }
    }
}
