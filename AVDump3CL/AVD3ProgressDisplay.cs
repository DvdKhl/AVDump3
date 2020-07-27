using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3UI;
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

	public sealed class AVD3ProgressDisplay {
		private Func<BytesReadProgress.Progress> getProgress; //TODO As Event
		private readonly DisplaySettings settings;
		private const int UpdatePeriodInTicks = 5;
		private const int MeanAverageMinuteInterval = 60 * 1000 / AVD3Console.TickPeriod;

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }

		private int maxBCCount;
		private int maxFCount;
		private int state;
		private BytesReadProgress.Progress curP, prevP;
		private readonly float[] totalSpeedAverages = new float[3];
		private readonly int[] totalSpeedDisplayAverages = new int[3];
		private string totalSpeedDisplayUnit;
		private int totalBytesShiftCount;
		private string totalBytesDisplayUnit;

		public AVD3ProgressDisplay(DisplaySettings settings) {
			this.settings = settings;

			prevP = curP = new BytesReadProgress.Progress();
		}


		public void Initialize(Func<BytesReadProgress.Progress> getProgress) {
			var totalBytes = TotalBytes;
			while(totalBytes > 9999) {
				totalBytes >>= 10;
				totalBytesShiftCount += 10;
			}
			totalBytesDisplayUnit = totalBytesShiftCount switch { 40 => "TiB", 30 => "GiB", 20 => "MiB", 10 => "KiB", _ => "Byt" };

			curP = getProgress();
			this.getProgress = getProgress;
		}

		public void WriteProgress(AVD3ConsoleProgressBuilder pb) {
			if(state == 0) {
				prevP = curP;
				curP = getProgress();

				//Windows makes the cursor visible again when the window is resized
				if(Environment.OSVersion.Platform == PlatformID.Win32NT && Console.CursorVisible) Console.CursorVisible = false;
			}
			var interpolationFactor = (state + 1) / (double)UpdatePeriodInTicks;
			state++;
			state %= UpdatePeriodInTicks;

			var processedMiBsInInterval = ((curP.BytesProcessed - prevP.BytesProcessed) >> 20) / UpdatePeriodInTicks;

			if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 1) {
				var now = DateTimeOffset.UtcNow;
				var prevSpeed = (prevP.BytesProcessed >> 20) / (now - prevP.StartedOn).TotalSeconds;
				var curSpeed = (curP.BytesProcessed >> 20) / (now - curP.StartedOn).TotalSeconds;
				totalSpeedAverages[0] = (float)(prevSpeed + interpolationFactor * (curSpeed - prevSpeed));

			} else {
				var maInterval = MeanAverageMinuteInterval;
				totalSpeedAverages[0] = totalSpeedAverages[0] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)AVD3Console.TickPeriod * 1000) / maInterval;
			}
			if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 5) {
				totalSpeedAverages[1] = totalSpeedAverages[0];
			} else {
				var maInterval = MeanAverageMinuteInterval * 5;
				totalSpeedAverages[1] = totalSpeedAverages[1] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)AVD3Console.TickPeriod * 1000) / maInterval;
			}
			if((DateTimeOffset.UtcNow - curP.StartedOn).TotalMinutes < 15) {
				totalSpeedAverages[2] = totalSpeedAverages[1];
			} else {
				var maInterval = MeanAverageMinuteInterval * 15;
				totalSpeedAverages[2] = totalSpeedAverages[2] * (maInterval - 1) / maInterval + (processedMiBsInInterval / (float)AVD3Console.TickPeriod * 1000) / maInterval;
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

			Display(pb, interpolationFactor);

			pb.SpecialJitterEvent = state == 0;
		}

		private void Display(AVD3ConsoleProgressBuilder sb, double relPos) {
			var consoleWidth = sb.ConsoleWidth;
			if(consoleWidth < 60) return;


			var now = DateTimeOffset.UtcNow;

			sb.Append('-', consoleWidth - 1).AppendLine();

			if(!settings.HideBuffers) {
				var barWidth = consoleWidth - 8 - 1 - 2 - 2;

				var curBCCount = 0;
				for(var i = 0; i < curP.BlockConsumerProgressCollection.Count; i++) {
					var cur = curP.BlockConsumerProgressCollection[i];
					var prev = prevP.BlockConsumerProgressCollection[i];

					if(cur.ActiveCount == 0 && prev.ActiveCount == 0) continue;
					curBCCount++;

					sb.AppendFixedLength(cur.Name, 8).Append(' ');

					sb.AppendBar(barWidth, prev.BufferFill + relPos * (cur.BufferFill - prev.BufferFill)).Append(' ');
					sb.Append(cur.ActiveCount).AppendLine();
				}

				if(maxBCCount < curBCCount) maxBCCount = curBCCount;
				for(var i = curBCCount; i < maxBCCount + 1; i++) {
					sb.AppendLine();
				}
			}


			if(!settings.HideFileProgress) {
				var barWidth = consoleWidth - 23;

				int curFCount = 0;
				foreach(var item in
					from cur in curP.FileProgressCollection
					join prev in prevP.FileProgressCollection on cur.Id equals prev.Id into PrevGJ
					from prev in PrevGJ.DefaultIfEmpty()
					orderby cur.StartedOn
					select new { cur, prev = prev ?? cur }
				) {
					sb.AppendFixedLength(Path.GetFileName(item.cur.FilePath), barWidth);

					var writerLockCount = item.cur.WriterLockCount - item.prev.WriterLockCount;
					var readerLockCount = (item.cur.ReaderLockCount - item.prev.ReaderLockCount) / Math.Max(1, curP.BlockConsumerProgressCollection.Count);
					var lockSign = writerLockCount == 0 && readerLockCount == 0 ? ' ' : (writerLockCount < readerLockCount ? '-' : '+');
					sb.Append(lockSign);

					var fileProgressFactor = 0d;
					if(item.prev.FileLength > 0) {
						var fileProgressFactorPrev = item.prev.BytesProcessed / (double)item.prev.FileLength;
						var fileProgressFactorCur = item.cur.BytesProcessed / (double)item.cur.FileLength;
						fileProgressFactor = fileProgressFactorPrev + relPos * (fileProgressFactorCur - fileProgressFactorPrev);
					} else {
						fileProgressFactor = 1;
					}
					sb.AppendBar(10, fileProgressFactor);


					var prevSpeed = (item.prev.BytesProcessed >> 20) / (now - item.prev.StartedOn).TotalSeconds;
					var curSpeed = (item.cur.BytesProcessed >> 20) / (now - item.cur.StartedOn).TotalSeconds;
					var speed = (int)(prevSpeed + relPos * (curSpeed - prevSpeed));
					sb.Append(Math.Min(999, speed).ToString().PadLeft(3)).Append("MiB/s").AppendLine();

					curFCount++;
				}

				if(maxFCount < curFCount) maxFCount = curFCount;
				for(var i = curFCount; i < maxFCount; i++) sb.AppendLine();
			}

			if(!settings.HideTotalProgress && TotalBytes > 0) {
				var barWidth = consoleWidth - 30;

				var bytesProcessedFactorPrev = (double)prevP.BytesProcessed / TotalBytes;
				var bytesProcessedFactorCur = (double)curP.BytesProcessed / TotalBytes;

				//Hack because there is a bug which causes prev to be higher than cur
				if(bytesProcessedFactorPrev > bytesProcessedFactorCur) bytesProcessedFactorCur = bytesProcessedFactorPrev;

				var bytesProcessedFactor = bytesProcessedFactorPrev + relPos * (bytesProcessedFactorCur - bytesProcessedFactorPrev);
				sb.Append("Total ").AppendBar(consoleWidth - 31, bytesProcessedFactor).Append(' ');
				sb.AppendPadLeft(Math.Min(9999, totalSpeedDisplayAverages[0]), 5);
				sb.AppendPadLeft(Math.Min(9999, totalSpeedDisplayAverages[1]), 5);
				sb.AppendPadLeft(Math.Min(9999, totalSpeedDisplayAverages[2]), 5);
				sb.Append(totalSpeedDisplayUnit).AppendLine();

				var etaStr = "-.--:--:--";
				if(totalSpeedAverages[2] != 0) {
					var eta = TimeSpan.FromSeconds(((TotalBytes - curP.BytesProcessed) >> 20) / totalSpeedAverages[2]);

					if(eta.TotalDays <= 9) {
						etaStr = eta.ToString(@"d\.hh\:mm\:ss");
					}
				}

				sb.Append(curP.FilesProcessed).Append('/').Append(TotalFiles).Append(" Files | ");
				sb.Append(curP.BytesProcessed >> totalBytesShiftCount).Append('/').Append(TotalBytes >> totalBytesShiftCount).Append(" ").Append(totalBytesDisplayUnit).Append(" | ");
				sb.Append((now - curP.StartedOn).ToString(@"d\.hh\:mm\:ss")).Append(" Elapsed | ");
				sb.Append(etaStr).Append(" Remaining").AppendLine();
			}
		}
	}
}
