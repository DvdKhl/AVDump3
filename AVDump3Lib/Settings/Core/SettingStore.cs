using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AVDump3Lib.Settings.Core {
	public class SettingStore : ISettingStore {
		private readonly Dictionary<ISettingProperty, object?> values = new();

		public SettingStore(ImmutableArray<ISettingProperty> settingProperties) => SettingProperties = settingProperties;

		public ImmutableArray<ISettingProperty> SettingProperties { get; }

		public object? GetRawPropertyValue(ISettingProperty settingProperty) => values.TryGetValue(settingProperty, out var value) ? value : ISettingStore.Unset;
		public object? GetPropertyValue(ISettingProperty settingProperty) => values.TryGetValue(settingProperty, out var value) ? value : (settingProperty ?? throw new ArgumentNullException(nameof(settingProperty))).DefaultValue;
		public bool ContainsProperty(ISettingProperty settingProperty) => values.ContainsKey(settingProperty);
		public void SetPropertyValue(ISettingProperty settingProperty, object? value) {
			if(value == ISettingStore.Unset) {
				values.Remove(settingProperty);
			} else {
				values[settingProperty] = value;
			}
		}
	}
}
