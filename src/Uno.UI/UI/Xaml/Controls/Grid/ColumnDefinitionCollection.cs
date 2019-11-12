using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls
{
	public partial class ColumnDefinitionCollection : IList<ColumnDefinition>, IEnumerable<ColumnDefinition>
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

		public void RemoveAt(int index) => _inner.RemoveAt(index);

		public ColumnDefinition this[int index]
		{
			get => _inner[index];
			set => _inner[index] = value;
		}

		public void Add(ColumnDefinition item) => _inner.Add(item);

		public void Clear() => _inner.Clear();

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

		public global::System.Collections.Generic.IEnumerator<ColumnDefinition> GetEnumerator() => _inner.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
	}
}
