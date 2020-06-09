using AVDump3Lib.Information.MetaInfo.Core;
using ExtKnot.StringInvariants;
using System.Resources;

namespace AVDump3Lib.Reporting.Core {
	public interface IReportFactory {
		string Name { get; }
		string Description { get; }
		IReport Create(FileMetaInfo fileMetaInfo);
	}

	public delegate IReport CreateReport(FileMetaInfo fileMetaInfo);
	public class ReportFactory : IReportFactory {
		private readonly CreateReport createReport;

		public string Name { get; }
		public string Description { get; }

		internal ReportFactory(string name, CreateReport createReport) : this(name, GetDescription(name), createReport) { }

		public ReportFactory(string name, string description, CreateReport createReport) {
			Name = name;
			Description = description;
			this.createReport = createReport;
		}


		private static string GetDescription(string name) {
			var description = Lang.ResourceManager.GetInvString(name.InvReplace("-", "") + "Description");
			return !string.IsNullOrEmpty(description) ? description : "<NoDescriptionGiven>";
		}

		public IReport Create(FileMetaInfo fileMetaInfo) {
			return createReport(fileMetaInfo);
		}
	}
}
