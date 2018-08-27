using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
    public class ChapterProcessSection : Section {
		private ulong? chapterProcessCodecId;

		public ulong ChapterProcessCodecId { get { return chapterProcessCodecId.HasValue ? chapterProcessCodecId.Value : 0; } }
		public EbmlList<ChapterProcessCommandSection> ChapterProcessCommands { get; private set; }
		//ChapProcessPrivate

		public ChapterProcessSection() { ChapterProcessCommands = new EbmlList<ChapterProcessCommandSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapProcessCommand.Id) {
				Section.CreateReadAdd(new ChapterProcessCommandSection(), reader, elemInfo, ChapterProcessCommands);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapProcessCodecID.Id) {
				chapterProcessCodecId = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("ChapterProcessCodecId", ChapterProcessCodecId);
			foreach(var chapterProcessCommand in ChapterProcessCommands) yield return CreatePair("ChapterProcessCommand", chapterProcessCommand);
		}
	}
}
