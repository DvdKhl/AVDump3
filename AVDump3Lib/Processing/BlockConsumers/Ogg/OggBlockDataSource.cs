using AVDump3Lib.Processing.BlockBuffers;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg;

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
	private readonly IBlockStreamReader reader;


	public OggBlockDataSource(IBlockStreamReader reader) {
		this.reader = reader;

	}

	public long Length => reader.Length;

	private static readonly ReadOnlyMemory<byte> OggS = new(new[] { (byte)'O', (byte)'g', (byte)'g', (byte)'S' });
	public bool SeekPastSyncBytes(bool advanceReader, int maxSkippableBytes = 1 << 20) {
		var bytesSkipped = 0;
		var magicBytes = OggS.Span;
		while(true) {
			var block = reader.GetBlock(reader.SuggestedReadLength);
			var offset = block.IndexOf(magicBytes);
			if(offset != -1 && offset <= maxSkippableBytes) {
				if(advanceReader) reader.Advance(offset + 4);
				return true;
			}
			bytesSkipped += offset;

			if(bytesSkipped > maxSkippableBytes || block.Length < 4 || !reader.Advance(block.Length - 3)) break;
		}
		return false;
	}

	public bool ReadOggPage(ref OggPage page) {
		if(!SeekPastSyncBytes(true)) return false;

		var block = reader.GetBlock(23 + 256 * 256);

		//page.FilePosition = Position;
		page.Version = block[0];
		page.Flags = (PageFlags)block[1];
		page.GranulePosition = block.Slice(2, 8);
		page.StreamId = MemoryMarshal.Read<uint>(block[10..]);
		page.PageIndex = MemoryMarshal.Read<uint>(block[14..]);
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
		page.Data = block.Slice(23 + offset, Math.Min(dataLength, block.Length - 23 - offset));

		return true;
	}
}
