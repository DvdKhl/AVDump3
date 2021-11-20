using AVDump3Lib.Processing.BlockBuffers;
using ExtKnot.StringInvariants;

namespace AVDump3Lib.Processing.BlockConsumers;

public interface IBlockConsumerFactory {
	string Name { get; }
	string Description { get; }
	IBlockConsumer Create(IBlockStreamReader reader, object tag);
}

public class BlockConsumerSetup {
	public BlockConsumerSetup(string name, IBlockStreamReader reader, object tag) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Reader = reader ?? throw new ArgumentNullException(nameof(reader));
		Tag = tag;
	}

	public string Name { get; }
	public IBlockStreamReader Reader { get; }
	public object Tag { get; }
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
			var description = Lang.ResourceManager.GetInvString("Consumer." + Name + ".Description");
			return !string.IsNullOrEmpty(description) ? description : "<NoDescriptionGiven>";
		}
	}
	public string Name { get; }

	public IBlockConsumer Create(IBlockStreamReader reader, object tag) {
		return createBlockConsumer(new BlockConsumerSetup(Name, reader, tag));
	}
}
