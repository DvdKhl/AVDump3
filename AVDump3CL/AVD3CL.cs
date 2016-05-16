using AVDump3Lib.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AVDump3CL {
	public class BytesReadProgress : IBytesReadProgress {
		private int filesProcessed;
		private int[] bcFilesProcessed;

		private long bytesProcessed;
		private long[] bcBytesProcessed;
		private Dictionary<string, int> bcNameIndexMap;
		private ConcurrentDictionary<IBlockStream, StreamConsumerProgressInfo> blockStreamProgress;

		private class StreamConsumerProgressInfo {
			public DateTimeOffset StartedOn { get; } = DateTimeOffset.UtcNow.AddMilliseconds(-1);
			public string Filename { get; }
			public IStreamConsumer StreamConsumer { get; }
			public long[] BytesRead { get; }
			public long Length { get; }

			public StreamConsumerProgressInfo(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
				Filename = (string)providedStream.Tag;
				StreamConsumer = streamConsumer;
				BytesRead = new long[streamConsumer.BlockConsumers.Count + 1];
				Length = providedStream.Stream.Length;
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
				var item = new BlockConsumerProgress();
				item.Name = pair.Key;
				item.BytesProcessed = bcBytesProcessed[pair.Value];
				item.FilesProcessed = bcFilesProcessed[pair.Value];
				blockConsumerProgress[pair.Value] = item;
			}

			var bytesProcessed = this.bytesProcessed;
			var filesProcessed = this.filesProcessed;

			var fileProgressCollection = new List<FileProgress>(blockStreamProgress.Count);
			foreach(var info in blockStreamProgress.Values.ToArray()) {
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
					info.Filename, info.StartedOn,
					info.Length, bytesRead[0],
					bcfProgress
				));
			}

			foreach(var blockConsumerProgressEntry in blockConsumerProgress) {
				if(blockConsumerProgressEntry.ActiveCount > 0) {
					blockConsumerProgressEntry.BufferFill /= blockConsumerProgressEntry.ActiveCount;
				}
				blockConsumerProgressEntry.BufferFill = Math.Min(1, Math.Max(0, blockConsumerProgressEntry.BufferFill));
			}

			return new Progress(filesProcessed, bytesProcessed, fileProgressCollection, blockConsumerProgress);
		}

		public void Register(ProvidedStream providedStream, IStreamConsumer streamConsumer) {
			blockStreamProgress.TryAdd(streamConsumer.BlockStream, new StreamConsumerProgressInfo(providedStream, streamConsumer));
			streamConsumer.Finished += BlockConsumerFinished;
		}

		private void BlockConsumerFinished(StreamConsumer s) {
			StreamConsumerProgressInfo info;
			blockStreamProgress.TryRemove(s.BlockStream, out info);

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

	public class AVD3CL {
		private Func<BytesReadProgress.Progress> getProgress;

		public AVD3CL(Func<BytesReadProgress.Progress> getProgress) {
			this.getProgress = getProgress;
		}

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }

		public void Display() {
			Console.CursorVisible = false;

			var sb = new StringBuilder();
			var sbLength = 0;
			var startedOn = DateTimeOffset.UtcNow.AddMilliseconds(-1);
			var maxBCCount = 0;
			var maxFCount = 0;
			while(true) {
				var consoleWidth = Console.BufferWidth - 1;
				var barWidth = consoleWidth - 8 - 1 - 2 - 2;
				var outputOn = DateTimeOffset.UtcNow;
				var p = getProgress();
				var progressSpan = DateTimeOffset.UtcNow - outputOn;

				sb.Length = 0;
				int barPosition, curBCCount = 0;
				foreach(var blockConsumerProgress in p.BlockConsumerProgressCollection) {
					if(blockConsumerProgress.ActiveCount == 0) continue;
					curBCCount++;

					barPosition = (int)(barWidth * blockConsumerProgress.BufferFill);

					sbLength = sb.Length;
					sb.Append(blockConsumerProgress.Name);
					sb.Append(' ', 8 - (sb.Length - sbLength));

					sb.Append('[').Append('*', barPosition).Append(' ', barWidth - barPosition).Append("] ");

					sb.Append(blockConsumerProgress.ActiveCount);

					sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();
				}

				if(maxBCCount < curBCCount) maxBCCount = curBCCount;
				for(int i = curBCCount; i < maxBCCount + 1; i++) {
					sb.Append(' ', consoleWidth).AppendLine();
				}

				int speed;
				barWidth = consoleWidth - 21;
				foreach(var fileProgress in p.FileProgressCollection.OrderBy(x => x.StartedOn)) {
					var fileName = Path.GetFileName(fileProgress.FilePath);
					if(fileName.Length > barWidth) fileName = fileName.Substring(0, barWidth);

					barPosition = (int)(10 * fileProgress.BytesProcessed / fileProgress.FileLength);
					speed = (int)((fileProgress.BytesProcessed >> 20) / (DateTimeOffset.UtcNow - fileProgress.StartedOn).TotalSeconds);

					sbLength = sb.Length;
					sb.Append(fileName);
					sb.Append(' ', barWidth - (sb.Length - sbLength));

					sb.Append('[').Append('*', barPosition).Append(' ', 10 - barPosition).Append("] ");

					sb.Append(speed).Append("MiB/s");

					sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();
				}

				if(maxFCount < p.FileProgressCollection.Count) maxFCount = p.FileProgressCollection.Count;
				for(int i = p.FileProgressCollection.Count; i < maxFCount; i++) {
					sb.Append(' ', consoleWidth).AppendLine();
				}

				barWidth = consoleWidth - 17;
				speed = (int)((p.BytesProcessed >> 20) / (DateTimeOffset.UtcNow - startedOn).TotalSeconds);
				barPosition = (int)(barWidth * p.BytesProcessed / TotalBytes);

				sbLength = sb.Length;
				sb.Append("Total [").Append('*', barPosition).Append(' ', barWidth - barPosition).Append("] ");
				sb.Append(speed).Append("MiB/s");

				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();

				var etaStr = "-.--:--:--";
				if(speed != 0) {
					var eta = TimeSpan.FromSeconds(((TotalBytes - p.BytesProcessed) >> 20) / speed);

					if(eta.TotalDays <= 9) {
						etaStr = eta.ToString(@"d\.hh\:mm\:ss");
					}
				}

				sbLength = sb.Length;
				sb.Append(p.FilesProcessed).Append('/').Append(TotalFiles).Append(" Files | ");
				sb.Append(p.BytesProcessed >> 30).Append('/').Append(TotalBytes >> 30).Append(" GiB | ");
				sb.Append(DateTimeOffset.UtcNow - startedOn).Append(" Elapsed | ");
				sb.Append(etaStr).Append(" Remaining");
				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();

				sb.Append(' ', consoleWidth).AppendLine();
				sb.Append(' ', consoleWidth).AppendLine();
				sb.Append(' ', consoleWidth).AppendLine();

				var cursorPos = Console.CursorTop;
				Console.Write(sb.ToString());

				Thread.Sleep(Math.Max(0, 200 - (int)(DateTimeOffset.UtcNow - outputOn).TotalMilliseconds));
				Console.WriteLine((DateTimeOffset.UtcNow - outputOn).ToString(@"d\.hh\:mm\:ss\.fff"));
				Console.SetCursorPosition(0, cursorPos);
			}
		}
	}
}
