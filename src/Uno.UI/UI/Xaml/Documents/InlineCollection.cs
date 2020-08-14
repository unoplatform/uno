#if !__WASM__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Documents
{
	public partial class InlineCollection : IList<Inline>, IEnumerable<Inline>
	{
		private readonly DependencyObjectCollection<Inline> _collection = new DependencyObjectCollection<Inline>();

		internal InlineCollection(DependencyObject parent)
		{
			_collection.SetParent(parent);
			_collection.VectorChanged += (s, e) => OnCollectionChanged();
		}

		private void OnCollectionChanged()
		{
#if !NET461
			switch (_collection.GetParent())
			{
				case TextBlock textBlock:
					textBlock.InvalidateInlines();
					break;
				case Inline inline:
					inline.InvalidateInlines();
					break;
				default:
					break;
			}
#endif
		}

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
#endif
