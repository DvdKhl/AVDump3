using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AVDump3Lib.Misc {
	public class FileTraversal {
		public static void Traverse(string path, bool includeSubFolders, Action<string> onFile, Action<Exception> onError) {
			try {
				if(File.Exists(path)) {
					onFile(path);
					return;
				} else if(Directory.Exists(path)) {
					TraverseDirectories(new[] { path }, includeSubFolders, onFile, onError);
				}
			} catch(Exception ex) {
				onError?.Invoke(ex);
			}
		}

		public static void Traverse(IEnumerable<string> fileSystemEntries, bool includeSubFolders, Action<string> onFile, Action<Exception> onError) {
			foreach(var path in fileSystemEntries.Where(path => !Directory.Exists(path) && !File.Exists(path))) {
				onError?.Invoke(new Exception("Path not found: " + path));
			}

			foreach(var filePath in fileSystemEntries.Where(path => File.Exists(path))) {
				try { onFile(filePath); } catch(Exception ex) { onError?.Invoke(ex); }
			}

			TraverseDirectories(fileSystemEntries.Where(path => Directory.Exists(path)), includeSubFolders, onFile, onError);
		}

		public static void TraverseDirectories(IEnumerable<string> directoryPaths, bool includeSubFolders, Action<string> onFile, Action<Exception> onError) {
			foreach(var directoryPath in directoryPaths) {
				try {
					foreach(var filePath in Directory.EnumerateFiles(directoryPath).OrderBy(x => x)) {
						try {
							onFile(filePath);
						} catch(Exception ex) { onError?.Invoke(ex); }
					}

					if(includeSubFolders) TraverseDirectories(Directory.EnumerateDirectories(directoryPath).OrderBy(x => x), includeSubFolders, onFile, onError);

				} catch(UnauthorizedAccessException ex) {
					if(!Directory.Exists("Error")) Directory.CreateDirectory("Error");
					File.AppendAllText(
						Path.Combine("Error", "UnauthorizedAccessExceptions.txt"),
						string.Format("{0} {1} \"{2}\"", Environment.Version.Build, DateTime.Now.ToString("s"), ex.Message) + Environment.NewLine
					);

				} catch(Exception ex) {
					onError?.Invoke(ex);
				}
			}
		}

	}
}
