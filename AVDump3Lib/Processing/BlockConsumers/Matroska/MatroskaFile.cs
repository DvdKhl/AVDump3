using AVDump3Lib.Processing.BlockConsumers.Matroska.EbmlHeader;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment;
using BXmlLib;
using BXmlLib.DocTypes.Ebml;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska;

public class MatroskaFile : Section {
	public EbmlHeaderSection EbmlHeader { get; private set; }
	public SegmentSection? Segment { get; private set; }
	private long lastFilePosition;

	public bool HasMetaData() {
		var isValid = Segment != null && Segment.SegmentInfo != null && Segment.Tracks != null;
		if(Segment?.SectionSize.HasValue ?? false) {
			isValid = isValid && SectionSize - Segment.SectionSize < (1 << 20) && SectionSize / Segment.SectionSize < 1.01;
			isValid = isValid && lastFilePosition - Segment.SectionSize < (1 << 20) && lastFilePosition / Segment.SectionSize < 1.01;
		}
		return isValid;
	}

	public MatroskaFile(long fileSize) { SectionSize = fileSize; }

	internal void Parse(IBXmlReader reader, CancellationToken ct) {
		reader.Strict = true;

		if(reader.Next() && reader.DocElement == EbmlDocType.EbmlHeader) {
			EbmlHeader = CreateRead(new EbmlHeaderSection(), reader);
		} else {
			//Todo: dispose reader / add warning
			return;
		}

		reader.Strict = false;

		while(reader.Next() && reader.DocElement != MatroskaDocType.Segment && reader.DocElement != MatroskaDocType.Info) {
			if(reader.BaseStream.Position > 4 * 1024 * 1024) {
				break;
			}
		}
		ct.ThrowIfCancellationRequested();

		if(reader.DocElement != null && reader.DocElement == MatroskaDocType.Segment) {
			Segment = CreateRead(new SegmentSection(), reader);
		} else if(reader.DocElement != null && reader.DocElement == MatroskaDocType.Info) {
			Segment = new SegmentSection();
			Segment.ContinueRead(reader);
		} else {
			//Todo: dispose reader / add warning
			return;
		}

		lastFilePosition = reader.BaseStream.Position;

		Validate();
	}

	protected override bool ProcessElement(IBXmlReader reader) { throw new NotSupportedException(); }

	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("EbmlHeader", EbmlHeader);
		yield return CreatePair("Segment", Segment);
	}
}
