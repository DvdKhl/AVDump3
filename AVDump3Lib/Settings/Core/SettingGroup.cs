using System.Resources;

namespace AVDump3Lib.Settings.Core;

public class SettingGroup : ISettingGroup {

	public string Name { get; }
	public string FullName { get; }

	public ISettingGroup? Parent { get; }
	public ResourceManager ResourceManager { get; }

	public SettingGroup(string name, ResourceManager resourceManager) : this(name, null, resourceManager) {
		ResourceManager = resourceManager;
	}
	public SettingGroup(string name, ISettingGroup? parent, ResourceManager resourceManager) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		Parent = parent;
		ResourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
		FullName = name;
		var curGroup = Parent;
		while(curGroup != null) {
			FullName = curGroup.Name + '.' + FullName;
			curGroup = curGroup.Parent;
		}
	}

}
