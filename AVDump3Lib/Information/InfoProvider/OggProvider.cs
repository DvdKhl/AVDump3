using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;
using System;
using System.Linq;

namespace AVDump3Lib.Information.InfoProvider {
	public class OggProvider : MediaProvider {
		public OggFile FileInfo { get; private set; }

		public OggProvider(OggFile oggFile) : base("OggProvider") { Populate(oggFile); }

		private void Populate(OggFile oggFile) {
			if(oggFile == null) {
				return;
			}

			Add(FileSizeType, oggFile.FileSize);
			Add(OverheadType, oggFile.Overhead);
			Add(DurationType, oggFile.Bitstreams.Where(b => !(b is UnknownOGGBitStream)).Max(b => (double)b.Duration));

			MetaInfoContainer stream = null;
			foreach(var bitStream in oggFile.Bitstreams) {
				if(bitStream is VideoOGGBitStream) {
					stream = new MetaInfoContainer(bitStream.Id, VideoStreamType);
					var vs = (VideoOGGBitStream)bitStream;
					Add(stream, VideoStream.PixelDimensionsType, new Dimensions(vs.Width, vs.Height));
					Add(stream, MediaStream.StatedSampleRateType, vs.FrameRate);
					Add(stream, MediaStream.SampleCountType, vs.FrameCount);
					AddNode(stream);


				} else if(bitStream is AudioOGGBitStream) {
					var audio = (AudioOGGBitStream)bitStream;
					stream = new MetaInfoContainer(bitStream.Id, AudioStreamType);
					Add(stream, AudioStream.ChannelCountType, audio.ChannelCount);
					Add(stream, MediaStream.StatedSampleRateType, audio.SampleRate);
					Add(stream, MediaStream.SampleCountType, audio.SampleCount);
					AddNode(stream);

				} else if(bitStream is SubtitleOGGBitStream) {
					stream = new MetaInfoContainer(bitStream.Id, SubtitleStreamType);
					AddNode(stream);
				}
				if(stream == null) {
					stream = new MetaInfoContainer(bitStream.Id, MediaStreamType);
					AddNode(stream);
				}

				var duration = TimeSpan.FromSeconds(bitStream.Duration);

				Add(stream, MediaStream.IdType, bitStream.Id);
				Add(stream, MediaStream.SizeType, bitStream.Size);
				Add(stream, MediaStream.DurationType, duration);
				Add(stream, MediaStream.ContainerCodecNameType, bitStream.CodecName);
				Add(stream, MediaStream.BitrateType, bitStream.Size * 8 / duration.TotalSeconds);

				if(bitStream is IVorbisComment) {
					var track = (IVorbisComment)bitStream;
					if(track.Comments.Items.Contains("title")) Add(stream, MediaStream.TitleType, track.Comments.Items["title"].Value.Aggregate((acc, str) => acc + "," + str));
					if(track.Comments.Items.Contains("language")) Add(stream, MediaStream.LanguageType, track.Comments.Items["language"].Value.Aggregate((acc, str) => acc + "," + str));
				}

				if(bitStream is IOGMStream) Add(stream, MediaStream.ContainerCodecNameType, ((IOGMStream)bitStream).ActualCodecName);

			}

			if(oggFile.Bitstreams.All(b => b is AudioOGGBitStream)) {
				Add(SuggestedFileExtensionType, "ogg");
			} else if(oggFile.Bitstreams.All(b => b.IsOfficiallySupported)) {
				Add(SuggestedFileExtensionType, "ogv");
			} else if(oggFile.Bitstreams.Any(b => b is OGMAudioOGGBitStream || b is OGMVideoOGGBitStream || b is OGMTextOGGBitStream)) {
				Add(SuggestedFileExtensionType, "ogm");
			}

		}
	}


}


//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using AVDump2Lib.BlockConsumers;
//using System.Globalization;
//using AVDump2Lib.InfoProvider.Tools;
//using AVDump2Lib.BlockConsumers.OgmOgg;

//namespace AVDump2Lib.InfoGathering.InfoProvider {
//    public class OgmOggProvider : InfoProviderBase {
//        public OgmOggFile provider { get; private set; }

//        public OgmOggProvider(OgmOggFile provider) {
//            infos = new InfoCollection();
//            this.provider = provider;

//            Add(EntryKey.Size, provider.FileSize.ToString(), "byte");
//            Add(EntryKey.Overhead, provider.Overhead.ToString(), "byte");

//            Add(EntryKey.Duration, provider.Tracks.Max(t => !(t is UnkownTrack) && t.Duration.HasValue ? t.Duration.Value.TotalSeconds : 0).ToString("0.000", CultureInfo.InvariantCulture), "s");

//            int[] indeces = new int[4];
//            foreach(var track in provider.Tracks) {
//                if(track is VideoTrack) {
//                    AddStreamInfo(track, StreamType.Video, indeces[0]++);
//                } else if(track is AudioTrack) {
//                    AddStreamInfo(track, StreamType.Audio, indeces[1]++);
//                } else if(track is SubtitleTrack) {
//                    AddStreamInfo(track, StreamType.Text, indeces[2]++);
//                } else if(track is UnkownTrack) {
//                    Add(StreamType.Unkown, indeces[3], EntryKey.Id, () => track.Id.ToString(), null);
//                    Add(StreamType.Unkown, indeces[3], EntryKey.Size, () => track.Length.ToString(), "byte");
//                    indeces[3]++;
//                }
//            }

//            if(provider.Tracks.Count == 1 && provider.Tracks[0].CodecName.Equals("Vorbis")) {
//                Add(EntryKey.Extension, "ogg", null);
//            } else {
//                Add(EntryKey.Extension, "ogm", null);
//            }
//        }

//        private void AddStreamInfo(Track track, StreamType type, int index) {
//            Add(type, index, EntryKey.Index, () => index.ToString(), null);
//            Add(type, index, EntryKey.Id, () => track.Id.ToString(), null);
//            Add(type, index, EntryKey.Size, () => track.Length.ToString(), "byte");
//            Add(type, index, EntryKey.Bitrate, () => track.Bitrate.HasValue? track.Bitrate.Value.ToString("0", CultureInfo.InvariantCulture) : null, "bit/s");
//            Add(type, index, EntryKey.Duration, () => track.Duration.HasValue ? track.Duration.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture) : null, "s");

//            if(track.Comments != null) {
//                if(track.Comments.Comments.Contains("title")) Add(type, index, EntryKey.Title, () => track.Comments.Comments["title"].Value[0], null);
//                if(track.Comments.Comments.Contains("language")) Add(type, index, EntryKey.Language, () => track.Comments.Comments["language"].Value[0], null);
//            }

//            Add(type, index, EntryKey.CodecId, () => track.CodecName, null);

//            switch(type) {
//                case StreamType.Video: var vTrack = (VideoTrack)track;
//                    Add(type, index, EntryKey.FourCC, () => vTrack.FourCC, null);
//                    Add(type, index, EntryKey.FrameCount, () => vTrack.FrameCount.ToString(), null);
//                    Add(type, index, EntryKey.FrameRate, () => vTrack.FrameRate.ToString("0.000", CultureInfo.InvariantCulture), "fps");
//                    Add(type, index, EntryKey.Width, () => vTrack.Width.ToString(), "px");
//                    Add(type, index, EntryKey.Height, () => vTrack.Height.ToString(), "px");
//                    break;
//                case StreamType.Audio: var aTrack = (AudioTrack)track;
//                    Add(type, index, EntryKey.SampleCount, () => aTrack.SampleCount.ToString(), null);
//                    Add(type, index, EntryKey.SamplingRate, () => aTrack.SampleRate.ToString(CultureInfo.InvariantCulture), null);
//                    Add(type, index, EntryKey.ChannelCount, () => aTrack.ChannelCount.ToString(), null);
//                    break;
//                case StreamType.Text: break;
//            }
//        }

//        public override void Dispose() { }
//    }
//}
