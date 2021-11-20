using System;

namespace AVDump3Lib.Information.MetaInfo.Core;

public class MetaInfoItemType<T> : MetaInfoItemType {
	public MetaInfoItemType(string key, string unit) : base(key, unit, typeof(T)) { }
	public MetaInfoItemType(string key) : base(key, MetaInfoItemType.DimensionslessUnit, typeof(T)) { }
}

public class MetaInfoItemType {
	public string Key { get; private set; }
	public string Unit { get; private set; }
	public Type ValueType { get; private set; }

	public MetaInfoItemType(string key, string unit, Type valueType) {
		Key = key;
		Unit = unit;
		ValueType = valueType;
	}

	public static string DimensionslessUnit { get; } = "Dimensionsless";
}
