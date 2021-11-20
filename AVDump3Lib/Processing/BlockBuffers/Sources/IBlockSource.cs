namespace AVDump3Lib.Processing.BlockBuffers.Sources;

public interface IBlockSource {
	int Read(Span<byte> block);
	long Length { get; }
}
