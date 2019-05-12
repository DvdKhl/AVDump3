using AVDump3Lib.Information;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AVDump3CL {
	class Program {

		private static CLSettingsHandler clSettingsHandler;

		static void Main(string[] args) {
			//new PerformanceTest().PerformanceReport();
			//return;

			if(args.Length == 1 && args[0].Equals("DEBUG")) {
				args = File.ReadLines("DebugParams.txt").Where(x => !x.StartsWith("//")).ToArray();
			}

			clSettingsHandler = new CLSettingsHandler();

			var moduleManagement = CreateModules();
			moduleManagement.RaiseIntialize();

			var pathsToProcess = new List<string>();
			try {
				if(!clSettingsHandler.ParseArgs(args, pathsToProcess)) {
					if(!Utils.UsingMono) Console.Read();
					return;
				}
			} catch(InvalidOperationException ex) {
				Console.Error.WriteLine(ex.Message);
				return;
			}

			var moduleInitResult = moduleManagement.RaiseInitialized();
			if(moduleInitResult.CancelStartup) {
				if(!string.IsNullOrEmpty(moduleInitResult.Reason)) {
					Console.WriteLine("Startup Cancel: " + moduleInitResult.Reason);
				}
				return;
			}

			var clModule = moduleManagement.GetModule<AVD3CLModule>();
			clModule.Process(pathsToProcess.ToArray());
		}

		private static AVD3ModuleManagement CreateModules() {
			var moduleManagement = new AVD3ModuleManagement();
			moduleManagement.LoadModules(AppDomain.CurrentDomain.BaseDirectory);
			moduleManagement.LoadModuleFromType(typeof(AVD3InformationModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ProcessingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ReportingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3SettingsModule), clSettingsHandler);
			moduleManagement.LoadModuleFromType(typeof(AVD3CLModule));
			return moduleManagement;
		}
	}
}
