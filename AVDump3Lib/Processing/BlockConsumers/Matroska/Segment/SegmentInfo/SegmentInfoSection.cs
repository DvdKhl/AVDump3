using System;
using System.Collections.Generic;
using CSEBML;
using CSEBML.DocTypes.Matroska;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.SegmentInfo {
    public class SegmentInfoSection : Section {
		private ulong? timecodeScale;
		private EbmlList<byte[]> segmentFamily;
		private byte[] segmentUId, prevUId, nextUId;

		public byte[] SegmentUId { get { return segmentUId == null ? null : (byte[])segmentUId.Clone(); } }
		public byte[] PreviousUId { get { return prevUId == null ? null : (byte[])prevUId.Clone(); } }
		public byte[] NextUId { get { return nextUId == null ? null : (byte[])nextUId.Clone(); } }
		public string SegmentFilename { get; private set; }
		public string PreviousFilename { get; private set; }
		public string NextFilename { get; private set; }
		public EbmlList<byte[]> SegmentFamily { get { return segmentFamily.DeepClone(item => { return (byte[])item.Clone(); }); } }
		public ulong TimecodeScale { get { return timecodeScale ?? 1000000; } }
		public double? Duration { get; private set; }
		public string Title { get; private set; }
		public string MuxingApp { get; private set; }
		public string WritingApp { get; private set; }
		public DateTime? ProductionDate { get; private set; }

		public EbmlList<ChapterTranslateSection> ChapterTranslate { get; private set; }

		public SegmentInfoSection() {
			segmentFamily = new EbmlList<byte[]>();
			ChapterTranslate = new EbmlList<ChapterTranslateSection>();
		}

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.SegmentUID.Id) {
				segmentUId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SegmentFilename.Id) {
				SegmentFilename = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PrevUID.Id) {
				prevUId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.PrevFilename.Id) {
				PreviousFilename = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.NextUID.Id) {
				nextUId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.NextFilename.Id) {
				NextFilename = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SegmentFamily.Id) {
				segmentFamily.Add((byte[])reader.RetrieveValue(elemInfo));
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTranslate.Id) {
				Section.CreateReadAdd(new ChapterTranslateSection(), reader, elemInfo, ChapterTranslate);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TimecodeScale.Id) {
				timecodeScale = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Duration.Id) {
				Duration = (double)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Title.Id) {
				Title = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.MuxingApp.Id) {
				MuxingApp = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.WritingApp.Id) {
				WritingApp = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.DateUTC.Id) {
				ProductionDate = (DateTime)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("SegmentUId", SegmentUId);
			yield return CreatePair("PreviousUId", PreviousUId);
			yield return CreatePair("NextUId", NextUId);
			yield return CreatePair("SegmentFilename", SegmentFilename);
			yield return CreatePair("PreviousFilename", PreviousFilename);
			yield return CreatePair("NextFilename", NextFilename);
			yield return CreatePair("Duration", Duration);
			yield return CreatePair("Title", Title);
			yield return CreatePair("MuxingApp", MuxingApp);
			yield return CreatePair("WritingApp", WritingApp);
			yield return CreatePair("ProductionDate", ProductionDate);
		}
	}

}
