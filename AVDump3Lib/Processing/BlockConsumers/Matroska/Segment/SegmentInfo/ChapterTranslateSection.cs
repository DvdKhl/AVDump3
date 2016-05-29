using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SegmentInfo {
    public class ChapterTranslateSection : Section {
		private byte[] id;

		public EbmlList<ulong> EditionUId { get; private set; }
		public ulong? Codec { get; private set; }
		public byte[] Id { get { return id != null ? (byte[])id.Clone() : null; } }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTranslateEditionUID.Id) {
				EditionUId.Add((ulong)reader.RetrieveValue(elemInfo));
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTranslateCodec.Id) {
				Codec = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTranslateID.Id) {
				id = (byte[])reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("EditionUId", EditionUId);
			yield return CreatePair("Codec", Codec);
			yield return CreatePair("Id", Id);
		}
	}
}
