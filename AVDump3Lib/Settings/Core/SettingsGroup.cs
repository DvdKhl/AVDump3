using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Resources;

namespace AVDump3Lib.Settings.Core {
	public interface ISettingsGroup {
		string Name { get; }
		ImmutableArray<SettingsProperty> Properties { get; }
		ResourceManager ResourceManager { get; }

		string? PropertyObjectToString(SettingsProperty prop, object? objectValue);
		object? PropertyStringToObject(SettingsProperty prop, string? stringValue);
	}

	public class SettingsGroup : ISettingsGroup {
		private readonly Func<SettingsProperty, string?, object?> toObject;
		private readonly Func<SettingsProperty, object?, string?> toString;

		public string Name { get; protected set; }

		public ImmutableArray<SettingsProperty> Properties { get; }
		public ResourceManager ResourceManager { get; }

		//protected SettingsProperty Register<TType>(string propertyName, TType defaultValue) {
		//	var settingsProperty = new SettingsProperty(propertyName, typeof(TType), defaultValue);
		//	Properties.Add(settingsProperty);

		//	return settingsProperty;
		//}

		//public static Func<SettingsProperty, object?, string?> DefaultToString => (p, v) => v?.ToString();
		public static Func<SettingsProperty, object?, string?> DefaultToString => (p, v) => (string?)Convert.ChangeType(v, typeof(string), CultureInfo.InvariantCulture);
		public static Func<SettingsProperty, string?, object?> DefaultToObject => (p, v) => Convert.ChangeType(v, p.ValueType, CultureInfo.InvariantCulture);

		public SettingsGroup(string name, ResourceManager resourceManager, Func<SettingsProperty, string?, object?> toObject, Func<SettingsProperty, object?, string?> toString, params SettingsProperty[] properties) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ResourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
			Properties = properties.ToImmutableArray();

			this.toObject = toObject;
			this.toString = toString;
		}

		public string? PropertyObjectToString(SettingsProperty prop, object? objectValue) => toString(prop, objectValue);
		public object? PropertyStringToObject(SettingsProperty prop, string? stringValue) => toObject(prop, stringValue);

	}
}
