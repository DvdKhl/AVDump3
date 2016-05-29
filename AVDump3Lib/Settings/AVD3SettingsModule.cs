using AVDump3Lib.Modules;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Settings {
	public interface IAVD3SettingsModule : IAVD3Module {
		event Func<IEnumerable<ArgGroup>> RegisterCommandlineArgs;
	}

	public class AVD3SettingsModule : IAVD3SettingsModule {
		public event Func<IEnumerable<ArgGroup>> RegisterCommandlineArgs;

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }

		public IEnumerable<ArgGroup> RaiseCommandlineRegistration() {
			return RegisterCommandlineArgs?.GetInvocationList()
				.Cast<Func<IEnumerable<ArgGroup>>>().Select(x => x()).SelectMany(x => x);
		}



	}
}
