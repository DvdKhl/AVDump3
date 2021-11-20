using System.Collections.Generic;
using System.IO;

namespace AVDump3Lib.Misc {
	public class AppendLineManager {
		private readonly Dictionary<string, StreamWriter> streamWriters = new();


		public void AppendLine(string filePath, string line) {
			lock(streamWriters) {
				if(!streamWriters.TryGetValue(filePath, out var streamWriter)) {
					streamWriter = streamWriters[filePath] = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
				}

				if(!string.IsNullOrEmpty(line)) {
					streamWriter.WriteLine(line);
					streamWriter.Flush();
				}
			}
		}

		public void Clear() {
			lock(streamWriters) {
				foreach(var streamWriter in streamWriters.Values) {
					streamWriter.Close();
				}
				streamWriters.Clear();
			}
		}
	}
}
