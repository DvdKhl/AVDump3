using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Core {
	public abstract class XmlReport : IReport {
		protected abstract XDocument Report { get; }
		public string FileExtension { get; } = "xml";

		public string ReportToString(Encoding encoding) {
			using var memStream = new MemoryStream();
			using(var xmlWriter = XmlWriter.Create(memStream, new XmlWriterSettings {
				Indent = true,
				NewLineChars = "\n",
				CheckCharacters = false,
				Encoding = Encoding.UTF8,
				OmitXmlDeclaration = true,
				ConformanceLevel = ConformanceLevel.Fragment
			})) {
				Report.Root.WriteTo(xmlWriter);
			}
			memStream.Position = 0;

			using var strReader = new StreamReader(memStream);

			return strReader.ReadToEnd();
		}

		public XDocument ReportToXml() { return new XDocument(Report); }

		public void SaveToFile(string filePath, Encoding encoding) {
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			File.AppendAllText(filePath, ReportToString(encoding) + "\n\n", encoding);
		}
	}
}
