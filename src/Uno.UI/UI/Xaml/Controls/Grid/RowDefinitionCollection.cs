using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls
{
	public partial class RowDefinitionCollection : DefinitionCollectionBase, IList<RowDefinition>, IEnumerable<RowDefinition>
	{
		private readonly DependencyObjectCollection<RowDefinition> _inner = new DependencyObjectCollection<RowDefinition>();

		internal event VectorChangedEventHandler<RowDefinition> CollectionChanged;

		public RowDefinitionCollection()
		{
			_inner.VectorChanged += (s, e) => CollectionChanged?.Invoke(s, e);
		}

		internal RowDefinitionCollection(DependencyObject owner) : this()
		{
			_inner.IsAutoPropertyInheritanceEnabled = false;
			_inner.SetParent(owner);
		}

		public int IndexOf(RowDefinition item) => _inner.IndexOf(item);

		public void Insert(int index, RowDefinition item) => _inner.Insert(index, item);

		int IList<DefinitionBase>.IndexOf(DefinitionBase item) => _inner.IndexOf(item as RowDefinition);

		void IList<DefinitionBase>.Insert(int index, DefinitionBase item) => throw new NotSupportedException();

		public void RemoveAt(int index) => _inner.RemoveAt(index);

		DefinitionBase IList<DefinitionBase>.this[int index]
		{
			get => _inner[index];
			set => throw new NotSupportedException();
		}

		DefinitionBase DefinitionCollectionBase.GetItemImpl(int index) => _inner[index];

		public RowDefinition this[int index]
		{
			get => _inner[index];
			set => _inner[index] = value;
		}

		public void Add(RowDefinition item) => _inner.Add(item);

		void ICollection<DefinitionBase>.Add(DefinitionBase item) => throw new NotSupportedException();

		public void Clear() => _inner.Clear();

		bool ICollection<DefinitionBase>.Contains(DefinitionBase item) => throw new NotSupportedException();

		void ICollection<DefinitionBase>.CopyTo(DefinitionBase[] array, int arrayIndex) => throw new NotSupportedException();

		bool ICollection<DefinitionBase>.Remove(DefinitionBase item) => throw new NotSupportedException();

		public bool Contains(RowDefinition item) => _inner.Contains(item);

		public void CopyTo(RowDefinition[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		public bool Remove(RowDefinition item) => _inner.Remove(item);

		public int Count => _inner.Count;

		public bool IsReadOnly => false;

		/// <summary>
		/// The inner list is exposed in order to get the struct enumerable exposed by List<T> to avoid allocations.
		/// </summary>
		internal List<RowDefinition> InnerList => _inner.Items;

		IEnumerator<DefinitionBase> IEnumerable<DefinitionBase>.GetEnumerator() => _inner.GetEnumerator();

		public global::System.Collections.Generic.IEnumerator<RowDefinition> GetEnumerator() => _inner.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();

		void DefinitionCollectionBase.Lock() => _inner.Lock();

		void DefinitionCollectionBase.Unlock() => _inner.Unlock();
	}
}
