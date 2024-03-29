using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;

public class ContentEncodingSection : Section {
	private ulong? contentEncodingOrder;
	private CEScopes? contentEncodingScope;
	private CETypes? contentEncodingType;

	public ulong? ContentEncodingOrder => contentEncodingOrder ?? 0;  //Default: 0
	public CEScopes ContentEncodingScope => contentEncodingScope ?? CEScopes.AllFrames;  //Default: 1
	public CETypes ContentEncodingType => contentEncodingType ?? CETypes.Compression;  //Default: 0

	public ContentCompressionSection ContentCompression { get; private set; }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.ContentEncodingOrder) {
			contentEncodingOrder = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.ContentEncodingScope) {
			contentEncodingScope = (CEScopes)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.ContentEncodingType) {
			contentEncodingType = (CETypes)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.ContentCompression) {
			ContentCompression = Section.CreateRead(new ContentCompressionSection(), reader);
		} else return false;

		return true;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("ContentEncodingOrder", ContentEncodingOrder);
		yield return CreatePair("ContentEncodingScope", ContentEncodingScope);
		yield return CreatePair("ContentEncodingType", ContentEncodingType);
		yield return CreatePair("ContentCompression", ContentCompression);
	}


	[Flags]
	public enum CEScopes { AllFrames = 1, CodecPrivate = 2, ContentCompression = 4 }
	public enum CETypes { Compression = 0, Encryption = 1 }
}
