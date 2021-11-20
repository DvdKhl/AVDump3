using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues;

public class CueReferenceSection : Section {
	private ulong? cueRefCodecState;
	private ulong? cueRefNumber;

	public ulong CueClusterPosition { get; private set; }
	public ulong CueRefCluster { get; private set; }

	public ulong CueRefNumber => cueRefNumber.HasValue && cueRefNumber.Value == 0 ? 1 : (cueRefNumber ?? 1);  //Default: 1, not 0
	public ulong CueRefCodecState => cueRefCodecState ?? 0;  //Default: 0

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.CueClusterPosition) {
			CueClusterPosition = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.CueRefCluster) {
			CueRefCluster = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.CueRefNumber) {
			cueRefNumber = (ulong)reader.RetrieveValue();
		} else if(reader.DocElement == MatroskaDocType.CueRefCodecState) {
			cueRefCodecState = (ulong)reader.RetrieveValue();
		} else return false;

		return true;
	}

	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		yield return CreatePair("CueClusterPosition", CueClusterPosition);
		yield return CreatePair("CueRefCluster", CueRefCluster);
		yield return CreatePair("CueRefNumber", CueRefNumber);
		yield return CreatePair("CueRefCodecState", CueRefCodecState);
	}
}
