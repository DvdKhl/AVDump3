using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Chapters {
    public class ChapterTrackSection : Section {
		public EbmlList<ulong> ChapterTrackNumbers { get; private set; }

		public ChapterTrackSection() { ChapterTrackNumbers = new EbmlList<ulong>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTrackNumber.Id) {
				ChapterTrackNumbers.Add((ulong)reader.RetrieveValue(elemInfo));
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var chapterTrackNumber in ChapterTrackNumbers) yield return new KeyValuePair<string, object>("ChapterTrackNumber", chapterTrackNumber);
		}
	}
}
