using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
	public class SettingsProperty {
		public string Name { get; private set; }
		public Type ValueType { get; private set; }
		public object? DefaultValue { get; private set; }

		public SettingsProperty(string name, Type valueType, object? defaultValue) {
			Name = name;
			ValueType = valueType;
			DefaultValue = defaultValue;
		}

	}
}
