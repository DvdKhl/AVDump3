using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Settings.Core {
	public class SettingsGroupStore {
		private readonly string groupName;
		private readonly SettingsStore store;
		private readonly Dictionary<string, SettingsProperty> properties = new Dictionary<string, SettingsProperty>();

		public SettingsGroupStore(string groupName, SettingsStore store) {
			this.groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
			this.store = store ?? throw new ArgumentNullException(nameof(store));

			properties = store.Groups.SelectMany(g => g.Properties.Select(p => (g, p))).ToDictionary(x => $"{x.g.Name}.{x.p.Name}", x => x.p);
		}


		protected void SetValue(object? value, [CallerMemberName]string propertyName = "") => store.SetPropertyValue(properties[$"{groupName}.{propertyName}"], value);
		protected object? GetValue([CallerMemberName] string propertyName = "") => store.GetPropertyValue(properties[$"{groupName}.{propertyName}"]);
		protected object GetRequiredValue([CallerMemberName] string propertyName = "") => GetValue(propertyName) ?? throw new Exception("Required value was null");
	}
}
