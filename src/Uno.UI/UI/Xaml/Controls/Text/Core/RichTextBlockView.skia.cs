// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichTextBlockView.h, RichTextBlockView.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

//---------------------------------------------------------------------------
//
//  RichTextBlockView
//
//  Implements ITextView querying methods for RichTextBlock.
//
//---------------------------------------------------------------------------
internal sealed class RichTextBlockView : ITextView
{
	// The offset adjustment to use for the start of each inline.  Offsets provided to TextRangeToTextBounds
	// should be adjusted by this amount to get the appropriate rects.  Offsets output from other methods should
	// already account for this adjustment.
	private const int PlaceHolderPositionsForInlines = 2;

	// Uno adaptation: the view is constructed directly with the PageNode (and owning
	// element). WinUI reaches the owner via m_pPageNode->GetPageOwner(); the Uno
	// PageNode exposes the same accessor, so GetUIScopeForPosition delegates to it.
	private readonly PageNode m_pPageNode;
	private readonly FrameworkElement m_owner;

	public RichTextBlockView(BlockNode pPageNode, FrameworkElement owner)
	{
		m_pPageNode = (PageNode)pPageNode;
		m_owner = owner;
	}

	public Rect[] TextRangeToTextBounds(uint startOffset, uint endOffset)
	{
		uint length;
		var bounds = new List<TextBounds>();

		MUX_ASSERT(endOffset >= startOffset);
		length = endOffset - startOffset;

		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty() &&
			length > 0)
		{
			// Snap the position to page bounds.
			uint startInPage = 0;
			uint pageStart = m_pPageNode.GetStartPosition();
			uint pageLength = m_pPageNode.GetContentLength();

			// Eliminate no-overlap cases.
			// Start offset is inclusive for bounds calculation, so <= check is correct here.
			if ((startOffset + length) <= pageStart ||
				startOffset >= (pageStart + pageLength))
			{
				length = 0;
			}
			else
			{
				if (startOffset < pageStart)
				{
					startInPage = 0;
					length -= (pageStart - startOffset);
				}
				else
				{
					startInPage = startOffset - pageStart;
				}
				length = Math.Min(length, pageLength - startInPage);
			}

			if (length > 0)
			{
				m_pPageNode.GetTextBounds(
					startInPage,
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
		selection.GetStartTextPosition(out var startPosition);
		selection.GetEndTextPosition(out var endPosition);

		startPosition.GetOffset(out var startOffset);
		endPosition.GetOffset(out var endOffset);

		return TextRangeToTextBounds(startOffset, endOffset);
	}

	public bool IsAtInsertionPosition(uint iTextPosition)
	{
		uint pageLocalPosition;

		// Other RichTextBlockView APIs adjust for gravity to match the page node. IsAtInsertionPosition does not because
		// if it's not within the page's content it's OK to return false and also because there's no concept of Insertion
		// for RichTextBlock.
		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty())
		{
			if (TransformPositionToPage(iTextPosition, out pageLocalPosition))
			{
				return m_pPageNode.IsAtInsertionPosition(pageLocalPosition);
			}
			else
			{
				return false;
			}
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

		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty())
		{
			pageLocalPosition = m_pPageNode.PixelPositionToTextPosition(pixelCoordinate, out gravity);
			pageLocalPosition = TransformPositionFromPage(pageLocalPosition);
		}

		return pageLocalPosition;
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
		uint pageLocalPosition;
		TextGravity gravity = eGravity;

		pixelOffset = 0;
		characterTop = 0;
		characterHeight = 0;
		lineTop = 0;
		lineHeight = 0;
		lineBaseline = 0;
		lineOffset = 0;

		// If the page has no break and the position is the last position on the page, it corresponds to the end of
		// the text container. In this case the view is considered to contain it. For pixel position, treat it as though it
		// has backward gravity, i.e. is the trailing edge of the last position on the page.
		if (iTextPosition == (m_pPageNode.GetStartPosition() + m_pPageNode.GetContentLength()) &&
			m_pPageNode.GetBreak() == null &&
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

		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty())
		{
			if (TransformPositionToPage(startOffset, out pageLocalPosition))
			{
				m_pPageNode.GetTextBounds(
					pageLocalPosition,
					length,
					bounds);

				if (bounds.Count > 0)
				{
					MUX_ASSERT(bounds.Count == 1);

					// Use values from first bounds rect.
					pixelOffset = (float)bounds[0].Rect.X;

					if ((gravity.HasFlag(TextGravity.CharacterBackward) && bounds[0].FlowDirection == m_pPageNode.GetFlowDirection())
						|| (!gravity.HasFlag(TextGravity.CharacterBackward) && bounds[0].FlowDirection != m_pPageNode.GetFlowDirection()))
					{
						pixelOffset += (float)bounds[0].Rect.Width;
					}

					lineTop = (float)bounds[0].Rect.Y;

					lineHeight = (float)bounds[0].Rect.Height;
				}
			} // Else: If the position is not on the page, don't return anything. Callers should check Contains.
		}
	}

	public FrameworkElement? GetUIScopeForPosition(uint iTextPosition, TextGravity eGravity)
	{
		uint pageLocalPosition;
		uint startOffset;
		TextGravity gravity = eGravity;

		// If the page has no break and the position is the last position on the page, it corresponds to the end of
		// the text container. In this case the view is considered to contain it. For GetUIScope, treat it as though it
		// has backward gravity, i.e. is the trailing edge of the last position on the page.
		if (iTextPosition == (m_pPageNode.GetStartPosition() + m_pPageNode.GetContentLength()) &&
			m_pPageNode.GetBreak() == null &&
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

		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty())
		{
			if (TransformPositionToPage(startOffset, out pageLocalPosition))
			{
				return m_pPageNode.GetPageOwner();
			}
			else
			{
				// This may be called for a position at the end of the block collection which would be just outside the page.
				return null;
			}
		}
		else
		{
			return null;
		}
	}

	public bool ContainsPosition(uint iTextPosition, TextGravity gravity)
	{
		uint pageLocalPosition;
		uint position;
		bool contains = false;

		// If the page has no break and the position is the last position on the page, it corresponds to the end of
		// the text container. In this case the view is considered to contain it. For Contains, treat it as though it
		// has backward gravity, i.e. is the trailing edge of the last position on the page.
		if (iTextPosition == (m_pPageNode.GetStartPosition() + m_pPageNode.GetContentLength()) &&
			m_pPageNode.GetBreak() == null &&
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

		if (!m_pPageNode.IsMeasureDirty() &&
			!m_pPageNode.IsArrangeDirty())
		{
			contains = TransformPositionToPage(position, out pageLocalPosition);
		}

		return contains;
	}

	public uint GetContentStartPosition() => m_pPageNode.GetStartPosition();

	public uint GetContentLength() => m_pPageNode.GetContentLength();

	public int GetAdjustedPosition(int charIndex)
	{
		int charCount = charIndex;
		int adjustedPosition = 0;

		RichTextBlock? owningRichTextBlock = GetOwningRichTextBlock();

		// Save the starting and ending positions of the specific RTB or RTBO currently being highlighted.
		int blockStartPosition = (int)GetContentStartPosition();
		int blockEndPosition = blockStartPosition + (int)m_pPageNode.GetContentLength();

		// Correct out of bound indexes to valid ones
		if (charIndex < 0)
		{
			adjustedPosition = PlaceHolderPositionsForInlines;
		}
		else if (owningRichTextBlock is not null)
		{
			bool previousBlock = false;
			foreach (var block in owningRichTextBlock.Blocks)
			{
				if (block is not Paragraph paragraph)
				{
					continue;
				}

				// Account for the offset at the end of each paragraph before searching the next one
				if (previousBlock)
				{
					adjustedPosition += PlaceHolderPositionsForInlines;
				}
				// Go through each inline to see if the position we're looking for is in it
				var inlines = paragraph.Inlines;
				bool adjustedPositionFound = TextBlockViewHelpers.AdjustPositionByCharacterCount(inlines, ref charCount, ref adjustedPosition);
				if (adjustedPositionFound)
				{
					// Don't need to search through any other paragraphs
					break;
				}
				previousBlock = true;
			}
		}

		// Since specified bounds may be outside of the RTB/RTBO that is
		// currently being rendered, snap to those limits.
		// If both bounds are outside of the RTB/RTBO, no highlight will
		// render since the endOffset is decremented after it is returned,
		// and will be smaller than the startOffset.
		if (adjustedPosition < blockStartPosition)
		{
			adjustedPosition = blockStartPosition;
		}
		if (adjustedPosition > blockEndPosition)
		{
			adjustedPosition = blockEndPosition;
		}

		return adjustedPosition;
	}

	public int GetCharacterIndex(int position)
	{
		int adjustedPosition = position;
		int charIndex = 0;

		RichTextBlock? owningRichTextBlock = GetOwningRichTextBlock();

		// Correct out of bound indexes to valid ones
		if (position < 0)
		{
			charIndex = 0;
		}
		else if (owningRichTextBlock is not null)
		{
			bool previousBlock = false;
			foreach (var block in owningRichTextBlock.Blocks)
			{
				if (block is not Paragraph paragraph)
				{
					continue;
				}

				// Account for the offset at the end of each paragraph before searching the next one
				if (previousBlock)
				{
					adjustedPosition -= PlaceHolderPositionsForInlines;
				}
				// Go through each inline to see if the position we're looking for is in it
				var inlines = paragraph.Inlines;
				bool charIndexFound = TextBlockViewHelpers.AdjustCharacterIndexByPosition(inlines, ref charIndex, ref adjustedPosition);
				if (charIndexFound)
				{
					// Don't need to search through any other paragraphs
					break;
				}
				previousBlock = true;
			}
		}

		return charIndex;
	}

	// Uno seam: WinUI down-casts m_pPageNode->GetPageOwner() to CRichTextBlock (or, for an
	// overflow, to CRichTextBlockOverflow->GetMaster()). RichTextBlockOverflow is not ported yet,
	// so only the RichTextBlock owner is resolved here.
	private RichTextBlock? GetOwningRichTextBlock() => m_pPageNode.GetPageOwner() as RichTextBlock;

	// Gets a page-relative offset from an external offset passed to the page and vice versa.
	// This is necessary because query methods can be called from a linked text view
	// with an arbitrary offset.
	// TransformToPage returns bool because the position may be on the page at all. TransformFromPage
	// is only called for a position on the page, so it will always succeed.
	private bool TransformPositionToPage(uint position, out uint pPosition)
	{
		uint pageLocalPosition;
		uint pageStart = m_pPageNode.GetStartPosition();

		if (position >= pageStart)
		{
			pageLocalPosition = position - pageStart;
			if (pageLocalPosition < m_pPageNode.GetContentLength())
			{
				pPosition = pageLocalPosition;
				return true;
			}
		}

		pPosition = 0;
		return false;
	}

	private uint TransformPositionFromPage(uint position)
	{
		uint pageStart = m_pPageNode.GetStartPosition();
		return (position + pageStart);
	}

	public static Rect[] GetBoundsCollectionForElement(
		ITextView textView,
		TextElement textElement)
	{
		// WinUI: get the element's content start/end TextPointerWrappers, read their
		// offsets, and call textView->TextRangeToTextBounds(startOffset, endOffset).
		//   IFC_RETURN(textElement->GetContentStart(...));
		//   IFC_RETURN(textElement->GetContentEnd(...));
		//   IFC_RETURN(contentStart->GetOffset(&contentStartOffset));
		//   IFC_RETURN(contentEnd->GetOffset(&contentEndOffset));
		//   IFC_RETURN(textView->TextRangeToTextBounds(contentStartOffset, contentEndOffset, ...));
		// TODO Uno (Stage 7): requires the text-pointer layer (CTextPointerWrapper.GetOffset).
		throw new NotSupportedException("TODO Uno (Stage 7): GetBoundsCollectionForElement (text-pointer offsets)");
	}
}
