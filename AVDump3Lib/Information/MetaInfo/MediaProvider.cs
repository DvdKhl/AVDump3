using AVDump3Lib.Information.MetaInfo.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public abstract class MediaProvider : MetaDataProvider {
        //public IEnumerable<Attachment> Attachments { get; private set; }
        //public IEnumerable<VirtualTrack> VirtualTracks { get; private set; }
        //public IEnumerable<Edition> Editions { get; private set; }
        //public IEnumerable<VideoStream> VideoStreams { get; private set; }
        //public IEnumerable<SubtitleStream> SubtitleStreams { get; private set; }

        public MediaProvider(string name) : base(name, MediaProviderType) { }
		public static readonly MetaInfoContainerType MediaProviderType = new MetaInfoContainerType("MediaProvider");

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
        public static readonly MetaInfoItemType<List<TargetedTag>> TagsType = new MetaInfoItemType<List<TargetedTag>>("Tags", null);


        public static readonly MetaInfoContainerType MediaStreamType = new MetaInfoContainerType("MediaStream");
        public static readonly MetaInfoContainerType AudioStreamType = new MetaInfoContainerType("AudioStream");
        public static readonly MetaInfoContainerType VideoStreamType = new MetaInfoContainerType("VideoStream");
        public static readonly MetaInfoContainerType SubtitleStreamType = new MetaInfoContainerType("SubtitleStream");
        public static readonly MetaInfoContainerType AttachmentType = new MetaInfoContainerType("Attachment");
        public static readonly MetaInfoContainerType JoinTrackBlocksType = new MetaInfoContainerType("JoinTrackBlocks");
        public static readonly MetaInfoContainerType CombineTrackPlanesType = new MetaInfoContainerType("CombineTrackPlanes");
        public static readonly MetaInfoContainerType ChaptersType = new MetaInfoContainerType("Chapters");
    }

    #region MediaStream
    public class MediaStream {
        public static readonly MetaInfoItemType<ulong> IdType = new MetaInfoItemType<ulong>("Id", null);
        public static readonly MetaInfoItemType<int> IndexType = new MetaInfoItemType<int>("Index", null);
        public static readonly MetaInfoItemType<bool> IsEnabledType = new MetaInfoItemType<bool>("IsEnabled", null);
        public static readonly MetaInfoItemType<bool> IsDefaultType = new MetaInfoItemType<bool>("IsDefault", null);
        public static readonly MetaInfoItemType<bool> IsForcedType = new MetaInfoItemType<bool>("IsForced", null);
        public static readonly MetaInfoItemType<bool> IsOverlayType = new MetaInfoItemType<bool>("IsOverlay", null);
        public static readonly MetaInfoItemType<string> TitleType = new MetaInfoItemType<string>("Title", null);
        public static readonly MetaInfoItemType<string> LanguageType = new MetaInfoItemType<string>("Language", null);
        public static readonly MetaInfoItemType<string> CodecIdType = new MetaInfoItemType<string>("CodecId", null);
        public static readonly MetaInfoItemType<string> CodecNameType = new MetaInfoItemType<string>("CodecName", null);
        public static readonly MetaInfoItemType<int> ColorDepth = new MetaInfoItemType<int>("ColorDepth", null);

        public static readonly MetaInfoItemType<int> CueCountType = new MetaInfoItemType<int>("CueCount", null);

        public static readonly MetaInfoItemType<long> SizeType = new MetaInfoItemType<long>("Size", "bytes");
        public static readonly MetaInfoItemType<TimeSpan> DurationType = new MetaInfoItemType<TimeSpan>("Duration", "s");
        public static readonly MetaInfoItemType<double> BitrateType = new MetaInfoItemType<double>("Bitrate", "bit/s");
        public static readonly MetaInfoItemType<string> StatedBitrateModeType = new MetaInfoItemType<string>("StatedBitrateMode", null);

        public static readonly MetaInfoItemType<string> EncoderNameType = new MetaInfoItemType<string>("EncoderName", null);
        public static readonly MetaInfoItemType<string> EncoderSettingsType = new MetaInfoItemType<string>("EncoderSettings", null);


        public static readonly MetaInfoItemType<long> SampleCountType = new MetaInfoItemType<long>("SampleCount", null);
        public static readonly MetaInfoItemType<double> StatedSampleRateType = new MetaInfoItemType<double>("StatedSampleRate", null);
        public static readonly MetaInfoItemType<double> MaxSampleRateType = new MetaInfoItemType<double>("MaxSampleRate", null);
        public static readonly MetaInfoItemType<double> MinSampleRateType = new MetaInfoItemType<double>("MinSampleRate", null);
        public static readonly MetaInfoItemType<double> AverageSampleRateType = new MetaInfoItemType<double>("AverageSampleRate", null);
        public static readonly MetaInfoItemType<double> DominantSampleRateType = new MetaInfoItemType<double>("DominantSampleRate", null);
        public static readonly MetaInfoItemType<List<SampleRateCountPair>> SampleRateHistogramType = new MetaInfoItemType<List<SampleRateCountPair>>("SampleRateHistogram", null);
        public static readonly MetaInfoItemType<double> SampleRateVarianceType = new MetaInfoItemType<double>("SampleRateVariance", null);

        public static readonly MetaInfoItemType<double> OutputSampleRateType = new MetaInfoItemType<double>("OutputSampleRate", null);
    }

    public class SampleRateCountPair {
        public double Rate { get; private set; }
        public long Count { get; private set; }

        public SampleRateCountPair(double rate, long count) { Rate = rate; Count = count; }
        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Rate, Count); }
    }
    public class AudioStream {
        public static readonly MetaInfoItemType<int> ChannelCountType = new MetaInfoItemType<int>("ChannelCount", null);
        public static readonly MetaInfoItemType<int> BitDepthType = new MetaInfoItemType<int>("BitDepth", null);
    }

    #region VideoStream
    public class VideoStream {
        public static readonly MetaInfoItemType<bool> IsInterlacedType = new MetaInfoItemType<bool>("IsInterlaced", null);
        public static readonly MetaInfoItemType<bool> HasAlphaType = new MetaInfoItemType<bool>("HasAlpha", null);
        public static readonly MetaInfoItemType<StereoModes> StereoModeType = new MetaInfoItemType<StereoModes>("StereoMode", null);

        public static readonly MetaInfoItemType<Dimensions> PixelDimensionsType = new MetaInfoItemType<Dimensions>("PixelDimensions", null);
        public static readonly MetaInfoItemType<Dimensions> DisplayDimensionsType = new MetaInfoItemType<Dimensions>("DisplayDimensions", null);
        public static readonly MetaInfoItemType<DisplayUnits> DisplayUnitType = new MetaInfoItemType<DisplayUnits>("DisplayUnit", null);
        public static readonly MetaInfoItemType<AspectRatioBehaviors> AspectRatioBehaviorType = new MetaInfoItemType<AspectRatioBehaviors>("AspectRatioBehavior", null);

        public static readonly MetaInfoItemType<double> DisplayAspectRatioType = new MetaInfoItemType<double>("DisplayAspectRatio", null);
        public static readonly MetaInfoItemType<double> PixelAspectRatioType = new MetaInfoItemType<double>("PixelAspectRatio", null);
        public static readonly MetaInfoItemType<double> StorageAspectRatioType = new MetaInfoItemType<double>("StorageAspectRatio", null);

        public static readonly MetaInfoItemType<CropSides> PixelCropType = new MetaInfoItemType<CropSides>("PixelCrop", null);
        public static readonly MetaInfoItemType<int> ColorSpaceType = new MetaInfoItemType<int>("ColorSpace", null);


    }

    public enum StereoModes { Mono, LeftRight, TopBottom, Checkboard, RowInterleaved, ColumnInterleaved, FrameAlternating, Reversed = 1 << 30, Other = 1 << 31, AnaGlyph, CyanRed, GreenMagenta }
    public enum DisplayUnits { Invalid, Pixel, Meter, AspectRatio, Unknown }
    public enum AspectRatioBehaviors { Invalid, FreeResizing, KeepAR, Fixed, Unknown }

    public class Dimensions {
        public Dimensions(int width, int height) {
            Width = width;
            Height = height;
        }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public override string ToString() { return string.Format("{0}, {1}", Width, Height); }
    }
    public class CropSides {
        public CropSides(int top, int right, int bottom, int left) {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }
        public int Top { get; private set; }
        public int Left { get; private set; }
        public int Right { get; private set; }
        public int Bottom { get; private set; }

        public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", Top, Right, Bottom, Left); }
    }
    #endregion

    #endregion

    #region Attachment
    public class Attachment {
        public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
        public static readonly MetaInfoItemType<int> SizeType = new MetaInfoItemType<int>("Size", "bytes");
        public static readonly MetaInfoItemType<string> DescriptionType = new MetaInfoItemType<string>("Description", "text");
        public static readonly MetaInfoItemType<string> NameType = new MetaInfoItemType<string>("Name", "text");
        public static readonly MetaInfoItemType<string> TypeType = new MetaInfoItemType<string>("Type", "text");

    }
    #endregion

    #region Planes
    public class CombineTrackPlanes {
        public static readonly MetaInfoItemType<CombineTrackPlane> CombineTrackPlaneType = new MetaInfoItemType<CombineTrackPlane>("CombineTrackPlane", null);

    }
    public class CombineTrackPlane {
        public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId", null);
        public static readonly MetaInfoItemType<TrackPlaneTypes> TrackPlaneTypeType = new MetaInfoItemType<TrackPlaneTypes>("TrackPlaneTypes", null);
    }
    public enum TrackPlaneTypes { Left, Right, Background }
    public class JoinTrackBlocks {
        public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId", null);
    }
    #endregion

    #region Chapters
    public class Chapters {
        public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
        public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden", null);
        public static readonly MetaInfoItemType<bool> IsDefaultType = new MetaInfoItemType<bool>("IsDefault", null);
        public static readonly MetaInfoItemType<bool> IsOrderedType = new MetaInfoItemType<bool>("IsOrdered", null);
        public static readonly MetaInfoContainerType ChapterType = new MetaInfoContainerType("Chapter");
    }

    public class Chapter {

        public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
        public static readonly MetaInfoItemType<string> IdStringType = new MetaInfoItemType<string>("IdString", null);

        public static readonly MetaInfoItemType<double> TimeStartType = new MetaInfoItemType<double>("TimeStart", "byte");
        public static readonly MetaInfoItemType<double> TimeEndType = new MetaInfoItemType<double>("TimeStart", "byte");

        public static readonly MetaInfoItemType<byte[]> SegmentIdType = new MetaInfoItemType<byte[]>("SegmentId", null);
        public static readonly MetaInfoItemType<int> SegmentChaptersIdType = new MetaInfoItemType<int>("SegmentChaptersId", null);

        public static readonly MetaInfoItemType<int> PhysicalEquivalentType = new MetaInfoItemType<int>("PhysicalEquivalent", null);

        public static readonly MetaInfoItemType<int> AssociatedTrackType = new MetaInfoItemType<int>("AssociatedTrack", null);

        public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden", null);
        public static readonly MetaInfoItemType<bool> IsEnabledType = new MetaInfoItemType<bool>("IsEnabled", null);

        public static readonly MetaInfoItemType<ChapterTitle> TitleType = new MetaInfoItemType<ChapterTitle>("Title", null);

        public static readonly MetaInfoItemType<bool> HasOperationsType = new MetaInfoItemType<bool>("HasOperations", null);
    }

    public class ChapterTitle {
        public ChapterTitle(string title, IEnumerable<string> languages, IEnumerable<string> countries) {
            Title = title;
            Languages = languages.ToList().AsReadOnly();
            Countries = countries.ToList().AsReadOnly();
        }
        public string Title { get; private set; }
        public ReadOnlyCollection<string> Languages { get; private set; }
        public ReadOnlyCollection<string> Countries { get; private set; }
    }
    #endregion

    #region Tags
    public class TargetedTag {
        public string TargetTitle { get; private set; }
        public TargetedTagType TargetType { get; private set; }

        public ReadOnlyCollection<Tag> Tags { get; private set; }
        public ReadOnlyCollection<Target> Targets { get; private set; }

        public TargetedTag(IEnumerable<Target> targets, IEnumerable<Tag> tags) {
            Targets = targets.ToList().AsReadOnly();
            Tags = tags.ToList().AsReadOnly();
        }
    }

    public class Tag {
        public ReadOnlyCollection<Tag> Children { get; private set; }

        public string Name { get; private set; }
        public object Value { get; private set; }
        public string Language { get; private set; }
        public bool IsDefault { get; private set; }

        public Tag(string name, object value, string language, bool isDefault, IEnumerable<Tag> children) {
            Name = name;
            Value = value;
            Language = language;
            IsDefault = isDefault;
            Children = children.ToList().AsReadOnly();
        }

    }

    public class Target {
        public TagTarget Type { get; private set; }
        public long Id { get; private set; }

        public Target(TagTarget type, long id) {
            Type = type;
            Id = id;
        }
    }

    public enum TagTarget { Track, Chapters, Chapter, Attachment }

    //public class TargetedTags : MetaInfoContainer {
    //	public static readonly MetaInfoItemType TypeType = new MetaInfoItemType("Type", null, typeof(TargetedTagType), "");
    //	public static readonly MetaInfoItemType TitleType = new MetaInfoItemType("Type", null, typeof(string), "");
    //	public static readonly MetaInfoItemType TrackIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType ChaptersIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType ChapterIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType AttachmentIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //
    //	public static readonly MetaInfoItemType TagType = new MetaInfoItemType("Tag", null, typeof(Tag), "");
    //}
    public enum TargetedTagType { Instant = 10, Scene = 20, ChapterOrTrack = 30, Session = 40, EpisodeOrAlbum = 50, SeasonOrVolume = 60, Collection = 70 }
    #endregion
}
