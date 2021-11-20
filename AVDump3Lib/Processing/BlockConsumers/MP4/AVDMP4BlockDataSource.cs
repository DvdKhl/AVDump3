using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib.DocTypes.MP4;

namespace AVDump3Lib.Processing.BlockConsumers.MP4;

public class AVDMP4BlockDataSource : MP4BlockDataSource {
	private readonly IBlockStreamReader reader;
	private int offset;

	public AVDMP4BlockDataSource(IBlockStreamReader reader) => this.reader = reader;

	public override long Position {
		get => reader.BytesRead + offset;
		set => Advance(checked((int)(value - reader.BytesRead)));
	}

	public override void CommitPosition() {
		reader.Advance(offset);
		offset = 0;
	}

	protected override void Advance(int length) {
		offset += length;
		if(offset >= reader.SuggestedReadLength) CommitPosition();
		IsEndOfStream = reader.Length == reader.BytesRead + offset;
	}

	protected override ReadOnlySpan<byte> GetDataBlock(int minLength) => reader.GetBlock(minLength)[offset..];
}
