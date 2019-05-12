using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
    public class EbmlList<T> : IList<T> {
		protected List<T> items;

		public EbmlList() : this(new T[0]) { }
		public EbmlList(T[] items) {
			this.items = new List<T>();
			foreach(var item in items) this.items.Add(item);
		}

		protected internal void Add(T item) { items.Add(item); }
		protected internal EbmlList<T> DeepClone(Func<T, T> cloneFunc) {
			EbmlList<T> clone = new EbmlList<T>();
			foreach(var item in items) clone.Add(cloneFunc(item));
			return clone;
		}

		public int Count { get { return items.Count; } }
		public IEnumerator<T> GetEnumerator() { return items.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return items.GetEnumerator(); }
		public bool Contains(T item) { return items.Contains(item); }
		public void CopyTo(T[] array, int arrayIndex) { items.CopyTo(array, arrayIndex); }
		public bool IsReadOnly { get { return true; } }
		public int IndexOf(T item) { return items.IndexOf(item); }
		public T this[int index] { get { return items[index]; } set { throw new NotSupportedException(); } }
		public void Insert(int index, T item) { throw new NotSupportedException(); }
		public bool Remove(T item) { throw new NotSupportedException(); }
		void ICollection<T>.Add(T item) { throw new NotSupportedException(); }
		public void Clear() { throw new NotSupportedException(); }
		public void RemoveAt(int index) { throw new NotSupportedException(); }
	}
}
