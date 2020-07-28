using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	partial class InlineCollection : IList<Inline>, IEnumerable<Inline>
	{
		private readonly UIElementCollection _collection;

		internal InlineCollection(UIElement containerElement)
		{
			_collection = new UIElementCollection(containerElement);
		}

		/// <inheritdoc />
		public IEnumerator<Inline> GetEnumerator() => _collection.OfType<Inline>().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <inheritdoc />
		public void Add(Inline item) => _collection.Add(item);

		/// <inheritdoc />
		public void Clear() => _collection.Clear();

		/// <inheritdoc />
		public bool Contains(Inline item) => _collection.Contains(item);

		/// <inheritdoc />
		public void CopyTo(Inline[] array, int arrayIndex) => throw new NotSupportedException();

		/// <inheritdoc />
		public bool Remove(Inline item) => _collection.Remove(item);

		/// <inheritdoc />
		public int Count => _collection.Count;

		/// <inheritdoc />
		public bool IsReadOnly => false;

		/// <inheritdoc />
		public int IndexOf(Inline item) => _collection.IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, Inline item) => _collection.Insert(index, item);

		/// <inheritdoc />
		public void RemoveAt(int index) => _collection.RemoveAt(index);

		/// <inheritdoc />
		public Inline this[int index]
		{
			get => (Inline)_collection[index];
			set => _collection[index] = value;
		}
	}
}
