using AVDump3Lib.Processing.BlockBuffers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.BlockBuffers {
	public interface IBlockStream {
		void CompleteConsumption(int consumerIndex);
		bool Advance(int consumerIndex, int length);
        ReadOnlySpan<byte> GetBlock(int consumerIndex, int minBlockLength);

		Task Produce(IProgress<BlockStreamProgress> progress, CancellationToken ct);
		long Length { get; }
        int BufferLength { get; }
		int BufferUnderrunCount { get; }
		int BufferOverrunCount { get; }
	}

	public struct BlockStreamProgress {
		public IBlockStream Sender { get; }
		public int Index { get; }
		public int BytesRead { get; }
		public BlockStreamProgress(IBlockStream sender, int index, int bytesRead) {
			Sender = sender;
			Index = index;
			BytesRead = bytesRead;
		}
	}

	public class BlockStream : IBlockStream {
		private bool hasStarted;
		private readonly IBlockSource blockSource;
		public ICircularBuffer Buffer { get; }

		private CancellationToken ct;
		private IProgress<BlockStreamProgress> progress;

		public int BufferUnderrunCount { get; private set; }
		public int BufferOverrunCount { get; private set; }
        public int BufferLength => Buffer.Length;

        public BlockStream(IBlockSource blockSource, ICircularBuffer buffer) {
			this.blockSource = blockSource ?? throw new ArgumentNullException(nameof(blockSource));

			Buffer = buffer;
		}

        public Task Produce(IProgress<BlockStreamProgress> progress, CancellationToken ct) {
			if(hasStarted) throw new InvalidOperationException("Has already started once");
			hasStarted = true;

			this.ct = ct;
			this.progress = progress;
			return Task.Factory.StartNew(Produce, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}
		public long Length => blockSource.Length;

		private object consumerLock = new object(), producerLock = new object();
		private void Produce() {
            //Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            while(!Buffer.IsProducionCompleted) {
                Span<byte> writerSpan;
                if((writerSpan = Buffer.ProducerBlock()).Length < 0) { //TODO
                    lock(producerLock) {
                        while((writerSpan = Buffer.ProducerBlock()).Length < 0) { //TODO
                            Monitor.Wait(producerLock, 1000);
                            ct.ThrowIfCancellationRequested();
                            BufferOverrunCount++;
                        }
                    }
                }

                var readBytes = blockSource.Read(writerSpan);
                progress?.Report(new BlockStreamProgress(this, -1, readBytes));

                if(readBytes != writerSpan.Length) Buffer.CompleteProduction();
                Buffer.ProducerAdvance(readBytes);
                lock(consumerLock) Monitor.PulseAll(consumerLock);
            }
		}


		public void CompleteConsumption(int consumerIndex) { Buffer.CompleteConsumption(consumerIndex); }

		public ReadOnlySpan<byte> GetBlock(int consumerIndex, int minBlockLength) {
            ReadOnlySpan<byte> readerSpan;

            if((readerSpan = Buffer.ConsumerBlock(consumerIndex)).Length < minBlockLength) {
				if(Buffer.ConsumerCompleted(consumerIndex)) throw new InvalidOperationException("Cannot read block when EOS is reached");
				lock (consumerLock) {
					while((readerSpan = Buffer.ConsumerBlock(consumerIndex)).Length < minBlockLength) {
                        if(Buffer.IsProducionCompleted) {
                            //We have to get the span again since the producer could have loaded the last data between the previous two statements
                            readerSpan = Buffer.ConsumerBlock(consumerIndex); 
                            break;
                        }

						Monitor.Wait(consumerLock, 1000);
						ct.ThrowIfCancellationRequested();
						BufferUnderrunCount++;
					}
				}
			}
            return readerSpan;
		}

		public bool Advance(int consumerIndex, int length) {
            Buffer.ConsumerAdvance(consumerIndex, length);
			progress?.Report(new BlockStreamProgress(this, consumerIndex, length));

			lock (producerLock) Monitor.Pulse(producerLock);
			return !Buffer.ConsumerCompleted(consumerIndex);
		}
	}

}
