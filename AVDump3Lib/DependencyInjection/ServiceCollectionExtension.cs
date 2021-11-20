using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using Microsoft.Extensions.DependencyInjection;

namespace AVDump3Lib.DependencyInjection;

public static class ServiceCollectionExtension {
	public static IServiceCollection AddAVDump3(this IServiceCollection serviceCollection) {
		serviceCollection.AddTransient<IStreamProvider, StreamFromPathsProvider>();
		serviceCollection.AddSingleton<IMirroredBufferPool, MirroredBufferPool>();
		serviceCollection.AddTransient<IStreamConsumerFactory, StreamConsumerFactory>();
		serviceCollection.AddTransient<IStreamConsumerCollection, StreamConsumerCollection>();

		return serviceCollection;
	}
}
