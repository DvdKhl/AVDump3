using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3CL {
	class Program {

		static void Main(string[] args) {
			if(args.Length == 1 && args[0].Equals("DEBUG")) {
				args = new string[] {
                    //"--Help",
					"--Conc=6:G:/,1;H:/,1;I:/,1",
					//"--BSize=8:8",
					//"--Consumers=CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER, MKV",
                    "--Consumers=CRC32, ED2K, MD5, SHA1, TTH, MKV, OGG",
                    //"--Consumers=MKV",
                    "--Reports=AVD3Report",
                    //"--PrintReports",
                    "--WExts=mkv",
                    "--RDir=Reports/",
                    "--SaveErrors",
                    "--IncludePersonalData",
                    "--ErrorDirectory=Error",
                    "I:/"
                };
			}
			var moduleManagemant = IniModules();
			var pathsToProcess = ProcessCommandlineArguments(moduleManagemant.GetModule<AVD3SettingsModule>(), args);
            moduleManagemant.RaiseAfterConfiguration();



            if(pathsToProcess == null) {
				Console.Read();
				return;
			}

			var clModule = moduleManagemant.GetModule<AVD3CLModule>();
			clModule.Process(pathsToProcess);

			Console.Read();
		}
		private static AVD3ModuleManagement IniModules() {
			var moduleManagement = new AVD3ModuleManagement();
			moduleManagement.LoadModules(AppDomain.CurrentDomain.BaseDirectory);
			moduleManagement.LoadModuleFromType(typeof(AVD3InformationModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ProcessingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ReportingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3SettingsModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3CLModule));
			moduleManagement.InitializeModules();
			return moduleManagement;
		}

		private static string[] ProcessCommandlineArguments(AVD3SettingsModule settingsModule, string[] arguments) {
			var unnamedArgs = new List<string>();
			var clManagement = new CLManagement();
			clManagement.SetUnnamedParamHandler(arg => unnamedArgs.Add(arg));

			var argGroups = settingsModule.RaiseCommandlineRegistration().ToArray();
			clManagement.RegisterArgGroups(argGroups);

			if(!clManagement.ParseArgs(arguments)) return null;

			return unnamedArgs.ToArray();
		}


	}
}
