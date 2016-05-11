using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Reporting {
	public interface IAVD3ReportingModule : IAVD3Module {

	}
	public class AVD3ReportingModule : IAVD3ReportingModule {
		private IAVD3InformationModule informationModule;

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
			informationModule = modules.OfType<IAVD3InformationModule>().Single();

			throw new NotImplementedException();
		}
	}
}
