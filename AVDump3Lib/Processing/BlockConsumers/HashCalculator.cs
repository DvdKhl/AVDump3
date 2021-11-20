using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.HashAlgorithms;
using System.Collections.Immutable;

namespace AVDump3Lib.Processing.BlockConsumers;

public class HashValue {
	public HashValue(string name, ImmutableArray<byte> value) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Value = value;
	}

	public string Name { get; }
	public ImmutableArray<byte> Value { get; }
}

public class HashCalculator : BlockConsumer {
	public int ReadLength { get; }

	public ImmutableArray<byte> HashValue { get; private set; }
	public ImmutableArray<ImmutableArray<byte>> AdditionalHashValues { get; private set; }


	public IAVDHashAlgorithm HashAlgorithm { get; }
	public HashCalculator(string name, IBlockStreamReader reader, IAVDHashAlgorithm transform) : base(name, reader) {
		HashAlgorithm = transform;

		var length = ((reader.SuggestedReadLength / transform.BlockSize) + 1) * transform.BlockSize;
		if(length > reader.MaxReadLength) {
			length -= transform.BlockSize;
			if(length == 0) {
				throw new Exception("Min/Max BlockLength too restrictive") {
					Data = {
							{ "TransformName", Name },
							{ "MaxBlockLength", reader.MaxReadLength },
							{ "HashBlockLength", transform.BlockSize }
						}
				};
			}
		}
		ReadLength = length;
	}

	protected override void DoWork(CancellationToken ct) {
		HashAlgorithm.Initialize();

		ReadOnlySpan<byte> block;
		int bytesProcessed;
		do {
			ct.ThrowIfCancellationRequested();

			block = Reader.GetBlock(ReadLength);
			bytesProcessed = HashAlgorithm.TransformFullBlocks(block);
		} while(Reader.Advance(bytesProcessed) && bytesProcessed != 0);

		var lastBytes = block.Length - bytesProcessed;

		HashValue = HashAlgorithm.TransformFinalBlock(block.Slice(bytesProcessed, lastBytes)).ToArray().ToImmutableArray();
		AdditionalHashValues = HashAlgorithm.AdditionalHashes;



		Reader.Advance(lastBytes);
	}

	public override void Dispose() {
		HashAlgorithm.Dispose();
		base.Dispose();
		GC.SuppressFinalize(this);

	}
}
