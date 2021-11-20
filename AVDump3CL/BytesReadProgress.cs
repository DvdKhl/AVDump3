using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AVDump3CL {
	public class BytesReadProgress : IBytesReadProgress {
		private int filesProcessed;
		private readonly int[] bcFilesProcessed;
		private DateTimeOffset startedOn;
		private long bytesProcessed;
		private readonly long[] bcBytesProcessed;
		private readonly Dictionary<string, int> bcNameIndexMap;
		private readonly ConcurrentDictionary<IBlockStream, StreamConsumerProgressInfo> blockStreamProgress;

		private class StreamConsumerProgressInfo {
			public DateTimeOffset StartedOn { get; } = DateTimeOffset.UtcNow.AddMilliseconds(-1);
			public string Filename { get; }
			public IStreamConsumer StreamConsumer { get; }
			public long[] BytesRead { get; }
			public long Length { get; }

			public StreamConsumerProgressInfo(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
				Filename = (string)providedStream.Tag;
				StreamConsumer = streamConsumer;
				BytesRead = new long[streamConsumer.BlockConsumers.Length + 1];
				Length = providedStream.Stream.Length;
			}
		}

		public class BlockConsumerProgress {
			public string Name { get; internal set; } = "";
			public int FilesProcessed { get; internal set; }
			public long BytesProcessed { get; internal set; }
			public double BufferFill { get; internal set; }
			public int ActiveCount { get; internal set; }
		}
		public class FileProgress {
			public Guid Id { get; }
			public string FilePath { get; private set; }
			public DateTimeOffset StartedOn { get; private set; }
			public long FileLength { get; private set; }
			public long BytesProcessed { get; private set; }
			public int ReaderLockCount { get; private set; }
			public int WriterLockCount { get; private set; }
			public IReadOnlyList<KeyValuePair<string, long>> BytesProcessedPerBlockConsumer { get; private set; }

			public FileProgress(Guid id, string filePath, DateTimeOffset startedOn, long fileLength, long bytesProcessed,
				int readerLockCount, int writerLockCount,
				IReadOnlyList<KeyValuePair<string, long>> bytesProcessedPerBlockConsumer
			) {
				Id = id;
				FilePath = filePath;
				StartedOn = startedOn;
				FileLength = fileLength;
				BytesProcessed = bytesProcessed;
				ReaderLockCount = readerLockCount;
				WriterLockCount = writerLockCount;
				BytesProcessedPerBlockConsumer = bytesProcessedPerBlockConsumer;
			}
		}
		public class Progress {
			public DateTimeOffset StartedOn { get; private set; }
			public int FilesProcessed { get; private set; }
			public long BytesProcessed { get; private set; }
			public IReadOnlyList<FileProgress> FileProgressCollection { get; private set; }
			public IReadOnlyList<BlockConsumerProgress> BlockConsumerProgressCollection { get; private set; }

			public Progress(DateTimeOffset startedOn, int filesProcessed, long bytesProcessed,
				IReadOnlyList<FileProgress> fileProgressCollection,
				IReadOnlyList<BlockConsumerProgress> blockConsumerProgressCollection
			) {
				StartedOn = startedOn;
				FilesProcessed = filesProcessed;
				BytesProcessed = bytesProcessed;
				FileProgressCollection = fileProgressCollection;
				BlockConsumerProgressCollection = blockConsumerProgressCollection;
			}
			public Progress() {
				FileProgressCollection = new List<FileProgress>().AsReadOnly();
				BlockConsumerProgressCollection = new List<BlockConsumerProgress>().AsReadOnly();
			}
		}

		public BytesReadProgress(IEnumerable<string> blockConsumerNames) {
			blockStreamProgress = new ConcurrentDictionary<IBlockStream, StreamConsumerProgressInfo>();

			bcNameIndexMap = new Dictionary<string, int>();
			foreach(var blockConsumerName in blockConsumerNames) {
				bcNameIndexMap.Add(blockConsumerName, bcNameIndexMap.Count);
			}

			bcFilesProcessed = new int[bcNameIndexMap.Count];
			bcBytesProcessed = new long[bcNameIndexMap.Count];
		}

		public void Report(BlockStreamProgress value) {
			var streamConsumerProgressPair = blockStreamProgress[value.Sender];
			Interlocked.Add(ref streamConsumerProgressPair.BytesRead[value.Index + 1], value.BytesRead);
		}

		public Progress GetProgress() {
			var blockConsumerProgress = new BlockConsumerProgress[bcBytesProcessed.Length];
			foreach(var pair in bcNameIndexMap) {
				blockConsumerProgress[pair.Value] = new BlockConsumerProgress {
					Name = pair.Key,
					BytesProcessed = bcBytesProcessed[pair.Value],
					FilesProcessed = bcFilesProcessed[pair.Value]
				};
			}

			var bytesProcessed = this.bytesProcessed;
			var filesProcessed = this.filesProcessed;

			var fileProgressCollection = new List<FileProgress>(blockStreamProgress.Count);
			foreach(var info in blockStreamProgress.Values.ToArray()) {
				var bufferLength = info.StreamConsumer.BlockStream.BufferLength;
				var bytesRead = (long[])info.BytesRead.Clone();
				var bcfProgress = new KeyValuePair<string, long>[bytesRead.Length - 1];

				var bytesProcessLocal = 0L;
				//bytesProcessed += (long)bytesRead.Skip(1).Where((x, i) => x != 0 && info.StreamConsumer.BlockConsumers[i].IsConsuming).DefaultIfEmpty(0).Average();
				var activeCount = 0;
				for(var i = 0; i < bcfProgress.Length; i++) {
					var blockConsumer = info.StreamConsumer.BlockConsumers[i];

					bcfProgress[i] = new KeyValuePair<string, long>(blockConsumer.Name, bytesRead[i + 1]);

					var index = bcNameIndexMap[blockConsumer.Name];
					var bcProgress = blockConsumerProgress[index];
					if(blockConsumer.IsConsuming) {
						bcProgress.ActiveCount++;
						activeCount++;
						bytesProcessLocal += bytesRead[i + 1];
					}
					bcProgress.BytesProcessed += bytesRead[i + 1];
					bcProgress.BufferFill += (bytesRead[0] - bytesRead[i + 1]) / (double)bufferLength;
				}
				if(activeCount > 0) bytesProcessed += bytesProcessLocal / activeCount; //TODO: Somtimes the bytesProcessed decreases compared to the previous call


				fileProgressCollection.Add(new FileProgress(
					info.StreamConsumer.Id,
					info.Filename, info.StartedOn,
					info.Length, bytesRead[0],
					info.StreamConsumer.BlockStream.BufferUnderrunCount,
					info.StreamConsumer.BlockStream.BufferOverrunCount,
					bcfProgress
				));
			}

			foreach(var blockConsumerProgressEntry in blockConsumerProgress) {
				if(blockConsumerProgressEntry.ActiveCount > 0) {
					blockConsumerProgressEntry.BufferFill /= blockConsumerProgressEntry.ActiveCount;
				}
				blockConsumerProgressEntry.BufferFill = Math.Min(1, Math.Max(0, blockConsumerProgressEntry.BufferFill));
			}

			return new Progress(startedOn, filesProcessed, bytesProcessed, fileProgressCollection, blockConsumerProgress);
		}

		public void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
			if(startedOn == DateTimeOffset.MinValue) {
				startedOn = DateTimeOffset.UtcNow;
			}

			blockStreamProgress.TryAdd(streamConsumer.BlockStream, new StreamConsumerProgressInfo(providedStream, streamConsumer));
			streamConsumer.Finished += BlockConsumerFinished;
		}
		public void Skip(ProvidedStream providedStream, long length) {
			if(startedOn == DateTimeOffset.MinValue) {
				startedOn = DateTimeOffset.UtcNow;
			}

			Interlocked.Add(ref bytesProcessed, length); //TODO? Not really processed
			Interlocked.Increment(ref filesProcessed);
		}

		private void BlockConsumerFinished(IStreamConsumer s) {
			blockStreamProgress.TryRemove(s.BlockStream, out _);

			if(s.RanToCompletion) {
				foreach(var blockConsumer in s.BlockConsumers) {
					var index = bcNameIndexMap[blockConsumer.Name];
					Interlocked.Add(ref bcBytesProcessed[index], s.BlockStream.Length);
					Interlocked.Increment(ref bcFilesProcessed[index]);
				}

				Interlocked.Add(ref bytesProcessed, s.BlockStream.Length);
				Interlocked.Increment(ref filesProcessed);
			}
		}

	}
}
