using System;
using System.Collections;
using System.Collections.Generic;
#if __WASM__
using System.Collections.Specialized;
using System.Linq;

#endif
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class InlineCollection : IList<Inline>, IEnumerable<Inline>
	{
#if __WASM__
		private readonly UIElementCollection _collection;
#else
		private readonly DependencyObjectCollection<Inline> _collection = new DependencyObjectCollection<Inline>();
#endif

#if __WASM__
		internal InlineCollection(UIElement containerElement)
		{
			_collection = new UIElementCollection(containerElement);
			_collection.CollectionChanged += OnCollectionChanged;
		}
#else
		internal InlineCollection(DependencyObject parent)
		{
			_collection.SetParent(parent);
			_collection.VectorChanged += (s, e) => OnCollectionChanged();
		}
#endif

		private void OnCollectionChanged(
#if __WASM__
			 object sender, NotifyCollectionChangedEventArgs e
#endif
			)
		{
#if !IS_UNIT_TESTS
			InvalidatePreorderTree();

#if __WASM__
			switch (_collection.Owner)
#else
			switch (_collection.GetParent())
#endif
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

		internal void InvalidatePreorderTree() => _preorderTree = null;

		/// <remarks>
		/// The PreorderTree invalidation logic is extremely buggy because the DP parent chain is flattened
		/// e.g. if you have a child Span, this InlineCollection will be the direct parent of these children
		/// when read using GetParent(). The returned value here is up to date only when this is the
		/// InlineCollection of a TextBlock.
		/// </remarks>
		private Inline[] PreorderTree
		{
			get
			{
				return _preorderTree ??= GetPreorderTree();

				Inline[] GetPreorderTree()
				{
					if (_collection.Count == 1 && _collection[0] is not Span)
					{
						return [_collection[0]];
					}
					else if (_collection.Count == 0)
					{
						return [];
					}
					else
					{
						var result = new List<Inline>(4);


#if __WASM__
						foreach (var current in _collection)
						{
							GetPreorderTreeInner((Inline)current, result);
						}
#else
						var enumerator = _collection.GetEnumeratorFast();

						while (enumerator.MoveNext())
						{
							GetPreorderTreeInner(enumerator.Current, result);
						}
#endif

						return result.ToArray();
					}

					static void GetPreorderTreeInner(Inline inline, List<Inline> accumulator)
					{
						accumulator.Add(inline);

						if (inline is Span span)
						{
#if __WASM__
							foreach (var current in span.Inlines._collection)
							{
								GetPreorderTreeInner((Inline)current, accumulator);
							}
#else
							var enumerator = span.Inlines.GetEnumeratorFast();

							while (enumerator.MoveNext())
							{
								GetPreorderTreeInner(enumerator.Current, accumulator);
							}
#endif
						}
					}
				}
			}
		}

		public IEnumerator<Inline> GetEnumerator() =>
#if __WASM__
			_collection.OfType<Inline>().GetEnumerator();
#else
			_collection.GetEnumerator();
#endif

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if !__WASM__
		private List<Inline>.Enumerator GetEnumeratorFast() => _collection.GetEnumeratorFast();
#endif

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
