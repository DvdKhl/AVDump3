using AVDump3Lib.Modules;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Settings {
	public interface IAVD3SettingsModule : IAVD3Module {
		void RegisterSettings(SettingsObject settingsObject);
		event EventHandler<ModuleInitResult> AfterConfiguration;
	}


	public class AVD3SettingsModule : IAVD3SettingsModule {
		public event EventHandler<ModuleInitResult> AfterConfiguration;

		private ICLSettingsHandler settingsHandler;
		private List<SettingsObject> settingsObjects = new List<SettingsObject>();
		public AVD3SettingsModule(ICLSettingsHandler handler) {
			settingsHandler = handler;
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }

		public ModuleInitResult Initialized() {
			var args = new ModuleInitResult(false);
			AfterConfiguration?.Invoke(this, args);
			return args;
		}

		public void RegisterSettings(SettingsObject settingsObject) => settingsHandler.Register(settingsObject);
	}
}
