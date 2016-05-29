using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
    public class ChapterProcessCommandSection : Section {
		public ProcessTime? ChapterProcessTime { get; private set; }
		//ChapProcessData

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapProcessTime.Id) {
				ChapterProcessTime = (ProcessTime)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("ChapterProcessTime", ChapterProcessTime);
		}

		public enum ProcessTime { During, Before, After }
	}
}
