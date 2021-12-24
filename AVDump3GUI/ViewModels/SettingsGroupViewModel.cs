using AVDump3Lib.Settings.Core;
using AVDump3GUI.Controls.Settings;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Resources;

namespace AVDump3GUI.ViewModels;

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
