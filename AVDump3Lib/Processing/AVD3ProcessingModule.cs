using AVDump3Lib.Modules;
using AVDump3Lib.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing {
	public interface IAVD3ProcessingModule : IAVD3Module {

	}
	public class AVD3ProcessingModule : IAVD3ProcessingModule {


		public AVD3ProcessingModule() {

		}
		public void LoadHashAlgorythms(string directoryPath) {
			foreach(var filePath in Directory.EnumerateFiles(directoryPath, "AVD3*Hash.dll")) {
				var moduleAssembly = Assembly.LoadFile(filePath);
			}
		}


		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {


		}
	}
}
