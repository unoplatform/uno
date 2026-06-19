// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlockViewHelpers.h, TextBlockViewHelpers.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Static helpers shared by the text views (RichTextBlockView / TextBlockView) for
// mapping embedded elements to text positions.
internal static class TextBlockViewHelpers
{
	// TextBlocks and RichTextBlocks contain several hidden characters. These constants
	// account for the reserved positions surrounding inlines and spans.
	public const int PlaceHolderPositionsForInlines = 2;
	public const int PlaceHolderOpenSpan = 1;
	public const int PlaceHolderCloseSpan = 1;

	// Takes a number of visible characters (charCount) and translates it to the actual
	// character position (adjusted for hidden positions) in the block.
	// Returns true if adjustment is complete, returns false if there is more to do.
	public static bool AdjustPositionByCharacterCount(
		InlineCollection inlines,
		ref int charCount,
		ref int adjustedPosition)
	{
		foreach (var inl in inlines)
		{
			inl.GetPositionCount(out var curInlineLengthUnsigned);
			int curInlineLength = (int)curInlineLengthUnsigned;

			// Look at Runs, LineBreaks (inlines that can't be nested)
			// to see if the position is contained in them
			if (inl is Run or LineBreak)
			{
				if (charCount == 0)
				{
					adjustedPosition += PlaceHolderPositionsForInlines;
					return true;
				}
				else if (charCount < (curInlineLength - PlaceHolderPositionsForInlines))
				{
					// Position is somewhere in this inline
					adjustedPosition += charCount + PlaceHolderPositionsForInlines;
					return true;
				}
				else
				{
					// Position is in another inline
					charCount -= curInlineLength - PlaceHolderPositionsForInlines;
					// Line separator character is a character, but not accounted for in curInlineLength
					if (inl is LineBreak)
					{
						charCount -= 1;
					}
					adjustedPosition += curInlineLength;
				}
			}
			else if (inl is InlineUIContainer)
			{
				// InlineUIContainer does not have any characters when getting the text from the inline collection.
				// InlineUIContainer only has 2 positions - Open/Close.
				adjustedPosition += curInlineLength;
			}
			else
			{
				// Spans can contain more spans, but eventually will contain a run
				var span = (Span)inl;
				adjustedPosition += PlaceHolderOpenSpan;
				bool adjustedPositionFound =
					AdjustPositionByCharacterCount(
						span.Inlines,
						ref charCount,
						ref adjustedPosition);
				if (adjustedPositionFound)
				{
					return true;
				}
				else
				{
					adjustedPosition += PlaceHolderCloseSpan;
				}
			}
		}

		// If the desired position is the very end of the inlineCollection,
		// we don't yet realize we've found it, but we have.
		inlines.GetPositionCount(out var totalPositions);
		if (charCount == 0 && adjustedPosition == ((int)totalPositions - PlaceHolderPositionsForInlines))
		{
			return true;
		}

		return false;
	}

	// Takes a number of positions (visible and hidden) and returns the character index
	// at that position.
	public static bool AdjustCharacterIndexByPosition(
		InlineCollection inlines,
		ref int charCount,
		ref int position)
	{
		foreach (var inl in inlines)
		{
			inl.GetPositionCount(out var curInlineLengthUnsigned);
			int curInlineLength = (int)curInlineLengthUnsigned;

			// Look at Runs, LineBreaks (inlines that can't be nested)
			// to see if the character index is contained in them
			if (inl is Run or LineBreak)
			{
				if (position <= PlaceHolderPositionsForInlines)
				{
					// Return charCount as is
					return true;
				}
				else if (position <= curInlineLength)
				{
					// Character index is somewhere in this inline
					charCount += position - PlaceHolderPositionsForInlines;
					return true;
				}
				else
				{
					// Character index is in another inline
					position -= curInlineLength;
					charCount += (curInlineLength - PlaceHolderPositionsForInlines);
					// Line separator character is a character, but not accounted for in curInlineLength
					if (inl is LineBreak)
					{
						charCount += 1;
					}
				}
			}
			else if (inl is InlineUIContainer)
			{
				// InlineUIContainer does not have any characters when getting the text from the inline collection.
				// It has 2 positions - Open/Close.
				position -= curInlineLength;
			}
			else
			{
				// Spans can contain more spans, but eventually will contain a run
				var span = (Span)inl;
				position -= PlaceHolderOpenSpan;
				bool charIndexFound =
					AdjustCharacterIndexByPosition(
						span.Inlines,
						ref charCount,
						ref position);
				if (charIndexFound)
				{
					return true;
				}
				else
				{
					position -= PlaceHolderCloseSpan;
				}
			}
		}

		return false;
	}

	// Walks the inline collection accumulating the text offset and returns true (updating
	// positionOfIUC) when the InlineUIContainer is found.
	public static bool FindIUCPositionInInlines(InlineCollection inlines, InlineUIContainer iuc, ref uint positionOfIUC)
	{
		foreach (var inl in inlines)
		{
			inl.GetPositionCount(out var curInlineLength);

			// Count positions in Runs, LineBreaks (inlines that can't be nested)
			if (inl is Run or LineBreak)
			{
				positionOfIUC += curInlineLength;
			}
			else if (inl is InlineUIContainer container)
			{
				// Found the InlineUIContainer we're looking for
				if (container == iuc)
				{
					return true;
				}

				// InlineUIContainer does not have any characters when getting the text from the inline collection.
				// InlineUIContainer only has 2 positions - Open/Close
				positionOfIUC += curInlineLength;
			}
			else
			{
				// Spans can contain more spans
				var span = (Span)inl;
				positionOfIUC += PlaceHolderOpenSpan;

				bool iucFound = FindIUCPositionInInlines(
									span.Inlines,
									iuc,
									ref positionOfIUC);
				if (iucFound)
				{
					return true;
				}
				else
				{
					positionOfIUC += PlaceHolderCloseSpan;
				}
			}
		}

		return false;
	}
}
