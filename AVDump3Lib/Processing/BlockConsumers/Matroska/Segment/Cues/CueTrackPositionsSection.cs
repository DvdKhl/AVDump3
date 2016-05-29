using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues {
    public class CueTrackPositionsSection : Section {
		public EbmlList<CueReferenceSection> CueReferences { get; private set; }

		private ulong? cueBlockNumber;
		private ulong? cueCodecState;

		public ulong CueTrack { get; private set; }
		public ulong CueClusterPosition { get; private set; }
		public ulong? CueRelativePosition { get; private set; }
		public ulong? CueDuration { get; private set; }
		public ulong CueBlockNumber { get { return cueBlockNumber.HasValue && cueBlockNumber.Value == 0 ? 1 : (cueBlockNumber ?? 1); } } //Default: 1, not 0
		public ulong CueCodecState { get { return cueCodecState ?? 0; } } //Default: 0

		public CueTrackPositionsSection() { CueReferences = new EbmlList<CueReferenceSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.CueReference.Id) {
				Section.CreateReadAdd(new CueReferenceSection(), reader, elemInfo, CueReferences);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueTrack.Id) {
				CueTrack = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueClusterPosition.Id) {
				CueClusterPosition = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueRelativePosition.Id) {
				CueRelativePosition = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueDuration.Id) {
				CueDuration = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueBlockNumber.Id) {
				cueBlockNumber = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueCodecState.Id) {
				cueCodecState = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}


		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var cueReference in CueReferences) yield return CreatePair("CueReferences", cueReference);
			yield return CreatePair("CueTrack", CueTrack);
			yield return CreatePair("CueClusterPosition", CueClusterPosition);
			yield return CreatePair("CueRelativePosition", CueRelativePosition);
			yield return CreatePair("CueDuration", CueDuration);
			yield return CreatePair("CueBlockNumber", CueBlockNumber);
			yield return CreatePair("CueCodecState", CueCodecState);
		}
	}
}
