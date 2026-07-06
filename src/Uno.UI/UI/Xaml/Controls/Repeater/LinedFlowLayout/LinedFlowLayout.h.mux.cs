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

		// Items info collected through the ItemsInfoRequested event for the regular (non-fast) path.
		private readonly List<double> m_itemsInfoDesiredAspectRatiosForRegularPath = new();
		private readonly List<double> m_itemsInfoMinWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoMaxWidthsForRegularPath = new();
		private readonly List<double> m_itemsInfoArrangeWidths = new();

#pragma warning disable 414 // Assigned but not used field: consumed by later WS-D2/D3 slices (measure/arrange), kept to mirror WinUI.
		// Items info collected through the ItemsInfoRequested event for the fast path.
		private double[] m_itemsInfoDesiredAspectRatiosForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMinWidthsForFastPath = Array.Empty<double>();
		private double[] m_itemsInfoMaxWidthsForFastPath = Array.Empty<double>();

		private int m_itemsInfoFirstIndex = -1;
		private double m_itemsInfoMinWidth = -1.0;
		private double m_itemsInfoMaxWidth = -1.0;
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
