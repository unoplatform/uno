// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextElementCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents;

// Base collection behavior for the text element model (CTextElementCollection). In WinUI this is a
// CDOCollection subclass that BlockCollection/InlineCollection derive from; it dirties the owning
// text control whenever the collection mutates and validates incoming elements through TextSchema.
//
// Uno's BlockCollection/InlineCollection are built on DependencyObjectCollection<T> and already
// route their own change notifications (see InlineCollection.OnCollectionChanged), so they don't
// derive from this base. These helpers are the faithful port of CTextElementCollection's logic the
// element model relies on; they operate on the owning object so the same dirtying/validation can be
// applied wherever a ported caller needs it.
internal static class TextElementCollection
{
	//------------------------------------------------------------------------
	//
	//  CTextElementCollection::MarkDirty
	//
	//  Called when the content of the collection is changed. Notifies the
	//  collection's parent that the inline has changed.
	//
	//------------------------------------------------------------------------
	internal static void MarkDirty(object? collection)
	{
		var parent = collection?.GetParent();

		switch (parent)
		{
			case TextBlock textBlock:
				textBlock.InvalidateInlines(true);
				break;
			case RichTextBlock richTextBlock:
				richTextBlock.InvalidateBlockContent();
				break;
			case Inline inline:
				inline.InvalidateInlines(true);
				break;
			case Block block:
				block.InvalidateInlines();
				break;
			case BlockCollection blockCollection:
				MarkDirty(blockCollection);
				break;
		}
	}

	//------------------------------------------------------------------------
	//
	//  CTextElementCollection::ValidateTextElement
	//
	//  Validate a new TextElement that wants to enter this collection. WinUI's
	//  base implementation accepts everything (always S_OK); element-tree
	//  constraints are enforced by TextSchema at the inline-collection level.
	//
	//------------------------------------------------------------------------
	internal static bool ValidateTextElement(TextElement textElement) => true;
}
