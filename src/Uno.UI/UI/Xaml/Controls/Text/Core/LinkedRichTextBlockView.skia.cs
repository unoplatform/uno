// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinkedRichTextBlockView.h, LinkedRichTextBlockView.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

//---------------------------------------------------------------------------
//
//  LinkedRichTextBlockView
//
//  Implements ITextView querying methods for linked CRichTextBlock with
//  overflows.
//
//---------------------------------------------------------------------------
internal sealed class LinkedRichTextBlockView : ITextView
{
	private readonly RichTextBlock m_pMaster;
	private RichTextBlockView? m_pInputContextView;

	public LinkedRichTextBlockView(RichTextBlock pMaster)
	{
		m_pMaster = pMaster;
	}

	// Sets the current source of input events. Set by the source before
	// notifying TextSelectionManager of input events. When TextSelectionManager
	// queries linked view, e.g. for pixel position from text position it can distinguish between
	// views of multiple links by checking the sender view. Pixel coordinates passed in to
	// PixelPositionToTextPosition will be relative to this view/its owning element.
	// TODO: Remove this and change methods on ITextView to accept sender.
	public void SetInputContextView(RichTextBlockView pView) => m_pInputContextView = pView;

	public Rect[] TextRangeToTextBounds(uint startOffset, uint endOffset)
	{
		// In current scenarios TextRangeToTextBounds is never called from any
		// place where a linked view is needed. It is only called from text UIElements
		// to generate highlight and focus rects. A text UIElement will have it's own view, even if it's linked,
		// and can use that.
		MUX_ASSERT(false);
		throw new NotSupportedException(); // E_NOTIMPL
	}

	public Rect[] TextSelectionToTextBounds(IJupiterTextSelection selection)
	{
		// In current scenarios TextSelectionToTextBounds is never called from TextSelection or any
		// other place where a linked view is needed. It is only called from text UIElements
		// to generate highlight rects. A text UIElement will have it's own view, even if it's linked,
		// and can use that.
		MUX_ASSERT(false);
		throw new NotSupportedException(); // E_NOTIMPL
	}

	public bool IsAtInsertionPosition(uint iTextPosition)
	{
		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		RichTextBlockOverflow? pOverflow = m_pMaster.GetNext() as RichTextBlockOverflow;

		while (pView is not null)
		{
			if (pView.ContainsPosition(iTextPosition, TextGravity.LineForwardCharacterForward))
			{
				return pView.IsAtInsertionPosition(iTextPosition);
			}

			if (pOverflow is not null)
			{
				pView = pOverflow.GetSingleElementTextView();
				pOverflow = pOverflow.GetNext() as RichTextBlockOverflow;
			}
			else
			{
				pView = null;
			}
		}

		return false;
	}

	public uint PixelPositionToTextPosition(Point pixelCoordinate, bool bIncludeNewline, out TextGravity gravity)
	{
		gravity = TextGravity.LineForwardCharacterForward;

		// The input context view should have been set by the element receiving input.
		if (m_pInputContextView is not null)
		{
			return m_pInputContextView.PixelPositionToTextPosition(pixelCoordinate, bIncludeNewline, out gravity);
		}

		return 0;
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
		pixelOffset = 0;
		characterTop = 0;
		characterHeight = 0;
		lineTop = 0;
		lineHeight = 0;
		lineBaseline = 0;
		lineOffset = 0;

		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		RichTextBlockOverflow? pOverflow = m_pMaster.GetNext() as RichTextBlockOverflow;

		while (pView is not null)
		{
			if (pView.ContainsPosition(iTextPosition, eGravity))
			{
				pView.TextPositionToPixelPosition(
					iTextPosition,
					eGravity,
					out pixelOffset,
					out characterTop,
					out characterHeight,
					out lineTop,
					out lineHeight,
					out lineBaseline,
					out lineOffset);
				break;
			}

			if (pOverflow is not null)
			{
				pView = pOverflow.GetSingleElementTextView();
				pOverflow = pOverflow.GetNext() as RichTextBlockOverflow;
			}
			else
			{
				pView = null;
			}
		}
	}

	public FrameworkElement? GetUIScopeForPosition(uint iTextPosition, TextGravity eGravity)
	{
		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		RichTextBlockOverflow? pOverflow = m_pMaster.GetNext() as RichTextBlockOverflow;

		while (pView is not null)
		{
			if (pView.ContainsPosition(iTextPosition, eGravity))
			{
				return pView.GetUIScopeForPosition(iTextPosition, eGravity);
			}

			if (pOverflow is not null)
			{
				pView = pOverflow.GetSingleElementTextView();
				pOverflow = pOverflow.GetNext() as RichTextBlockOverflow;
			}
			else
			{
				pView = null;
			}
		}

		return null;
	}

	public uint GetContentStartPosition()
	{
		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		return pView?.GetContentStartPosition() ?? 0;
	}

	public uint GetContentLength()
	{
		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		RichTextBlockOverflow? pOverflow = m_pMaster.GetNext() as RichTextBlockOverflow;
		uint contentLength = 0;

		while (pView is not null)
		{
			contentLength += pView.GetContentLength();

			if (pOverflow is not null)
			{
				pView = pOverflow.GetSingleElementTextView();
				pOverflow = pOverflow.GetNext() as RichTextBlockOverflow;
			}
			else
			{
				pView = null;
			}
		}

		return contentLength;
	}

	public int GetAdjustedPosition(int charIndex)
	{
		// RichTextBlockOverflow gets its highlighting positions from RichTextBlock
		MUX_ASSERT(false);
		return 0;
	}

	// UIA navigation will retrieve a character index from an overflowed RTB here
	public int GetCharacterIndex(int position)
		=> m_pMaster.GetSingleElementTextView()?.GetCharacterIndex(position) ?? 0;

	public bool ContainsPosition(uint iTextPosition, TextGravity gravity)
	{
		RichTextBlockView? pView = m_pMaster.GetSingleElementTextView();
		RichTextBlockOverflow? pOverflow = m_pMaster.GetNext() as RichTextBlockOverflow;

		while (pView is not null)
		{
			if (pView.ContainsPosition(iTextPosition, gravity))
			{
				return true;
			}

			if (pOverflow is not null)
			{
				pView = pOverflow.GetSingleElementTextView();
				pOverflow = pOverflow.GetNext() as RichTextBlockOverflow;
			}
			else
			{
				pView = null;
			}
		}

		return false;
	}
}
