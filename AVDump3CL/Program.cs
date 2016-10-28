using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AVDump3CL {


	class Program {
		private static CLSettingsHandler clSettingsHandler;

		static void Main(string[] args) {
			if(args.Length == 1 && args[0].Equals("DEBUG")) {
				args = new string[] {
                    //"--Help",
					"--Conc=6",
					"--BSize=8:8",
					//"--Consumers=CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER, MKV",
                    //"--Consumers",
					"--Consumers=SHA1",
					"--PrintHashes",
                    //"--Consumers=MKV",
                    //"--Reports=AVD3Report",
					//"--PrintReports",
					//"--HideBuffers",
                    //"--HideTotalProgress",
                    //"--HideFileProgress",
                    //"--WExts=mkv, avi, ogg, ogm, mp4",
                    //"--Reports=AniDBReport",
                    //"--RDir=Reports/",
					"--SaveErrors",
					"--IncludePersonalData",
					"--ErrorDirectory=Error",
					"--PauseBeforeExit",
                    //"--NullStreamTest=100:1000000:1",
                    //@"D:\MyStuff\BigFile", //C548A93C
                    @"D:\MyStuff\MediumFile", //5B64C2B0
					//@"C:\Users\Arokh\AppData\Roaming\mIRC\downloads\"
				};

				//if(true) {
				//	using(var stream = File.Create(@"D:\MyStuff\MediumFile")) {
				//		var b = new byte[1L << 30];
				//		stream.Write(b, 0, b.Length);
				//		stream.Write(b, 0, b.Length);
				//	}
				//}


				//File.WriteAllBytes(@"D:\MyStuff\SmallFile", Enumerable.Range(0, (1 << 20) + 2).Select(x => (byte)1).ToArray());
			}

			clSettingsHandler = new CLSettingsHandler();

			var moduleManagemant = IniModules();
			moduleManagemant.RaiseBeforeConfiguration();

			var pathsToProcess = new List<string>();
			try {
				if(!clSettingsHandler.ParseArgs(args, pathsToProcess)) {
					Console.Read();
					return;
				}
			} catch(InvalidOperationException ex) {
				Console.Error.WriteLine(ex.Message);
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
