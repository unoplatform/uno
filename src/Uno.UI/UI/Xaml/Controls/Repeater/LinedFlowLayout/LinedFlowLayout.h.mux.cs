// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.h, commit b8cfb8490

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	partial class LinedFlowLayout
	{
		// Properties' default values.
		private const LinedFlowLayoutItemsJustification s_defaultItemsJustification = LinedFlowLayoutItemsJustification.Start;
		private const LinedFlowLayoutItemsStretch s_defaultItemsStretch = LinedFlowLayoutItemsStretch.None;
		private const double s_defaultActualLineHeight = 0.0;
		private const double s_defaultLineSpacing = 0.0;
		private const double s_defaultLineHeight = double.NaN;
		private const double s_defaultMinItemSpacing = 0.0;

		// #pragma region ILayoutOverrides
		// #pragma endregion

		// #pragma region IVirtualizingLayoutOverrides
		// #pragma endregion

		internal void SetDesiredAspectRatios(double[] values) => m_itemsInfoDesiredAspectRatiosForFastPath = (double[])values.Clone();

		internal void SetMinWidths(double[] values) => m_itemsInfoMinWidthsForFastPath = (double[])values.Clone();

		internal void SetMaxWidths(double[] values) => m_itemsInfoMaxWidthsForFastPath = (double[])values.Clone();

		// #pragma region LayoutsTestHooks

		internal int FirstRealizedItemIndexDbg()
		{
			if (!m_wasInitializedForContext)
			{
				return -1;
			}

			return m_elementManager.GetFirstRealizedDataIndex();
		}

		internal int LastRealizedItemIndexDbg()
		{
			if (!m_wasInitializedForContext)
			{
				return -1;
			}

			int realizedElementCount = m_elementManager.GetRealizedElementCount;
			return realizedElementCount == 0 ? -1 : m_elementManager.GetFirstRealizedDataIndex() + realizedElementCount - 1;
		}

		internal int FirstFrozenItemIndexDbg() => m_firstFrozenItemIndex;

		internal int LastFrozenItemIndexDbg() => m_lastFrozenItemIndex;

		internal double AverageItemAspectRatioDbg() => m_averageItemAspectRatioDbg;

		internal double ForcedAverageItemAspectRatioDbg() => m_forcedAverageItemAspectRatioDbg;

		internal void ForcedAverageItemAspectRatioDbg(double forcedAverageItemAspectRatio)
		{
			if (m_forcedAverageItemAspectRatioDbg != forcedAverageItemAspectRatio)
			{
				m_forcedAverageItemAspectRatioDbg = forcedAverageItemAspectRatio;
				InvalidateLayout();
			}
		}

		internal double ForcedAverageItemsPerLineDividerDbg() => m_forcedAverageItemsPerLineDividerDbg;

		internal void ForcedAverageItemsPerLineDividerDbg(double forcedAverageItemsPerLineDivider)
		{
			if (m_forcedAverageItemsPerLineDividerDbg != forcedAverageItemsPerLineDivider)
			{
				m_forcedAverageItemsPerLineDividerDbg = forcedAverageItemsPerLineDivider;
				InvalidateLayout();
			}
		}

		internal double ForcedWrapMultiplierDbg() => m_forcedWrapMultiplierDbg;

		// Allows to change LinedFlowLayout::GetItemWidthMultiplierThreshold()'s return value for testing purposes.
		internal void ForcedWrapMultiplierDbg(double forcedWrapMultiplier)
		{
			if (m_forcedWrapMultiplierDbg != forcedWrapMultiplier)
			{
				m_forcedWrapMultiplierDbg = forcedWrapMultiplier;
				InvalidateLayout();
			}
		}

		internal bool IsFastPathSupportedDbg() => m_isFastPathSupportedDbg;

		// Allows the fall path layout to be turned off for testing purposes.
		// This allows for instance to provide sizing information for an entire
		// small source collection and still exercise the regular path.
		internal void IsFastPathSupportedDbg(bool isFastPathSupported)
		{
			if (m_isFastPathSupportedDbg != isFastPathSupported)
			{
				m_isFastPathSupportedDbg = isFastPathSupported;

				if (UsesFastPathLayout())
				{
					ResetItemsInfo();
				}

				InvalidateLayout();
			}
		}

		internal double RawAverageItemsPerLineDbg() => m_averageItemsPerLine.first;

		internal double SnappedAverageItemsPerLineDbg() => m_averageItemsPerLine.second;

		internal int GetLineIndexDbg(int itemIndex) => GetLineIndex(itemIndex, UsesFastPathLayout());

		// #pragma endregion

		// Structs
		private struct ItemsInfo
		{
			public int m_itemsRangeStartIndex = -1;
			public int m_itemsRangeLength = -1;
			public double m_minWidth = -1.0;
			public double m_maxWidth = -1.0;

			public ItemsInfo()
			{
			}
		}

		private sealed class ItemsLayout
		{
			public List<int> m_lineItemCounts = new();
			public List<double> m_lineItemWidths = new();
			public double m_availableLineItemsWidth;
			public double m_drawback;
			public double m_smallestHeadItemWidth;
			public double m_smallestTailItemWidth;
			public double m_bestEqualizingHeadItemDrawbackImprovement;
			public double m_bestEqualizingTailItemDrawbackImprovement;
			public int m_smallestHeadItemIndex;
			public int m_smallestTailItemIndex;
			public int m_smallestHeadLineIndex;
			public int m_smallestTailLineIndex;
			public int m_bestEqualizingHeadItemIndex;
			public int m_bestEqualizingTailItemIndex;
			public int m_bestEqualizingHeadLineIndex;
			public int m_bestEqualizingTailLineIndex;
		}

		// Constants
		private const string s_cannotShareLinedFlowLayout = "LinedFlowLayout cannot be shared.";
		private const int s_measureCountdownStart = 5;

		private static readonly ItemsInfo s_emptyItemsInfo = new()
		{
			m_itemsRangeStartIndex = -1 /*itemsRangeStartIndex*/,
			m_itemsRangeLength = -1 /*itemsRangeLength*/,
			m_minWidth = -1.0 /*minWidth*/,
			m_maxWidth = -1.0 /*maxWidth*/,
		};

		// Methods

		// #ifdef DBG
		// static winrt::hstring DependencyPropertyToStringDbg(
		//     winrt::IDependencyProperty const& dependencyProperty);
		// void LogElementManagerDbg();
		// void LogItemsInfoDbg(
		//     int itemsRangeStartIndex,
		//     int itemsRangeRequestedLength,
		//     ItemsInfo const& itemsInfo);
		// void LogItemsLayoutDbg(
		//     ItemsLayout const& itemsLayout,
		//     double averageLineItemsWidth) const;
		// void LogLayoutDbg();
		// void LogVirtualizingLayoutContextDbg(
		//     winrt::VirtualizingLayoutContext const& context) const;
		// void VerifyInternalLockedItemsDbg(
		//     std::map<int, int> const& internalLockedItemIndexes,
		//     int beginSizedLineIndex,
		//     int endSizedLineIndex,
		//     int beginSizedItemIndex,
		//     int endSizedItemIndex);
		// void VerifyLockedItemsDbg();
		// #endif

		// Fields
		private readonly ElementManager m_elementManager = new();

		// Data structure for storing item desired aspect ratios.
		private LinedFlowLayoutItemAspectRatios? m_aspectRatios;

		// Map keeping track of the locked items.
		// Key:   index of locked item
		// Value: index of line holding the locked item
		private readonly SortedDictionary<int, int> m_lockedItemIndexes = new();

		// Element index used in bring-into-view operations to disconnected items.
		private int m_anchorIndex = -1;
		private int m_anchorIndexRetentionCountdown;

		// Keep track of the measured items width. The heights are always equal to LineHeight.
		private Dictionary<UIElement, float>? m_elementAvailableWidths;
		private Dictionary<UIElement, float>? m_elementDesiredWidths;

		private readonly List<int> m_lineItemCounts = new();
		private readonly List<float> m_itemsInfoArrangeWidths = new();

		// Items info collected through the ItemsInfoRequested event:
		// - Used only by the regular path layout:
		private readonly List<double> m_itemsInfoDesiredAspectRatiosForRegularPath = new();
		private readonly List<double> m_itemsInfoMinWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoMaxWidthsForRegularPath = new();
		private int m_itemsInfoFirstIndex = -1; // Indicates the index of the first element represented in the 4 vectors above.

		// - Used only by the fast path layout
		private double[] m_itemsInfoDesiredAspectRatiosForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMinWidthsForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMaxWidthsForFastPath = Array.Empty<double>();
		// - Used by both layout paths:
		private double m_itemsInfoMinWidth = -1.0;
		private double m_itemsInfoMaxWidth = -1.0;
		// Note that the List<double> instances are used in the regular path because information gathered by successive ItemsInfoRequested occurrences
		// is stitched together in those lists. This allows to only request information for a fraction of the sized items belonging to 5 viewports
		// in each ItemsInfoRequested event.
		// The fast path is only using cheaper temporary arrays because no such stitching is performed. Information is gathered for
		// the entire source collection and the arrays are discarded at the end of the measure path.

		// This countdown is used during initial loading in order to clamp the average aspect ratio between
		// 2/3 and 3/2 to avoid extranuous item realizations while the first items are still unpopulated.
		// It also allows a progressive expansion of the effective CacheLength to the minimum of 4 when
		// the ItemsRepeater's value is smaller than that.
		private int m_measureCountdown = s_measureCountdownStart;

		private int m_itemCount;
		private int m_unsizedNearLineCount = -1;
		private int m_unrealizedNearLineCount = -1;
		private int m_firstSizedLineIndex = -1;
		private int m_lastSizedLineIndex = -1;
		private int m_firstSizedItemIndex = -1;
		private int m_lastSizedItemIndex = -1;
		private int m_firstFrozenLineIndex = -1;
		private int m_lastFrozenLineIndex = -1;
		private int m_firstFrozenItemIndex = -1;
		private int m_lastFrozenItemIndex = -1;
		private int m_invalidateMeasureTimerTickCount;
		private bool m_isVirtualizingContext;
		private bool m_wasInitializedForContext;
		private bool m_forceRelayout;
		private bool m_isFirstOrLastItemLocked;
		private float m_previousAvailableWidth;
		private float m_maxLineWidth;
		private double m_roundingScaleFactor = 1.0;

		// First double is the 'raw' average-items-per-line value that was snapped to the second double.
		// Second double is the 'snapped' average-items-per-line value starting from the raw first double.
		// The snapping is to a power of 1.1.
		private (double first, double second) m_averageItemsPerLine;

		// Timer used to trigger an asynchronous measure pass when no items info was provided by the ItemsInfoRequested event.
		// It is started after an item was realized or when the timer expires and m_invalidateMeasureTimerTickCount is still
		// smaller than 7. The interval begins at 100ms and is then increased by 50% each time it is re-started, until
		// m_invalidateMeasureTimerTickCount reaches 7. By then the interval is 1.7s and the total time elapsed is about 5s
		// when the timer is no longer re-started.
		private DispatcherTimer? m_invalidateMeasureTimer;

		// Fields used to support the LayoutsTestHooks
		private double m_averageItemAspectRatioDbg;
		private double m_forcedAverageItemAspectRatioDbg;
		private double m_forcedAverageItemsPerLineDividerDbg;
		private double m_forcedWrapMultiplierDbg;
		private bool m_isFastPathSupportedDbg = true;

		// #ifdef DBG
		// Other debug fields
		// int m_previousFirstDisplayedArrangedLineIndexDbg{ -1 };
		// int m_previousLastDisplayedArrangedLineIndexDbg{ -1 };
		// #endif
	}
}
