using CSEBML;
using CSEBML.DocTypes.Matroska;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks {
    public class ContentEncodingSection : Section {
		private ulong? contentEncodingOrder;
		private CEScopes? contentEncodingScope;
		private CETypes? contentEncodingType;

		public ulong? ContentEncodingOrder { get { return contentEncodingOrder ?? 0; } } //Default: 0
		public CEScopes ContentEncodingScope { get { return contentEncodingScope ?? CEScopes.AllFrames; } } //Default: 1
		public CETypes ContentEncodingType { get { return contentEncodingType ?? CETypes.Compression; } } //Default: 0

		public ContentCompressionSection ContentCompression { get; private set; }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncodingOrder.Id) {
				contentEncodingOrder = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncodingScope.Id) {
				contentEncodingScope = (CEScopes)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncodingType.Id) {
				contentEncodingType = (CETypes)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ContentCompression.Id) {
				ContentCompression = Section.CreateRead(new ContentCompressionSection(), reader, elemInfo);
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
}
