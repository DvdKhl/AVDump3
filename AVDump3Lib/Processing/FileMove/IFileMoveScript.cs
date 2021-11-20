using AVDump3Lib.Information.MetaInfo.Core;

namespace AVDump3Lib.Processing.FileMove;

public interface IFileMoveScript : IDisposable {
	void Load();
	bool CanReload { get; }

	Task<string?> GetFilePathAsync(FileMetaInfo fileMetaInfo);
	bool SourceChanged();
	IFileMoveScript CreateScope();
}
