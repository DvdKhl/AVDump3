﻿using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockConsumers;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.StreamConsumer {
	public interface IStreamConsumerCollection {
	}

	public interface  IBytesReadProgress : IProgress<BlockStreamProgress> {
		void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer);
	}


	public class StreamConsumerCollection : IStreamConsumerCollection {
		public event EventHandler<ConsumingStreamEventArgs> ConsumingStream;

		private IStreamConsumerFactory streamConsumerFactory;
		private IStreamProvider streamProvider;
		private object isRunningSyncRoot = new object();


		public StreamConsumerCollection(IStreamConsumerFactory streamConsumerFactory, IStreamProvider streamProvider) {
			this.streamConsumerFactory = streamConsumerFactory;
			this.streamProvider = streamProvider;
		}

		public bool IsRunning { get; private set; }

		public void ConsumeStreams(CancellationToken ct, IBytesReadProgress progress) {
			lock(isRunningSyncRoot) {
				if(IsRunning) throw new InvalidOperationException();
				IsRunning = true;
			}

			using(var cts = new CancellationTokenSource())
			using(var counter = new CountdownEvent(1))
			using(ct.Register(() => cts.Cancel())) {
				Exception firstChanceException = null;
				foreach(var providedStream in streamProvider.GetConsumingEnumerable(ct)) {
					ct.ThrowIfCancellationRequested();

					counter.AddCount();
					Task.Factory.StartNew(() => {
						ConsumeStream(providedStream, progress, cts.Token);
					}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ContinueWith(t => {
						providedStream.Dispose();
						counter.Signal();
						if(t.IsFaulted) {
							if(firstChanceException == null) firstChanceException = t.Exception.Flatten();
							cts.Cancel();
						}
					});
					if(firstChanceException != null) break;
				}

				counter.Signal();
				counter.Wait(ct);

				if(firstChanceException != null) {
					throw firstChanceException;
				}
			}

			lock(isRunningSyncRoot) IsRunning = false;
		}
		private void ConsumeStream(ProvidedStream providedStream, IBytesReadProgress progress, CancellationToken ct) {
			var tcs = new TaskCompletionSource<IReadOnlyCollection<IBlockConsumer>>();
			var eventArgs = new ConsumingStreamEventArgs(providedStream.Tag, tcs.Task);
			ConsumingStream?.Invoke(this, eventArgs);

			//Thread.CurrentThread.Priority = ThreadPriority.Highest;

			var retry = false;
			int retryCount = 0;
			do {
				try {
					var streamConsumer = streamConsumerFactory.Create(providedStream.Stream);
					progress?.Register(providedStream, streamConsumer);

					streamConsumer.ConsumeStream(progress, ct);
					tcs.SetResult(streamConsumer.BlockConsumers);

				} catch(StreamConsumerException ex) {
					var e = new StreamConsumerExceptionEventArgs(ex, retryCount++);
					eventArgs.RaiseOnException(this, e);
					if(!e.IsHandled) {
						throw new StreamConsumerCollectionException("Unhandled exception in StreamConsumerException", ex);
					} else {
						retry = e.Retry;
					}

				} catch(Exception ex) {
					throw new StreamConsumerCollectionException("Unhandled exception in StreamConsumerCollectionException", ex);
				}
			} while(retry);
		}
	}

	public class StreamConsumerExceptionEventArgs : EventArgs {
		public bool IsHandled { get; set; }
		public bool Retry { get; set; }
		public int RetryCount { get; private set; }
		public Exception Cause { get; private set; }

		public StreamConsumerExceptionEventArgs(Exception cause, int retryCount) { Cause = cause; RetryCount = retryCount; }
	}

	public class ConsumingStreamEventArgs : EventArgs {
		public object Tag { get; private set; }

		public event EventHandler<StreamConsumerExceptionEventArgs> OnException;
		public Task<IReadOnlyCollection<IBlockConsumer>> FinishedProcessing { get; private set; }

		internal void RaiseOnException(object sender, StreamConsumerExceptionEventArgs ex) {

			OnException?.Invoke(sender, ex);
		}

		public ConsumingStreamEventArgs(object tag, Task<IReadOnlyCollection<IBlockConsumer>> finishedProcessing) {
			Tag = tag;
			FinishedProcessing = finishedProcessing;
		}
	}

	public class StreamConsumerCollectionException : AVD3LibException {
		public StreamConsumerCollectionException(string message) : base("FileConsumerCollection threw an Exception: " + message) { }
		public StreamConsumerCollectionException(string message, Exception innerException) : base("FileConsumerCollection threw an Exception: " + message, innerException) { }
		protected StreamConsumerCollectionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
		//public override System.Xml.Linq.XElement ToXElement() { var exRoot = base.ToXElement(); return exRoot; }
	}

}
