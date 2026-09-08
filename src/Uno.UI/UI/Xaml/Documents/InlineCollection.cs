using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Documents
{
	public partial class InlineCollection : IList<Inline>, IEnumerable<Inline>
	{
		private readonly DependencyObjectCollection<Inline> _collection = new DependencyObjectCollection<Inline>();
		private int _updateDepth;
		private bool _hasPendingCollectionChange;
		private string _knownTextAfterUpdate;
		private IReadOnlyList<Inline> _removedAfterUpdate;
		private IReadOnlyList<Inline> _insertedAfterUpdate;

		internal InlineCollection(DependencyObject parent)
		{
			_collection.SetParent(parent);
			_collection.VectorChanged += (s, e) => OnCollectionChanged();
		}

		private void OnCollectionChanged(
			)
		{
			if (_updateDepth > 0)
			{
				_hasPendingCollectionChange = true;
				return;
			}

			NotifyCollectionChanged(knownText: null, removed: null, inserted: null);
		}

		private void NotifyCollectionChanged(
			string knownText,
			IReadOnlyList<Inline> removed,
			IReadOnlyList<Inline> inserted,
			bool updateText = true)
		{
			InvalidateTraversedTree();

			switch (_collection.GetParent())
			{
				case TextBlock textBlock:
					if (!updateText)
					{
						textBlock.InvalidateInlinesWithoutTextUpdate(removed, inserted);
					}
					else if (knownText is null)
					{
						textBlock.InvalidateInlines(true);
					}
					else
					{
						textBlock.InvalidateInlines(knownText, removed, inserted);
					}
					break;
				case Inline inline:
					inline.InvalidateInlines(true);
					break;
				default:
					break;
			}
		}

		internal void ReplaceRange(
			int index,
			int count,
			IReadOnlyList<Inline> replacement,
			string knownText,
			bool updateText)
		{
			ArgumentNullException.ThrowIfNull(replacement);
			if (updateText)
			{
				ArgumentNullException.ThrowIfNull(knownText);
			}
			if ((uint)index > (uint)_collection.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			if (count < 0 || count > _collection.Count - index)
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			_updateDepth++;
			_knownTextAfterUpdate = knownText;
			var removed = new Inline[count];
			for (var i = 0; i < count; i++)
			{
				removed[i] = (Inline)_collection[index + i];
			}
			_removedAfterUpdate = removed;
			_insertedAfterUpdate = replacement;
			var completed = false;
			try
			{
#if __WASM__
				for (var i = 0; i < count; i++)
				{
					_collection.RemoveAt(index);
				}
				for (var i = 0; i < replacement.Count; i++)
				{
					_collection.Insert(index + i, replacement[i]);
				}
#else
				_collection.ReplaceRange(index, count, replacement);
#endif
				completed = true;
			}
			finally
			{
				_updateDepth--;
				if (_updateDepth == 0 && _hasPendingCollectionChange)
				{
					_hasPendingCollectionChange = false;
					var text = completed ? _knownTextAfterUpdate : null;
					var removedItems = completed ? _removedAfterUpdate : null;
					var insertedItems = completed ? _insertedAfterUpdate : null;
					_knownTextAfterUpdate = null;
					_removedAfterUpdate = null;
					_insertedAfterUpdate = null;
					NotifyCollectionChanged(text, removedItems, insertedItems, updateText);
				}
				else if (_updateDepth == 0)
				{
					_knownTextAfterUpdate = null;
					_removedAfterUpdate = null;
					_insertedAfterUpdate = null;
				}
			}
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
			}
		}

		public IEnumerator<Inline> GetEnumerator() =>
			_collection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private List<Inline>.Enumerator GetEnumeratorFast() => _collection.GetEnumeratorFast();

		/// <inheritdoc />
		public void Add(Inline item)
		{
			ValidateInline(item, nameof(item));
			_collection.Add(item);
		}

		/// <inheritdoc />
		public void Clear()
		{
			foreach (var inline in _collection)
			{
				if (ClearFocusForRemovedInline(inline))
				{
					break;
				}
			}

			_collection.Clear();
		}

		/// <inheritdoc />
		public bool Contains(Inline item) => _collection.Contains(item);

		/// <inheritdoc />
		public void CopyTo(Inline[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public bool Remove(Inline item)
		{
			var index = _collection.IndexOf(item);
			if (index < 0)
			{
				return false;
			}

			RemoveAt(index);
			return true;
		}

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
		public void RemoveAt(int index)
		{
			ClearFocusForRemovedInline(_collection[index]);
			_collection.RemoveAt(index);
		}

		/// <inheritdoc />
		public Inline this[int index]
		{
			get => (Inline)_collection[index];
			set
			{
				ValidateInline(value, nameof(value));
				if (!ReferenceEquals(_collection[index], value))
				{
					ClearFocusForRemovedInline(_collection[index]);
				}

				_collection[index] = value;
			}
		}

		private static bool ClearFocusForRemovedInline(Inline inline)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(inline, VisualTree.LookupOptions.NoFallback);
			if (focusManager?.FocusedElement is Inline focusedInline &&
				inline.Enumerate().Any(candidate => ReferenceEquals(candidate, focusedInline)))
			{
				// Inline collections do not run a live Leave walk when an item is removed.
				focusManager.ClearFocus(canCancel: false);
				return true;
			}

			return false;
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
			var current = _collection.GetParent();

			// Walk up through Span/Hyperlink/etc. to find the owning control.
			while (current is Inline inline)
				current = inline.GetParent();

			return current is TextBlock;
		}
	}
}
