#if !__WASM__
using System;
using System.Collections;
using System.Collections.Generic;
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
#if !IS_UNIT_TESTS
			_preorderTree = null;

			switch (_collection.GetParent())
			{
				case TextBlock textBlock:
					textBlock.InvalidateInlines(true);
					break;
				case Inline inline:
					inline.InvalidateInlines(true);
					break;
				default:
					break;
			}
#endif
		}

		private Inline[] _preorderTree;

		internal Inline[] PreorderTree => _preorderTree ??= GetPreorderTree();

		private Inline[] GetPreorderTree()
		{
			if (_collection.Count == 1 && _collection[0] is not Span)
			{
				return new Inline[] { _collection[0] };
			}
			else if (_collection.Count == 0)
			{
				return Array.Empty<Inline>();
			}
			else
			{
				var result = new List<Inline>(4);

				var enumerator = _collection.GetEnumeratorFast();

				while (enumerator.MoveNext())
				{
					GetPreorderTreeInner(enumerator.Current, result);
				}

				return result.ToArray();
			}

			static void GetPreorderTreeInner(Inline inline, List<Inline> accumulator)
			{
				accumulator.Add(inline);

				if (inline is Span span)
				{
					var enumerator = span.Inlines.GetEnumeratorFast();

					while (enumerator.MoveNext())
					{
						GetPreorderTreeInner(enumerator.Current, accumulator);
					}
				}
			}
		}

		public IEnumerator<Inline> GetEnumerator() => _collection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		internal List<Inline>.Enumerator GetEnumeratorFast() => _collection.GetEnumeratorFast();

		/// <inheritdoc />
		public void Add(Inline item) => _collection.Add(item);

		/// <inheritdoc />
		public void Clear() => _collection.Clear();

		/// <inheritdoc />
		public bool Contains(Inline item) => _collection.Contains(item);

		/// <inheritdoc />
		public void CopyTo(Inline[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

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
