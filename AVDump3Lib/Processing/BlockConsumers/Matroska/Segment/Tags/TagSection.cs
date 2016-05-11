using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Tags {
    public class TagSection : Section {
		public TargetsSection Targets { get; private set; }
		public EbmlList<SimpleTagSection> SimpleTags { get; private set; }

		public TagSection() { SimpleTags = new EbmlList<SimpleTagSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.Targets.Id) {
				Targets = Section.CreateRead(new TargetsSection(), reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SimpleTag.Id) {
				Section.CreateReadAdd(new SimpleTagSection(), reader, elemInfo, SimpleTags);
			} else return false;

			return true;
		}
		protected override void Validate() { if(Targets == null) Targets = new TargetsSection(); }
		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return new KeyValuePair<string, object>("Targets", Targets);
			foreach(var simpleTag in SimpleTags) yield return CreatePair("SimpleTag", simpleTag);
		}
	}
}
