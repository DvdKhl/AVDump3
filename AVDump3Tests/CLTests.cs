using AVDump3CL;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using Xunit;

namespace AVDump3Tests {
	public class CLTests {
		[Fact]
		public void WithoutArgs() {
			var management = AVD3CLModule.Create(null);
			var avd3CLModule = management.GetModule<AVD3CLModule>();
			avd3CLModule.HandleArgs(Array.Empty<string>());

			var settingsModule = management.GetModule<AVD3SettingsModule>();
			CLSettingsHandler.PrintHelp(settingsModule.SettingProperties, null, true);
		}
	}
}
