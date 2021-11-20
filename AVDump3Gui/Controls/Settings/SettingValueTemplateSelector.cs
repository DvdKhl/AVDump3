using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace AVDump3Gui.Controls.Settings;

public class SettingValueTemplateSelector : IDataTemplate {
	public bool SupportsRecycling => false;
	[Content]
	public Dictionary<string, IDataTemplate> Templates { get; } = new Dictionary<string, IDataTemplate>();

	public IControl Build(object data) {
		var settingValueDisplay = data as SettingValueDisplay;

		var key = ((SettingValueDisplay)data).Property.ValueTypeKey;
		if(
			!Templates.TryGetValue(key, out var dataTemplate) &&
			!(Templates.TryGetValue("enum", out dataTemplate) && settingValueDisplay.Property.ValueType.IsEnum) &&
			!Templates.TryGetValue("default", out dataTemplate)
		) {
			throw new Exception("DataTemplate for key {} is missing and no default DataTemplate was provided");
		}

		return dataTemplate.Build(data);
	}

	public bool Match(object data) => data is SettingValueDisplay;
}
