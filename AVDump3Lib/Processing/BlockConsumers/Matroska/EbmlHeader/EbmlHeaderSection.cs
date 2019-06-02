using BXmlLib;
using BXmlLib.DocTypes.Ebml;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.EbmlHeader {
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

		protected sealed override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == EbmlDocType.DocType) {
				docType = (string)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.DocTypeReadVersion) {
				docTypeReadVersion = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.DocTypeVersion) {
				docTypeVersion = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.EbmlMaxIDLength) {
				ebmlMaxIdLength = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.EbmlMaxSizeLength) {
				ebmlMaxSizeLength = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.EbmlReadVersion) {
				ebmlReadVersion = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == EbmlDocType.EbmlVersion) {
				ebmlVersion = (ulong)reader.RetrieveValue();
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
