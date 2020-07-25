using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
	public interface ISettingsProperty {
		ImmutableArray<string> AlternativeNames { get; }
		object? DefaultValue { get; }
		string Name { get; }
		Type ValueType { get; }
	}

	public class SettingsProperty : ISettingsProperty {
		public string Name { get; }
		public ImmutableArray<string> AlternativeNames { get; private set; }

		public Type ValueType { get; }
		public object? DefaultValue { get; }


		public SettingsProperty(string name, IEnumerable<string> alternativeNames, Type valueType, object? defaultValue) {
			Name = name;
			AlternativeNames = alternativeNames?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(alternativeNames));

			ValueType = valueType;
			DefaultValue = defaultValue;
		}

		public static SettingsProperty From<TType>(string name, IEnumerable<string> alternativeNames, TType defaultValue) => new SettingsProperty(name, alternativeNames, typeof(TType), defaultValue);
	}
}
