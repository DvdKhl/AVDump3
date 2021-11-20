using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers.MP4;
using BXmlLib.DocTypes.MP4;
using BXmlLib.DocTypes.MP4.Boxes;
using System.Collections.Immutable;

namespace AVDump3Lib.Information.InfoProvider;

public class MP4Provider : MediaProvider {
	public MP4Node? RootBox { get; private set; }

	public MP4Provider(string name) : base(name) {
	}

	public MP4Provider(MP4Node? box) : base("MP4Provider") { Populate(box); }

	private void Populate(MP4Node? box) {
		RootBox = box;
		if(RootBox == null) {
			return;
		}

		var fileTypeBox = new FileTypeBox(RootBox.Descendents(MP4DocType.FileType).First().Data.Span);
		Add(FileSizeType, RootBox.Size);
		Add(ContainerVersionType,
			$"MajorBrands={fileTypeBox.MajorBrands} " +
			$"MinorVersion={fileTypeBox.MinorVersion} " +
			$"CompatibleBrands={string.Join("/", fileTypeBox.CompatibleBrands.ToArray().Select(x => x.ToString()))}");

		var movieHeaderBox = new MovieHeaderBox(RootBox.Descendents(MP4DocType.MovieHeader).First().Data.Span);
		Add(CreationDateType, movieHeaderBox.CreationDate);
		Add(ModificationDateType, movieHeaderBox.ModificationDate);

		Add(DurationType, movieHeaderBox.Duration / (double)movieHeaderBox.Timescale);

		var tracksBox = RootBox.Descendents(MP4DocType.Track).ToArray();

		if(tracksBox.All(x => "soun".Equals(MP4DocType.KeyToString(new HandlerBox(x.Descendents(MP4DocType.Handler).First().Data.Span).HandlerType)))) {
			Add(SuggestedFileExtensionType, ImmutableArray.Create("m4a"));
		} else {
			Add(SuggestedFileExtensionType, ImmutableArray.Create("mp4"));
		}

		foreach(var trackBox in tracksBox) PopulateTrack(trackBox);
	}

	private void PopulateTrack(MP4Node track) {
		var trackHeaderBox = new TrackHeaderBox(track.Descendents(MP4DocType.TrackHeader).Single().Data.Span);
		var mediaHeaderBox = new MediaHeaderBox(track.Descendents(MP4DocType.MediaHeader).Single().Data.Span);
		var sampleDescriptionBox = new SampleDescriptionBox(track.Descendents(MP4DocType.SampleDescription).Single().Data.Span);

		MetaInfoContainer stream = null;
		var handlerBox = new HandlerBox(track.Descendents(MP4DocType.Handler).Single().Data.Span);
		switch(MP4DocType.KeyToString(handlerBox.HandlerType)) {
			case "vide": stream = PopulateVideoTrack(track, in trackHeaderBox, in mediaHeaderBox, in sampleDescriptionBox); break;

			case "soun":
				stream = new MetaInfoContainer(trackHeaderBox.TrackId, AudioStreamType);
				AddNode(stream);
				break;

			case "hint":
				break;
		}

		Add(MediaStream.IdType, trackHeaderBox.TrackId);
		Add(stream, MediaStream.CreationDateType, trackHeaderBox.CreationDate, ("Source", "TrackHeader"));
		Add(stream, MediaStream.ModificationDateType, trackHeaderBox.ModificationDate, ("Source", "TrackHeader"));
		Add(stream, MediaStream.CreationDateType, mediaHeaderBox.CreationDate, ("Source", "MediaHeader"));
		Add(stream, MediaStream.ModificationDateType, mediaHeaderBox.ModificationDate, ("Source", "MediaHeader"));
	}




	private MetaInfoContainer PopulateVideoTrack(MP4Node trackNode, in TrackHeaderBox trackHeaderBox, in MediaHeaderBox mediaHeaderBox, in SampleDescriptionBox sampleDescriptionBox) {
		//var trackData = 
		BuildTrackData(trackNode);

		Dimensions? pixelDimensions = new((int)trackHeaderBox.Width, (int)trackHeaderBox.Height);
		//var displayDimensions = new Dimensions((int)trackHeaderBox.Width, (int)trackHeaderBox.Height);
		//var frameCount = 0L;

		var frameCountPerSample = new int[sampleDescriptionBox.EntryCount];
		for(var i = 0; i < sampleDescriptionBox.EntryCount; i++) {
			var visualSampleEntry = sampleDescriptionBox.GetVideoEntry(i);
			frameCountPerSample[i] = visualSampleEntry.FrameCount;

			if(visualSampleEntry.Width != pixelDimensions.Width || visualSampleEntry.Height != pixelDimensions.Height) {
				pixelDimensions = null;
				break;
			}
			if(visualSampleEntry.HorizontalResolution != pixelDimensions.Width || visualSampleEntry.VerticalResolution != pixelDimensions.Height) {
				pixelDimensions = null;
				break;
			}
		}








		MetaInfoContainer stream;
		stream = new MetaInfoContainer(trackHeaderBox.TrackId, VideoStreamType);

		Add(stream, VideoStream.PixelDimensionsType, pixelDimensions);
		Add(stream, VideoStream.DisplayDimensionsType, pixelDimensions);
		//Add(stream, MediaStream.SampleCountType, mediaHeaderBox., ("Source", "TrackHeader"));
		AddNode(stream);
		return stream;
	}

	private class TrackData {
		public SampleToIndex[] SampleToIndexMap = Array.Empty<SampleToIndex>();
		public uint[] SampleSizes = Array.Empty<uint>();
		public SampleToChunkBox.SampleToChunkItem[] SampleToChunkItems = Array.Empty<SampleToChunkBox.SampleToChunkItem>();
		public CompositionOffsetBox.SampleItem[] CompositionOffsetItems = Array.Empty<CompositionOffsetBox.SampleItem>();
		public TimeToSampleBox.SampleItem[] TimeToSampleItems = Array.Empty<TimeToSampleBox.SampleItem>();

	}
	private struct SampleToIndex {
		public uint ChunkIndex;
		public uint SampleDescriptionIndex;
		public uint SampleToChunkIndex;
		public uint CompositionOffsetIndex;
		public uint TimeToSampleIndex;
	}
	private static TrackData BuildTrackData(MP4Node trackNode) {
		var trackData = new TrackData();

		var sampleSizeNode = trackNode.Descendents(MP4DocType.SampleSize).FirstOrDefault();
		if(sampleSizeNode != null) {
			var sampleSizeBox = new SampleSizeBox(sampleSizeNode.Data.Span);
			trackData.SampleToIndexMap = new SampleToIndex[sampleSizeBox.Samples.Length];

			if(sampleSizeBox.SampleSize == 0) {
				trackData.SampleSizes = sampleSizeBox.Samples.ToArray();
			} else {

				var sampleSize = sampleSizeBox.SampleSize;
				trackData.SampleSizes = new uint[trackData.SampleSizes.Length];
				for(var i = 0; i < trackData.SampleSizes.Length; i++) {
					trackData.SampleSizes[i] = sampleSize;
				}
			}
		}

		var compactSampleSizeNode = trackNode.Descendents(MP4DocType.CompactSampleSize).FirstOrDefault();
		if(sampleSizeNode == null && compactSampleSizeNode != null) {
			var compactSampleSizeBox = new CompactSampleSizeBox(compactSampleSizeNode.Data.Span);
			trackData.SampleToIndexMap = new SampleToIndex[compactSampleSizeBox.Samples.Length];

			trackData.SampleSizes = new uint[compactSampleSizeBox.Samples.Length];
			for(var i = 0; i < trackData.SampleSizes.Length; i++) {
				trackData.SampleSizes[i] = compactSampleSizeBox.Samples[i];
			}
		}


		if(trackData.SampleToIndexMap != null) {
			var sampleToChunkNode = trackNode.Descendents(MP4DocType.SampleToChunk).FirstOrDefault();
			if(sampleToChunkNode != null) {
				var sampleSizeBox = new SampleToChunkBox(sampleToChunkNode.Data.Span);
				trackData.SampleToChunkItems = sampleSizeBox.Items.ToArray();



				var currentSampleIndex = 0;
				void setSampleToIndexItems(uint chunkCount, uint sampleToChunkIndex) {
					var sampleToChunk = trackData.SampleToChunkItems[sampleToChunkIndex];

					for(uint j = 0; j < chunkCount; j++) {
						for(var k = 0; k < sampleToChunk.SamplesPerChunk; k++) {
							ref var sampleToIndex = ref trackData.SampleToIndexMap[currentSampleIndex++];
							sampleToIndex.SampleDescriptionIndex = sampleToChunk.SampleDescriptionIndex;
							sampleToIndex.ChunkIndex = sampleToChunk.FirstChunk + j;
							sampleToIndex.SampleToChunkIndex = sampleToChunkIndex;
						}
					}
				}

				var curSampleToChunk = trackData.SampleToChunkItems[0];
				for(uint i = 1; i < trackData.SampleToChunkItems.Length; i++) {
					var nextSampleToChunk = trackData.SampleToChunkItems[i];

					var chunkCount = nextSampleToChunk.FirstChunk - curSampleToChunk.FirstChunk;
					setSampleToIndexItems(chunkCount, i - 1);
					curSampleToChunk = nextSampleToChunk;
				}
				setSampleToIndexItems(
					(uint)(trackData.SampleToIndexMap.Length - currentSampleIndex) / trackData.SampleToChunkItems.Last().SamplesPerChunk,
					(uint)trackData.SampleToChunkItems.Length - 1
				);
			}

			var compositionOffsetNode = trackNode.Descendents(MP4DocType.CompositionOffset).FirstOrDefault();
			if(compositionOffsetNode != null) {
				var compositionOffsetBox = new CompositionOffsetBox(compositionOffsetNode.Data.Span);
				trackData.CompositionOffsetItems = compositionOffsetBox.Samples.ToArray();

				var currentSampleIndex = 0;
				for(uint i = 0; i < trackData.CompositionOffsetItems.Length; i++) {
					var sampleItem = trackData.CompositionOffsetItems[i];

					for(var j = 0; j < sampleItem.Count; j++) {
						ref var sampleToIndex = ref trackData.SampleToIndexMap[currentSampleIndex++];
						sampleToIndex.CompositionOffsetIndex = i;
					}
				}
			}


			var timeToSampleNode = trackNode.Descendents(MP4DocType.TimeToSample).FirstOrDefault();
			if(timeToSampleNode != null) {
				var timeToSampleBox = new TimeToSampleBox(compositionOffsetNode.Data.Span);
				trackData.TimeToSampleItems = timeToSampleBox.Samples.ToArray();

				var currentSampleIndex = 0;
				for(uint i = 0; i < trackData.TimeToSampleItems.Length; i++) {
					var sampleItem = trackData.TimeToSampleItems[i];

					for(var j = 0; j < sampleItem.Count; j++) {
						ref var sampleToIndex = ref trackData.SampleToIndexMap[currentSampleIndex++];
						sampleToIndex.TimeToSampleIndex = i;
					}
				}
			}



		}

		return trackData;
	}
}
