using System.Collections.Generic;
using CSEBML;
using CSEBML.DocTypes;

namespace AVDump3Lib.BlockConsumers.Matroska.EbmlHeader {
    public class EbmlHeaderSection : Section {
		private ulong? ebmlVersion;
		private ulong? ebmlReadVersion;
		private ulong? ebmlMaxIdLength;
		private ulong? ebmlMaxSizeLength;
		private string docType;
		private ulong? docTypeVersion;
		private ulong? docTypeReadVersion;

		public ulong EbmlVersion { get { return ebmlVersion ?? 1; } }
		public ulong EbmlReadVersion { get { return ebmlReadVersion ?? 1; } }
		public ulong EbmlMaxIdLength { get { return ebmlMaxIdLength ?? 4; } }
		public ulong EbmlMaxSizeLength { get { return ebmlMaxSizeLength ?? 8; } }
		public ulong DocTypeReadVersion { get { return docTypeReadVersion ?? 1; } }
		public ulong DocTypeVersion { get { return docTypeVersion ?? 1; } }
		public string DocType { get { return docType ?? "matroska"; } }

		protected sealed override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == EBMLDocType.DocType.Id) {
				docType = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.DocTypeReadVersion.Id) {
				docTypeReadVersion = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.DocTypeVersion.Id) {
				docTypeVersion = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.EBMLMaxIDLength.Id) {
				ebmlMaxIdLength = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.EBMLMaxSizeLength.Id) {
				ebmlMaxSizeLength = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.EBMLReadVersion.Id) {
				ebmlReadVersion = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == EBMLDocType.EBMLVersion.Id) {
				ebmlVersion = (ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected sealed override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("EbmlVersion", EbmlVersion);
			yield return CreatePair("EbmlReadVersion", EbmlReadVersion);
			yield return CreatePair("EbmlMaxIdLength", EbmlMaxIdLength);
			yield return CreatePair("EbmlMaxSizeLength", EbmlMaxSizeLength);
			yield return CreatePair("DocTypeReadVersion", DocTypeReadVersion);
			yield return CreatePair("DocTypeVersion", DocTypeVersion);
			yield return CreatePair("DocType", DocType);
		}
	}
}
