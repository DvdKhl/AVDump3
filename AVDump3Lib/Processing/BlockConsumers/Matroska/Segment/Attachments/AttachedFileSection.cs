using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Attachments {
    public class AttachedFileSection : Section {
		public string FileDescription { get; private set; }
		public string FileName { get; private set; }
		public string FileMimeType { get; private set; }
		public ulong? FileUId { get; private set; }
		public ulong FileDataSize { get; private set; }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.FileDescription.Id) {
				FileDescription = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.FileName.Id) {
				FileName = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.FileMimeType.Id) {
				FileMimeType = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.FileUID.Id) {
				FileUId = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.FileData.Id) {
				FileDataSize = (ulong)elemInfo.DataLength.Value;
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("FileDescription", FileDescription);
			yield return CreatePair("FileName", FileName);
			yield return CreatePair("FileMimeType", FileMimeType);
			yield return CreatePair("FileUId", FileUId);
			yield return CreatePair("FileDataSize", FileDataSize);
		}
	}
}
