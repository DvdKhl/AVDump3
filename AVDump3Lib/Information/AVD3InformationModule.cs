using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AVDump3Lib.Information {
	public interface IAVD3InformationModule : IAVD3Module {
		IReadOnlyCollection<IInfoProviderFactory> InfoProviderFactories { get; }
	}
	public class AVD3InformationModule : IAVD3InformationModule {
		private List<IInfoProviderFactory> infoProviderFactories;

		public IReadOnlyCollection<IInfoProviderFactory> InfoProviderFactories { get; }


		public AVD3InformationModule() {
			infoProviderFactories = new List<IInfoProviderFactory> {
				new InfoProviderFactory(setup => new MatroskaProvider(setup.BlockConsumers.OfType<MatroskaParser>().FirstOrDefault()?.Info)),
				new InfoProviderFactory(setup => new OggProvider(setup.BlockConsumers.OfType<OggParser>().FirstOrDefault()?.Info)),
				new InfoProviderFactory(setup => new FormatInfoProvider(setup.FilePath)),
				new InfoProviderFactory(setup => new MediaInfoLibProvider(setup.FilePath)),
				new InfoProviderFactory(setup => new HashProvider(setup.BlockConsumers.OfType<HashCalculator>())),
			};

			InfoProviderFactories = infoProviderFactories.AsReadOnly();
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		}

        public void AfterConfiguration() {        }
    }
}
