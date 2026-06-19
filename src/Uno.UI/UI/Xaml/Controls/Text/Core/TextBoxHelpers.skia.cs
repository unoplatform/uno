// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBoxHelpers.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents.RichTextServices;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Text.Core;

// Read-only subset of CTextBoxHelpers needed by PlainTextPosition. The editable-container
// helpers (InsertText/DeleteText/VerifyPositionPair/GetText buffer copy, etc.) are TextBox-only
// and are not ported for the read-only RichTextBlock path.
internal static class TextBoxHelpers
{
	// Unicode literals matching WinUI's text constants.
	private const char UNICODE_LINE_FEED = '\u000A';
	private const char UNICODE_CARRIAGE_RETURN = '\u000D';

	// #define IS_TRAILING_SURROGATE(character) (((character) & 0xFC00) == 0xDC00)
	private static bool IsTrailingSurrogate(char character) => (character & 0xFC00) == 0xDC00;

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
}
