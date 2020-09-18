using AVDump3UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AVDump3CL {

	public sealed class AVD3ProgressDisplay {
		private Func<BytesReadProgress.Progress> getProgress; //TODO As Event
		private readonly DisplaySettings settings;
		private const int UpdatePeriodInTicks = 5;
		private const int MeanAverageMinuteInterval = 60 * 1000 / AVD3Console.TickPeriod;

		public long TotalBytes { get; set; }
		public int TotalFiles { get; set; }
		public bool Finished { get; set; }

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
			if(totalSpeedAverages[0] > 9999999 || totalSpeedAverages[1] > 9999999 || totalSpeedAverages[2] > 9999999) {
				totalSpeedDisplayAverages[0] = (int)totalSpeedAverages[0] >> 20;
				totalSpeedDisplayAverages[1] = (int)totalSpeedAverages[1] >> 20;
				totalSpeedDisplayAverages[2] = (int)totalSpeedAverages[2] >> 20;
				totalSpeedDisplayUnit = "TiB/s";

			}
			else if(totalSpeedAverages[0] > 9999 || totalSpeedAverages[1] > 9999 || totalSpeedAverages[2] > 9999) {
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
			var consoleWidth = sb.DisplayWidth;
			if(consoleWidth < 72) return;


			var now = DateTimeOffset.UtcNow;

			sb.Append('-', consoleWidth - 2).AppendLine();

			if(!settings.HideBuffers && !sb.Finished) {
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


			if(!settings.HideFileProgress && !sb.Finished) {
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

				var bytesProcessedFactorPrev = Math.Clamp((double)prevP.BytesProcessed / TotalBytes, 0, 1);
				var bytesProcessedFactorCur = Math.Clamp((double)curP.BytesProcessed / TotalBytes, 0, 1);

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
