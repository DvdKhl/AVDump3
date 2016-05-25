using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace AVDump3Lib.Information.MetaInfo {
	public class MetaInfoContainer {
		private List<MetaInfoItem> items = new List<MetaInfoItem>();
		private List<MetaInfoContainer> nodes = new List<MetaInfoContainer>();

		public IReadOnlyList<MetaInfoItem> Items { get; }
		public IReadOnlyList<MetaInfoContainer> Nodes { get; }


		public MetaInfoContainer() {
			Items = items.AsReadOnly();
			Nodes = nodes.AsReadOnly();
		}

		public void Add<T>(MetaInfoItem<T> item) { items.Add(item); }
        public void Add(MetaInfoContainer node) { nodes.Add(node); }



		public IEnumerable<MetaInfoItem<T>> Select<T>(MetaInfoItemType<T> type) {
			return items.Where(i => i.Type == type).OfType<MetaInfoItem<T>>();
		}
		public MetaInfoItem<T> SelectFirst<T>(MetaInfoItemType<T> type) { return Select(type).FirstOrDefault(); }

	}
}
