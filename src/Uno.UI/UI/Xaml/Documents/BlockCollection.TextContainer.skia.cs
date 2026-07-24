// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BlockCollection.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents;

partial class BlockCollection
{
	// Position count for each contained Block. null until cached by CacheLengths.
	private uint[]? m_pLengths;
	private uint m_length;

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetPositionCount
	//
	//  Synopsis: Returns the sum of the nested block position counts.
	//
	//------------------------------------------------------------------------
	internal void GetPositionCount(out uint pcPositions)
	{
		if (m_pLengths == null)
		{
			CacheLengths();
		}

		pcPositions = m_length;
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetRun
	//
	//  Synopsis: Returns a run of characters all of the same format starting
	//            at the given position.
	//
	//  If the startPosition corresponds to the (reserved) start or end
	//  position of an element, GetRun returns
	//    1) *ppCharacters == NULL,
	//    2) *pFormatting == NULL,
	//    3) *pcCharacters == number of reserved positions (1 for start or end of Inline).
	//
	//  If the startPosition corresponds to a (UTF-16) code unit, GetRun
	//  returns
	//    1) *ppCharacters points to that code unit,
	//    2) *pcCharacters is set to the length of the longest character run
	//       that is contiguous in memory, and for which all code units share
	//       the same formatting,
	//    3) *pFormatting points to the format shared by the code units at
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
		uint blockIndex = 0;
		uint blockOffset = characterPosition;

		if (m_pLengths == null)
		{
			CacheLengths();
		}

		// If the collection is empty, m_pLengths may still be null.
		if (m_pLengths != null)
		{
			MUX_ASSERT(characterPosition < m_length);

			while (blockOffset >= m_pLengths[blockIndex])
			{
				blockOffset -= m_pLengths[blockIndex];
				blockIndex++;
				MUX_ASSERT(blockIndex < GetCount());
			}

			this[(int)blockIndex].GetRun(
				blockOffset,
				out ppTextFormatting,
				out ppInheritedProperties,
				out pNestingType,
				out ppNestedElement,
				out ppCharacters,
				out pcCharacters);
		}
		else
		{
			ppCharacters = ReadOnlyMemory<char>.Empty;
			pcCharacters = 0;

			ppTextFormatting = null;
			ppInheritedProperties = null;
			ppNestedElement = null;

			pNestingType = TextNestingType.NestedContent;
		}
	}

	//------------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::GetContainingElement
	//
	//  Synopsis: return the nested block containing a text position.
	//
	//------------------------------------------------------------------------
	internal void GetContainingElement(
		uint characterPosition,
		out TextElement? ppContainingElement)
	{
		uint blockIndex = 0;
		uint blockOffset = characterPosition;
		TextElement? pElement = null;

		if (m_pLengths == null)
		{
			CacheLengths();
		}

		// If the collection is empty, m_pLengths may still be null.
		if (m_pLengths != null)
		{
			MUX_ASSERT(characterPosition <= m_length);

			if (characterPosition < m_length)
			{
				while (blockOffset >= m_pLengths[blockIndex])
				{
					blockOffset -= m_pLengths[blockIndex];
					blockIndex++;
					MUX_ASSERT(blockIndex < GetCount());
				}

				if (Count > 0)
				{
					this[(int)blockIndex].GetContainingElement(
						blockOffset,
						out pElement);
				}
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
		uint blockIndex = 0;
		bool found = false;
		uint count = GetCount();
		uint offsetInBlock = 0;
		uint blockLength = 0;

		// Start with an offset of 0, BlockCollection doesn't reserve any positions.
		uint offset = 0;

		while (blockIndex < count &&
			!found)
		{
			this[(int)blockIndex].GetElementEdgeOffset(
				pElement,
				edge,
				out offsetInBlock,
				out found);

			if (found)
			{
				offset += offsetInBlock;
				break;
			}
			else
			{
				this[(int)blockIndex].GetPositionCount(out blockLength);
				offset += blockLength;
			}
			blockIndex++;
		}

		pFound = found;
		pOffset = offset;
	}

	// Invalidate the cached block lengths when the collection content changes
	// (CBlockCollection::MarkDirty).
	private protected override void OnCollectionChanged()
	{
		base.OnCollectionChanged();
		ResetLengths();
	}

	// Invalidates the cached block lengths.
	internal void ResetLengths()
	{
		m_pLengths = null;
		m_length = 0;
	}

	//-----------------------------------------------------------------------
	//
	//  Method:   CBlockCollection::CacheLengths
	//
	//  Synopsis: Fill in the array of element lengths, each length
	//  corresponds to one contained CBlock.
	//
	//------------------------------------------------------------------------
	internal void CacheLengths()
	{
		MUX_ASSERT(m_pLengths == null);
		m_length = 0;

		if (Count > 0)
		{
			uint blockCount = GetCount();

			m_pLengths = new uint[blockCount];

			for (uint i = 0; i < blockCount; i++)
			{
				this[(int)i].GetPositionCount(out m_pLengths[i]);
				m_length += m_pLengths[i];
			}
		}
	}
}
