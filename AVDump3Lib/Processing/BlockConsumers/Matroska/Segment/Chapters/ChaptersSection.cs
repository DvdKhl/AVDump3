using System.Collections.Generic;
using CSEBML;
using CSEBML.DocTypes.Matroska;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Chapters {
    public class ChaptersSection : Section {
		public EbmlList<EditionEntrySection> Items { get; private set; }

		public ChaptersSection() { Items = new EbmlList<EditionEntrySection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.EditionEntry.Id) {
				Section.CreateReadAdd(new EditionEntrySection(), reader, elemInfo, Items);
				return true;
			}
			return false;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() { foreach(var item in Items) yield return CreatePair("EditionEntry", item); }
	}
}
