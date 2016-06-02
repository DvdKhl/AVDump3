using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Core {
    public abstract class XmlReport : IReport {
        protected abstract XDocument Report { get; }


        public string FileExtension { get; } = "xml";

        public string ReportToString() {
            using(var textWriter = new StringWriter())
            using(var safeXmlWriter = new SafeXmlWriter(textWriter)) {
                Report.WriteTo(safeXmlWriter);
                return textWriter.ToString();
            }
        }

        public XDocument ReportToXml() { return new XDocument(Report); }

        public void SaveToFile(string filePath) {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using(var safeXmlWriter = new SafeXmlWriter(filePath, Encoding.UTF8)) {
                Report.WriteTo(safeXmlWriter);
            }
        }
    }
}
