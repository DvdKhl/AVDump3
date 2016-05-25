using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump2Lib.InfoProvider.Tools {
    public class HashProvider : MediaProvider {
        public HashProvider(IEnumerable<HashResult> hashResults) : base("HashProvider") {
            foreach(var hashResult in hashResults) Add(hashResult.Name, hashResult.Value);
        }

        public class HashResult {
            public MetaInfoItemType<byte[]> Name { get; private set; }
            public byte[] Value { get; private set; }
            public HashResult(MetaInfoItemType<byte[]> type, byte[] value) { Name = type; Value = value; }
        }

        public static readonly MetaInfoItemType CRC32Type = new MetaInfoItemType("CRC32", null, typeof(byte[]));
        public static readonly MetaInfoItemType ED2KType = new MetaInfoItemType("ED2K", null, typeof(byte[]));
        public static readonly MetaInfoItemType ED2KAltType = new MetaInfoItemType("ED2KAlt", null, typeof(byte[]));
        public static readonly MetaInfoItemType SHA1Type = new MetaInfoItemType("SHA1", null, typeof(byte[]));
        public static readonly MetaInfoItemType SHA256Type = new MetaInfoItemType("SHA256", null, typeof(byte[]));
        public static readonly MetaInfoItemType SHA384Type = new MetaInfoItemType("SHA384", null, typeof(byte[]));
        public static readonly MetaInfoItemType SHA512Type = new MetaInfoItemType("SHA512", null, typeof(byte[]));
        public static readonly MetaInfoItemType TigerType = new MetaInfoItemType("Tiger", null, typeof(byte[]));
        public static readonly MetaInfoItemType TTHType = new MetaInfoItemType("TTH", null, typeof(byte[]));
        public static readonly MetaInfoItemType AICHType = new MetaInfoItemType("AICH", null, typeof(byte[]));
        public static readonly MetaInfoItemType MD4Type = new MetaInfoItemType("MD4", null, typeof(byte[]));
        public static readonly MetaInfoItemType MD5Type = new MetaInfoItemType("MD5", null, typeof(byte[]));
    }
}
