using System;
using System.Collections.Immutable;

namespace AVDump3Lib.Settings.Core {

	public delegate string? SettingPropertyValueToString(ISettingProperty settingProperty, object? value);
	public delegate object? SettingPropertyValueToObject(ISettingProperty settingProperty, string? value);


	public class SettingProperty : ISettingProperty {
		public string Name { get; }
		public ImmutableArray<string> AlternativeNames { get; }
		public ISettingGroup Group { get; }

		public Type ValueType { get; }
		public object? UserValueType { get; }
		public object? DefaultValue { get; }

		public string? ToString(object? objectValue) => toString(this, objectValue);
		public object? ToObject(string? stringValue) => toObject(this, stringValue);

		private readonly SettingPropertyValueToObject toObject;
		private readonly SettingPropertyValueToString toString;

		public SettingProperty(string name, ImmutableArray<string> alternativeNames, ISettingGroup group, Type valueType, object? userValueType, object? defaultValue, SettingPropertyValueToObject toObject, SettingPropertyValueToString toString) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			AlternativeNames = alternativeNames;
			Group = group ?? throw new ArgumentNullException(nameof(group));
			ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
			UserValueType = userValueType;
			DefaultValue = defaultValue;
			this.toObject = toObject ?? throw new ArgumentNullException(nameof(toObject));
			this.toString = toString ?? throw new ArgumentNullException(nameof(toString));
		}
	}









}
