using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues {
    public class CuesSection : Section {
		public EbmlList<CuePointSection> CuePoints { get; private set; }


		public CuesSection() { CuePoints = new EbmlList<CuePointSection>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.CuePoint) {
				Section.CreateReadAdd(new CuePointSection(), reader, CuePoints);
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
