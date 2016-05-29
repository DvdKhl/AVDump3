using AVDump3Lib.Processing.BlockBuffers;
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
		private DateTimeOffset startedOn;
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
			public int ReaderLockCount { get; private set; }
			public int WriterLockCount { get; private set; }
			public IReadOnlyList<KeyValuePair<string, long>> BytesProcessedPerBlockConsumer { get; private set; }

			public FileProgress(string filePath, DateTimeOffset startedOn, long fileLength, long bytesProcessed,
				int readerLockCount, int writerLockCount,
				IReadOnlyList<KeyValuePair<string, long>> bytesProcessedPerBlockConsumer
			) {
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
				//item.BufferFill = 1;
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
					//bcProgress.BufferFill = Math.Min(bcProgress.BufferFill,(bytesRead[0] - bytesRead[i + 1]) / (double)bufferLength);

				}

				fileProgressCollection.Add(new FileProgress(
					info.Filename, info.StartedOn,
					info.Length, bytesRead[0],
					info.StreamConsumer.BlockStream.ReaderBlockCount,
					info.StreamConsumer.BlockStream.WriterBlockCount,
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

	public sealed class AVD3CL : IDisposable {
		private Func<BytesReadProgress.Progress> getProgress;

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }

		private string output;
		private int maxBCCount;
		private int maxFCount;
		private int maxCursorPos;
		private Timer timer;
		private int TicksInPeriod = 5;
		private int state;
		private int sbLineCount;
		private bool dirty;
		private StringBuilder sb;
		private BytesReadProgress.Progress curP, prevP;

		public AVD3CL(Func<BytesReadProgress.Progress> getProgress) {
			this.getProgress = getProgress;
			sb = new StringBuilder();
		}


		public void Display() {
			curP = getProgress();
			timer = new Timer(TimerCallback, null, 500, 100);
		}

		public void Stop() {
			Console.SetCursorPosition(0, maxCursorPos);
			Console.WriteLine();
		}

		private void TimerCallback(object sender) {
			if(!Monitor.TryEnter(timer)) return;
			Console.Write(output); dirty = true;
			maxCursorPos = Math.Max(maxCursorPos, Console.CursorTop);
			Console.SetCursorPosition(0, Console.CursorTop - sbLineCount);

			if(state == 0) {
				prevP = curP;
				curP = getProgress();
			}
			sb.Length = 0;

			Display(sb, state / (double)TicksInPeriod);
			output = sb.ToString();

			state++;
			state %= TicksInPeriod;

			Monitor.Exit(timer);
		}

		public void Writeline(string line) {
			lock (timer) {
				if(dirty) {
					var clearLine = new string(' ', Console.BufferWidth - 1);
					for(int i = 0; i < sbLineCount; i++) {
						Console.WriteLine(clearLine);
					}
					Console.SetCursorPosition(0, Console.CursorTop - sbLineCount);
					dirty = false;
				}

				Console.WriteLine(line);
			}
		}

		private void Display(StringBuilder sb, double relPos) {

			var sbLength = 0;
			var consoleWidth = Console.BufferWidth - 1;
			consoleWidth = 79;

			var barWidth = consoleWidth - 8 - 1 - 2 - 2;
			var outputOn = DateTimeOffset.UtcNow;
			var progressSpan = DateTimeOffset.UtcNow - outputOn;
			var now = DateTimeOffset.UtcNow;

			sbLineCount = 0;
			sb.Length = 0;
			sbLineCount++;
			sb.Append('-', consoleWidth).AppendLine();
			double prevBarPosition, curBarPosition;
			int barPosition, curBCCount = 0;
			for(int i = 0; i < curP.BlockConsumerProgressCollection.Count; i++) {
				var cur = curP.BlockConsumerProgressCollection[i];
				var prev = prevP.BlockConsumerProgressCollection[i];

				if(cur.ActiveCount == 0 && prev.ActiveCount == 0) continue;
				curBCCount++;

				prevBarPosition = barWidth * prev.BufferFill;
				curBarPosition = barWidth * cur.BufferFill;
				barPosition = (int)(prevBarPosition + relPos * (curBarPosition - prevBarPosition));

				sbLength = sb.Length;
				sb.Append(cur.Name);
				sb.Append(' ', 8 - (sb.Length - sbLength));

				sb.Append('[').Append('#', barPosition).Append(' ', barWidth - barPosition).Append("] ");
				sb.Append(cur.ActiveCount);

				sbLineCount++;
				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();
			}

			if(maxBCCount < curBCCount) maxBCCount = curBCCount;
			for(int i = curBCCount; i < maxBCCount + 1; i++) {
				sbLineCount++;
				sb.Append(' ', consoleWidth).AppendLine();
			}


			double prevSpeed, curSpeed;
			int speed, curFCount = 0;
			barWidth = consoleWidth - 21;
			foreach(var item in from cur in curP.FileProgressCollection
								join prev in prevP.FileProgressCollection on cur.FilePath equals prev.FilePath
								orderby cur.StartedOn
								select new { cur, prev }

			) {
				var fileName = Path.GetFileName(item.cur.FilePath);
				curFCount++;

				//prevBarPosition = (int)(10 * item.prev.BytesProcessed / item.prev.FileLength);
				//curBarPosition = (int)(10 * item.cur.BytesProcessed / item.cur.FileLength);
				//barPosition = (int)(prevBarPosition + relPos * (curBarPosition - prevBarPosition));
				barPosition = (int)(10 * item.cur.BytesProcessed / item.cur.FileLength);

				prevSpeed = (int)((item.prev.BytesProcessed >> 20) / (now - item.prev.StartedOn).TotalSeconds);
				curSpeed = (int)((item.cur.BytesProcessed >> 20) / (now - item.cur.StartedOn).TotalSeconds);
				speed = (int)(prevSpeed + relPos * (curSpeed - prevSpeed));

				sbLength = sb.Length;
				sb.Append(fileName, 0, Math.Min(barWidth, fileName.Length));
				//sb.Append("RL=").Append(item.cur.ReaderLockCount).Append(" WL=").Append(item.cur.WriterLockCount);
				sb.Append(' ', barWidth - (sb.Length - sbLength));

				sb.Append('[').Append('#', barPosition).Append(' ', 10 - barPosition).Append("] ");

				sb.Append(speed).Append("MiB/s");

				sbLineCount++;
				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();


			}

			if(maxFCount < curFCount) maxFCount = curFCount;
			for(int i = curFCount; i < maxFCount; i++) {
				sbLineCount++;
				sb.Append(' ', consoleWidth).AppendLine();
			}

			barWidth = consoleWidth - 17;
			prevSpeed = (prevP.BytesProcessed >> 20) / (now - prevP.StartedOn).TotalSeconds;
			curSpeed = (curP.BytesProcessed >> 20) / (now - curP.StartedOn).TotalSeconds;
			speed = (int)(prevSpeed + relPos * (curSpeed - prevSpeed));

			prevBarPosition = (int)(barWidth * prevP.BytesProcessed / TotalBytes);
			curBarPosition = (int)(barWidth * curP.BytesProcessed / TotalBytes);
			barPosition = (int)(prevBarPosition + relPos * (curBarPosition - prevBarPosition));

			sbLength = sb.Length;
			sb.Append("Total [").Append('#', barPosition).Append(' ', barWidth - barPosition).Append("] ");
			sb.Append(Math.Min(999, speed)).Append("MiB/s");

			sbLineCount++;
			sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();

			var etaStr = "-.--:--:--";
			if(speed != 0) {
				var eta = TimeSpan.FromSeconds(((TotalBytes - curP.BytesProcessed) >> 20) / speed);

				if(eta.TotalDays <= 9) {
					etaStr = eta.ToString(@"d\.hh\:mm\:ss");
				}
			}

			sbLength = sb.Length;
			sb.Append(curP.FilesProcessed).Append('/').Append(TotalFiles).Append(" Files | ");
			sb.Append(curP.BytesProcessed >> 30).Append('/').Append(TotalBytes >> 30).Append(" GiB | ");
			sb.Append((now - curP.StartedOn).ToString(@"d\.hh\:mm\:ss")).Append(" Elapsed | ");
			sb.Append(etaStr).Append(" Remaining");
			sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();

			sb.Append(' ', consoleWidth).AppendLine();
			sb.Append(' ', consoleWidth).AppendLine();
			sb.Append(' ', consoleWidth).AppendLine();
			sbLineCount += 4;
		}

		public void Dispose() {
			lock (timer) {
				timer.Dispose();
			}
		}
	}
}
