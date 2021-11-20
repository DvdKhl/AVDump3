namespace AVDump3Lib.Misc;

public static class FileTraversal {
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

			} catch(Exception ex) {
				onError?.Invoke(ex);
			}
		}
	}

}
