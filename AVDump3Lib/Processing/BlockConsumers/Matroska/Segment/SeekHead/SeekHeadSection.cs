using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SeekHead {
    public class SeekHeadSection : Section {
		public EbmlList<SeekSection> Seeks { get; private set; }


		public SeekHeadSection() { Seeks = new EbmlList<SeekSection>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.Seek) {
				Section.CreateReadAdd(new SeekSection(), reader, Seeks);
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
