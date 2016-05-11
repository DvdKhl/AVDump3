using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Tracks {
    public class ContentCompressionSection : Section {
		private CompAlgos? contentCompAlgo;

		public CompAlgos ContentCompAlgo { get { return contentCompAlgo ?? CompAlgos.zlib; } }
		//ContentCompSetting

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ContentCompAlgo.Id) {
				contentCompAlgo = (CompAlgos)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("ContentCompAlgo", ContentCompAlgo);
		}

		public enum CompAlgos { zlib = 0, bzlib = 1, lzo1x = 2, HeaderScripting = 3 }
	}
}
