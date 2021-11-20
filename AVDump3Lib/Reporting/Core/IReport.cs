using System.Text;

namespace AVDump3Lib.Reporting.Core {
	public interface IReport {
		string FileExtension { get; }
		string ReportToString(Encoding encoding);
		void SaveToFile(string filePath, string reportContentPrefix, Encoding encoding);
	}
}
