using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.FileMove;

public interface IMoveScript {
	string GetFilePath(FileMoveContext ctx);
}

public class FileMoveScriptByAssembly : FileMoveScript {
	private readonly FileInfo assemblyFileInfo;

	public FileMoveScriptByAssembly(IEnumerable<IFileMoveConfigure> extensions, string filePath) : base(extensions) {
		if(string.IsNullOrEmpty(filePath)) throw new ArgumentException("Parameter may not be null or empty", nameof(filePath));
		assemblyFileInfo = new FileInfo(filePath);
	}

	public override bool CanReload { get; } = true;
	public override Task<string?> ExecuteInternalAsync(FileMoveContext ctx) => throw new NotImplementedException();
	public override void Load() => throw new NotImplementedException();
	public override bool SourceChanged() {
		var lastWriteTime = assemblyFileInfo.LastWriteTime;
		assemblyFileInfo.Refresh();
		return lastWriteTime != assemblyFileInfo.LastWriteTime;
	}
}
