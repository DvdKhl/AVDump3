namespace AVDump3Lib.BlockBuffers.Sources {
    public interface IBlockSource {
        int Read(byte[] block);
		long Length { get; }
	}
}
