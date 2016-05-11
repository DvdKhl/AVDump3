using System.Globalization;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class MediaStream : MetaInfoContainer {
		public static readonly MetaInfoItemType IdType = new MetaInfoItemType("Id", null, typeof(long), "");
		public static readonly MetaInfoItemType IndexType = new MetaInfoItemType("Index", null, typeof(int), "");
		public static readonly MetaInfoItemType IsEnabledType = new MetaInfoItemType("IsEnabled", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsDefaultType = new MetaInfoItemType("IsDefault", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsForcedType = new MetaInfoItemType("IsForced", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsOverlayType = new MetaInfoItemType("IsOverlay", null, typeof(bool), "");
		public static readonly MetaInfoItemType TitleType = new MetaInfoItemType("Title", null, typeof(string), "");
		public static readonly MetaInfoItemType LanguageType = new MetaInfoItemType("Language", null, typeof(string), "");
		public static readonly MetaInfoItemType CodecIdType = new MetaInfoItemType("CodecId", null, typeof(string), "");
		public static readonly MetaInfoItemType CodecNameType = new MetaInfoItemType("CodecName", null, typeof(string), "");
		public static readonly MetaInfoItemType ColorDepth = new MetaInfoItemType("ColorDepth", null, typeof(int), "");

		public static readonly MetaInfoItemType CueCountType = new MetaInfoItemType("CueCount", null, typeof(int), "");

		public static readonly MetaInfoItemType SizeType = new MetaInfoItemType("Size", "bytes", typeof(int), "");
		public static readonly MetaInfoItemType DurationType = new MetaInfoItemType("Duration", "s", typeof(int), "");
		public static readonly MetaInfoItemType BitrateType = new MetaInfoItemType("Bitrate", "bit/s", typeof(int), "");
		public static readonly MetaInfoItemType StatedBitrateModeType = new MetaInfoItemType("StatedBitrateMode", null, typeof(string), "");

		public static readonly MetaInfoItemType EncoderNameType = new MetaInfoItemType("EncoderName", null, typeof(string), "");
		public static readonly MetaInfoItemType EncoderSettingsType = new MetaInfoItemType("EncoderSettings", null, typeof(string), "");


		public static readonly MetaInfoItemType SampleCountType = new MetaInfoItemType("SampleCount", null, typeof(long), "");
		public static readonly MetaInfoItemType StatedSampleRateType = new MetaInfoItemType("StatedSampleRate", null, typeof(double), "");
		public static readonly MetaInfoItemType MaxSampleRateType = new MetaInfoItemType("MaxSampleRate", null, typeof(double), "");
		public static readonly MetaInfoItemType MinSampleRateType = new MetaInfoItemType("MinSampleRate", null, typeof(double), "");
		public static readonly MetaInfoItemType AverageSampleRateType = new MetaInfoItemType("AverageSampleRate", null, typeof(double), "");
		public static readonly MetaInfoItemType DominantSampleRateType = new MetaInfoItemType("DominantSampleRate", null, typeof(double), "");
		public static readonly MetaInfoItemType SampleRateHistogramType = new MetaInfoItemType("SampleRateHistogram", null, typeof(SampleRateHistogram), "");
		public static readonly MetaInfoItemType SampleRateVarianceType = new MetaInfoItemType("SampleRateVariance", null, typeof(double), "");

		public static readonly MetaInfoItemType OutputSampleRateType = new MetaInfoItemType("OutputSampleRate", null, typeof(double), "");

        public MediaStream(MetaInfoItemType type) :base(type) {}
	}

	public class SampleRateHistogram : MetaInfoContainer {
        public SampleRateHistogram() : base(MediaStream.SampleRateHistogramType) { }

		public static readonly MetaInfoItemType FrameRateCountPairType = new MetaInfoItemType("SampleRateCountPair", null, typeof(SampleRateCountPair), "");

	}
	public class SampleRateCountPair {
		public double Rate { get; private set; }
		public long Count { get; private set; }

		public SampleRateCountPair(double rate, long count) { Rate = rate; Count = count; }
		public override string ToString() { return string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Rate, Count); }
	}

}
