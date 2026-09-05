// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayoutItemAspectRatios.h, commit b8cfb8490

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

// This class is used to track weighted aspect ratios of some LinedFlowLayout items. Blocks tracking 64 contiguous items
// are allocated. To minimize allocations, those blocks are re-used when a different area of the items collection needs
// to be tracked.
//
// Example:
// Initially blocks [0,63][64,127] track the first items of 5700 items i the collection.  The user uses the PageDown key a
// few times and the tracking blocks are extended to [0,63][64,127][128,191][192,255].  Then the user hits the End key
// causing the blocks to track [0,63][64,127][128,191][192,255][5632,5695][5696,5759].  Finally the user keeps hitting the
// PageUp key to the middle of the collection and the blocks become:
// [2624,2687][2688,2751][2752,2815][2816,2879][2880,2943][2944,3007][3008,3071].
//
// The consumer of this class is in charge of setting a limit to the number of items being tracked. In this case the LinedFlowLayout
// sets the limit to 10 scrolling viewports worth of items. If on average a scrolling viewport can hold 40 items, no more than
// 7 blocks (i.e. 400 / 64) will be allocated to store aspect ratios of 400 items at a time.
//
// As the user jumps from area to area in the items collection, the closest existing blocks from the new area are preserved,
// while the furthest away are recycled.
partial class LinedFlowLayoutItemAspectRatios
{
	internal LinedFlowLayoutItemAspectRatios()
	{
	}

	internal readonly struct ItemAspectRatio
	{
		internal ItemAspectRatio(float aspectRatio, int weight)
		{
			AspectRatio = aspectRatio;
			Weight = weight;
		}

		internal float AspectRatio { get; }

		internal int Weight { get; }
	}

	private static readonly ItemAspectRatio s_emptyItemAspectRatio = new(
		0.0f /*aspectRatio*/,
		0 /*weight*/);

	// #ifdef DBG
	// void LogItemAspectRatiosDbg();
	// #endif

	private partial class ItemAspectRatioBlock
	{
		internal ItemAspectRatioBlock()
		{
		}

		internal int StartIndex
		{
			get => m_startIndex;
			set => m_startIndex = value;
		}

		internal int EndIndex => m_startIndex == -1 ? -1 : m_startIndex + c_blockSize - 1;

		internal static int BlockSize() => c_blockSize;

		// #ifdef DBG
		// void LogItemAspectRatioBlockDbg();
		// #endif

		private const int c_blockSize = 64;

		private readonly ItemAspectRatio[] m_aspectRatios = new ItemAspectRatio[c_blockSize];
		private int m_startIndex = -1;
	}

	// WinUI stores the blocks in a std::set<std::shared_ptr<ItemAspectRatioBlock>>. Every consumer iterates
	// all blocks (the aggregations are order-independent sums) and block reuse/removal is always chosen
	// explicitly by GetEmptyOrFurthestBlock, so a List reproduces WinUI's behavior without relying on the
	// set's pointer-ordering.
	private readonly List<ItemAspectRatioBlock> m_itemAspectRatioBlocks = new();
}
