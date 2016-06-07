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
        private static CLSettingsHandler clSettingsHandler;

        static void Main(string[] args) {
			if(args.Length == 1 && args[0].Equals("DEBUG")) {
				args = new string[] {
                    "--Help",
					"--Conc=6:G:/,1;H:/,1;I:/,1",
					//"--BSize=8:8",
					//"--Consumers=CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER, MKV",
                    "--Consumers=CRC32, ED2K, MD5, SHA1, TTH, MKV, OGG",
                    //"--Consumers=MKV",
                    "--Reports=AVD3Report",
                    //"--PrintReports",
                    "--WExts=mkv, avi, ogg, ogm, mp4",
                    "--Reports=AniDBReport",
                    "--RDir=Reports/",
                    "--SaveErrors",
                    "--IncludePersonalData",
                    "--ErrorDirectory=Error",
                    "G:/",
                    "H:/",
                    "I:/"
                };
			}
            clSettingsHandler = new CLSettingsHandler();

            var moduleManagemant = IniModules();
            moduleManagemant.RaiseBeforeConfiguration();

            var pathsToProcess = new List<string>();
            if(!clSettingsHandler.ParseArgs(args, pathsToProcess)) {
				Console.Read();
				return;
            }

            moduleManagemant.RaiseAfterConfiguration();

			var clModule = moduleManagemant.GetModule<AVD3CLModule>();
			clModule.Process(pathsToProcess.ToArray());
		}
		private static AVD3ModuleManagement IniModules() {
            var moduleManagement = new AVD3ModuleManagement();
			moduleManagement.LoadModules(AppDomain.CurrentDomain.BaseDirectory);
			moduleManagement.LoadModuleFromType(typeof(AVD3InformationModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ProcessingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3ReportingModule));
			moduleManagement.LoadModuleFromType(typeof(AVD3SettingsModule), clSettingsHandler);
			moduleManagement.LoadModuleFromType(typeof(AVD3CLModule));
			moduleManagement.InitializeModules();
			return moduleManagement;
		}
	}
}
