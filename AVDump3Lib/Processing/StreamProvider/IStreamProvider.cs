namespace AVDump3Lib.Processing.StreamProvider;

public abstract class ProvidedStream : IDisposable {
	public object Tag { get; private set; }
	public Stream Stream { get; }
	public abstract void Dispose();

	public ProvidedStream(object tag, Stream stream) {
		Tag = tag;
		Stream = stream;
	}
}

public interface IStreamProvider {
	IEnumerable<ProvidedStream> GetConsumingEnumerable(CancellationToken ct);
}
