using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Attachments;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cluster;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SeekHead;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SegmentInfo;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;
using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment {
	public class SegmentSection : Section {
		#region Fields & Properties
		public SegmentInfoSection SegmentInfo { get; private set; }
		public AttachmentsSection Attachments { get; private set; }
		public ChaptersSection Chapters { get; private set; }
		public ClusterSection Cluster { get; private set; }
		public TracksSection Tracks { get; private set; }
		public EbmlList<TagsSection> Tags { get; private set; }
		public CuesSection Cues { get; private set; }
		public SeekHeadSection SeekHead { get; private set; }
		#endregion

		public SegmentSection() { Cluster = new ClusterSection(); Tags = new EbmlList<TagsSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.Cluster.Id) {
				Cluster.Read(reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Tracks.Id && Tracks == null) { //TODO Add warning
				Tracks = Section.CreateRead(new TracksSection(), reader, elemInfo);
				Cluster.AddTracks(Tracks.Items); //Must be set and != 0 (add warning)
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Info.Id) {
				SegmentInfo = Section.CreateRead(new SegmentInfoSection(), reader, elemInfo);
				Cluster.TimeCodeScale = SegmentInfo.TimecodeScale;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Chapters.Id) {
				Chapters = Section.CreateRead(new ChaptersSection(), reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Attachments.Id) {
				Attachments = Section.CreateRead(new AttachmentsSection(), reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Tags.Id) {
				Section.CreateReadAdd(new TagsSection(), reader, elemInfo, Tags);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SeekHead.Id) {
				SeekHead = Section.CreateRead(new SeekHeadSection(), reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Cues.Id) {
				Cues = Section.CreateRead(new CuesSection(), reader, elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("SegmentInfo", SegmentInfo);
			yield return CreatePair("Tracks", Tracks);
			yield return CreatePair("Chapters", Chapters);
			foreach(var tags in Tags) yield return CreatePair("Tags", tags);
			yield return CreatePair("Attachments", Attachments);
			//yield return CreatePair("Cluster", Cluster);
			//yield return CreatePair("SeekHead", SeekHead);
			//yield return CreatePair("Cues", Cues);
		}
	}
}
