using AVDump3Lib.Settings.Core;

namespace AVDump3Gui.Controls.Settings;

public enum SettingValueDisplayType {
	Default, Current, Active
}
public class SettingValueDisplay {
	public SettingValueDisplay(ISettingPropertyItem settingProperty, SettingValueDisplayType type) {
		Property = settingProperty;
		Type = type;
	}

	public ISettingPropertyItem Property { get; private set; }
	public SettingValueDisplayType Type { get; private set; }

	public object Value {
		get => Type switch {
			SettingValueDisplayType.Default => Property.DefaultValue,
			SettingValueDisplayType.Current => Property.ValueRaw == ISettingStore.Unset ? null : Property.ValueRaw,
			SettingValueDisplayType.Active => Property.Value,
			_ => null
		};
		set {
			if(Type == SettingValueDisplayType.Current) {
				Property.ValueRaw = value;
			}
		}
	}

	public bool IsReadOnly => Type != SettingValueDisplayType.Current;

	public string ValueAsString {
		get => Property.ToString(Value);
		set => Property.Value = Property.ToObject(value);
	}

}
