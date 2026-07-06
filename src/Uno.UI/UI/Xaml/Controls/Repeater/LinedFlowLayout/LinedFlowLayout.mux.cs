// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LinedFlowLayout.cpp, commit b8cfb8490

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// A layout that arranges items into justified lines of equal height, sizing each item
	/// according to its aspect ratio (WinUI 1.8 LinedFlowLayout).
	/// </summary>
	/// <remarks>
	/// Ported incrementally (workstream D). This slice provides the public API surface
	/// (dependency properties, events, <see cref="LinedFlowLayoutItemsInfoRequestedEventArgs"/>,
	/// and the items-info invalidation seam). The measure/arrange line-breaking algorithm
	/// (LinedFlowLayout.cpp) is ported in a later slice; members that depend on it throw
	/// <see cref="NotImplementedException"/> until then.
	/// </remarks>
	public partial class LinedFlowLayout : VirtualizingLayout
	{
		public LinedFlowLayout()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_LinedFlowLayout);
			LayoutId = "LinedFlowLayout";

			// DPs are registered via the static initializers in LinedFlowLayout.Properties.cs
			// (WinUI's EnsureProperties()).

			SetIndexBasedLayoutOrientation(IndexBasedLayoutOrientation.LeftToRight);
		}

		#region ILinedFlowLayout

		/// <summary>
		/// Locks the provided item to its line until the average-items-per-line or the collection
		/// changes, at which point the <see cref="ItemsUnlocked"/> event is raised.
		/// </summary>
		public int LockItemToLine(int itemIndex)
		{
			// TODO (WS-D3): port the item-locking algorithm from LinedFlowLayout.cpp.
			throw new NotImplementedException("LinedFlowLayout.LockItemToLine is not yet ported (WS-D3).");
		}

		/// <summary>
		/// Discards the item sizing info previously gathered through the <see cref="ItemsInfoRequested"/>
		/// event and forces a re-layout in the next measure pass.
		/// </summary>
		public void InvalidateItemsInfo()
		{
			InvalidateLayout(forceRelayout: true, resetItemsInfo: true, invalidateMeasure: true);
		}

		/// <summary>
		/// Index of the first item for which sizing info is requested through the
		/// <see cref="ItemsInfoRequested"/> event.
		/// </summary>
		public int RequestedRangeStartIndex => UsesFastPathLayout() ? 0 : m_itemsInfoFirstIndex;

		/// <summary>
		/// Number of items for which sizing info is requested through the
		/// <see cref="ItemsInfoRequested"/> event.
		/// </summary>
		public int RequestedRangeLength => UsesFastPathLayout() ? m_itemCount : m_itemsInfoDesiredAspectRatiosForRegularPath.Count;

		#endregion

		// Seams invoked by LinedFlowLayoutItemsInfoRequestedEventArgs to store the fast-path sizing
		// info provided by an ItemsInfoRequested event handler (LinedFlowLayout.h).
		internal void SetDesiredAspectRatios(double[] values) => m_itemsInfoDesiredAspectRatiosForFastPath = (double[])values.Clone();

		internal void SetMinWidths(double[] values) => m_itemsInfoMinWidthsForFastPath = (double[])values.Clone();

		internal void SetMaxWidths(double[] values) => m_itemsInfoMaxWidthsForFastPath = (double[])values.Clone();

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var dependencyProperty = args.Property;

			if (dependencyProperty != ActualLineHeightProperty)
			{
				InvalidateLayout();
			}
		}

		// - forceRelayout == true:     forces a re-layout in the next measure pass.
		// - resetItemsInfo == true:    resets the items info gathered so far.
		// - invalidateMeasure == true: triggers a new measure pass.
		internal void InvalidateLayout(
			bool forceRelayout = true,
			bool resetItemsInfo = false,
			bool invalidateMeasure = true)
		{
			MUX_ASSERT(forceRelayout || resetItemsInfo || invalidateMeasure);

			if (forceRelayout)
			{
				// Perform a complete re-layout during the next layout pass.
				// TODO (WS-D5): NotifyLinedFlowLayoutInvalidatedDbg(LinedFlowLayoutInvalidationTrigger.InvalidateLayoutCall).
				m_forceRelayout = true;
			}

			if (resetItemsInfo)
			{
				// Discard any potential item sizing info previously collected through the ItemsInfoRequested event.
				ResetItemsInfo();
			}

			if (invalidateMeasure)
			{
				// Trigger a new layout pass.
				InvalidateMeasure();
			}
		}

		private void ResetItemsInfo()
		{
			m_itemsInfoDesiredAspectRatiosForRegularPath.Clear();
			m_itemsInfoMinWidthsForRegularPath.Clear();
			m_itemsInfoMaxWidthsForRegularPath.Clear();
			m_itemsInfoArrangeWidths.Clear();
			m_itemsInfoFirstIndex = -1;
			m_itemsInfoMinWidth = -1.0;
			m_itemsInfoMaxWidth = -1.0;
		}

		#region Items info (ItemsInfoRequested plumbing)

		// Returns True when an ItemsInfoRequested handler provided arrange widths (fast or regular path).
		private bool UsesArrangeWidthInfo() => m_itemsInfoArrangeWidths.Count > 0;

		// Returns True when an ItemsInfoRequested handler provided sizing information for the entire source
		// collection. In that case the fast path can be performed; it is characterized by a populated
		// m_itemsInfoArrangeWidths and m_itemsInfoFirstIndex remaining at -1.
		private bool UsesFastPathLayout() => UsesArrangeWidthInfo() && m_itemsInfoFirstIndex == -1;

		// Raises the ItemsInfoRequested event for the [itemsRangeStartIndex, +itemsRangeRequestedLength) range
		// and returns the sizing info the handler provided (or s_emptyItemsInfo when there is no handler).
		// WinUI declares this private; it is internal here so the public ItemsInfoRequested round-trip is
		// testable before the measure path that calls it (WS-D3c) is ported.
		internal ItemsInfo RaiseItemsInfoRequested(
			int itemsRangeStartIndex,
			int itemsRangeRequestedLength)
		{
			MUX_ASSERT(itemsRangeStartIndex >= 0);
			MUX_ASSERT(itemsRangeRequestedLength > 0);
			MUX_ASSERT(itemsRangeStartIndex + itemsRangeRequestedLength - 1 < m_itemCount);

			if (ItemsInfoRequested is { } itemsInfoRequested)
			{
				var itemsInfoRequestedEventArgs = new LinedFlowLayoutItemsInfoRequestedEventArgs(this, itemsRangeStartIndex, itemsRangeRequestedLength);

				itemsInfoRequested(this, itemsInfoRequestedEventArgs);

				// Disconnect the LinedFlowLayout from the LinedFlowLayoutItemsInfoRequestedEventArgs so that any
				// subsequent calls to SetDesiredAspectRatios/SetMinWidths/SetMaxWidths have no effect.
				itemsInfoRequestedEventArgs.ResetLinedFlowLayout();

				// The original ItemsRangeStartIndex value may have been overwritten by the handler to a smaller
				// value. This allows a handler to provide more information than requested, because it's available
				// and performant to do so. It reduces subsequent needs to raise the event again.
				return new ItemsInfo
				{
					m_itemsRangeStartIndex = itemsInfoRequestedEventArgs.ItemsRangeStartIndex,
					m_itemsRangeLength = itemsInfoRequestedEventArgs.ItemsRangeLength,
					m_minWidth = (float)itemsInfoRequestedEventArgs.MinWidth,
					m_maxWidth = (float)itemsInfoRequestedEventArgs.MaxWidth,
				};
			}

			return s_emptyItemsInfo;
		}

		#endregion

		#region ILayoutOverrides

		protected internal override ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider()
		{
			// TODO (WS-D4): return new LinedFlowLayoutItemCollectionTransitionProvider().
			throw new NotImplementedException("LinedFlowLayout.CreateDefaultItemTransitionProvider is not yet ported (WS-D4).");
		}

		#endregion

		#region IVirtualizingLayoutOverrides

		protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
		{
			if (m_wasInitializedForContext)
			{
				throw new InvalidOperationException(s_cannotShareLinedFlowLayout);
			}

			base.InitializeForContextCore(context);

			m_wasInitializedForContext = true;
			m_isVirtualizingContext = IsVirtualizingContext(context);
			m_itemCount = context.ItemCount;
			m_elementManager.SetContext(context);
		}

		protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
		{
			MUX_ASSERT(m_wasInitializedForContext);
			MUX_ASSERT(m_isVirtualizingContext == IsVirtualizingContext(context));

			base.UninitializeForContextCore(context);

			m_wasInitializedForContext = false;
			m_itemCount = 0;
			if (m_isVirtualizingContext)
			{
				m_isVirtualizingContext = false;
				m_elementManager.ClearRealizedRange();
			}

			InvalidateMeasureTimerStop(false /*isForDestructor*/);
			UnlockItems();
		}

		protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		{
			// TODO (WS-D3): port the line-breaking measure algorithm from LinedFlowLayout.cpp.
			throw new NotImplementedException("LinedFlowLayout.MeasureOverride is not yet ported (WS-D3).");
		}

		protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
		{
			// TODO (WS-D3): port the arrange algorithm from LinedFlowLayout.cpp.
			throw new NotImplementedException("LinedFlowLayout.ArrangeOverride is not yet ported (WS-D3).");
		}

		protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
		{
			// TODO (WS-D3f): port collection-change handling from LinedFlowLayout.cpp.
			throw new NotImplementedException("LinedFlowLayout.OnItemsChangedCore is not yet ported (WS-D3f).");
		}

		#endregion

		#region State helpers

		private bool IsVirtualizingContext(VirtualizingLayoutContext context)
		{
			if (context != null)
			{
				var rect = context.RealizationRect;
				bool hasInfiniteSize = double.IsInfinity(rect.Height) || double.IsInfinity(rect.Width);
				return !hasInfiniteSize;
			}
			return false;
		}

		// Stops the timer used to trigger an asynchronous measure pass when no items info was provided
		// by the ItemsInfoRequested event. The full timer subsystem lands in WS-D3f; this null-safe
		// stop is required by UninitializeForContextCore.
		private void InvalidateMeasureTimerStop(bool isForDestructor)
		{
			if (m_invalidateMeasureTimer is not null && m_invalidateMeasureTimer.IsEnabled)
			{
				m_invalidateMeasureTimer.Stop();
			}
		}

		// Raises the ItemsUnlocked event when there are locked items. Raised when previously locked
		// items are cleared because the source collection or the average items per line changed.
		private void UnlockItems()
		{
			if (m_lockedItemIndexes.Count > 0 || m_isFirstOrLastItemLocked)
			{
				m_lockedItemIndexes.Clear();
				m_isFirstOrLastItemLocked = false;
				ItemsUnlocked?.Invoke(this, null!);
			}
		}

		#endregion

		#region Line geometry & snapping (pure helpers)

		// internal (rather than WinUI's private) so the pure-math slices can be unit/runtime-tested
		// in isolation before the measure algorithm that consumes them is ported.
		internal int GetLineCount(double averageItemsPerLine)
		{
			if (m_itemCount == 0)
			{
				return 0;
			}

			MUX_ASSERT(m_itemCount > 0);

			int lineCount = GetLineIndexFromAverageItemsPerLine(m_itemCount - 1, averageItemsPerLine) + 1;

			return lineCount;
		}

		internal int GetLineIndexFromAverageItemsPerLine(int itemIndex, double averageItemsPerLine)
		{
			MUX_ASSERT(averageItemsPerLine >= 1.0);

			int lineIndex = (int)(itemIndex / averageItemsPerLine);

			return lineIndex;
		}

		internal (double first, double second) SnapAverageItemsPerLine(
			double oldAverageItemsPerLineRaw,
			double newAverageItemsPerLineRaw)
		{
			MUX_ASSERT(oldAverageItemsPerLineRaw == 0.0 || oldAverageItemsPerLineRaw >= 1.0);
			MUX_ASSERT(newAverageItemsPerLineRaw == 0.0 || newAverageItemsPerLineRaw >= 1.0);

			// A snapped average-items-per-line value must be a power of 1.1.
			const double valuePower = 1.1;
			// When the raw value crosses the median between two successive powers of 1.1, the old snapped
			// value is retained unless the raw delta is greater than 0.1.
			const double distanceThreshold = 0.1;
			double appliedValuePower = m_forcedAverageItemsPerLineDividerDbg == 0.0 ? valuePower : m_forcedAverageItemsPerLineDividerDbg;
			double oldAverageItemsPerLineSnapped = (oldAverageItemsPerLineRaw == 0.0) ? 0.0 : SnapToPower(oldAverageItemsPerLineRaw, appliedValuePower);
			double newAverageItemsPerLineSnapped = (newAverageItemsPerLineRaw == 0.0) ? 0.0 : SnapToPower(newAverageItemsPerLineRaw, appliedValuePower);

			if (oldAverageItemsPerLineSnapped != newAverageItemsPerLineSnapped &&
				Math.Abs(oldAverageItemsPerLineRaw - newAverageItemsPerLineRaw) <= distanceThreshold)
			{
				// The snapped values differ but the raw values are close: keep the old pair so the applied
				// average items per line is stable when the raw values hover around the 1.1^N midpoint.
				return (oldAverageItemsPerLineRaw, oldAverageItemsPerLineSnapped);
			}

			return (newAverageItemsPerLineRaw, newAverageItemsPerLineSnapped);
		}

		// Snaps the provided value to a power of valuePower. Example: value=3.75 snaps to 1.1^14 = 3.7975.
		internal double SnapToPower(double value, double valuePower)
		{
			double dividerLog = Math.Log(valuePower);
			double valueLog = Math.Log(value);
			double valueExp = Math.Ceiling(valueLog / dividerLog);
			double valueRndCeil = Math.Pow(valuePower, valueExp);
			double valueRndFloor = valueRndCeil / valuePower;

			// Return valuePower^valueExp or valuePower^(valueExp - 1), whichever is closest to value.
			if (Math.Abs(valueRndCeil - value) <= Math.Abs(valueRndFloor - value))
			{
				return valueRndCeil;
			}
			else
			{
				return valueRndFloor;
			}
		}

		#endregion

		#region Arrange width (pure)

		// Clamps desiredAspectRatio * actualLineHeight to [minWidth, maxWidth] and then applies scaleFactor,
		// re-clamping to the relevant bound. WinUI declares this private; it is internal here so it can be
		// unit-tested before the measure path that consumes it is fully ported (WS-D3c).
		internal double GetArrangeWidth(
			double desiredAspectRatio,
			double minWidth,
			double maxWidth,
			double actualLineHeight,
			double scaleFactor)
		{
			MUX_ASSERT(desiredAspectRatio > 0.0);

			double arrangeWidth = desiredAspectRatio * actualLineHeight;

			minWidth = Math.Max(0.0, minWidth);

			arrangeWidth = Math.Max(minWidth, arrangeWidth);

			if (maxWidth >= 0.0)
			{
				arrangeWidth = Math.Min(maxWidth, arrangeWidth);
			}

			if (scaleFactor != 1.0)
			{
				arrangeWidth *= scaleFactor;

				if (scaleFactor < 1.0)
				{
					arrangeWidth = Math.Max(minWidth, arrangeWidth);
				}

				if (maxWidth >= 0.0 && scaleFactor > 1.0)
				{
					arrangeWidth = Math.Min(maxWidth, arrangeWidth);
				}
			}

			return arrangeWidth;
		}

		#endregion

		#region Items info accessors & buffers (WS-D3c)

		// std::vector::resize(count, value) equivalent for a List<T> (clear + fill).
		private static void ClearAndFill<T>(List<T> list, int count, T value)
		{
			list.Clear();

			for (int i = 0; i < count; i++)
			{
				list.Add(value);
			}
		}

		private void EnsureItemsInfoDesiredAspectRatios(int itemCount) => ClearAndFill(m_itemsInfoDesiredAspectRatiosForRegularPath, itemCount, -1.0);

		private void EnsureItemsInfoMinWidths(int itemCount) => ClearAndFill(m_itemsInfoMinWidthsForRegularPath, itemCount, -1.0);

		private void EnsureItemsInfoMaxWidths(int itemCount) => ClearAndFill(m_itemsInfoMaxWidthsForRegularPath, itemCount, -1.0);

		private void EnsureItemsInfoArrangeWidths(int itemCount) => ClearAndFill(m_itemsInfoArrangeWidths, itemCount, -1.0f);

		private void EnsureLineItemCounts(int lineCount) => ClearAndFill(m_lineItemCounts, lineCount, 0);

		private void ResetLinesInfo()
		{
			m_lineItemCounts.Clear();
		}

		private void ResetSizedLines()
		{
			m_firstSizedLineIndex = -1;
			m_lastSizedLineIndex = -1;
			m_firstSizedItemIndex = -1;
			m_lastSizedItemIndex = -1;
			m_firstFrozenLineIndex = -1;
			m_lastFrozenLineIndex = -1;
			m_firstFrozenItemIndex = -1;
			m_lastFrozenItemIndex = -1;
		}

		private void ExitRegularPath()
		{
			m_itemsInfoFirstIndex = -1;
			m_unsizedNearLineCount = -1;
			m_unrealizedNearLineCount = -1;
			m_elementAvailableWidths = null;
			m_elementDesiredWidths = null;
			ResetSizedLines();
		}

		// Fast path layout:
		//   Returns the arrange width computed from the m_itemsInfoDesiredAspectRatiosForFastPath, m_itemsInfoMinWidthsForFastPath, m_itemsInfoMaxWidthsForFastPath,
		//   m_itemsInfoMinWidth, m_itemsInfoMaxWidth fields.
		// Regular path layout:
		//   Returns the arrange width computed from the m_itemsInfoDesiredAspectRatiosForRegularPath, m_itemsInfoMinWidthsForRegularPath, m_itemsInfoMaxWidthsForRegularPath,
		//   m_itemsInfoMinWidth, m_itemsInfoMaxWidth fields.
		// The desired aspect ratio is combined with the provided scaleFactor.
		// internal (rather than WinUI's private) so it can be white-box tested before the measure path that consumes it lands.
		internal float GetArrangeWidthFromItemsInfo(
			int sizedItemIndex,
			double actualLineHeight,
			double averageAspectRatio,
			double scaleFactor)
		{
			double desiredAspectRatio = GetDesiredAspectRatioFromItemsInfo(sizedItemIndex, UsesFastPathLayout());

			if (desiredAspectRatio <= 0)
			{
				// The average aspect ratio is used as a fallback value for items that were given a negative ratio
				// by the ItemsInfoRequested handler.
				desiredAspectRatio = averageAspectRatio;
			}

			return (float)GetArrangeWidth(
				desiredAspectRatio,
				GetMinWidthFromItemsInfo(sizedItemIndex),
				GetMaxWidthFromItemsInfo(sizedItemIndex),
				actualLineHeight,
				scaleFactor);
		}

		internal double GetDesiredAspectRatioFromItemsInfo(
			int itemIndex,
			bool usesFastPathLayout)
		{
			MUX_ASSERT(itemIndex >= 0);
			MUX_ASSERT(itemIndex < m_itemCount);

			double desiredAspectRatio;

			if (usesFastPathLayout)
			{
				MUX_ASSERT(m_itemsInfoFirstIndex == -1);
				MUX_ASSERT(itemIndex < m_itemsInfoDesiredAspectRatiosForFastPath.Length);

				desiredAspectRatio = m_itemsInfoDesiredAspectRatiosForFastPath[itemIndex];
			}
			else
			{
				MUX_ASSERT(m_itemsInfoFirstIndex >= 0);
				MUX_ASSERT(itemIndex - m_itemsInfoFirstIndex < m_itemsInfoDesiredAspectRatiosForRegularPath.Count);

				desiredAspectRatio = m_itemsInfoDesiredAspectRatiosForRegularPath[itemIndex - m_itemsInfoFirstIndex];
			}

			return desiredAspectRatio;
		}

		internal double GetMaxWidthFromItemsInfo(int itemIndex)
		{
			MUX_ASSERT(itemIndex >= 0);
			MUX_ASSERT(itemIndex < m_itemCount);
			MUX_ASSERT(m_itemsInfoMaxWidth >= 0.0 || m_itemsInfoMaxWidth == -1.0);

			if (UsesFastPathLayout())
			{
				// Fast path layout
				if (itemIndex < m_itemsInfoMaxWidthsForFastPath.Length)
				{
					return Math.Min(m_itemsInfoMaxWidth, m_itemsInfoMaxWidthsForFastPath[itemIndex]);
				}
			}
			else
			{
				// Regular path layout
				if (itemIndex - m_itemsInfoFirstIndex >= 0 &&
					itemIndex - m_itemsInfoFirstIndex < m_itemsInfoMaxWidthsForRegularPath.Count)
				{
					return Math.Min(m_itemsInfoMaxWidth, m_itemsInfoMaxWidthsForRegularPath[itemIndex - m_itemsInfoFirstIndex]);
				}
			}

			return m_itemsInfoMaxWidth;
		}

		internal double GetMinWidthFromItemsInfo(int itemIndex)
		{
			MUX_ASSERT(itemIndex >= 0);
			MUX_ASSERT(itemIndex < m_itemCount);
			MUX_ASSERT(m_itemsInfoMinWidth >= 0.0 || m_itemsInfoMinWidth == -1.0);

			if (UsesFastPathLayout())
			{
				// Fast path layout
				if (itemIndex < m_itemsInfoMinWidthsForFastPath.Length)
				{
					return Math.Max(m_itemsInfoMinWidth, m_itemsInfoMinWidthsForFastPath[itemIndex]);
				}
			}
			else
			{
				// Regular path layout
				if (itemIndex - m_itemsInfoFirstIndex >= 0 &&
					itemIndex - m_itemsInfoFirstIndex < m_itemsInfoMinWidthsForRegularPath.Count)
				{
					return Math.Max(m_itemsInfoMinWidth, m_itemsInfoMinWidthsForRegularPath[itemIndex - m_itemsInfoFirstIndex]);
				}
			}

			return m_itemsInfoMinWidth;
		}

		#endregion
	}
}
