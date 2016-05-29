using CSEBML.DataSource;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
    internal class Page {
		private static byte[] OggS = { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };

		public long FilePosition { get; private set; }
		public long DataPosition { get; private set; }

		public PageFlags Flags;
		public byte Version;
		public byte[] GranulePosition;
		public uint StreamId;
		public uint PageIndex;
		public byte[] Checksum;
		public byte SegmentCount;
		public int DataLength;

		public int[] PacketOffsets;

		private IEBMLDataSource src;

		public static Page Read(IEBMLDataSource src) {
			long offset;
			int syncIndex = 0;
			var block = src.GetData(4, out offset);

			while(syncIndex < 4 && src.Position + offset != src.Length) {
				if(offset == block.Length) block = src.GetData(1, out offset);
				if(OggS[syncIndex] == block[offset++]) syncIndex++; else syncIndex = 0;
			}
			if(syncIndex != 4 || src.Position + 27 > src.Length) return null;

			var page = new Page { src = src };
			block = src.GetData(23, out offset);

			page.FilePosition = src.Position;

			page.Version = block[offset++];
			page.Flags = (PageFlags)block[offset++];
			page.GranulePosition = new byte[] { block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++], block[offset++] };
			page.StreamId = BitConverter.ToUInt32(block, (int)offset); offset += 4;
			page.PageIndex = BitConverter.ToUInt32(block, (int)offset); offset += 4;
			page.Checksum = new byte[] { block[offset++], block[offset++], block[offset++], block[offset++] };

			var segmentCount = page.SegmentCount = block[offset++];
			if(src.Position + segmentCount > src.Length) return null;
			block = src.GetData(segmentCount, out offset);

			var packetOffsets = new List<int>();
			while(segmentCount != 0) {
				page.DataLength += block[offset];

				if(block[offset] != 255) packetOffsets.Add(page.DataLength);

				offset++;
				segmentCount--;
			}
			page.PacketOffsets = packetOffsets.ToArray();
			if(block[offset - 1] == 255) page.Flags |= PageFlags.SpanAfter;

			page.DataPosition = src.Position;

			return page;
		}

		public byte[] GetData(out int offset) {
			if(src == null || src.Position != DataPosition) { offset = -1; return null; }

			long o;
			var b = src.GetData(DataLength, out o);
			offset = (int)o;

			src = null;

			return b;
		}
		public void Skip() {
			if(src == null) return;
			src.Position = DataPosition + DataLength;
			src = null;
		}
	}
	internal enum PageFlags { None = 0, SpanBefore = 1, Header = 2, Footer = 4, SpanAfter = 1 << 31 }
}
