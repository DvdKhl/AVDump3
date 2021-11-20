using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SegmentInfo;

public class ChapterTranslateSection : Section {
	private byte[] id;

	public EbmlList<ulong> EditionUId { get; private set; }
	public ulong? Codec { get; private set; }
	public byte[] Id => id != null ? (byte[])id.Clone() : null;

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.ChapterTranslateEditionUID) {
			EditionUId.Add((ulong)reader.RetrieveValue());
		} else if(reader.DocElement == MatroskaDocType.ChapterTranslateCodec) {
			Codec = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.ChapterTranslateID) {
			id = (byte[])reader.RetrieveValue();
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
