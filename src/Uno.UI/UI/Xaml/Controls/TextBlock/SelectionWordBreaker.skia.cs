// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SelectionWordBreaker.cpp, SelectionWordBreaker.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Text.Core;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

//------------------------------------------------------------------------
//  Summary:
//      Simple interface that can retrieve characters from a text backend (like an ITextContainer)
//      Exposes the minimal functionality needed by CSelectionWordBreaker
//------------------------------------------------------------------------
internal interface ISimpleTextBackend
{
	char GetCharacter(uint offset);
}

// Backward and ForwardIncludeTrailingWhitespace navigate the "proper" word boundaries,
// which include trailing whitespaces:
//
// "[word1  ][word2 ][word3][,][word4   ]"
//
// ForwardExact will, if the position doesn't start at a whitespace, find the ForwardIncludeTrailingWhitespace
// boundary and move back to trim all the trailing whitespaces.
//
internal enum FindBoundaryType
{
	Backward = 0,
	ForwardIncludeTrailingWhitespace,
	ForwardExact
}

//------------------------------------------------------------------------
//
// CSelectionWordBreaker
//
// A short term substitute for replacing TextBox word navigation algorithm,
// that used to live in CTextView/CTextLineRider classes.
// Needs more work before this can be shared by TextBox/RichTextBox backends.
//
//------------------------------------------------------------------------
internal static class CSelectionWordBreaker
{
	internal static bool IsForwardDirection(FindBoundaryType findType)
		=> findType != FindBoundaryType.Backward;

	//  Returns whether the passed character is considered a punctuation or symbol character
	// MUX Reference: free function IsXamlPunctuationOrSymbol in SelectionWordBreaker.cpp.
	// UcdGetGeneralCategory(...) in [Pc..So] maps exactly to .NET's IsPunctuation || IsSymbol:
	// IsPunctuation covers Pc, Pd, Ps, Pe, Pi, Pf, Po; IsSymbol covers Sm, Sc, Sk, So.
	private static bool IsXamlPunctuationOrSymbol(char character)
		=> char.IsPunctuation(character) || char.IsSymbol(character);

	// MUX Reference text.cpp (IsXamlWhitespace/IsXamlNewline). Ported here as private helpers because
	// the SelectionWordBreaker port is scoped to this file and these are the only callers it needs.
	private static bool IsXamlWhitespace(char character)
		=> character == UNICODE_SPACE
			|| character == UNICODE_CHARACTER_TABULATION
			|| character == UNICODE_FORM_FEED
			|| character == UNICODE_CARRIAGE_RETURN;

	private static bool IsXamlNewline(char character)
		=> character == '\u000A'  // LF
			|| character == '\u000B'  // VT
			|| character == '\u000C'  // FF
			|| character == '\u000D'  // CR
			|| character == '\u2028'  // LS
			|| character == '\u2029'  // PS
			|| character == '\u0085'; // NL

	// The text backing store navigator is used to retrieve text content by walking the content tree.
	// The class supports navigation of both the TextBox and RichText backing stores.
	// Ported as a struct to mirror the by-value parameter passing in the C++ (CanBreak/IsSelectionBreak/
	// IsNavigationBreak take navigators by value, snapshotting their position).
	private struct CTextBackingStoreNavigator
	{
		private readonly ISimpleTextBackend m_pTextBackendNoRef;
		private TextPosition m_navigatorPosition;

		public CTextBackingStoreNavigator(ISimpleTextBackend pTextBackend)
		{
			m_pTextBackendNoRef = pTextBackend;
		}

		// Gets a text character from the backing store or
		// indicate that the position is at a breaking symbol.
		public char GetCharacter()
		{
			char character = UNICODE_NEXT_LINE;

			if (m_navigatorPosition.GetOffset(out var offset))
			{
				character = m_pTextBackendNoRef.GetCharacter(offset);
			}

			return character;
		}

		// Attempts to navigate to the next insertion position such that
		// a text character follows that new position.
		public bool MoveNext()
		{
			bool fIsAtEndOfContainer = false;

			m_navigatorPosition.GetNextInsertionPosition(out var fMoved, out var newPosition);

			if (fMoved)
			{
				m_navigatorPosition = newPosition;
			}
			else
			{
				// GetNextInsertionPosition may return the same position we currently are located even if we're not at the last position, due to an issue in the
				// integration with line services (see BLUE:440033)
				// To work around this, we move to the last position of the block if fMoved is FALSE and we're not there already
				IsAtOrAfterLastPositionOfContainer(out fIsAtEndOfContainer);
				if (!fIsAtEndOfContainer)
				{
					MoveToLastPositionOfContainer();
				}

				fMoved = !fIsAtEndOfContainer;
			}

			return fMoved;
		}

		// Attempts to navigate to the previous insertion position such that
		// a text character follows that new position.
		public bool MovePrevious()
		{
			m_navigatorPosition.GetPreviousInsertionPosition(out var fMoved, out var newPosition);

			if (fMoved)
			{
				m_navigatorPosition = newPosition;
			}

			return fMoved;
		}

		// Return current navigator offset.
		public TextPosition GetPosition() => m_navigatorPosition;

		// Move navigator position to new offset.
		public void MoveTo(TextPosition newPosition) => m_navigatorPosition = newPosition;

		// Check whether the navigator is located at or after the last position of the text container
		private void IsAtOrAfterLastPositionOfContainer(out bool atEnd)
		{
			PlainTextPosition origPlainPosition = m_navigatorPosition.GetPlainPosition();
			ITextContainer? pNoRefContainer = origPlainPosition.GetTextContainer();

			origPlainPosition.GetOffset(out var origOffset);
			pNoRefContainer!.GetPositionCount(out var positionCount);

			atEnd = ((origOffset + 1) >= positionCount);
		}

		// Move the navigator position to the last character of the text container
		private void MoveToLastPositionOfContainer()
		{
			PlainTextPosition origPlainPosition = m_navigatorPosition.GetPlainPosition();
			ITextContainer? pNoRefContainer = origPlainPosition.GetTextContainer();

			pNoRefContainer!.GetPositionCount(out var positionCount);
			origPlainPosition.GetGravity(out var origGravity);

			MUX_ASSERT(positionCount > 0); // Because IsAtOrAfterLastPositionOfContainer will return TRUE and this function should then not be called
			m_navigatorPosition = new TextPosition(new PlainTextPosition(pNoRefContainer, positionCount - 1, origGravity));
		}
	}

	// Given a potential break position and list of valid breaks, returns
	// whether a break for text selection is allowed at that position.
	private static bool IsSelectionBreak(
		char prevChar,
		char breakChar,
		CTextBackingStoreNavigator navigator,
		FindBoundaryType direction,
		List<uint> breakIndexes)
	{
		char curChar = (direction == FindBoundaryType.Backward) ? prevChar : breakChar;
		bool canBreak = IsXamlNewline(curChar) || IsXamlPunctuationOrSymbol(curChar);

		if (!canBreak)
		{
			canBreak = CanBreak(navigator, breakIndexes);
		}

		return canBreak;
	}

	private static bool IsNavigationBreak(
		CTextBackingStoreNavigator navigator,
		CTextBackingStoreNavigator prevNavigator,
		bool fPrevSpace,
		List<uint> breakIndexes,
		bool isNonSpaced)
	{
		if (!isNonSpaced)
		{
			// For English and any other language that uses spaces to separate words
			if (IsPunctuationAtEndOfWord(navigator.GetCharacter(), fPrevSpace)
				|| IsWordStartingWithPunctuation(navigator.GetCharacter(), fPrevSpace, prevNavigator.GetCharacter())
				|| !CanBreak(navigator, breakIndexes))
			{
				return false;
			}
		}
		else
		{
			// For Japanese, Chinese, and any other language that does not use spaces to separate words
			if (!CanBreak(navigator, breakIndexes))
			{
				return false;
			}
		}
		return true;
	}

	// Given a potential break position and list of valid breaks, returns
	// whether a break is allowed at that position.
	// TODO: make this more efficient than having the vector walked from the
	// beginning every time the navigator is checking a break position.
	private static bool CanBreak(
		CTextBackingStoreNavigator navigator,
		List<uint> breakIndexes)
	{
		PlainTextPosition plainPosition = navigator.GetPosition().GetPlainPosition();

		plainPosition.GetOffset(out var curOffsetPosition);

		int characterIndex = plainPosition.GetTextView()!.GetCharacterIndex((int)curOffsetPosition);

		for (int i = 0; i < breakIndexes.Count; ++i)
		{
			if (breakIndexes[i] == characterIndex)
			{
				return true;
			}
		}
		return false;
	}

	// Given a position, find the next "by word" navigation boundary in the given direction.
	//
	// It returns the position right after the break (ie, the word boundary is in between
	// the returned position and the previous one)
	//
	// NOTE: Wordbreaking is split between this function (for TextPattern) and
	// GetAdjacentWordSelectionBoundary (for Text Selection). Changes in one of them might need to
	// be reflected in the other function, depending on the nature of the change
	public static void GetAdjacentWordNavigationBoundary(
		TextPosition currentPosition,
		ISimpleTextBackend pTextBackend,
		FindBoundaryType findType,
		out TextPosition pBoundaryPosition)
	{
		MUX_ASSERT(findType != FindBoundaryType.ForwardExact);

		// fPrevSpace gets set to TRUE when there is one or more whitespace characters between
		// the two consecutive non-whitespace characters we are considering as a break location
		bool fPrevSpace = false;
		bool fMoved;

		CTextBackingStoreNavigator navigator = new(pTextBackend);
		CTextBackingStoreNavigator prevNavigator = new(pTextBackend);

		navigator.MoveTo(currentPosition);

		// Get the container's text
		string containerText = currentPosition.GetPlainPosition().GetTextContainer()!.GetText(false /* insertNewlines */);

		List<uint> breakOffsets = GetTextSegments(containerText, currentPosition.GetPlainPosition().GetTextContainer()!);
		bool isNonSpaced = IsNonSpaceDelimitedLanguage(currentPosition.GetPlainPosition().GetTextContainer()!);

		if (IsForwardDirection(findType))
		{
			// Forwards
			prevNavigator.MoveTo(navigator.GetPosition());
			fMoved = navigator.MoveNext();

			// Move navigator to first non-whitespace character
			// Newlines are considered word breaks
			while (fMoved
				   && !IsXamlNewline(navigator.GetCharacter())
				   && IsXamlWhitespace(navigator.GetCharacter()))
			{
				fPrevSpace = true;
				fMoved = navigator.MoveNext();
			}

			// Consider this position for the line break, and keep going forward if not acceptable.
			// We're always checking two consecutive non-whitespace characters.
			while (fMoved
				   && !IsXamlNewline(navigator.GetCharacter())
				   && !IsNavigationBreak(navigator, prevNavigator, fPrevSpace, breakOffsets, isNonSpaced))
			{
				// Try next position
				prevNavigator.MoveTo(navigator.GetPosition());
				fPrevSpace = false;
				fMoved = navigator.MoveNext(); // Could hit EOL and return FALSE

				// Move navigator to first non-whitespace character
				// Newlines are considered word breaks
				while (fMoved
					   && !IsXamlNewline(navigator.GetCharacter())
					   && IsXamlWhitespace(navigator.GetCharacter()))
				{
					fPrevSpace = true;
					fMoved = navigator.MoveNext(); // Could hit EOL and return FALSE
				}
			}
		}
		else
		{
			// Backwards
			fMoved = navigator.MovePrevious();

			// Move navigator to first non-whitespace character
			// Newlines are considered word breaks
			while (fMoved
				   && !IsXamlNewline(navigator.GetCharacter())
				   && IsXamlWhitespace(navigator.GetCharacter()))
			{
				fMoved = navigator.MovePrevious();    // Could hit BOL and return FALSE
			}

			if (fMoved
				&& !IsXamlNewline(navigator.GetCharacter()))
			{
				prevNavigator.MoveTo(navigator.GetPosition());
				fMoved = prevNavigator.MovePrevious();
				fPrevSpace = false;

				// Consider this position for the line break, and keep going back if not acceptable.
				// We're always checking two consecutive non-whitespace characters.
				while (fMoved
					   && !IsXamlNewline(prevNavigator.GetCharacter())
					   && !IsNavigationBreak(navigator, prevNavigator, fPrevSpace, breakOffsets, isNonSpaced))
				{
					// Try previous position
					navigator.MoveTo(prevNavigator.GetPosition());

					// We need to move navigator to a non-whitespace position, else we can have navigator in a whitespace character,
					// which means CanBreak would return non-desired breaks when there is an intervening whitespace
					// Both navigator and prevNavigator should be at non-whitespace characters
					while (fMoved
						   && IsXamlWhitespace(navigator.GetCharacter()))
					{
						fMoved = navigator.MovePrevious();    // Could hit BOL and return FALSE
					}

					fPrevSpace = false;
					prevNavigator.MoveTo(navigator.GetPosition());
					fMoved = prevNavigator.MovePrevious();

					// Move prevNavigator to first non-whitespace character
					// Newlines are considered word breaks
					while (fMoved
						   && !IsXamlNewline(prevNavigator.GetCharacter())
						   && IsXamlWhitespace(prevNavigator.GetCharacter()))
					{
						fPrevSpace = true;
						fMoved = prevNavigator.MovePrevious();    // We could hit BOL and return FALSE
					}
				}
			}
		}

		pBoundaryPosition = navigator.GetPosition();
	}

	// This help us group punctuation that's separate from a word as its own word.
	// For example, in "a !!! b", "!!!" should be treated as its own word.
	// This is different than punctuation at the end of a word, where it should be grouped with the word.
	// For example: "This is a sentence." The period should be grouped with "sentence".
	private static bool IsPunctuationAtEndOfWord(char character, bool fPrevSpace)
		=> IsXamlPunctuationOrSymbol(character) && fPrevSpace == false;

	// This helps group puntuation at the start of a word with that word.
	// For example, " *typo " or " ?Que "
	private static bool IsWordStartingWithPunctuation(char character, bool fPrevSpace, char prevCharacter)
	{
		// This check should only happen when navigators are on non-whitespace characters.
		MUX_ASSERT(!IsXamlWhitespace(character));

		if (!IsXamlPunctuationOrSymbol(prevCharacter) || fPrevSpace == true)
		{
			return false;
		}
		if (IsXamlPunctuationOrSymbol(character) || IsXamlNewline(character))
		{
			return false;
		}
		return true;
	}

	// Given a position, find the next word boundary for selection in the given direction.
	//
	// It returns the position right after the break (ie, the word boundary is in between
	// the returned position and the previous one)
	//
	// NOTE: Wordbreaking is split between this function (for Text Selection) and
	// GetAdjacentWordNavigationBoundary (for TextPattern). Changes in one of them might need to
	// be reflected in the other function, depending on the nature of the change
	public static void GetAdjacentWordSelectionBoundary(
		TextPosition currentPosition,
		ISimpleTextBackend pTextBackend,
		FindBoundaryType findType,
		out TextPosition pBoundaryPosition)
	{
		// fPrevSpace gets set to TRUE when there is one or more whitespace characters between
		// the two consecutive non-whitespace characters we are considering as a break location.
		// Unlike the navigation variant, the selection break check (IsSelectionBreak) ignores it,
		// so WinUI keeps the assignments but never reads it here — preserved verbatim for fidelity.
#pragma warning disable CS0219 // Variable is assigned but its value is never used
		bool fPrevSpace = false;
#pragma warning restore CS0219
		bool fMoved = true;

		CTextBackingStoreNavigator navigator = new(pTextBackend);
		CTextBackingStoreNavigator prevNavigator = new(pTextBackend);

		navigator.MoveTo(currentPosition);

		// Get the container's text
		string containerText = currentPosition.GetPlainPosition().GetTextContainer()!.GetText(false /* insertNewlines */);

		List<uint> breakOffsets = GetTextSegments(containerText, currentPosition.GetPlainPosition().GetTextContainer()!);

		if (IsForwardDirection(findType))
		{
			// Forwards
			if (IsXamlPunctuationOrSymbol(navigator.GetCharacter()))
			{
				// A contiguous sequence of punctuation signs should be considered a whole word
				while (fMoved
					   && IsXamlPunctuationOrSymbol(navigator.GetCharacter()))
				{
					fMoved = navigator.MoveNext();
				}
			}
			else
			{
				prevNavigator.MoveTo(navigator.GetPosition());

				fMoved = navigator.MoveNext();

				// Move navigator to first non-whitespace character
				// Newlines are considered word breaks
				while (fMoved
					   && !IsXamlNewline(navigator.GetCharacter())
					   && IsXamlWhitespace(navigator.GetCharacter()))
				{
					fPrevSpace = true;
					fMoved = navigator.MoveNext();
				}

				// Consider this position for the word break, and keep going
				// forward if not acceptable. We're always checking two consecutive non-whitespace characters (except in the first iteration if we started at whitespace)
				while (fMoved
						&& !IsSelectionBreak(
							prevNavigator.GetCharacter(),
							navigator.GetCharacter(),
							navigator,
							findType,
							breakOffsets))
				{
					// Try next position
					prevNavigator.MoveTo(navigator.GetPosition());
					fPrevSpace = false;
					fMoved = navigator.MoveNext(); // Could hit EOL and return FALSE

					// Move navigator to first non-whitespace character
					// Newlines are considered word breaks
					while (fMoved
						   && !IsXamlNewline(navigator.GetCharacter())
						   && IsXamlWhitespace(navigator.GetCharacter()))
					{
						fPrevSpace = true;
						fMoved = navigator.MoveNext(); // Could hit EOL and return FALSE
					}
				}

				// If we want an exact boundary, move to right after prevNavigator if it isn't in whitespace
				// (because it is the last non-whitespace before the break if it isn't whitespace)
				// (if it is whitespace, it is because we started at it and we want to stop at the actual break)
				if ((findType == FindBoundaryType.ForwardExact) && !IsXamlWhitespace(prevNavigator.GetCharacter()))
				{
					navigator.MoveTo(prevNavigator.GetPosition());
					navigator.MoveNext();
				}
			}
		}
		else
		{
			// Backwards
			if (IsXamlPunctuationOrSymbol(navigator.GetCharacter()))
			{
				// A contiguous sequence of punctuation signs should be considered a whole word
				while (fMoved
					   && IsXamlPunctuationOrSymbol(navigator.GetCharacter()))
				{
					fMoved = navigator.MovePrevious();
				}

				if (fMoved)
				{
					navigator.MoveNext();
				}
			}
			else
			{
				// Move navigator to first non-whitespace character
				// Newlines are considered word breaks
				while (fMoved
					   && !IsXamlNewline(navigator.GetCharacter())
					   && IsXamlWhitespace(navigator.GetCharacter()))
				{
					fMoved = navigator.MovePrevious();    // Could hit BOL and return FALSE
				}

				if (fMoved
					&& !IsXamlNewline(navigator.GetCharacter()))
				{
					prevNavigator.MoveTo(navigator.GetPosition());
					fMoved = prevNavigator.MovePrevious();
					fPrevSpace = false;

					// Consider this position for the word break, and keep going
					// back if not acceptable. We're always checking two consecutive non-whitespace characters
					while (fMoved
							&& !IsSelectionBreak(
								prevNavigator.GetCharacter(),
								navigator.GetCharacter(),
								navigator,
								findType,
								breakOffsets))
					{
						// Try previous position
						navigator.MoveTo(prevNavigator.GetPosition());
						fPrevSpace = false;
						fMoved = prevNavigator.MovePrevious();

						// Move navigator to first non-whitespace character
						// Newlines are considered word breaks
						while (fMoved
							   && !IsXamlNewline(prevNavigator.GetCharacter())
							   && IsXamlWhitespace(prevNavigator.GetCharacter()))
						{
							fPrevSpace = true;
							fMoved = prevNavigator.MovePrevious();    // We could hit BOL and return FALSE
						}
					}
				}
			}
		}

		pBoundaryPosition = navigator.GetPosition();
	}

	// Given a position, find the next non-whitespace character in
	// the given direction. Newlines here will also be considered whitespace
	public static void GetAdjacentNonWhitespaceCharacter(
		TextPosition currentPosition,
		ISimpleTextBackend pTextBackend,
		FindBoundaryType findType,
		out TextPosition pBoundaryPosition)
	{
		bool fMoved = true;
		CTextBackingStoreNavigator navigator = new(pTextBackend);
		navigator.MoveTo(currentPosition);

		if (IsForwardDirection(findType))
		{
			// Forwards
			while (fMoved
				   && (IsXamlNewline(navigator.GetCharacter())
					 || IsXamlWhitespace(navigator.GetCharacter())))
			{
				fMoved = navigator.MoveNext();
			}
		}
		else
		{
			// Backwards
			while (fMoved
				   && (IsXamlNewline(navigator.GetCharacter())
					 || IsXamlWhitespace(navigator.GetCharacter())))
			{
				fMoved = navigator.MovePrevious();
			}
		}

		pBoundaryPosition = navigator.GetPosition();
	}

	private static void GetLanguage(ITextContainer pTextContainer, out string language)
	{
		// WinUI reads FrameworkElement.Language off the owner UIElement via GetValueByIndex.
		language = (pTextContainer.GetOwnerUIElement() as FrameworkElement)?.Language ?? string.Empty;
		if (string.IsNullOrEmpty(language))
		{
			// If we can't determine a language, use "und" (undetermined) to use Unicode Standard language-neutral rules.
			language = "und";
		}
	}

	private static bool IsNonSpaceDelimitedLanguage(ITextContainer pTextContainer)
	{
		GetLanguage(pTextContainer, out var language);

		if (language.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ||  // Japanese
			language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||  // Chinese
			language.StartsWith("th", StringComparison.OrdinalIgnoreCase) ||  // Thai
			language.StartsWith("ko-", StringComparison.OrdinalIgnoreCase) || // Korean -- Need to differentiate from Konkani (kok)
			string.Equals(language, "ko", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

	// Uses a SelectableWordsSegmenter to break the text into selectable segments,
	// then adds each character offset where there can be a break to a list.
	// A SelectableWordsSegmenter includes the trailing space after a word, whereas
	// a WordsSegmenter would not. This is the behavior we want in both navigation and selection.
	//
	// Uno substitution: WinUI backs this with Windows.Data.Text.ISelectableWordsSegmenter (a
	// language-aware ICU word segmenter that bundles trailing spaces into each segment). Uno's
	// cross-platform text stack does not expose that segmenter as a public API, so we reproduce the
	// SelectableWordsSegmenter behavior for space-delimited (Latin) languages directly off the
	// character classification helpers used everywhere else in this port: a selectable word segment
	// runs from a non-whitespace start through its trailing whitespace, ending right before the next
	// non-whitespace run. The resulting break offsets match the segmenter's for the space-delimited
	// path that IsNonSpaceDelimitedLanguage flags as spaced.
	//
	// TODO Uno (Stage 7 P1): for non-space-delimited languages (CJK/Thai) WinUI relies on ICU
	// dictionary segmentation via ISelectableWordsSegmenter. Wire a real ICU word-break segmenter
	// (see Microsoft.UI.Xaml.Documents.UnicodeText's ICU UBRK_WORD usage) and remove this fallback.
	private static List<uint> GetTextSegments(
		string containerText,
		ITextContainer pTextContainer)
	{
		List<uint> breakOffsets = new() { 0 };

		int length = containerText.Length;
		int i = 0;
		while (i < length)
		{
			// Skip the word's non-whitespace run.
			while (i < length && !IsSelectableWordSpace(containerText[i]))
			{
				i++;
			}
			// Bundle the trailing whitespace into the same segment, like SelectableWordsSegmenter does.
			while (i < length && IsSelectableWordSpace(containerText[i]))
			{
				i++;
			}
			breakOffsets.Add((uint)i);
		}

		return breakOffsets;
	}

	// Whitespace that delimits selectable word segments. Matches IsXamlWhitespace plus the
	// newline forms, so a word segment never spans a line break.
	private static bool IsSelectableWordSpace(char character)
		=> IsXamlWhitespace(character) || IsXamlNewline(character);

	// MUX Reference text.h Unicode constants.
	private const char UNICODE_CHARACTER_TABULATION = '\u0009';
	private const char UNICODE_FORM_FEED = '\u000C';
	private const char UNICODE_CARRIAGE_RETURN = '\u000D';
	private const char UNICODE_SPACE = '\u0020';
	private const char UNICODE_NEXT_LINE = '\u0085';
}
