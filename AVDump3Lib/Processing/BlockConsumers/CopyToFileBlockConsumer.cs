using AVDump3Lib.Processing.BlockBuffers;

namespace AVDump3Lib.Processing.BlockConsumers;

public class CopyToFileBlockConsumer : BlockConsumer {
	public string FilePath { get; }

	public CopyToFileBlockConsumer(string name, IBlockStreamReader reader, string filePath) : base(name, reader) {
		FilePath = filePath;
	}

	protected override void DoWork(CancellationToken ct) {
		using var fileStream = File.OpenWrite(FilePath);
		ReadOnlySpan<byte> block;
		do {
			ct.ThrowIfCancellationRequested();
			block = Reader.GetBlock(Reader.SuggestedReadLength);
			fileStream.Write(block);
		} while(Reader.Advance(block.Length));
	}
}

