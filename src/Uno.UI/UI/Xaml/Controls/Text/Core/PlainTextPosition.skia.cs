// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PlainTextPosition.cpp, PlainTextPosition.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

/*
Summary:
    A lightweight, integer-based text position for plain-text scenarios.

Remarks:
    When a position is invalid, all calls fail except:
        * IsValid: returns FALSE
        * Equals: returns FALSE
        * LessThan: returns FALSE
*/
// Ported as a struct to preserve the value/copy semantics WinUI relied on
// (copy constructor + "*pPosition = *this"). Mutating helpers operate on a copy.
internal struct PlainTextPosition
{
	private ITextContainer? m_pContainer;
	private uint m_offset;
	private TextGravity m_gravity;

	//------------------------------------------------------------------------
	//  Summary:
	//      Creates a position with the given offset.
	//------------------------------------------------------------------------
	public PlainTextPosition(
		ITextContainer pContainer,
		uint offset,
		TextGravity gravity)
	{
		m_pContainer = pContainer;
		m_offset = offset;
		m_gravity = gravity;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text position integral offset.
	//------------------------------------------------------------------------
	public bool GetOffset(out uint pOffset)
	{
		pOffset = 0;
		if (!CheckValid())
		{
			return false;
		}
		pOffset = m_offset;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text position gravity.
	//------------------------------------------------------------------------
	public bool GetGravity(out TextGravity pGravity)
	{
		pGravity = default;
		if (!CheckValid())
		{
			return false;
		}
		pGravity = m_gravity;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the text position gravity.
	//------------------------------------------------------------------------
	public bool GetCharacterRect(
		TextGravity gravity,
		out Rect pRect)
	{
		Rect rect = default;
		pRect = rect;

		if (!CheckValid())
		{
			return false;
		}

		ITextView? pTextView = GetTextView();
		if (pTextView != null)
		{
			bool contains = pTextView.ContainsPosition(m_offset, gravity);

			// TextView may not contain this position, e.g. if layout hasn't happened yet or it doesn't
			// fit in available space. In that case we want to return empty rect, not default to something.

			if (contains)
			{
				pTextView.TextPositionToPixelPosition(
					m_offset,
					gravity,
					out float x,
					out _,
					out _,
					out float y,
					out float height,
					out _,
					out float lineOffset);
				rect.X = x + lineOffset;
				rect.Y = y;
				rect.Height = height;
			}
		}
		pRect = rect;

		return true;
	}

	public ITextContainer? GetTextContainer()
	{
		if (IsValid())
		{
			return m_pContainer;
		}
		return null;
	}

	public bool GetLogicalParent(out DependencyObject? ppParent)
	{
		ppParent = null;

		if (!CheckValid())
		{
			return false;
		}

		m_pContainer!.GetContainingElement(m_offset, out var pTextElementParent);
		DependencyObject? pParent = pTextElementParent;

		if (pParent == null)
		{
			// If the container could not find a parent, return it's owning element.
			pParent = m_pContainer.GetOwnerUIElement();
		}

		ppParent = pParent;

		return true;
	}

	public bool GetVisualParent(out FrameworkElement? ppParent)
	{
		FrameworkElement? pParent = null;
		ppParent = null;

		if (!CheckValid())
		{
			return false;
		}

		ITextView? pTextView = GetTextView();
		if (pTextView != null)
		{
			bool contains = pTextView.ContainsPosition(m_offset, m_gravity);

			if (contains)
			{
				pParent = pTextView.GetUIScopeForPosition(m_offset, m_gravity);
			}
		}

		ppParent = pParent;

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the position is valid.
	//
	//  Remarks:
	//      A text position is valid if:
	//          1) It's been provided with a text container in its constructor.
	//          2) Its offset falls within the container's range
	//------------------------------------------------------------------------
	public bool IsValid()
		=> m_pContainer != null && m_offset <= TextBoxHelpers.GetMaxTextPosition(m_pContainer);

	//------------------------------------------------------------------------
	//  Summary:
	//      Returns TRUE if both positions are equal.
	//------------------------------------------------------------------------
	public bool Equals(PlainTextPosition other)
	{
		if (!IsValid() || !other.GetOffset(out var otherOffset))
		{
			return false;
		}

		return m_offset == otherOffset;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Returns TRUE if 'this' is less than 'other'.
	//------------------------------------------------------------------------
	public bool LessThan(PlainTextPosition other)
	{
		if (!IsValid() || !other.GetOffset(out var otherOffset))
		{
			return false;
		}

		return m_offset < otherOffset;
	}

	/*
	Overloaded comparison operators for convenience. They're all defined in terms
	of PlainTextPosition::Equals and PlainTextPosition::LessThan.
	*/

	public static bool operator ==(PlainTextPosition lhs, PlainTextPosition rhs) => lhs.Equals(rhs);

	public static bool operator !=(PlainTextPosition lhs, PlainTextPosition rhs) => !(lhs == rhs);

	public static bool operator <(PlainTextPosition lhs, PlainTextPosition rhs) => lhs.LessThan(rhs);

	public static bool operator <=(PlainTextPosition lhs, PlainTextPosition rhs) => lhs < rhs || lhs == rhs;

	public static bool operator >(PlainTextPosition lhs, PlainTextPosition rhs) => !(lhs <= rhs);

	public static bool operator >=(PlainTextPosition lhs, PlainTextPosition rhs) => !(lhs < rhs);

	public override bool Equals(object? obj) => obj is PlainTextPosition other && Equals(other);

	public override int GetHashCode()
	{
		GetOffset(out var offset);
		return (int)offset;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the PlainTextPosition represents a valid insertion position.
	//
	//  Remarks:
	//      A position is a valid insertion position
	//------------------------------------------------------------------------
	public bool IsAtInsertionPosition(out bool pIsAtInsertionPosition)
	{
		pIsAtInsertionPosition = false;

		if (!CheckValid())
		{
			return false;
		}

		// We may be called without a textboxview or formatting parameters during
		// control creation. Provide sensible results for this case.
		ITextView? pTextView = GetTextView();
		if (pTextView != null)
		{
			bool contains = pTextView.ContainsPosition(m_offset, m_gravity);

			if (contains)
			{
				pIsAtInsertionPosition = pTextView.IsAtInsertionPosition(m_offset);
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the next insertion position, if any.
	//
	//  Parameters:
	//      pFoundPosition - Set to TRUE if a next insertion position is found.
	//      pPosition      - The next position, if found.
	//------------------------------------------------------------------------
	public bool GetNextInsertionPosition(
		out bool pFoundPosition,
		out PlainTextPosition pPosition)
	{
		pPosition = this;
		return pPosition.MoveToNextInsertionPosition(out pFoundPosition);
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the previous insertion position, if any.
	//
	//  Parameters:
	//      pFoundPosition - Set to TRUE if a next insertion position is found.
	//      pPosition      - The previous position, if found.
	//------------------------------------------------------------------------
	public bool GetPreviousInsertionPosition(
		out bool pFoundPosition,
		out PlainTextPosition pPosition)
	{
		pPosition = this;
		return pPosition.MoveToPreviousInsertionPosition(out pFoundPosition);
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the backspace position, if any.  This can be distinct
	//  from the "previous insertion position" for some languages.
	//
	//  Parameters:
	//      pFoundPosition - Set to TRUE if a next insertion position is found.
	//      pPosition      - The previous position, if found.
	//------------------------------------------------------------------------
	public bool GetBackspacePosition(
		out bool pFoundPosition,
		out PlainTextPosition pPosition)
	{
		pPosition = this;
		return pPosition.MoveToBackspacePosition(out pFoundPosition);
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Retrieves the position at the specified offset.
	//
	//  Parameters:
	//      pFoundPosition - Set to TRUE if a position at the offset is found.
	//      pPosition      - The position, if found.
	//------------------------------------------------------------------------
	public bool GetPositionAtOffset(
		int offset,
		TextGravity gravity,
		out bool pFoundPosition,
		out PlainTextPosition pPosition)
	{
		pPosition = this;
		return pPosition.MoveByOffset(offset, gravity, out pFoundPosition);
	}

	public bool Clone(out PlainTextPosition pPosition)
	{
		pPosition = this;
		return true; // RRETURN_REMOVAL
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Checks whether the given position is inside a \r\n sequence.
	//------------------------------------------------------------------------
	public bool IsInsideLineBreak(out bool pIsInsideLineBreak)
	{
		pIsInsideLineBreak = false;

		if (!CheckValid())
		{
			return false;
		}

		m_pContainer!.GetRun(m_offset, out _, out _, out _, out _, out var characters, out var numCharacters);
		if (numCharacters != 0 && !characters.IsEmpty)
		{
			if (characters.Span[0] == UNICODE_LINE_FEED && m_offset > 0)
			{
				// There's a LF here.  Go back one character and look for CR.
				m_pContainer.GetRun(m_offset - 1, out _, out _, out _, out _, out var lookForCR, out var cLookForCR);
				if (cLookForCR != 0 && !lookForCR.IsEmpty && lookForCR.Span[0] == UNICODE_CARRIAGE_RETURN)
				{
					// We are in the middle of a CR+LF sequence.
					pIsInsideLineBreak = true;
				}
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Moves to the next insertion position.
	//
	//  Parameters:
	//      pFoundPosition - Indicates whether an insertion position was found. If FALSE,
	//                       the call did not change the position.
	//------------------------------------------------------------------------
	private bool MoveToNextInsertionPosition(out bool pFoundPosition)
	{
		pFoundPosition = false;
		uint oldOffset = m_offset;
		bool isAtInsertionPosition = false;

		if (!CheckValid())
		{
			return false;
		}

		m_pContainer!.GetPositionCount(out var numPositions);
		while (m_offset < numPositions && !isAtInsertionPosition)
		{
			++m_offset;
			if (!IsAtInsertionPosition(out isAtInsertionPosition))
			{
				// Failure should not leave the object in an invalid state. Revert to the old
				// position
				m_offset = oldOffset;
				return false;
			}
		}

		pFoundPosition = isAtInsertionPosition;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Moves to the previous insertion position.
	//
	//  Parameters:
	//      pFoundPosition - Indicates whether an insertion position was found. If FALSE,
	//                       the call did not change the position.
	//------------------------------------------------------------------------
	private bool MoveToPreviousInsertionPosition(out bool pFoundPosition)
	{
		pFoundPosition = false;
		uint oldOffset = m_offset;
		bool isAtInsertionPosition = false;

		if (!CheckValid())
		{
			return false;
		}

		m_pContainer!.GetPositionCount(out _);
		while (m_offset > 0 && !isAtInsertionPosition)
		{
			--m_offset;
			if (!IsAtInsertionPosition(out isAtInsertionPosition))
			{
				m_offset = oldOffset;
				return false;
			}
		}

		pFoundPosition = isAtInsertionPosition;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Moves to a position corresponding to backspace.
	//
	//  Parameters:
	//      pFoundPosition - Indicates whether an insertion position was found. If FALSE,
	//                       the call did not change the position.
	//------------------------------------------------------------------------
	private bool MoveToBackspacePosition(out bool pFoundPosition)
	{
		pFoundPosition = false;
		uint oldOffset = m_offset;
		bool isAtBackspacePosition = false;

		if (!CheckValid())
		{
			return false;
		}

		while (m_offset > 0 && !isAtBackspacePosition)
		{
			--m_offset;
			TextBoxHelpers.IsNotInSurrogateCRLF(m_pContainer!, m_offset, out isAtBackspacePosition);
		}

		pFoundPosition = isAtBackspacePosition;
		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Moves by the specified offset.
	//
	//------------------------------------------------------------------------
	private bool MoveByOffset(
		int offset,
		TextGravity gravity,
		out bool pFoundPosition)
	{
		pFoundPosition = false;
		uint positionsMoved = 0;
		uint absoluteOffset;

		if (!CheckValid())
		{
			return false;
		}

		m_pContainer!.GetPositionCount(out var positionCount);

		if (offset >= 0)
		{
			absoluteOffset = (uint)offset;
			while (m_offset < positionCount &&
				   positionsMoved < absoluteOffset)
			{
				++m_offset;
				positionsMoved++;
			}
		}
		else
		{
			absoluteOffset = (uint)(-offset);
			while (m_offset > 0 &&
				   positionsMoved < absoluteOffset)
			{
				--m_offset;
				positionsMoved++;
			}
		}

		pFoundPosition = (positionsMoved == absoluteOffset);
		m_gravity = gravity;

		return true;
	}

	// WinUI casts the owner UIElement to CRichTextBlock / CTextBlock and returns its GetTextView().
	// On Uno the owner exposes the view through ITextViewHost (wired in Stage 6/9); until then a
	// host that hasn't computed its view simply returns null and insertion-position scanning falls
	// back to the empty-view behavior, exactly as WinUI does during control creation.
	// WinUI declares this protected and grants `friend class CSelectionWordBreaker`; the C# friend
	// equivalent is internal so CSelectionWordBreaker.CanBreak can reach GetCharacterIndex.
	internal ITextView? GetTextView()
	{
		if (IsValid())
		{
			if (m_pContainer!.GetOwnerUIElement() is ITextViewHost host)
			{
				return host.GetTextView();
			}
		}

		return null;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Fails if <IsValid> returns FALSE.
	//------------------------------------------------------------------------
	private bool CheckValid() => IsValid();

	// Unicode literals matching WinUI's text constants.
	private const char UNICODE_LINE_FEED = '\u000A';
	private const char UNICODE_CARRIAGE_RETURN = '\u000D';
}
