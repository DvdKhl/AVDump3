using System.Collections.Generic;
using CSEBML;
using CSEBML.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags {
    public class TagsSection : Section {
		public EbmlList<TagSection> Items { get; private set; }

		public TagsSection() { Items = new EbmlList<TagSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.Tag.Id) {
				Section.CreateReadAdd(new TagSection(), reader, elemInfo, Items);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var tag in Items) yield return CreatePair("Tag", tag);
		}


	}
}
