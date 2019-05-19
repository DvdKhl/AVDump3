using AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;
using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Cluster {
	public class ClusterSection : Section {
		public Dictionary<int, Track> Tracks { get; private set; }

		public ClusterSection() { Tracks = new Dictionary<int, Track>(); }
		public ulong TimeCodeScale { get; set; }
		private long timecode;

		public void AddTracks(IEnumerable<TrackEntrySection> tracks) {
			foreach(var track in tracks) {
				var trackNumber = (int)track.TrackNumber.Value;
				Tracks.Add(trackNumber, new Track(trackNumber, track.TrackTimecodeScale ?? 1d, track));
			}
		}

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.SimpleBlock || reader.DocElement == MatroskaDocType.Block) {
                MatroskaDocType.RetrieveMatroskaBlock(reader, out MatroskaBlock matroskaBlock);
                Track track;
				if(
					!Tracks.TryGetValue(matroskaBlock.TrackNumber, out track) &&
					!Tracks.TryGetValue(~matroskaBlock.TrackNumber, out track)
				) {
					Tracks.Add(~matroskaBlock.TrackNumber, track = new Track(~matroskaBlock.TrackNumber, 1, null));
				}
				track.Timecodes.Add(new TrackTimecode((ulong)((matroskaBlock.TimeCode + timecode) * track.TimecodeScale * TimeCodeScale), matroskaBlock.FrameCount, matroskaBlock.Data.Length));

			} else if(reader.DocElement == MatroskaDocType.BlockGroup) {
				Read(reader);
			} else if(reader.DocElement == MatroskaDocType.Timecode) {
				timecode = (long)(ulong)reader.RetrieveValue();
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() { yield break; }


		public class Track {
			private TrackEntrySection mkvTrack;

			public int TrackNumber { get; private set; }
			public double TimecodeScale { get; private set; }

			public List<TrackTimecode> Timecodes { get; private set; }

			public Track(int trackNumber, double timecodeScale, TrackEntrySection mkvTrack) {
				TrackNumber = trackNumber;
				TimecodeScale = mkvTrack?.TrackTimecodeScale ?? 1d;
				this.mkvTrack = mkvTrack;

				Timecodes = new List<TrackTimecode>();
			}


			public TrackInfo TrackInfo { get { return (trackInfo == null) ? trackInfo = CalcTrackInfo() : trackInfo; } }
			private TrackInfo trackInfo;

			private TrackInfo CalcTrackInfo() {
				try {
					//Hack to get info from subtitles stored CodecPrivate
					if(
						Timecodes.Count == 0 && mkvTrack.CodecPrivate != null &&
						("S_TEXT/ASS".Equals(mkvTrack.CodecId) || "S_TEXT/SSA".Equals(mkvTrack.CodecId))
					) {
						ExtractSubtitleInfo();
					}

					Timecodes.Sort();


					double[] rate = new double[3];
					double? minSampleRate = null, maxSampleRate = null;

					var oldTC = Timecodes.FirstOrDefault();

					int pos = 0, prevPos = 0, prevprevPos;
					double maxDiff;

					int frames = oldTC.frames;
					long trackSize = (mkvTrack.CodecPrivate?.Length ?? 0) + oldTC.size;

					var sampleRateHistogram = new Dictionary<double, int>();
					var bitRateHistogram = new Dictionary<double, int>();

					foreach(var timecode in Timecodes.Skip(1)) {
						//fps[pos] = 1d / ((timecode.timeCode - oldTC.timeCode) / (double)oldTC.frames / 1000000000d);
						rate[pos] = (1000000000d * oldTC.frames) / (timecode.timeCode - oldTC.timeCode);

						if(!double.IsInfinity(rate[pos]) && !double.IsNaN(rate[pos])) {
							if(!sampleRateHistogram.ContainsKey(rate[pos])) sampleRateHistogram[rate[pos]] = 0;
							sampleRateHistogram[rate[pos]]++;
						}


						var bitRate = timecode.size * 8000000000d / (timecode.frames * (timecode.timeCode - oldTC.timeCode));
						if(!double.IsInfinity(bitRate) && !double.IsNaN(bitRate)) {
							if(!bitRateHistogram.ContainsKey(bitRate)) bitRateHistogram[bitRate] = 0;
							bitRateHistogram[bitRate] += timecode.frames;
						}

						oldTC = timecode;
						prevprevPos = prevPos;
						prevPos = pos;
						pos = (pos + 1) % 3;

						trackSize += timecode.size;
						frames += timecode.frames;

						maxDiff = (rate[prevprevPos] + rate[pos] / 2) * 0.1;
						if(Math.Abs(rate[prevPos] - rate[prevprevPos]) < maxDiff && Math.Abs(rate[prevPos] - rate[pos]) < maxDiff) {
							if(!minSampleRate.HasValue || minSampleRate.Value > rate[prevPos]) minSampleRate = rate[prevPos];
							if(!maxSampleRate.HasValue || maxSampleRate.Value < rate[prevPos]) maxSampleRate = rate[prevPos];
						}
					}

					var trackLength = TimeSpan.FromMilliseconds((Timecodes.LastOrDefault().timeCode - Timecodes.FirstOrDefault().timeCode) / 1000000);

					return new TrackInfo {
						SampleRateHistogram = sampleRateHistogram.Select(kvp => new SampleRateCountPair(kvp.Key, kvp.Value)).ToList().AsReadOnly(),
						AverageBitrate = (trackSize != 0 && trackLength.Ticks != 0) ? trackSize * 8 / trackLength.TotalSeconds : (double?)null,
						AverageSampleRate = (frames != 0 && trackLength.Ticks != 0) ? frames / trackLength.TotalSeconds : (double?)null,
						MinSampleRate = minSampleRate,
						MaxSampleRate = maxSampleRate,
						TrackLength = trackLength,
						TrackSize = trackSize,
						SampleCount = frames
					};

				} catch(Exception) { }

				return null;
			}

			private void ExtractSubtitleInfo() {
				var assOrSsaContent = Encoding.UTF8.GetString(mkvTrack.CodecPrivate);

				var eventSectionStart = assOrSsaContent.IndexOf("[Events]");
				if(eventSectionStart < 0) return;

				var formatStart = assOrSsaContent.IndexOf("Format:", eventSectionStart);
				if(formatStart < 0) return;
				formatStart += 7;

				var formatEnd = assOrSsaContent.IndexOf("\n", formatStart) - 1;
				if(formatEnd < 0) return;

				var columns = assOrSsaContent.Substring(formatStart, formatEnd - formatStart).Replace(" ", "").Split(',');
				var startIndex = Array.IndexOf(columns, "Start");
				var endIndex = Array.IndexOf(columns, "End");

				var lines = assOrSsaContent.Substring(formatEnd + 1).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split(new[] { ',' }, columns.Length)).ToArray();

				foreach(var line in lines) {
					Timecodes.Add(new TrackTimecode((ulong)TimeSpan.ParseExact(line[startIndex], @"h\:mm\:ss\.ff", CultureInfo.InvariantCulture).TotalMilliseconds * 1000000, 1, 0));
				}
			}
		}

		public class TrackInfo {
			public double? AverageBitrate { get; internal set; }
			public double? AverageSampleRate { get; internal set; }
			public double? MinSampleRate { get; internal set; }
			public double? MaxSampleRate { get; internal set; }

			public ReadOnlyCollection<SampleRateCountPair> SampleRateHistogram { get; internal set; }

			public TimeSpan TrackLength { get; internal set; }
			public long TrackSize { get; internal set; }
			public int SampleCount { get; internal set; }

			//public TrackInfo(ReadOnlyCollection<LaceRateCountPair> laceRateHistogram, double? averageBitrate, double? averageLaceRate, TimeSpan trackLength, long trackSize, int laceCount) {
			//	AverageBitrate = averageBitrate;
			//	AverageLaceRate = averageLaceRate;
			//	TrackLength = trackLength; TrackSize = trackSize; LaceCount = laceCount;
			//	LaceRateHistogram = laceRateHistogram;
			//}
		}

		public class SampleRateCountPair {
			public double SampleRate { get; private set; }
			public long Count { get; private set; }
			public SampleRateCountPair(double laceRate, long count) { SampleRate = laceRate; Count = count; }
		}

		public struct TrackTimecode : IComparable<TrackTimecode> {
			public ulong timeCode;
			public byte frames;
			public int size;

			public TrackTimecode(ulong timeCode, byte frames, int size) {
				this.frames = frames; this.size = size; this.timeCode = timeCode;
			}

			public int CompareTo(TrackTimecode other) {
				return timeCode.CompareTo(other.timeCode);
			}
		}
	}
}
