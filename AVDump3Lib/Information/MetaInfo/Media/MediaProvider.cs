using System;

namespace AVDump3Lib.Information.MetaInfo.Media {
	public abstract class MediaProvider : MetaDataProvider {
		//public IEnumerable<Attachment> Attachments { get; private set; }
		//public IEnumerable<VirtualTrack> VirtualTracks { get; private set; }
		//public IEnumerable<Edition> Editions { get; private set; }
		//public IEnumerable<VideoStream> VideoStreams { get; private set; }
		//public IEnumerable<SubtitleStream> SubtitleStreams { get; private set; }

		public MediaProvider(string name) : base(name) { }

		public static readonly MetaInfoItemType<long> FileSizeType = new MetaInfoItemType<long>("FileSize", "bytes");
		public static readonly MetaInfoItemType<long> OverheadType = new MetaInfoItemType<long>("Overhead", "bytes");

		public static readonly MetaInfoItemType<byte[]> IdType = new MetaInfoItemType<byte[]>("Id", null);
		public static readonly MetaInfoItemType<byte[]> NextIdType = new MetaInfoItemType<byte[]>("NextId", null);
		public static readonly MetaInfoItemType<byte[]> PreviousIdType = new MetaInfoItemType<byte[]>("PreviousId", null);
		public static readonly MetaInfoItemType<byte[]> RelationIdType = new MetaInfoItemType<byte[]>("RelationId", null);

		public static readonly MetaInfoItemType<long> TimecodeScaleType = new MetaInfoItemType<long>("TimecodeScale", null);
		public static readonly MetaInfoItemType<double> DurationType = new MetaInfoItemType<double>("Duration", "s");
		public static readonly MetaInfoItemType<DateTime> CreationDateType = new MetaInfoItemType<DateTime>("CreationDate", null);
		public static readonly MetaInfoItemType<string> TitleType = new MetaInfoItemType<string>("Title", null);
		public static readonly MetaInfoItemType<string> MuxingAppType = new MetaInfoItemType<string>("MuxingApp", null);
		public static readonly MetaInfoItemType<string> WritingAppType = new MetaInfoItemType<string>("WritingApp", null);
		public static readonly MetaInfoItemType<string> FullPathType = new MetaInfoItemType<string>("FullPath", null);
		public static readonly MetaInfoItemType<string> DirectoryNameType = new MetaInfoItemType<string>("DirectoryName", null);
		public static readonly MetaInfoItemType<string> FileNameType = new MetaInfoItemType<string>("FileName", null);
		public static readonly MetaInfoItemType<string> FileExtensionType = new MetaInfoItemType<string>("FileExtension", null);
		public static readonly MetaInfoItemType<string> SuggestedFileExtensionType = new MetaInfoItemType<string>("SuggestedFileExtension", null);


		public static readonly MetaInfoItemType<MediaStream> MediaStreamType = new MetaInfoItemType<MediaStream>("MediaStream", null);
		public static readonly MetaInfoItemType<AudioStream> AudioStreamType = new MetaInfoItemType<AudioStream>("AudioStream", null);
		public static readonly MetaInfoItemType<VideoStream> VideoStreamType = new MetaInfoItemType<VideoStream>("VideoStream", null);
		public static readonly MetaInfoItemType<SubtitleStream> SubtitleStreamType = new MetaInfoItemType<SubtitleStream>("SubtitleStream", null);
		public static readonly MetaInfoItemType<Attachment> AttachmentType = new MetaInfoItemType<Attachment>("Attachment", null);
		public static readonly MetaInfoItemType<JoinTrackBlocks> JoinTrackBlocksType = new MetaInfoItemType<JoinTrackBlocks>("JoinTrackBlocks", null);
		public static readonly MetaInfoItemType<CombineTrackPlanes> CombineTrackPlanesType = new MetaInfoItemType<CombineTrackPlanes>("CombineTrackPlanes", null);
		public static readonly MetaInfoItemType<Tags> TagsType = new MetaInfoItemType<Tags>("Tags", null);
		public static readonly MetaInfoItemType<Chapters> ChaptersType = new MetaInfoItemType<Chapters>("Chapters", null);
	}

	public class CombineTrackPlanes : MetaInfoContainer {
		public static readonly MetaInfoItemType<CombineTrackPlane> CombineTrackPlaneType = new MetaInfoItemType<CombineTrackPlane>("CombineTrackPlane", null);
		public CombineTrackPlanes() { }
	}
	public class CombineTrackPlane : MetaInfoContainer {
		public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId", null);
		public static readonly MetaInfoItemType<TrackPlaneTypes> TrackPlaneTypeType = new MetaInfoItemType<TrackPlaneTypes>("TrackPlaneTypes", null);
		public CombineTrackPlane() { }
	}
	public enum TrackPlaneTypes { Left, Right, Background }

	public class JoinTrackBlocks : MetaInfoContainer {
		public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId", null);
		public JoinTrackBlocks() { }
	}

}
