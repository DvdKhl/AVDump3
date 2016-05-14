using System;

namespace AVDump3Lib.Information.MetaInfo {
    public class MetaInfoItemType {
		public string Key { get; private set; }
		public string Unit { get; private set; }
		public Type ValueType { get; private set; }
		public string Description { get; private set; }

		public MetaInfoItemType(string key, string unit, Type valueType, string description) {
			Key = key; Unit = unit; ValueType = valueType; Description = description;
		}
	}
}
