using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockBuffers.Sources;
using AVDump3Lib.Processing.BlockConsumers;
using System.IO;
using System.Linq;

namespace AVDump3Lib.Processing.StreamConsumer {
	public interface IStreamConsumerFactory {
		IStreamConsumer Create(Stream stream);
	}
	public class StreamConsumerFactory : IStreamConsumerFactory {
		private IBlockConsumerSelector blockConsumerSelector;
		private IMirroredBufferPool bufferPool;

		public StreamConsumerFactory(IBlockConsumerSelector blockConsumerSelector, IMirroredBufferPool bufferPool) {
			this.blockConsumerSelector = blockConsumerSelector;
			this.bufferPool = bufferPool;
		}

		public IStreamConsumer Create(Stream stream) {
			var blockConsumerFactories = blockConsumerSelector.Select();
			if(!blockConsumerFactories.Any()) {
				return null;
			}

			var blockSource = new StreamBlockSource(stream);

            var buffer = bufferPool.Take();
            var circularBuffer = new CircularBuffer64Bit(buffer, blockConsumerFactories.Count()); //TODO CircularBuffer64Bit CircularBuffer32Bit
            var blockStream = new BlockStream(blockSource, circularBuffer);
			var blockConsumers = blockConsumerSelector.Create(blockConsumerFactories, blockStream).ToArray();

			var streamConsumer =  new StreamConsumer(blockStream, blockConsumers);
			streamConsumer.Finished += s => bufferPool.Release(buffer);

			return streamConsumer;
		}

	}
}
