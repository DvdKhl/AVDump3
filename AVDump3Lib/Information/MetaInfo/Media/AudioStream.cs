namespace AVDump3Lib.Information.MetaInfo.Media {
    public class AudioStream : MediaStream {
        public static readonly MetaInfoItemType<int> ChannelCountType = new MetaInfoItemType<int>("ChannelCount", null);
        public static readonly MetaInfoItemType<int> BitDepthType = new MetaInfoItemType<int>("BitDepth", null);


        public AudioStream(int id) : base(id) {
        }
    }
}
