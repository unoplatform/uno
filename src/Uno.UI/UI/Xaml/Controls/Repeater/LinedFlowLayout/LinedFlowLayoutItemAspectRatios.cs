// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemAspectRatios.cpp, tag winui3/release/1.8.4

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// This class is used to track weighted aspect ratios of some LinedFlowLayout items.
/// Blocks tracking 64 contiguous items are allocated. To minimize allocations,
/// those blocks are re-used when a different area of the items collection needs to be tracked.
/// </summary>
internal sealed class LinedFlowLayoutItemAspectRatios
{
	public struct ItemAspectRatio
	{
		public float AspectRatio;
		public int Weight;

		public ItemAspectRatio(float aspectRatio, int weight)
		{
			AspectRatio = aspectRatio;
			Weight = weight;
		}
	}

	public static readonly ItemAspectRatio EmptyItemAspectRatio = new(0.0f, 0);

	private readonly List<ItemAspectRatioBlock> _itemAspectRatioBlocks = new();

	/// <summary>
	/// Returns True when there is at least one stored ratio in the provided index range,
	/// with a strictly positive weight lower than the provided number.
	/// </summary>
	public bool HasLowerWeight(int firstItemIndex, int lastItemIndex, int weight)
	{
		foreach (var block in _itemAspectRatioBlocks)
		{
			if (block.HasLowerWeight(firstItemIndex, lastItemIndex, weight))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Returns False when at least one aspect ratio is being tracked.
	/// </summary>
	public bool IsEmpty()
	{
		foreach (var block in _itemAspectRatioBlocks)
		{
			if (block.StartIndex != -1)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Returns the average of the weighted aspect ratios.
	/// </summary>
	public float GetAverageAspectRatio(int firstItemIndex, int lastItemIndex, int weight)
	{
		float totalAspectRatios = 0.0f;
		int totalWeights = 0;

		foreach (var block in _itemAspectRatioBlocks)
		{
			var totals = block.GetTotals(firstItemIndex, lastItemIndex, weight);
			totalAspectRatios += totals.AspectRatio;
			totalWeights += totals.Weight;
		}

		if (totalAspectRatios > 0.0f && totalWeights > 0)
		{
			return totalAspectRatios / totalWeights;
		}

		return 0.0f;
	}

	/// <summary>
	/// Returns the ItemAspectRatio stored for the provided item index, or EmptyItemAspectRatio
	/// when no storage was done for that index.
	/// </summary>
	public ItemAspectRatio GetAt(int itemIndex)
	{
		foreach (var block in _itemAspectRatioBlocks)
		{
			if (block.IncludesItemIndex(itemIndex))
			{
				return block.GetAt(itemIndex);
			}
		}
		return EmptyItemAspectRatio;
	}

	/// <summary>
	/// Stores the ItemAspectRatio for the specified index.
	/// </summary>
	public void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
	{
		// Look through the existing blocks to see if one already covers the provided index.
		foreach (var block in _itemAspectRatioBlocks)
		{
			if (block.IncludesItemIndex(itemIndex))
			{
				block.SetAt(itemIndex, itemAspectRatio);
				return;
			}
		}

		// Get a block to store the ItemAspectRatio.
		var blockToReuse = GetEmptyOrFurthestBlock(itemIndex);

		if (blockToReuse.StartIndex != -1)
		{
			blockToReuse.Clear();
		}

		// Use it to store the provided ItemAspectRatio.
		blockToReuse.StartIndex = (itemIndex / ItemAspectRatioBlock.BlockSize) * ItemAspectRatioBlock.BlockSize;
		blockToReuse.SetAt(itemIndex, itemAspectRatio);
	}

	/// <summary>
	/// Keeps 4 cleared blocks at most for future use and deletes all the others.
	/// </summary>
	public void Clear()
	{
		const int minBlockCount = 4;

		while (_itemAspectRatioBlocks.Count > minBlockCount)
		{
			_itemAspectRatioBlocks.RemoveAt(0);
		}

		foreach (var block in _itemAspectRatioBlocks)
		{
			block.Clear();
		}
	}

	/// <summary>
	/// Allocates enough blocks to store 'size' ItemAspectRatio instances.
	/// </summary>
	public void Resize(int size, int referenceItemIndex)
	{
		int requiredBlocks = (int)Math.Ceiling((double)size / ItemAspectRatioBlock.BlockSize);
		int existingBlocks = _itemAspectRatioBlocks.Count;

		if (requiredBlocks == existingBlocks)
		{
			return;
		}

		if (requiredBlocks > existingBlocks)
		{
			for (int block = existingBlocks + 1; block <= requiredBlocks; block++)
			{
				_itemAspectRatioBlocks.Add(new ItemAspectRatioBlock());
			}
		}
		else
		{
			for (int block = requiredBlocks; block < existingBlocks; block++)
			{
				var blockToRemove = GetEmptyOrFurthestBlock(referenceItemIndex);
				_itemAspectRatioBlocks.Remove(blockToRemove);
			}
		}
	}

	private ItemAspectRatioBlock GetEmptyOrFurthestBlock(int fromItemIndex)
	{
		ItemAspectRatioBlock furthestBlock = null;

		foreach (var block in _itemAspectRatioBlocks)
		{
			if (block.StartIndex == -1)
			{
				return block;
			}

			if (furthestBlock == null ||
				Math.Abs((furthestBlock.StartIndex + furthestBlock.EndIndex) / 2 - fromItemIndex) <
				Math.Abs((block.StartIndex + block.EndIndex) / 2 - fromItemIndex))
			{
				furthestBlock = block;
			}
		}

		return furthestBlock;
	}

	private sealed class ItemAspectRatioBlock
	{
		public const int BlockSize = 64;

		private readonly ItemAspectRatio[] _aspectRatios = new ItemAspectRatio[BlockSize];

		public int StartIndex { get; set; } = -1;

		public int EndIndex => StartIndex == -1 ? -1 : StartIndex + BlockSize - 1;

		public ItemAspectRatio GetTotals(int firstItemIndex, int lastItemIndex, int weight)
		{
			if (StartIndex == -1)
			{
				return EmptyItemAspectRatio;
			}

			float totalAspectRatios = 0.0f;
			int totalWeights = 0;

			for (int i = 0; i < BlockSize; i++)
			{
				int currentWeight = _aspectRatios[i].Weight;
				int currentIndex = StartIndex + i;

				if (currentWeight == weight || (currentIndex >= firstItemIndex && currentIndex <= lastItemIndex))
				{
					totalAspectRatios += _aspectRatios[i].AspectRatio * currentWeight;
					totalWeights += currentWeight;
				}
			}

			return new ItemAspectRatio(totalAspectRatios, totalWeights);
		}

		public bool HasLowerWeight(int firstItemIndex, int lastItemIndex, int weight)
		{
			if (StartIndex == -1)
			{
				return false;
			}

			for (int i = 0; i < BlockSize; i++)
			{
				int currentWeight = _aspectRatios[i].Weight;
				int currentIndex = StartIndex + i;

				if (currentWeight > 0 && currentWeight < weight &&
					currentIndex >= firstItemIndex && currentIndex <= lastItemIndex)
				{
					return true;
				}
			}

			return false;
		}

		public bool IncludesItemIndex(int itemIndex)
		{
			return StartIndex <= itemIndex && EndIndex >= itemIndex;
		}

		public ItemAspectRatio GetAt(int itemIndex)
		{
			return _aspectRatios[itemIndex - StartIndex];
		}

		public void SetAt(int itemIndex, ItemAspectRatio itemAspectRatio)
		{
			_aspectRatios[itemIndex - StartIndex] = itemAspectRatio;
		}

		public void Clear()
		{
			StartIndex = -1;
			for (int i = 0; i < BlockSize; i++)
			{
				_aspectRatios[i] = EmptyItemAspectRatio;
			}
		}
	}
}
