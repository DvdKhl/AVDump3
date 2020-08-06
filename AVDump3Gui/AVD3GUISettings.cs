using AVDump3Lib.Settings;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;

namespace AVDump3Gui {
	public class AVD3GUISettings : AVD3UISettings {
		public static new ResourceManager ResourceManager => new ResourceManagerMerged(Lang.ResourceManager, AVD3UISettings.ResourceManager);

		public AVD3GUISettings(ISettingStore store) : base(store) {
		}

		public static new IEnumerable<ISettingProperty> GetProperties() {
			return AVD3UISettings.GetProperties();
		}
	}


}
