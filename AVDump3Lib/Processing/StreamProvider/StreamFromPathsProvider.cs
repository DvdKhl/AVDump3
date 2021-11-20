using AVDump3Lib.Misc;
using ExtKnot.StringInvariants;
using System.Collections.ObjectModel;

namespace AVDump3Lib.Processing.StreamProvider;

public class PathPartitions {
	public int ConcurrentCount { get; private set; }
	public ReadOnlyCollection<PathPartition> Partitions { get; private set; }

	public PathPartitions(int concurrentCount, IEnumerable<PathPartition> partitions) {
		ConcurrentCount = concurrentCount;
		Partitions = Array.AsReadOnly(partitions.ToArray());
	}
}

public class PathPartition {
	public string Path { get; }
	public int ConcurrentCount { get; }

	public PathPartition(string path, int concurrentCount) {
		Path = path;
		ConcurrentCount = concurrentCount;
	}
}

public sealed class StreamFromPathsProvider : IStreamProvider, IDisposable {
	private readonly List<LocalConcurrency> localConcurrencyPartitions;
	private readonly SemaphoreSlim globalConcurrency = new(1);

	public int TotalFileCount { get; private set; }
	public long TotalBytes { get; private set; }

	public StreamFromPathsProvider(PathPartitions pathPartitions) {
		if(pathPartitions is null) throw new ArgumentNullException(nameof(pathPartitions));

		globalConcurrency = new SemaphoreSlim(pathPartitions.ConcurrentCount);

		localConcurrencyPartitions = pathPartitions.Partitions.Select(pp => new LocalConcurrency(pp.Path, pp.ConcurrentCount)).ToList();
		localConcurrencyPartitions.Add(new LocalConcurrency("", pathPartitions.ConcurrentCount));
		//localConcurrencyPartitions.Sort((a, b) => b.Path.Length.CompareTo(a.Path.Length));

	}

	public void AddFiles(IEnumerable<string> paths, bool includeSubFolders, Func<FileInfo, bool> accept, Action<Exception> onError) {
		FileTraversal.Traverse(paths, includeSubFolders, filePath => {
			var fileInfo = new FileInfo(filePath);

			if(!accept(fileInfo)) return;
			//if(fileInfo.Length < 1 << 30) return;

			if(fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) {
				using var stream = fileInfo.OpenRead();
				TotalBytes += stream.Length;

			} else {
				TotalBytes += fileInfo.Length;
			}

			localConcurrencyPartitions.First(ldKey => filePath.InvStartsWith(ldKey.Path)).Files.Enqueue(filePath);
			TotalFileCount++;

		}, onError);
	}

	public IEnumerable<ProvidedStream> GetConsumingEnumerable(CancellationToken ct) {
		while(localConcurrencyPartitions.Sum(ldPathLimit => ldPathLimit.Files.Count) != 0) {
			globalConcurrency.Wait(ct);
			var localLimits = localConcurrencyPartitions.Where(ll => ll.Files.Count != 0).ToArray();
			var i = WaitHandle.WaitAny(localLimits.Select(ll => ll.Limit.AvailableWaitHandle).ToArray());
			localLimits[i].Limit.Wait(ct);

			var path = localLimits[i].Files.Dequeue();
			ProvidedStreamFromPath? providedStream = null;
			try {
				providedStream = new ProvidedStreamFromPath(this, path);
			} catch(FileNotFoundException) { //TODO error handling
				Release(path);
			} catch(IOException) { //TODO error handling
				Release(path);
			}

			if(providedStream != null) yield return providedStream;
		}
	}

	private void Release(string filePath) {
		localConcurrencyPartitions.First(ldKey => filePath.InvStartsWith(ldKey.Path)).Limit.Release();
		globalConcurrency.Release();
	}

	public void Dispose() {
		globalConcurrency.Dispose();
		foreach(var localConcurrencyPartition in localConcurrencyPartitions) {
			localConcurrencyPartition.Limit.Dispose();
		}

	}

	private class LocalConcurrency : IDisposable {
		public string Path { get; }
		public SemaphoreSlim Limit { get; }
		public Queue<string> Files { get; } = new Queue<string>();

		public LocalConcurrency(string path, int concurrentCount) {
			Path = path ?? throw new ArgumentNullException(nameof(path));
			Limit = new SemaphoreSlim(concurrentCount);
		}

		public void Dispose() => Limit.Dispose();
	}

	private class ProvidedStreamFromPath : ProvidedStream {
		private readonly StreamFromPathsProvider provider;
		private readonly string filePath;

		public ProvidedStreamFromPath(StreamFromPathsProvider provider, string filePath) : base(filePath, File.OpenRead(filePath)) {
			this.provider = provider;
			this.filePath = filePath;
		}

		public override void Dispose() {
			provider.Release(filePath);
			Stream.Dispose();
		}
	}
}
