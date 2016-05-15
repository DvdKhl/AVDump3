using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockConsumers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Processing.BlockConsumers {
	public interface IBlockConsumerSelector {
		IEnumerable<IBlockConsumer> Select(IBlockStream blockStream);
	}

	public class BlockConsumerSelectorEventArgs : EventArgs {
		public string Name { get; private set; }
		public bool Select { get; set; }

		public BlockConsumerSelectorEventArgs(string name) {
			Name = name;
		}
	}


	public class BlockConsumerSelector : IBlockConsumerSelector {
		private IBlockConsumerFactory[] blockConsumerFactories;

		public event EventHandler<BlockConsumerSelectorEventArgs> Filter;

		public BlockConsumerSelector(IEnumerable<IBlockConsumerFactory> blockConsumerFactories) {
			this.blockConsumerFactories = blockConsumerFactories.ToArray();
		}

		public IEnumerable<IBlockConsumer> Select(IBlockStream blockStream) {
			int readerIndex = 0;
			for(int i = 0; i < blockConsumerFactories.Length; i++) {
				var args = new BlockConsumerSelectorEventArgs(blockConsumerFactories[i].Name);
				Filter?.Invoke(this, args);
				if(!args.Select) continue;

				var blockReader = new BlockStreamReader(blockStream, readerIndex++);
				yield return blockConsumerFactories[i].Create(blockReader);
			}
		}
	}
}
