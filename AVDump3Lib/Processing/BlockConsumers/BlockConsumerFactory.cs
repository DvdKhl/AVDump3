using AVDump3Lib.Processing.BlockBuffers;

namespace AVDump3Lib.Processing.BlockConsumers {
	public interface IBlockConsumerFactory {
		string Name { get; }
		string Description { get; }
		IBlockConsumer Create(IBlockStreamReader reader);
	}

	public delegate IBlockConsumer CreateBlockConsumer(IBlockStreamReader reader);

	public class BlockConsumerFactory : IBlockConsumerFactory {
		private CreateBlockConsumer createBlockConsumer;

		public BlockConsumerFactory(string name, CreateBlockConsumer createBlockConsumer) {
			Name = name;
			this.createBlockConsumer = createBlockConsumer;
		}

		public virtual string Description {
			get {
				var description = Lang.ResourceManager.GetString(Name.Replace("-", "") + "ConsumerDescription");
				return !string.IsNullOrEmpty(description) ? description : "<NoDescriptionGiven>";
			}
		}
		public string Name { get; }

		public IBlockConsumer Create(IBlockStreamReader reader) {
			return createBlockConsumer(reader);
		}
	}
}
