namespace AVDump3Lib.Information.MetaInfo.Media {
    public class Attachment : MetaInfoContainer {
		public static readonly MetaInfoItemType IdType = new MetaInfoItemType("Id", null, typeof(int), "");
		public static readonly MetaInfoItemType SizeType = new MetaInfoItemType("Size", "bytes", typeof(int), "");
		public static readonly MetaInfoItemType DescriptionType = new MetaInfoItemType("Description", "text", typeof(string), "");
		public static readonly MetaInfoItemType NameType = new MetaInfoItemType("Name", "text", typeof(string), "");
		public static readonly MetaInfoItemType TypeType = new MetaInfoItemType("Type", "text", typeof(string), "");

        public Attachment() : base(MediaProvider.AttachmentType) {}
	}
}
