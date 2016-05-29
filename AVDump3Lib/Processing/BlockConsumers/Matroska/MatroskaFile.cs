using System;
using System.Collections.Generic;
using System.Threading;
using CSEBML;
using CSEBML.DocTypes;
using CSEBML.DocTypes.Matroska;
using AVDump3Lib.Processing.BlockConsumers.Matroska.EbmlHeader;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
	public class MatroskaFile : Section {
		public EbmlHeaderSection EbmlHeader { get; private set; }
		public SegmentSection Segment { get; private set; }


		public MatroskaFile(long fileSize) { SectionSize = fileSize; }

		internal void Parse(EBMLReader reader, CancellationToken ct) {
			var elementInfo = reader.Next();
			if(elementInfo.DocElement.Id == EBMLDocType.EBMLHeader.Id) {
				EbmlHeader = Section.CreateRead(new EbmlHeaderSection(), reader, elementInfo);
			} else {
				//Todo: dispose reader / add warning
				return;
			}

			//while((elementInfo = reader.Next()) != null && Section.IsGlobalElement(elementInfo)) ;
			while((elementInfo = reader.Next()) != null && elementInfo.DocElement.Id != MatroskaDocType.Segment.Id && elementInfo.DocElement.Id != MatroskaDocType.Info.Id) {
				if(reader.BaseStream.Position > 4 * 1024 * 1024) {
					elementInfo = null;
					break;
				}
			}

			if(elementInfo != null && elementInfo.DocElement.Id == MatroskaDocType.Segment.Id) {
				Segment = Section.CreateRead(new SegmentSection(), reader, elementInfo);
			} else if(elementInfo != null && elementInfo.DocElement.Id == MatroskaDocType.Info.Id) {
				Segment = new SegmentSection();
				Segment.ContinueRead(reader, elementInfo);
			} else {
				//Todo: dispose reader / add warning
				return;
			}

			Validate();
		}

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elementInfo) { throw new NotSupportedException(); }

		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("EbmlHeader", EbmlHeader);
			yield return CreatePair("Segment", Segment);
		}
	}
}
