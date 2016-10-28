using AVDump3Lib.Modules;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Settings {
    public interface IAVD3SettingsModule : IAVD3Module {
        void RegisterSettings(SettingsObject settingsObject);

    }

    public class AVD3SettingsModule : IAVD3SettingsModule {
        private ISettingsHandler settingsHandler;
        private List<SettingsObject> settingsObjects = new List<SettingsObject>();


        public AVD3SettingsModule(ISettingsHandler handler) {
            settingsHandler = handler;
        }

        public void Initialize(IReadOnlyCollection<IAVD3Module> modules) { }
        public void BeforeConfiguration(ModuleConfigurationEventArgs args) {
            settingsHandler.Register(settingsObjects);
        }
        public void AfterConfiguration(ModuleConfigurationEventArgs args) {
        }


        public void RegisterSettings(SettingsObject settingsObject) { settingsObjects.Add(settingsObject); }

    }
}
