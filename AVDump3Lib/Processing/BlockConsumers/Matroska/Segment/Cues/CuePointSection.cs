using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Cues {
    public class CuePointSection : Section {
		public EbmlList<CueTrackPositionsSection> CueTrackPositions { get; private set; }
		public ulong CueTime { get; private set; }

		public CuePointSection() { CueTrackPositions = new EbmlList<CueTrackPositionsSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.CueTrackPositions.Id) {
				Section.CreateReadAdd(new CueTrackPositionsSection(), reader, elemInfo, CueTrackPositions);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueTime.Id) {
				CueTime = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}

		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var cueTrackPosition in CueTrackPositions) yield return CreatePair("CueTrackPositions", cueTrackPosition);
			yield return CreatePair("CueTime", CueTime);
		}
	}
}
