using AVDump3Lib.BlockConsumers;
using AVDump3Lib.Information.MetaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information.InfoProvider {
	public interface IInfoProviderFactory {
		MetaDataProvider Create(InfoProviderSetup setup);
	}

	public class InfoProviderSetup {
		public InfoProviderSetup(string filePath, IReadOnlyCollection<IBlockConsumer> blockConsumers) {
			FilePath = filePath;
			BlockConsumers = blockConsumers;
		}

		public string FilePath { get; }
		public IReadOnlyCollection<IBlockConsumer> BlockConsumers { get; }
	}

	public delegate MetaDataProvider CreateInfoProvider(InfoProviderSetup reader);

	public class InfoProviderFactory: IInfoProviderFactory {
		private CreateInfoProvider createInfoProvider;


		public InfoProviderFactory(CreateInfoProvider createInfoProvider) {
			this.createInfoProvider = createInfoProvider;
		}

		public string Name { get; }
		public MetaDataProvider Create(InfoProviderSetup setup) {
			return createInfoProvider(setup);
		}
	}
}
