using System;

namespace AVDump3Lib.Processing.HashAlgorithms;

public sealed class NullHashAlgorithm : AVDHashAlgorithm {
	public override int BlockSize { get; }

	protected override void InitializeInternal() { }
	public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) => ReadOnlySpan<byte>.Empty;
	protected override unsafe void HashCore(in ReadOnlySpan<byte> data) { }

	public NullHashAlgorithm(int blockSize) => BlockSize = blockSize;
}
