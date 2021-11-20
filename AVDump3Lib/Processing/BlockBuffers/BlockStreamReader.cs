using System;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Processing.BlockBuffers;

public interface IBlockStreamReader {
	bool Advance(int length);
	ReadOnlySpan<byte> GetBlock(int minBlockLength);
	long Length { get; }
	long BytesRead { get; }
	bool Completed { get; }
	int SuggestedReadLength { get; }
	int MaxReadLength { get; }

	void Complete();
}

public class BlockStreamReader : IBlockStreamReader {
	private readonly int readerIndex;
	private readonly IBlockStream blockStream;

	public long BytesRead { get; private set; }

	public long Length => blockStream.Length;

	public bool Completed { get; private set; }

	public int SuggestedReadLength { get; }
	public int MaxReadLength { get; }

	public BlockStreamReader(IBlockStream blockStream, int readerIndex) {
		this.blockStream = blockStream;
		this.readerIndex = readerIndex;

		MaxReadLength = blockStream.BufferLength / 2;
		SuggestedReadLength = MaxReadLength / 2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ReadOnlySpan<byte> GetBlock(int minBlockLength) => blockStream.GetBlock(readerIndex, minBlockLength);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Advance(int length) {
		BytesRead += length;
		return blockStream.Advance(readerIndex, length);
	}

	public void Complete() {
		blockStream.CompleteConsumption(readerIndex);
		Completed = true;
	}
}
