// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemAspectRatios.cpp, commit b8cfb8490

#nullable enable

using System;
using System.Collections.Generic;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Tracks weighted aspect ratios of some LinedFlowLayout items. Blocks tracking 64 contiguous items
	/// are allocated and re-used when a different area of the items collection needs to be tracked, so as
	/// the user jumps around the collection the closest existing blocks to the new area are preserved while
	/// the furthest away are recycled. The consumer (LinedFlowLayout) caps how many items are tracked.
	/// </summary>
	internal class LinedFlowLayoutItemAspectRatios
	{
		internal readonly struct ItemAspectRatio
		{
			public ItemAspectRatio(float aspectRatio, int weight)
			{
				AspectRatio = aspectRatio;
				Weight = weight;
			}

			public float AspectRatio { get; }
			public int Weight { get; }
		}

		internal static readonly ItemAspectRatio s_emptyItemAspectRatio = new(0.0f /*aspectRatio*/, 0 /*weight*/);

		// WinUI stores the blocks in a std::set<std::shared_ptr<ItemAspectRatioBlock>>. Every consumer iterates
		// all blocks (the aggregations are order-independent sums) and block reuse/removal is always chosen
		// explicitly by GetEmptyOrFurthestBlock, so a List reproduces WinUI's behavior without relying on the
		// set's pointer-ordering.
		private readonly List<ItemAspectRatioBlock> m_itemAspectRatioBlocks = new();

		// Returns True when there is at least one stored ratio in the provided index range,
		// with a strictly positive weight lower than the provided number.
		public bool HasLowerWeight(
			int firstItemIndex,
			int lastItemIndex,
			int weight)
		{
			foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
			{
				if (itemAspectRatioBlock.HasLowerWeight(firstItemIndex, lastItemIndex, weight))
				{
					return true;
				}
			}

			return false;
		}

		// Returns False when at least one aspect ratio is being tracked.
		public bool IsEmpty()
		{
			foreach (var itemAspectRatioBlock in m_itemAspectRatioBlocks)
			{
				if (itemAspectRatioBlock.StartIndex != -1)
				{
					// StartIndex != -1 indicates that this block tracks at least one aspect ratio.
					return false;
				}
			}

			return true;
		}

		// Returns the average of the weighted aspect ratios. Items outside the provided index range must
		// have a weight equal to the provided number to be included.
		public float GetAverageAspectRatio(
			int firstItemIndex,
			int lastItemIndex,
			int weight)
		{
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
				return totalAspectRatios / totalWeights;
			}

			return 0.0f;
		}

		// Returns the ItemAspectRatio stored for the provided item index, or s_emptyItemAspectRatio
		// when no storage was done for that index.
		public ItemAspectRatio GetAt(
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
		public void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
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

			// Get a block to store the ItemAspectRatio. GetEmptyOrFurthestBlock either returns a still empty block
			// or the furthest away block from itemIndex.
			var itemAspectRatioBlockToReuse = GetEmptyOrFurthestBlock(itemIndex);

			MUX_ASSERT(itemAspectRatioBlockToReuse != null);

			if (itemAspectRatioBlockToReuse!.StartIndex != -1)
			{
				// Clear the furthest block from itemIndex.
				itemAspectRatioBlockToReuse.Clear();
			}

			// Use it to store the provided ItemAspectRatio.
			// Blocks are such that StartIndex has to be a multiple of c_blockSize: 0, 64, 128, etc... to avoid
			// having two blocks covering the same item index.
			itemAspectRatioBlockToReuse.StartIndex = (itemIndex / ItemAspectRatioBlock.BlockSize()) * ItemAspectRatioBlock.BlockSize();
			itemAspectRatioBlockToReuse.SetAt(itemIndex, itemAspectRatio);
		}

		// Keeps 4 cleared blocks at most for future use and deletes all the others.
		public void Clear()
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
		public void Resize(
			int size,
			int referenceItemIndex)
		{
			MUX_ASSERT(size >= 1);
			MUX_ASSERT(referenceItemIndex >= 0);

			var requiredBlocks = (int)Math.Ceiling((double)size / ItemAspectRatioBlock.BlockSize());
			var existingBlocks = m_itemAspectRatioBlocks.Count;

			if (requiredBlocks == existingBlocks)
			{
				return;
			}

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

		private class ItemAspectRatioBlock
		{
			private const int c_blockSize = 64;

			private readonly ItemAspectRatio[] m_aspectRatios = new ItemAspectRatio[c_blockSize];
			private int m_startIndex = -1;

			public int StartIndex
			{
				get => m_startIndex;
				set => m_startIndex = value;
			}

			public int EndIndex => m_startIndex == -1 ? -1 : m_startIndex + c_blockSize - 1;

			public static int BlockSize() => c_blockSize;

			// Returns True when there is at least one stored ratio in the block, within the provided
			// index range, with a strictly positive weight lower than the provided number.
			public bool HasLowerWeight(
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

			public bool IncludesItemIndex(
				int itemIndex) => StartIndex <= itemIndex && EndIndex >= itemIndex;

			// Returns an ItemAspectRatio for which the ratio is the total of the block's weighted ratios, and the
			// weight is the total of the block's weights. Items outside the provided index range must have a weight
			// equal to the provided number to be included.
			public ItemAspectRatio GetTotals(
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
						totalAspectRatios += m_aspectRatios[itemAspectRatioIndex].AspectRatio * currentWeight;
						totalWeights += currentWeight;
					}
				}

				return new ItemAspectRatio(totalAspectRatios, totalWeights);
			}

			// Returns the ItemAspectRatio stored in this block for the provided item index.
			public ItemAspectRatio GetAt(
				int itemIndex)
			{
				MUX_ASSERT(m_startIndex >= 0);
				MUX_ASSERT(itemIndex >= m_startIndex);
				MUX_ASSERT(itemIndex <= m_startIndex + c_blockSize - 1);

				return m_aspectRatios[itemIndex - m_startIndex];
			}

			// Sets the ItemAspectRatio in this block for the provided item index.
			public void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
			{
				MUX_ASSERT(m_startIndex >= 0);
				MUX_ASSERT(itemIndex >= m_startIndex);
				MUX_ASSERT(itemIndex <= m_startIndex + c_blockSize - 1);

				m_aspectRatios[itemIndex - m_startIndex] = itemAspectRatio;
			}

			// Clears the block for future reuse.
			public void Clear()
			{
				m_startIndex = -1;

				for (var itemAspectRatioIndex = 0; itemAspectRatioIndex < c_blockSize; itemAspectRatioIndex++)
				{
					m_aspectRatios[itemAspectRatioIndex] = s_emptyItemAspectRatio;
				}
			}
		}
	}
}
