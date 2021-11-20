using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;

public class TargetsSection : Section {
	private ulong? targetTypeValue;
	private readonly EbmlList<ulong> trackUId, editionUId, chapterUId, attachmentUId;

	public ulong TargetTypeValue => targetTypeValue ?? 50;  //Def: 50
	public string TargetType { get; private set; }
	public EbmlList<ulong> TrackUIds => trackUId.Count != 0 ? trackUId : new EbmlList<ulong>(new ulong[] { 0 });
	public EbmlList<ulong> EditionUIds => editionUId.Count != 0 ? editionUId : new EbmlList<ulong>(new ulong[] { 0 });
	public EbmlList<ulong> ChapterUIds => chapterUId.Count != 0 ? chapterUId : new EbmlList<ulong>(new ulong[] { 0 });
	public EbmlList<ulong> AttachmentUIds => attachmentUId.Count != 0 ? attachmentUId : new EbmlList<ulong>(new ulong[] { 0 });
	//public EbmlList<SimpleTagSection> SimpleTags { get; private set; }


	public TargetsSection() {
		trackUId = new EbmlList<ulong>();
		editionUId = new EbmlList<ulong>();
		chapterUId = new EbmlList<ulong>();
		attachmentUId = new EbmlList<ulong>();
		//SimpleTags = new EbmlList<SimpleTagSection>();
	}

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.TargetTypeValue) {
			targetTypeValue = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.TargetType) {
			TargetType = (string)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.TagTrackUID) {
			trackUId.Add((ulong)reader.RetrieveValue());
		} else if(reader.DocElement == MatroskaDocType.TagEditionUID) {
			editionUId.Add((ulong)reader.RetrieveValue());
		} else if(reader.DocElement == MatroskaDocType.TagChapterUID) {
			chapterUId.Add((ulong)reader.RetrieveValue());
		} else if(reader.DocElement == MatroskaDocType.TagAttachmentUID) {
			attachmentUId.Add((ulong)reader.RetrieveValue());
			//} else if(reader.DocElement == MatroskaDocType.SimpleTag) {
			//	Section.CreateReadAdd(new SimpleTagSection(), reader, SimpleTags);
		} else return false;

		return true;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("TargetTypeValue", TargetTypeValue);
		yield return CreatePair("TargetType", TargetType);
		foreach(var trackUId in TrackUIds) yield return CreatePair("TrackUId", trackUId);
		foreach(var editionUId in EditionUIds) yield return CreatePair("EditionUId", editionUId);
		foreach(var chapterUId in ChapterUIds) yield return CreatePair("ChapterUId", chapterUId);
		foreach(var attachmentUId in AttachmentUIds) yield return CreatePair("AttachmentUId", attachmentUId);
		//foreach(var simpleTag in SimpleTags) yield return CreatePair("SimpleTag", simpleTag);
	}

}
