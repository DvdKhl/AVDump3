using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SeekHead {
    public class SeekSection : Section {
		private byte[] seekId;

		public byte[] SeekId { get { return seekId != null ? (byte[])seekId.Clone() : null; } }
		public ulong SeekPosition { get; private set; }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.SeekID) {
				seekId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.SeekPosition) {
				SeekPosition = (ulong)reader.RetrieveValue();
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
