using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using System.Collections.Immutable;

namespace AVDump3Lib.Processing.StreamConsumer;

public delegate void StreamConsumerEventHandler(IStreamConsumer streamConsumer);
public interface IStreamConsumer {
	event StreamConsumerEventHandler Finished;
	bool RanToCompletion { get; }
	Guid Id { get; }
	ImmutableArray<IBlockConsumer> BlockConsumers { get; }
	IBlockStream BlockStream { get; }
	void ConsumeStream(IProgress<BlockStreamProgress>? progress, CancellationToken ct);
}



public class StreamConsumer : IStreamConsumer {
	private readonly IBlockConsumer[] blockConsumers;

	public event StreamConsumerEventHandler? Finished;

	public Guid Id { get; } = Guid.NewGuid();

	public IBlockStream BlockStream { get; }
	public ImmutableArray<IBlockConsumer> BlockConsumers { get; }

	public bool RanToCompletion { get; private set; }

	public StreamConsumer(IBlockStream blockStream, IEnumerable<IBlockConsumer> blockConsumers) {
		this.blockConsumers = blockConsumers.ToArray();
		BlockConsumers = this.blockConsumers.ToImmutableArray();

		BlockStream = blockStream;
	}

	public void ConsumeStream(IProgress<BlockStreamProgress>? progress, CancellationToken ct) {
		if(blockConsumers.Any()) {
			var tasks = new Task[blockConsumers.Length + 1];
			tasks[^1] = BlockStream.Produce(progress, ct);

			for(var i = 0; i < blockConsumers.Length; i++) {
				var consumerIndex = i;

				tasks[consumerIndex] = Task.Factory.StartNew(
					() => blockConsumers[consumerIndex].ProcessBlocks(ct),
					ct, TaskCreationOptions.LongRunning, TaskScheduler.Default
				);
			}

			try {
				Task.WaitAll(tasks, CancellationToken.None);
			} catch(AggregateException ex) {
				var wasCancelled = false;
				ex.Flatten().Handle(ex => {
					wasCancelled |= ex is OperationCanceledException;
					return ex is OperationCanceledException;
				});
				if(wasCancelled) {
					throw new OperationCanceledException("ConsumeStream operation was cancelled", ex, ct);
				}
			}
		}

		//foreach(var blockConsumer in blockConsumers) blockConsumer.Dispose();


		var exceptions = blockConsumers
			.Where(x => x.Exception != null)
			.Select(x => x.Exception)
			.ToArray();

		if(exceptions.Any(x => x is OperationCanceledException)) {
			throw new OperationCanceledException("ConsumeStream operation was cancelled", new AggregateException(exceptions), ct);
		}


		RanToCompletion = exceptions.Length == 0;
		Finished?.Invoke(this);
		if(exceptions.Length > 0) throw new StreamConsumerException(new AggregateException(exceptions));
	}

}

//[Serializable]
public class StreamConsumerException : AVD3LibException {
	public StreamConsumerException(string message, Exception innerException) : base(message, innerException) { }
	public StreamConsumerException(Exception innerException) : base("StreamConsumer threw an Exception", innerException) { }



	//protected StreamConsumerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	//public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
	//public override System.Xml.Linq.XElement ToXElement() { var exRoot = base.ToXElement(); return exRoot; }
}
