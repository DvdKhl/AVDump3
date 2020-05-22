using AVDump3Lib.Information.FormatHeaders;
using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using AVDump3Lib.Misc.Linq;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cluster;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags;
using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Information.InfoProvider {
	public class MatroskaProvider : MediaProvider {
		public MatroskaFile MFI { get; private set; }

		private double CalcDeviation(ReadOnlyCollection<ClusterSection.SampleRateCountPair> histogram) {
			var count = histogram.Sum(i => i.Count);
			var sqrSum = histogram.Sum(i => i.SampleRate * i.SampleRate);
			var mean = histogram.Sum(i => i.SampleRate) / count;
			return Math.Sqrt(sqrSum / count - mean * mean);
		}

		public MatroskaProvider(MatroskaFile mfi) : base("MatroskaProvider") { Populate(mfi); }

		private void Populate(MatroskaFile mfi) {
			MFI = mfi;
			if(MFI == null) {
				return;
			}

			Add(FileSizeType, MFI.SectionSize);
			Add(ContainerVersionType, $"DocType={MFI.EbmlHeader.DocType} DocTypeVersion={MFI.EbmlHeader.DocTypeVersion}");
			Add(CreationDateType, MFI.Segment.SegmentInfo.ProductionDate);
			Add(DurationType, MFI.Segment.SegmentInfo.Duration.OnNotNullReturn(x => x * MFI.Segment.SegmentInfo.TimecodeScale / 1000000000d));


			Add(IdType, MFI.Segment.SegmentInfo.SegmentUId);
			Add(PreviousIdType, MFI.Segment.SegmentInfo.PreviousUId);
			Add(NextIdType, MFI.Segment.SegmentInfo.NextUId);
			Add(PreviousFileNameType, MFI.Segment.SegmentInfo.PreviousFilename);
			Add(NextFileNameType, MFI.Segment.SegmentInfo.NextFilename);


			Add(WritingAppType, MFI.Segment.SegmentInfo.WritingApp);
			Add(MuxingAppType, MFI.Segment.SegmentInfo.MuxingApp);

			if(MFI.EbmlHeader.DocType.Equals("webm", StringComparison.OrdinalIgnoreCase)) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("webm"));
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Video && e.Video.StereoMode != VideoSection.StereoModes.Mono)) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("mk3d"));
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Video)) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("mkv"));
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Audio)) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("mka"));
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Subtitle)) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("mks"));
			}

			foreach(var track in MFI.Segment.Tracks.Items) {
				PopulateTrack(track);
			}

			var mkvTags = MFI.Segment.Tags.Count != 0 ? MFI.Segment.Tags.MaxBy(t => t.Items.Count) : null;
			if(mkvTags != null) PopulateTags(mkvTags);

			Add(AttachmentsSizeType, MFI.Segment.Attachments?.SectionSize ?? 0);
			if(MFI.Segment.Attachments != null) {
				foreach(var attachment in MFI.Segment.Attachments.Items) {
					var attachments = new MetaInfoContainer(attachment.FileUId, AttachmentType);
					Add(attachments, Attachment.IdType, (long)attachment.FileUId);
					Add(attachments, Attachment.SizeType, attachment.SectionSize);
					Add(attachments, Attachment.TypeType, attachment.FileMimeType);
					Add(attachments, Attachment.DescriptionType, attachment.FileDescription);
					AddNode(attachments);
				}
			}

			if(MFI.Segment.Chapters != null) {
				foreach(var edition in MFI.Segment.Chapters.Items) PopulateChapters(edition);
			}
		}

		private void PopulateChapters(EditionEntrySection edition) {
			var chapters = new MetaInfoContainer(edition.EditionUId ?? (ulong)Nodes.Count(x => x.Type == ChaptersType), ChaptersType);

			Add(chapters, Chapters.IdType, (int?)edition.EditionUId);
			Add(chapters, Chapters.IsHiddenType, edition.EditionFlags.HasFlag(EditionEntrySection.Options.Hidden));
			Add(chapters, Chapters.IsDefaultType, edition.EditionFlags.HasFlag(EditionEntrySection.Options.Default));
			Add(chapters, Chapters.IsOrderedType, edition.EditionFlags.HasFlag(EditionEntrySection.Options.Ordered));

			foreach(var atom in edition.ChapterAtoms) PopulateChaptersSub(atom, chapters);

			AddNode(chapters);
		}
		private void PopulateChaptersSub(ChapterAtomSection atom, MetaInfoContainer chapters) {
			var chapter = new MetaInfoContainer(atom.ChapterUId ?? (ulong)chapters.Nodes.Count(x => x.Type == Chapters.ChapterType), Chapters.ChapterType);
			chapters.AddNode(chapter);

			Add(chapter, Chapter.IdType, (int?)atom.ChapterUId);
			Add(chapter, Chapter.IdStringType, atom.ChapterStringUId);
			Add(chapter, Chapter.TimeStartType, atom.ChapterTimeStart / 1000000000d);
			Add(chapter, Chapter.TimeEndType, atom.ChapterTimeEnd.OnNotNullReturn(v => v / 1000000000d));
			Add(chapter, Chapter.IsHiddenType, atom.ChapterFlags.HasFlag(ChapterAtomSection.Options.Hidden));
			Add(chapter, Chapter.IsEnabledType, atom.ChapterFlags.HasFlag(ChapterAtomSection.Options.Enabled));
			Add(chapter, Chapter.SegmentIdType, atom.ChapterSegmentUId);
			Add(chapter, Chapter.SegmentChaptersIdType, (int?)atom.ChapterSegmentEditionUId);
			Add(chapter, Chapter.PhysicalEquivalentType, (int?)atom.ChapterPhysicalEquiv);
			if(atom.ChapterTrack != null) foreach(var tid in atom.ChapterTrack.ChapterTrackNumbers) Add(chapter, Chapter.AssociatedTrackType, (int)tid);
			Add(chapter, Chapter.SegmentIdType, atom.ChapterSegmentUId);
			foreach(var chapterDisplay in atom.ChapterDisplays) Add(chapter, Chapter.TitleType, new ChapterTitle(chapterDisplay.ChapterString, chapterDisplay.ChapterLanguages, chapterDisplay.ChapterCountries));
			foreach(var subAtom in atom.ChapterAtoms) PopulateChaptersSub(subAtom, chapter);
			Add(chapter, Chapter.HasOperationsType, atom.ChapterProcesses.Count != 0);
		}


		private void PopulateTags(TagsSection mkvTags) {
			var tags = new List<TargetedTag>();

			foreach(var mkvTag in mkvTags.Items) {

				var targets = from trackId in mkvTag.Targets.TrackUIds select new Target(TagTarget.Track, (long)trackId);
				targets = targets.Concat(from editionId in mkvTag.Targets.EditionUIds select new Target(TagTarget.Chapter, (long)editionId));
				targets = targets.Concat(from chapterId in mkvTag.Targets.ChapterUIds select new Target(TagTarget.Chapters, (long)chapterId));
				targets = targets.Concat(from attachmentId in mkvTag.Targets.AttachmentUIds select new Target(TagTarget.Attachment, (long)attachmentId));

				var tag = new TargetedTag(targets, mkvTag.SimpleTags.Select(simpleTag => PopulateTagsSub(simpleTag)));
				tags.Add(tag);
			}
			Add(TagsType, tags);
		}
		private Tag PopulateTagsSub(SimpleTagSection tag) {
			return new Tag(tag.TagName, (object)tag.TagString ?? tag.TagBinary, tag.TagLanguage, tag.TagDefault, tag.SimpleTags.Select(simpleTag => PopulateTagsSub(simpleTag)));
		}


		private readonly int[] indeces = new int[4];
		private void PopulateTrack(TrackEntrySection track) {
			var trackIndex = 0;

			MetaInfoContainer stream;
			var trackInfo = MFI.Segment.Cluster.Tracks[(int)track.TrackNumber.Value].TrackInfo;

			switch(track.TrackType) {
				case TrackEntrySection.Types.Video:
					stream = new MetaInfoContainer(track.TrackUId ?? (ulong)Nodes.Count(x => x.Type == VideoStreamType), VideoStreamType);
					AddNode(stream);
					trackIndex = indeces[0]++;

					Add(stream, VideoStream.AspectRatioBehaviorType, Convert(track.Video.AspectRatioType));
					Add(stream, VideoStream.ColorSpaceType, track.Video.ColorSpace.OnNotNullReturn(x => BitConverter.ToInt32(x, 0)));
					Add(stream, VideoStream.DisplayDimensionsType, new Dimensions((int)track.Video.DisplayWidth, (int)track.Video.DisplayHeight));
					Add(stream, VideoStream.DisplayUnitType, Convert(track.Video.DisplayUnit));
					Add(stream, VideoStream.HasAlphaType, track.Video.AlphaMode != 0);
					Add(stream, VideoStream.IsInterlacedType, track.Video.Interlaced);
					Add(stream, VideoStream.PixelCropType, new CropSides((int)track.Video.PixelCropTop, (int)track.Video.PixelCropRight, (int)track.Video.PixelCropBottom, (int)track.Video.PixelCropLeft));
					Add(stream, VideoStream.PixelDimensionsType, new Dimensions((int)track.Video.PixelWidth, (int)track.Video.PixelHeight));
					Add(stream, VideoStream.StorageAspectRatioType, track.Video.PixelWidth / (double)track.Video.PixelHeight);
					Add(stream, VideoStream.PixelAspectRatioType, (track.Video.DisplayWidth * track.Video.PixelHeight) / (double)(track.Video.DisplayHeight * track.Video.PixelWidth));
					Add(stream, VideoStream.DisplayAspectRatioType, track.Video.DisplayWidth / (double)track.Video.DisplayHeight);

					Add(stream, MediaStream.SampleCountType, trackInfo?.SampleCount);
					Add(stream, MediaStream.StatedSampleRateType, track.DefaultDuration.HasValue ? 1000000000d / track.DefaultDuration.Value : 0);

					Add(stream, VideoStream.StereoModeType, Convert(track.Video.StereoMode));
					//Add(stream, VideoStream.StereoModeType, Convert(track.Video.OldStereoMode));

					break;

				case TrackEntrySection.Types.Audio:
					stream = new MetaInfoContainer(track.TrackUId ?? (ulong)Nodes.Count(x => x.Type == AudioStreamType), AudioStreamType);
					AddNode(stream);
					trackIndex = indeces[1]++;

					Add(stream, AudioStream.BitDepthType, (int?)track.Audio.BitDepth);
					Add(stream, AudioStream.ChannelCountType, (int?)track.Audio.ChannelCount);
					Add(stream, MediaStream.OutputSampleRateType, track.Audio.OutputSamplingFrequency);
					Add(stream, MediaStream.StatedSampleRateType, track.Audio.SamplingFrequency);
					if(trackInfo != null) Add(stream, MediaStream.SampleCountType, ((long)(trackInfo.TrackLength.Ticks * track.Audio.SamplingFrequency) / 10000000));
					break;

				case TrackEntrySection.Types.Subtitle:
					stream = new MetaInfoContainer(track.TrackUId ?? (ulong)Nodes.Count(x => x.Type == SubtitleStreamType), SubtitleStreamType);
					AddNode(stream);
					trackIndex = indeces[2]++;
					break;

				default:
					stream = new MetaInfoContainer(track.TrackUId ?? (ulong)Nodes.Count(x => x.Type == MediaStreamType), MediaStreamType);
					AddNode(stream);
					trackIndex = indeces[3]++;
					break;
			}

			if(trackInfo != null && (track.TrackType == TrackEntrySection.Types.Video || track.TrackType == TrackEntrySection.Types.Audio)) {
				Add(stream, MediaStream.SampleRateHistogramType, trackInfo.SampleRateHistogram.Select(x => new SampleRateCountPair(x.SampleRate, x.Count)).ToList());

				Add(stream, MediaStream.AverageSampleRateType, trackInfo.AverageSampleRate);
				Add(stream, MediaStream.MinSampleRateType, trackInfo.MinSampleRate);
				Add(stream, MediaStream.MaxSampleRateType, trackInfo.MaxSampleRate);
				Add(stream, MediaStream.DominantSampleRateType, trackInfo.SampleRateHistogram.OrderByDescending(p => p.Count).FirstOrDefault()?.SampleRate);
				Add(stream, MediaStream.SampleRateVarianceType, CalcDeviation(trackInfo.SampleRateHistogram));
				Add(stream, MediaStream.BitrateType, trackInfo.AverageBitrate);
			}

			Add(stream, MediaStream.IndexType, trackIndex);
			Add(stream, MediaStream.IdType, track.TrackUId);
			Add(stream, MediaStream.IsDefaultType, track.TrackFlags.HasFlag(TrackEntrySection.Options.Default));
			Add(stream, MediaStream.IsEnabledType, track.TrackFlags.HasFlag(TrackEntrySection.Options.Enabled));
			Add(stream, MediaStream.IsForcedType, track.TrackFlags.HasFlag(TrackEntrySection.Options.Forced));
			Add(stream, MediaStream.IsOverlayType, track.TrackOverlay.Count != 0);
			Add(stream, MediaStream.LanguageType, track.Language);
			Add(stream, MediaStream.TitleType, track.Name);
			Add(stream, MediaStream.ContainerCodecIdType, track.CodecId);
			Add(stream, MediaStream.ContainerCodecNameType, track.CodecName);
			if(MFI.Segment.Cues != null) Add(stream, MediaStream.CueCountType, MFI.Segment.Cues.CuePoints.Count(cp => cp.CueTrackPositions.Any(p => p.CueTrack == track.TrackUId)));

			if(track.CodecPrivate != null) {
				Add(stream, MediaStream.CodecPrivateSizeType, track.CodecPrivate.Length);

				if("V_MS/VFW/FOURCC".InvEquals(track.CodecId) && track.CodecPrivate.Length >= BitmapInfoHeader.LENGTH) {
					var header = new BitmapInfoHeader(track.CodecPrivate);
					Add(stream, MediaStream.ContainerCodecCCType, header.FourCC);
				}
				if("A_MS/ACM".InvEquals(track.CodecId) && track.CodecPrivate.Length >= WaveFormatEx.LENGTH) {
					var header = new WaveFormatEx(track.CodecPrivate);
					Add(stream, MediaStream.ContainerCodecCCType, header.TwoCC);
				}
			}

			if(trackInfo != null) {
				Add(stream, MediaStream.DurationType, trackInfo.TrackLength);
				Add(stream, MediaStream.SizeType, trackInfo.TrackSize);
			}
		}




		private AspectRatioBehaviors Convert(VideoSection.ARType t) {
			return t switch
			{
				VideoSection.ARType.FreeResizing => AspectRatioBehaviors.FreeResizing,
				VideoSection.ARType.KeepAR => AspectRatioBehaviors.KeepAR,
				VideoSection.ARType.Fixed => AspectRatioBehaviors.Fixed,
				_ => AspectRatioBehaviors.Unknown,
			};
		}

		private DisplayUnits Convert(VideoSection.Unit u) {
			return u switch
			{
				VideoSection.Unit.Pixels => DisplayUnits.Pixel,
				VideoSection.Unit.Centimeters => DisplayUnits.Meter,
				VideoSection.Unit.Inches => DisplayUnits.Meter,
				VideoSection.Unit.AspectRatio => DisplayUnits.AspectRatio,
				_ => DisplayUnits.Unknown,
			};
		}
		private StereoModes Convert(VideoSection.StereoModes s) {
			return s switch
			{
				VideoSection.StereoModes.Mono => StereoModes.Mono,
				VideoSection.StereoModes.LeftRight => StereoModes.LeftRight,
				VideoSection.StereoModes.BottomTop => StereoModes.TopBottom | StereoModes.Reversed,
				VideoSection.StereoModes.TopBottom => StereoModes.TopBottom,
				VideoSection.StereoModes.CheckBoardRight => StereoModes.Checkboard | StereoModes.Reversed,
				VideoSection.StereoModes.CheckboardLeft => StereoModes.Checkboard,
				VideoSection.StereoModes.RowInterleavedRight => StereoModes.RowInterleaved | StereoModes.Reversed,
				VideoSection.StereoModes.RowInterleavedLeft => StereoModes.RowInterleaved,
				VideoSection.StereoModes.ColumnInterleavedRight => StereoModes.ColumnInterleaved | StereoModes.Reversed,
				VideoSection.StereoModes.ColumnInterleavedLeft => StereoModes.ColumnInterleaved,
				VideoSection.StereoModes.AnaGlyphCyanRed => StereoModes.AnaGlyph | StereoModes.CyanRed,
				VideoSection.StereoModes.RightLeft => StereoModes.LeftRight | StereoModes.Reversed,
				VideoSection.StereoModes.AnaGlyphGreenMagenta => StereoModes.AnaGlyph | StereoModes.GreenMagenta,
				VideoSection.StereoModes.AlternatingFramesRight => StereoModes.FrameAlternating | StereoModes.Reversed,
				VideoSection.StereoModes.AlternatingFramesLeft => StereoModes.FrameAlternating,
				_ => StereoModes.Other,
			};
		}
		private StereoModes Convert(VideoSection.OldStereoModes? s) {
			return (s.GetValueOrDefault(VideoSection.OldStereoModes.Mono)) switch
			{
				VideoSection.OldStereoModes.Mono => StereoModes.Mono,
				_ => StereoModes.Other,
			};
		}
	}
}