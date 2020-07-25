using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AVDump3Lib.Settings.Core {
	public class SettingsStore {
		private readonly Dictionary<SettingsProperty, object?> values = new Dictionary<SettingsProperty, object?>();

		public ImmutableArray<SettingsGroup> Groups { get; }

		public object? GetPropertyValue(SettingsProperty property) => values.TryGetValue(property, out var value) ? value : (property ?? throw new ArgumentNullException(nameof(property))).DefaultValue;
		public void SetPropertyValue(SettingsProperty property, object? value) => values[property] = value;

		public SettingsStore(IEnumerable<SettingsGroup> groups) {
			Groups = groups.ToImmutableArray();
		}
	}
}
