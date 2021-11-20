using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters;

public class ChaptersSection : Section {
	public EbmlList<EditionEntrySection> Items { get; private set; }

	public ChaptersSection() { Items = new EbmlList<EditionEntrySection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.EditionEntry) {
			Section.CreateReadAdd(new EditionEntrySection(), reader, Items);
			return true;
		}
		return false;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() { foreach(var item in Items) yield return CreatePair("EditionEntry", item); }
}
