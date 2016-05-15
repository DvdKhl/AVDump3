using System;
using System.Threading;
using System.Threading.Tasks;
using AVDump3Lib.BlockBuffers.Sources;

namespace AVDump3Lib.BlockBuffers {
	public interface IBlockStream {
		void DropOut(int consumerIndex);
		bool Advance(int consumerIndex);
		byte[] GetBlock(int consumerIndex, out int readBytes);
		CircularBlockBuffer Buffer { get; }
		Task Produce(IProgress<BlockStreamProgress> progress, CancellationToken ct);
		long Length { get; }
	}

	public struct BlockStreamProgress {
		public IBlockStream Sender { get;  }
		public int Index { get;  }
		public int BytesRead { get;  }
		public BlockStreamProgress(IBlockStream sender, int index, int bytesRead) {
			Sender = sender;
			Index = index;
			BytesRead = bytesRead;
		}
	}

	public class BlockStream : IBlockStream {
		private bool hasStarted;
		private bool isEndOfStream;
		private readonly IBlockSource blockSource;
		public CircularBlockBuffer Buffer { get; }

		private CancellationToken ct;
		private IProgress<BlockStreamProgress> progress;


		public BlockStream(IBlockSource blockSource, CircularBlockBuffer buffer) {
			if(blockSource == null) throw new ArgumentNullException(nameof(blockSource));
			this.blockSource = blockSource;

			Buffer = buffer;
		}

		private bool IsEndOfStream(int consumerIndex) {
			return !Buffer.ConsumerCanRead(consumerIndex) && isEndOfStream;
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
			while(!isEndOfStream) {
				lock(producerLock) {
					while(!Buffer.ProducerCanWrite()) {
						Monitor.Wait(producerLock, 1000);
						ct.ThrowIfCancellationRequested();
					}
				}

				var readBytes = blockSource.Read(Buffer.ProducerBlock());
				progress?.Report(new BlockStreamProgress(this, -1, readBytes));

				isEndOfStream = readBytes != Buffer.BlockLength;
				Buffer.ProducerAdvance();
				lock(consumerLock) Monitor.PulseAll(consumerLock);
			}
		}


		public void DropOut(int consumerIndex) { Buffer.ConsumerDropOut(consumerIndex); }

		public byte[] GetBlock(int consumerIndex, out int readBytes) {
			if(!Buffer.ConsumerCanRead(consumerIndex)) {
				if(IsEndOfStream(consumerIndex)) throw new InvalidOperationException("Cannot read block when EOS is reached");
				lock(consumerLock) {
					while(!Buffer.ConsumerCanRead(consumerIndex)) {
						Monitor.Wait(consumerLock, 1000);
						ct.ThrowIfCancellationRequested();
					}
				}
			}

			readBytes = (int)Math.Min(Buffer.BlockLength, Length - Buffer.ConsumerBlocksRead(consumerIndex) * Buffer.BlockLength);

			return Buffer.ConsumerBlock(consumerIndex);
		}

		public bool Advance(int consumerIndex) {
			//TODO move out param from GetBlock to this method to avoid calculating readBytes twice
			var readBytes = (int)Math.Min(Buffer.BlockLength, Length - Buffer.ConsumerBlocksRead(consumerIndex) * Buffer.BlockLength);
			progress?.Report(new BlockStreamProgress(this, consumerIndex, readBytes));

			Buffer.ConsumerAdvance(consumerIndex);
			lock(producerLock) Monitor.Pulse(producerLock);
			return !IsEndOfStream(consumerIndex);

		}
	}

}
