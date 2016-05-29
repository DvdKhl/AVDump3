using AVDump3Lib.Information.MetaInfo.Core;

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
