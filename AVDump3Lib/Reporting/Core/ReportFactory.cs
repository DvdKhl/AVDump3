using AVDump3Lib.Information.MetaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Reporting.Core {
	public interface IReportFactory {
		string Name { get; }
		IReport Create(FileMetaInfo fileMetaInfo);
	}

	public delegate IReport CreateReport(FileMetaInfo fileMetaInfo);
	public class ReportFactory : IReportFactory {
		private CreateReport createReport;

		public string Name { get; }

		public ReportFactory(string name, CreateReport createReport) {
			Name = name;
			this.createReport = createReport;
		}

		public IReport Create(FileMetaInfo fileMetaInfo) {
			return createReport(fileMetaInfo);
		}
	}
}
