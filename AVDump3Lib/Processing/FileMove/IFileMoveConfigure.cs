using AVDump3Lib.Information.MetaInfo.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AVDump3Lib.Processing.FileMove {
	public interface IFileMoveConfigure {
		void ConfigureServiceCollection(IServiceCollection services);
		string ReplaceToken(string key, FileMoveContext ctx);
	}
}
