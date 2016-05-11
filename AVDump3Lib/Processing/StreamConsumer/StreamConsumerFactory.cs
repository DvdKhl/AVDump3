using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockBuffers.Sources;
using AVDump3Lib.Processing.BlockConsumers;
using System.IO;

namespace AVDump3Lib.Processing.StreamConsumer {
    public interface IStreamConsumerFactory {
		IStreamConsumer Create(Stream stream);
	}
	public class StreamConsumerFactory : IStreamConsumerFactory {
		private IBlockConsumerSelector blockConsumerSelector;
		private IBlockPool blockPool;

		public StreamConsumerFactory(IBlockConsumerSelector blockConsumerSelector, IBlockPool blockPool) {
			this.blockConsumerSelector = blockConsumerSelector;
			this.blockPool = blockPool;
		}

		public IStreamConsumer Create(Stream stream) {
			var blockSource = new StreamBlockSource(stream);
			var buffer = new CircularBlockBuffer(blockPool.Take(), 0/*consumerCount*/);
			var blockStream = new BlockStream(blockSource, buffer);
			var streamConsumer =  new StreamConsumer(blockStream, blockConsumerSelector.Select(blockStream));
			streamConsumer.Finished += (s, e) => blockPool.Release(buffer.Blocks);

			return streamConsumer;
		}

	}
}
