// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlockView.h, TextBlockView.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

//---------------------------------------------------------------------------
//
//  TextBlockView
//
//  Implements ITextView querying methods for TextBlock.
//
//---------------------------------------------------------------------------
internal sealed class TextBlockView : ITextView
{
	// The offset adjustment to use for the start of inlines.  Offsets provided to TextRangeToTextBounds
	// should be adjusted by this amount to get the appropriate rects.  Offsets output from other methods should
	// already account for this adjustment.
	private const uint PlaceHolderPositionsForInlines = 2;

	// Uno adaptation: WinUI's view holds a CTextBlock and reaches its PageNode via
	// CTextBlock::GetPageNode(). The Uno control does not expose the PageNode until
	// Stage 9, so the view is constructed directly with the (single) page node and the
	// owning element. TextBlock is always TextMode::Normal here — the WinUI
	// DWriteLayout fast-path is a native optimization that has no equivalent on the
	// Skia node-tree path and is therefore omitted.
	private readonly BlockNode m_pPageNode;
	private readonly FrameworkElement m_pTextBlock;

	public TextBlockView(BlockNode pageNode, FrameworkElement owner)
	{
		m_pPageNode = pageNode;
		m_pTextBlock = owner;
	}

	// Gets the physical bounds for a character range within the element.
	public Rect[] TextRangeToTextBounds(uint startOffset, uint endOffset)
	{
		var bounds = new List<TextBounds>();
		MUX_ASSERT(endOffset >= startOffset);
		uint length = endOffset - startOffset;

		BlockNode pPageNode = m_pPageNode;

		if (!pPageNode.IsMeasureDirty() &&
			!pPageNode.IsArrangeDirty() &&
			length > 0)
		{
			// Snap the position to page bounds. TextBlock is always a single page
			// which starts at 0, so only the end offset may need adjustment.
			uint pageLength = pPageNode.GetContentLength();

			// Eliminate no-overlap cases.
			// Start offset is inclusive for bounds calculation, so <= check is correct here.
			if (startOffset >= pageLength)
			{
				length = 0;
			}
			else if (endOffset >= pageLength)
			{
				length = pageLength - startOffset;
			}

			if (length > 0)
			{
				pPageNode.GetTextBounds(
					startOffset,
					length,
					bounds);

				if (bounds.Count > 0)
				{
					var pBounds = new Rect[bounds.Count];

					for (int i = 0; i < bounds.Count; i++)
					{
						pBounds[i] = bounds[i].Rect;
					}

					return pBounds;
				}
			}
		}

		return Array.Empty<Rect>();
	}

	// Gets the physical bounds for the range occupied by a TextSelection.
	public Rect[] TextSelectionToTextBounds(IJupiterTextSelection selection)
	{
		// Obtain selection start and end offset, and get range bounds from the PageNode.
		// IFC_RETURN(pSelection->GetStartTextPosition(&startPosition));
		// IFC_RETURN(pSelection->GetEndTextPosition(&endPosition));
		// IFC_RETURN(startPosition.GetOffset(&startOffset));
		// IFC_RETURN(endPosition.GetOffset(&endOffset));
		// IFC_RETURN(TextRangeToTextBounds(startOffset, endOffset, ...));
		throw new NotSupportedException("TODO Uno (Stage 7): TextSelectionToTextBounds");
	}

	public bool IsAtInsertionPosition(uint iTextPosition)
	{
		BlockNode pPageNode = m_pPageNode;

		if (!pPageNode.IsMeasureDirty() &&
			!pPageNode.IsArrangeDirty() &&
			iTextPosition < pPageNode.GetContentLength())
		{
			return pPageNode.IsAtInsertionPosition(iTextPosition);
		}
		else
		{
			return false;
		}
	}

	public uint PixelPositionToTextPosition(Point pixelCoordinate, bool bIncludeNewline, out TextGravity gravity)
	{
		uint pageLocalPosition = 0;
		gravity = TextGravity.LineForwardCharacterForward;

		BlockNode pPageNode = m_pPageNode;

		if (!pPageNode.IsMeasureDirty() &&
			!pPageNode.IsArrangeDirty())
		{
			pageLocalPosition = pPageNode.PixelPositionToTextPosition(pixelCoordinate, out gravity);
		}

		return pageLocalPosition;
	}

	public uint GetContentStartPosition() => 0;

	public uint GetContentLength() => m_pPageNode.GetContentLength();

	public int GetAdjustedPosition(int charIndex)
	{
		int adjustedPosition;

		// Correct out of bound indexes to valid ones
		if (charIndex < 0)
		{
			adjustedPosition = (int)PlaceHolderPositionsForInlines;
		}
		else if ((uint)charIndex >= (GetContentLength() - (int)PlaceHolderPositionsForInlines))
		{
			adjustedPosition = (int)GetContentLength() - (int)PlaceHolderPositionsForInlines;
		}
		else
		{
			// Go through each inline to see if the charIndex we're looking for is in it.
			// TODO Uno (Stage 7): TextBlockViewHelpers.AdjustPositionByCharacterCount walks the
			// inline run model to map a character index to an adjusted text position.
			throw new NotSupportedException("TODO Uno (Stage 7): GetAdjustedPosition (inline run-model walk)");
		}

		return adjustedPosition;
	}

	public int GetCharacterIndex(int position)
	{
		int characterIndex;

		// Correct out of bound indexes to valid ones
		if (position < PlaceHolderPositionsForInlines)
		{
			characterIndex = 0;
		}
		else if (position >= (int)GetContentLength())
		{
			characterIndex = (int)GetContentLength() - (int)PlaceHolderPositionsForInlines;
		}
		else
		{
			// Go through each inline to see if the charIndex we're looking for is in it.
			// TODO Uno (Stage 7): TextBlockViewHelpers.AdjustCharacterIndexByPosition walks the
			// inline run model to map a text position to a character index.
			throw new NotSupportedException("TODO Uno (Stage 7): GetCharacterIndex (inline run-model walk)");
		}

		return characterIndex;
	}

	public void TextPositionToPixelPosition(
		uint iTextPosition,
		TextGravity eGravity,
		out float pixelOffset,
		out float characterTop,
		out float characterHeight,
		out float lineTop,
		out float lineHeight,
		out float lineBaseline,
		out float lineOffset)
	{
		uint startOffset;
		uint length = 1;
		var bounds = new List<TextBounds>();
		TextGravity gravity = eGravity;

		pixelOffset = 0;
		characterTop = 0;
		characterHeight = 0;
		lineTop = 0;
		lineHeight = 0;
		lineBaseline = 0;
		lineOffset = 0;

		BlockNode pPageNode = m_pPageNode;
		// If the page has no break and the position is the last position on the page, it corresponds to the end of
		// the text container. In this case the view is considered to contain it. For pixel position, treat it as though it
		// has backward gravity, i.e. is the trailing edge of the last position on the page.
		if (iTextPosition == pPageNode.GetContentLength() &&
			pPageNode.GetBreak() == null &&
			!gravity.HasFlag(TextGravity.CharacterBackward))
		{
			gravity = TextGravity.LineForwardCharacterBackward;
		}

		if (gravity.HasFlag(TextGravity.CharacterBackward) &&
			iTextPosition > 0)
		{
			startOffset = iTextPosition - 1;
		}
		else
		{
			startOffset = iTextPosition;
		}

		if (!pPageNode.IsMeasureDirty() &&
			!pPageNode.IsArrangeDirty() &&
			startOffset < pPageNode.GetContentLength())
		{
			pPageNode.GetTextBounds(
				startOffset,
				length,
				bounds);

			if (bounds.Count > 0)
			{
				MUX_ASSERT(bounds.Count == 1);

				// Use values from first bounds rect.
				pixelOffset = (float)bounds[0].Rect.X;

				if ((gravity.HasFlag(TextGravity.CharacterBackward) && bounds[0].FlowDirection == pPageNode.GetFlowDirection())
					|| (!gravity.HasFlag(TextGravity.CharacterBackward) && bounds[0].FlowDirection != pPageNode.GetFlowDirection()))
				{
					pixelOffset += (float)bounds[0].Rect.Width;
				}

				lineTop = (float)bounds[0].Rect.Y;

				lineHeight = (float)bounds[0].Rect.Height;
			}
		} // Else: If the position is not on the page, don't return anything. Callers should check Contains.
	}

	public FrameworkElement? GetUIScopeForPosition(uint iTextPosition, TextGravity eGravity)
	{
		// For TextBlock, it's OK to always return the TextBlock as the UIScope. Even if the
		// position is not part of formatted content, its character rect, etc. will be empty/nonexistent
		// but the TextBlock can be considered its visual parent always.
		return m_pTextBlock;
	}

	public bool ContainsPosition(uint iTextPosition, TextGravity gravity)
	{
		uint position;
		bool contains = false;

		BlockNode pPageNode = m_pPageNode;
		// If the page has no break and the position is the last position on the page, it corresponds to the end of
		// the text container. In this case the view is considered to contain it. For Contains, treat it as though it
		// has backward gravity, i.e. is the trailing edge of the last position on the page.
		if (iTextPosition == pPageNode.GetContentLength() &&
			pPageNode.GetBreak() == null &&
			!gravity.HasFlag(TextGravity.CharacterBackward))
		{
			gravity = TextGravity.LineForwardCharacterBackward;
		}

		if (gravity.HasFlag(TextGravity.CharacterBackward) &&
			iTextPosition > 0)
		{
			position = iTextPosition - 1;
		}
		else
		{
			position = iTextPosition;
		}

		if (!pPageNode.IsMeasureDirty() &&
			!pPageNode.IsArrangeDirty() &&
			position < pPageNode.GetContentLength())
		{
			contains = true;
		}

		return contains;
	}
}
