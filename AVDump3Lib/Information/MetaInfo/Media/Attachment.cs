namespace AVDump3Lib.Information.MetaInfo.Media {
    public class Attachment : MetaInfoContainer {
		public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
		public static readonly MetaInfoItemType<int> SizeType = new MetaInfoItemType<int>("Size", "bytes");
		public static readonly MetaInfoItemType<string> DescriptionType = new MetaInfoItemType<string>("Description", "text");
		public static readonly MetaInfoItemType<string> NameType = new MetaInfoItemType<string>("Name", "text");
		public static readonly MetaInfoItemType<string> TypeType = new MetaInfoItemType<string>("Type", "text");

        public Attachment() {}
	}
}
