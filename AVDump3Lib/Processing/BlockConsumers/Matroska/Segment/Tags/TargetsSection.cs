using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags {
    public class TargetsSection : Section {
		private ulong? targetTypeValue;
		private EbmlList<ulong> trackUId, editionUId, chapterUId, attachmentUId;

		public ulong TargetTypeValue { get { return targetTypeValue.HasValue ? targetTypeValue.Value : 50; } } //Def: 50
		public string TargetType { get; private set; }
		public EbmlList<ulong> TrackUIds { get { return trackUId.Count != 0 ? trackUId : new EbmlList<ulong>(new ulong[] { 0 }); } }
		public EbmlList<ulong> EditionUIds { get { return editionUId.Count != 0 ? editionUId : new EbmlList<ulong>(new ulong[] { 0 }); } }
		public EbmlList<ulong> ChapterUIds { get { return chapterUId.Count != 0 ? chapterUId : new EbmlList<ulong>(new ulong[] { 0 }); } }
		public EbmlList<ulong> AttachmentUIds { get { return attachmentUId.Count != 0 ? attachmentUId : new EbmlList<ulong>(new ulong[] { 0 }); } }
		//public EbmlList<SimpleTagSection> SimpleTags { get; private set; }


		public TargetsSection() {
			trackUId = new EbmlList<ulong>();
			editionUId = new EbmlList<ulong>();
			chapterUId = new EbmlList<ulong>();
			attachmentUId = new EbmlList<ulong>();
			//SimpleTags = new EbmlList<SimpleTagSection>();
		}

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.TargetTypeValue.Id) {
				targetTypeValue = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TargetType.Id) {
				TargetType = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagTrackUID.Id) {
				trackUId.Add((ulong)reader.RetrieveValue(elemInfo));
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagEditionUID.Id) {
				editionUId.Add((ulong)reader.RetrieveValue(elemInfo));
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagChapterUID.Id) {
				chapterUId.Add((ulong)reader.RetrieveValue(elemInfo));
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagAttachmentUID.Id) {
				attachmentUId.Add((ulong)reader.RetrieveValue(elemInfo));
			//} else if(elemInfo.DocElement.Id == MatroskaDocType.SimpleTag.Id) {
			//	Section.CreateReadAdd(new SimpleTagSection(), reader, elemInfo, SimpleTags);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("TargetTypeValue", TargetTypeValue);
			yield return CreatePair("TargetType", TargetType);
			foreach(var trackUId in TrackUIds) yield return CreatePair("TrackUId", trackUId);
			foreach(var editionUId in EditionUIds) yield return CreatePair("EditionUId", editionUId);
			foreach(var chapterUId in ChapterUIds) yield return CreatePair("ChapterUId", chapterUId);
			foreach(var attachmentUId in AttachmentUIds) yield return CreatePair("AttachmentUId", attachmentUId);
			//foreach(var simpleTag in SimpleTags) yield return CreatePair("SimpleTag", simpleTag);
		}

	}
}
