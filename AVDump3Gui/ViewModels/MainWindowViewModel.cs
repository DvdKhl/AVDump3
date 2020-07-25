using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AVDump3Lib.Settings.Core;
using AVDump3UI;

namespace AVDump3Gui.ViewModels {

	public class AVDFile {
		public FileInfo Info { get; set; }
	}


	public class MainWindowViewModel : ViewModelBase {
		public string Greeting => "Welcome to Avalonia!";

		public List<AVDFile> Files { get; } = new List<AVDFile>() { new AVDFile { Info = new FileInfo(@"Z:\Media\Renamed\Aiura\Aiura 01 - The Day Before [Commie][HDTV][01d20eb9][1223248].mkv") } };



		public List<SettingsObject> SettingGroups { get; } = new AVD3UIModuleSettings().SettingObjects().ToList();
	}
}
