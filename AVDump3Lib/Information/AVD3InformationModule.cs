using AVDump3Lib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information {
	public interface IAVD3InformationModule : IAVD3Module {

	}
	public class AVD3InformationModule : IAVD3InformationModule {
		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		}
	}
}
