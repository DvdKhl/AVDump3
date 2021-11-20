using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockBuffers.Sources;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamProvider;

namespace AVDump3Lib.Processing.StreamConsumer;

public interface IStreamConsumerFactory {
	IStreamConsumer Create(ProvidedStream providedStream);
}
public class StreamConsumerFactory : IStreamConsumerFactory {
	private readonly IBlockConsumerSelector blockConsumerSelector;
	private readonly IMirroredBufferPool bufferPool;

	public StreamConsumerFactory(IBlockConsumerSelector blockConsumerSelector, IMirroredBufferPool bufferPool, int minProducerReadLength, int maxProducerReadLength) {
		this.blockConsumerSelector = blockConsumerSelector;
		this.bufferPool = bufferPool;
		MinProducerReadLength = minProducerReadLength;
		MaxProducerReadLength = maxProducerReadLength;
	}

	public int MinProducerReadLength { get; }
	public int MaxProducerReadLength { get; }

	public IStreamConsumer Create(ProvidedStream providedStream) {
		var blockConsumerFactories = blockConsumerSelector.Select();
		if(!blockConsumerFactories.Any()) {
			return null;
		}

		var blockSource = new StreamBlockSource(providedStream.Stream);

		var buffer = bufferPool.Take();
		var circularBuffer = new CircularBuffer64Bit(buffer, blockConsumerFactories.Count()); //TODO CircularBuffer64Bit CircularBuffer32Bit
		var blockStream = new BlockStream(blockSource, circularBuffer, MinProducerReadLength, MaxProducerReadLength);
		var blockConsumers = blockConsumerSelector.Create(blockConsumerFactories, blockStream, providedStream.Tag).ToArray();

		var streamConsumer = new StreamConsumer(blockStream, blockConsumers);
		streamConsumer.Finished += s => bufferPool.Release(buffer);

		return streamConsumer;
	}

}
