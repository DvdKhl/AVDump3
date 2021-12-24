using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AVDump3GUI.Controls.Settings;

public class SettingValueTemplateSelector : DataTemplateSelector {
	public Dictionary<string, DataTemplate> Templates { get; } = new Dictionary<string, DataTemplate>();

	public override DataTemplate SelectTemplate(object item, DependencyObject container) {
		var settingValueDisplay = item as SettingValueDisplay;

		var key = ((SettingValueDisplay)item).Property.ValueTypeKey;
		if(
			!Templates.TryGetValue(key, out var dataTemplate) &&
			!(Templates.TryGetValue("enum", out dataTemplate) && settingValueDisplay.Property.ValueType.IsEnum) &&
			!Templates.TryGetValue("default", out dataTemplate)
		) {
			throw new Exception("DataTemplate for key {} is missing and no default DataTemplate was provided");
		}

		return dataTemplate;

	}
}
