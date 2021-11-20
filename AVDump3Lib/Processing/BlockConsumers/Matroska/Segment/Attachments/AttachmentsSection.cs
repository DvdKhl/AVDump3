using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Attachments;

public class AttachmentsSection : Section {
	public EbmlList<AttachedFileSection> Items { get; private set; }

	public AttachmentsSection() { Items = new EbmlList<AttachedFileSection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.AttachedFile) {
			CreateReadAdd(new AttachedFileSection(), reader, Items);
			return true;
		}
		return false;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		foreach(var attachedFile in Items) yield return CreatePair("AttachedFile", attachedFile);
	}
}
