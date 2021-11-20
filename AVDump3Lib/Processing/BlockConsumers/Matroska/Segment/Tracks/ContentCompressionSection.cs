using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;

public class ContentCompressionSection : Section {
	private CompAlgos? contentCompAlgo;

	public CompAlgos ContentCompAlgo => contentCompAlgo ?? CompAlgos.zlib;
	//ContentCompSetting

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.ContentCompAlgo) {
			contentCompAlgo = (CompAlgos)reader.RetrieveValue();
		} else return false;

		return true;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("ContentCompAlgo", ContentCompAlgo);
	}

	public enum CompAlgos { zlib = 0, bzlib = 1, lzo1x = 2, HeaderScripting = 3 }
}
