using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using ExtKnot.StringInvariants;

namespace AVDump3Gui.ViewModels {

	public class AVDFile {
		public FileInfo Info { get; set; }
	}


	public class SettingsPropertyViewModel : ISettingsPropertyItem {
		private readonly SettingsGroup settingsGroup;
		private readonly SettingsProperty settingProperty;

		public SettingsPropertyViewModel(SettingsGroup settingsGroup, SettingsProperty settingProperty) {
			this.settingsGroup = settingsGroup ?? throw new ArgumentNullException(nameof(settingsGroup));
			this.settingProperty = settingProperty ?? throw new ArgumentNullException(nameof(settingProperty));
		}

		public string Description => settingsGroup.ResourceManager.GetInvString($"{settingsGroup.Name}.{settingProperty.Name}.Description") ?? "";
		public string Example => settingsGroup.ResourceManager.GetInvString($"{settingsGroup.Name}.{settingProperty.Name}.Example") ?? "";


		public ImmutableArray<string> AlternativeNames => ((ISettingsProperty)settingProperty).AlternativeNames;
		public object DefaultValue => settingsGroup.PropertyObjectToString(settingProperty, ((ISettingsProperty)settingProperty).DefaultValue);
		public string Name => ((ISettingsProperty)settingProperty).Name;
		public Type ValueType => ((ISettingsProperty)settingProperty).ValueType;
	}


	public class SettingsGroupViewModel : ISettingsGroupItem {
		private readonly SettingsGroup settingsGroup;

		public string Description => settingsGroup.ResourceManager.GetInvString($"{Name}.Description");


		public string Name => settingsGroup.Name;
		public ImmutableArray<SettingsPropertyViewModel> Properties { get; set; }


		public SettingsGroupViewModel(SettingsGroup settingsGroup) {
			this.settingsGroup = settingsGroup ?? throw new ArgumentNullException(nameof(settingsGroup));
			Properties = settingsGroup.Properties.Select(x => new SettingsPropertyViewModel(settingsGroup, x)).ToImmutableArray();
		}
	}

	public class MainWindowViewModel : ViewModelBase {

		public List<AVDFile> Files { get; } = new List<AVDFile>() { new AVDFile { Info = new FileInfo(@"Z:\Media\Renamed\Aiura\Aiura 01 - The Day Before [Commie][HDTV][01d20eb9][1223248].mkv") } };



		public List<SettingsGroupViewModel> SettingGroups { get; } = new SettingsStore(AVD3CLModuleSettings.GetGroups()).Groups.Select(x => new SettingsGroupViewModel(x)).ToList();
	}
}
