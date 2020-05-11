using AVDump3Lib.Information;
using AVDump3Lib.Misc;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;

namespace AVDump3CL {
	public class AVD3CLInstance {
		static private void Start(string[] args) {
			if(ProcessFromFileArgument(ref args)) return;

			var serviceCollection = new ServiceCollection();

			serviceCollection.AddScoped<ISettingsHandler, CLSettingsHandler>();
			serviceCollection.AddSingleton<IAVD3InformationModule, AVD3InformationModule>();
			serviceCollection.AddSingleton<IAVD3ProcessingModule, AVD3ProcessingModule>();
			serviceCollection.AddSingleton<IAVD3ReportingModule, AVD3ReportingModule>();
			serviceCollection.AddSingleton<IAVD3SettingsModule, AVD3SettingsModule>();
			serviceCollection.AddSingleton<IAVD3CLModule, AVD3CLModule>();


			var serviceProvider = serviceCollection.BuildServiceProvider();

			var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

			using var scope = scopeFactory.CreateScope();
			var settingsHandler = scope.ServiceProvider.GetRequiredService<ISettingsHandler>();
		}

		private static bool ProcessFromFileArgument(ref string[] args) {
			if(args.Length > 0 && args[0].Equals("FROMFILE")) {
				if(args.Length < 2 || !File.Exists(args[1])) {
					Console.WriteLine("FROMFILE: File not found");
					return false;
				}
				args = File.ReadLines(args[1]).Where(x => !x.StartsWith("//") && !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace("\r", "")).Concat(args.Skip(2)).ToArray();

			}
			return true;
		}
	}

	class Program {
		private static CLSettingsHandler clSettingsHandler;

		static void Main(string[] args) {
			if(args.Length > 0 && args[0].Equals("FROMFILE")) {
				if(args.Length < 2 || !File.Exists(args[1])) {
					Console.WriteLine("FROMFILE: File not found");
					return;
				}
				args = File.ReadLines(args[1]).Where(x => !x.StartsWith("//") && !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace("\r", "")).Concat(args.Skip(2)).ToArray();
			}

			if(args.Length > 0 && args[0].Equals("PRINTARGS")) {
				foreach(var arg in args) Console.WriteLine(arg);
				Console.WriteLine();

				args = args.Skip(1).ToArray();
			}


			clSettingsHandler = new CLSettingsHandler();

			var moduleManagement = CreateModules();
			moduleManagement.RaiseIntialize();

			var pathsToProcess = new List<string>();
			try {
				if(!clSettingsHandler.ParseArgs(args, pathsToProcess)) {
					if(Utils.UsingWindows) Console.Read();
					return;
				}
			} catch(Exception ex) {
				Console.WriteLine("Error while parsing commandline arguments:");
				Console.WriteLine(ex.Message);
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
