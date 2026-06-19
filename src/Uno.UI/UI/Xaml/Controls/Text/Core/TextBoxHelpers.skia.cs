// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBoxHelpers.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.BlockLayout;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

// MUX Reference EnumDefs.h (TagConversion). Controls whether GetCharacter substitutes a
// placeholder character for the (reserved) open/close edges of TextElements.
internal enum TagConversion
{
	None = 0,
	Default,
}

// Read-only subset of CTextBoxHelpers needed by PlainTextPosition and TextSelectionManager.
// The editable-container helpers (InsertText/DeleteText/GetText buffer copy, etc.) are TextBox-only
// and are not ported for the read-only RichTextBlock path.
internal static class TextBoxHelpers
{
	// Unicode literals matching WinUI's text constants.
	private const char UNICODE_LINE_FEED = '\u000A';
	private const char UNICODE_CARRIAGE_RETURN = '\u000D';

	// #define IS_TRAILING_SURROGATE(character) (((character) & 0xFC00) == 0xDC00)
	private static bool IsTrailingSurrogate(char character) => (character & 0xFC00) == 0xDC00;

	// CTextBoxHelpers::CharacterGravityBackward — true when the gravity leans on the backward character.
	internal static bool CharacterGravityBackward(TextGravity eGravity)
		=> (eGravity & TextGravity.CharacterBackward) == TextGravity.CharacterBackward;

	//------------------------------------------------------------------------
	//  Summary:
	//  Returns the maximum valid text position taking into account the
	//  special empty RichTextBlock case.
	//------------------------------------------------------------------------
	internal static uint GetMaxTextPosition(ITextContainer pTextContainer)
	{
		pTextContainer.GetPositionCount(out var cPositions);

		// Empty RichTextBlocks actually have one position, even when empty, as the ContentEnd
		// position is still there. It is always the position just after the end of content.
		if (cPositions == 0 &&
			pTextContainer.GetOwnerUIElement() is RichTextBlock)
		{
			cPositions = 1;
		}

		return cPositions;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//  Verifies that a pair of positions is within the container's range and
	//  ordered. Used by selection normalization.
	//------------------------------------------------------------------------
	internal static bool VerifyPositionPair(
		ITextContainer pTextContainer,
		uint iTextPosition1,
		uint iTextPosition2)
	{
		uint cPositions = GetMaxTextPosition(pTextContainer);

		// Verify that their offset is within the range of the container.
		if (!(iTextPosition1 <= cPositions))
		{
			return false;
		}
		if (!(iTextPosition2 <= cPositions))
		{
			return false;
		}

		// Verify that the positions are ordered.
		if (!(iTextPosition1 <= iTextPosition2))
		{
			return false;
		}

		return true;
	}

	internal static void IsNotInSurrogateCRLF(
		ITextContainer pContainer,
		uint offset,
		out bool pIsNotInSurrogateCRLF)
	{
		bool isNotInSurrogateCRLF = true; // Assume it's TRUE initially

		// WinUI: IFCEXPECT_RETURN(pContainer != nullptr) — the C# parameter is a non-null reference.
		pContainer.GetPositionCount(out var numPositions);
		MUX_ASSERT(offset <= numPositions);

		pContainer.GetRun(offset, out _, out _, out _, out _, out var characters, out var numCharacters);
		if (numCharacters != 0 && !characters.IsEmpty)
		{
			if (IsTrailingSurrogate(characters.Span[0]))
			{
				isNotInSurrogateCRLF = false;
			}
			// See if we are in the middle of a CR+LF sequence.
			else if (characters.Span[0] == UNICODE_LINE_FEED && offset > 0)
			{
				// There's a LF here.  Go back one character and look for CR.
				pContainer.GetRun(offset - 1, out _, out _, out _, out _, out var lookForCR, out var cLookForCR);
				if (cLookForCR != 0 && !lookForCR.IsEmpty && lookForCR.Span[0] == UNICODE_CARRIAGE_RETURN)
				{
					// We are in the middle of a CR+LF sequence.
					isNotInSurrogateCRLF = false;
				}
			}
		}

		pIsNotInSurrogateCRLF = isNotInSurrogateCRLF;
	}

	//------------------------------------------------------------------------
	//
	//  Method:  SelectWordFromTextPosition
	//
	//  Synopsis:
	//      Given a text position, select the word at that position.  Called
	//  in response to double-clicking in text editor area.
	//
	//------------------------------------------------------------------------
	internal static bool SelectWordFromTextPosition(
		ITextContainer pTextContainer,
		TextPosition hitPosition,
		IJupiterTextSelection pTextSelection,
		FindBoundaryType forwardFindBoundaryType,
		TagConversion tagConversion)
	{
		TextGravity eHitGravity = TextGravity.LineForwardCharacterForward;
		bool isValidPosition = true;

		MUX_ASSERT(forwardFindBoundaryType != FindBoundaryType.Backward);

		if (!GetAdjacentWordSelectionBoundaryPosition(
			pTextContainer,
			hitPosition,
			forwardFindBoundaryType,
			tagConversion,
			out var wordEndTextPosition,
			out eHitGravity))
		{
			return false;
		}

		// We will start the search for the start word boundary one position before the word end
		// (which is the last character of the word we're going to select),
		// as GetAdjacentWordSelectionBoundaryPosition will not move backwards if on a break
		// Since we start at the last character of the to-be-selected word, it will find the appropriate break backwards

		// wordEndTextPosition should be greater than hitPosition unless hitPosition is really the last position
		// in the block. So its previous insertion position will surely be in a position before, unless the block is totally empty
		// (in which case we don't want to select anything)
		if (!wordEndTextPosition.GetPreviousInsertionPosition(out isValidPosition, out hitPosition))
		{
			return false;
		}
		if (isValidPosition)
		{
			if (!GetAdjacentWordSelectionBoundaryPosition(
				pTextContainer,
				hitPosition,
				FindBoundaryType.Backward,
				tagConversion,
				out var wordStartTextPosition,
				out _))
			{
				return false;
			}

			if (!pTextSelection.Select(
				wordStartTextPosition, // Anchor position
				wordEndTextPosition,   // Moving position
				eHitGravity))
			{
				return false;
			}
		}

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   GetClosestNonWhitespaceWordBoundary
	//
	//  Synopsis:
	//      Given a text position, finds the closest non-whitespace word boundary
	//  (ties favor the left boundary)
	//
	//------------------------------------------------------------------------
	internal static bool GetClosestNonWhitespaceWordBoundary(
		ITextContainer pTextContainer,
		TextPosition hitPosition,
		TagConversion tagConversion,
		out TextPosition pClosestPosition)
	{
		pClosestPosition = hitPosition;
		CTextContainerWrapper backend = new(pTextContainer, tagConversion);

		CSelectionWordBreaker.GetAdjacentNonWhitespaceCharacter(hitPosition, backend, FindBoundaryType.Backward, out var leftNonWhitespace);
		CSelectionWordBreaker.GetAdjacentNonWhitespaceCharacter(hitPosition, backend, FindBoundaryType.ForwardExact, out var rightNonWhitespace);

		if (!hitPosition.GetOffset(out var hitOffset) ||
			!leftNonWhitespace.GetOffset(out var leftOffset) ||
			!rightNonWhitespace.GetOffset(out var rightOffset))
		{
			return false;
		}

		MUX_ASSERT(leftOffset <= hitOffset);
		MUX_ASSERT(hitOffset <= rightOffset);

		pClosestPosition = ((hitOffset - leftOffset) <= (rightOffset - hitOffset)) ? leftNonWhitespace : rightNonWhitespace;

		return true;
	}

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Given a text position and desired direction, find the next word boundary
	//  position in the specified direction and determine text gravity.
	//
	//------------------------------------------------------------------------
	internal static bool GetAdjacentWordSelectionBoundaryPosition(
		ITextContainer pTextContainer,
		TextPosition textPosition,
		FindBoundaryType findType,
		TagConversion tagConversion,
		out TextPosition pAdjacentPosition,
		out TextGravity peAdjacentGravity)
	{
		CTextContainerWrapper backend = new(pTextContainer, tagConversion);

		CSelectionWordBreaker.GetAdjacentWordSelectionBoundary(textPosition, backend, findType, out pAdjacentPosition);

		// The gravity is normally the opposite of the direction
		peAdjacentGravity = CSelectionWordBreaker.IsForwardDirection(findType)
			? TextGravity.LineForwardCharacterBackward
			: TextGravity.LineBackwardCharacterForward;

		return true;
	}

	//------------------------------------------------------------------------
	//  Summary:
	//      Wraps an ITextContainer according to the ISimpleTextBackend interface.
	//------------------------------------------------------------------------
	private sealed class CTextContainerWrapper : ISimpleTextBackend
	{
		private readonly ITextContainer m_pTextContainerNoRef;
		private readonly TagConversion m_tagConversion;

		public CTextContainerWrapper(ITextContainer pTextContainer, TagConversion tagConversion)
		{
			m_pTextContainerNoRef = pTextContainer;
			m_tagConversion = tagConversion;
		}

		// Helper to retrieve the character pointed to by the offset from the backing store.
		// NOTE: This always returns a single UTF-16 code unit.
		public char GetCharacter(uint offset)
		{
			// WinUI: ASSERT(m_pTextContainerNoRef != nullptr) — the C# field is a non-null reference.
			char character = UNICODE_NEXT_LINE;

			string text = m_pTextContainerNoRef.GetText(offset, offset + 1, false /*insertNewlines*/);
			if (text.Length == 1)
			{
				character = text[0];
			}
			else if (m_tagConversion != TagConversion.None) // This means we're in the border (open/close) of a tag
			{
				m_pTextContainerNoRef.GetRun(offset, out _, out _, out var nestingType, out var borderElement, out _, out _);
				if (borderElement == null)
				{
					// On success, GetRun shouldn't return a null borderElement, but sometimes it does.
					// The only elements that may do that are BlockCollection and InlineCollection.
					// Considering it a paragraph break is the best fallback here.
					character = UNICODE_PARAGRAPH_SEPARATOR;
				}
				else
				{
					character = borderElement switch
					{
						Paragraph => UNICODE_PARAGRAPH_SEPARATOR,
						LineBreak => UNICODE_LINE_SEPARATOR,
						Run => UNICODE_SPACE,
						InlineUIContainer => (nestingType == TextNestingType.OpenNesting) ? UNICODE_NEXT_LINE : UNICODE_SPACE,
						_ => UNICODE_SPACE,
					};
				}
			}

			return character;
		}
	}

	// MUX Reference text.h Unicode constants.
	private const char UNICODE_NEXT_LINE = '\u0085';
	private const char UNICODE_SPACE = ' ';
	private const char UNICODE_LINE_SEPARATOR = '\u2028';
	private const char UNICODE_PARAGRAPH_SEPARATOR = '\u2029';
}
