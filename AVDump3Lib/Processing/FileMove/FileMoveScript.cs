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
		private readonly List<AVDTokenHandler> tokenHandlers = new();

		public abstract void Load();
		public abstract bool CanReload { get; }

		public abstract Task<string?> ExecuteInternalAsync(FileMoveContext ctx);

		public Task<string?> GetFilePathAsync(FileMetaInfo fileMetaInfo) {
			return GetFilePathInternalAsync(fileMetaInfo, serviceProvider);
		}
		private async Task<string?> GetFilePathInternalAsync(FileMetaInfo fileMetaInfo, IServiceProvider serviceProvider) {
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

		public IFileMoveScript CreateScope() {
			var serviceScope = serviceProvider.CreateScope();

			return new FileMoveScriptScoped(this, serviceScope);
		}

		public void Dispose() {
			GC.SuppressFinalize(this);
		}

		private class FileMoveScriptScoped : IFileMoveScript {
			private readonly FileMoveScript parent;
			private readonly IServiceScope serviceScope;

			public FileMoveScriptScoped(FileMoveScript parent, IServiceScope serviceScope) {
				this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
				this.serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
			}

			public bool CanReload => parent.CanReload;

			public IFileMoveScript CreateScope() { throw new NotSupportedException(); }

			public void Dispose() => serviceScope.Dispose();

			public Task<string?> GetFilePathAsync(FileMetaInfo fileMetaInfo) {
				return parent.GetFilePathInternalAsync(fileMetaInfo, serviceScope.ServiceProvider);
			}

			public void Load() {
				parent.Load();
			}

			public bool SourceChanged() {
				return parent.SourceChanged();
			}
		}
	}
}
