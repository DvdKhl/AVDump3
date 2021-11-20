using ExtKnot.StringInvariants;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Core;

public class MetaInfoContainer {
	private class MetaInfoItemCollection : KeyedCollection<MetaInfoItemType, MetaInfoItem> {
		protected override MetaInfoItemType GetKeyForItem(MetaInfoItem item) => item.Type;
	}

	private readonly MetaInfoItemCollection items = new();
	private readonly List<MetaInfoContainer> nodes = new();

	public IReadOnlyList<MetaInfoItem> Items { get; }
	public IReadOnlyList<MetaInfoContainer> Nodes { get; }
	public MetaInfoContainerType Type { get; private set; }
	public ulong Id { get; private set; }


	public MetaInfoContainer(ulong id, MetaInfoContainerType type) {
		Items = items; //TODO wrap in read only collection
		Nodes = nodes.AsReadOnly();
		Type = type;
		Id = id;
	}

	public bool Add(MetaInfoItem item) {
		if(items.Contains(item.Type)) {
			return false;
		}
		items.Add(item);
		return true;
	}
	public bool Add<T>(MetaInfoItem<T> item) { return Add((MetaInfoItem)item); }
	public void AddNode(MetaInfoContainer node) { nodes.Add(node); }



	public MetaInfoItem<T>? Select<T>(MetaInfoItemType<T> type) {
		return items.Contains(type) ? (MetaInfoItem<T>)items[type] : null;
	}
	public MetaInfoItem<TValue> Select<TType, TValue>(string typeName) where TType : MetaInfoItemType<TValue> {
		return items.OfType<MetaInfoItem<TValue>>().FirstOrDefault(x => x.Type.Key.InvEquals(typeName));
	}

}
