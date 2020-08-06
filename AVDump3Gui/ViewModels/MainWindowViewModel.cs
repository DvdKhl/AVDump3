using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using AVDump3Gui.Controls.Settings;
using AVDump3Lib;
using AVDump3Lib.Misc;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using ExtKnot.StringInvariants;
using ReactiveUI;

namespace AVDump3Gui.ViewModels {

	public class AVDFile {
		public FileInfo Info { get; set; }
	}


	public class SettingsPropertyViewModel : ISettingPropertyItem {
		public ResourceManager ResourceManager { get; }
		public ISettingStore SettingStore { get; }
		public ISettingProperty Base { get; }

		public SettingsPropertyViewModel(ISettingProperty settingProperty, ISettingStore settingStore, ResourceManager resourceManager) {
			Base = settingProperty ?? throw new ArgumentNullException(nameof(settingProperty));
			SettingStore = settingStore ?? throw new ArgumentNullException(nameof(settingStore));

			ResourceManager = resourceManager;
		}

		public string Description => ResourceManager.GetInvString($"{Base.Group.FullName}.{Base.Name}.Description") ?? "";
		public string Example => ResourceManager.GetInvString($"{Base.Group.FullName}.{Base.Name}.Example") ?? "";


		public ImmutableArray<string> AlternativeNames => Base.AlternativeNames;
		public string Name => Base.Name;
		public Type ValueType => Base.ValueType;
		public string ValueTypeKey => Base.ValueType.Name;

		public object DefaultValue => Base.DefaultValue;
		public object ValueRaw { get => SettingStore.GetRawPropertyValue(Base); set => SettingStore.SetPropertyValue(Base, value); }
		public object Value { get => SettingStore.GetPropertyValue(Base); set => SettingStore.SetPropertyValue(Base, value); }

		public object ToObject(string stringValue) => Base.ToObject(stringValue);
		public string ToString(object objectValue) => Base.ToString(objectValue);
	}


	public class SettingsGroupViewModel : ISettingGroupItem {
		public string Description => ResourceManager.GetInvString($"{Name}.Description");

		public ResourceManager ResourceManager { get; }
		public ISettingGroup Base { get; }

		public string Name => Base.Name;
		public ImmutableArray<ISettingPropertyItem> Properties { get; set; }

		public SettingsGroupViewModel(IEnumerable<SettingsPropertyViewModel> settingPropertyItems, ResourceManager resourceManager) {
			if(settingPropertyItems.Any(x => x.Base.Group != settingPropertyItems.First().Base.Group)) throw new Exception();

			Base = settingPropertyItems.First().Base.Group;
			Properties = settingPropertyItems.Cast<ISettingPropertyItem>().ToImmutableArray();
			ResourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
		}

	}


	public class MainWindowViewModel : ViewModelBase {
		private ISettingStore fileSettingStore;
		private ISettingStore userSettingStore;


		public List<SettingsGroupViewModel> SettingGroups { get; }
		public AVD3UIControl AVD3Control { get; }


		public MainWindowViewModel() {
			var props = AVD3GUISettings.GetProperties().ToImmutableArray();

			userSettingStore = new SettingStore(props);


			SettingGroups = props.GroupBy(x => x.Group).Select(x => new SettingsGroupViewModel(x.Select(y => new SettingsPropertyViewModel(y, userSettingStore, AVD3GUISettings.ResourceManager)), AVD3GUISettings.ResourceManager)).ToList();


			//AVD3Control = new AVD3UIControl(new AVD3ControlSettings(null, null, 64 << 20, 1 << 20, 8 << 20));


			var parseResult = CLSettingsHandler.ParseArgs(props, Environment.GetCommandLineArgs());

			foreach(var settingValue in parseResult.SettingValues) {
				userSettingStore.SetPropertyValue(settingValue.Key, settingValue.Value);
			}

			Files = new List<AVDFile>();
			FileTraversal.Traverse(parseResult.UnnamedArgs, true, filePath => {
				Files.Add(new AVDFile() { Info = new FileInfo(filePath) });
			}, e => { });

		}

		public List<AVDFile> Files { get; }



	}
}
