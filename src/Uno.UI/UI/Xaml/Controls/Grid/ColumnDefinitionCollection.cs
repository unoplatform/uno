using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Uno.Extensions.Specialized;

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

		public void Insert(int index, ColumnDefinition item)
		{
			_inner.Insert(index, item);
			(item as DefinitionBase).Changed += OnDefinitionChanged;
		}

		public void RemoveAt(int index)
		{
			(_inner[index] as DefinitionBase).Changed -= OnDefinitionChanged;
			_inner.RemoveAt(index);
		}

		IEnumerable<DefinitionBase> DefinitionCollectionBase.GetItems() => _inner;

		DefinitionBase DefinitionCollectionBase.GetItem(int index) => _inner[index];

		public ColumnDefinition this[int index]
		{
			get => _inner[index];
			set
			{
				if (_inner[index] != value)
				{
					(_inner[index] as DefinitionBase).Changed -= OnDefinitionChanged;
					(value as DefinitionBase).Changed += OnDefinitionChanged;
					_inner[index] = value;
				}
			}
		}

		public void Add(ColumnDefinition item)
		{
			_inner.Add(item);
			(item as DefinitionBase).Changed += OnDefinitionChanged;
		}

		public void Clear()
		{
			_inner.ForEach(item => (item as DefinitionBase).Changed -= OnDefinitionChanged);
			_inner.Clear();
		}

		public bool Contains(ColumnDefinition item) => _inner.Contains(item);

		public void CopyTo(ColumnDefinition[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

		public bool Remove(ColumnDefinition item)
		{
			(item as DefinitionBase).Changed -= OnDefinitionChanged;
			return _inner.Remove(item);
		}

		public int Count => _inner.Count;

		public uint Size => (uint)_inner.Count;

		public bool IsReadOnly => false;

		/// <summary>
		/// The inner list is exposed in order to get the struct enumerable exposed by List<T> to avoid allocations.
		/// </summary>
		internal List<ColumnDefinition> InnerList => _inner.Items;

		public global::System.Collections.Generic.IEnumerator<ColumnDefinition> GetEnumerator() => _inner.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();

		void DefinitionCollectionBase.Lock() => _inner.Lock();

		void DefinitionCollectionBase.Unlock() => _inner.Unlock();

		private void OnDefinitionChanged(object sender, EventArgs e)
		{
			// The event is not important, since the listener will only react to the event itself, not the args
			CollectionChanged?.Invoke(_inner, new VectorChangedEventArgs(CollectionChange.ItemChanged, 0));
		}
	}
}
