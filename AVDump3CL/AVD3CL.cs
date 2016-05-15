using AVDump3Lib.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AVDump3CL {
	public class BytesReadProgress : IBytesReadProgress, IDisposable {
		private int filesProcessed;
		private int[] bcFilesProcessed;

		private long bytesProcessed;
		private long[] bcBytesProcessed;
		private Dictionary<string, int> bcNameIndexMap;
		private Dictionary<IBlockStream, StreamConsumerProgressInfo> blockStreamProgress;

		private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

		private class StreamConsumerProgressInfo {
			public DateTimeOffset StartedOn { get; } = DateTimeOffset.UtcNow.AddMilliseconds(-1);
			public ProvidedStream ProvidedStream { get; }
			public IStreamConsumer StreamConsumer { get; }
			public long[] BytesRead { get; }

			public StreamConsumerProgressInfo(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
				ProvidedStream = providedStream;
				StreamConsumer = streamConsumer;
				BytesRead = new long[streamConsumer.BlockConsumers.Count + 1];
			}
		}

		public class BlockConsumerProgress {
			public string Name { get; internal set; }
			public int FilesProcessed { get; internal set; }
			public long BytesProcessed { get; internal set; }
			public double BufferFill { get; internal set; }
			public int ActiveCount { get; internal set; }
		}
		public class FileProgress {
			public string FilePath { get; private set; }
			public DateTimeOffset StartedOn { get; private set; }
			public long FileLength { get; private set; }
			public long BytesProcessed { get; private set; }
			public IReadOnlyList<KeyValuePair<string, long>> BytesProcessedPerBlockConsumer { get; private set; }

			public FileProgress(string filePath, DateTimeOffset startedOn, long fileLength, long bytesProcessed,
				IReadOnlyList<KeyValuePair<string, long>> bytesProcessedPerBlockConsumer
			) {
				FilePath = filePath;
				StartedOn = startedOn;
				FileLength = fileLength;
				BytesProcessed = bytesProcessed;
				BytesProcessedPerBlockConsumer = bytesProcessedPerBlockConsumer;
			}
		}
		public class Progress {
			public int FilesProcessed { get; private set; }
			public long BytesProcessed { get; private set; }
			public IReadOnlyList<FileProgress> FileProgressCollection { get; private set; }
			public IReadOnlyList<BlockConsumerProgress> BlockConsumerProgressCollection { get; private set; }

			public Progress(int filesProcessed, long bytesProcessed,
				IReadOnlyList<FileProgress> fileProgressCollection,
				IReadOnlyList<BlockConsumerProgress> blockConsumerProgressCollection
			) {
				FilesProcessed = filesProcessed;
				BytesProcessed = bytesProcessed;
				FileProgressCollection = fileProgressCollection;
				BlockConsumerProgressCollection = blockConsumerProgressCollection;
			}
		}

		public BytesReadProgress(IEnumerable<string> blockConsumerNames) {
			blockStreamProgress = new Dictionary<IBlockStream, StreamConsumerProgressInfo>();

			bcNameIndexMap = new Dictionary<string, int>();
			foreach(var blockConsumerName in blockConsumerNames) {
				bcNameIndexMap.Add(blockConsumerName, bcNameIndexMap.Count);
			}

			bcFilesProcessed = new int[bcNameIndexMap.Count];
			bcBytesProcessed = new long[bcNameIndexMap.Count];
		}

		public void Report(BlockStreamProgress value) {
			rwLock.EnterReadLock();
			var streamConsumerProgressPair = blockStreamProgress[value.Sender];
			rwLock.ExitReadLock();

			Interlocked.Add(ref streamConsumerProgressPair.BytesRead[value.Index + 1], value.BytesRead);
		}

		public Progress GetProgress() {
			var blockConsumerProgress = new BlockConsumerProgress[bcBytesProcessed.Length];
			foreach(var pair in bcNameIndexMap) {
				var item = new BlockConsumerProgress();
				item.Name = pair.Key;
				item.BytesProcessed = bcBytesProcessed[pair.Value];
				item.FilesProcessed = bcFilesProcessed[pair.Value];
				blockConsumerProgress[pair.Value] = item;
			}

			rwLock.EnterReadLock();
			var bytesProcessed = this.bytesProcessed;
			var filesProcessed = this.filesProcessed;

			var fileProgressCollection = new List<FileProgress>(blockStreamProgress.Count);
			foreach(var info in blockStreamProgress.Values) {
				var bufferLength = info.StreamConsumer.BlockStream.Buffer.BlockLength * info.StreamConsumer.BlockStream.Buffer.Blocks.Length;
				var bytesRead = (long[])info.BytesRead.Clone();
				var bcfProgress = new KeyValuePair<string, long>[bytesRead.Length - 1];

				bytesProcessed += (long)bytesRead.Skip(1).Average();

				for(int i = 0; i < bcfProgress.Length; i++) {
					bcfProgress[i] = new KeyValuePair<string, long>(
						info.StreamConsumer.BlockConsumers[i].Name,
						bytesRead[i + 1]
					);

					var index = bcNameIndexMap[info.StreamConsumer.BlockConsumers[i].Name];

					var bcProgress = blockConsumerProgress[index];
					if(info.StreamConsumer.BlockConsumers[i].IsConsuming) bcProgress.ActiveCount++;
					bcProgress.BytesProcessed += bytesRead[i + 1];
					bcProgress.BufferFill += (bytesRead[0] - bytesRead[i + 1]) / (double)bufferLength;

				}

				fileProgressCollection.Add(new FileProgress(
					(string)info.ProvidedStream.Tag,
					info.StartedOn,
					info.ProvidedStream.Stream.Length,
					bytesRead[0], bcfProgress
				));
			}
			rwLock.ExitReadLock();

			foreach(var blockConsumerProgressEntry in blockConsumerProgress) {
				if(blockConsumerProgressEntry.ActiveCount > 0) {
					blockConsumerProgressEntry.BufferFill /= blockConsumerProgressEntry.ActiveCount;
				}
				blockConsumerProgressEntry.BufferFill = Math.Min(1, Math.Max(0, blockConsumerProgressEntry.BufferFill));
			}

			return new Progress(filesProcessed, bytesProcessed, fileProgressCollection, blockConsumerProgress);
		}

		public void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
			rwLock.EnterWriteLock();
			blockStreamProgress.Add(streamConsumer.BlockStream, new StreamConsumerProgressInfo(providedStream, streamConsumer));
			rwLock.ExitWriteLock();
			streamConsumer.Finished += BlockConsumerFinished;
		}

		private void BlockConsumerFinished(StreamConsumer s) {
			rwLock.EnterWriteLock();
			var info = blockStreamProgress[s.BlockStream];
			blockStreamProgress.Remove(s.BlockStream);
			rwLock.ExitWriteLock();

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

		public void Dispose() {
			rwLock.Dispose();
		}
	}

	public class AVD3CL {
		private Func<BytesReadProgress.Progress> getProgress;

		public AVD3CL(Func<BytesReadProgress.Progress> getProgress) {
			this.getProgress = getProgress;
		}

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }

		public void Process() {
			Console.CursorVisible = false;

			var c = 0;
			var sb = new StringBuilder();
			var startedOn = DateTimeOffset.UtcNow.AddMilliseconds(-1);
			while(true) {
				var consoleWidth = Console.BufferWidth - 1;
				var barWidth = consoleWidth - 8 - 1 - 2 - 2;
				var outputOn = DateTimeOffset.UtcNow;
				var p = getProgress();

				sb.Length = 0;
				int speed, barPosition;
				foreach(var blockConsumerProgress in p.BlockConsumerProgressCollection) {
					if(blockConsumerProgress.ActiveCount == 0) {
						//continue;
					}

					barPosition = (int)(barWidth * blockConsumerProgress.BufferFill);

					sb.AppendLine((
						blockConsumerProgress.Name.PadLeft(8) +
						"[" + "".PadLeft(barPosition, '*') + "".PadLeft(barWidth - barPosition, ' ') + "] " +
						blockConsumerProgress.ActiveCount.ToString().PadLeft(2)).PadRight(barWidth));
				}
				sb.AppendLine("".PadLeft(consoleWidth));

				barWidth = consoleWidth - 21;
				foreach(var fileProgress in p.FileProgressCollection) {
					var fileName = Path.GetFileName(fileProgress.FilePath);
					if(fileName.Length > barWidth) fileName = fileName.Substring(0, barWidth);

					barPosition = (int)(10 * fileProgress.BytesProcessedPerBlockConsumer.Average(x => x.Value) / fileProgress.FileLength);
					speed = (int)((fileProgress.BytesProcessed >> 20) / (DateTimeOffset.UtcNow - fileProgress.StartedOn).TotalSeconds);
					sb.AppendLine((
						fileName.PadRight(barWidth) +
						"[" + "".PadLeft(barPosition, '*') + "".PadLeft(10 - barPosition, ' ') + "]" +
						speed.ToString().PadLeft(4) + "MiB/s").PadRight(consoleWidth));
				}

				barWidth = consoleWidth - 17;
				speed = (int)((p.BytesProcessed >> 20) / (DateTimeOffset.UtcNow - startedOn).TotalSeconds);
				barPosition = (int)(barWidth * p.BytesProcessed / TotalBytes);
				sb.AppendLine((
					"Total [" + "".PadLeft(barPosition, '*') + "".PadLeft(barWidth - barPosition, ' ') + "]" +
					speed.ToString().PadLeft(4) + "MiB/s").PadRight(consoleWidth));


				var etaStr = "-.--:--:--";
				if(speed != 0) {
					var eta = TimeSpan.FromSeconds(((TotalBytes - p.BytesProcessed) >> 20) / speed);

					if(eta.TotalDays <= 9) {
						etaStr = eta.ToString(@"d\.hh\:mm\:ss");
					}
				}

				sb.AppendLine((
					p.FilesProcessed + "/" + TotalFiles + "Files " +
					(p.BytesProcessed >> 30) + "/" + (TotalBytes >> 30) + " GiB " +
					(DateTimeOffset.UtcNow - startedOn).ToString(@"d\.hh\:mm\:ss") + " Elapsed " +
					etaStr + " Remaining").PadRight(consoleWidth));

				sb.AppendLine("".PadLeft(consoleWidth));
				sb.AppendLine("".PadLeft(consoleWidth));
				sb.AppendLine("".PadLeft(consoleWidth));
				sb.AppendLine("".PadLeft(consoleWidth));

				//(p.FilesProcessed + "/" + TotalFiles).PadLeft(12) + "Files" +

				var cursorPos = Console.CursorTop;
				Console.Write(sb.ToString());
				Console.WriteLine(((int)(DateTimeOffset.UtcNow - outputOn).TotalMilliseconds).ToString().PadLeft(5) + " " + c++.ToString().PadLeft(5));

				Console.SetCursorPosition(0, cursorPos);

				Thread.Sleep(Math.Max(0, 200 - (int)(DateTimeOffset.UtcNow - outputOn).TotalMilliseconds));
			}
		}



	}
}
