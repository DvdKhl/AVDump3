using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Cues {
    public class CueReferenceSection : Section {
		private ulong? cueRefCodecState;
		private ulong? cueRefNumber;

		public ulong CueClusterPosition { get; private set; }
		public ulong CueRefCluster { get; private set; }

		public ulong CueRefNumber { get { return cueRefNumber.HasValue && cueRefNumber.Value == 0 ? 1 : (cueRefNumber ?? 1); } } //Default: 1, not 0
		public ulong CueRefCodecState { get { return cueRefCodecState ?? 0; } } //Default: 0

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.CueClusterPosition.Id) {
				CueClusterPosition = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueRefCluster.Id) {
				CueRefCluster = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueRefNumber.Id) {
				cueRefNumber = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.CueRefCodecState.Id) {
				cueRefCodecState = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}

		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("CueClusterPosition", CueClusterPosition);
			yield return CreatePair("CueRefCluster", CueRefCluster);
			yield return CreatePair("CueRefNumber", CueRefNumber);
			yield return CreatePair("CueRefCodecState", CueRefCodecState);
		}
	}
}
