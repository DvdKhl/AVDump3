namespace AVDump3Lib.Settings.Core;

public interface ISettingsHandler {
	void Register(IEnumerable<ISettingProperty> settingProperties);
}
