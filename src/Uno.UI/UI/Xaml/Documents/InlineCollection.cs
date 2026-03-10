using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

#if __WASM__
using System.Collections.Specialized;
#endif

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
			InvalidateTraversedTree();

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
				case Block block:
					block.InvalidateInlines();
					break;
				default:
					break;
			}
#endif
		}

		private (Inline[] preorderTree, Inline[] leafTree)? _traversedTree;

		internal void InvalidateTraversedTree()
		{
			_traversedTree = null;
		}

		/// <remarks>
		/// The PreorderTree invalidation logic is extremely buggy because the DP parent chain is flattened
		/// e.g. if you have a child Span, this InlineCollection will be the direct parent of these children
		/// when read using GetParent(). The returned value here is up to date only when this is the
		/// InlineCollection of a TextBlock.
		/// </remarks>
		internal (Inline[] preorderTree, Inline[] leafTree) TraversedTree
		{
			get
			{
				if (_traversedTree is { } traversedTree)
				{
					return traversedTree;
				}
				var preOrderTree = GetPreorderTree();
				return (_traversedTree = (preOrderTree, preOrderTree.Where(inline => inline is Run or LineBreak).ToArray())).Value;

				Inline[] GetPreorderTree()
				{
					if (_collection.Count == 1 && _collection[0] is not Span)
					{
						return [(Inline)_collection[0]];
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
		public void Add(Inline item)
		{
			ValidateInline(item, nameof(item));
			_collection.Add(item);
		}

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
		public void Insert(int index, Inline item)
		{
			ValidateInline(item, nameof(item));
			_collection.Insert(index, item);
		}

		/// <inheritdoc />
		public void RemoveAt(int index) => _collection.RemoveAt(index);

		/// <inheritdoc />
		public Inline this[int index]
		{
			get => (Inline)_collection[index];
			set
			{
				ValidateInline(value, nameof(value));
				_collection[index] = value;
			}
		}

		/// <summary>
		/// WinUI only supports <see cref="InlineUIContainer"/> within a <see cref="RichTextBlock"/>; adding one to a
		/// <see cref="TextBlock"/> throws. We match that contract instead of silently dropping the element. See uno#23510.
		/// </summary>
		private void ValidateInline(Inline item, string paramName)
		{
			// Only an InlineUIContainer (or a Span that may nest one) can ever be invalid; a plain Run/LineBreak
			// is always fine, so it skips both the owner walk and the Span recursion.
			if (item is not (InlineUIContainer or Span))
			{
				return;
			}

			// Resolve ownership before recursing: a Paragraph/RichTextBlock-owned collection always allows the
			// container, so there is no need to scan Span content in rich-text scenarios.
			if (IsOwnedByTextBlock() && ContainsInlineUIContainer(item))
			{
				throw new ArgumentException(
					"InlineUIContainer is not supported in a TextBlock. It can only be used within a RichTextBlock.",
					paramName);
			}
		}

		private static bool ContainsInlineUIContainer(Inline item)
		{
			if (item is InlineUIContainer)
			{
				return true;
			}

			if (item is Span span)
			{
				foreach (var child in span.Inlines)
				{
					if (ContainsInlineUIContainer(child))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsOwnedByTextBlock()
		{
			var current =
#if __WASM__
				(object)_collection.Owner;
#else
				_collection.GetParent();
#endif

			// Walk up through Span/Hyperlink/etc. to find the owning control.
			while (current is Inline inline)
				current = inline.GetParent();

			return current is TextBlock;
		}
	}
}
