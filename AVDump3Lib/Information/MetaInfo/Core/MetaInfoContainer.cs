using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace AVDump3Lib.Information.MetaInfo {


    public class MetaInfoContainer {
        private class MetaInfoItemCollection : KeyedCollection<MetaInfoItemType, MetaInfoItem> {
            protected override MetaInfoItemType GetKeyForItem(MetaInfoItem item) => item.Type;
        }

        private MetaInfoItemCollection items = new MetaInfoItemCollection();
        private List<MetaInfoContainer> nodes = new List<MetaInfoContainer>();

        public IReadOnlyList<MetaInfoItem> Items { get; }
        public IReadOnlyList<MetaInfoContainer> Nodes { get; }
        public int Id { get; private set; }


        public MetaInfoContainer(int id) {
            Items = items; //TODO wrap in read only collection
            Nodes = nodes.AsReadOnly();
            Id = id;
        }

        public void Add<T>(MetaInfoItem<T> item) { items.Add(item); }
        public void AddNode(MetaInfoContainer node) { nodes.Add(node); }



        public IEnumerable<MetaInfoItem<T>> Select<T>(MetaInfoItemType<T> type) {
            return items.Where(i => i.Type == type).OfType<MetaInfoItem<T>>();
        }
        public MetaInfoItem<T> SelectFirst<T>(MetaInfoItemType<T> type) { return Select(type).FirstOrDefault(); }

    }
}
