using System;
using System.Threading;
using System.Threading.Tasks;
using AVDump3Lib.BlockBuffers.Sources;

namespace AVDump3Lib.BlockBuffers {
	public interface IBlockStream {
		void DropOut(int consumerIndex);
		bool Advance(int consumerIndex);
		byte[] GetBlock(int consumerIndex);
		Task Produce(IProgress<int> progress, CancellationToken ct);
		long Length { get; }
	}

	public class BlockStream : IBlockStream {
		private bool hasStarted;
		private bool isEndOfStream;
		private readonly IBlockSource blockSource;
		private readonly CircularBlockBuffer buffer;

		private CancellationToken ct;
		private IProgress<int> progress;


		public BlockStream(IBlockSource blockSource, CircularBlockBuffer buffer) {
			if(blockSource == null) throw new ArgumentNullException(nameof(blockSource));
			this.blockSource = blockSource;

			this.buffer = buffer;
		}

		private bool IsEndOfStream(int consumerIndex) {
			return !buffer.ConsumerCanRead(consumerIndex) && isEndOfStream;
		}

		public Task Produce(IProgress<int> progress, CancellationToken ct) {
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
					while(!buffer.ProducerCanWrite()) {
						Monitor.Wait(producerLock, 1000);
						ct.ThrowIfCancellationRequested();
					}
				}

				var bytesread = blockSource.Read(buffer.ProducerBlock());
				progress?.Report(bytesread);

				isEndOfStream = bytesread != buffer.BlockLength;
				buffer.ProducerAdvance();
				lock(consumerLock) Monitor.PulseAll(consumerLock);
			}
		}


		public void DropOut(int consumerIndex) { buffer.ConsumerDropOut(consumerIndex); }

		public byte[] GetBlock(int consumerIndex) {
			if(!buffer.ConsumerCanRead(consumerIndex)) {
				if(IsEndOfStream(consumerIndex)) throw new InvalidOperationException("Cannot read block when EOS is reached");
				lock(consumerLock) {
					while(!buffer.ConsumerCanRead(consumerIndex)) {
						Monitor.Wait(consumerLock, 1000);
						ct.ThrowIfCancellationRequested();
					}
				}
			}

			return buffer.ConsumerBlock(consumerIndex);
		}

		public bool Advance(int consumerIndex) {
			buffer.ConsumerAdvance(consumerIndex);
			lock(producerLock) Monitor.Pulse(producerLock);
			return !IsEndOfStream(consumerIndex);
		}
	}

}
