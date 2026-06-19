// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextElement.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class TextElement
{
	//------------------------------------------------------------------------
	//  CTextElement::GetContentStart — TextPointer at the start of this element's content.
	//------------------------------------------------------------------------
	internal TextPointer? GetContentStart() => GetTextPointer(ElementEdge.ContentStart);

	//------------------------------------------------------------------------
	//  CTextElement::GetContentEnd — TextPointer at the end of this element's content.
	//------------------------------------------------------------------------
	internal TextPointer? GetContentEnd() => GetTextPointer(ElementEdge.ContentEnd);

	//------------------------------------------------------------------------
	//  CTextElement::GetElementStart — TextPointer just before this element.
	//------------------------------------------------------------------------
	internal TextPointer? GetElementStart() => GetTextPointer(ElementEdge.ElementStart);

	//------------------------------------------------------------------------
	//  CTextElement::GetElementEnd — TextPointer just after this element.
	//------------------------------------------------------------------------
	internal TextPointer? GetElementEnd() => GetTextPointer(ElementEdge.ElementEnd);

	//------------------------------------------------------------------------
	//  CTextElement::GetTextPointer
	//
	//  Returns a TextPointer at the passed element edge. RichTextBlock/TextBlock
	//  use the plain-text path (the container backing store).
	//------------------------------------------------------------------------
	private TextPointer? GetTextPointer(ElementEdge edge)
	{
		var frameworkElement = GetContainingFrameworkElement();

		if (frameworkElement is null)
		{
			// If we're outside a TextBlock or RichTextBlock: WinUI fails with NotSupportedException.
			return null;
		}

		ITextContainer? textContainer = frameworkElement switch
		{
			RichTextBlock richTextBlock => richTextBlock.Blocks.GetTextContainer(),
			// TextBlock's InlineCollection implements ITextContainer directly (skia).
			TextBlock textBlock => textBlock.Inlines as ITextContainer,
			// Outside a TextBlock or RichTextBlock: WinUI fails with NotSupportedException.
			_ => null,
		};

		if (textContainer is null)
		{
			return null;
		}

		var plainTextPosition = GetElementEdgePositionFromTextContainer(edge, textContainer);
		return TextPointer.CreateInstanceWithInternalPointer(plainTextPosition);
	}

	//------------------------------------------------------------------------
	//  CTextElement::GetElementEdgePositionFromTextContainer
	//
	//  Builds the PlainTextPosition for an element edge inside its container.
	//------------------------------------------------------------------------
	private PlainTextPosition GetElementEdgePositionFromTextContainer(ElementEdge edge, ITextContainer textContainer)
	{
		textContainer.GetElementEdgeOffset(this, edge, out var offset, out var found);

		// Element must be found within the container otherwise we wouldn't have matched it to the control.
		MUX_ASSERT(found);

		TextGravity gravity;
		switch (edge)
		{
			case ElementEdge.ContentStart:
			case ElementEdge.ElementEnd:
				gravity = TextGravity.LineForwardCharacterBackward;
				break;
			case ElementEdge.ContentEnd:
			case ElementEdge.ElementStart:
				gravity = TextGravity.LineForwardCharacterForward;
				break;
			default:
				MUX_ASSERT(false);
				gravity = TextGravity.LineForwardCharacterForward;
				break;
		}

		return new PlainTextPosition(textContainer, offset, gravity);
	}
}
