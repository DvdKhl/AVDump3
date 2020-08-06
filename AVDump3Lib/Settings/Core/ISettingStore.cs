using System.Collections.Immutable;

namespace AVDump3Lib.Settings.Core {
	public interface ISettingStore {
		static object Unset { get; } = new object();

		ImmutableArray<ISettingProperty> SettingProperties { get; }

		object? ContainsProperty(ISettingProperty settingProperty);
		object? GetPropertyValue(ISettingProperty settingProperty);
		object? GetRawPropertyValue(ISettingProperty settingProperty);
		void SetPropertyValue(ISettingProperty settingProperty, object? value);
	}
}
