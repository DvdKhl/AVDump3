using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Reporting.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Reports {
	public class MatroskaReport : XmlReport {
		protected override XDocument Report { get; }

		public MatroskaReport(FileMetaInfo fileMetaInfo) {
			Report = new XDocument();

			var matroskaFile = fileMetaInfo.Providers.OfType<MatroskaProvider>().SingleOrDefault()?.MFI;
			if(matroskaFile == null) {
				return;
			}



			void traverse(XElement parent, Section section) {
				foreach(var item in section) {
					var child = new XElement(item.Key);
					parent.Add(child);

					if(item.Value is Section childSection) {
						traverse(child, childSection);

					} else {
						if(item.Value != null) child.Value = item.Value.ToString();
					}
				}

			}

			var rootElem = new XElement("File");
			Report.Add(rootElem);
			traverse(rootElem, matroskaFile);

		}
	}
}
