using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AVDump3Lib.Information.MetaInfo {
    public class MetaInfoItem {
		public MetaInfoItemType Type { get; private set; }

		public ReadOnlyCollection<KeyValuePair<string, string>> Notes { get; private set; }

		public object Value { get; private set; }
		public MetaDataProvider Provider { get; private set; }

		protected MetaInfoItem() { }

		public MetaInfoItem(MetaInfoItemType type, object value, MetaDataProvider provider, ReadOnlyCollection<KeyValuePair<string, string>> notes) {
			Type = type; Value = value; Provider = provider; Notes = notes;
		}

		public override string ToString() {
			return string.Format("MetaInfoItem(Key={0}, Value={1}, Provider={2})", Type.Key, Value, Provider);
		}
	}

}
