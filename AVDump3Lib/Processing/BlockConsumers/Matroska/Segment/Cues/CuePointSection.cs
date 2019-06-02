using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues {
	public class CuePointSection : Section {
		public EbmlList<CueTrackPositionsSection> CueTrackPositions { get; private set; }
		public ulong CueTime { get; private set; }

		public CuePointSection() { CueTrackPositions = new EbmlList<CueTrackPositionsSection>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.CueTrackPositions) {
				Section.CreateReadAdd(new CueTrackPositionsSection(), reader, CueTrackPositions);
			} else if(reader.DocElement == MatroskaDocType.CueTime) {
				CueTime = (ulong)reader.RetrieveValue();
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
