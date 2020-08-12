using AVDump3Lib.Settings;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Resources;
using System.Text;

namespace AVDump3CL {
	public class AVD3CLSettings : AVD3UISettings {
		public static new ResourceManager ResourceManager => new ResourceManagerMerged(Lang.ResourceManager, AVD3UISettings.ResourceManager);

		public DisplaySettings Display { get; }

		public AVD3CLSettings(ISettingStore store) : base(store) {

			Display = new DisplaySettings(store);
		}

		public static new IEnumerable<ISettingProperty> GetProperties() {
			return AVD3UISettings.GetProperties()
				.Concat(DisplaySettings.CreateProperties());
		}
	}

	public class DisplaySettings : SettingFacade {
		public DisplaySettings(ISettingStore store) : base(SettingGroup, store) { }

		public bool HideBuffers => (bool)GetRequiredValue();
		public bool HideFileProgress => (bool)GetRequiredValue();
		public bool HideTotalProgress => (bool)GetRequiredValue();
		public bool ShowDisplayJitter => (bool)GetRequiredValue();
		public bool ForwardConsoleCursorOnly => (bool)GetRequiredValue();

		public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(DisplaySettings)[0..^8], Lang.ResourceManager);
		public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
		public static IEnumerable<ISettingProperty> CreateProperties() {
			yield return From(SettingGroup, nameof(HideBuffers), None, AVD3UISettings.UnspecifiedType, false);
			yield return From(SettingGroup, nameof(HideFileProgress), None, AVD3UISettings.UnspecifiedType, false);
			yield return From(SettingGroup, nameof(HideTotalProgress), None, AVD3UISettings.UnspecifiedType, false);
			yield return From(SettingGroup, nameof(ShowDisplayJitter), None, AVD3UISettings.UnspecifiedType, false);
			yield return From(SettingGroup, nameof(ForwardConsoleCursorOnly), None, AVD3UISettings.UnspecifiedType, false);
		}
	}

}
