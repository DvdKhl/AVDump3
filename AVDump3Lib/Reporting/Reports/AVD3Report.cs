using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using AVDump3Lib.Reporting.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Reports {
    public class AVD3Report : IReport {
        private XDocument report;


        public AVD3Report(FileMetaInfo fileMetaInfo) {
            var xDoc = new XDocument();
            var rootElem = new XElement("FileInfo");
            xDoc.Add(rootElem);

            rootElem.Add(new XElement("Path", fileMetaInfo.FileInfo.FullName));
            rootElem.Add(new XElement("Size", fileMetaInfo.FileInfo.Length));

            foreach(var provider in fileMetaInfo.CondensedProviders) {
                rootElem.Add(BuildReportMedia(provider));
            }

            report = xDoc;
        }

        public string FileExtension { get; } = "xml";

        public XElement BuildReportMedia(MetaInfoContainer container) {
            var rootElem = new XElement(container.Type?.Name ?? container.GetType().Name);

            foreach(var item in container.Items) {
                object valueStr;
                if(item.Type.ValueType == typeof(byte[])) {
                    valueStr = BitConverter.ToString((byte[])item.Value).Replace("-", "");
                } else if(item.Value is ICollection) {
                    var values = new List<XElement>();
                    foreach(var itemValue in (IEnumerable)item.Value) {
                        values.Add(new XElement("Item", itemValue));
                    }
                    valueStr = values;

                } else {
                    valueStr = item.Value.ToString();
                }

                rootElem.Add(new XElement(item.Type.Key,
                    new XAttribute("p", item.Provider.Name),
                    new XAttribute("t", item.Type.ValueType.Name),
                    new XAttribute("u", item.Type.Unit ?? "Unknown"),
                    valueStr
                ));
            }

            foreach(var node in container.Nodes) {
                rootElem.Add(BuildReportMedia(node));
            }

            return rootElem;
        }

        public string ReportToString() {
            using(var textWriter = new StringWriter())
            using(var safeXmlWriter = new SafeXmlWriter(textWriter)) {
                report.WriteTo(safeXmlWriter);
                return textWriter.ToString();
            }
        }

        public XDocument ReportToXml() { return new XDocument(report); }

        public void SaveToFile(string filePath) {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using(var safeXmlWriter = new SafeXmlWriter(filePath, Encoding.UTF8)) {
                report.WriteTo(safeXmlWriter);
            }
        }
    }
}
