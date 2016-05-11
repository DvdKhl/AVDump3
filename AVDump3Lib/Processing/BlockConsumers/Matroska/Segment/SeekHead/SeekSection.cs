using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.SeekHead {
    public class SeekSection : Section {
		private byte[] seekId;

		public byte[] SeekId { get { return seekId != null ? (byte[])seekId.Clone() : null; } }
		public ulong SeekPosition { get; private set; }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.SeekID.Id) {
				seekId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SeekPosition.Id) {
				SeekPosition = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}

		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("SeekId", SeekId);
			yield return CreatePair("SeekPosition", SeekPosition);
		}
	}
}
