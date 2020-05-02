using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing {
	public interface IAVD3ProcessingModule : IAVD3Module {
		CPUInstructions AvailableSIMD { get; }

		IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

		event EventHandler<BlockConsumerFilterEventArgs> BlockConsumerFilter;
		IStreamConsumerCollection CreateStreamConsumerCollection(IStreamProvider streamProvider, int bufferLength, int minProducerReadLength, int maxProducerReadLength);
	}

	public static class AVD3ProcessingModuleService {
		public static IServiceCollection AddAVD3ProcessingModule(this IServiceCollection services) {




			return services;
		}
	}
}
