using AVDump3Lib;
using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.Core;
using AVDump3Lib.UI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AVDump3GUI.ViewModels;

public class AVD3GUIModule : AVD3UIModule {
	public override AVD3Console Console { get; } = new AVD3Console();
	public ISettingStore SettingStore { get; set; }


	private BytesReadProgress bytesReadProgress = new ();
	protected override AVD3UISettings Settings => settings;
	private AVD3GUISettings settings = null!;

	public override IBytesReadProgress CreateBytesReadProgress() => bytesReadProgress;

	protected override void ProcessException(Exception ex) {	}


	public FileProgress[] GetProgress() {
		return bytesReadProgress.GetProgress();
	}

	public static AVD3ModuleManagement Create(string moduleDirectory, ISettingStore settingStore) {
		var moduleManagement = CreateModules(moduleDirectory);
		var guiModule = moduleManagement.GetModule<AVD3GUIModule>();
		guiModule.SettingStore = settingStore;

		moduleManagement.RaiseIntialize();


		return moduleManagement;
	}
	public static bool Run(AVD3ModuleManagement moduleManagement) {
		var moduleInitResult = moduleManagement.RaiseInitialized();
		if(moduleInitResult.CancelStartup) {
			if(!string.IsNullOrEmpty(moduleInitResult.Reason)) {
				System.Console.WriteLine("Startup Cancel: " + moduleInitResult.Reason);
			}
			return false;
		}
		return true;
	}

	private static AVD3ModuleManagement CreateModules(string moduleDirectory) {
		var moduleManagement = new AVD3ModuleManagement();
		moduleManagement.LoadModuleFromType(typeof(AVD3GUIModule));
		if(!string.IsNullOrEmpty(moduleDirectory)) moduleManagement.LoadModules(moduleDirectory);
		moduleManagement.LoadModuleFromType(typeof(AVD3InformationModule));
		moduleManagement.LoadModuleFromType(typeof(AVD3ProcessingModule));
		moduleManagement.LoadModuleFromType(typeof(AVD3ReportingModule));
		return moduleManagement;
	}

	public override void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		base.Initialize(modules);

		settings = new AVD3GUISettings(SettingStore);
		InitializeSettings(new SettingsModuleInitResult(SettingStore));
	}

	protected override IStreamProvider CreateFileStream(string[] paths) {
		return base.CreateFileStream(paths);
	}

	protected override void OnProcessingStarting(CancellationTokenSource cts) {
		base.OnProcessingStarting(cts);
	}

	protected override void OnProcessingFinished() {
		base.OnProcessingFinished();
	}

	protected override void OnProcessingFullyFinished() {
		base.OnProcessingFullyFinished();
	}

	protected override void OnException(AVD3LibException ex) {
	}
}
