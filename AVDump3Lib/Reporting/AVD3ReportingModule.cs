using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Reporting.Core;
using AVDump3Lib.Reporting.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Reporting {
	public interface IAVD3ReportingModule : IAVD3Module {

	}
	public class AVD3ReportingModule : IAVD3ReportingModule {
		private List<IReportFactory> reportFactories;

		public IReadOnlyCollection<IReportFactory> ReportFactories { get; }


		public AVD3ReportingModule() {
			reportFactories = new List<IReportFactory> {
				new ReportFactory(fileMetaInfo => new AVD3Report(fileMetaInfo))
			};

			ReportFactories = reportFactories.AsReadOnly();
		}

		public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {
		}
	}
}
