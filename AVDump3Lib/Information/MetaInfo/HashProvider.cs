using AVDump3Lib.Information.MetaInfo.Core;
using System.Collections.Generic;

namespace AVDump3Lib.Information.MetaInfo.Tools {
    public class HashProvider : MetaDataProvider {
        public HashProvider(IEnumerable<HashResult> hashResults) : base("HashProvider", HashProviderType) {
        }
		public static readonly MetaInfoContainerType HashProviderType = new MetaInfoContainerType("HashProvider");

		public class HashResult {
            public MetaInfoItemType Name { get; private set; }
            public byte[] Value { get; private set; }
            public HashResult(MetaInfoItemType type, byte[] value) { Name = type; Value = value; }
        }

        public static readonly MetaInfoItemType<byte[]> CRC32Type = new MetaInfoItemType<byte[]>("CRC32", null);
        public static readonly MetaInfoItemType<byte[]> ED2KType = new MetaInfoItemType<byte[]>("ED2K", null);
        public static readonly MetaInfoItemType<byte[]> ED2KAltType = new MetaInfoItemType<byte[]>("ED2KAlt", null);
        public static readonly MetaInfoItemType<byte[]> SHA1Type = new MetaInfoItemType<byte[]>("SHA1", null);
        public static readonly MetaInfoItemType<byte[]> SHA256Type = new MetaInfoItemType<byte[]>("SHA256", null);
        public static readonly MetaInfoItemType<byte[]> SHA384Type = new MetaInfoItemType<byte[]>("SHA384", null);
        public static readonly MetaInfoItemType<byte[]> SHA512Type = new MetaInfoItemType<byte[]>("SHA512", null);
        public static readonly MetaInfoItemType<byte[]> TigerType = new MetaInfoItemType<byte[]>("Tiger", null);
        public static readonly MetaInfoItemType<byte[]> TTHType = new MetaInfoItemType<byte[]>("TTH", null);
        public static readonly MetaInfoItemType<byte[]> AICHType = new MetaInfoItemType<byte[]>("AICH", null);
        public static readonly MetaInfoItemType<byte[]> MD4Type = new MetaInfoItemType<byte[]>("MD4", null);
        public static readonly MetaInfoItemType<byte[]> MD5Type = new MetaInfoItemType<byte[]>("MD5", null);
    }
}
