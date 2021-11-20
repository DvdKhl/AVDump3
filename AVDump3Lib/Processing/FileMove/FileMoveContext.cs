using AVDump3Lib.Information.MetaInfo.Core;
using System;

namespace AVDump3Lib.Processing.FileMove;

public class FileMoveContext {
	public FileMoveContext(Func<string, string> getHandler, IServiceProvider serviceProvider, FileMetaInfo fileMetaInfo) {
		Get = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
		ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		FileMetaInfo = fileMetaInfo ?? throw new ArgumentNullException(nameof(fileMetaInfo));
	}

	public Func<string, string> Get { get; }
	public FileMetaInfo FileMetaInfo { get; }
	public IServiceProvider ServiceProvider { get; }
}
