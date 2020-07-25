using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
	public class SettingsProperty {
		private readonly SettingsObject settingsObject;

		public string Name { get; private set; }
		public Type ValueType { get; private set; }
		public object? DefaultValue { get; private set; }

		public string Description => settingsObject.ResourceManager.GetInvString($"{settingsObject.Name}.{Name}.Description");
		public string Example => settingsObject.ResourceManager.GetInvString($"{settingsObject.Name}.{Name}.Example");

		public SettingsProperty(SettingsObject settingsObject, string name, Type valueType, object? defaultValue) {
			this.settingsObject = settingsObject;
			Name = name;
			ValueType = valueType;
			DefaultValue = defaultValue;
		}

	}
}
