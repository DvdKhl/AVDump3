using AVDump3Lib.Information.MetaInfo.Core;
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
        public MetaInfoContainerType Type { get; private set; }
        public int Id { get; private set; }


        public MetaInfoContainer(int id, MetaInfoContainerType type) {
            Items = items; //TODO wrap in read only collection
            Nodes = nodes.AsReadOnly();
            Type = type;
            Id = id;
        }

        public void Add<T>(MetaInfoItem<T> item) { items.Add(item); }
        public void AddNode(MetaInfoContainer node) { nodes.Add(node); }



        public MetaInfoItem<T> Select<T>(MetaInfoItemType<T> type) {
            return (MetaInfoItem<T>)items[type];
        }

    }
}
