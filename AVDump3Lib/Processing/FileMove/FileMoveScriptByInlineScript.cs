using AVDump3Lib.Information.MetaInfo.Core;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.FileMove {
	public class FileMoveScriptByInlineScript : FileMoveScript {
		private readonly string script;
		private ScriptRunner<string>? scriptRunner;

		public FileMoveScriptByInlineScript(IEnumerable<IFileMoveConfigure> extensions, string script) : base(extensions) {
			if(string.IsNullOrEmpty(script)) throw new ArgumentException("Parameter may not be null or empty", nameof(script));
			this.script = script;
		}

		public override bool CanReload { get; } = false;

		public override async Task<string?> ExecuteInternalAsync(FileMoveContext ctx) {
			if(scriptRunner == null) return null;

			var dstFilePath = await scriptRunner(ctx);
			return dstFilePath;
		}

		public override void Load() {
			var fileMoveScript = CSharpScript.Create<string>(script, ScriptOptions.Default.WithReferences(AppDomain.CurrentDomain.GetAssemblies()), typeof(FileMoveContext));
			scriptRunner = fileMoveScript.CreateDelegate();
		}
	}
}
