using System;
using System.Collections.Generic;
using System.Globalization;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class MediaStream : MetaInfoContainer {
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

        public MediaStream(int id) : base(id) {
        }
    }

	public class SampleRateCountPair {
		public double Rate { get; private set; }
		public long Count { get; private set; }

		public SampleRateCountPair(double rate, long count) { Rate = rate; Count = count; }
		public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Rate, Count); }
	}

}
