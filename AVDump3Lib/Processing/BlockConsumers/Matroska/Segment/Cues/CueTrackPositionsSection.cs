using BXmlLib;
using BXmlLib.DocTypes.Matroska;
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

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.CueReference) {
				Section.CreateReadAdd(new CueReferenceSection(), reader, CueReferences);
			} else if(reader.DocElement == MatroskaDocType.CueTrack) {
				CueTrack = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CueClusterPosition) {
				CueClusterPosition = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CueRelativePosition) {
				CueRelativePosition = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CueDuration) {
				CueDuration = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CueBlockNumber) {
				cueBlockNumber = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CueCodecState) {
				cueCodecState = (ulong)reader.RetrieveValue();
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
