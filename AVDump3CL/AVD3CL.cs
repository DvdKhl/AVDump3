using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
				if(activeCount > 0) bytesProcessed += bytesProcessLocal / activeCount;


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

	public sealed class AVD3CL : IDisposable {
		private Func<BytesReadProgress.Progress> getProgress; //TODO As Event
		private readonly DisplaySettings settings;
		private const int TickPeriod = 100;
		private const int UpdatePeriodInTicks = 5;
		private const int MeanAverageMinuteInterval = 60 * 1000 / TickPeriod;
		private readonly StringBuilder sb = new StringBuilder();
		private readonly List<string> toWrite = new List<string>();
		private readonly Timer timer;

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }
		public bool IsProcessing { get; set; }

		public event EventHandler<StringBuilder> AdditionalLines;

		private int displayUpdateCount;
		private int displaySkipCount;

		private string output;
		private int maxBCCount;
		private int maxFCount;
		private int maxCursorPos;
		private int state;
		private int sbLineCount, sbLineCountPrev;
		private bool hasLastDisplay;
		private BytesReadProgress.Progress curP, prevP;
		private readonly float[] totalSpeedAverages = new float[3];
		private readonly int[] totalSpeedDisplayAverages = new int[3];
		private string totalSpeedDisplayUnit;
		private int totalBytesShiftCount;
		private string totalBytesDisplayUnit;

		public AVD3CL(DisplaySettings settings) {
			this.settings = settings;

			output = "";
			timer = new Timer(TimerCallback);
			prevP = curP = new BytesReadProgress.Progress();
		}


		public void Display(Func<BytesReadProgress.Progress> getProgress) {
			var totalBytes = TotalBytes;
			while(totalBytes > 9999) {
				totalBytes >>= 10;
				totalBytesShiftCount += 10;
			}
			totalBytesDisplayUnit = totalBytesShiftCount switch { 40 => "TiB", 30 => "GiB", 20 => "MiB", 10 => "KiB", _ => "Byt" };

			curP = getProgress();
			timer.Change(500, TickPeriod);
			this.getProgress = getProgress;
		}

		public void Stop() {
			Thread.Sleep(2 * UpdatePeriodInTicks * TickPeriod);
			timer.Change(Timeout.Infinite, Timeout.Infinite);
			lock(timer) {
				if(!settings.ForwardConsoleCursorOnly) Console.SetCursorPosition(0, maxCursorPos);
				Console.WriteLine();
			}
		}

		private readonly Stopwatch sw = new Stopwatch();
		private void TimerCallback(object? _) {
			if(!Monitor.TryEnter(timer)) {
				displaySkipCount++;
				return;
			}
			sw.Restart();

			Console.Write(output);

			sb.Length = 0;

			string[] toWrite;
			lock(this.toWrite) {
				toWrite = this.toWrite.ToArray();
				this.toWrite.Clear();
			}
			var consoleWidth = Console.BufferWidth - 1;
			for(var i = 0; i < toWrite.Length; i++) {
				var line = toWrite[i];

				var sbLength = sb.Length;
				sb.Append(line);
				if(i < sbLineCount) sb.Append(' ', Math.Max(0, consoleWidth - (sb.Length - sbLength)));
				sb.AppendLine();
			}

			if(!settings.ForwardConsoleCursorOnly) {

				if(state == 0) {
					prevP = curP;
					curP = getProgress();
				}

				var processedMiBsInInterval = ((curP.BytesProcessed - prevP.BytesProcessed) >> 20) / UpdatePeriodInTicks;
				var interpolationFactor = (state + 1) / (double)UpdatePeriodInTicks;

				if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 1) {
					var now = DateTimeOffset.UtcNow;
					var prevSpeed = (prevP.BytesProcessed >> 20) / (now - prevP.StartedOn).TotalSeconds;
					var curSpeed = (curP.BytesProcessed >> 20) / (now - curP.StartedOn).TotalSeconds;
					totalSpeedAverages[0] = (float)(prevSpeed + interpolationFactor * (curSpeed - prevSpeed));

				} else {
					var maInterval = MeanAverageMinuteInterval;
					totalSpeedAverages[0] = totalSpeedAverages[0] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)TickPeriod * 1000) / maInterval;
				}
				if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 5) {
					totalSpeedAverages[1] = totalSpeedAverages[0];
				} else {
					var maInterval = MeanAverageMinuteInterval * 5;
					totalSpeedAverages[1] = totalSpeedAverages[1] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)TickPeriod * 1000) / maInterval;
				}
				if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 15) {
					totalSpeedAverages[2] = totalSpeedAverages[1];
				} else {
					var maInterval = MeanAverageMinuteInterval * 15;
					totalSpeedAverages[2] = totalSpeedAverages[2] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)TickPeriod * 1000) / maInterval;
				}
				if(totalSpeedAverages[0] > 9999 || totalSpeedAverages[1] > 9999 || totalSpeedAverages[2] > 9999) {
					totalSpeedDisplayAverages[0] = (int)totalSpeedAverages[0] >> 10;
					totalSpeedDisplayAverages[1] = (int)totalSpeedAverages[1] >> 10;
					totalSpeedDisplayAverages[2] = (int)totalSpeedAverages[2] >> 10;
					totalSpeedDisplayUnit = "GiB/s";

				} else {
					totalSpeedDisplayAverages[0] = (int)totalSpeedAverages[0];
					totalSpeedDisplayAverages[1] = (int)totalSpeedAverages[1];
					totalSpeedDisplayAverages[2] = (int)totalSpeedAverages[2];
					totalSpeedDisplayUnit = "MiB/s";
				}

				maxCursorPos = Math.Max(maxCursorPos, Console.CursorTop);
				Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - sbLineCount));

				sbLineCountPrev = sbLineCount;
				sbLineCount = 0;
				if(IsProcessing) {
					Display(sb, interpolationFactor);
				} else if(!hasLastDisplay) {
					hasLastDisplay = true;
					Display(sb, 1);
					sbLineCount = 0;
				}


				AdditionalLines?.Invoke(this, sb);

				sb.Append(' ', consoleWidth).AppendLine();
				sb.Append(' ', consoleWidth).AppendLine();
				sb.Append(' ', consoleWidth).AppendLine();
				sbLineCount += 3;

				if(settings.ShowDisplayJitter) {
					sb.AppendLine();
					sb.Append(
						displayUpdateCount++.ToString("0000") + " " +
						displaySkipCount.ToString("000") + " " +
						sw.ElapsedMilliseconds.ToString("000000") + " " +
						(state == 0 ? sw.ElapsedMilliseconds.ToString("000000") : "")
					);
					sbLineCount++;
				}

				state++;
				state %= UpdatePeriodInTicks;
			}

			output = sb.ToString();

			Monitor.Exit(timer);
		}
		public void WriteStatusLine(string statusLine) {
			sb.AppendLine(statusLine);
			sbLineCount++;
		}

		public void Writeline(params string[] lines) => Writeline((IReadOnlyList<string>)lines);
		public void Writeline(IReadOnlyList<string> lines) {
			if(sw.IsRunning) {
				lines = lines.SelectMany(x => x.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)).ToArray();
				lock(toWrite) toWrite.AddRange(lines);
			} else {
				Console.WriteLine(string.Join("\n", lines));
			}
		}

		private void Display(StringBuilder sb, double relPos) {
			var sbLength = sb.Length;
			var consoleWidth = Console.BufferWidth - 1;
			consoleWidth = Math.Min(120, consoleWidth);
			if(consoleWidth < 60) return;


			var barWidth = consoleWidth - 8 - 1 - 2 - 2;
			var now = DateTimeOffset.UtcNow;

			sbLineCount++;
			sb.Append('-', consoleWidth).AppendLine();

			double prevBarPosition, curBarPosition;
			int barPosition, curBCCount = 0;

			if(!settings.HideBuffers) {
				for(var i = 0; i < curP.BlockConsumerProgressCollection.Count; i++) {
					var cur = curP.BlockConsumerProgressCollection[i];
					var prev = prevP.BlockConsumerProgressCollection[i];

					if(cur.ActiveCount == 0 && prev.ActiveCount == 0) continue;
					curBCCount++;

					prevBarPosition = barWidth * prev.BufferFill;
					curBarPosition = barWidth * cur.BufferFill;
					barPosition = (int)(prevBarPosition + relPos * (curBarPosition - prevBarPosition));

					sbLength = sb.Length;
					sb.Append(cur.Name.Substring(0, Math.Min(cur.Name.Length, 8)));
					sb.Append(' ', 8 - (sb.Length - sbLength));

					sb.Append('[').Append('#', barPosition).Append(' ', barWidth - barPosition).Append("] ");
					sb.Append(cur.ActiveCount);

					sbLineCount++;
					sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();
				}

				if(maxBCCount < curBCCount) maxBCCount = curBCCount;
				for(var i = curBCCount; i < maxBCCount + 1; i++) {
					sbLineCount++;
					sb.Append(' ', consoleWidth).AppendLine();
				}
			}


			double prevSpeed, curSpeed;
			int speed, curFCount = 0;
			if(!settings.HideFileProgress) {
				barWidth = consoleWidth - 23;
				foreach(var item in
					from cur in curP.FileProgressCollection
					join prev in prevP.FileProgressCollection on cur.Id equals prev.Id into PrevGJ
					from prev in PrevGJ.DefaultIfEmpty()
					orderby cur.StartedOn
					select new { cur, prev = prev ?? cur }
				) {
					var fileName = Path.GetFileName(item.cur.FilePath);
					curFCount++;

					if(item.prev.FileLength > 0) {
						prevBarPosition = (int)(10 * item.prev.BytesProcessed / item.prev.FileLength);
						curBarPosition = (int)(10 * item.cur.BytesProcessed / item.cur.FileLength);
						barPosition = (int)(prevBarPosition + relPos * (curBarPosition - prevBarPosition));
						//barPosition = (int)(10 * item.cur.BytesProcessed / item.cur.FileLength);
					} else {
						barPosition = 10;
					}

					prevSpeed = (int)((item.prev.BytesProcessed >> 20) / (now - item.prev.StartedOn).TotalSeconds);
					curSpeed = (int)((item.cur.BytesProcessed >> 20) / (now - item.cur.StartedOn).TotalSeconds);
					speed = (int)(prevSpeed + relPos * (curSpeed - prevSpeed));

					var writerLockCount = item.cur.WriterLockCount - item.prev.WriterLockCount;
					var readerLockCount = (item.cur.ReaderLockCount - item.prev.ReaderLockCount) / Math.Max(1, curP.BlockConsumerProgressCollection.Count);
					var lockSign = writerLockCount == 0 && readerLockCount == 0 ? ' ' : (writerLockCount < readerLockCount ? '-' : '+');



					sbLength = sb.Length;
					sb.Append(fileName, 0, Math.Min(barWidth, fileName.Length));
					//sb.Append("RL=").Append(item.cur.ReaderLockCount).Append(" WL=").Append(item.cur.WriterLockCount);
					sb.Append(' ', barWidth - (sb.Length - sbLength));

					sb.Append(lockSign).Append('[').Append('#', barPosition).Append(' ', 10 - barPosition).Append("] ");

					sb.Append(Math.Min(999, speed).ToString().PadLeft(3)).Append("MiB/s");

					sbLineCount++;
					sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();


				}

				if(maxFCount < curFCount) maxFCount = curFCount;
				for(var i = curFCount; i < maxFCount; i++) {
					sbLineCount++;
					sb.Append(' ', consoleWidth).AppendLine();
				}
			}

			if(!settings.HideTotalProgress && TotalBytes > 0) {
				barWidth = consoleWidth - 30;
				//prevSpeed = (prevP.BytesProcessed >> 20) / (now - prevP.StartedOn).TotalSeconds;
				//curSpeed = (curP.BytesProcessed >> 20) / (now - curP.StartedOn).TotalSeconds;
				//speed = (int)(prevSpeed + relPos * (curSpeed - prevSpeed));

				prevBarPosition = (int)(barWidth * prevP.BytesProcessed / TotalBytes);
				curBarPosition = (int)(barWidth * curP.BytesProcessed / TotalBytes);
				barPosition = (int)Math.Ceiling(prevBarPosition + relPos * (curBarPosition - prevBarPosition));


				//9999 9999 9999MiB/s

				sbLength = sb.Length;
				sb.Append("Total [").Append('#', barPosition).Append(' ', consoleWidth - 30 - barPosition).Append("] ");
				sb.Append(Math.Min(9999, totalSpeedDisplayAverages[0]).ToString().PadLeft(5));
				sb.Append(Math.Min(9999, totalSpeedDisplayAverages[1]).ToString().PadLeft(5));
				sb.Append(Math.Min(9999, totalSpeedDisplayAverages[2]).ToString().PadLeft(5));
				sb.Append(totalSpeedDisplayUnit);

				sbLineCount++;
				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();

				var etaStr = "-.--:--:--";
				if(totalSpeedAverages[2] != 0) {
					var eta = TimeSpan.FromSeconds(((TotalBytes - curP.BytesProcessed) >> 20) / totalSpeedAverages[2]);

					if(eta.TotalDays <= 9) {
						etaStr = eta.ToString(@"d\.hh\:mm\:ss");
					}
				}

				sbLength = sb.Length;
				sb.Append(curP.FilesProcessed).Append('/').Append(TotalFiles).Append(" Files | ");
				sb.Append(curP.BytesProcessed >> totalBytesShiftCount).Append('/').Append(TotalBytes >> totalBytesShiftCount).Append(" ").Append(totalBytesDisplayUnit).Append(" | ");
				sb.Append((now - curP.StartedOn).ToString(@"d\.hh\:mm\:ss")).Append(" Elapsed | ");
				sb.Append(etaStr).Append(" Remaining");

				sbLineCount++;
				sb.Append(' ', consoleWidth - (sb.Length - sbLength)).AppendLine();
			}

		}


		public void Dispose() {
			lock(timer) {
				timer.Dispose();
			}
		}

		public IDisposable LockConsole() {
			Monitor.Enter(timer);

			Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop + sbLineCountPrev));
			Console.WriteLine();

			return new ProxyDisposable(() => Monitor.Exit(timer));
		}

		private class ProxyDisposable : IDisposable {
			private readonly Action dispose;
			public ProxyDisposable(Action dispose) => this.dispose = dispose;
			public void Dispose() => dispose();
		}
	}
}
