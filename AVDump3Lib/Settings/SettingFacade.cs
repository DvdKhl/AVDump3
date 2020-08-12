using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Settings.Core {
	public delegate string? SettingFacadeToString<T>(ISettingProperty settingProperty, T? value) where T : class;
	public delegate T? SettingFacadeToObject<T>(ISettingProperty settingProperty, string? value) where T : class;
	public delegate string SettingFacadeToStringNonNull<T>(ISettingProperty settingProperty, T value);
	public delegate T SettingFacadeToObjectNonNull<T>(ISettingProperty settingProperty, string value);


	public delegate ISettingProperty SettingFacadeFrom<T>(string name, ImmutableArray<string> alternativeNames, T defaultValue, SettingFacadeToObjectNonNull<T> toObject, SettingFacadeToStringNonNull<T> toString) where T : class;

	public class SettingFacade {
		protected static ImmutableArray<string> None => ImmutableArray<string>.Empty;

		protected static object? DefaultToObject(ISettingProperty settingProperty, string? value) {
			if(settingProperty == null) throw new ArgumentNullException(nameof(settingProperty));

			if(typeof(ImmutableArray<string>).IsAssignableFrom(settingProperty.ValueType)) {
				return ImmutableArray.CreateRange((value ?? "").Split(',').Select(x => x.Trim()));
			}


			if(settingProperty is null) throw new ArgumentNullException(nameof(settingProperty));
			return Convert.ChangeType(value, settingProperty.ValueType, CultureInfo.InvariantCulture);
		}

		protected static string? DefaultToString(ISettingProperty settingProperty, object? value) {
			if(value is ImmutableArray<string> items) {
				return "{" + string.Join(", ", items) + "}";
			}

			return (string?)Convert.ChangeType(value, typeof(string), CultureInfo.InvariantCulture);
		}

		protected static ImmutableArray<string> Names(params string[] altNames) => altNames.ToImmutableArray();

		protected static ISettingProperty FromWithNullToNull<T>(ISettingGroup group, string name, ImmutableArray<string> alternativeNames, object userValueTyp, T defaultValue, SettingFacadeToObjectNonNull<T> toObject, SettingFacadeToStringNonNull<T> toString) where T : class {
			return new SettingProperty(name, alternativeNames, group, typeof(T), userValueTyp, defaultValue, (p, s) => s == null ? null : toObject(p, s), (p, o) => o == null ? null : toString(p, (T)o));
		}
		protected static ISettingProperty From<T>(ISettingGroup group, string name, ImmutableArray<string> alternativeNames, object? userValueTyp, T? defaultValue, SettingFacadeToObject<T> toObject, SettingFacadeToString<T> toString) where T : class {
			return new SettingProperty(name, alternativeNames, group, typeof(T), userValueTyp, defaultValue, (p, s) => toObject(p, s), (p, o) => toString(p, (T?)o));
		}
		protected static ISettingProperty From<T>(ISettingGroup group, string name, ImmutableArray<string> alternativeNames, object? userValueTyp, T defaultValue, SettingFacadeToObjectNonNull<T> toObject, SettingFacadeToStringNonNull<T> toString) where T : struct {
			return new SettingProperty(name, alternativeNames, group, typeof(T), userValueTyp, defaultValue, (p, s) => toObject(p, s ?? ""), (p, o) => o == null ? "" : toString(p, (T)o));
		}
		protected static ISettingProperty From<T>(ISettingGroup group, string name, ImmutableArray<string> alternativeNames, object? userValueTyp, T defaultValue, SettingPropertyValueToObject toObject, SettingPropertyValueToString toString) {
			return new SettingProperty(name, alternativeNames, group, typeof(T), userValueTyp, defaultValue, toObject, toString);
		}
		protected static ISettingProperty From<T>(ISettingGroup group, string name, ImmutableArray<string> alternativeNames, object? userValueTyp, T defaultValue) {
			return new SettingProperty(name, alternativeNames, group, typeof(T), userValueTyp, defaultValue, DefaultToObject, DefaultToString);
		}

		private readonly ISettingGroup settingGroup;
		private readonly ISettingStore store;
		private readonly Dictionary<string, ISettingProperty> properties = new Dictionary<string, ISettingProperty>();

		public SettingFacade(ISettingGroup settingGroup, ISettingStore store) {
			this.settingGroup = settingGroup ?? throw new ArgumentNullException(nameof(settingGroup));
			this.store = store ?? throw new ArgumentNullException(nameof(store));

			properties = store.SettingProperties.ToDictionary(x => $"{x.Group.Name}.{x.Name}");
		}


		protected void SetValue(object? value, [CallerMemberName] string propertyName = "") => store.SetPropertyValue(properties[$"{settingGroup.FullName}.{propertyName}"], value);
		protected object? GetValue([CallerMemberName] string propertyName = "") => store.GetPropertyValue(properties[$"{settingGroup.FullName}.{propertyName}"]);
		protected object GetRequiredValue([CallerMemberName] string propertyName = "") => GetValue(propertyName) ?? throw new Exception("Required value was null");
	}
}
