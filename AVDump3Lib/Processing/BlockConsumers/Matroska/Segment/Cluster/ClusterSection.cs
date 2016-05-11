using AVDump3Lib.BlockConsumers.Matroska.Segment.Tracks;
using CSEBML;
using CSEBML.DocTypes.Matroska;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Cluster {
    public class ClusterSection : Section {
		public Dictionary<int, Track> Tracks { get; private set; }

		public ClusterSection() { Tracks = new Dictionary<int, Track>(); }
		public ulong TimeCodeScale { get; set; }
		private long timecode;

		public void AddTracks(IEnumerable<TrackEntrySection> tracks) {
			foreach(var track in tracks) {
				var trackNumber = (int)track.TrackNumber.Value;
				Tracks.Add(trackNumber, new Track(trackNumber, track.TrackTimecodeScale ?? 1d));
			}
		}

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.SimpleBlock.Id || elemInfo.DocElement.Id == MatroskaDocType.Block.Id) {
				MatroskaBlock matroskaBlock = (MatroskaBlock)reader.RetrieveValue(elemInfo);
				Track track;
				if(Tracks.TryGetValue(matroskaBlock.TrackNumber, out track)) {
					track.Timecodes.Add(new TrackTimecode((ulong)((matroskaBlock.TimeCode + timecode) * track.TimecodeScale * TimeCodeScale), matroskaBlock.FrameCount, matroskaBlock.DataLength));
				} else throw new Exception("Invalid track index (" + matroskaBlock.TrackNumber + ") in matroska block");

			} else if(elemInfo.DocElement.Id == MatroskaDocType.BlockGroup.Id) {
				Read(reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Timecode.Id) {
				timecode = (long)(ulong)reader.RetrieveValue(elemInfo);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() { yield break; }


		public class Track {
			public int TrackNumber { get; private set; }
			public double TimecodeScale { get; private set; }

			public List<TrackTimecode> Timecodes { get; private set; }

			public Track(int trackNumber, double timecodeScale) {
				TrackNumber = trackNumber;
				TimecodeScale = timecodeScale;

				Timecodes = new List<TrackTimecode>();
			}


			public TrackInfo TrackInfo { get { return (trackInfo == null) ? trackInfo = CalcTrackInfo() : trackInfo; } } private TrackInfo trackInfo;

			private TrackInfo CalcTrackInfo() {
				try {

					if(Timecodes.Count == 0) return new TrackInfo();

					Timecodes.Sort();

					var trackLength = TimeSpan.FromMilliseconds((Timecodes.Last().timeCode - Timecodes.First().timeCode) / 1000000);

					double[] rate = new double[3];
					double? minSampleRate = null, maxSampleRate = null;

					var oldTC = Timecodes.First();

					int pos = 0, prevPos = 0, prevprevPos;
					double maxDiff;

					int frames = oldTC.frames;
					long trackSize = oldTC.size;

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


					return new TrackInfo{
						SampleRateHistogram = sampleRateHistogram.Select(kvp => new SampleRateCountPair(kvp.Key, kvp.Value)).ToList().AsReadOnly(),
						AverageBitrate= (trackSize != 0 && trackLength.Ticks != 0) ? trackSize * 8 / trackLength.TotalSeconds : (double?)null,
						AverageSampleRate= (frames != 0 && trackLength.Ticks != 0) ? frames / trackLength.TotalSeconds : (double?)null,
						MinSampleRate = minSampleRate,
						MaxSampleRate = maxSampleRate,
						TrackLength= trackLength,
						TrackSize= trackSize,
						SampleCount= (int)frames
					};

				} catch(Exception) { }

				return null;
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
			public uint size;

			public TrackTimecode(ulong timeCode, byte frames, uint size) {
				this.frames = frames; this.size = size; this.timeCode = timeCode;
			}

			public int CompareTo(TrackTimecode other) {
				return timeCode.CompareTo(other.timeCode);
			}
		}
	}
}
