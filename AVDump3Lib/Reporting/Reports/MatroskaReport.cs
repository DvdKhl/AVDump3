using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Reporting.Core;
using System;
using System.Linq;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Reports {
	public class MatroskaReport : XmlReport {
		protected override XDocument Report { get; }

		public MatroskaReport(FileMetaInfo fileMetaInfo) {
			Report = new XDocument();
			var rootElem = new XElement("File");
			Report.Add(rootElem);

			var matroskaFile = fileMetaInfo.Providers.OfType<MatroskaProvider>().SingleOrDefault()?.MFI;
			if(matroskaFile == null) {
				return;
			}

			static void traverse(XElement parent, Section section) {
				foreach(var item in section) {
					var child = new XElement(item.Key);
					parent.Add(child);

					if(item.Value is Section childSection) {
						traverse(child, childSection);

					} else {
						if(item.Value != null) {

							if(item.Value is byte[] b) {
								child.Add(new XAttribute("Size", b.Length));
								if(b.Length > 0) {
									child.Value = BitConverter.ToString(b, 0, Math.Min(1024, b.Length)).Replace("-", "") + (b.Length > 1024 ? "..." : "");
								}
							} else {
								child.Value = item.Value.ToString();
							}
						}
					}
				}

			}

			traverse(rootElem, matroskaFile);

		}
	}
}
