using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamProvider;
using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace AVDump3Lib.Processing.StreamConsumer;

public interface IStreamConsumerCollection {
	IStreamConsumerFactory StreamConsumerFactory { get; }
	IStreamProvider StreamProvider { get; }

	event EventHandler<ConsumingStreamEventArgs> ConsumingStream;

	void ConsumeStreams(IBytesReadProgress progress, CancellationToken ct);
}

public interface IBytesReadProgress : IProgress<BlockStreamProgress> {
	void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer);
	void Skip(ProvidedStream providedStream, long length);
}


public class StreamConsumerCollection : IStreamConsumerCollection {
	public IStreamConsumerFactory StreamConsumerFactory { get; }
	public IStreamProvider StreamProvider { get; }
	public event EventHandler<ConsumingStreamEventArgs> ConsumingStream;

	private readonly object isRunningSyncRoot = new();


	public StreamConsumerCollection(IStreamConsumerFactory streamConsumerFactory, IStreamProvider streamProvider) {
		this.StreamConsumerFactory = streamConsumerFactory;
		this.StreamProvider = streamProvider;
	}

	public bool IsRunning { get; private set; }


	public void ConsumeStreams(IBytesReadProgress progress, CancellationToken ct) {
		lock(isRunningSyncRoot) {
			if(IsRunning) throw new InvalidOperationException();
			IsRunning = true;
		}

		using(var cts = new CancellationTokenSource())
		using(var counter = new CountdownEvent(1))
		using(ct.Register(() => cts.Cancel())) {
			AggregateException? firstChanceException = null;

			try {
				foreach(var providedStream in StreamProvider.GetConsumingEnumerable(ct)) {
					ct.ThrowIfCancellationRequested();

					counter.AddCount();
					Task.Factory.StartNew(() => {
						ConsumeStream(providedStream, progress, cts.Token);
					}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ContinueWith(t => {
						if(t.IsFaulted || t.IsCanceled) {
							if(firstChanceException == null) firstChanceException = t.Exception?.Flatten();
							cts.Cancel();
						}
						counter.Signal();
					}, TaskScheduler.Current);
					if(firstChanceException != null) break;
				}
			} catch(OperationCanceledException) { }


			counter.Signal();
			counter.Wait(CancellationToken.None);

			if(firstChanceException != null) {
				firstChanceException.Handle(ex => ex is OperationCanceledException);
			}
		}

		lock(isRunningSyncRoot) IsRunning = false;
	}
	private void ConsumeStream(ProvidedStream providedStream, IBytesReadProgress? progress, CancellationToken ct) {
		var tcs = new TaskCompletionSource<ImmutableArray<IBlockConsumer>>();
		var eventArgs = new ConsumingStreamEventArgs(providedStream.Tag, tcs.Task, ct);
		ConsumingStream?.Invoke(this, eventArgs);

		//Thread.CurrentThread.Priority = ThreadPriority.Highest;

		bool retry;
		var retryCount = 0;
		IStreamConsumer streamConsumer = null;
		try {
			do {
				retry = false;
				providedStream.Stream.Position = 0;
				streamConsumer = StreamConsumerFactory.Create(providedStream);
				try {

					if(streamConsumer != null) {
						progress?.Register(providedStream, streamConsumer);
						streamConsumer.ConsumeStream(progress, ct);
					} else {
						progress?.Skip(providedStream, providedStream.Stream.Length);
					}


				} catch(OperationCanceledException ex) {
					throw;

				} catch(StreamConsumerException ex) {
					ex.Data.Add("StreamTag", new SensitiveData(providedStream.Tag));

					var e = new StreamConsumerExceptionEventArgs(ex, retryCount++);
					eventArgs.RaiseOnException(this, e);
					if(!e.IsHandled) {
						throw new StreamConsumerCollectionException("Refused to handle StreamConsumerException", ex);
					} else {
						retry = e.Retry;
					}

				} catch(Exception ex) {
					throw new StreamConsumerCollectionException("Unhandled exception in StreamConsumerCollectionException", ex);
				}
			} while(retry);
			providedStream.Dispose();

			try {
				tcs.SetResult(streamConsumer?.BlockConsumers ?? ImmutableArray<IBlockConsumer>.Empty);
			} catch(Exception ex) {
				throw new StreamConsumerCollectionException("After stream processing exception", ex);
			}

			eventArgs.ResumeNext.Wait(ct);

		} catch(OperationCanceledException ex) {
			throw;

		} catch(Exception ex) {
			throw;
			//TODO
		} finally {
			var blockConsumers = streamConsumer?.BlockConsumers ?? ImmutableArray<IBlockConsumer>.Empty;
			foreach(var blockConsumer in blockConsumers) blockConsumer.Dispose();
		}
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

	public event EventHandler<StreamConsumerExceptionEventArgs> OnException = delegate { };
	public Task<ImmutableArray<IBlockConsumer>> FinishedProcessing { get; private set; }
	public CancellationToken CT { get; }
	public ManualResetEventSlim ResumeNext { get; } = new ManualResetEventSlim(false);

	internal void RaiseOnException(object sender, StreamConsumerExceptionEventArgs ex) {
		OnException?.Invoke(sender, ex);
	}

	public ConsumingStreamEventArgs(object tag, Task<ImmutableArray<IBlockConsumer>> finishedProcessing, CancellationToken ct) {
		FinishedProcessing = finishedProcessing;
		Tag = tag;
		CT = ct;
	}
}

public class StreamConsumerCollectionException : AVD3LibException {
	public StreamConsumerCollectionException(string message) : base("FileConsumerCollection threw an Exception: " + message) { }
	public StreamConsumerCollectionException(string message, Exception innerException) : base("FileConsumerCollection threw an Exception: " + message, innerException) { }

	public override void GetObjectData(SerializationInfo info, StreamingContext context) { base.GetObjectData(info, context); }
	//public override System.Xml.Linq.XElement ToXElement() { var exRoot = base.ToXElement(); return exRoot; }
}
