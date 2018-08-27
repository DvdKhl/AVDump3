namespace AVDump3Lib.Processing.BlockBuffers.Sources {
    public interface IBlockSource {
        int Read(byte[] block);
		long Length { get; }
	}
}
