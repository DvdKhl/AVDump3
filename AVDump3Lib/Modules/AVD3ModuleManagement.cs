using System.Reflection;

namespace AVDump3Lib.Modules;

public class AVD3ModuleManagement {
	private readonly List<IAVD3Module> modules;

	public AVD3ModuleManagement() {
		modules = new List<IAVD3Module>();
	}

	public void LoadModules(string directoryPath) {
		foreach(var filePath in Directory.EnumerateFiles(directoryPath, "AVD3*Module.dll")) {
			var moduleAssembly = Assembly.LoadFrom(filePath);

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

	public void RaiseIntialize() {
		var modules = this.modules.AsReadOnly();
		foreach(var module in modules) module.Initialize(modules);
	}

	public ModuleInitResult RaiseInitialized() {
		foreach(var module in modules) {
			var result = module.Initialized();
			if(result.CancelStartup) return result;
		}
		return new ModuleInitResult(false);
	}

	public T GetModule<T>() where T : IAVD3Module { return modules.OfType<T>().SingleOrDefault(); }
	public IEnumerable<T> OfType<T>() { return modules.OfType<T>(); }

	public void AddModule(IAVD3Module instance) => modules.Add(instance);
	public void Shutdown() {
		foreach(var module in modules) {
			module.Shutdown();
		}
	}
}
