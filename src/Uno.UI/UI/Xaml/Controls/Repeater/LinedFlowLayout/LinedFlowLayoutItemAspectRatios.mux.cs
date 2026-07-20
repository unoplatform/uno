// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemAspectRatios.cpp, commit b8cfb8490

#nullable enable

using System;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class LinedFlowLayoutItemAspectRatios
{
	// Returns True when there is at least one stored ratio in the provided index range,
	// with a strictly positive weight lower than the provided number.
	internal bool HasLowerWeight(
		int firstItemIndex,
		int lastItemIndex,
		int weight)
	{
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			if (itemAspectRatioBlock.HasLowerWeight(firstItemIndex, lastItemIndex, weight))
			{
				// LINEDFLOWLAYOUT_TRACE_INFO_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"firstItemIndex", firstItemIndex);
				// LINEDFLOWLAYOUT_TRACE_INFO_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"lastItemIndex", lastItemIndex);
				// LINEDFLOWLAYOUT_TRACE_INFO_DBG(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, L"returns True");
				return true;
			}
		}

		// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"firstItemIndex", firstItemIndex);
		// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"lastItemIndex", lastItemIndex);
		// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR, METH_NAME, this, L"returns False");
		return false;
	}

	// Returns False when at least one aspect ratio is being tracked.
	internal bool IsEmpty()
	{
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			if (itemAspectRatioBlock.StartIndex != -1)
			{
				// StartIndex() != -1 indicates that this block tracks at least one aspect ratio.
				return false;
			}
		}

		return true;
	}

	// Returns the average of the weighted aspect ratios. Items outside the provided index range must
	// have a weight equal to the provided number to be included.
	internal float GetAverageAspectRatio(
		int firstItemIndex,
		int lastItemIndex,
		int weight)
	{
		// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"firstItemIndex", firstItemIndex);
		// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"lastItemIndex", lastItemIndex);
		MUX_ASSERT(!IsEmpty());

		var totalAspectRatios = 0.0f;
		var totalWeights = 0;

		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			var itemAspectRatioBlockTotals = itemAspectRatioBlock.GetTotals(firstItemIndex, lastItemIndex, weight);

			totalAspectRatios += itemAspectRatioBlockTotals.AspectRatio;
			totalWeights += itemAspectRatioBlockTotals.Weight;
		}

		if (totalAspectRatios > 0.0f && totalWeights > 0)
		{
			// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"totalAspectRatios", totalAspectRatios);
			// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"totalWeights", totalWeights);
			// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"returned value", totalAspectRatios / totalWeights);
			return totalAspectRatios / totalWeights;
		}

		return 0.0f;
	}

	// Returns the ItemAspectRatio stored for the provided item index, or s_emptyItemAspectRatio
	// when no storage was done for that index.
	internal ItemAspectRatio GetAt(
		int itemIndex)
	{
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			if (itemAspectRatioBlock.IncludesItemIndex(itemIndex))
			{
				return itemAspectRatioBlock.GetAt(itemIndex);
			}
		}

		return s_emptyItemAspectRatio;
	}

	// Stores the ItemAspectRatio for the specified index.
	internal void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
	{
		// Look through the existing blocks to see if one already covers the provided index.
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			if (itemAspectRatioBlock.IncludesItemIndex(itemIndex))
			{
				// Found a match for the provided index. Use that existing block to store the ItemAspectRatio.
				itemAspectRatioBlock.SetAt(itemIndex, itemAspectRatio);
				return;
			}
		}

		// Get a block to store the ItemAspectRatio. GetEmptyOrFurthestBlock either returns a still empty block or the furthest away block from itemIndex.
		var itemAspectRatioBlockToReuse = GetEmptyOrFurthestBlock(itemIndex);

		MUX_ASSERT(itemAspectRatioBlockToReuse != null);

		if (itemAspectRatioBlockToReuse!.StartIndex != -1)
		{
			// Clear the furthest block from itemIndex.
			itemAspectRatioBlockToReuse.Clear();
		}

		// Use it to store the provided ItemAspectRatio.
		// Blocks are such that m_startIndex has to be a multiple of c_blockSize: 0, 64, 128, etc... to avoid having two blocks covering the same item index.
		itemAspectRatioBlockToReuse.StartIndex = (itemIndex / ItemAspectRatioBlock.BlockSize()) * ItemAspectRatioBlock.BlockSize();
		itemAspectRatioBlockToReuse.SetAt(itemIndex, itemAspectRatio);
	}

	// Keeps 4 cleared blocks at most for future use and deletes all the others.
	internal void Clear()
	{
		const int c_minBlockCount = 4;

		// Keep at most c_minBlockCount ItemAspectRatioBlock instances allocated for future use, to avoid reallocations.
		while (m_itemAspectRatioBlocks.Count > c_minBlockCount)
		{
			m_itemAspectRatioBlocks.RemoveAt(0);
		}

		// Each remaining ItemAspectRatioBlock is cleared for future reuse.
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			itemAspectRatioBlock.Clear();
		}

		MUX_ASSERT(IsEmpty());
	}

	// Allocates enough blocks to store 'size' ItemAspectRatio instances. When the required number of blocks
	// is smaller than the already existing ones, the furthest away block(s) from 'referenceItemIndex' are deleted.
	internal void Resize(
		int size,
		int referenceItemIndex)
	{
		// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_INT_INT, METH_NAME, this, size, referenceItemIndex);
		MUX_ASSERT(size >= 1);
		MUX_ASSERT(referenceItemIndex >= 0);

		var requiredBlocks = (int)Math.Ceiling((double)size / ItemAspectRatioBlock.BlockSize());
		var existingBlocks = m_itemAspectRatioBlocks.Count;

		if (requiredBlocks == existingBlocks)
		{
			return;
		}

		// LINEDFLOWLAYOUT_TRACE_INFO(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"size", size);
		// LINEDFLOWLAYOUT_TRACE_INFO(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"referenceItemIndex", referenceItemIndex);
		// LINEDFLOWLAYOUT_TRACE_INFO(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"requiredBlocks", requiredBlocks);
		// LINEDFLOWLAYOUT_TRACE_INFO(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"existingBlocks", existingBlocks);
		if (requiredBlocks > existingBlocks)
		{
			for (var block = existingBlocks + 1; block <= requiredBlocks; block++)
			{
				m_itemAspectRatioBlocks.Add(new ItemAspectRatioBlock());
			}
		}
		else
		{
			for (var block = requiredBlocks; block < existingBlocks; block++)
			{
				var itemAspectRatioBlockToRemove = GetEmptyOrFurthestBlock(referenceItemIndex);

				MUX_ASSERT(itemAspectRatioBlockToRemove != null);

				m_itemAspectRatioBlocks.Remove(itemAspectRatioBlockToRemove!);
			}
		}

		MUX_ASSERT(requiredBlocks == m_itemAspectRatioBlocks.Count);
	}

	// Returns an empty block if one still exists, or else the furthest block away from 'fromItemIndex'.
	private ItemAspectRatioBlock? GetEmptyOrFurthestBlock(
		int fromItemIndex)
	{
		ItemAspectRatioBlock? furthestItemAspectRatioBlock = null;

		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			if (itemAspectRatioBlock.StartIndex == -1)
			{
				return itemAspectRatioBlock;
			}
			else if (furthestItemAspectRatioBlock == null ||
				Math.Abs((furthestItemAspectRatioBlock.StartIndex + furthestItemAspectRatioBlock.EndIndex) / 2 - fromItemIndex) < Math.Abs((itemAspectRatioBlock.StartIndex + itemAspectRatioBlock.EndIndex) / 2 - fromItemIndex))
			{
				furthestItemAspectRatioBlock = itemAspectRatioBlock;
			}
		}

		return furthestItemAspectRatioBlock;
	}

	private partial class ItemAspectRatioBlock
	{
		// Returns True when there is at least one stored ratio in the block, within the provided
		// index range, with a strictly positive weight lower than the provided number.
		internal bool HasLowerWeight(
			int firstItemIndex,
			int lastItemIndex,
			int weight)
		{
			if (m_startIndex == -1)
			{
				return false;
			}

			for (var itemAspectRatioIndex = 0; itemAspectRatioIndex < c_blockSize; itemAspectRatioIndex++)
			{
				var currentWeight = m_aspectRatios[itemAspectRatioIndex].Weight;
				var currentIndex = m_startIndex + itemAspectRatioIndex;

				if (currentWeight > 0 && currentWeight < weight &&
					currentIndex >= firstItemIndex && currentIndex <= lastItemIndex)
				{
					return true;
				}
			}

			return false;
		}

		internal bool IncludesItemIndex(
			int itemIndex) => StartIndex <= itemIndex && EndIndex >= itemIndex;

		// Returns an ItemAspectRatio for which the ratio is the total of the block's weighted ratios, and the weight is the total of
		// the block's weights. Items outside the provided index range must have a weight equal to the provided number to be included.
		internal ItemAspectRatio GetTotals(
			int firstItemIndex,
			int lastItemIndex,
			int weight)
		{
			if (m_startIndex == -1)
			{
				return s_emptyItemAspectRatio;
			}

			var totalAspectRatios = 0.0f;
			var totalWeights = 0;

			for (var itemAspectRatioIndex = 0; itemAspectRatioIndex < c_blockSize; itemAspectRatioIndex++)
			{
				var currentWeight = m_aspectRatios[itemAspectRatioIndex].Weight;
				var currentIndex = m_startIndex + itemAspectRatioIndex;

				if (currentWeight == weight || (currentIndex >= firstItemIndex && currentIndex <= lastItemIndex))
				{
					// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"itemIndex included", currentIndex);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"weight included", currentWeight);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE_DBG(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"aspect ratio included", m_aspectRatios[itemAspectRatioIndex].m_aspectRatio);
					totalAspectRatios += m_aspectRatios[itemAspectRatioIndex].AspectRatio * currentWeight;
					totalWeights += currentWeight;
				}
#if DEBUG
				else if (currentWeight != 0)
				{
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"itemIndex excluded", currentIndex);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"weight excluded", currentWeight);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"aspect ratio excluded", m_aspectRatios[itemAspectRatioIndex].m_aspectRatio);
				}
#endif
			}

			return new ItemAspectRatio(totalAspectRatios, totalWeights);
		}

		// Returns the ItemAspectRatio stored in this block for the provided item index.
		internal ItemAspectRatio GetAt(
			int itemIndex)
		{
			MUX_ASSERT(m_startIndex >= 0);
			MUX_ASSERT(itemIndex >= m_startIndex);
			MUX_ASSERT(itemIndex <= m_startIndex + c_blockSize - 1);

			return m_aspectRatios[itemIndex - m_startIndex];
		}

		// Sets the ItemAspectRatio in this block for the provided item index.
		internal void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
		{
			MUX_ASSERT(m_startIndex >= 0);
			MUX_ASSERT(itemIndex >= m_startIndex);
			MUX_ASSERT(itemIndex <= m_startIndex + c_blockSize - 1);

			m_aspectRatios[itemIndex - m_startIndex] = itemAspectRatio;
		}

		// Clears the block for future reuse.
		internal void Clear()
		{
			m_startIndex = -1;

			for (var itemAspectRatioIndex = 0; itemAspectRatioIndex < c_blockSize; itemAspectRatioIndex++)
			{
				m_aspectRatios[itemAspectRatioIndex] = s_emptyItemAspectRatio;
			}
		}
	}

#if DEBUG
	internal void LogItemAspectRatiosDbg()
	{
		// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"Block count", m_itemAspectRatioBlocks.size());
		foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
		{
			itemAspectRatioBlock.LogItemAspectRatioBlockDbg();
		}
	}

	private partial class ItemAspectRatioBlock
	{
		internal void LogItemAspectRatioBlockDbg()
		{
			// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"m_startIndex", m_startIndex);
			for (var itemAspectRatioIndex = 0; itemAspectRatioIndex < c_blockSize; itemAspectRatioIndex++)
			{
				var aspectRatio = m_aspectRatios[itemAspectRatioIndex].AspectRatio;
				var weight = m_aspectRatios[itemAspectRatioIndex].Weight;

				MUX_ASSERT(!(m_startIndex == -1 && aspectRatio != 0.0f));
				MUX_ASSERT(!(m_startIndex == -1 && weight != 0));

				if (weight != 0)
				{
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"itemIndex", m_startIndex + itemAspectRatioIndex);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this, L"aspectRatio", aspectRatio);
					// LINEDFLOWLAYOUT_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"weight", weight);
				}
			}
		}
	}
#endif
}
