using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Reporting.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using static AVDump3Lib.Information.InfoProvider.MediaInfoLibNativeMethods;

namespace AVDump3Lib.Reporting.Reports {
	public class MediaInfoLibXmlReport : XmlReport {
		protected override XDocument Report { get; }

		public MediaInfoLibXmlReport(string filePath) {
			Report = new XDocument();


			var node = new XElement("File");
			Report.Add(node);

			XElement subNode;

			using(var mediaInfo = new MediaInfoLibNativeMethods()) {
				mediaInfo.Open(filePath);

				int streamCount, entryCount;
				string name, text, measure;
				foreach(StreamTypes streamKind in Enum.GetValues(typeof(StreamTypes))) {
					streamCount = mediaInfo.GetCount(streamKind);

					for(var i = 0; i < streamCount; i++) {
						entryCount = mediaInfo.GetCount(streamKind, i);
						subNode = new XElement(streamKind.ToString());
						node.Add(subNode);

						for(var j = 0; j < entryCount; j++) {
							name = mediaInfo.Get(streamKind, i, j, InfoTypes.Name).Replace("/", "-").Replace("(", "").Replace(")", "").Replace(" ", "_");
							if(name.Equals("Chapters_Pos_End") || name.Equals("Chapters_Pos_Begin") || name.Contains("-String")) continue;
							if(name.Equals("Bits-Pixel*Frame")) name = "BitsPerPixel";

							text = mediaInfo.Get(streamKind, i, j, InfoTypes.Text);
							measure = mediaInfo.Get(streamKind, i, j, InfoTypes.Measure).Trim();

							if(name.IndexOfAny(new char[] { ')', ':' }) < 0 && !string.IsNullOrEmpty(text)) {
								subNode.Add(new XElement(name, text, new XAttribute("Unit", measure)));
							} else {
								//Debug.Print(name + " " + text + " " + measure);
							}
						}
						if(streamKind == StreamTypes.Menu) {
							int indexStart;
							int indexEnd;
							XElement chapterNode;

							if(int.TryParse(mediaInfo.Get(streamKind, i, "Chapters_Pos_Begin"), out indexStart) && int.TryParse(mediaInfo.Get(streamKind, i, "Chapters_Pos_End"), out indexEnd)) {
								chapterNode = new XElement("Chapters");
								subNode.Add(chapterNode);
								for(; indexStart < indexEnd; indexStart++) {
									chapterNode.Add(new XElement("Chapter", mediaInfo.Get(streamKind, i, indexStart, InfoTypes.Text), new XAttribute("TimeStamp", mediaInfo.Get(streamKind, i, indexStart, InfoTypes.Name))));
								}
							}
						}
					}
				}
			}

		}
	}
}
