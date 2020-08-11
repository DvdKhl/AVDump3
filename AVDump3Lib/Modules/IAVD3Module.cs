using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Modules {
	public class ModuleInitResult {
		public bool CancelStartup { get; private set; }
		public string Reason { get; private set; }

		public ModuleInitResult(bool cancelStartup) { CancelStartup = cancelStartup; }
		public ModuleInitResult(string cancelReason) { Cancel(cancelReason); }

		public void Cancel() { Cancel(""); }
		public void Cancel(string reason) {
			CancelStartup = true;
			Reason = reason;
		}
	}

	public interface IAVD3Module {
		void Initialize(IReadOnlyCollection<IAVD3Module> modules);
		ModuleInitResult Initialized();

		void Shutdown();
	}
}
