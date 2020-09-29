using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Reporting.Core {
	public interface IReport {
		string FileExtension { get; }
		string ReportToString(Encoding encoding);
		void SaveToFile(string filePath, string reportContentPrefix, Encoding encoding);
	}
}
