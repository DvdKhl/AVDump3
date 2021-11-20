using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Core {
	public abstract class XmlReport : IReport {
		protected abstract XDocument Report { get; }
		public string FileExtension { get; } = "xml";

		public string ReportToString(Encoding encoding) {
			var settings = new XmlWriterSettings {
				Indent = true,
				NewLineChars = "\n",
				CheckCharacters = false,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			};

			return ReportToString(settings);
		}
		public string ReportToString(XmlWriterSettings settings) {
			using var memStream = new MemoryStream();
			using(var xmlWriter = XmlWriter.Create(memStream, settings)) {
				Report.Root.WriteTo(xmlWriter);
			}
			memStream.Position = 0;

			using var strReader = new StreamReader(memStream);

			return strReader.ReadToEnd();
		}

		public XDocument ReportToXml() { return new XDocument(Report); }

		public void SaveToFile(string filePath, string reportContentPrefix, Encoding encoding) {
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			File.AppendAllText(filePath, (!string.IsNullOrEmpty(reportContentPrefix) ? reportContentPrefix + "\n" : "") + ReportToString(encoding) + "\n\n", encoding);
		}
	}
}
