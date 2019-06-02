using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
	public class ChapterProcessCommandSection : Section {
		public ProcessTime? ChapterProcessTime { get; private set; }
		//ChapProcessData

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ChapProcessTime) {
				ChapterProcessTime = (ProcessTime)reader.RetrieveValue();
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
