using AVDump3Lib.Processing.BlockBuffers;

namespace AVDump3Lib.Processing.BlockConsumers {
	public interface IBlockConsumerFactory {
		string Name { get; }
		IBlockConsumer Create(IBlockStreamReader reader);
	}

	public delegate IBlockConsumer CreateBlockConsumer(IBlockStreamReader reader);

	public class BlockConsumerFactory : IBlockConsumerFactory {
		private CreateBlockConsumer createBlockConsumer;

		public BlockConsumerFactory(string name, CreateBlockConsumer createBlockConsumer) {
			Name = name;
			this.createBlockConsumer = createBlockConsumer;
		}

		public string Name { get; }
		public IBlockConsumer Create(IBlockStreamReader reader) {
			return createBlockConsumer(reader);
		}
	}
}
