// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class InlineCollection
{
	// Count of positions covered by each nested inline. null until cached by CachePositionCounts.
	private uint[]? m_pPositionCounts;
	private uint m_cCollectionPositions;

	//-----------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::CachePositionCounts
	//
	//  Synopsis: Fill in the array of position counts, each position count
	//  corresponds to one contained CInline.
	//
	//------------------------------------------------------------------------
	internal void CachePositionCounts()
	{
		MUX_ASSERT(m_pPositionCounts == null);

		m_cCollectionPositions = 0;  // Initialize for no inlines present at all

		if (Count > 0)
		{
			uint cInlines = (uint)Count;

			m_pPositionCounts = new uint[cInlines];
			m_cCollectionPositions = 2;  // Reserve two positions for the ends of the collection.

			for (uint i = 0; i < cInlines; i++)
			{
				this[(int)i].GetPositionCount(out m_pPositionCounts[i]);
				m_cCollectionPositions += m_pPositionCounts[i];
			}
		}
	}

	// Invalidates the cached position counts. Called when the content of the collection changes
	// (from InlineCollection.OnCollectionChanged). (CInlineCollection::MarkDirty invalidated the same cache.)
	partial void ResetPositionCountsPartial()
	{
		m_pPositionCounts = null;
		m_cCollectionPositions = 0;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetPositionCount
	//
	//  Synopsis: Returns the sum of the nested inline position counts, plus
	//            one each for the start and end of this collection.
	//
	//------------------------------------------------------------------------
	internal void GetPositionCount(out uint pcPositions)
	{
		if (m_pPositionCounts == null)
		{
			CachePositionCounts();
		}

		pcPositions = m_cCollectionPositions;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetRun
	//
	//  Synopsis: Returns a run of characters all of the same format starting
	//            at the given position.
	//
	//  If the startPosition corresponds to the (reserved) start or end
	//  position of an Inline, GetRun returns
	//    1) *ppCharacters == NULL,
	//    2) *pTextFormatting == NULL,
	//    3) *pcCharacters == number of reserved positions (1 for start or end of Inline).
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
	//------------------------------------------------------------------------
	internal void GetRun(
		uint characterPosition,
		out RichTextServices.TextFormatting? ppTextFormatting,
		out InheritedProperties? ppInheritedProperties,
		out TextNestingType pNestingType,
		out TextElement? ppNestedElement,
		out ReadOnlyMemory<char> ppCharacters,
		out uint pcCharacters)
	{
		if (m_pPositionCounts == null)
		{
			CachePositionCounts();
		}

		MUX_ASSERT(characterPosition <= m_cCollectionPositions);

		if (characterPosition == m_cCollectionPositions)
		{
			// Following the end of the paragraph, return a dummy
			// Unicode paragraph separator character.
			DependencyObject? pParent = GetParentInternal();

			// Only TextBlock is expected to call with the position following end of inline collection.
			TextBlock? pTextBlock = pParent as TextBlock;
			MUX_ASSERT(pTextBlock != null);

			ppCharacters = "\x2029".AsMemory();
			pcCharacters = 1;

			pTextBlock!.GetTextFormatting(out ppTextFormatting);
			pTextBlock!.GetInheritedProperties(out ppInheritedProperties);

			pNestingType = TextNestingType.NestedContent;

			ppNestedElement = null;
		}
		else if (characterPosition == 0
				 || characterPosition == m_cCollectionPositions - 1)
		{
			// If the position corresponds to the start or end of this inline collection,
			// return a reserved run.

			ppCharacters = ReadOnlyMemory<char>.Empty;
			pcCharacters = 1;

			ppTextFormatting = null;
			ppInheritedProperties = null;

			pNestingType = (characterPosition == 0) ? TextNestingType.OpenNesting : TextNestingType.CloseNesting;

			// The owner element is not known to the collection.
			// Hence the caller is responsible for setting ppNestedElement.
			ppNestedElement = null;
		}
		else
		{
			// The requested position is within one of our nested
			// inlines. Determine which.

			uint inlineIndex = 0;  // Index of inline containing requested position
			uint inlineOffset = characterPosition; // Offset into inline

			inlineOffset -= 1; // Allow for initial reserved position

			while (inlineOffset >= m_pPositionCounts![inlineIndex])
			{
				inlineOffset -= m_pPositionCounts[inlineIndex];
				inlineIndex++;
				MUX_ASSERT(inlineIndex < m_cCollectionPositions);
			}

			this[(int)inlineIndex].GetRun(
				inlineOffset,
				out ppTextFormatting,
				out ppInheritedProperties,
				out pNestingType,
				out ppNestedElement,
				out ppCharacters,
				out pcCharacters);
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetContainingElement
	//
	//  Synopsis: return the nested inline containing a text position.
	//
	//------------------------------------------------------------------------
	internal void GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement)
	{
		TextElement? pElement = null;

		if (m_pPositionCounts == null)
		{
			CachePositionCounts();
		}

		MUX_ASSERT(characterPosition <= m_cCollectionPositions);

		if (characterPosition == 0 ||
			characterPosition >= m_cCollectionPositions - 1)
		{
			// If the position corresponds to the start or end of this inline collection (reserved by inline collection),
			// there is no containing element. InlineCollection is not a containing element,
			// it's parent must deal with this case.
			pElement = null;
		}
		else
		{
			// The requested position is within one of our nested
			// inlines. Determine which.
			uint inlineIndex = 0;  // Index of inline containing requested position
			uint inlineOffset = characterPosition; // Offset into inline

			inlineOffset -= 1; // Allow for initial reserved position

			while (inlineOffset >= m_pPositionCounts![inlineIndex])
			{
				inlineOffset -= m_pPositionCounts[inlineIndex];
				inlineIndex++;
				MUX_ASSERT(inlineIndex < m_cCollectionPositions);
			}

			if (Count > 0)
			{
				this[(int)inlineIndex].GetContainingElement(
					inlineOffset,
					out pElement);
			}
		}

		ppContainingElement = pElement;
	}

	internal void GetElementEdgeOffset(
		TextElement pElement,
		ElementEdge edge,
		out uint pOffset,
		out bool pFound)
	{
		uint inlineIndex = 0;
		bool found = false;
		uint count = (uint)Count;
		uint offsetInInline = 0;
		uint inlineLength = 0;

		// Start with an offset of 1 for the reserved start position.
		uint offset = 1;

		if (Count > 0)
		{
			while (inlineIndex < count &&
				   !found)
			{
				this[(int)inlineIndex].GetElementEdgeOffset(
					pElement,
					edge,
					out offsetInInline,
					out found);

				if (found)
				{
					offset += offsetInInline;
					break;
				}
				else
				{
					this[(int)inlineIndex].GetPositionCount(out inlineLength);
					offset += inlineLength;
				}
				inlineIndex++;
			}
		}

		pFound = found;
		pOffset = offset;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetText
	//
	//  Synopsis: Concatenates all text content between 2 offsets into a
	//            flat string.
	//
	//------------------------------------------------------------------------
	internal string GetText(bool insertNewlines)
	{
		GetPositionCount(out var cPositions);
		return GetText(0, cPositions, insertNewlines);
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CInlineCollection::GetText
	//
	//  Synopsis: Concatenates all text content between 2 offsets into a flat string.
	//
	//  NOTE (Uno): WinUI delegated to CTextBoxHelpers::GetText, which walks the run
	//  model between the two positions. That helper is not yet ported; the run-model
	//  walk below reproduces its text-concatenation behavior over GetRun.
	//
	//------------------------------------------------------------------------
	internal string GetText(uint iTextPosition1, uint iTextPosition2, bool insertNewlines)
	{
		GetPositionCount(out var cPositions);

		if (iTextPosition2 > cPositions)
		{
			iTextPosition2 = cPositions;
		}

		var builder = new global::System.Text.StringBuilder();
		uint position = iTextPosition1;

		while (position < iTextPosition2)
		{
			GetRun(
				position,
				out _,
				out _,
				out _,
				out _,
				out var characters,
				out var cCharacters);

			if (cCharacters == 0)
			{
				break;
			}

			if (!characters.IsEmpty)
			{
				uint take = Math.Min(cCharacters, iTextPosition2 - position);
				var span = characters.Span;
				builder.Append(span.Slice(0, (int)Math.Min(take, (uint)span.Length)));
			}

			position += cCharacters;
		}

		return builder.ToString();
	}

	// Returns the owning element of the inline collection (TextBlock, Span or Paragraph).
	// Equivalent to CInlineCollection::GetParentInternal(false).
	private DependencyObject? GetParentInternal()
		=> _collection.GetParent() as DependencyObject;
}
