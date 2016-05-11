using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.SeekHead {
    public class SeekHeadSection : Section {
		public EbmlList<SeekSection> Seeks { get; private set; }


		public SeekHeadSection() { Seeks = new EbmlList<SeekSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elementInfo) {
			if(elementInfo.DocElement.Id == MatroskaDocType.Seek.Id) {
				Section.CreateReadAdd(new SeekSection(), reader, elementInfo, Seeks);
				return true;
			}
			return false;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var seek in Seeks) yield return CreatePair("Seek", seek);
		}


	}

}
