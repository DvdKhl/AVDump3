using AVDump3Lib.Misc;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AVDump3GUI.ViewModels;


public class MainWindowViewModel : BindableBase {
	//private readonly ISettingStore fileSettingStore;
	private readonly ISettingStore userSettingStore;
	public event Action<string> ConsoleWrite = delegate { };


	public List<SettingsGroupViewModel> SettingGroups { get; }


	public MainWindowViewModel() {
		var props = AVD3GUISettings.GetProperties().ToImmutableArray();

		if(!File.Exists("Settings.txt")) File.Create("Settings.txt").Dispose();
		var parseResult = CLSettingsHandler.ParseArgs(props, File.ReadAllLines("Settings.txt"));


		userSettingStore = new SettingStore(props);

		foreach(var settingValue in parseResult.SettingValues) {
			userSettingStore.SetPropertyValue(settingValue.Key, settingValue.Value);
		}


		SettingGroups = props.GroupBy(x => x.Group).Select(x => new SettingsGroupViewModel(x.Select(y => new SettingsPropertyViewModel(y, userSettingStore, AVD3GUISettings.ResourceManager)), AVD3GUISettings.ResourceManager)).ToList();





		foreach(var settingValue in parseResult.SettingValues) {
			userSettingStore.SetPropertyValue(settingValue.Key, settingValue.Value);
		}

		Files = new ObservableCollection<AVDFile>();
		//if(Directory.Exists(@"Y:\\Anime")) {
		//	FileTraversal.Traverse(new[] { @"Y:\\Anime\Stalled" }, true, filePath => {
		//		Files.Add(new AVDFile() { Info = new FileInfo(filePath) });
		//	}, e => { });
		//}

	}

	public DelegateCommand SaveSettingsCommand => saveSettingsCommand ??= new DelegateCommand(SaveSettingsExecute);
	private DelegateCommand saveSettingsCommand = null!;

	private void SaveSettingsExecute() {
		var settingsBuilder = new StringBuilder();
		foreach(var item in SettingGroups.SelectMany(x => x.Properties.Select(y => new { Group = x, Prop = y }).OrderBy(x => x.Group.Name + "." + x.Prop.Name))) {
			item.Prop.UpdateStoredValue();

			if(item.Prop.ValueRaw != ISettingStore.Unset) {
				settingsBuilder.AppendLine("--" + item.Group.Name + "." + item.Prop.Name + "=" + item.Prop.ToString(item.Prop.ValueRaw));
			}
		}
		File.WriteAllText("Settings.txt", settingsBuilder.ToString());
	}


	public DelegateCommand StartCommand => startCommand ??= new DelegateCommand(StartExecute, () => !isRunning);
	private DelegateCommand startCommand = null!;

	private bool isRunning = false;
	private void StartExecute() {
		if(isRunning) return;
		isRunning = true;
		StartCommand.RaiseCanExecuteChanged();

		var moduleManagement = AVD3GUIModule.Create(null, userSettingStore);

		if(!AVD3GUIModule.Run(moduleManagement)) {

		}

		var guiModule = moduleManagement.GetModule<AVD3GUIModule>();
		guiModule.Console.ConsoleWrite += str => ConsoleWrite(str);


		Timer progressTimer = new Timer(o => {
			var progress = guiModule.GetProgress();

			Application.Current.Dispatcher.Invoke(() => {
				foreach(var item in progress) {
					var avdFile = fileMap[item.FilePath];
					avdFile.Completed = item.Completed;
				}
			});
		}, null, 1000, 1000);

		Task.Factory.StartNew(() => {
			guiModule.Process(Files.Select(x => x.Info.FullName).ToArray());
			isRunning = false;
			StartCommand.RaiseCanExecuteChanged();

			progressTimer.Dispose();


			var progress = guiModule.GetProgress();
			Application.Current.Dispatcher.Invoke(() => {
				foreach(var item in progress) {
					var avdFile = fileMap[item.FilePath];
					avdFile.Completed = item.Completed;
				}
			});
		}, TaskCreationOptions.LongRunning);



		DispatcherTimer timer = new DispatcherTimer(
			TimeSpan.FromSeconds(1),
			DispatcherPriority.Render,
			(sender, args) => {

			},
			Application.Current.Dispatcher
		);
	}

	internal void AddPaths(string[] e) {
		FileTraversal.Traverse(e, true, filePath => {
			AVDFile item = new AVDFile() { Info = new FileInfo(filePath) };
			Files.Add(item);
			fileMap.Add(item.Info.FullName, item);
		}, e => { });
		RaisePropertyChanged(nameof(Files));
	}

	public ObservableCollection<AVDFile> Files { get; }
	private Dictionary<string, AVDFile> fileMap = new Dictionary<string, AVDFile>();


}
