using AVDump3Lib.Information;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;

namespace AVDump3CL {
	class Program {

		private static CLSettingsHandler clSettingsHandler;

		static void Main(string[] args) {
			//new PerformanceTest().PerformanceReport();
			//return;

			if(args.Length == 1 && args[0].Equals("DEBUG")) {
				args = new string[] {
                    //"--Help",
					//@"--Conc=3:G:\,1;H:\,1;I:\,1;D:\,1",
					//"--BSize=128:128",
					//"--Consumers=MP4",
					//"--Consumers=CRC32",
					//"--Consumers=OGG",
					//"--DLPath=Donelog.txt",
                    //"--Consumers",
					//"--Reports",
					//"--Consumers=ED2K",
					"--PrintHashes",
					"--Consumers=MKV",
                    //"--Reports=AniDBReport",
					//"--PrintReports",
					//"--HideBuffers",
                    //"--HideTotalProgress",
                    //"--HideFileProgress",
                    //"--WExts=mkv,avi,ogg,ogm,mp4",
                    //"--Reports=AniDBReport",
                    //"--RDir=Reports/",
					//"--SaveErrors",
					//"--IncludePersonalData",
					//"--ErrorDirectory=Error",
					"--PauseBeforeExit",
					//"--NullStreamTest=12:10000:4",
					//"--Host=ommina.l5.ca:9002",
					//"--Auth=Arokh:Anime",
					//@"D:\Ziel.txt",
					//@"G:\Anime", //5B64C2B0
                    //@"H:\Anime", //5B64C2B0
                    //@"I:\Anime", //5B64C2B0
                    //@"D:\New folder\Genius Party - 1 - Genius Party [no group](FF0FA0CE).mp4",
                    @"E:\Anime\[Exiled-Destiny]_Girls_Und_Panzer_Der_Movie_(2DB009FC).mkv",
					//@"E:\Anime\",
					//@"G:\Anime\Processed\Shinseiki Evangelion Gekijouban Shi to Shinsei 2 - Rebirth [aF][DVD][640x360][1bfc05dc].ogm"
                    //@"D:\MyStuff\BigFile", //C548A93C
				};

				//if(true) {
				//	using(var stream = File.Create(@"D:\MyStuff\MediumFile")) {
				//		var b = new byte[64];
				//		for(int i = 0; i < b.Length; i++) b[i] = (byte)'a';
				//		stream.Write(b, 0, b.Length);
				//		//stream.Write(b, 0, b.Length);
				//	}
				//}


				//File.WriteAllBytes(@"D:\MyStuff\SmallFile", Enumerable.Range(0, (1 << 20) + 2).Select(x => (byte)1).ToArray());
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
