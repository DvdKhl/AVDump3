using AVDump3Lib.Information.InfoProvider;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Reporting.Core;
using ExtKnot.StringInvariants;
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

			using var mediaInfo = new MediaInfoLibNativeMethods();
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
						name = mediaInfo.Get(j, streamKind, i, InfoTypes.Name).InvReplace("/", "-").InvReplace("(", "").InvReplace(")", "").InvReplace(" ", "_");
						if(name.InvEquals("Chapters_Pos_End") || name.InvEquals("Chapters_Pos_Begin") || name.InvContains("-String")) continue;
						if(name.InvEquals("Bits-Pixel*Frame")) name = "BitsPerPixel";

						text = mediaInfo.Get(j, streamKind, i, InfoTypes.Text);
						measure = mediaInfo.Get(j, streamKind, i, InfoTypes.Measure).Trim();

						if(name.IndexOfAny(new char[] { ')', ':' }) < 0 && !string.IsNullOrEmpty(text)) {
							subNode.Add(new XElement(name, text, new XAttribute("Unit", measure)));
						} else {
							//Debug.Print(name + " " + text + " " + measure);
						}
					}
					if(streamKind == StreamTypes.Menu) {
						XElement chapterNode;
						if(
							int.TryParse(mediaInfo.Get("Chapters_Pos_Begin", streamKind, i), out var indexStart) &&
							int.TryParse(mediaInfo.Get("Chapters_Pos_End", streamKind, i), out var indexEnd)
						) {
							chapterNode = new XElement("Chapters");
							subNode.Add(chapterNode);
							for(; indexStart < indexEnd; indexStart++) {
								chapterNode.Add(new XElement("Chapter", mediaInfo.Get(indexStart, streamKind, i, InfoTypes.Text), new XAttribute("TimeStamp", mediaInfo.Get(indexStart, streamKind, i, InfoTypes.Name))));
							}
						}
					}
				}
			}

		}
	}
}
