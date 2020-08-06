using System.Resources;

namespace AVDump3Lib.Settings.Core {
	public interface ISettingGroup {
		string Name { get; }
		ISettingGroup? Parent { get; }
		string FullName { get; }
		ResourceManager ResourceManager { get; }
	}
}
