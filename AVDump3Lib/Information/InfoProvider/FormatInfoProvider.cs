using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;

namespace AVDump3Lib.Information.InfoProvider {
	public class FormatInfoProvider : MetaDataProvider {
		public FormatInfoProvider(string filePath) : base("FormatInfoProvider", FormatInfoProviderType) {
		}
		public static readonly MetaInfoContainerType FormatInfoProviderType = new MetaInfoContainerType("FormatInfoProvider");

	}

	public interface IMagicBytePattern {

	}
	public interface IFormatPattern {

	}

}
