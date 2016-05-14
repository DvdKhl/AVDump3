using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace AVDump3Lib.Information.MetaInfo {
    public class MetaInfoContainer {
		public MetaInfoContainer(MetaInfoItemType type) {
            Type = type;

            MetaInfoItemTypes = Array.AsReadOnly(GetDocElements(GetType(), null).ToArray());
		}

        public MetaInfoItemType Type { get; private set; }


        private static IEnumerable<MetaInfoItemType> GetDocElements(Type type, Predicate<MetaInfoItemType> filter) {
			var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.FlattenHierarchy);
			foreach(var field in fields) {
				var obj = field.GetValue(null);
				if(obj is MetaInfoItemType && (filter == null || filter((MetaInfoItemType)obj))) yield return (MetaInfoItemType)obj;
			}
		}


		private List<MetaInfoItem> items = new List<MetaInfoItem>();
		public ReadOnlyCollection<MetaInfoItem> Items { get { return items.AsReadOnly(); } }

		private void Add(MetaInfoItem item) { items.Add(item); } //TODO: Check for Contains

		protected internal void Add(MetaInfoItemType type, object value, MetaDataProvider provider, params string[][] notes) {
			if(value == null || (value is string && string.IsNullOrWhiteSpace((string)value))) return;

			ReadOnlyCollection<KeyValuePair<string, string>> kvpNotes = null;

			try {
				kvpNotes = notes.Select(note => new KeyValuePair<string, string>(note[0], note[1])).ToList().AsReadOnly();
			} catch(Exception) { }

			Add(new MetaInfoItem(type, value, provider, kvpNotes));
		}
		//protected internal void Add(MetaInfoItemType type, Func<object> getValue, MetaDataProvider provider, params string[][] notes) {
		//	object value = null;
		//
		//	try { value = getValue(); } catch(Exception) { }
		//
		//	if(value == null) return;
		//	Add(type, value, provider, notes);
		//}


		public IEnumerable<MetaInfoItem> Select(MetaInfoItemType type) { return items.Where(i => i.Type == type); }
		public MetaInfoItem SelectFirst(MetaInfoItemType type) { return items.Where(i => i.Type == type).FirstOrDefault(); }

		public IEnumerable<MetaInfoItem> SelectWithSubClasses(MetaInfoItemType type) { return items.Where(i => i.Type.ValueType == type.ValueType || i.Type.ValueType.IsSubclassOf(type.ValueType)); }

		public IEnumerable<T> Values<T>() { return items.Select(v => v.Value).OfType<T>(); }

		//protected HashSet<MetaInfoItemType> types = new HashSet<MetaInfoItemType>();
		public ReadOnlyCollection<MetaInfoItemType> MetaInfoItemTypes { get; private set; }
	}
}
