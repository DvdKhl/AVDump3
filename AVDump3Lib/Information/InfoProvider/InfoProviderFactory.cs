using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information.InfoProvider {
	public interface IInfoProviderFactory {
        Type ProviderType { get; }

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


		public InfoProviderFactory(Type providerType, CreateInfoProvider createInfoProvider) {
            ProviderType = providerType;
            this.createInfoProvider = createInfoProvider;
		}

		public Type ProviderType { get; }
		public MetaDataProvider Create(InfoProviderSetup setup) {
			return createInfoProvider(setup);
		}
	}
}
