namespace AVDump3Lib.Information.MetaInfo.Media {
    public class AudioStream : MediaStream {
        public static readonly MetaInfoItemType ChannelCountType = new MetaInfoItemType("ChannelCount", null, typeof(int), "");
        public static readonly MetaInfoItemType BitDepthType = new MetaInfoItemType("BitDepth", null, typeof(int), "");

        public AudioStream() : base(MediaProvider.AudioStreamType) { }
    }
}
