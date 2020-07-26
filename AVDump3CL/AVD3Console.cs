using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace AVDump3CL {

	public class AVD3ConsoleProgressBuilder {
		public int ConsoleWidth { get; private set; }

		public StringBuilder Buffer { get; } = new StringBuilder();
		public int ProgressLineCount { get; private set; }

		public bool SpecialJitterEvent { get; set; }

		private int lastLinePosition;


		public AVD3ConsoleProgressBuilder AppendLine() {
			var repeatCount = ConsoleWidth - (Buffer.Length - lastLinePosition);
			if(repeatCount > 0) Buffer.Append(' ', repeatCount);
			Buffer.AppendLine();

			lastLinePosition = Buffer.Length;
			ProgressLineCount++;
			return this;
		}

		public AVD3ConsoleProgressBuilder AppendBar(int barSize, double fillFactor) {
			barSize -= 2;

			var barFillCharCount = (int)Math.Ceiling(barSize * fillFactor);

			Buffer.Append('[').Append('#', barFillCharCount).Append(' ', barSize - barFillCharCount).Append(']');
			return this;
		}

		public AVD3ConsoleProgressBuilder AppendLinePad(int length) {
			if(length > lastLinePosition) Buffer.Append(' ', length - lastLinePosition);
			return this;
		}

		public AVD3ConsoleProgressBuilder AppendFixedLength(string value, int length) {
			if(value.Length < length) {
				Buffer.Append(value).Append(' ', length - value.Length);
			} else {
				Buffer.Append(value, 0, length);
			}
			return this;
		}
		public AVD3ConsoleProgressBuilder AppendPadRight(int value, int length) {
			var pos = Buffer.Length;
			Buffer.Append(value);

			if(Buffer.Length - pos < length) {
				Buffer.Append(' ', length - (Buffer.Length - pos));
			}

			return this;
		}

		public AVD3ConsoleProgressBuilder AppendPadLeft(int value, int length) {
			Buffer.Append(value.ToString().PadLeft(length)); //Meh
			return this;
		}

		public AVD3ConsoleProgressBuilder Append(char value, int repeatCount) {
			Buffer.Append(value, repeatCount);
			return this;
		}

		public AVD3ConsoleProgressBuilder Append(string value, int startIndex, int charCount) {
			Buffer.Append(value, startIndex, charCount);
			return this;
		}

		public AVD3ConsoleProgressBuilder Append(int value) {
			Buffer.Append(value);
			return this;
		}
		public AVD3ConsoleProgressBuilder Append(char value) {
			Buffer.Append(value);
			return this;
		}

		public AVD3ConsoleProgressBuilder Append(long value) {
			Buffer.Append(value);
			return this;
		}

		public AVD3ConsoleProgressBuilder Append(string value) {
			Buffer.Append(value);
			return this;
		}

		public void MarkLastLine() {
			lastLinePosition = Buffer.Length;
		}

		public void Reset() {
			Buffer.Length = 0;
			lastLinePosition = 0;
			ProgressLineCount = 0;
			SpecialJitterEvent = false;
			ConsoleWidth = Math.Min(120, Console.BufferWidth);
		}
	}

	public delegate void WriteProgress(AVD3ConsoleProgressBuilder builder);

	public interface IAVD3Console {
		event WriteProgress WriteProgress;

		IDisposable LockConsole();
		void WriteLine(IEnumerable<string> values);
		void WriteLine(string value);
	}

	public class AVD3Console : IDisposable, IAVD3Console {
		public const int TickPeriod = 100;

		private readonly object progressWriteLock = new object();
		private readonly object progressWriteActiveChangeLock = new object();

		private readonly Timer progressTimer;
		private readonly AVD3ConsoleProgressBuilder progressBuilder = new AVD3ConsoleProgressBuilder();
		private readonly List<string> toWrite = new List<string>();

		private int displaySkipCount;
		private int maxTopCursorPos;
		private bool progressWriteActive;
		private int jitterDisplayUpdateCount;
		private Stopwatch perfWatch = new Stopwatch();

		public event WriteProgress WriteProgress = delegate { };
		public bool ShowDisplayJitter { get; set; }
		public bool ShowingProgress => progressWriteActive;

		public AVD3Console() {
			progressTimer = new Timer(OnWriteProgress);
		}

		public void StartProgressDisplay() {
			lock(progressWriteLock) {
				lock(progressWriteActiveChangeLock) {
					progressWriteActive = true;
					progressTimer.Change(500, TickPeriod);
				}
			}
		}
		public void StopProgressDisplay() {
			Thread.Sleep(500); //HACK Wait until progress bar has filled completely

			lock(progressWriteLock) {
				lock(progressWriteActiveChangeLock) {
					progressTimer.Change(Timeout.Infinite, Timeout.Infinite);
					progressWriteActive = false;

					Console.SetCursorPosition(0, maxTopCursorPos);
				}
			}
		}

		private void OnWriteProgress(object? _) {
			if(!Monitor.TryEnter(progressWriteLock, 10)) {
				displaySkipCount++;
				return;
			}

			try {
				perfWatch.Restart();

				Console.Write(progressBuilder.Buffer);


				var progressLineCountPrev = progressBuilder.ProgressLineCount;
				progressBuilder.Reset();

				maxTopCursorPos = Math.Max(maxTopCursorPos, Console.CursorTop);
				Console.SetCursorPosition(0, Math.Max(0, Console.CursorTop - progressLineCountPrev));

				string[] toWrite;
				lock(this.toWrite) {
					toWrite = this.toWrite.ToArray();
					this.toWrite.Clear();
				}

				var linesCleared = 0;
				var consoleWidth = Console.BufferWidth - 1;
				for(var i = 0; i < toWrite.Length; i++) {
					var line = toWrite[i];

					if(linesCleared < progressLineCountPrev) {
						var lines = line.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
						for(int j = 0; j < lines.Length; j++) {
							progressBuilder.Buffer.Append(lines[j]);
							if(lines[j].Length < progressBuilder.ConsoleWidth) {
								progressBuilder.Buffer.Append(' ', progressBuilder.ConsoleWidth - lines[j].Length);
							}

							progressBuilder.Buffer.AppendLine();
							linesCleared++;
						}
					} else {
						progressBuilder.Buffer.Append(line).AppendLine();
					}

					progressBuilder.MarkLastLine();
				}

				WriteProgress(progressBuilder);

				if(ShowDisplayJitter) {
					progressBuilder.AppendLine();
					progressBuilder.Append(
						jitterDisplayUpdateCount++.ToString("0000") + " " +
						displaySkipCount.ToString("000") + " " +
						perfWatch.ElapsedMilliseconds.ToString("000000") + " " +
						(progressBuilder.SpecialJitterEvent ? perfWatch.ElapsedMilliseconds.ToString("000000") : "")
					);
				}


			} finally {
				Monitor.Exit(progressWriteLock);
			}
		}

		public void WriteLine(string value) {
			lock(progressWriteActiveChangeLock) {
				if(progressWriteActive) {
					lock(toWrite) toWrite.Add(value);
				} else {
					Console.WriteLine(value);
				}
			}
		}
		public void WriteLine(IEnumerable<string> values) {
			lock(progressWriteActiveChangeLock) {
				if(progressWriteActive) {
					lock(toWrite) toWrite.AddRange(values);
				} else {
					Console.WriteLine(string.Join("\n", values));
				}
			}
		}

		public IDisposable LockConsole() {
			Monitor.Enter(progressWriteLock);
			return new ProxyDisposable(() => Monitor.Exit(progressWriteLock));
		}

		public void Dispose() => ((IDisposable)progressTimer).Dispose();

		private class ProxyDisposable : IDisposable {
			private readonly Action dispose;
			public ProxyDisposable(Action dispose) => this.dispose = dispose;
			public void Dispose() => dispose();
		}

	}
}
