using System.Collections.Immutable;
using System.Resources;

namespace AVDump3GUI.Controls.Settings;

public interface ISettingGroupItem {
	string Description { get; }
	string Name { get; }
	ImmutableArray<ISettingPropertyItem> Properties { get; }
	ResourceManager ResourceManager { get; }
}
