using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Cues {
    public class CuesSection : Section {
		public EbmlList<CuePointSection> CuePoints { get; private set; }


		public CuesSection() { CuePoints = new EbmlList<CuePointSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elementInfo) {
			if(elementInfo.DocElement.Id == MatroskaDocType.CuePoint.Id) {
				Section.CreateReadAdd(new CuePointSection(), reader, elementInfo, CuePoints);
				return true;
			}
			return false;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var cuePoint in CuePoints) yield return CreatePair("CuePoint", cuePoint);
		}

	}


}
