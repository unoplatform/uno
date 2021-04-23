using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls
{
	public partial class ColumnDefinitionCollection : DefinitionCollectionBase, IList<ColumnDefinition>, IEnumerable<ColumnDefinition>
	{
		private readonly DependencyObjectCollection<ColumnDefinition> _inner = new DependencyObjectCollection<ColumnDefinition>();

		internal event VectorChangedEventHandler<ColumnDefinition> CollectionChanged;

		public ColumnDefinitionCollection()
		{
			_inner.VectorChanged += (s, e) => CollectionChanged?.Invoke(s, e);
		}

		internal ColumnDefinitionCollection(DependencyObject owner) : this()
		{
			_inner.IsAutoPropertyInheritanceEnabled = false;
			_inner.SetParent(owner);
		}

		public int IndexOf(ColumnDefinition item) => _inner.IndexOf(item);

		public void Insert(int index, ColumnDefinition item) => _inner.Insert(index, item);

		int IList<DefinitionBase>.IndexOf(DefinitionBase item) => _inner.IndexOf(item as ColumnDefinition);

		void IList<DefinitionBase>.Insert(int index, DefinitionBase item) => throw new NotSupportedException();

		public void RemoveAt(int index) => _inner.RemoveAt(index);

		DefinitionBase IList<DefinitionBase>.this[int index]
		{
			get => _inner[index];
			set => throw new NotSupportedException();
		}

		DefinitionBase DefinitionCollectionBase.GetItem(int index) => _inner[index];

		public ColumnDefinition this[int index]
		{
			get => _inner[index];
			set => _inner[index] = value;
		}

		public void Add(ColumnDefinition item) => _inner.Add(item);

		void ICollection<DefinitionBase>.Add(DefinitionBase item) => throw new NotSupportedException();

		public void Clear() => _inner.Clear();

		bool ICollection<DefinitionBase>.Contains(DefinitionBase item) => throw new NotSupportedException();

		void ICollection<DefinitionBase>.CopyTo(DefinitionBase[] array, int arrayIndex) => throw new NotSupportedException();

		bool ICollection<DefinitionBase>.Remove(DefinitionBase item) => throw new NotSupportedException();

		public bool Contains(ColumnDefinition item) => _inner.Contains(item);

		public void CopyTo(ColumnDefinition[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		public bool Remove(ColumnDefinition item) => _inner.Remove(item);

		public int Count => _inner.Count;

		public uint Size => (uint)_inner.Count;

		public bool IsReadOnly => false;

		/// <summary>
		/// The inner list is exposed in order to get the struct enumerable exposed by List<T> to avoid allocations.
		/// </summary>
		internal List<ColumnDefinition> InnerList => _inner.Items;

		IEnumerator<DefinitionBase> IEnumerable<DefinitionBase>.GetEnumerator() => _inner.GetEnumerator();

		public global::System.Collections.Generic.IEnumerator<ColumnDefinition> GetEnumerator() => _inner.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();

		void DefinitionCollectionBase.Lock() => _inner.Lock();

		void DefinitionCollectionBase.Unlock() => _inner.Unlock();
	}
}
