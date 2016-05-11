using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockConsumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.StreamConsumer {
    public interface IStreamConsumer {
		event EventHandler Finished;
        IReadOnlyCollection<IBlockConsumer> BlockConsumers { get; }
        void ConsumeStream(IProgress<int> progress, CancellationToken ct);
    }
    public class StreamConsumer : IStreamConsumer {
        private IBlockStream blockStream;
        private IBlockConsumer[] blockConsumers;

		public event EventHandler Finished;

		public IReadOnlyCollection<IBlockConsumer> BlockConsumers { get; }

        public StreamConsumer(IBlockStream blockStream, IEnumerable<IBlockConsumer> blockConsumers) {
            this.blockConsumers = blockConsumers.ToArray();
            BlockConsumers = Array.AsReadOnly(this.blockConsumers);

            this.blockStream = blockStream;
        }

        public void ConsumeStream(IProgress<int> progress, CancellationToken ct) {
            if(blockConsumers.Any()) {
                Task[] tasks = new Task[blockConsumers.Length + 1];
                tasks[tasks.Length - 1] = blockStream.Produce(progress, ct);

                for(int i = 0; i < blockConsumers.Length; i++) {
                    int consumerIndex = i;

                    tasks[consumerIndex] = Task.Factory.StartNew(
                        () => blockConsumers[consumerIndex].ProcessBlocks(ct),
                        ct, TaskCreationOptions.LongRunning, TaskScheduler.Default
                    );
                }

                Task.WaitAll(tasks, ct);
            }
			Finished?.Invoke(this, EventArgs.Empty);


			if(blockConsumers.Any(ldBlockConsumer => ldBlockConsumer.Exception != null)) {
                throw new AggregateException(from b in blockConsumers where b.Exception != null select b.Exception);
            }
        }

    }


    public class StreamConsumerException : AVD3LibException {
        public StreamConsumerException(string message, Exception innerException) : base("StreamConsumer threw an Exception: " + message, innerException) { }
        public StreamConsumerException(Exception innerException) : base("StreamConsumer threw an Exception", innerException) { }
        protected StreamConsumerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
        //public override System.Xml.Linq.XElement ToXElement() { var exRoot = base.ToXElement(); return exRoot; }
    }

}
