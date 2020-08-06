using AVDump3Lib.Modules;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace AVDump3Lib.Settings {
	public interface IAVD3SettingsModule : IAVD3Module {
		void RegisterSettings(IEnumerable<ISettingProperty> settingProperties);
		event EventHandler<SettingsModuleInitResult> ConfigurationFinished;
	}

	public class SettingsModuleInitResult : ModuleInitResult {
		public ISettingStore Store { get;  }

		public SettingsModuleInitResult(ISettingStore store) : base(false) {
			Store = store ?? throw new ArgumentNullException(nameof(store));
		}
	}


	public class AVD3SettingsModule : IAVD3SettingsModule {
		public event EventHandler<SettingsModuleInitResult> ConfigurationFinished = delegate { };

		private List<ISettingProperty> settingsGroups = new List<ISettingProperty>();

		public IReadOnlyList<ISettingProperty> SettingProperties { get; private set; }
		
		public ISettingStore Store { get; private set; }


		public ISettingStore BuildStore() => Store = new SettingStore(settingsGroups.ToImmutableArray());

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }

		public ModuleInitResult Initialized() {
			var args = new SettingsModuleInitResult(Store);
			ConfigurationFinished?.Invoke(this, args);
			return args;
		}

		public AVD3SettingsModule() {
			Store = new SettingStore(ImmutableArray<ISettingProperty>.Empty);

			SettingProperties = settingsGroups.AsReadOnly();

		}

		public void RegisterSettings(IEnumerable<ISettingProperty> settingsGroups) => this.settingsGroups.AddRange(settingsGroups);
	}
}
