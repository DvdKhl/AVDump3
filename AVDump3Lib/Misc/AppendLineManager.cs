using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AVDump3Lib.Misc {
	public class AppendLineManager {
		private readonly Dictionary<string, StreamWriter> streamWriters = new Dictionary<string, StreamWriter>();


		public void AppendLine(string filePath, string line) {
			lock(streamWriters) {
				if(!streamWriters.TryGetValue(filePath, out var streamWriter)) {
					streamWriter = streamWriters[filePath] = new StreamWriter(filePath, true);
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
