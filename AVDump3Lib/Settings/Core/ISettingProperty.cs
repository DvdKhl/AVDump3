using System;
using System.Collections.Immutable;

namespace AVDump3Lib.Settings.Core {
	public interface ISettingProperty {
		ImmutableArray<string> AlternativeNames { get; }
		object? DefaultValue { get; }
		ISettingGroup Group { get; }
		string Name { get; }
		Type ValueType { get; }
		object? UserValueType { get; }

		object? ToObject(string? stringValue);
		string? ToString(object? objectValue);
	}
}
