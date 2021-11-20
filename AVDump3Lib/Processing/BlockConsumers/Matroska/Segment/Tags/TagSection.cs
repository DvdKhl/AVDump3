using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;

public class TagSection : Section {
	public TargetsSection Targets { get; private set; }
	public EbmlList<SimpleTagSection> SimpleTags { get; private set; }

	public TagSection() { SimpleTags = new EbmlList<SimpleTagSection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.Targets) {
			Targets = Section.CreateRead(new TargetsSection(), reader);
		} else if(reader.DocElement == MatroskaDocType.SimpleTag) {
			Section.CreateReadAdd(new SimpleTagSection(), reader, SimpleTags);
		} else return false;

		return true;
	}
	protected override void Validate() { if(Targets == null) Targets = new TargetsSection(); }
	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return new KeyValuePair<string, object>("Targets", Targets);
		foreach(var simpleTag in SimpleTags) yield return CreatePair("SimpleTag", simpleTag);
	}
}
