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
    public class AVD3Report : XmlReport {
        protected override XDocument Report { get; }


        public AVD3Report(FileMetaInfo fileMetaInfo) {
            var xDoc = new XDocument();
            var rootElem = new XElement("FileInfo");
            xDoc.Add(rootElem);

            rootElem.Add(new XElement("Path", fileMetaInfo.FileInfo.FullName));
            rootElem.Add(new XElement("Size", fileMetaInfo.FileInfo.Length));

            foreach(var provider in fileMetaInfo.CondensedProviders) {
                rootElem.Add(BuildReportMedia(provider));
            }

            Report = xDoc;
        }

		private static string GetDisplayTypeName(Type type) {
			if (type == typeof(ReadOnlyMemory<byte>)) {
				return "Binary";
			} else {
				return type.Name;
			}
		}

        public XElement BuildReportMedia(MetaInfoContainer container) {
            var rootElem = new XElement(container.Type?.Name ?? container.GetType().Name);

            foreach(var item in container.Items) {
                object valueStr;
                if(item.Type.ValueType == typeof(byte[])) {
                    valueStr = BitConverter.ToString((byte[])item.Value).Replace("-", "");
				} else if (item.Value is ReadOnlyMemory<byte>) {
					valueStr = BitConverter.ToString(((ReadOnlyMemory<byte>)item.Value).ToArray()).Replace("-", "");
				} else if (item.Value is ICollection) {
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
                    new XAttribute("t", GetDisplayTypeName(item.Type.ValueType)),
                    new XAttribute("u", item.Type.Unit ?? "Unknown"),
                    valueStr
                ));
            }

            foreach(var node in container.Nodes) {
                rootElem.Add(BuildReportMedia(node));
            }

            return rootElem;
        }
    }
}
