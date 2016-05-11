using System.Collections.Generic;

namespace AVDump3Lib.Information.MetaInfo.Tools {
    public class HashProvider : MetaDataProvider {
        public HashProvider(IEnumerable<HashResult> hashResults) : base("HashProvider") {
            foreach(var hashResult in hashResults) Add(hashResult.Name, hashResult.Value, this);
        }

        public class HashResult {
            public MetaInfoItemType Name { get; private set; }
            public byte[] Value { get; private set; }
            public HashResult(MetaInfoItemType type, byte[] value) { Name = type; Value = value; }
        }

        public static readonly MetaInfoItemType CRC32Type = new MetaInfoItemType("CRC32", null, typeof(byte[]), "32bit Cyclic redundancy check - http://en.wikipedia.org/wiki/Cyclic_redundancy_check#CRC-32");
        public static readonly MetaInfoItemType ED2KType = new MetaInfoItemType("ED2K", null, typeof(byte[]), "ED2K Hash Algorithm - http://en.wikipedia.org/wiki/Ed2k_URI_scheme#eD2k_hash_algorithm");
        public static readonly MetaInfoItemType ED2KAltType = new MetaInfoItemType("ED2KAlt", null, typeof(byte[]), "ED2K Hash Algorithm Alternate - http://en.wikipedia.org/wiki/Ed2k_URI_scheme#eD2k_hash_algorithm");
        public static readonly MetaInfoItemType SHA1Type = new MetaInfoItemType("SHA1", null, typeof(byte[]), "Secure Hash Algorithm 1 - http://en.wikipedia.org/wiki/Secure_hash_algorithm");
        public static readonly MetaInfoItemType SHA256Type = new MetaInfoItemType("SHA256", null, typeof(byte[]), "Secure Hash Algorithm 256 - http://en.wikipedia.org/wiki/Secure_hash_algorithm");
        public static readonly MetaInfoItemType SHA384Type = new MetaInfoItemType("SHA384", null, typeof(byte[]), "Secure Hash Algorithm 384 - http://en.wikipedia.org/wiki/Secure_hash_algorithm");
        public static readonly MetaInfoItemType SHA512Type = new MetaInfoItemType("SHA512", null, typeof(byte[]), "Secure Hash Algorithm 512 - http://en.wikipedia.org/wiki/Secure_hash_algorithm");
        public static readonly MetaInfoItemType TigerType = new MetaInfoItemType("Tiger", null, typeof(byte[]), "Tiger hash algorithm - http://en.wikipedia.org/wiki/Tiger_(cryptography)");
        public static readonly MetaInfoItemType TTHType = new MetaInfoItemType("TTH", null, typeof(byte[]), "Tiger Tree Hash - http://en.wikipedia.org/wiki/Hash_tree#Tiger_tree_hash");
        public static readonly MetaInfoItemType AICHType = new MetaInfoItemType("AICH", null, typeof(byte[]), "Advanced Intelligent Corruption Handling - http://www.emule-project.net/home/perl/help.cgi?l=1&topic_id=589&rm=show_topic");
        public static readonly MetaInfoItemType MD4Type = new MetaInfoItemType("MD4", null, typeof(byte[]), "Message Digest 4 - http://en.wikipedia.org/wiki/MD4");
        public static readonly MetaInfoItemType MD5Type = new MetaInfoItemType("MD5", null, typeof(byte[]), "Message Digest 5 - http://en.wikipedia.org/wiki/MD5");
    }
}
