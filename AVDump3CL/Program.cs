using AVDump2Lib.InfoProvider.Tools;
using AVDump3Lib.BlockBuffers;
using AVDump3Lib.BlockConsumers;
using AVDump3Lib.BlockConsumers.Matroska;
using AVDump3Lib.HashAlgorithms;
using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace AVDump3CL {
	class Program {

		static void Main(string[] args) {
			if(args.Length == 0) {
				args = new string[] {
					"--Conc=6:G:/,2;H:/,2;I:/,2",
					"--BSize=8:8",
                    //"--Consumers=CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER",
                    "--Consumers=CRC32, SHA1, ED2K, MKV",
                    //@"G:\Software\en_visual_studio_enterprise_2015_with_update_2_x86_x64_dvd_8510142.iso",
                    "G:/Anime",
					"I:/Anime",
					"H:/Anime"
				};
			}

			var moduleManagemant = IniModules();
			var pathsToProcess = ProcessCommandlineArguments(moduleManagemant.GetModule<AVD3SettingsModule>(), args);

			var avd3CL = CreateAVD3CL(args);
			avd3CL?.Process();
		}
		private static AVD3ModuleManagement IniModules() {
			var moduleManagament = new AVD3ModuleManagement();
			moduleManagament.LoadModules(AppDomain.CurrentDomain.BaseDirectory);
			moduleManagament.LoadModuleFromType(typeof(AVD3InformationModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3ProcessingModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3ReportingModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3SettingsModule));
			moduleManagament.InitializeModules();
			return moduleManagament;
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

		private static AVD3CL CreateAVD3CL(string[] arguments) {
			//var msg = new RegisterCLArgGroupMessage(argGroup => clManagement.RegisterArgGroup(argGroup));
			//foreach(var ext in extensionManagement.Items) extensionManagement.SendMessage(ext.Key, new RecievedMessageEventArgs(null, msg));

			var bMap = new Dictionary<string, Func<IBlockStreamReader, IBlockConsumer>> {
				{"SHA1", r => new HashCalculator(r, SHA1.Create(), HashProvider.SHA1Type) },
				{"SHA256", r => new HashCalculator(r, SHA256.Create(), HashProvider.SHA256Type) },
				{"SHA384", r => new HashCalculator(r, SHA384.Create(), HashProvider.SHA384Type) },
				{"SHA512", r => new HashCalculator(r, SHA512.Create(), HashProvider.SHA512Type) },
				{"MD4", r => new HashCalculator(r, new Md4(), HashProvider.MD4Type) },
				{"MD5", r => new HashCalculator(r, MD5.Create(), HashProvider.MD5Type) },
				{"ED2K", r => new HashCalculator(r, new Ed2k(), HashProvider.ED2KType) },
				{"TIGER", r => new HashCalculator(r, new Tiger(), HashProvider.TigerType) },
				{"TTH", r => new HashCalculator(r, new TTH(Environment.ProcessorCount), HashProvider.TTHType) },
				{"CRC32", r => new HashCalculator(r, new Crc32(), HashProvider.CRC32Type) },
				{"MKV", r => new MatroskaParser(r) }
			};

			//ts.TraceInformation("Parsing commandline arguments");
			if(!clManagement.ParseArgs(arguments)) return null;

			var bcf = new BlockConsumerSelector(usedBlockConsumers.Select(x => bMap[x]).ToArray());
			var bp = new BlockPool(blockCount, blockLength);

			var scf = new StreamConsumerFactory(bcf, bp);
			var sp = new StreamFromPathsProvider(globalConcurrentCount,
				partitions, unnamedArgs, true,
				path => path.EndsWith("mkv"), ex => { }
			);

			return new AVD3CL(new StreamConsumerCollection(scf, sp)) {
				UseNtfsAlternateStreams = useNtfsAlternateStreams
			};

		}
	}
}
