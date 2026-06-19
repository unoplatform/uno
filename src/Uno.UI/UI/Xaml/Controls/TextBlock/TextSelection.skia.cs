// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference TextSelection.h, TextSelection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Documents.BlockLayout.TextGravity;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

//  Synopsis:
//      Implements logical selection object for plain-text controls.
//
//  See also:
//      <ITextSelection>.

// MUX Reference TextSelectionNotify.h, tag winui3/release/1.8.2, commit 4a1c6184c
internal interface ITextSelectionNotify
{
	void OnSelectionChanged();
}

internal sealed class TextSelection : IJupiterTextSelection
{
	private ITextContainer m_pContainer;
	private PlainTextPosition m_movingPosition;
	private PlainTextPosition m_anchorPosition;
	private TextGravity m_eCursorGravity;
	private ITextView m_pTextView;
	private ITextSelectionNotify? m_textSelectionNotify;

	private ITextContainer GetContainer()
	{
		MUX_ASSERT(m_pContainer != null);
		return m_pContainer!;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Constructor.
	//------------------------------------------------------------------------
	private TextSelection(
		ITextContainer pContainer,
		ITextView pTextView,
		ITextSelectionNotify? textSelectionNotify)
	{
		m_pContainer = pContainer;
		m_pTextView = pTextView;
		m_eCursorGravity = LineForwardCharacterBackward;
		m_movingPosition = new PlainTextPosition(pContainer, 0, LineForwardCharacterForward);
		m_anchorPosition = new PlainTextPosition(pContainer, 0, LineForwardCharacterForward);
		m_textSelectionNotify = textSelectionNotify;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::Create
	//
	//  Synopsis: Static create method.
	//
	//------------------------------------------------------------------------
	public static bool Create(
		ITextContainer pContainer,
		ITextView pTextView,
		ITextSelectionNotify? textSelectionNotify,
		out TextSelection ppSelection)
	{
		// Create an instance of selection object.

		TextSelection pSelection = new TextSelection(
			pContainer,
			pTextView,
			textSelectionNotify);

		// Assign outgoing object.

		ppSelection = pSelection;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'moving' end of the selection.
	//------------------------------------------------------------------------
	public bool GetMovingTextPosition(out TextPosition pPosition)
	{
		pPosition = new TextPosition(m_movingPosition);
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'anchor' (fixed) end of the selection.
	//------------------------------------------------------------------------
	public bool GetAnchorTextPosition(out TextPosition pPosition)
	{
		pPosition = new TextPosition(m_anchorPosition);
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'start' of the selection.
	//------------------------------------------------------------------------
	public bool GetStartTextPosition(out TextPosition pPosition)
		=> m_movingPosition < m_anchorPosition ? GetMovingTextPosition(out pPosition) : GetAnchorTextPosition(out pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the 'end' of the selection.
	//------------------------------------------------------------------------
	public bool GetEndTextPosition(out TextPosition pPosition)
		=> m_movingPosition > m_anchorPosition ? GetMovingTextPosition(out pPosition) : GetAnchorTextPosition(out pPosition);

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the cursor, set through <Select>.
	//------------------------------------------------------------------------
	public TextGravity GetCursorGravity() => m_eCursorGravity;

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the start position.
	//------------------------------------------------------------------------
	public TextGravity GetStartGravity() => IsEmpty() ? m_eCursorGravity : LineForwardCharacterForward;

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the end position.
	//------------------------------------------------------------------------
	public TextGravity GetEndGravity() => IsEmpty() ? m_eCursorGravity : LineBackwardCharacterBackward;

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text gravity of the moving position.
	//------------------------------------------------------------------------
	public TextGravity GetMovingGravity()
	{
		if (IsEmpty())
		{
			// Moving position is a cursor (empty selection)
			return m_eCursorGravity;
		}
		else if (m_movingPosition < m_anchorPosition)
		{
			// Moving position is start of a non-empty selection
			return LineForwardCharacterForward;
		}
		else
		{
			// Moving position is end of a non-empty selection
			return LineBackwardCharacterBackward;
		}
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the selection is empty (i.e. represents a caret).
	//------------------------------------------------------------------------
	public bool IsEmpty() => m_anchorPosition == m_movingPosition;

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the length of the selection (== 0 for a caret).
	//------------------------------------------------------------------------
	public bool GetLength(out uint pLength)
	{
		pLength = 0;
		uint startOffset = 0;
		uint endOffset = 0;

		if (!GetStartTextPosition(out var startTextPosition))
		{
			return false;
		}
		if (!GetEndTextPosition(out var endTextPosition))
		{
			return false;
		}

		if (!startTextPosition.GetOffset(out startOffset))
		{
			return false;
		}
		if (!endTextPosition.GetOffset(out endOffset))
		{
			return false;
		}

		pLength = endOffset - startOffset;

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to cover the given range. XUINT32-based overload.
	//
	//  Remarks:
	//      Anchor position and moving position can be the same.
	//------------------------------------------------------------------------
	public bool Select(
		uint iAnchorTextPosition,
		uint iMovingTextPosition,
		TextGravity eCursorGravity)
	{
		if (!NormalizeSelection(
			m_pContainer,
			m_pTextView,
			ref iAnchorTextPosition,
			ref iMovingTextPosition))
		{
			return false;
		}

		// TODO: Figure out correct gravity for anchor. Moving uses cursor gravity.
		m_anchorPosition = new PlainTextPosition(GetContainer(), iAnchorTextPosition, LineForwardCharacterBackward);
		m_movingPosition = new PlainTextPosition(GetContainer(), iMovingTextPosition, eCursorGravity);

		m_eCursorGravity = eCursorGravity;

		if (m_textSelectionNotify != null)
		{
			m_textSelectionNotify.OnSelectionChanged();
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to cover the given range. TextPosition-based overload.
	//
	//  Remarks:
	//      Anchor position and moving position can be the same.
	//------------------------------------------------------------------------
	public bool Select(
		in TextPosition anchorTextPosition,
		in TextPosition movingTextPosition,
		TextGravity eCursorGravity)
	{
		uint anchorOffset = 0;
		uint movingOffset = 0;

		// Even though anchor/moving positions will have gravity associated, do not use it to select since selection will be normalized.
		if (!anchorTextPosition.GetOffset(out anchorOffset))
		{
			return false;
		}
		if (!movingTextPosition.GetOffset(out movingOffset))
		{
			return false;
		}

		if (!Select(anchorOffset, movingOffset, eCursorGravity))
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Resets the selection to represent a caret at the text position indicated
	//      by the given point.
	//------------------------------------------------------------------------
	public bool SetCaretPositionFromPoint(Point point)
	{
		uint iHitPosition = m_pTextView.PixelPositionToTextPosition(
			point,        // pixel offset
			false,        // hit after newline treated as before newline
			out TextGravity eHitGravity);

		if (!Select(
			iHitPosition, // Anchor position
			iHitPosition, // Moving position
			eHitGravity))
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Extends the selection by moving the 'moving position' to the text
	//      position indicated by the given point.
	//------------------------------------------------------------------------
	public bool ExtendSelectionByMouse(Point point)
	{
		uint iAnchorPosition = 0;
		TextGravity eHitGravity = LineForwardCharacterForward;

		if (!m_anchorPosition.GetOffset(out iAnchorPosition))
		{
			return false;
		}

		uint iHitPosition = m_pTextView.PixelPositionToTextPosition(
			point,   // pixel offset
			true,    // Allow selection to include newline
			out _);

		// Update the selection moving position to the new hit
		// position, while keeping the anchor position unchanged.

		if (!Select(
			iAnchorPosition, // Old anchor position
			iHitPosition,    // New moving position
			eHitGravity))
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Selects the word that falls under the given point, if any.
	//------------------------------------------------------------------------
	public bool SelectWord(Point point)
	{
		MUX_ASSERT(false);

		// This capability is currently living at CTextEditor::SelectWord() because it
		// is common code between selection types, and CTextEditor::SelectWord()
		// is the only client so we have code in one place instead of two.

		// This does indeed break from WPF precedent, where it lives in TextPointer.
		// Re-evaluate once our own TextPosition is up and running.

		return false; // RRETURN(E_NOTIMPL)
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text underlying the selection. If the selection is empty,
	//      returns a NULL string with a length of 0.
	//------------------------------------------------------------------------
	public bool GetText(out string pstrText)
	{
		uint startOffset = 0;
		uint endOffset = 0;

		// Fill in default answer
		pstrText = string.Empty;

		if (!IsEmpty())
		{
			if (!GetStartTextPosition(out var startTextPosition))
			{
				return false;
			}
			if (!GetEndTextPosition(out var endTextPosition))
			{
				return false;
			}

			if (!startTextPosition.GetOffset(out startOffset))
			{
				return false;
			}
			if (!endTextPosition.GetOffset(out endOffset))
			{
				return false;
			}

			string characters = m_pContainer.GetText(
				startOffset,
				endOffset,
				true /*insertNewlines*/);

			// For TextBlock and RichTextBlock selection is not normalized.
			// So it is possible that the moving and anchor positions are not equal
			// yet there is no text in between.
			if (characters.Length > 0)
			{
				pstrText = characters;
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Returns a XAML string describing the object tree of the selection.
	//
	//  Remarks:
	//      Not supported in plain text.
	//------------------------------------------------------------------------
	public bool GetXaml(out string pstrXaml)
	{
		pstrXaml = string.Empty;

		return false; // RRETURN(E_NOT_SUPPORTED)
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::MoveToInsertionPosition
	//
	//  Synopsis: This is a no-op if current offset is a valid caret stop (aka) insertion
	//            position. If not, updates this position's offset to the next
	//            valid caret stop position in requested direction.
	//
	//------------------------------------------------------------------------
	private static bool MoveToInsertionPosition(
		ITextContainer pContainer,
		ITextView pTextView,
		ref uint pTextPosition,
		LogicalDirection eDirection)
	{
		bool bIsAtInsertionPosition = false;

		bool bMoved = false;
		PlainTextPosition position = new PlainTextPosition(pContainer, pTextPosition, LineForwardCharacterForward);

		// Check whether we are already at an insertion position
		if (!position.IsAtInsertionPosition(out bIsAtInsertionPosition))
		{
			return false;
		}

		if (!bIsAtInsertionPosition)
		{
			// *pTextPosition is not an insertion position.
			// Find the nearest insertion position in the direction requested.

			if (eDirection == LogicalDirection.Forward)
			{
				if (!position.GetNextInsertionPosition(out bMoved, out position))
				{
					return false;
				}
			}
			else
			{
				if (!position.GetPreviousInsertionPosition(out bMoved, out position))
				{
					return false;
				}
			}

			// Return the insertion position to our caller.
			if (!position.GetOffset(out pTextPosition))
			{
				return false;
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::NormalizeRange (static)
	//
	//  Synopsis: Static method to normalize the moving position and anchor
	//  offset of a range so that it starts and ends on an insertion position.
	//
	//------------------------------------------------------------------------
	private static bool NormalizeRange(
		ITextContainer pContainer,
		ITextView pTextView,
		ref uint piStartTextPosition,
		ref uint piEndTextPosition)
	{
		// Normalize cursor or selection start position

		if (!TextBoxHelpers.VerifyPositionPair(
			pContainer,
			piStartTextPosition,
			piEndTextPosition))
		{
			return false;
		}

		if (piStartTextPosition == piEndTextPosition)
		{
			// Normalize cursor (a zero length selection)
			if (!MoveToInsertionPosition(pContainer, pTextView, ref piStartTextPosition, LogicalDirection.Backward))
			{
				return false;
			}
			piEndTextPosition = piStartTextPosition;
		}
		else
		{
			// Normalize non-empty selection
			if (!MoveToInsertionPosition(pContainer, pTextView, ref piStartTextPosition, LogicalDirection.Backward))
			{
				return false;
			}
			if (!MoveToInsertionPosition(pContainer, pTextView, ref piEndTextPosition, LogicalDirection.Forward))
			{
				return false;
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::NormalizeSelection (static)
	//
	//  Synopsis: Static method to normalize the moving position and anchor
	//  offset of a range so that it starts and ends on an insertion position.
	//
	//------------------------------------------------------------------------
	private static bool NormalizeSelection(
		ITextContainer pContainer,
		ITextView pTextView,
		ref uint piAnchorTextPosition,
		ref uint piMovingTextPosition)
	{
		uint iStartTextPosition = 0;
		uint iEndTextPosition = 0;

		// Determine start position (lowest offset)

		if (piAnchorTextPosition <= piMovingTextPosition)
		{
			iStartTextPosition = piAnchorTextPosition;
			iEndTextPosition = piMovingTextPosition;
		}
		else
		{
			iStartTextPosition = piMovingTextPosition;
			iEndTextPosition = piAnchorTextPosition;
		}

		// Normalize range

		if (!NormalizeRange(pContainer, pTextView, ref iStartTextPosition, ref iEndTextPosition))
		{
			return false;
		}

		// Return to caller as moving and anchor positions

		if (piAnchorTextPosition <= piMovingTextPosition)
		{
			piAnchorTextPosition = iStartTextPosition;
			piMovingTextPosition = iEndTextPosition;
		}
		else
		{
			piMovingTextPosition = iStartTextPosition;
			piAnchorTextPosition = iEndTextPosition;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::ExtendSelectionByMouse
	//
	//------------------------------------------------------------------------
	public bool ExtendSelectionByMouse(in TextPosition cursorPosition)
	{
		uint uCursorPosition = 0;
		uint uAnchorPosition = 0;
		TextGravity hitGravity = LineForwardCharacterForward;

		if (!GetAnchorTextPosition(out var anchorPosition))
		{
			return false;
		}
		if (!anchorPosition.GetOffset(out uAnchorPosition))
		{
			return false;
		}

		if (!cursorPosition.GetOffset(out uCursorPosition))
		{
			return false;
		}

		// Update the selection moving position to the new hit
		// position, while keeping the anchor position unchanged.
		if (!Select(
			uAnchorPosition,
			uCursorPosition,
			hitGravity))
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::SetCaretPositionFromTextPosition
	//
	//------------------------------------------------------------------------
	public bool SetCaretPositionFromTextPosition(in TextPosition position)
	{
		uint uPosition = 0;
		TextGravity hitGravity = LineForwardCharacterForward;

		if (!position.GetOffset(out uPosition))
		{
			return false;
		}
		if (!position.GetPlainPosition().GetGravity(out hitGravity))
		{
			return false;
		}

		if (!Select(
			uPosition, // Anchor position
			uPosition, // Moving position
			hitGravity))
		{
			return false;
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   TextSelection::SelectWord
	//
	//------------------------------------------------------------------------
	public bool SelectWord(in TextPosition position) => false; // RRETURN(E_UNEXPECTED)

	public void ResetSelection()
	{
		m_movingPosition = new PlainTextPosition(m_pContainer, 0, LineForwardCharacterForward);
		m_anchorPosition = new PlainTextPosition(m_pContainer, 0, LineForwardCharacterForward);
	}
}
