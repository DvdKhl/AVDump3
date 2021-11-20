using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters;

public class ChapterProcessSection : Section {
	private ulong? chapterProcessCodecId;

	public ulong ChapterProcessCodecId => chapterProcessCodecId ?? 0;
	public EbmlList<ChapterProcessCommandSection> ChapterProcessCommands { get; private set; }
	//ChapProcessPrivate

	public ChapterProcessSection() { ChapterProcessCommands = new EbmlList<ChapterProcessCommandSection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.ChapProcessCommand) {
			Section.CreateReadAdd(new ChapterProcessCommandSection(), reader, ChapterProcessCommands);
		} else if(reader.DocElement == MatroskaDocType.ChapProcessCodecID) {
			chapterProcessCodecId = (ulong)reader.RetrieveValue();
		} else return false;

		return true;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("ChapterProcessCodecId", ChapterProcessCodecId);
		foreach(var chapterProcessCommand in ChapterProcessCommands) yield return CreatePair("ChapterProcessCommand", chapterProcessCommand);
	}
}
