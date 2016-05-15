﻿using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockBuffers.Sources;
using AVDump3Lib.Processing.BlockConsumers;
using System.IO;
using System.Linq;

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
			var buffer = new CircularBlockBuffer(blockPool.Take());
			var blockStream = new BlockStream(blockSource, buffer);
			var blockConsumers = blockConsumerSelector.Select(blockStream).ToArray();
			buffer.SetConsumerCount(blockConsumers.Length);

			var streamConsumer =  new StreamConsumer(blockStream, blockConsumers);
			streamConsumer.Finished += s => blockPool.Release(buffer.Blocks);

			return streamConsumer;
		}

	}
}
