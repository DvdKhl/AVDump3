using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Processing.BlockConsumers;
using System.Collections.Immutable;

namespace AVDump3Lib.Information.MetaInfo;

public class HashInfoItemType : MetaInfoItemType<ImmutableArray<byte>> { public HashInfoItemType(string key) : base(key) { } }

public class HashProvider : MetaDataProvider {
	public HashProvider(IEnumerable<HashCalculator> hashCalculators) : base("HashProvider", HashProviderType) {
		if(hashCalculators is null) throw new ArgumentNullException(nameof(hashCalculators));

		foreach(var hashCalculator in hashCalculators) {
			Add(new HashInfoItemType(hashCalculator.Name), hashCalculator.HashValue);
			for(var i = 0; i < hashCalculator.AdditionalHashValues.Length; i++) {
				var additionalHashValue = hashCalculator.AdditionalHashValues[i];
				Add(new HashInfoItemType(hashCalculator.Name + (i + 2)), additionalHashValue);
			}
		}
	}
	public static readonly MetaInfoContainerType HashProviderType = new("HashProvider");


	//public static readonly MetaInfoItemType CRC32Type = new MetaInfoItemType("CRC32", null, typeof(byte[]));
	//public static readonly MetaInfoItemType ED2KType = new MetaInfoItemType("ED2K", null, typeof(byte[]));
	//public static readonly MetaInfoItemType ED2KAltType = new MetaInfoItemType("ED2KAlt", null, typeof(byte[]));
	//public static readonly MetaInfoItemType SHA1Type = new MetaInfoItemType("SHA1", null, typeof(byte[]));
	//public static readonly MetaInfoItemType SHA256Type = new MetaInfoItemType("SHA256", null, typeof(byte[]));
	//public static readonly MetaInfoItemType SHA384Type = new MetaInfoItemType("SHA384", null, typeof(byte[]));
	//public static readonly MetaInfoItemType SHA512Type = new MetaInfoItemType("SHA512", null, typeof(byte[]));
	//public static readonly MetaInfoItemType TigerType = new MetaInfoItemType("Tiger", null, typeof(byte[]));
	//public static readonly MetaInfoItemType TTHType = new MetaInfoItemType("TTH", null, typeof(byte[]));
	//public static readonly MetaInfoItemType AICHType = new MetaInfoItemType("AICH", null, typeof(byte[]));
	//public static readonly MetaInfoItemType MD4Type = new MetaInfoItemType("MD4", null, typeof(byte[]));
	//public static readonly MetaInfoItemType MD5Type = new MetaInfoItemType("MD5", null, typeof(byte[]));
}
