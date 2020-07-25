using AVDump3Lib.Information.MetaInfo.Core;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.FileMove {
	public interface IFileMoveScript {
		void Load();
		bool CanReload { get; }

		Task<string?> GetFilePathAsync(FileMetaInfo fileMetaInfo);
		bool SourceChanged();
	}
}
