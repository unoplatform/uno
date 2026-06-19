// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSchema.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

// Schema validation for the text element model. Decides which TextElements may enter a given
// content tree (TextBlock vs Hyperlink) — see CTextSchema. WinUI consults this from the inline
// collection's ValidateTextElement before an element is added.
internal static class TextSchema
{
	internal static bool IsInlineUIContainer(TextElement element) => element is InlineUIContainer;

	internal static bool IsHyperlink(TextElement element) => element is Hyperlink;

	internal static bool IsRun(TextElement element) => element is Run;

	internal static bool IsSpan(TextElement element) => element is Span;

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether a given TextElement can be part of a TextBlock's content tree.
	//
	//  Remarks:
	//      InlineUIContainers and Hyperlinks are not supported in TextBlocks.
	//
	//      If the given element is a span, we walk its content (recursively, if necessary)
	//      to ensure that it doesn't contain any unsupported elements.
	//------------------------------------------------------------------------
	internal static bool TextBlockSupportsElement(TextElement textElement)
	{
		// Trivial case: hyperlinks or inline UI containers.
		if (IsInlineUIContainer(textElement))
		{
			return false;
		}

		// Spans can contain unsupported elements, so we have to walk their inlines.
		if (IsSpan(textElement))
		{
			var span = (Span)textElement;
			var inlines = span.Inlines;

			var numInlines = inlines.Count;
			for (var i = 0; i < numInlines; ++i)
			{
				if (!TextBlockSupportsElement(inlines[i]))
				{
					return false;
				}
			}
		}

		// If we've come this far, we've encountered no unsupported elements.
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether a given TextElement can be part of a Hyperlink's content tree.
	//
	//  Remarks:
	//      Only runs and non-Hyperlink spans are supported in Hyperlinks.
	//
	//      If the given element is a span, we walk its content (recursively, if necessary)
	//      to ensure that it doesn't contain any unsupported elements.
	//------------------------------------------------------------------------
	internal static bool HyperlinkSupportsElement(TextElement textElement)
	{
		if (IsRun(textElement))
		{
			return true;
		}
		else if (IsSpan(textElement) && !IsHyperlink(textElement))
		{
			// Spans can contain unsupported elements, so we have to walk their inlines.
			var span = (Span)textElement;
			var inlines = span.Inlines;

			var numInlines = inlines.Count;
			for (var i = 0; i < numInlines; ++i)
			{
				if (!HyperlinkSupportsElement(inlines[i]))
				{
					return false;
				}
			}

			// If we've come this far, we've encountered no unsupported elements in the span.
			return true;
		}

		return false;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the given TextElement can be added to the given InlineCollection.
	//
	//  Remarks:
	//      If the InlineCollection is part of a TextBlock, it's subject to the constraints
	//      on TextBlock elements (see <TextBlockSupportsElement>)
	//------------------------------------------------------------------------
	internal static bool InlineCollectionSupportsElement(InlineCollection collection, TextElement textElement)
	{
		// Assume the inline is supported.
		if (IsInlineCollectionInElement(collection, typeof(Microsoft.UI.Xaml.Controls.TextBlock)))
		{
			return TextBlockSupportsElement(textElement);
		}
		else if (IsInlineCollectionInElement(collection, typeof(Hyperlink)))
		{
			return HyperlinkSupportsElement(textElement);
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the given inline collection is part of the content tree
	//      of an element with the given type.
	//------------------------------------------------------------------------
	internal static bool IsInlineCollectionInElement(InlineCollection collection, global::System.Type elementType)
	{
		object? parent = collection.GetParent();
		while (parent is not null)
		{
			if (elementType.IsInstanceOfType(parent))
			{
				return true;
			}

			parent = parent.GetParent();
		}

		return false;
	}
}
