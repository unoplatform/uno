// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextRangeCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

// This implemention was based heavily off of CPointCollection
internal partial class TextRangeCollection
{
	// CCollection overrides

	public uint Append(TextRange value)
	{
		_items.Add(value);

		uint index = (uint)_items.Count;

		OnAddToCollection(value);

		return index;
	}

	public void Insert(uint index, TextRange value)
	{
		// Redirect calls that would generate an append operation
		if (index >= _items.Count)
		{
			Append(value);
			return;
		}

		// Put the new object in its slot.
		_items.Insert((int)index, value);

		OnAddToCollection(value);
	}

	public TextRange? RemoveAt(uint index)
	{
		// Bail out quickly if the index is out of range
		if (index >= _items.Count)
		{
			return null;
		}

		var removedData = _items[(int)index];
		_items.RemoveAt((int)index);

		OnRemoveFromCollection(removedData, (int)index);

		return removedData;
	}

	public TextRange? GetItem(uint index)
	{
		// Bail out quickly if the index is out of range
		if (index >= _items.Count)
		{
			return null;
		}

		// Like the insertion operation the GetItem operation is best handled in the
		// flattened form.  We waste some memory for performance reasons.
		return _items[(int)index];
	}

	public int IndexOf(TextRange value)
	{
		var found = _items.IndexOf(value);

		if (found >= 0)
		{
			return found;
		}
		else
		{
			// this item is not in the collection
			throw new ArgumentException();
		}
	}

	public uint GetCount() => (uint)_items.Count;

	public void Neat() => _items.Clear();

	public IReadOnlyList<TextRange> GetCollection() => _items;

	protected void OnAddToCollection(TextRange value) => OnCollectionChanged();

	protected void OnRemoveFromCollection(TextRange value, int previousIndex) => OnCollectionChanged();

	protected void OnClear() => OnCollectionChanged();

	private void OnCollectionChanged()
	{
		var owner = Owner;

		if (owner is not null)
		{
			if (owner is TextHighlighter textHighlighterOwner)
			{
				// TODO Uno (Stage 9): route invalidation to the owner. The C++ owner
				// (CTextHighlighter) exposes InvalidateTextRanges(); the existing managed
				// TextHighlighter does not yet, and the live render path currently consumes
				// IList<TextHighlighter> on TextBlock/RichTextBlock rather than this collection.
				_ = textHighlighterOwner;
			}
			else
			{
				// CTextRangeCollection has unsupported owner.  If CTextRangeCollection is added
				// to new types in the future, it must be appropriate cast here to notify the parent type
				// of collection changes.
				MUX_ASSERT(false);
			}
		}
	}

	// CCollection::GetOwner() equivalent. Set by the owning TextHighlighter when this
	// collection is attached (NeedsOwnerInfo() == true in the C++ original).
	internal DependencyObject? Owner { get; set; }

	private readonly List<TextRange> _items = new();
}
