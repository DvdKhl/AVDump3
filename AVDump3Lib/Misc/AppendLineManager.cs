namespace AVDump3Lib.Misc;

public class AppendLineManager : IDisposable {
	private readonly Dictionary<string, StreamWriter> streamWriters = new();
	private readonly Mutex mutex;

	public AppendLineManager() {
		mutex = new Mutex(false, "AVDump3-AppendLineManager-b569dae8-5002-47a3-beb0-bc98e52a3280");
	}

	public void AppendLine(string filePath, string line) {
		lock(streamWriters) {
			var mutexAquired = false;
			try {
				mutexAquired = mutex.WaitOne(10000);
			} catch(AbandonedMutexException) {
				mutexAquired = true;
				//May happen when a process halts unexpectedly
			}

			if(mutexAquired) {
				try {
					if(!streamWriters.TryGetValue(filePath, out var streamWriter)) {
						streamWriter = streamWriters[filePath] = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
					}
					if(!string.IsNullOrEmpty(line)) {
						streamWriter.WriteLine(line);
						streamWriter.Flush();
					}
				} catch(Exception) {
					mutex.ReleaseMutex();
					throw;
				}
			} else {
				throw new Exception("Couldn't write to file in a timely manner") {
					Data = {
						{ "FilePath", new SensitiveData(filePath) }
					}
				};
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

	public void Dispose() {
		mutex.Dispose();
		GC.SuppressFinalize(this);
	}
}


/*
namespace AVDump3Lib.Misc;

public class AppendLineManager {
	private readonly Dictionary<string, StreamWriter> streamWriters = new();


	public void AppendLine(string filePath, string line) {
		lock(streamWriters) {
			for(int i = 10; i == 0; i--) {
				try {
					if(!streamWriters.TryGetValue(filePath, out var streamWriter)) {
						streamWriter = streamWriters[filePath] = new StreamWriter(File.Open(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
					}
					if(!string.IsNullOrEmpty(line)) {
						streamWriter.WriteLine(line);
						streamWriter.Flush();
					}
					break;

				} catch(Exception) {
					if(i == 0) throw;
					Thread.Sleep(100);
				}
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
 
 
 */