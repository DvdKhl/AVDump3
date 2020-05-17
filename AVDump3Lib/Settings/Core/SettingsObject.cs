using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
	public class SettingsObject {
		private readonly Dictionary<SettingsProperty, object?> values = new Dictionary<SettingsProperty, object?>();

		public string Name { get; protected set; }

		public List<SettingsProperty> Properties { get; private set; } = new List<SettingsProperty>();
		public ResourceManager ResourceManager { get; }

		protected SettingsProperty Register<TType>(string propertyName, TType defaultValue) {
			var settingsProperty = new SettingsProperty(propertyName, typeof(TType), defaultValue);
			Properties.Add(settingsProperty);

			return settingsProperty;
		}


		public SettingsObject(string name, ResourceManager resourceManager) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ResourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
		}

		public void UnsetValue(SettingsProperty property) => values.Remove(property);
		public void SetValue(SettingsProperty property, object? value) => values[property] = value;
		public object? GetValue(SettingsProperty property) => values.TryGetValue(property, out var value) ? value : (property ?? throw new ArgumentNullException(nameof(property))).DefaultValue;
		public object GetRequiredValue(SettingsProperty property) => (values.TryGetValue(property, out var value) ? value : (property ?? throw new ArgumentNullException(nameof(property))).DefaultValue) ?? throw new Exception("Required value was null");
	}
}
