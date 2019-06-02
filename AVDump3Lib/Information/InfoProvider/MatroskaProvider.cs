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
using System;
using System.Collections.Generic;
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

			Add(FileSizeType, MFI.SectionSize.Value);
			Add(ContainerVersionType, $"DocType={MFI.EbmlHeader.DocType} DocTypeVersion={MFI.EbmlHeader.DocTypeVersion}");
			Add(CreationDateType, MFI.Segment.SegmentInfo.ProductionDate);
			Add(DurationType, MFI.Segment.SegmentInfo.Duration.OnNotNullReturn(x => x * MFI.Segment.SegmentInfo.TimecodeScale / 1000000000d));

			Add(WritingAppType, MFI.Segment.SegmentInfo.WritingApp);
			Add(MuxingAppType, MFI.Segment.SegmentInfo.MuxingApp);

			if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Video)) {
				Add(SuggestedFileExtensionType, "mkv");
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Audio)) {
				Add(SuggestedFileExtensionType, "mka");
			} else if(MFI.Segment.Tracks.Items.Any(e => e.TrackType == TrackEntrySection.Types.Subtitle)) {
				Add(SuggestedFileExtensionType, "mks");
			}

			foreach(var track in MFI.Segment.Tracks.Items) {
				PopulateTrack(track);
			}

			var mkvTags = MFI.Segment.Tags.Count != 0 ? MFI.Segment.Tags.MaxBy(t => t.Items.Count) : null;
			if(mkvTags != null) PopulateTags(mkvTags);


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


		private int[] indeces = new int[4];
		private void PopulateTrack(TrackEntrySection track) {
			var trackIndex = 0;

			MetaInfoContainer stream;
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
					Add(stream, VideoStream.PixelAspectRatioType, (track.Video.DisplayWidth / (double)track.Video.DisplayHeight) / (track.Video.PixelWidth / (double)track.Video.PixelHeight));
					Add(stream, VideoStream.DisplayAspectRatioType, track.Video.DisplayWidth / (double)track.Video.DisplayHeight);

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

			var trackInfo = MFI.Segment.Cluster.Tracks[(int)track.TrackNumber.Value].TrackInfo;
			if(trackInfo != null) {
				Add(stream, MediaStream.SampleCountType, trackInfo.SampleCount);
				Add(stream, MediaStream.SampleRateHistogramType, trackInfo.SampleRateHistogram.Select(x => new SampleRateCountPair(x.SampleRate, x.Count)).ToList());
				Add(stream, MediaStream.AverageSampleRateType, trackInfo.AverageSampleRate);
				Add(stream, MediaStream.MinSampleRateType, trackInfo.MinSampleRate);
				Add(stream, MediaStream.MaxSampleRateType, trackInfo.MaxSampleRate);
				Add(stream, MediaStream.DominantSampleRateType, trackInfo.SampleRateHistogram.OrderByDescending(p => p.Count).FirstOrDefault()?.SampleRate);
				Add(stream, MediaStream.SampleRateVarianceType, CalcDeviation(trackInfo.SampleRateHistogram));
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

				if("V_MS/VFW/FOURCC".Equals(track.CodecId) && track.CodecPrivate.Length >= BitmapInfoHeader.LENGTH) {
					var header = new BitmapInfoHeader(track.CodecPrivate);
					Add(stream, MediaStream.ContainerCodecIdType, header.FourCC);
				}
				if("A_MS/ACM".Equals(track.CodecId) && track.CodecPrivate.Length >= WaveFormatEx.LENGTH) {
					var header = new WaveFormatEx(track.CodecPrivate);
					Add(stream, MediaStream.ContainerCodecIdType, header.TwoCC);
				}
			}

			if(trackInfo != null) {
				Add(stream, MediaStream.BitrateType, trackInfo.AverageBitrate);
				Add(stream, MediaStream.DurationType, trackInfo.TrackLength);
				Add(stream, MediaStream.SizeType, trackInfo.TrackSize);
			}
		}




		private AspectRatioBehaviors Convert(VideoSection.ARType t) {
			switch(t) {
				case VideoSection.ARType.FreeResizing: return AspectRatioBehaviors.FreeResizing;
				case VideoSection.ARType.KeepAR: return AspectRatioBehaviors.KeepAR;
				case VideoSection.ARType.Fixed: return AspectRatioBehaviors.Fixed;
				default: return AspectRatioBehaviors.Unknown;
			}
		}

		private DisplayUnits Convert(VideoSection.Unit u) {
			switch(u) {
				case VideoSection.Unit.Pixels: return DisplayUnits.Pixel;
				case VideoSection.Unit.Centimeters: return DisplayUnits.Meter;
				case VideoSection.Unit.Inches: return DisplayUnits.Meter;
				case VideoSection.Unit.AspectRatio: return DisplayUnits.AspectRatio;
				default: return DisplayUnits.Unknown;
			}
		}
		private StereoModes Convert(VideoSection.StereoModes s) {
			switch(s) {
				case VideoSection.StereoModes.Mono: return StereoModes.Mono;
				case VideoSection.StereoModes.LeftRight: return StereoModes.LeftRight;
				case VideoSection.StereoModes.BottomTop: return StereoModes.TopBottom | StereoModes.Reversed;
				case VideoSection.StereoModes.TopBottom: return StereoModes.TopBottom;
				case VideoSection.StereoModes.CheckBoardRight: return StereoModes.Checkboard | StereoModes.Reversed;
				case VideoSection.StereoModes.CheckboardLeft: return StereoModes.Checkboard;
				case VideoSection.StereoModes.RowInterleavedRight: return StereoModes.RowInterleaved | StereoModes.Reversed;
				case VideoSection.StereoModes.RowInterleavedLeft: return StereoModes.RowInterleaved;
				case VideoSection.StereoModes.ColumnInterleavedRight: return StereoModes.ColumnInterleaved | StereoModes.Reversed;
				case VideoSection.StereoModes.ColumnInterleavedLeft: return StereoModes.ColumnInterleaved;
				case VideoSection.StereoModes.AnaGlyphCyanRed: return StereoModes.AnaGlyph | StereoModes.CyanRed;
				case VideoSection.StereoModes.RightLeft: return StereoModes.LeftRight | StereoModes.Reversed;
				case VideoSection.StereoModes.AnaGlyphGreenMagenta: return StereoModes.AnaGlyph | StereoModes.GreenMagenta;
				case VideoSection.StereoModes.AlternatingFramesRight: return StereoModes.FrameAlternating | StereoModes.Reversed;
				case VideoSection.StereoModes.AlternatingFramesLeft: return StereoModes.FrameAlternating;
				default: return StereoModes.Other;
			}
		}
		private StereoModes Convert(VideoSection.OldStereoModes? s) {
			switch(s.GetValueOrDefault(VideoSection.OldStereoModes.Mono)) {
				case VideoSection.OldStereoModes.Mono: return StereoModes.Mono;
				default: return StereoModes.Other;
			}
		}
	}
}