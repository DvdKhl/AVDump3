using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Processing.BlockConsumers {
	public interface IBlockConsumerSelector {
		event EventHandler<BlockConsumerSelectorEventArgs> Filter;
		IEnumerable<IBlockConsumerFactory> Select();
		IEnumerable<IBlockConsumer> Create(IEnumerable<IBlockConsumerFactory> factories, IBlockStream blockStream);
	}

	public class BlockConsumerSelectorEventArgs : EventArgs {
		public string Name { get; private set; }
		public bool Select { get; set; }

		public BlockConsumerSelectorEventArgs(string name) {
			Name = name;
		}
	}


	public class BlockConsumerSelector : IBlockConsumerSelector {
		private readonly IBlockConsumerFactory[] blockConsumerFactories;

		public event EventHandler<BlockConsumerSelectorEventArgs> Filter;

		public BlockConsumerSelector(IEnumerable<IBlockConsumerFactory> blockConsumerFactories) {
			this.blockConsumerFactories = blockConsumerFactories.ToArray();
		}

		public IEnumerable<IBlockConsumerFactory> Select() {
			for(var i = 0; i < blockConsumerFactories.Length; i++) {
				var args = new BlockConsumerSelectorEventArgs(blockConsumerFactories[i].Name);
				Filter?.Invoke(this, args);
				if(!args.Select) continue;

				yield return blockConsumerFactories[i];
			}
		}
		public IEnumerable<IBlockConsumer> Create(IEnumerable<IBlockConsumerFactory> factories, IBlockStream blockStream) {
			var readerIndex = 0;
			foreach(var factory in factories) {
				var blockReader = new BlockStreamReader(blockStream, readerIndex++);
				yield return factory.Create(blockReader);
			}
		}
	}
}
