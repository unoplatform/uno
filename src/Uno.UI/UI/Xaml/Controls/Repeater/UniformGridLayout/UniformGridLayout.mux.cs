// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference UniformGridLayout.cpp, commit 4b206bce3

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class UniformGridLayout
{
	// #pragma region IUniformGridLayout

	public UniformGridLayout()
	{
		// TODO Uno: RuntimeProfiler marker not ported.
		// Original C++: __RP_Marker_ClassById(RuntimeProfiler::ProfId_UniformGridLayout);
		LayoutId = "UniformGridLayout";

		UpdateIndexBasedLayoutOrientation(Orientation.Horizontal);
	}

	// #pragma endregion

	// #pragma region IVirtualizingLayoutOverrides

	/// <inheritdoc />
	protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		var state = context.LayoutState;
		UniformGridLayoutState gridState = null;
		if (state != null)
		{
			gridState = GetAsGridState(state);
		}

		if (gridState == null)
		{
			if (state != null)
			{
				throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState.");
			}

			// Custom deriving layouts could potentially be stateful.
			// If that is the case, we will just create the base state required by UniformGridLayout ourselves.
			gridState = new UniformGridLayoutState();
		}

		gridState.InitializeForContext(context, this);
	}

	/// <inheritdoc />
	protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
	{
		var gridState = GetAsGridState(context.LayoutState);
		gridState.UninitializeForContext(context);
	}

	/// <inheritdoc />
	protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		// Set the width and height on the grid state. If the user already set them then use the preset.
		// If not, we have to measure the first element and get back a size which we're going to be using for the rest of the items.
		var gridState = GetAsGridState(context.LayoutState);
		gridState.EnsureElementSize(availableSize, context, m_minItemWidth, m_minItemHeight, m_itemsStretch, Orientation, MinRowSpacing, MinColumnSpacing, m_maximumRowsOrColumns);

		var desiredSize = GetFlowAlgorithm(context).Measure(
			availableSize,
			context,
			true, /* isWrapping */
			MinItemSpacing(),
			LineSpacing(),
			m_maximumRowsOrColumns /* maxItemsPerLine */,
			GetScrollOrientation(),
			true /* isVirtualizationEnabled */,
			LayoutId);

		return new Size(desiredSize.Width, desiredSize.Height);
	}

	/// <inheritdoc />
	protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		var value = GetFlowAlgorithm(context).Arrange(
			finalSize,
			context,
			true, /* isWrapping */
			(FlowLayoutAlgorithm.LineAlignment)m_itemsJustification,
			LayoutId);

		var gridState = GetAsGridState(context.LayoutState);
		gridState.InvalidateElementSize();

		return new Size(value.Width, value.Height);
	}

	/// <inheritdoc />
	protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
		// Always invalidate layout to keep the view accurate.
		InvalidateLayout();
	}

	// #pragma endregion

	// #pragma region IFlowLayoutAlgorithmDelegates

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context)
	{
		var gridState = GetAsGridState(context.LayoutState);
		return new Size((float)gridState.EffectiveItemWidth, (float)gridState.EffectiveItemHeight);
	}

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context)
	{
		var gridState = GetAsGridState(context.LayoutState);
		return new Size((float)gridState.EffectiveItemWidth, (float)gridState.EffectiveItemHeight);
	}

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
		=> remainingSpace < 0;

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
		Size availableSize,
		VirtualizingLayoutContext context)
	{
		Rect bounds = new Rect(double.NaN, double.NaN, double.NaN, double.NaN);
		int anchorIndex = -1;

		int itemsCount = context.ItemCount;
		var realizationRect = context.RealizationRect;

		if (itemsCount > 0 && MajorSize(realizationRect) > 0)
		{
			var gridState = GetAsGridState(context.LayoutState);
			var lastExtent = gridState.FlowAlgorithm.LastExtent();
			uint itemsPerLine = GetItemsPerLine(availableSize, context);
			float majorSize = GetMajorSize(itemsCount, itemsPerLine, GetMajorItemSizeWithSpacing(context));
			double realizationWindowStartWithinExtent = MajorStart(realizationRect) - MajorStart(lastExtent);

			if (realizationWindowStartWithinExtent + MajorSize(realizationRect) >= 0.0f && realizationWindowStartWithinExtent <= majorSize)
			{
				double offset = Math.Max(0.0f, MajorStart(realizationRect) - MajorStart(lastExtent));
				int anchorLineIndex = (int)(offset / GetMajorItemSizeWithSpacing(context));

				anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, (int)(anchorLineIndex * itemsPerLine)));
				bounds = GetLayoutRectForDataIndex(availableSize, anchorIndex, lastExtent, context);
			}
		}

		return new FlowLayoutAnchorInfo(anchorIndex, MajorStart(bounds));
	}

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
		int targetIndex,
		Size availableSize,
		VirtualizingLayoutContext context)
	{
		int count = context.ItemCount;
		if (targetIndex >= 0 && targetIndex < count)
		{
			// The anchor index returned is NOT the first index in the targetIndex's line. It is the targetIndex
			// itself, in order to stay consistent with the ElementManager::DiscardElementsOutsideWindow method
			// which keeps a single element prior to the realization window. If the first index in the targetIndex's
			// line were used as the anchor, it would be discarded and re-recreated in an infinite loop.
			var gridState = GetAsGridState(context.LayoutState);
			double offset = MajorStart(GetLayoutRectForDataIndex(availableSize, targetIndex, gridState.FlowAlgorithm.LastExtent(), context));

			return new FlowLayoutAnchorInfo(targetIndex, offset);
		}

		return new FlowLayoutAnchorInfo(-1, double.NaN);
	}

	Rect IFlowLayoutAlgorithmDelegates.Algorithm_GetExtent(
		Size availableSize,
		VirtualizingLayoutContext context,
		UIElement firstRealized,
		int firstRealizedItemIndex,
		Rect firstRealizedLayoutBounds,
		UIElement lastRealized,
		int lastRealizedItemIndex,
		Rect lastRealizedLayoutBounds)
	{
		// UNREFERENCED_PARAMETER(lastRealized);

		var extent = default(Rect);

		// Constants
		int itemsCount = context.ItemCount;
		double availableSizeMinor = Minor(availableSize);

		uint itemsPerLine = Math.Min( // note use of uints
			Math.Max(1u, availableSizeMinor.IsFinite()
				? (uint)((availableSizeMinor + MinItemSpacing()) / GetMinorItemSizeWithSpacing(context))
				: (uint)itemsCount),
			Math.Max(1u, m_maximumRowsOrColumns));
		float lineSize = GetMajorItemSizeWithSpacing(context);

		if (itemsCount > 0)
		{
			// Only use all of the space if item stretch is fill, otherwise size layout according to items placed
			SetMinorSize(ref extent,
				availableSizeMinor.IsFinite() && m_itemsStretch == UniformGridLayoutItemsStretch.Fill
					? availableSizeMinor
					: Math.Max(0.0f, itemsPerLine * GetMinorItemSizeWithSpacing(context) - (float)MinItemSpacing()));
			SetMajorSize(ref extent, GetMajorSize(itemsCount, itemsPerLine, lineSize));

			if (firstRealized != null)
			{
				MUX_ASSERT(lastRealized != null);

				SetMajorStart(ref extent, MajorStart(firstRealizedLayoutBounds) - (firstRealizedItemIndex / itemsPerLine) * lineSize);
				var remainingItemsOnLastRealizedLine = Math.Min(itemsCount - lastRealizedItemIndex - 1, (int)(itemsPerLine - ((lastRealizedItemIndex + 1) % itemsPerLine)));
				int remainingItems = itemsCount - lastRealizedItemIndex - 1 - remainingItemsOnLastRealizedLine;
				float remainingItemsMajorSize = GetMajorSize(remainingItems, itemsPerLine, lineSize);
				SetMajorSize(ref extent, MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) + (remainingItemsMajorSize > 0.0f ? ((float)LineSpacing() + remainingItemsMajorSize) : 0.0f));
			}
			else
			{
				REPEATER_TRACE_INFO("%s: \tEstimating extent with no realized elements. \n", LayoutId);
			}
		}
		else
		{
			MUX_ASSERT(firstRealizedItemIndex == -1);
			MUX_ASSERT(lastRealizedItemIndex == -1);
		}

		REPEATER_TRACE_INFO("%s: \tExtent is (%.0f,%.0f). Based on lineSize %.0f and items per line %.0f. \n",
			LayoutId, extent.Width, extent.Height, lineSize, itemsPerLine);
		return extent;
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(
		UIElement element,
		int index,
		Size availableSize,
		Size measureSize,
		Size desiredSize,
		Size provisionalArrangeSize,
		VirtualizingLayoutContext context)
	{
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(
		int startIndex,
		int countInLine,
		double lineSize,
		VirtualizingLayoutContext context)
	{
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLayoutRoundFactorChanged(VirtualizingLayoutContext context)
	{
	}

	int IFlowLayoutAlgorithmDelegates.Algorithm_GetFlowLayoutLogItemIndexDbg() => LogItemIndexDbg();

	void IFlowLayoutAlgorithmDelegates.Algorithm_SetFlowLayoutAnchorInfoDbg(int index, double offset)
		=> SetLayoutAnchorInfoDbg(index, offset);

	// #pragma endregion

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;
		if (property == OrientationProperty)
		{
			var orientation = (Orientation)args.NewValue;

			//Note: For UniformGridLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
			//i.e. the properties are the inverse of each other.
			var scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal;
			SetScrollOrientation(scrollOrientation);

			UpdateIndexBasedLayoutOrientation(orientation);
		}
		else if (property == MinColumnSpacingProperty)
		{
			m_minColumnSpacing = (double)args.NewValue;
		}
		else if (property == MinRowSpacingProperty)
		{
			m_minRowSpacing = (double)args.NewValue;
		}
		else if (property == ItemsJustificationProperty)
		{
			m_itemsJustification = (UniformGridLayoutItemsJustification)args.NewValue;
		}
		else if (property == ItemsStretchProperty)
		{
			m_itemsStretch = (UniformGridLayoutItemsStretch)args.NewValue;
		}
		else if (property == MinItemWidthProperty)
		{
			m_minItemWidth = (double)args.NewValue;
		}
		else if (property == MinItemHeightProperty)
		{
			m_minItemHeight = (double)args.NewValue;
		}
		else if (property == MaximumRowsOrColumnsProperty)
		{
			m_maximumRowsOrColumns = (uint)(int)args.NewValue;
		}

		InvalidateLayout();
	}

	// #pragma region private helpers

	private uint GetItemsPerLine(Size availableSize, VirtualizingLayoutContext context)
	{
		double availableSizeMinor = Minor(availableSize);
		uint maximumRowsOrColumns = Math.Max(1u, m_maximumRowsOrColumns);

		if (availableSizeMinor.IsFinite())
		{
			// Floor the cast result at 1u: when the available minor size is finite but narrower
			// than one item's minor stride, (availableSizeMinor + MinItemSpacing) /
			// GetMinorItemSizeWithSpacing evaluates to < 1 and would cast to 0u, producing an
			// integer divide-by-zero in callers (e.g. GetMajorSize, GetLayoutRectForDataIndex).
			return Math.Min(
				Math.Max(1u, (uint)((availableSizeMinor + MinItemSpacing()) / GetMinorItemSizeWithSpacing(context))),
				maximumRowsOrColumns);
		}

		return maximumRowsOrColumns;
	}

	private float GetMajorSize(int itemsCount, uint itemsPerLine, float majorItemSizeWithSpacing)
	{
		MUX_ASSERT(itemsPerLine > 0);

		int fullLinesCount = itemsCount / (int)itemsPerLine;
		int partialLineCount = (itemsCount % itemsPerLine) == 0 ? 0 : 1;
		int totalLinesCount = fullLinesCount + partialLineCount;

		if (totalLinesCount > 0)
		{
			return totalLinesCount * majorItemSizeWithSpacing - (float)LineSpacing();
		}

		return 0.0f;
	}

	private float GetMinorItemSizeWithSpacing(VirtualizingLayoutContext context)
	{
		var minItemSpacing = MinItemSpacing();
		var gridState = GetAsGridState(context.LayoutState);
		return GetScrollOrientation() == ScrollOrientation.Vertical
			? (float)(gridState.EffectiveItemWidth + minItemSpacing)
			: (float)(gridState.EffectiveItemHeight + minItemSpacing);
	}

	private float GetMajorItemSizeWithSpacing(VirtualizingLayoutContext context)
	{
		var lineSpacing = LineSpacing();
		var gridState = GetAsGridState(context.LayoutState);
		return GetScrollOrientation() == ScrollOrientation.Vertical
			? (float)(gridState.EffectiveItemHeight + lineSpacing)
			: (float)(gridState.EffectiveItemWidth + lineSpacing);
	}

	private Rect GetLayoutRectForDataIndex(
		Size availableSize,
		int index,
		Rect lastExtent,
		VirtualizingLayoutContext context)
	{
		uint itemsPerLine = GetItemsPerLine(availableSize, context);
		int lineIndex = index / (int)itemsPerLine;
		int indexInLine = index - (lineIndex * (int)itemsPerLine);

		var gridState = GetAsGridState(context.LayoutState);
		Rect bounds = MinorMajorRect(
			(float)(indexInLine * GetMinorItemSizeWithSpacing(context) + MinorStart(lastExtent)),
			(float)(lineIndex * GetMajorItemSizeWithSpacing(context) + MajorStart(lastExtent)),
			GetScrollOrientation() == ScrollOrientation.Vertical ? (float)gridState.EffectiveItemWidth : (float)gridState.EffectiveItemHeight,
			GetScrollOrientation() == ScrollOrientation.Vertical ? (float)gridState.EffectiveItemHeight : (float)gridState.EffectiveItemWidth);

		return bounds;
	}

	private void UpdateIndexBasedLayoutOrientation(Orientation orientation)
		=> SetIndexBasedLayoutOrientation(orientation == Orientation.Horizontal
			? IndexBasedLayoutOrientation.LeftToRight
			: IndexBasedLayoutOrientation.TopToBottom);

	// #pragma endregion
}
