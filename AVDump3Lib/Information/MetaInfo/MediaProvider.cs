using AVDump3Lib.Information.MetaInfo.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo {
	public abstract class MediaProvider : MetaDataProvider {
		//public IEnumerable<Attachment> Attachments { get; private set; }
		//public IEnumerable<VirtualTrack> VirtualTracks { get; private set; }
		//public IEnumerable<Edition> Editions { get; private set; }
		//public IEnumerable<VideoStream> VideoStreams { get; private set; }
		//public IEnumerable<SubtitleStream> SubtitleStreams { get; private set; }

		public MediaProvider(string name) : base(name, MediaProviderType) { }
		public static readonly MetaInfoContainerType MediaProviderType = new("MediaProvider");

		public static readonly MetaInfoItemType<string> FileTypeIdentifierType = new("FileTypeIdentifier"); //TODO

		public static readonly MetaInfoItemType<long> FileSizeType = new("FileSize", "bytes");
		public static readonly MetaInfoItemType<long> OverheadType = new("Overhead", "bytes");
		public static readonly MetaInfoItemType<string> ContainerVersionType = new("ContainerVersion");

		public static readonly MetaInfoItemType<ImmutableArray<byte>> IdType = new("Id");
		public static readonly MetaInfoItemType<ImmutableArray<byte>> RelationIdType = new("RelationId");
		public static readonly MetaInfoItemType<ImmutableArray<byte>> NextIdType = new("NextId");
		public static readonly MetaInfoItemType<ImmutableArray<byte>> PreviousIdType = new("PreviousId");
		public static readonly MetaInfoItemType<string> NextFileNameType = new("NextFileName");
		public static readonly MetaInfoItemType<string> PreviousFileNameType = new("PreviousName");

		public static readonly MetaInfoItemType<long> TimecodeScaleType = new("TimecodeScale");
		public static readonly MetaInfoItemType<double> DurationType = new("Duration", "s");
		public static readonly MetaInfoItemType<DateTime> CreationDateType = new("CreationDate");
		public static readonly MetaInfoItemType<DateTime> ModificationDateType = new("ModificationDate");
		public static readonly MetaInfoItemType<string> TitleType = new("Title");
		public static readonly MetaInfoItemType<string> MuxingAppType = new("MuxingApp");
		public static readonly MetaInfoItemType<string> WritingAppType = new("WritingApp");
		public static readonly MetaInfoItemType<string> DirectoryNameType = new("DirectoryName");
		public static readonly MetaInfoItemType<string> FileNameType = new("FileName");
		public static readonly MetaInfoItemType<string> FileExtensionType = new("FileExtension");
		public static readonly MetaInfoItemType<ImmutableArray<string>> SuggestedFileExtensionType = new("SuggestedFileExtension");
		public static readonly MetaInfoItemType<List<TargetedTag>> TagsType = new("Tags");
		public static readonly MetaInfoItemType<long> AttachmentsSizeType = new("AttachmentSize");


		public static readonly MetaInfoContainerType MediaStreamType = new("MediaStream");
		public static readonly MetaInfoContainerType AudioStreamType = new("AudioStream");
		public static readonly MetaInfoContainerType VideoStreamType = new("VideoStream");
		public static readonly MetaInfoContainerType SubtitleStreamType = new("SubtitleStream");
		public static readonly MetaInfoContainerType AttachmentType = new("Attachment");
		public static readonly MetaInfoContainerType JoinTrackBlocksType = new("JoinTrackBlocks");
		public static readonly MetaInfoContainerType CombineTrackPlanesType = new("CombineTrackPlanes");
		public static readonly MetaInfoContainerType ChaptersType = new("Chapters");
	}

	#region MediaStream
	public static class MediaStream {
		public static readonly MetaInfoItemType<ulong> IdType = new("Id");
		public static readonly MetaInfoItemType<int> IndexType = new("Index");
		public static readonly MetaInfoItemType<bool> IsEnabledType = new("IsEnabled");
		public static readonly MetaInfoItemType<bool> IsDefaultType = new("IsDefault");
		public static readonly MetaInfoItemType<bool> IsForcedType = new("IsForced");
		public static readonly MetaInfoItemType<bool> IsOverlayType = new("IsOverlay");
		public static readonly MetaInfoItemType<string> TitleType = new("Title");
		public static readonly MetaInfoItemType<string> LanguageType = new("Language");
		public static readonly MetaInfoItemType<string> CodecIdType = new("CodecId");
		public static readonly MetaInfoItemType<string> CodecProfileType = new("CodecProfile");
		public static readonly MetaInfoItemType<string> CodecVersionType = new("CodecVersion");
		public static readonly MetaInfoItemType<string> CodecAdditionalFeaturesType = new("CodecAdditionalFeatures");
		public static readonly MetaInfoItemType<string> CodecCommercialIdType = new("CodecCommercialId");
		public static readonly MetaInfoItemType<string> ContainerCodecIdType = new("ContainerCodecId");
		public static readonly MetaInfoItemType<string> ContainerCodecCCType = new("ContainerCodecCC");
		public static readonly MetaInfoItemType<string> ContainerCodecIdWithCodecPrivateType = new("ContainerCodecIdWithCodecPrivate");
		public static readonly MetaInfoItemType<string> ContainerCodecNameType = new("ContainerCodecName");
		public static readonly MetaInfoItemType<string> CodecNameType = new("CodecName");
		public static readonly MetaInfoItemType<DateTime> CreationDateType = new("CreationDate");
		public static readonly MetaInfoItemType<DateTime> ModificationDateType = new("ModificationDate");

		public static readonly MetaInfoItemType<int> CueCountType = new("CueCount");

		public static readonly MetaInfoItemType<long> SizeType = new("Size", "bytes");
		public static readonly MetaInfoItemType<int> CodecPrivateSizeType = new("CodecPrivateSize", "bytes");
		public static readonly MetaInfoItemType<TimeSpan> DurationType = new("Duration", "s");
		public static readonly MetaInfoItemType<double> BitrateType = new("Bitrate", "bit/s");
		public static readonly MetaInfoItemType<string> StatedBitrateModeType = new("StatedBitrateMode");

		public static readonly MetaInfoItemType<string> EncoderNameType = new("EncoderName");
		public static readonly MetaInfoItemType<string> EncoderSettingsType = new("EncoderSettings");


		public static readonly MetaInfoItemType<long> SampleCountType = new("SampleCount");
		public static readonly MetaInfoItemType<double> StatedSampleRateType = new("StatedSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> MaxSampleRateType = new("MaxSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> MinSampleRateType = new("MinSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> AverageSampleRateType = new("AverageSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> DominantSampleRateType = new("DominantSampleRate", "s^-1");
		public static readonly MetaInfoItemType<List<SampleRateCountPair>> SampleRateHistogramType = new("SampleRateHistogram", "s^-1");
		public static readonly MetaInfoItemType<double> SampleRateVarianceType = new("SampleRateVariance", "s^-1");

		public static readonly MetaInfoItemType<double> OutputSampleRateType = new("OutputSampleRate", "s^-1");
	}

	public class SampleRateCountPair {
		public double Rate { get; private set; }
		public long Count { get; private set; }

		public SampleRateCountPair(double rate, long count) { Rate = rate; Count = count; }
		public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Rate, Count); }
	}
	public static class AudioStream {
		public static readonly MetaInfoItemType<int> ChannelCountType = new("ChannelCount");
		public static readonly MetaInfoItemType<int> BitDepthType = new("BitDepth");
	}

	#region VideoStream
	public static class VideoStream {
		public static readonly MetaInfoItemType<bool> IsInterlacedType = new("IsInterlaced");
		public static readonly MetaInfoItemType<bool> HasVariableFrameRateType = new("HasVariableFrameRate");
		public static readonly MetaInfoItemType<bool> HasAlphaType = new("HasAlpha");
		public static readonly MetaInfoItemType<StereoModes> StereoModeType = new("StereoMode");

		public static readonly MetaInfoItemType<Dimensions> PixelDimensionsType = new("PixelDimensions");
		public static readonly MetaInfoItemType<Dimensions> DisplayDimensionsType = new("DisplayDimensions");
		public static readonly MetaInfoItemType<DisplayUnit> DisplayUnitType = new("DisplayUnit");
		public static readonly MetaInfoItemType<AspectRatioBehavior> AspectRatioBehaviorType = new("AspectRatioBehavior");

		public static readonly MetaInfoItemType<double> DisplayAspectRatioType = new("DisplayAspectRatio");
		public static readonly MetaInfoItemType<double> PixelAspectRatioType = new("PixelAspectRatio");
		public static readonly MetaInfoItemType<double> StorageAspectRatioType = new("StorageAspectRatio");

		public static readonly MetaInfoItemType<CropSides> PixelCropType = new("PixelCrop");
		public static readonly MetaInfoItemType<ChromeSubsampling> ChromaSubsamplingType = new("ChromaSubsampling");
		public static readonly MetaInfoItemType<int> ColorSpaceType = new("ColorSpace");
		public static readonly MetaInfoItemType<int> ColorBitDepthType = new("ColorBitDepth");
	}

	[Flags]
	public enum StereoModes {
		Invalid = 0,
		Mono = 1 << 0,
		LeftRight = 1 << 1,
		TopBottom = 1 << 2,
		Checkboard = 1 << 3,
		RowInterleaved = 1 << 4,
		ColumnInterleaved = 1 << 5,
		FrameAlternating = 1 << 6,
		AnaGlyph = 1 << 7,
		CyanRed = 1 << 8,
		GreenMagenta = 1 << 9,
		Reversed = 1 << 30,
		Other = 1 << 31
	}
	public enum DisplayUnit { Invalid, Pixel, Meter, AspectRatio, Unknown }
	public enum AspectRatioBehavior { Invalid, FreeResizing, KeepAR, Fixed, Unknown }

	public class ChromeSubsampling {
		public int Y { get; }
		public int Cb { get; }
		public int Cr { get; }
		public ChromeSubsampling(int y, int cb, int cr) {
			Y = y;
			Cb = cb;
			Cr = cr;
		}
		public ChromeSubsampling(string data) {
			var parts = data.Split(':');
			Y = int.Parse(parts[0]);
			Cb = int.Parse(parts[1]);
			Cr = int.Parse(parts[2]);
		}

		public override string ToString() => $"{Y}:{Cb}:{Cr}";
	}
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
	public static class Attachment {
		public static readonly MetaInfoItemType<long> IdType = new("Id");
		public static readonly MetaInfoItemType<long> SizeType = new("Size", "bytes");
		public static readonly MetaInfoItemType<string> DescriptionType = new("Description");
		public static readonly MetaInfoItemType<string> NameType = new("Name");
		public static readonly MetaInfoItemType<string> TypeType = new("Type");
	}
	#endregion

	#region Planes
	public static class CombineTrackPlanes {
		public static readonly MetaInfoItemType<CombineTrackPlane> CombineTrackPlaneType = new("CombineTrackPlane");

	}
	public class CombineTrackPlane {
		public static readonly MetaInfoItemType<int> TrackIdType = new("TrackId");
		public static readonly MetaInfoItemType<TrackPlaneType> TrackPlaneTypeType = new("TrackPlaneTypes");
	}
	public enum TrackPlaneType { Left, Right, Background }
	public static class JoinTrackBlocks {
		public static readonly MetaInfoItemType<int> TrackIdType = new("TrackId");
	}
	#endregion

	#region Chapters
	public static class Chapters {
		public static readonly MetaInfoItemType<int> IdType = new("Id");
		public static readonly MetaInfoItemType<string> FormatType = new("Format");
		public static readonly MetaInfoItemType<bool> IsHiddenType = new("IsHidden");
		public static readonly MetaInfoItemType<bool> IsDefaultType = new("IsDefault");
		public static readonly MetaInfoItemType<bool> IsOrderedType = new("IsOrdered");
		public static readonly MetaInfoContainerType ChapterType = new("Chapter");
	}

	public static class Chapter {

		public static readonly MetaInfoItemType<int> IdType = new("Id");
		public static readonly MetaInfoItemType<string> IdStringType = new("IdString");

		public static readonly MetaInfoItemType<double> TimeStartType = new("TimeStart", "byte");
		public static readonly MetaInfoItemType<double> TimeEndType = new("TimeStart", "byte");

		public static readonly MetaInfoItemType<ImmutableArray<byte>> SegmentIdType = new("SegmentId");
		public static readonly MetaInfoItemType<int> SegmentChaptersIdType = new("SegmentChaptersId");

		public static readonly MetaInfoItemType<int> PhysicalEquivalentType = new("PhysicalEquivalent");

		public static readonly MetaInfoItemType<int> AssociatedTrackType = new("AssociatedTrack");

		public static readonly MetaInfoItemType<bool> IsHiddenType = new("IsHidden");
		public static readonly MetaInfoItemType<bool> IsEnabledType = new("IsEnabled");

		public static readonly MetaInfoItemType<ImmutableArray<ChapterTitle>> TitlesType = new("Titles");

		public static readonly MetaInfoItemType<bool> HasOperationsType = new("HasOperations");
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

		public override string ToString() {
			return Title + " Languages(" + string.Join(", ", Languages) + ")" + " Countries(" + string.Join(", ", Countries) + ")";
		}
	}
	#endregion

	#region Tags
	public class TargetedTag {
		public string TargetTitle { get; private set; } = "";
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
