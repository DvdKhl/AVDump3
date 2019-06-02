using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SegmentInfo {
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

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.SegmentUID) {
				segmentUId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.SegmentFilename) {
				SegmentFilename = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.PrevUID) {
				prevUId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.PrevFilename) {
				PreviousFilename = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.NextUID) {
				nextUId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.NextFilename) {
				NextFilename = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.SegmentFamily) {
				segmentFamily.Add((byte[])reader.RetrieveValue());
			} else if(reader.DocElement == MatroskaDocType.ChapterTranslate) {
				Section.CreateReadAdd(new ChapterTranslateSection(), reader, ChapterTranslate);
			} else if(reader.DocElement == MatroskaDocType.TimecodeScale) {
				timecodeScale = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.Duration) {
				Duration = (double)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.Title) {
				Title = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.MuxingApp) {
				MuxingApp = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.WritingApp) {
				WritingApp = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.DateUTC) {
				ProductionDate = (DateTime)reader.RetrieveValue();
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
