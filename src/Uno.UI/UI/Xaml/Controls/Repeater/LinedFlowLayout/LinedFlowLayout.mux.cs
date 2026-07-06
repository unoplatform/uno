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
			double scaleFactor = 1.0)
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

		#region Line geometry & averages (WS-D3c)

		internal int GetFirstItemIndexInLineIndex(int lineVectorIndex)
		{
			MUX_ASSERT(lineVectorIndex >= 0);
			MUX_ASSERT(lineVectorIndex < m_lineItemCounts.Count);

			int itemIndex = 0;

			for (int lineVectorIndexTmp = 0; lineVectorIndexTmp < lineVectorIndex; lineVectorIndexTmp++)
			{
				itemIndex += m_lineItemCounts[lineVectorIndexTmp];
			}

			return itemIndex;
		}

		internal int GetLastItemIndexInLineIndex(int lineVectorIndex)
		{
			MUX_ASSERT(lineVectorIndex >= 0);
			MUX_ASSERT(lineVectorIndex < m_lineItemCounts.Count);

			int firstItemIndexInLineIndex = GetFirstItemIndexInLineIndex(lineVectorIndex);

			return firstItemIndexInLineIndex + m_lineItemCounts[lineVectorIndex] - 1;
		}

		// Returns the line index for the provided item index.
		internal int GetLineIndex(int itemIndex, bool usesFastPathLayout)
		{
			MUX_ASSERT(m_isVirtualizingContext);
			MUX_ASSERT(itemIndex >= 0);
			MUX_ASSERT(itemIndex < m_itemCount);

			if (itemIndex == 0)
			{
				// May occur while m_averageItemsPerLine.second is still 0.
				return 0;
			}

			int lineIndex = 0;

			if (usesFastPathLayout)
			{
				int lineItemCounts = 0;

				while (lineItemCounts + m_lineItemCounts[lineIndex] <= itemIndex)
				{
					lineItemCounts += m_lineItemCounts[lineIndex];
					lineIndex++;
				}
			}
			else
			{
				MUX_ASSERT(m_averageItemsPerLine.second >= 1.0);

				// Determine the element's line index depending on its locked status.
				if (m_lockedItemIndexes.TryGetValue(itemIndex, out int lockedLineIndex))
				{
					// Since the item is locked, use the line index it's locked on.
					lineIndex = lockedLineIndex;
				}
				else
				{
					int sizedItemIndex = m_firstSizedItemIndex;
					int sizedItemCount = m_lastSizedItemIndex - m_firstSizedItemIndex + 1;
					int sizedLineVectorCount = m_lineItemCounts.Count;

					// If the item is sized, use its sized line index.
					if (sizedItemIndex != -1 &&
						itemIndex >= sizedItemIndex &&
						itemIndex < sizedItemIndex + sizedItemCount &&
						m_lastSizedLineIndex - m_firstSizedLineIndex + 1 == sizedLineVectorCount)
					{
						MUX_ASSERT(m_firstSizedLineIndex >= 0);

						int sizedLineVectorIndex = 0;

						while (sizedItemIndex + m_lineItemCounts[sizedLineVectorIndex] - 1 < itemIndex)
						{
							sizedItemIndex += m_lineItemCounts[sizedLineVectorIndex];
							sizedLineVectorIndex++;

							MUX_ASSERT(sizedLineVectorIndex < sizedLineVectorCount);
						}

						lineIndex = m_firstSizedLineIndex + sizedLineVectorIndex;
					}
					else
					{
						// As a last resort, use the estimated line index based on the average items per line.
						lineIndex = GetLineIndexFromAverageItemsPerLine(itemIndex, m_averageItemsPerLine.second);
					}
				}
			}

			return lineIndex;
		}

		// Returns the largest line width for a range of lines.
		// When item sizing info is used, that range is the sized lines.
		// Otherwise it's the frozen lines only.
		internal float GetLinesDesiredWidth()
		{
			if (m_firstFrozenLineIndex == -1)
			{
				// This occurs when GetFirstAndLastDisplayedLineIndexes returns firstDisplayedLineIndex==-1
				// because the scrollViewport is still 0.0 for example.
				return 0.0f;
			}

			MUX_ASSERT(m_firstSizedLineIndex >= 0);
			MUX_ASSERT(m_lastSizedLineIndex >= m_firstSizedLineIndex);
			MUX_ASSERT(m_firstSizedItemIndex >= 0);
			MUX_ASSERT(m_lastSizedItemIndex >= m_firstSizedItemIndex);
			MUX_ASSERT(m_firstFrozenLineIndex >= 0);
			MUX_ASSERT(m_lastFrozenLineIndex >= m_firstFrozenLineIndex);
			MUX_ASSERT(m_firstFrozenItemIndex >= 0);
			MUX_ASSERT(m_lastFrozenItemIndex >= m_firstFrozenItemIndex);

			bool usesArrangeWidthInfo = UsesArrangeWidthInfo();
			float minItemSpacing = (float)MinItemSpacing;
			int firstLineIndex = usesArrangeWidthInfo ? m_firstSizedLineIndex : m_firstFrozenLineIndex;
			int lastLineIndex = usesArrangeWidthInfo ? m_lastSizedLineIndex : m_lastFrozenLineIndex;
			int itemIndex = usesArrangeWidthInfo ? m_firstSizedItemIndex : m_firstFrozenItemIndex;
			float maxLineWidth = 0.0f;

			MUX_ASSERT(!usesArrangeWidthInfo || m_itemsInfoFirstIndex >= 0);

			for (int lineIndex = firstLineIndex; lineIndex <= lastLineIndex; lineIndex++)
			{
				int lineItemsCount = m_lineItemCounts[lineIndex - m_firstSizedLineIndex];
				float lineWidth = 0.0f;

				for (int lineItemIndex = 0; lineItemIndex < lineItemsCount; lineItemIndex++)
				{
					float itemWidth = 0.0f;

					if (usesArrangeWidthInfo)
					{
						itemWidth = m_itemsInfoArrangeWidths[itemIndex - m_itemsInfoFirstIndex];
					}
					else
					{
						MUX_ASSERT(m_elementManager.IsDataIndexRealized(itemIndex));

						if (m_elementManager.GetRealizedElement(itemIndex) is { } element)
						{
							itemWidth = (float)element.DesiredSize.Width;
						}
					}

					if (lineItemIndex > 0)
					{
						lineWidth += minItemSpacing;
					}

					lineWidth += itemWidth;

					itemIndex++;
				}

				maxLineWidth = Math.Max(lineWidth, maxLineWidth);
			}

			return maxLineWidth;
		}

		// Returns the current average aspect ratio, either based on the m_aspectRatios storage when it's
		// populated, or m_averageItemsPerLine when set, or finally the default defaultAspectRatio == 1.0.
		internal double GetAverageAspectRatio(float availableWidth, double actualLineHeight)
		{
			if (m_aspectRatios == null || m_aspectRatios.IsEmpty())
			{
				if (m_averageItemsPerLine.second == 0.0 || actualLineHeight == 0.0)
				{
					const double defaultAspectRatio = 1.0;

					return defaultAspectRatio;
				}
				else
				{
					return availableWidth / m_averageItemsPerLine.second / actualLineHeight;
				}
			}

			return GetAverageAspectRatioFromStorage();
		}

		// Returns the average aspect ratio from m_aspectRatios or 1.0 when it is empty.
		internal double GetAverageAspectRatioFromStorage()
		{
			const double defaultAspectRatio = 1.0;

			if (m_aspectRatios == null || m_aspectRatios.IsEmpty())
			{
				return defaultAspectRatio;
			}

			int firstRealizedItemIndex = m_isVirtualizingContext ? m_elementManager.GetDataIndexFromRealizedRangeIndex(0) : 0;
			int lastRealizedItemIndex = firstRealizedItemIndex + m_elementManager.GetRealizedElementCount - 1;
			// Items outside the current realized range must have a weight equal to c_maxAspectRatioWeight to be taken into account.
			// Smaller weights may not reflect the real aspect ratio and negatively influence the average.
			float averageItemAspectRatio = m_aspectRatios.GetAverageAspectRatio(firstRealizedItemIndex, lastRealizedItemIndex, c_maxAspectRatioWeight);

			return averageItemAspectRatio == 0.0f ? defaultAspectRatio : averageItemAspectRatio;
		}

		internal void SetAverageItemsPerLine(
			(double first, double second) averageItemsPerLine,
			bool unlockItems)
		{
			if (averageItemsPerLine.second == m_averageItemsPerLine.second)
			{
				// When the old and new snapped average-items-per-line are identical,
				// just retain the raw value change.
				m_averageItemsPerLine.first = averageItemsPerLine.first;
			}
			else
			{
				m_averageItemsPerLine = averageItemsPerLine;

				if (unlockItems)
				{
					UnlockItems();
				}

				// TODO (WS-D5): globalTestHooks.NotifyLinedFlowLayoutSnappedAverageItemsPerLineChanged(this).
			}
		}

		#endregion

		#region Measure engine: realization leaf + scale factors (WS-D3c)

		// Measures 'element' with 'availableSize' and returns its resulting DesiredSize, then re-measures it
		// with the width that must be restored (its recorded arrange/available/desired width) so the temporary
		// measure has no lasting effect. Returns (-1, -1) when no width is available to undo the measure.
		internal Size GetDesiredSizeForAvailableSize(
			int itemIndex,
			UIElement element,
			Size availableSize,
			double actualLineHeightToRestore)
		{
			// WinUI asserts element != null here; the non-nullable UIElement parameter already enforces that
			// in C# (and MUX_ASSERT, being [Conditional("DEBUG")], would otherwise demote element to maybe-null).
			MUX_ASSERT(itemIndex >= 0);

			// When m_itemsInfoArrangeWidths is populated,
			// - m_itemsInfoFirstIndex != -1: Regular path layout case, with items info available.
			// - m_itemsInfoFirstIndex == -1: Fast path layout case, without items info available.
			int itemsInfoArrangeWidthsIndex = itemIndex - (m_itemsInfoFirstIndex == -1 ? 0 : m_itemsInfoFirstIndex);
			float measureWidthToRestore = 0.0f;

			if (itemsInfoArrangeWidthsIndex < m_itemsInfoArrangeWidths.Count)
			{
				float arrangeWidth = m_itemsInfoArrangeWidths[itemsInfoArrangeWidthsIndex];

				if (arrangeWidth < 0.0f)
				{
					// The m_itemsInfoArrangeWidths vector has not been set for the provided 'itemIndex' yet.
					// The effects of measuring 'element' with 'availableSize' below could not be undone.
					// Returning -1, -1 to indicate no desired size could be measured.
					return new Size(-1.0f, -1.0f);
				}
				else
				{
					measureWidthToRestore = arrangeWidth;
				}
			}
			else if (m_elementAvailableWidths != null && m_elementDesiredWidths != null)
			{
				// Check if the element has a recorded desired or available width
				// to restore after measurement with the provided 'availableSize'.
				if (m_elementAvailableWidths.TryGetValue(element, out float recordedAvailableWidth))
				{
					// Use the previous available width to undo the effects of the measure with 'availableSize'.
					measureWidthToRestore = recordedAvailableWidth;
				}
				else if (m_elementDesiredWidths.TryGetValue(element, out float recordedDesiredWidth))
				{
					// Use the previous desired width to undo the effects of the measure with 'availableSize'.
					measureWidthToRestore = recordedDesiredWidth;
				}
				else
				{
					// No recorded width to use to undo effects of a measure with 'availableSize'.
					return new Size(-1.0f, -1.0f);
				}
			}

			element.Measure(availableSize);

			Size desiredSize = element.DesiredSize;

			element.Measure(new Size(measureWidthToRestore, (float)actualLineHeightToRestore));

			return desiredSize;
		}

		// Computes the expansion factor for a line with a desired width smaller than the available width.
		internal double ComputeLineExpandFactor(
			bool forward,
			int sizedItemIndex,
			int lineItemsCount,
			double lineItemsWidth,
			double availableWidth,
			double minItemSpacings,
			double actualLineHeight,
			double averageAspectRatio)
		{
			bool usesFastPathLayout = UsesFastPathLayout();
			double scaleFactor;
			bool largerScaleFactorNeeded;
			HashSet<int> ignoredItemIndexes = new();

			MUX_ASSERT(lineItemsWidth < availableWidth);

			availableWidth -= minItemSpacings;
			lineItemsWidth -= minItemSpacings;

			MUX_ASSERT(availableWidth > 0.0);
			MUX_ASSERT(lineItemsWidth > 0.0);

			do
			{
				int sizedItemIndexTmp = sizedItemIndex;

				largerScaleFactorNeeded = false;
				scaleFactor = availableWidth / lineItemsWidth;

				for (int itemIndex = 0; itemIndex < lineItemsCount; itemIndex++)
				{
					if (!ignoredItemIndexes.Contains(itemIndex))
					{
						double minWidth = 0.0, maxWidth = double.PositiveInfinity, desiredWidth = 0.0;
						UIElement? element = null;

						if (!usesFastPathLayout && m_itemsInfoFirstIndex == -1)
						{
							MUX_ASSERT(m_elementManager.IsDataIndexRealized(sizedItemIndexTmp));

							if ((element = m_elementManager.GetRealizedElement(sizedItemIndexTmp /*dataIndex*/)) != null)
							{
								if (element is FrameworkElement frameworkElement)
								{
									minWidth = frameworkElement.MinWidth;
									maxWidth = frameworkElement.MaxWidth;
									desiredWidth = element.DesiredSize.Width;
								}
							}
						}
						else
						{
							double desiredAspectRatio = GetDesiredAspectRatioFromItemsInfo(sizedItemIndexTmp, usesFastPathLayout);

							if (desiredAspectRatio <= 0)
							{
								// The average aspect ratio is used as a fallback value for items that were given a negative ratio
								// by the ItemsInfoRequested handler.
								desiredAspectRatio = averageAspectRatio;
							}

							desiredWidth = desiredAspectRatio * actualLineHeight;

							double desiredMinWidth = GetMinWidthFromItemsInfo(sizedItemIndexTmp);

							if (desiredMinWidth >= 0.0)
							{
								minWidth = desiredMinWidth;
								desiredWidth = Math.Max(minWidth, desiredWidth);
							}

							double desiredMaxWidth = GetMaxWidthFromItemsInfo(sizedItemIndexTmp);

							if (desiredMaxWidth >= 0.0)
							{
								maxWidth = desiredMaxWidth;
								desiredWidth = Math.Min(maxWidth, desiredWidth);
							}
						}

						if (maxWidth < desiredWidth * scaleFactor)
						{
							availableWidth = Math.Max(0.0, availableWidth - maxWidth);
							lineItemsWidth = Math.Max(0.0, lineItemsWidth - desiredWidth);

							ignoredItemIndexes.Add(itemIndex);
							largerScaleFactorNeeded = true;
							break;
						}

						if (element != null &&                                                  // ItemsInfoRequested event did not provide sizing information.
							(scaleFactor - 1.0) * minWidth > 1.0 / m_roundingScaleFactor &&     // scaleFactor is large enough that the rounded scaled desired width is expected to be larger than the rounded min width.
							Math.Abs(minWidth - desiredWidth) <= 1.0 / m_roundingScaleFactor)    // element's DesiredSize.Width and element.MinWidth have the same rounded value.
						{
							Size scaledAvailableSize = new Size((float)(desiredWidth * scaleFactor), (float)actualLineHeight);
							Size scaledDesiredSize = GetDesiredSizeForAvailableSize(sizedItemIndexTmp, element, scaledAvailableSize, actualLineHeight);

							if (scaledDesiredSize.Width != -1.0f && Math.Abs(minWidth - scaledDesiredSize.Width) <= 1.0 / m_roundingScaleFactor)
							{
								// These are cases where the desired width matches the minimum width even though the element is given a larger available width.
								availableWidth = Math.Max(0.0, availableWidth - minWidth);
								lineItemsWidth = Math.Max(0.0, lineItemsWidth - minWidth);

								ignoredItemIndexes.Add(itemIndex);
								largerScaleFactorNeeded = true;
								break;
							}
						}
					}

					sizedItemIndexTmp = forward ? sizedItemIndexTmp + 1 : sizedItemIndexTmp - 1;
				}
			}
			while (availableWidth > 0.0 && lineItemsWidth > 0.0 && largerScaleFactorNeeded);

			MUX_ASSERT(scaleFactor > 1.0);

			return scaleFactor;
		}

		// Computes the shrinking factor for a line with a desired width larger than the available width.
		// Returns 0 when the items' minimum width prevent enough shrinking to accommodate the available width.
		internal double ComputeLineShrinkFactor(
			bool forward,
			int sizedItemIndex,
			int lineItemsCount,
			double lineItemsWidth,
			double availableWidth,
			double minItemSpacings,
			double actualLineHeight,
			double averageAspectRatio)
		{
			bool usesFastPathLayout = UsesFastPathLayout();
			double scaleFactor;
			bool smallerScaleFactorNeeded;
			HashSet<int> ignoredItemIndexes = new();

			MUX_ASSERT(lineItemsWidth > availableWidth);

			availableWidth -= minItemSpacings;
			lineItemsWidth -= minItemSpacings;

			MUX_ASSERT(availableWidth > 0.0);
			MUX_ASSERT(lineItemsWidth > 0.0);

			do
			{
				int sizedItemIndexTmp = sizedItemIndex;

				smallerScaleFactorNeeded = false;
				scaleFactor = availableWidth / lineItemsWidth;

				for (int itemIndex = 0; itemIndex < lineItemsCount; itemIndex++)
				{
					if (!ignoredItemIndexes.Contains(itemIndex))
					{
						double minWidth = 0.0, desiredWidth = 0.0;

						if (!usesFastPathLayout && m_itemsInfoFirstIndex == -1)
						{
							MUX_ASSERT(m_elementManager.IsDataIndexRealized(sizedItemIndexTmp));

							if (m_elementManager.GetRealizedElement(sizedItemIndexTmp /*dataIndex*/) is { } element)
							{
								if (element is FrameworkElement frameworkElement)
								{
									minWidth = frameworkElement.MinWidth;
								}

								desiredWidth = element.DesiredSize.Width;
							}
						}
						else
						{
							double desiredAspectRatio = GetDesiredAspectRatioFromItemsInfo(sizedItemIndexTmp, usesFastPathLayout);

							if (desiredAspectRatio <= 0)
							{
								// The average aspect ratio is used as a fallback value for items that were given a negative ratio
								// by the ItemsInfoRequested handler.
								desiredAspectRatio = averageAspectRatio;
							}

							desiredWidth = desiredAspectRatio * actualLineHeight;

							double desiredMaxWidth = GetMaxWidthFromItemsInfo(sizedItemIndexTmp);

							if (desiredMaxWidth >= 0.0)
							{
								desiredWidth = Math.Min(desiredMaxWidth, desiredWidth);
							}

							double desiredMinWidth = GetMinWidthFromItemsInfo(sizedItemIndexTmp);

							if (desiredMinWidth >= 0.0)
							{
								minWidth = desiredMinWidth;
								desiredWidth = Math.Max(minWidth, desiredWidth);
							}
						}

						if (minWidth > desiredWidth * scaleFactor)
						{
							availableWidth = Math.Max(0.0, availableWidth - minWidth);
							lineItemsWidth = Math.Max(0.0, lineItemsWidth - desiredWidth);

							ignoredItemIndexes.Add(itemIndex);
							smallerScaleFactorNeeded = true;
							break;
						}
					}

					sizedItemIndexTmp = forward ? sizedItemIndexTmp + 1 : sizedItemIndexTmp - 1;
				}
			}
			while (availableWidth > 0.0 && lineItemsWidth > 0.0 && smallerScaleFactorNeeded);

			MUX_ASSERT(scaleFactor > 0.0);
			MUX_ASSERT(scaleFactor < 1.0);

			return smallerScaleFactorNeeded ? 0.0 : scaleFactor;
		}

		#endregion

		#region Measure engine: drawback + layout-worthiness + items-info setters (WS-D3c)

		// Grades an ItemsLayout: lines that exceed the available width are penalized more (cubic) than lines
		// with room to spare (quadratic). A lower drawback is a better layout.
		internal void ComputeItemsLayoutDrawback(
			double availableWidth,
			bool isLastSizedLineStretchEnabled,
			ItemsLayout itemsLayout)
		{
			int sizedLineCount = itemsLayout.m_lineItemWidths.Count;
			int lineVectorCount = isLastSizedLineStretchEnabled ? sizedLineCount : sizedLineCount - 1;

			if (lineVectorCount == 0)
			{
				itemsLayout.m_drawback = 0;
				return;
			}

			const double c_lineTooSmallExponent = 2.0;
			// Be careful when using Math.Pow(..., 3) as a negative input results in a negative output.
			// For the moment, c_lineTooLargeExponent is only applied to positive numbers, so its use is safe.
			const double c_lineTooLargeExponent = 3.0;
			double drawback = 0.0;

			for (int lineVectorIndex = 0; lineVectorIndex < lineVectorCount; lineVectorIndex++)
			{
				double lineWidthDelta = itemsLayout.m_lineItemWidths[lineVectorIndex] - availableWidth;

				if (lineWidthDelta != 0.0)
				{
					// To minimize the occurrences of lines that exceed the available width, they are graded worse than lines that have room to spare.
					drawback += Math.Pow(lineWidthDelta, lineWidthDelta > 0.0 ? c_lineTooLargeExponent : c_lineTooSmallExponent);
				}
			}

			// The last line does not participate in the grading if it's smaller than the available width, whether ItemsStretch is Fill or not.
			if (!isLastSizedLineStretchEnabled)
			{
				double lineWidthDelta = itemsLayout.m_lineItemWidths[sizedLineCount - 1] - availableWidth;

				if (lineWidthDelta > 0.0)
				{
					// The last line exceeds the available width, grade it as such.
					drawback += Math.Pow(lineWidthDelta, c_lineTooLargeExponent);
				}
			}

			itemsLayout.m_drawback = drawback;
		}

		internal bool IsItemsLayoutContractionWorthy(ItemsLayout itemsLayout)
		{
			if (itemsLayout.m_smallestTailItemWidth == double.MaxValue || itemsLayout.m_smallestTailItemWidth == 0.0)
			{
				return false;
			}

			int sizedLineCount = itemsLayout.m_lineItemWidths.Count;

			if (sizedLineCount > 1)
			{
				for (int lineVectorIndex = 0; lineVectorIndex < sizedLineCount; lineVectorIndex++)
				{
					if (itemsLayout.m_lineItemCounts[lineVectorIndex] > 1)
					{
						return true;
					}
				}
			}

			return false;
		}

		internal bool IsItemsLayoutEqualizationWorthy(
			ItemsLayout itemsLayout,
			bool withHeadItem)
		{
			if ((withHeadItem && itemsLayout.m_bestEqualizingHeadItemIndex == int.MaxValue) ||
				(!withHeadItem && itemsLayout.m_bestEqualizingTailItemIndex == int.MaxValue))
			{
				return false;
			}

			int sizedLineCount = itemsLayout.m_lineItemWidths.Count;

			if (sizedLineCount > 1)
			{
				for (int lineVectorIndex = 0; lineVectorIndex < sizedLineCount; lineVectorIndex++)
				{
					if (itemsLayout.m_lineItemCounts[lineVectorIndex] > 1)
					{
						return true;
					}
				}
			}

			return false;
		}

		// Determines whether using a larger adjustedAvailableWidth may lead to an ItemsLayout with a smaller drawback.
		internal bool IsItemsLayoutExpansionWorthy(ItemsLayout itemsLayout)
		{
			if (itemsLayout.m_smallestHeadItemWidth == double.MaxValue || itemsLayout.m_smallestHeadItemWidth == 0.0)
			{
				return false;
			}

			int sizedLineCount = itemsLayout.m_lineItemWidths.Count;
			bool isExpansionWorthy = false;

			if (sizedLineCount > 1)
			{
				for (int lineVectorIndex = 0; lineVectorIndex < sizedLineCount; lineVectorIndex++)
				{
					if (itemsLayout.m_lineItemCounts[lineVectorIndex] > 1)
					{
						isExpansionWorthy = true;
						break;
					}
				}
			}

			if (isExpansionWorthy)
			{
				int lineVectorIndex;

				for (lineVectorIndex = 0; lineVectorIndex < sizedLineCount; lineVectorIndex++)
				{
					if (itemsLayout.m_lineItemWidths[lineVectorIndex] > itemsLayout.m_availableLineItemsWidth)
					{
						break;
					}
				}

				isExpansionWorthy = lineVectorIndex < sizedLineCount;
			}

			MUX_ASSERT(!isExpansionWorthy || itemsLayout.m_smallestHeadItemWidth > 0);

			return isExpansionWorthy;
		}

		// Returns True when 'itemIndex' is locked in a line within the [beginLineIndex, endLineIndex] range.
		internal bool IsLockedItem(
			bool forward,
			int beginLineIndex,
			int endLineIndex,
			int itemIndex)
		{
			if (m_lockedItemIndexes.TryGetValue(itemIndex, out int lineIndex))
			{
				if (forward)
				{
					if (lineIndex >= beginLineIndex && lineIndex <= endLineIndex)
					{
						return true;
					}
				}
				else
				{
					if (lineIndex <= beginLineIndex && lineIndex >= endLineIndex)
					{
						return true;
					}
				}
			}

			return false;
		}

		internal void SetArrangeWidthFromItemsInfo(
			int itemIndex,
			float arrangeWidth)
		{
			MUX_ASSERT(m_itemsInfoFirstIndex >= -1);

			// m_itemsInfoFirstIndex >=  0: Regular path layout case, with items info available.
			// m_itemsInfoFirstIndex == -1: Fast path layout case, without items info available.
			int itemsInfoArrangeWidthsIndex = itemIndex - (m_itemsInfoFirstIndex == -1 ? 0 : m_itemsInfoFirstIndex);

			MUX_ASSERT(itemsInfoArrangeWidthsIndex >= 0);
			MUX_ASSERT(itemsInfoArrangeWidthsIndex < m_itemsInfoArrangeWidths.Count);

			m_itemsInfoArrangeWidths[itemsInfoArrangeWidthsIndex] = arrangeWidth;
		}

		internal void SetDesiredAspectRatioFromItemsInfo(
			int itemIndex,
			double desiredAspectRatio)
		{
			MUX_ASSERT(itemIndex < m_itemCount);
			MUX_ASSERT(m_itemsInfoFirstIndex >= 0);
			MUX_ASSERT(itemIndex - m_itemsInfoFirstIndex >= 0);
			MUX_ASSERT(itemIndex - m_itemsInfoFirstIndex < m_itemsInfoDesiredAspectRatiosForRegularPath.Count);

			m_itemsInfoDesiredAspectRatiosForRegularPath[itemIndex - m_itemsInfoFirstIndex] = desiredAspectRatio;
		}

		#endregion

		#region Measure engine: apply per-line item widths (WS-D3c)

		// Computes the final item widths based on the best ItemsLayout, available width and stretching mode.
		// Items are potentially shrunk to accommodate a smaller available line width, or stretched to accommodate a larger available line width.
		internal void ComputeItemsLayoutWithLockedItems(
			ItemsLayout itemsLayout,
			double availableWidth,
			double minItemSpacing,
			double actualLineHeight,
			double averageAspectRatio,
			int beginSizedLineIndex,
			int endSizedLineIndex,
			int beginSizedItemIndex,
			int endSizedItemIndex,
			int beginLineVectorIndex,
			bool isLastSizedLineStretchEnabled)
		{
			MUX_ASSERT(!UsesFastPathLayout());
			MUX_ASSERT(!(beginSizedLineIndex < endSizedLineIndex && beginSizedItemIndex >= endSizedItemIndex));
			MUX_ASSERT(!(beginSizedLineIndex > endSizedLineIndex && beginSizedItemIndex <= endSizedItemIndex));
			MUX_ASSERT(Math.Abs(beginSizedLineIndex - endSizedLineIndex) <= Math.Abs(beginSizedItemIndex - endSizedItemIndex));

			bool forward = beginSizedItemIndex <= endSizedItemIndex;
			int sizedLineCount = itemsLayout.m_lineItemCounts.Count;

			int lineVectorIndex;

			for (lineVectorIndex = 0; lineVectorIndex < sizedLineCount; lineVectorIndex++)
			{
				MUX_ASSERT(itemsLayout.m_lineItemCounts[lineVectorIndex] > 0);

				m_lineItemCounts[beginLineVectorIndex + lineVectorIndex] = itemsLayout.m_lineItemCounts[lineVectorIndex];
			}

			bool itemsAreStretched = ItemsStretch == LinedFlowLayoutItemsStretch.Fill;
			int sizedItemIndex = beginSizedItemIndex;

			for (lineVectorIndex = forward ? 0 : sizedLineCount - 1; ; lineVectorIndex = forward ? lineVectorIndex + 1 : lineVectorIndex - 1)
			{
				int lineItemsCount = itemsLayout.m_lineItemCounts[lineVectorIndex];
				double lineItemsWidth = itemsLayout.m_lineItemWidths[lineVectorIndex];
				double minItemSpacings = lineItemsCount > 1 ? (lineItemsCount - 1) * minItemSpacing : 0.0;
				double scaleFactor = 1.0;

				if (lineItemsWidth - minItemSpacings > 0.0)
				{
					if (lineItemsWidth > availableWidth)
					{
						scaleFactor = ComputeLineShrinkFactor(
							forward,
							sizedItemIndex,
							lineItemsCount,
							lineItemsWidth,
							availableWidth,
							minItemSpacings,
							actualLineHeight,
							averageAspectRatio);
						MUX_ASSERT(scaleFactor >= 0.0);
						MUX_ASSERT(scaleFactor < 1.0);
					}
					else if ((!forward || lineVectorIndex < (isLastSizedLineStretchEnabled ? sizedLineCount : sizedLineCount - 1)) &&
						lineItemsWidth < availableWidth &&
						itemsAreStretched)
					{
						scaleFactor = ComputeLineExpandFactor(
							forward,
							sizedItemIndex,
							lineItemsCount,
							lineItemsWidth,
							availableWidth,
							minItemSpacings,
							actualLineHeight,
							averageAspectRatio);
						MUX_ASSERT(scaleFactor > 1.0);
					}
				}

				if (scaleFactor != 1.0)
				{
					for (int itemIndex = 0; itemIndex < lineItemsCount; itemIndex++)
					{
						MUX_ASSERT((forward && sizedItemIndex <= endSizedItemIndex) || (!forward && sizedItemIndex >= endSizedItemIndex));

						double minWidth = 0.0;

						if (m_itemsInfoFirstIndex == -1)
						{
							MUX_ASSERT(m_elementManager.IsDataIndexRealized(sizedItemIndex));

							if (m_elementManager.GetRealizedElement(sizedItemIndex) is { } element)
							{
								if (scaleFactor < 1.0)
								{
									if (element is FrameworkElement frameworkElement)
									{
										minWidth = frameworkElement.MinWidth;
									}
								}

								var elementAvailableSize = new Size(
									Math.Max((float)minWidth, (float)(element.DesiredSize.Width * scaleFactor)),
									(float)actualLineHeight);

								element.Measure(elementAvailableSize);

								float itemWidth = (float)element.DesiredSize.Width;

								elementAvailableSize.Width = itemWidth;

								MUX_ASSERT(m_elementAvailableWidths != null);

								// WinUI's find-then-insert/emplace keeps the value already stored for an existing
								// key (std::map::emplace does not overwrite), i.e. TryAdd semantics.
								m_elementAvailableWidths!.TryAdd(element, itemWidth);
							}
						}
						else
						{
							MUX_ASSERT(sizedItemIndex - m_itemsInfoFirstIndex < m_itemsInfoArrangeWidths.Count);

							float arrangeWidth = GetArrangeWidthFromItemsInfo(sizedItemIndex, actualLineHeight, averageAspectRatio, scaleFactor);

							SetArrangeWidthFromItemsInfo(sizedItemIndex, arrangeWidth);

							if (m_elementManager.IsDataIndexRealized(sizedItemIndex))
							{
								if (m_elementManager.GetRealizedElement(sizedItemIndex) is { } element)
								{
									var elementAvailableSize = new Size(
										arrangeWidth,
										(float)actualLineHeight);

									element.Measure(elementAvailableSize);
								}
							}
						}

						sizedItemIndex = forward ? sizedItemIndex + 1 : sizedItemIndex - 1;
					}
				}
				else
				{
					// Measuring items even when no scale factor is applied so that they fill their available width (otherwise background-colored bands appear on the items' left/right edges).
					for (int itemIndex = 0; itemIndex < lineItemsCount; itemIndex++)
					{
						MUX_ASSERT((forward && sizedItemIndex <= endSizedItemIndex) || (!forward && sizedItemIndex >= endSizedItemIndex));
						MUX_ASSERT(m_itemsInfoFirstIndex == -1 || sizedItemIndex - m_itemsInfoFirstIndex < m_itemsInfoArrangeWidths.Count);

						float arrangeWidth = 0.0f;

						if (m_itemsInfoFirstIndex != -1)
						{
							// Case where LinedFlowLayout.ItemsInfoRequested returned partial sizing information.
							// Retrieve the item's arrange width computed from the regular-path items-info fields.
							arrangeWidth = GetArrangeWidthFromItemsInfo(sizedItemIndex, actualLineHeight, averageAspectRatio);

							// Store that arrange width in the m_itemsInfoArrangeWidths vector.
							SetArrangeWidthFromItemsInfo(sizedItemIndex, arrangeWidth);
						}

						if (m_elementManager.IsDataIndexRealized(sizedItemIndex))
						{
							if (m_elementManager.GetRealizedElement(sizedItemIndex) is { } element)
							{
								if (m_itemsInfoFirstIndex == -1)
								{
									// Case where LinedFlowLayout.ItemsInfoRequested returned no sizing information.
									// Retrieve the item's arrange width as its desired width.
									arrangeWidth = (float)element.DesiredSize.Width;
								}

								var elementAvailableSize = new Size(
									arrangeWidth,
									(float)actualLineHeight);

								element.Measure(elementAvailableSize);
							}
						}

						sizedItemIndex = forward ? sizedItemIndex + 1 : sizedItemIndex - 1;
					}

					// Making sure the items fill the line when ItemsStretch is Fill.
					MUX_ASSERT(!(itemsAreStretched && availableWidth - lineItemsWidth > lineItemsCount && lineItemsWidth > minItemSpacings && lineVectorIndex != sizedLineCount - 1));
				}

				if (forward && lineVectorIndex == sizedLineCount - 1)
				{
					break;
				}
				if (!forward && lineVectorIndex == 0)
				{
					break;
				}
			}
		}

		#endregion

		#region Measure engine: displayed & frozen lines (WS-D3c)

		// Computes the first and last line indexes at least partially (or, for forFullyDisplayedLines, fully)
		// displayed within the scroll viewport. Returns -1/-1 when nothing is displayable.
		internal void GetFirstAndLastDisplayedLineIndexes(
			double scrollViewport,
			double scrollOffset,
			double padding,
			double lineSpacing,
			double actualLineHeight,
			int lineCount,
			bool forFullyDisplayedLines,
			out int firstDisplayedLineIndex,
			out int lastDisplayedLineIndex)
		{
			MUX_ASSERT(m_isVirtualizingContext);
			MUX_ASSERT(scrollViewport != double.PositiveInfinity);
			MUX_ASSERT(lineCount > 0);

			firstDisplayedLineIndex = lastDisplayedLineIndex = -1;

			double lineHeight = actualLineHeight + lineSpacing;

			if (lineHeight == 0.0 || scrollViewport == 0.0)
			{
				return;
			}

			double linesSpacing = lineCount == 0 ? 0.0 : (lineCount - 1) * lineSpacing;
			double scrollExtent = lineCount * actualLineHeight + linesSpacing;
			double maxScrollOffset = Math.Max(0.0, scrollExtent - scrollViewport);
			double lineSpacingPortion = lineSpacing / lineHeight;

			scrollOffset = Math.Min(maxScrollOffset, scrollOffset);

			if (Math.Abs(scrollOffset) < c_offsetEqualityEpsilon)
			{
				scrollOffset = 0.0;
			}

			padding = Math.Min(padding, scrollViewport / 2.0);

			double nearPadding = Math.Min(scrollOffset, padding);
			double farPadding = Math.Min(maxScrollOffset - scrollOffset, padding);

			if (!(forFullyDisplayedLines && actualLineHeight + nearPadding + farPadding > scrollViewport))
			{
				double nearDisplayed = scrollOffset + nearPadding;
				double farDisplayed = scrollOffset + scrollViewport - farPadding;

				if (farDisplayed > 0.0 && nearDisplayed < scrollExtent)
				{
					nearDisplayed = Math.Max(0.0, nearDisplayed);
					farDisplayed = Math.Min(scrollExtent, farDisplayed);

					double fractionalFirstDisplayedLineIndex = nearDisplayed / lineHeight;
					int roundedFirstDisplayedLineIndex = (int)fractionalFirstDisplayedLineIndex;

					if ((double)roundedFirstDisplayedLineIndex + 1 - fractionalFirstDisplayedLineIndex <= lineSpacingPortion &&
						roundedFirstDisplayedLineIndex + 1 < lineCount)
					{
						// The portion displayed before the first displayed item is smaller than the line spacing - it can be ignored.
						firstDisplayedLineIndex = roundedFirstDisplayedLineIndex + 1;
					}
					else
					{
						firstDisplayedLineIndex = roundedFirstDisplayedLineIndex;
					}

					if (forFullyDisplayedLines &&
						fractionalFirstDisplayedLineIndex > roundedFirstDisplayedLineIndex &&
						roundedFirstDisplayedLineIndex + 1 < lineCount)
					{
						firstDisplayedLineIndex = roundedFirstDisplayedLineIndex + 1;
					}

					MUX_ASSERT(firstDisplayedLineIndex >= 0);
					MUX_ASSERT(firstDisplayedLineIndex < lineCount);

					double fractionalLastDisplayedLineIndex = (farDisplayed + lineSpacing) / lineHeight;
					int roundedLastDisplayedLineIndex = (int)fractionalLastDisplayedLineIndex;

					lastDisplayedLineIndex = roundedLastDisplayedLineIndex;

					if (forFullyDisplayedLines)
					{
						if (fractionalLastDisplayedLineIndex >= 1.0)
						{
							lastDisplayedLineIndex = Math.Min(roundedLastDisplayedLineIndex, lineCount) - 1;
						}
					}
					else
					{
						if (fractionalLastDisplayedLineIndex - roundedLastDisplayedLineIndex <= lineSpacingPortion)
						{
							lastDisplayedLineIndex = Math.Max(roundedLastDisplayedLineIndex - 1, 0);
						}
					}

					MUX_ASSERT(lastDisplayedLineIndex >= 0);
					MUX_ASSERT(lastDisplayedLineIndex < lineCount);

					if (firstDisplayedLineIndex > lastDisplayedLineIndex)
					{
						firstDisplayedLineIndex = lastDisplayedLineIndex = -1;
					}
				}
			}
			// else actualLineHeight is large enough that no line can be fully displayed in the scroll viewport.
		}

		// Computes the range of frozen lines/items around the displayed lines. Returns True when the scroll
		// offset jumped far enough that a full re-layout is required.
		internal bool ComputeFrozenItemsRange(
			double scrollViewport,
			double scrollOffset,
			double lineSpacing,
			double actualLineHeight,
			int lineCount,
			int beginSizedLineIndex,
			int endSizedLineIndex,
			int beginSizedItemIndex,
			int endSizedItemIndex,
			out int adjustedBeginSizedItemIndex,
			out int adjustedEndSizedItemIndex)
		{
			MUX_ASSERT(!UsesFastPathLayout());
			MUX_ASSERT(m_isVirtualizingContext == (scrollViewport != double.PositiveInfinity));
			MUX_ASSERT(!(beginSizedLineIndex < endSizedLineIndex && beginSizedItemIndex >= endSizedItemIndex));
			MUX_ASSERT(!(beginSizedLineIndex > endSizedLineIndex && beginSizedItemIndex <= endSizedItemIndex));
			MUX_ASSERT(Math.Abs(beginSizedLineIndex - endSizedLineIndex) <= Math.Abs(beginSizedItemIndex - endSizedItemIndex));

			adjustedBeginSizedItemIndex = -1;
			adjustedEndSizedItemIndex = -1;

			if (!m_isVirtualizingContext)
			{
				// Layout operating in non-virtualizing mode. All lines and items are frozen.
				MUX_ASSERT(m_firstSizedLineIndex == 0);
				MUX_ASSERT(m_itemCount > 0);

				m_firstFrozenLineIndex = 0;
				m_lastFrozenLineIndex = m_lastSizedLineIndex;
				m_firstFrozenItemIndex = 0;
				m_lastFrozenItemIndex = m_itemCount - 1;
				return false;
			}

			GetFirstAndLastDisplayedLineIndexes(
				scrollViewport,
				scrollOffset,
				0 /*padding*/,
				lineSpacing,
				actualLineHeight,
				lineCount,
				false /*forFullyDisplayedLines*/,
				out int firstDisplayedLineIndex,
				out int lastDisplayedLineIndex);

			if (firstDisplayedLineIndex == -1)
			{
				// This situation occurs when actualLineHeight + lineSpacing or context.VisibleRect().Height is 0.
				m_firstFrozenLineIndex = -1;
				m_lastFrozenLineIndex = -1;
				m_firstFrozenItemIndex = -1;
				m_lastFrozenItemIndex = -1;
				return false;
			}

			MUX_ASSERT(lastDisplayedLineIndex != -1);
			MUX_ASSERT(firstDisplayedLineIndex >= m_firstSizedLineIndex);
			MUX_ASSERT(lastDisplayedLineIndex <= m_lastSizedLineIndex);

			int linesPerScrollViewport = (int)(scrollViewport / (actualLineHeight + lineSpacing));

			// Frozen lines before the first displayed line use 80% of a scroll viewport or 40% of the sized area whichever is largest.
			int nearFrozenLinesCount = Math.Min((int)(c_frozenLinesRatio * linesPerScrollViewport) + 1, firstDisplayedLineIndex - m_firstSizedLineIndex);
			nearFrozenLinesCount = Math.Max(nearFrozenLinesCount, (int)(c_frozenLinesRatio / 2.0 * ((double)firstDisplayedLineIndex - m_firstSizedLineIndex)));

			// Frozen lines after the last displayed line use 80% of a scroll viewport or 40% of the sized area whichever is largest.
			int farFrozenLinesCount = Math.Min((int)(c_frozenLinesRatio * linesPerScrollViewport) + 1, m_lastSizedLineIndex - lastDisplayedLineIndex);
			farFrozenLinesCount = Math.Max(farFrozenLinesCount, (int)(c_frozenLinesRatio / 2.0 * ((double)m_lastSizedLineIndex - lastDisplayedLineIndex)));

			m_firstFrozenLineIndex = firstDisplayedLineIndex - nearFrozenLinesCount;
			m_lastFrozenLineIndex = lastDisplayedLineIndex + farFrozenLinesCount;

			MUX_ASSERT(m_firstFrozenLineIndex >= m_firstSizedLineIndex);
			MUX_ASSERT(m_lastFrozenLineIndex <= m_lastSizedLineIndex);

			int nearUnfrozenLinesCount = m_firstFrozenLineIndex - beginSizedLineIndex;
			int farUnfrozenLinesCount = endSizedLineIndex - m_lastFrozenLineIndex;

			if (nearUnfrozenLinesCount < 0 || farUnfrozenLinesCount < 0)
			{
				// nearUnfrozenLinesCount < 0 occurs when the scrolling offset has decreased too fast and the first frozen line has caught up and crossed the previous first sized line.
				// farUnfrozenLinesCount < 0 occurs when the scrolling offset has increased too fast and the last frozen line has caught up and crossed the previous last sized line.
				// These conditions can also occur when performing a large programmatic offset jump via ScrollView.ScrollTo/ScrollBy. Trigger a full re-layout.
				return true;
			}

			int firstFrozenItemIndex = beginSizedItemIndex;
			int lastFrozenItemIndex = endSizedItemIndex;

			adjustedBeginSizedItemIndex = beginSizedItemIndex;
			adjustedEndSizedItemIndex = endSizedItemIndex;

			if (m_firstSizedLineIndex < beginSizedLineIndex)
			{
				MUX_ASSERT(beginSizedItemIndex > 0);
				MUX_ASSERT(m_firstSizedItemIndex <= beginSizedItemIndex);

				if (nearUnfrozenLinesCount > 0)
				{
					MUX_ASSERT(beginSizedLineIndex - m_firstSizedLineIndex + nearUnfrozenLinesCount <= m_lineItemCounts.Count);

					for (int frozenLineVectorIndex = beginSizedLineIndex - m_firstSizedLineIndex;
						frozenLineVectorIndex < beginSizedLineIndex - m_firstSizedLineIndex + nearUnfrozenLinesCount;
						frozenLineVectorIndex++)
					{
						MUX_ASSERT(m_lineItemCounts[frozenLineVectorIndex] > 0);

						adjustedBeginSizedItemIndex += m_lineItemCounts[frozenLineVectorIndex];
						m_lineItemCounts[frozenLineVectorIndex] = 0;
					}

					firstFrozenItemIndex = adjustedBeginSizedItemIndex;
				}
			}
			else if (nearUnfrozenLinesCount > 0)
			{
				MUX_ASSERT(m_firstSizedLineIndex == beginSizedLineIndex);
				MUX_ASSERT(nearUnfrozenLinesCount <= m_lineItemCounts.Count);

				for (int frozenLineVectorIndex = 0; frozenLineVectorIndex < nearUnfrozenLinesCount; frozenLineVectorIndex++)
				{
					MUX_ASSERT(m_lineItemCounts[frozenLineVectorIndex] > 0);

					firstFrozenItemIndex += m_lineItemCounts[frozenLineVectorIndex];
				}
			}

			if (endSizedLineIndex < m_lastSizedLineIndex)
			{
				if (farUnfrozenLinesCount > 0)
				{
					for (int frozenLineVectorIndex = m_lineItemCounts.Count - farUnfrozenLinesCount - m_lastSizedLineIndex + endSizedLineIndex;
						frozenLineVectorIndex < m_lineItemCounts.Count - m_lastSizedLineIndex + endSizedLineIndex;
						frozenLineVectorIndex++)
					{
						MUX_ASSERT(m_lineItemCounts[frozenLineVectorIndex] > 0);

						adjustedEndSizedItemIndex -= m_lineItemCounts[frozenLineVectorIndex];
						m_lineItemCounts[frozenLineVectorIndex] = 0;
					}

					lastFrozenItemIndex = adjustedEndSizedItemIndex;
				}
			}
			else if (farUnfrozenLinesCount > 0)
			{
				MUX_ASSERT(endSizedLineIndex == m_lastSizedLineIndex);
				MUX_ASSERT(farUnfrozenLinesCount <= m_lineItemCounts.Count);

				for (int frozenLineVectorIndex = m_lineItemCounts.Count - farUnfrozenLinesCount;
					frozenLineVectorIndex < m_lineItemCounts.Count;
					frozenLineVectorIndex++)
				{
					MUX_ASSERT(m_lineItemCounts[frozenLineVectorIndex] > 0);

					lastFrozenItemIndex -= m_lineItemCounts[frozenLineVectorIndex];
				}
			}

			m_firstFrozenItemIndex = firstFrozenItemIndex;
			m_lastFrozenItemIndex = lastFrozenItemIndex;

			return false;
		}

		#endregion
	}
}
