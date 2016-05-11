using System;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public abstract class MediaProvider : MetaDataProvider {
		//public IEnumerable<Attachment> Attachments { get; private set; }
		//public IEnumerable<VirtualTrack> VirtualTracks { get; private set; }
		//public IEnumerable<Edition> Editions { get; private set; }
		//public IEnumerable<VideoStream> VideoStreams { get; private set; }
		//public IEnumerable<SubtitleStream> SubtitleStreams { get; private set; }

		public MediaProvider(string name) : base(name) { }

		public static readonly MetaInfoItemType FileSizeType = new MetaInfoItemType("FileSize", "bytes", typeof(long), "");
		public static readonly MetaInfoItemType OverheadType = new MetaInfoItemType("Overhead", "bytes", typeof(long), "Bytes which are not content");

		public static readonly MetaInfoItemType IdType = new MetaInfoItemType("Id", null, typeof(byte[]), "");
		public static readonly MetaInfoItemType NextIdType = new MetaInfoItemType("NextId", null, typeof(byte[]), "");
		public static readonly MetaInfoItemType PreviousIdType = new MetaInfoItemType("PreviousId", null, typeof(byte[]), "");
		public static readonly MetaInfoItemType RelationIdType = new MetaInfoItemType("RelationId", null, typeof(byte[]), "");

		public static readonly MetaInfoItemType TimecodeScaleType = new MetaInfoItemType("TimecodeScale", null, typeof(long), "");
		public static readonly MetaInfoItemType DurationType = new MetaInfoItemType("Duration", "s", typeof(double), "Specified duration in the container. Streams may have different durations.");
		public static readonly MetaInfoItemType CreationDateType = new MetaInfoItemType("CreationDate", null, typeof(DateTime), "");
		public static readonly MetaInfoItemType TitleType = new MetaInfoItemType("Title", null, typeof(string), "");
		public static readonly MetaInfoItemType MuxingAppType = new MetaInfoItemType("MuxingApp", null, typeof(string), "");
		public static readonly MetaInfoItemType WritingAppType = new MetaInfoItemType("WritingApp", null, typeof(string), "");
		public static readonly MetaInfoItemType FullPathType = new MetaInfoItemType("FullPath", null, typeof(string), "");
		public static readonly MetaInfoItemType DirectoryNameType = new MetaInfoItemType("DirectoryName", null, typeof(string), "");
		public static readonly MetaInfoItemType FileNameType = new MetaInfoItemType("FileName", null, typeof(string), "");
		public static readonly MetaInfoItemType FileExtensionType = new MetaInfoItemType("FileExtension", null, typeof(string), "");
		public static readonly MetaInfoItemType SuggestedFileExtensionType = new MetaInfoItemType("SuggestedFileExtension", null, typeof(string), "");



		public static readonly MetaInfoItemType MediaStreamType = new MetaInfoItemType("MediaStream", null, typeof(MediaStream), "");
		public static readonly MetaInfoItemType AudioStreamType = new MetaInfoItemType("AudioStream", null, typeof(AudioStream), "");
		public static readonly MetaInfoItemType VideoStreamType = new MetaInfoItemType("VideoStream", null, typeof(VideoStream), "");
		public static readonly MetaInfoItemType SubtitleStreamType = new MetaInfoItemType("SubtitleStream", null, typeof(SubtitleStream), "");
		public static readonly MetaInfoItemType AttachmentType = new MetaInfoItemType("Attachment", null, typeof(Attachment), "");
		public static readonly MetaInfoItemType JoinTrackBlocksType = new MetaInfoItemType("JoinTrackBlocks", null, typeof(JoinTrackBlocks), "");
		public static readonly MetaInfoItemType CombineTrackPlanesType = new MetaInfoItemType("CombineTrackPlanes", null, typeof(CombineTrackPlanes), "");
		public static readonly MetaInfoItemType TagsType = new MetaInfoItemType("Tags", null, typeof(Tags), "");
		public static readonly MetaInfoItemType ChaptersType = new MetaInfoItemType("Chapters", null, typeof(Chapters), "");
	}

	public class CombineTrackPlanes : MetaInfoContainer {
		public static readonly MetaInfoItemType CombineTrackPlaneType = new MetaInfoItemType("CombineTrackPlane", null, typeof(CombineTrackPlane), "");
        public CombineTrackPlanes() : base(MediaProvider.CombineTrackPlanesType) {

        }
	}
	public class CombineTrackPlane : MetaInfoContainer {
		public static readonly MetaInfoItemType TrackIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
		public static readonly MetaInfoItemType TrackPlaneTypeType = new MetaInfoItemType("TrackPlaneTypes", null, typeof(TrackPlaneTypes), "");
        public CombineTrackPlane() : base(CombineTrackPlanes.CombineTrackPlaneType) {

        }
	}
	public enum TrackPlaneTypes { Left, Right, Background }

	public class JoinTrackBlocks : MetaInfoContainer {
		public static readonly MetaInfoItemType TrackIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
        public JoinTrackBlocks() : base(MediaProvider.JoinTrackBlocksType) {

        }
	}

}
