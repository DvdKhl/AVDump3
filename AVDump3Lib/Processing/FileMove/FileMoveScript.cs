using AVDump3Lib.Information.MetaInfo.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.FileMove {
	public delegate string AVDTokenHandler(string key, FileMoveContext ctx);

	public abstract class FileMoveScript : IFileMoveScript {
		private readonly IServiceProvider serviceProvider;
		private readonly List<AVDTokenHandler> tokenHandlers = new List<AVDTokenHandler>();

		public abstract void Load();
		public abstract bool CanReload { get; }

		public abstract Task<string?> ExecuteInternalAsync(FileMoveContext ctx);

		public async Task<string?> GetFilePathAsync(FileMetaInfo fileMetaInfo) {
			FileMoveContext? ctx = null;
			string TokenHandler(string key) => tokenHandlers.Select(x => x.Invoke(key, ctx)).FirstOrDefault(x => x != null);
			ctx = new FileMoveContext(TokenHandler, serviceProvider, fileMetaInfo);

			var destFilePath = "";
			try {
				destFilePath = await ExecuteInternalAsync(ctx).ConfigureAwait(false);
				if(destFilePath == null) return null;
				destFilePath = Path.GetFullPath(destFilePath);

			} catch(Exception) {
				destFilePath = null;
			}

			return destFilePath;
		}

		public virtual bool SourceChanged() => false;

		public FileMoveScript(IEnumerable<IFileMoveConfigure> extensions) {
			var serviceCollection = new ServiceCollection();

			foreach(var extension in extensions) {
				extension.ConfigureServiceCollection(serviceCollection);
				tokenHandlers.Add(extension.ReplaceToken);
			}

			serviceProvider = serviceCollection.BuildServiceProvider();
		}

	}
}
