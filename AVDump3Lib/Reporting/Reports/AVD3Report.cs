using AVDump2Lib.InfoProvider.Tools;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Media;
using AVDump3Lib.Reporting.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Reports {
	public class AVD3Report : IReport {

		public AVD3Report(FileMetaInfo fileMetaInfo) {
			var xDoc = new XDocument();
			var rootElem = new XElement("FileInfo");
			xDoc.Add(rootElem);

			rootElem.Add(new XElement("Path", fileMetaInfo.FileInfo.FullName));
			rootElem.Add(new XElement("Size", fileMetaInfo.FileInfo.Length));

			foreach(var provider in fileMetaInfo.CondensedProviders) {
				rootElem.Add(BuildReportMedia(provider));
			}
		}
		public XElement BuildReportMedia(MetaInfoContainer container) {
			var rootElem = new XElement(container.Type.Name);

			foreach(var item in container.Items) {
				string valueStr;
				if(item.Type.ValueType == typeof(byte[])) {
					valueStr = BitConverter.ToString((byte[])item.Value).Replace("-", "");
				} else {
					valueStr = item.Value.ToString();
				}

				rootElem.Add(new XElement(item.Type.Key,
					new XAttribute("p", item.Provider.Name),
					new XAttribute("u", item.Type.ValueType?.Name),
					valueStr
				));
			}

			return rootElem;
		}
	}
}
