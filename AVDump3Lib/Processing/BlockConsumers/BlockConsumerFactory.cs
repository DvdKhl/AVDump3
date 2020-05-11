using AVDump3Lib.Processing.BlockBuffers;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace AVDump3Lib.Processing.BlockConsumers {
	public interface IBlockConsumerFactory {
		string Name { get; }
		string Description { get; }
		IBlockConsumer Create(IBlockStreamReader reader);
	}

	public class BlockConsumerSetup {
		public BlockConsumerSetup(string name, IBlockStreamReader reader) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Reader = reader ?? throw new ArgumentNullException(nameof(reader));
		}

		public string Name { get; }
		public IBlockStreamReader Reader { get; }
	}

	public delegate IBlockConsumer CreateBlockConsumer(BlockConsumerSetup setup);

	public class BlockConsumerFactory : IBlockConsumerFactory {
		private readonly CreateBlockConsumer createBlockConsumer;

		public BlockConsumerFactory(string name, CreateBlockConsumer createBlockConsumer) {
			Name = name;
			this.createBlockConsumer = createBlockConsumer;
		}

		public virtual string Description {
			get {
				var description = Lang.ResourceManager.GetInvString(Name.InvReplace("-", "") + "ConsumerDescription");
				return !string.IsNullOrEmpty(description) ? description : "<NoDescriptionGiven>";
			}
		}
		public string Name { get; }

		public IBlockConsumer Create(IBlockStreamReader reader) {
			return createBlockConsumer(new BlockConsumerSetup(Name, reader));
		}
	}
}
