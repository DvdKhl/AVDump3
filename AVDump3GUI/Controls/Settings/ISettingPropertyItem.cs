using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Immutable;

namespace AVDump3GUI.Controls.Settings;

public interface ISettingPropertyItem {

	ImmutableArray<string> AlternativeNames { get; }
	object DefaultValue { get; }
	string Description { get; }
	string Example { get; }
	string Name { get; }
	Type ValueType { get; }
	//ISettingGroupItem Parent { get; }
	object? ToObject(string? stringValue);
	string? ToString(object? objectValue);
	void UpdateStoredValue();

	object ValueRaw { get; set; }

	bool IsSet { get; }
	bool IsSetAndUnchanged { get; }
	bool IsChanged { get; }

	string ValueTypeKey { get; }
	object Value { get; set; }
}
