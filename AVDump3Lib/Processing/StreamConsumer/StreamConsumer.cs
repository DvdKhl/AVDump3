using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.StreamConsumer {
	public delegate void StreamConsumerEventHandler(IStreamConsumer streamConsumer);
	public interface IStreamConsumer {
		event StreamConsumerEventHandler Finished;
		bool RanToCompletion { get; }

		IReadOnlyList<IBlockConsumer> BlockConsumers { get; }
		IBlockStream BlockStream { get; }
		void ConsumeStream(IProgress<BlockStreamProgress> progress, CancellationToken ct);
	}



	public class StreamConsumer : IStreamConsumer {
		private IBlockConsumer[] blockConsumers;

		public event StreamConsumerEventHandler Finished;

		public IBlockStream BlockStream { get; }
		public IReadOnlyList<IBlockConsumer> BlockConsumers { get; }

		public bool RanToCompletion { get; private set; }

		public StreamConsumer(IBlockStream blockStream, IEnumerable<IBlockConsumer> blockConsumers) {
			this.blockConsumers = blockConsumers.ToArray();
			BlockConsumers = Array.AsReadOnly(this.blockConsumers);

			BlockStream = blockStream;
		}

		public void ConsumeStream(IProgress<BlockStreamProgress> progress, CancellationToken ct) {
			if(blockConsumers.Any()) {
				Task[] tasks = new Task[blockConsumers.Length + 1];
				tasks[tasks.Length - 1] = BlockStream.Produce(progress, ct);

				for(int i = 0; i < blockConsumers.Length; i++) {
					int consumerIndex = i;

					tasks[consumerIndex] = Task.Factory.StartNew(
						() => blockConsumers[consumerIndex].ProcessBlocks(ct),
						ct, TaskCreationOptions.LongRunning, TaskScheduler.Default
					);
				}

				Task.WaitAll(tasks, ct);
			}

			var exceptions = (
				from b in blockConsumers
				where b.Exception != null
				select b.Exception
			).ToArray();

			RanToCompletion = exceptions.Length == 0;
			Finished?.Invoke(this);
			if(exceptions.Length > 0) throw new StreamConsumerException(new AggregateException(exceptions));
		}

	}

	[Serializable]
	public class StreamConsumerException : AVD3LibException {
		public StreamConsumerException(string message, Exception innerException) : base("StreamConsumer threw an Exception: " + message, innerException) { }
		public StreamConsumerException(Exception innerException) : base("StreamConsumer threw an Exception", innerException) { }
		protected StreamConsumerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
		//public override System.Xml.Linq.XElement ToXElement() { var exRoot = base.ToXElement(); return exRoot; }
	}

}
