// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference CRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class Run
{
	//------------------------------------------------------------------------
	//
	//  Method:   CRun::GetTextLength
	//
	//------------------------------------------------------------------------
	private int GetTextLength() => Text?.Length ?? 0;

	//------------------------------------------------------------------------
	//
	//  Method:   CRun::GetPositionCount
	//
	//  Synopsis: Returns the number of character positions in this run,
	//            including one cp each for start and end of the run.
	//
	//------------------------------------------------------------------------
	internal override void GetPositionCount(out uint pcPositions)
		=> pcPositions = (uint)GetTextLength() + 2;

	//------------------------------------------------------------------------
	//
	//  Method:   CRun::GetText
	//
	//------------------------------------------------------------------------
	private void GetText(out ReadOnlyMemory<char> ppCharacters, out uint pcCharacters)
	{
		ppCharacters = ReadOnlyMemory<char>.Empty;
		pcCharacters = 0;

		if (Text is { Length: > 0 } text)
		{
			pcCharacters = (uint)text.Length;
			ppCharacters = text.AsMemory();
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CRun::GetRun
	//
	//  Synopsis: Returns a run of characters all of the same format starting
	//            at the given position.
	//
	//  If the startPosition corresponds to the (reserved) start or end
	//  position of an Inline, GetRun returns
	//    1) *ppCharacters == NULL,
	//    2) *pcCharacters == number of reserved positions (1 for start or end of Inline).
	//    3) if there is any text (i.e. cCharacters>2) then *ppTextFormatting is set to
	//       null. In the special case that there is no text, just reserved
	//       positions, then ppTextFormatting is resolved for position zero just as it
	//       is for text below.
	//
	//  If the startPosition corresponds to a (UTF-16) code unit, GetRun
	//  returns
	//    1) *ppCharacters points to that code unit,
	//    2) *pcCharacters is set to the length of the longest character run
	//       that is contiguous in memory, and for which all code units share
	//       the same formatting,
	//    3) *pTextFormatting points to the format shared by the code units at
	//       *ppCharacters. Inheritance of formatting properties is already
	//       resolved.
	//
	//   If the start position is beyond the end of the collection, fail.
	//
	//   Note that resolving formatting may involve walking the tree for
	//   inheritance and parsing properties such as language, so we avoid
	//   resolving formatting more than once per run in normal formatting
	//   scenarios.
	//
	//------------------------------------------------------------------------
	internal override void GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
	{
		GetPositionCount(out var cPositions);
		MUX_ASSERT(characterPosition < cPositions);

		if (characterPosition == 0
			|| characterPosition == cPositions - 1)
		{
			// If the position corresponds to the start or end of this run,
			// return a reserved run.

			ppCharacters = ReadOnlyMemory<char>.Empty;
			pcCharacters = 1;

			ppNestedElement = this;

			pNestingType = (characterPosition == 0) ? TextNestingType.OpenNesting : TextNestingType.CloseNesting;
		}
		else
		{
			// The requested position is within our string.

			GetText(out ppCharacters, out pcCharacters);

			// If we are here, we must have a non-empty run.

			MUX_ASSERT(pcCharacters > 0);
			MUX_ASSERT(!ppCharacters.IsEmpty);

			// Calculations below take into account the single reserved
			// character positions before and after the content text.

			ppCharacters = ppCharacters.Slice((int)(characterPosition - 1));
			pcCharacters = cPositions - characterPosition - 1;

			ppNestedElement = this;

			pNestingType = TextNestingType.NestedContent;
		}

		GetTextFormatting(out ppTextFormatting);
		GetInheritedProperties(out ppInheritedProperties);
	}
}
