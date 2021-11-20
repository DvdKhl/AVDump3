using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Attachments;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cluster;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cues;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SeekHead;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.SegmentInfo;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;
using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment;

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

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.Cluster) {
			Cluster.Read(reader);
		} else if(reader.DocElement == MatroskaDocType.Tracks && Tracks == null) { //TODO Add warning
			Tracks = CreateRead(new TracksSection(), reader);
			Cluster.AddTracks(Tracks.Items); //Must be set and != 0 (add warning)
		} else if(reader.DocElement == MatroskaDocType.Info) {
			SegmentInfo = CreateRead(new SegmentInfoSection(), reader);
			Cluster.TimeCodeScale = SegmentInfo.TimecodeScale;
		} else if(reader.DocElement == MatroskaDocType.Chapters) {
			Chapters = CreateRead(new ChaptersSection(), reader);
		} else if(reader.DocElement == MatroskaDocType.Attachments) {
			Attachments = CreateRead(new AttachmentsSection(), reader);
		} else if(reader.DocElement == MatroskaDocType.Tags) {
			CreateReadAdd(new TagsSection(), reader, Tags);
		} else if(reader.DocElement == MatroskaDocType.SeekHead) {
			SeekHead = CreateRead(new SeekHeadSection(), reader);
		} else if(reader.DocElement == MatroskaDocType.Cues) {
			Cues = CreateRead(new CuesSection(), reader);
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
