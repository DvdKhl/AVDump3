using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Modules {
	public class ModuleConfigurationEventArgs : EventArgs {
		public bool Continue { get; set; }
		public string Reason { get; set; }
	}

	public class AVD3ModuleManagement {
		private List<IAVD3Module> modules;

		public AVD3ModuleManagement() {
			modules = new List<IAVD3Module>();
		}

		public void LoadModules(string directoryPath) {
			foreach(var filePath in Directory.EnumerateFiles(directoryPath, "AVD3*Module.dll")) {
				var moduleAssembly = Assembly.LoadFile(filePath);

				var moduleTypes = moduleAssembly.GetTypes().Where(
					t => !t.IsAbstract && typeof(IAVD3Module).IsAssignableFrom(t) && t.GetConstructors().Any(
						c => c.GetParameters().Length == 0 && c.IsPublic
					)
				);
				foreach(var moduleType in moduleTypes) {
					LoadModuleFromType(moduleType);
				}
			}
		}

		public void LoadModuleFromType(Type moduleType, params object[] args) {
			var module = (IAVD3Module)Activator.CreateInstance(moduleType, args);
			modules.Add(module);
		}

		public void InitializeModules() {
			var modules = this.modules.AsReadOnly();
			foreach(var module in modules) {
				module.Initialize(modules);
			}
		}

		public T GetModule<T>() where T: IAVD3Module { return modules.OfType<T>().SingleOrDefault(); }

        public ModuleConfigurationEventArgs RaiseBeforeConfiguration() => RaiseConfiguration((m, e) => m.BeforeConfiguration(e));
		public ModuleConfigurationEventArgs RaiseAfterConfiguration() => RaiseConfiguration((m, e) => m.AfterConfiguration(e));
		private ModuleConfigurationEventArgs RaiseConfiguration(Action<IAVD3Module, ModuleConfigurationEventArgs> onConfiguration) {
			var e = new ModuleConfigurationEventArgs { Continue = true };
			foreach(var module in modules) {
				onConfiguration(module, e);
				if(!e.Continue) break;
			}
			return e;
		}
    }
}
