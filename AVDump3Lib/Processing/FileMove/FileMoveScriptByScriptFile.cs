using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.FileMove {
	public class FileMoveScriptByScriptFile : FileMoveScript {
		private readonly ScriptOptions scriptOptions;
		private readonly FileInfo scriptFileInfo;
		private readonly Converter<string, string>? preprocessing;
		private ScriptRunner<string>? scriptRunner;

		public FileMoveScriptByScriptFile(IEnumerable<IFileMoveConfigure> extensions, string filePath, Converter<string, string>? preprocessing = null) : base(extensions) {
			if(string.IsNullOrEmpty(filePath)) throw new ArgumentException("Parameter may not be null or empty", nameof(filePath));
			scriptFileInfo = new FileInfo(filePath);
			this.preprocessing = preprocessing;

			scriptOptions = ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies());
		}

		public override bool CanReload { get; } = true;

		public override async Task<string?> ExecuteInternalAsync(FileMoveContext ctx) {
			if(scriptRunner == null) return null;

			var dstFilePath = await scriptRunner(ctx);
			return dstFilePath;
		}

		public override void Load() {
			var code = File.ReadAllText(scriptFileInfo.FullName);
			if(preprocessing != null) code = preprocessing(code);

			var fileMoveScript = CSharpScript.Create<string>(code, scriptOptions, typeof(FileMoveContext));
			scriptRunner = fileMoveScript.CreateDelegate();
		}

		public override bool SourceChanged() {
			var lastWriteTime = scriptFileInfo.LastWriteTime;
			scriptFileInfo.Refresh();
			return lastWriteTime != scriptFileInfo.LastWriteTime;
		}
	}
}
