using AVDump3Lib.Processing.BlockBuffers;

namespace AVDump3Lib.Processing.BlockConsumers;

public interface IBlockConsumer : IDisposable {
	string Name { get; }
	Exception Exception { get; }
	void ProcessBlocks(CancellationToken ct);
	bool IsConsuming { get; }
}


public abstract class BlockConsumer : IBlockConsumer {
	protected IBlockStreamReader Reader { get; }

	public Exception Exception { get; private set; }

	public string Name { get; } //TODO

	public virtual bool IsConsuming => !Reader.Completed;

	public BlockConsumer(string name, IBlockStreamReader reader) {
		Name = name;
		Reader = reader;
	}

	public void ProcessBlocks(CancellationToken ct) {
		try {
			DoWork(ct);
		} catch(Exception ex) {
			ex.Data.Add("BlockConsumerName", Name);
			ex.Data.Add("BlockConsumerReadBytes", Reader.BytesRead);
			Exception = ex;

		} finally {
			Reader.Complete();
		}
	}

	protected abstract void DoWork(CancellationToken ct);

	public virtual void Dispose() {
		GC.SuppressFinalize(this);
	}
}
