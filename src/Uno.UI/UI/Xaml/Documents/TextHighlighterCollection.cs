// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextHighlighterCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

internal partial class TextHighlighterCollection
{
	public void OnCollectionChanged()
	{
		var owner = Owner;

		if (owner is not null)
		{
			if (owner is TextBlock textBlockOwner)
			{
				// TODO Uno (Stage 9): textBlockOwner->InvalidateRender(). The live render path
				// currently consumes IList<TextHighlighter> directly on TextBlock rather than
				// this collection, so invalidation is not yet routed here.
				_ = textBlockOwner;
			}
			else if (owner is RichTextBlock richTextBlockOwner)
			{
				// TODO Uno (Stage 9): richTextBlockOwner->InvalidateRender().
				_ = richTextBlockOwner;
			}
			else
			{
				// TextHighlighterCollection has unsupported owner.  If TextHighlighterCollection is added
				// to new types in the future, it must be appropriate cast here to notify the parent type
				// of collection changes.
				MUX_ASSERT(false);
			}
		}
	}

	protected void OnAddToCollection(TextHighlighter dependencyObject) => OnCollectionChanged();

	protected void OnRemoveFromCollection(TextHighlighter dependencyObject, int previousIndex) => OnCollectionChanged();

	protected void OnClear() => OnCollectionChanged();

	// CDOCollection::GetOwner() equivalent. Set by the owning element (TextBlock /
	// RichTextBlock) when this collection is attached (NeedsOwnerInfo() == true).
	internal DependencyObject? Owner { get; set; }

	public IReadOnlyList<TextHighlighter> GetCollection() => _items;

	public uint GetCount() => (uint)_items.Count;

	private readonly List<TextHighlighter> _items = new();
}
