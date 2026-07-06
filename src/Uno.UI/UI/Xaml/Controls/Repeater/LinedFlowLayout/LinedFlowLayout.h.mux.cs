// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.h, commit b8cfb8490

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class LinedFlowLayout
	{
		// Default property values (LinedFlowLayout.h).
		private const LinedFlowLayoutItemsJustification s_defaultItemsJustification = LinedFlowLayoutItemsJustification.Start;
		private const LinedFlowLayoutItemsStretch s_defaultItemsStretch = LinedFlowLayoutItemsStretch.None;
		private const double s_defaultActualLineHeight = 0.0;
		private const double s_defaultLineSpacing = 0.0;
		private const double s_defaultLineHeight = double.NaN;
		private const double s_defaultMinItemSpacing = 0.0;

		// Constants (LinedFlowLayout.h).
		private const string s_cannotShareLinedFlowLayout = "LinedFlowLayout cannot be shared.";
		private const int s_measureCountdownStart = 5;

		// The desired aspect ratio associated with an item has a weight between 1 and 16. Each time an item's
		// aspect ratio is evaluated, its weight is incremented up to 16. The average aspect ratio is computed
		// using those weights to stabilize m_averageItemsPerLine and reduce total re-layouts (LinedFlowLayout.cpp).
		private const int c_maxAspectRatioWeight = 16;

		// Sizing info returned by the ItemsInfoRequested event handler for a range of items.
		// WinUI declares ItemsInfo private; it is internal here so RaiseItemsInfoRequested's return
		// value can be validated by tests before the measure path that consumes it (WS-D3c) is ported.
		internal struct ItemsInfo
		{
			public int m_itemsRangeStartIndex = -1;
			public int m_itemsRangeLength = -1;
			public double m_minWidth = -1.0;
			public double m_maxWidth = -1.0;

			public ItemsInfo()
			{
			}
		}

		private static readonly ItemsInfo s_emptyItemsInfo = new()
		{
			m_itemsRangeStartIndex = -1,
			m_itemsRangeLength = -1,
			m_minWidth = -1.0,
			m_maxWidth = -1.0,
		};

		// Items info collected through the ItemsInfoRequested event for the regular (non-fast) path.
		private readonly List<double> m_itemsInfoDesiredAspectRatiosForRegularPath = new();
		private readonly List<double> m_itemsInfoMinWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoMaxWidthsForRegularPath = new();
		// WinUI: std::vector<float>. Holds the per-item arrange widths (fast or regular path).
		private readonly List<float> m_itemsInfoArrangeWidths = new();

		// First index of the regular-path items info (-1 when the fast path is used or no info is available).
		// Read by UsesFastPathLayout()/RequestedRangeStartIndex (WS-D3b); written by ResetItemsInfo (WS-D3c: ExitRegularPath).
		private int m_itemsInfoFirstIndex = -1;

		// Items info collected through the ItemsInfoRequested event for the fast path.
		// WinUI: winrt::com_array<double>. Written by the Set*Widths seams; read by Get*FromItemsInfo (WS-D3c).
		private double[] m_itemsInfoDesiredAspectRatiosForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMinWidthsForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMaxWidthsForFastPath = Array.Empty<double>();

		// Global min/max widths provided by the ItemsInfoRequested handler; read by Get{Min,Max}WidthFromItemsInfo (WS-D3c).
		private double m_itemsInfoMinWidth = -1.0;
		private double m_itemsInfoMaxWidth = -1.0;

#pragma warning disable 414 // Assigned but not yet read: consumed by later WS-D3c measure slices, kept to mirror WinUI.
		private bool m_forceRelayout;
#pragma warning restore 414

		// Core state (LinedFlowLayout.h). Owns the realized-element bookkeeping and the
		// weighted aspect-ratio cache. LinedFlowLayout stores its state on the instance
		// (it cannot be shared across ItemsRepeaters, unlike the other layouts).
		private readonly ElementManager m_elementManager = new();

		// Item count as reported by the context; refreshed on each Initialize/OnItemsChanged.
		private int m_itemCount;

		// Map keeping track of the locked items. Key: locked item index; Value: line index holding it.
		private readonly SortedDictionary<int, int> m_lockedItemIndexes = new();

		private bool m_isVirtualizingContext;
		private bool m_wasInitializedForContext;
		private bool m_isFirstOrLastItemLocked;

		// Fields consumed by later WS-D3b..D3f measure/arrange slices. Declared now so the state
		// shape mirrors LinedFlowLayout.h; some are only read (or only written) until those slices land.
#pragma warning disable 169, 414, 649
		// Data structure for storing item desired aspect ratios (allocated during measure).
		private LinedFlowLayoutItemAspectRatios? m_aspectRatios;

		// Element index used in bring-into-view operations to disconnected items.
		private int m_anchorIndex = -1;
		private int m_anchorIndexRetentionCountdown;

		// Measured item widths (heights are always equal to ActualLineHeight).
		private Dictionary<UIElement, float>? m_elementAvailableWidths;
		private Dictionary<UIElement, float>? m_elementDesiredWidths;

		private readonly List<int> m_lineItemCounts = new();

		// Clamps the average aspect ratio during initial loading; also progressively expands the
		// effective CacheLength to a minimum of 4.
		private int m_measureCountdown = s_measureCountdownStart;

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
		private float m_previousAvailableWidth;
		private float m_maxLineWidth;
		private double m_roundingScaleFactor = 1.0;

		// First value is the 'raw' average-items-per-line; second is that value snapped to a power of 1.1.
		private (double first, double second) m_averageItemsPerLine;

		// Timer used to trigger an asynchronous measure pass when no items info was provided by the
		// ItemsInfoRequested event (started from the measure path in WS-D3f).
		private DispatcherTimer? m_invalidateMeasureTimer;

		// Fields backing the LayoutsTestHooks (WS-D5). Always compiled (not DBG-only); consumed by the
		// measure algorithm (e.g. m_forcedAverageItemsPerLineDividerDbg tunes SnapAverageItemsPerLine).
		private double m_averageItemAspectRatioDbg;
		private double m_forcedAverageItemAspectRatioDbg;
		private double m_forcedAverageItemsPerLineDividerDbg;
		private double m_forcedWrapMultiplierDbg;
		private bool m_isFastPathSupportedDbg = true;
#pragma warning restore 169, 414, 649
	}
}
