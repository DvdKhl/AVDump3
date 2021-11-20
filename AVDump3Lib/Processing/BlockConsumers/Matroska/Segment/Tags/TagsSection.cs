using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;

public class TagsSection : Section {
	public EbmlList<TagSection> Items { get; private set; }

	public TagsSection() { Items = new EbmlList<TagSection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.Tag) {
			Section.CreateReadAdd(new TagSection(), reader, Items);
		} else return false;

		return true;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		foreach(var tag in Items) yield return CreatePair("Tag", tag);
	}


}
