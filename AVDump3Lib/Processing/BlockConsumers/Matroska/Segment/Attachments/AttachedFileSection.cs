using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Attachments {
	public class AttachedFileSection : Section {
		public string FileDescription { get; private set; }
		public string FileName { get; private set; }
		public string FileMimeType { get; private set; }
		public ulong? FileUId { get; private set; }
		public ulong FileDataSize { get; private set; }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.FileDescription) {
				FileDescription = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.FileName) {
				FileName = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.FileMimeType) {
				FileMimeType = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.FileUID) {
				FileUId = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.FileData) {
				FileDataSize = (ulong)reader.Header.DataLength;
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
