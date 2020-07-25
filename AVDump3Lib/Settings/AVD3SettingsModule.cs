using AVDump3Lib.Modules;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Settings {
	public interface IAVD3SettingsModule : IAVD3Module {
		void RegisterSettings(IEnumerable<SettingsGroup> settingsGroups);
		event EventHandler<SettingsModuleInitResult> ConfigurationFinished;
	}

	public class SettingsModuleInitResult : ModuleInitResult {
		public SettingsStore Store { get;  }

		public SettingsModuleInitResult(SettingsStore store) : base(false) {
			Store = store ?? throw new ArgumentNullException(nameof(store));
		}
	}


	public class AVD3SettingsModule : IAVD3SettingsModule {
		public event EventHandler<SettingsModuleInitResult> ConfigurationFinished = delegate { };

		private List<SettingsGroup> settingsGroups = new List<SettingsGroup>();
		public IReadOnlyList<SettingsGroup> SettingsGroups { get; private set; }
		public SettingsStore Store { get; private set; }


		public SettingsStore BuildStore() => Store = new SettingsStore(SettingsGroups);

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }

		public ModuleInitResult Initialized() {
			var args = new SettingsModuleInitResult(Store);
			ConfigurationFinished?.Invoke(this, args);
			return args;
		}

		public AVD3SettingsModule() {
			SettingsGroups = settingsGroups.AsReadOnly();
		}

		public void RegisterSettings(IEnumerable<SettingsGroup> settingsGroups) => this.settingsGroups.AddRange(settingsGroups);
	}
}
