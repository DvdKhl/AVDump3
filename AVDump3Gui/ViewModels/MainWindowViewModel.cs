using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using AVDump3Gui.Controls.Settings;
using AVDump3Lib.Misc;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using ExtKnot.StringInvariants;

namespace AVDump3Gui.ViewModels {

	public class AVDFile {
		public FileInfo Info { get; set; }
	}


	public class SettingsPropertyViewModel : ISettingsPropertyItem {
		private readonly SettingsGroup settingsGroup;
		public SettingsProperty Base { get; }

		public SettingsPropertyViewModel(SettingsGroup settingsGroup, SettingsProperty settingProperty) {
			this.settingsGroup = settingsGroup ?? throw new ArgumentNullException(nameof(settingsGroup));
			this.Base = settingProperty ?? throw new ArgumentNullException(nameof(settingProperty));
		}

		public string Description => settingsGroup.ResourceManager.GetInvString($"{settingsGroup.Name}.{Base.Name}.Description") ?? "";
		public string Example => settingsGroup.ResourceManager.GetInvString($"{settingsGroup.Name}.{Base.Name}.Example") ?? "";


		public ImmutableArray<string> AlternativeNames => ((ISettingsProperty)Base).AlternativeNames;
		public object DefaultValue => settingsGroup.PropertyObjectToString(Base, ((ISettingsProperty)Base).DefaultValue);
		public string Name => ((ISettingsProperty)Base).Name;
		public Type ValueType => ((ISettingsProperty)Base).ValueType;
	}


	public class SettingsGroupViewModel : ISettingsGroupItem {
		private readonly SettingsGroup settingsGroup;

		public string Description => settingsGroup.ResourceManager.GetInvString($"{Name}.Description");


		public string Name => settingsGroup.Name;
		public ImmutableArray<ISettingsPropertyItem> Properties { get; set; }

		public SettingsGroupViewModel(SettingsGroup settingsGroup) {
			this.settingsGroup = settingsGroup ?? throw new ArgumentNullException(nameof(settingsGroup));
			Properties = settingsGroup.Properties.Select(x => (ISettingsPropertyItem)new SettingsPropertyViewModel(settingsGroup, x)).ToImmutableArray();
		}

		public string PropertyObjectToString(ISettingsPropertyItem prop, object objectValue) => settingsGroup.PropertyObjectToString(((SettingsPropertyViewModel)prop).Base, objectValue);
		public object PropertyStringToObject(ISettingsPropertyItem prop, string stringValue) => settingsGroup.PropertyStringToObject(((SettingsPropertyViewModel)prop).Base, stringValue);
	}



	public class MainWindowViewModel : ViewModelBase {
		private SettingsStore settingsStore = new SettingsStore(AVD3CLModuleSettings.GetGroups());
		private IEnumerable<ISettingsValueItem> settingsValues;

		public List<SettingsGroupViewModel> SettingsGroups { get; }
		public IEnumerable<ISettingsValueItem> SettingsValues {
			get => settingsValues; 
			set {
				settingsValues = value;

				foreach(var settingsValue in settingsValues) {
					var prop = ((SettingsPropertyViewModel)settingsValue.Property).Base;
					var propValue = settingsStore.GetPropertyValue(prop);

					settingsValue.Value = propValue;
				}
			}
		}

		public MainWindowViewModel() {
			settingsStore = new SettingsStore(AVD3CLModuleSettings.GetGroups());
			SettingsGroups = settingsStore.Groups.Select(x => new SettingsGroupViewModel(x)).ToList();

			var parseResult = CLSettingsHandler.ParseArgs(settingsStore.Groups, Environment.GetCommandLineArgs());

			foreach(var settingValue in parseResult.SettingValues) {
				settingsStore.SetPropertyValue(settingValue.Key, settingValue.Value);
			}

			Files = new List<AVDFile>();
			FileTraversal.Traverse(parseResult.UnnamedArgs, true, filePath => {
				Files.Add(new AVDFile() { Info = new FileInfo(filePath) });
			}, e => { });

		}

		public List<AVDFile> Files { get; }



	}
}
