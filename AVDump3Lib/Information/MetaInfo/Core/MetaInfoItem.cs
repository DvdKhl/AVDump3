using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AVDump3Lib.Information.MetaInfo.Core {
	public class MetaInfoItem<T> : MetaInfoItem {
		public new T Value { get; }
		public MetaInfoItem(MetaInfoItemType type, T value, MetaDataProvider provider, IReadOnlyDictionary<string, string> notes)
			: base(type, provider, notes) {
			Value = value;
		}
		public override string ToString() {
			return string.Format("MetaInfoItem(Key={0}, Value={1}, Provider={2})", Type.Key, Value, Provider);
		}

		protected override object GetValue() { return Value; }
	}

	public abstract class MetaInfoItem {
		public MetaInfoItemType Type { get; private set; }
		public object Value { get { return GetValue(); } }
		public IReadOnlyDictionary<string, string> Notes { get; private set; }
		public MetaDataProvider Provider { get; private set; }

		//public T Select<T>() => (T)GetValue();

		public TValue Select<TValue>(MetaInfoItemType<TValue>? type) { return (TValue)Value; }


		protected abstract object GetValue();
		protected MetaInfoItem(MetaInfoItemType type, MetaDataProvider provider, IReadOnlyDictionary<string, string> notes) {
			Type = type;
			Provider = provider;
			Notes = notes;
		}
	}

}
