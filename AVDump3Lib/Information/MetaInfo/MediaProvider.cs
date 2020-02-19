using AVDump3Lib.Information.MetaInfo.Core;
using System;
using System.Collections.Generic;
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
		public static readonly MetaInfoContainerType MediaProviderType = new MetaInfoContainerType("MediaProvider");

		public static readonly MetaInfoItemType<long> FileSizeType = new MetaInfoItemType<long>("FileSize", "bytes");
		public static readonly MetaInfoItemType<long> OverheadType = new MetaInfoItemType<long>("Overhead", "bytes");
		public static readonly MetaInfoItemType<string> ContainerVersionType = new MetaInfoItemType<string>("ContainerVersion");

		public static readonly MetaInfoItemType<ReadOnlyMemory<byte>> IdType = new MetaInfoItemType<ReadOnlyMemory<byte>>("Id");
		public static readonly MetaInfoItemType<ReadOnlyMemory<byte>> RelationIdType = new MetaInfoItemType<ReadOnlyMemory<byte>>("RelationId");
		public static readonly MetaInfoItemType<ReadOnlyMemory<byte>> NextIdType = new MetaInfoItemType<ReadOnlyMemory<byte>>("NextId");
		public static readonly MetaInfoItemType<ReadOnlyMemory<byte>> PreviousIdType = new MetaInfoItemType<ReadOnlyMemory<byte>>("PreviousId");
		public static readonly MetaInfoItemType<string> NextFileNameType = new MetaInfoItemType<string>("NextFileName");
		public static readonly MetaInfoItemType<string> PreviousFileNameType = new MetaInfoItemType<string>("PreviousName");

		public static readonly MetaInfoItemType<long> TimecodeScaleType = new MetaInfoItemType<long>("TimecodeScale");
		public static readonly MetaInfoItemType<double> DurationType = new MetaInfoItemType<double>("Duration", "s");
		public static readonly MetaInfoItemType<DateTime> CreationDateType = new MetaInfoItemType<DateTime>("CreationDate");
		public static readonly MetaInfoItemType<DateTime> ModificationDateType = new MetaInfoItemType<DateTime>("ModificationDate");
		public static readonly MetaInfoItemType<string> TitleType = new MetaInfoItemType<string>("Title");
		public static readonly MetaInfoItemType<string> MuxingAppType = new MetaInfoItemType<string>("MuxingApp");
		public static readonly MetaInfoItemType<string> WritingAppType = new MetaInfoItemType<string>("WritingApp");
		public static readonly MetaInfoItemType<string> DirectoryNameType = new MetaInfoItemType<string>("DirectoryName");
		public static readonly MetaInfoItemType<string> FileNameType = new MetaInfoItemType<string>("FileName");
		public static readonly MetaInfoItemType<string> FileExtensionType = new MetaInfoItemType<string>("FileExtension");
		public static readonly MetaInfoItemType<string> SuggestedFileExtensionType = new MetaInfoItemType<string>("SuggestedFileExtension");
		public static readonly MetaInfoItemType<List<TargetedTag>> TagsType = new MetaInfoItemType<List<TargetedTag>>("Tags");
		public static readonly MetaInfoItemType<long> AttachmentsSizeType = new MetaInfoItemType<long>("AttachmentSize");


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
		public static readonly MetaInfoItemType<ulong> IdType = new MetaInfoItemType<ulong>("Id");
		public static readonly MetaInfoItemType<int> IndexType = new MetaInfoItemType<int>("Index");
		public static readonly MetaInfoItemType<bool> IsEnabledType = new MetaInfoItemType<bool>("IsEnabled");
		public static readonly MetaInfoItemType<bool> IsDefaultType = new MetaInfoItemType<bool>("IsDefault");
		public static readonly MetaInfoItemType<bool> IsForcedType = new MetaInfoItemType<bool>("IsForced");
		public static readonly MetaInfoItemType<bool> IsOverlayType = new MetaInfoItemType<bool>("IsOverlay");
		public static readonly MetaInfoItemType<string> TitleType = new MetaInfoItemType<string>("Title");
		public static readonly MetaInfoItemType<string> LanguageType = new MetaInfoItemType<string>("Language");
		public static readonly MetaInfoItemType<string> CodecIdType = new MetaInfoItemType<string>("CodecId");
		public static readonly MetaInfoItemType<string> CodecProfileType = new MetaInfoItemType<string>("CodecProfile");
		public static readonly MetaInfoItemType<string> CodecVersionType = new MetaInfoItemType<string>("CodecVersion");
		public static readonly MetaInfoItemType<string> CodecAdditionalFeaturesType = new MetaInfoItemType<string>("CodecAdditionalFeatures");
		public static readonly MetaInfoItemType<string> CodecCommercialIdType = new MetaInfoItemType<string>("CodecCommercialId");
		public static readonly MetaInfoItemType<string> ContainerCodecIdType = new MetaInfoItemType<string>("ContainerCodecId");
		public static readonly MetaInfoItemType<string> ContainerCodecCCType = new MetaInfoItemType<string>("ContainerCodecCC");
		public static readonly MetaInfoItemType<string> ContainerCodecIdWithCodecPrivateType = new MetaInfoItemType<string>("ContainerCodecIdWithCodecPrivate");
		public static readonly MetaInfoItemType<string> ContainerCodecNameType = new MetaInfoItemType<string>("ContainerCodecName");
		public static readonly MetaInfoItemType<string> CodecNameType = new MetaInfoItemType<string>("CodecName");
		public static readonly MetaInfoItemType<DateTime> CreationDateType = new MetaInfoItemType<DateTime>("CreationDate");
		public static readonly MetaInfoItemType<DateTime> ModificationDateType = new MetaInfoItemType<DateTime>("ModificationDate");

		public static readonly MetaInfoItemType<int> CueCountType = new MetaInfoItemType<int>("CueCount");

		public static readonly MetaInfoItemType<long> SizeType = new MetaInfoItemType<long>("Size", "bytes");
		public static readonly MetaInfoItemType<int> CodecPrivateSizeType = new MetaInfoItemType<int>("CodecPrivateSize", "bytes");
		public static readonly MetaInfoItemType<TimeSpan> DurationType = new MetaInfoItemType<TimeSpan>("Duration", "s");
		public static readonly MetaInfoItemType<double> BitrateType = new MetaInfoItemType<double>("Bitrate", "bit/s");
		public static readonly MetaInfoItemType<string> StatedBitrateModeType = new MetaInfoItemType<string>("StatedBitrateMode");

		public static readonly MetaInfoItemType<string> EncoderNameType = new MetaInfoItemType<string>("EncoderName");
		public static readonly MetaInfoItemType<string> EncoderSettingsType = new MetaInfoItemType<string>("EncoderSettings");


		public static readonly MetaInfoItemType<long> SampleCountType = new MetaInfoItemType<long>("SampleCount");
		public static readonly MetaInfoItemType<double> StatedSampleRateType = new MetaInfoItemType<double>("StatedSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> MaxSampleRateType = new MetaInfoItemType<double>("MaxSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> MinSampleRateType = new MetaInfoItemType<double>("MinSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> AverageSampleRateType = new MetaInfoItemType<double>("AverageSampleRate", "s^-1");
		public static readonly MetaInfoItemType<double> DominantSampleRateType = new MetaInfoItemType<double>("DominantSampleRate", "s^-1");
		public static readonly MetaInfoItemType<List<SampleRateCountPair>> SampleRateHistogramType = new MetaInfoItemType<List<SampleRateCountPair>>("SampleRateHistogram", "s^-1");
		public static readonly MetaInfoItemType<double> SampleRateVarianceType = new MetaInfoItemType<double>("SampleRateVariance", "s^-1");

		public static readonly MetaInfoItemType<double> OutputSampleRateType = new MetaInfoItemType<double>("OutputSampleRate", "s^-1");
	}

	public class SampleRateCountPair {
		public double Rate { get; private set; }
		public long Count { get; private set; }

		public SampleRateCountPair(double rate, long count) { Rate = rate; Count = count; }
		public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Rate, Count); }
	}
	public class AudioStream {
		public static readonly MetaInfoItemType<int> ChannelCountType = new MetaInfoItemType<int>("ChannelCount");
		public static readonly MetaInfoItemType<int> BitDepthType = new MetaInfoItemType<int>("BitDepth");
	}

	#region VideoStream
	public class VideoStream {
		public static readonly MetaInfoItemType<bool> IsInterlacedType = new MetaInfoItemType<bool>("IsInterlaced");
		public static readonly MetaInfoItemType<bool> HasVariableFrameRateType = new MetaInfoItemType<bool>("HasVariableFrameRate");
		public static readonly MetaInfoItemType<bool> HasAlphaType = new MetaInfoItemType<bool>("HasAlpha");
		public static readonly MetaInfoItemType<StereoModes> StereoModeType = new MetaInfoItemType<StereoModes>("StereoMode");

		public static readonly MetaInfoItemType<Dimensions> PixelDimensionsType = new MetaInfoItemType<Dimensions>("PixelDimensions");
		public static readonly MetaInfoItemType<Dimensions> DisplayDimensionsType = new MetaInfoItemType<Dimensions>("DisplayDimensions");
		public static readonly MetaInfoItemType<DisplayUnits> DisplayUnitType = new MetaInfoItemType<DisplayUnits>("DisplayUnit");
		public static readonly MetaInfoItemType<AspectRatioBehaviors> AspectRatioBehaviorType = new MetaInfoItemType<AspectRatioBehaviors>("AspectRatioBehavior");

		public static readonly MetaInfoItemType<double> DisplayAspectRatioType = new MetaInfoItemType<double>("DisplayAspectRatio");
		public static readonly MetaInfoItemType<double> PixelAspectRatioType = new MetaInfoItemType<double>("PixelAspectRatio");
		public static readonly MetaInfoItemType<double> StorageAspectRatioType = new MetaInfoItemType<double>("StorageAspectRatio");

		public static readonly MetaInfoItemType<CropSides> PixelCropType = new MetaInfoItemType<CropSides>("PixelCrop");
		public static readonly MetaInfoItemType<ChromeSubsampling> ChromaSubsamplingType = new MetaInfoItemType<ChromeSubsampling>("ChromaSubsampling");
		public static readonly MetaInfoItemType<int> ColorSpaceType = new MetaInfoItemType<int>("ColorSpace");
		public static readonly MetaInfoItemType<int> ColorBitDepthType = new MetaInfoItemType<int>("ColorBitDepth");


	}

	public enum StereoModes { Mono, LeftRight, TopBottom, Checkboard, RowInterleaved, ColumnInterleaved, FrameAlternating, Reversed = 1 << 30, Other = 1 << 31, AnaGlyph, CyanRed, GreenMagenta }
	public enum DisplayUnits { Invalid, Pixel, Meter, AspectRatio, Unknown }
	public enum AspectRatioBehaviors { Invalid, FreeResizing, KeepAR, Fixed, Unknown }

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
	public class Attachment {
		public static readonly MetaInfoItemType<long> IdType = new MetaInfoItemType<long>("Id");
		public static readonly MetaInfoItemType<long> SizeType = new MetaInfoItemType<long>("Size", "bytes");
		public static readonly MetaInfoItemType<string> DescriptionType = new MetaInfoItemType<string>("Description");
		public static readonly MetaInfoItemType<string> NameType = new MetaInfoItemType<string>("Name");
		public static readonly MetaInfoItemType<string> TypeType = new MetaInfoItemType<string>("Type");
	}
	#endregion

	#region Planes
	public class CombineTrackPlanes {
		public static readonly MetaInfoItemType<CombineTrackPlane> CombineTrackPlaneType = new MetaInfoItemType<CombineTrackPlane>("CombineTrackPlane");

	}
	public class CombineTrackPlane {
		public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId");
		public static readonly MetaInfoItemType<TrackPlaneTypes> TrackPlaneTypeType = new MetaInfoItemType<TrackPlaneTypes>("TrackPlaneTypes");
	}
	public enum TrackPlaneTypes { Left, Right, Background }
	public class JoinTrackBlocks {
		public static readonly MetaInfoItemType<int> TrackIdType = new MetaInfoItemType<int>("TrackId");
	}
	#endregion

	#region Chapters
	public class Chapters {
		public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id");
		public static readonly MetaInfoItemType<string> FormatType = new MetaInfoItemType<string>("Format");
		public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden");
		public static readonly MetaInfoItemType<bool> IsDefaultType = new MetaInfoItemType<bool>("IsDefault");
		public static readonly MetaInfoItemType<bool> IsOrderedType = new MetaInfoItemType<bool>("IsOrdered");
		public static readonly MetaInfoContainerType ChapterType = new MetaInfoContainerType("Chapter");
	}

	public class Chapter {

		public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id");
		public static readonly MetaInfoItemType<string> IdStringType = new MetaInfoItemType<string>("IdString");

		public static readonly MetaInfoItemType<double> TimeStartType = new MetaInfoItemType<double>("TimeStart", "byte");
		public static readonly MetaInfoItemType<double> TimeEndType = new MetaInfoItemType<double>("TimeStart", "byte");

		public static readonly MetaInfoItemType<ReadOnlyMemory<byte>> SegmentIdType = new MetaInfoItemType<ReadOnlyMemory<byte>>("SegmentId");
		public static readonly MetaInfoItemType<int> SegmentChaptersIdType = new MetaInfoItemType<int>("SegmentChaptersId");

		public static readonly MetaInfoItemType<int> PhysicalEquivalentType = new MetaInfoItemType<int>("PhysicalEquivalent");

		public static readonly MetaInfoItemType<int> AssociatedTrackType = new MetaInfoItemType<int>("AssociatedTrack");

		public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden");
		public static readonly MetaInfoItemType<bool> IsEnabledType = new MetaInfoItemType<bool>("IsEnabled");

		public static readonly MetaInfoItemType<ChapterTitle> TitleType = new MetaInfoItemType<ChapterTitle>("Title");

		public static readonly MetaInfoItemType<bool> HasOperationsType = new MetaInfoItemType<bool>("HasOperations");
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
