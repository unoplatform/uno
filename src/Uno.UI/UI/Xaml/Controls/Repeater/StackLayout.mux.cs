// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference StackLayout.cpp, commit 4b206bce3

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayout
{
	// #pragma region IStackLayout

	public StackLayout()
	{
		// TODO Uno: RuntimeProfiler marker not ported.
		// Original C++: __RP_Marker_ClassById(RuntimeProfiler::ProfId_StackLayout);
		LayoutId = "StackLayout";

		UpdateIndexBasedLayoutOrientation(Orientation.Vertical);
	}

	// #pragma endregion

	// #pragma region IVirtualizingLayoutOverrides

	/// <inheritdoc />
	protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		var state = context.LayoutState;
		StackLayoutState stackState = null;
		if (state != null)
		{
			stackState = GetAsStackState(state);
		}

		if (stackState == null)
		{
			if (state != null)
			{
				throw new InvalidOperationException("LayoutState must derive from StackLayoutState.");
			}

			// Custom deriving layouts could potentially be stateful.
			// If that is the case, we will just create the base state required by UniformGridLayout ourselves.
			stackState = new StackLayoutState();
		}

		stackState.InitializeForContext(context, this);
	}

	/// <inheritdoc />
	protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
	{
		if (GetAsStackState(context.LayoutState) is StackLayoutState stackState)
		{
			stackState.UninitializeForContext(context);
		}
	}

	/// <inheritdoc />
	protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		if (context.LayoutState == null)
		{
			return default;
		}

		var stackState = GetAsStackState(context.LayoutState);
		stackState.OnMeasureStart();

		var algo = GetFlowAlgorithm(context);
		var desiredSize = algo.Measure(
			availableSize,
			context,
			false, /* isWrapping */
			0 /* minItemSpacing */,
			m_itemSpacing,
			uint.MaxValue /* maxItemsPerLine */,
			GetScrollOrientation(),
			IsVirtualizationEnabled,
			LayoutId);

#if !__SKIA__
		// Uno workaround: Keep track of realized items count for viewport invalidation optimization on native targets
		stackState.Uno_LastKnownItemsCount = context.ItemCount;
		stackState.Uno_LastKnownRealizedElementsCount = algo.RealizedElementCount;
		stackState.Uno_LastKnownDesiredSize = desiredSize;
#endif

		return new Size(desiredSize.Width, desiredSize.Height);
	}

	/// <inheritdoc />
	protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		if (context.LayoutState == null)
		{
			return default;
		}

		var value = GetFlowAlgorithm(context).Arrange(
			finalSize,
			context,
			false, /* isWraping */
			FlowLayoutAlgorithm.LineAlignment.Start,
			LayoutId);

		return new Size(value.Width, value.Height);
	}

	/// <inheritdoc />
	protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		if (context.LayoutState is { } layoutState)
		{
			if (GetAsStackState(layoutState) is StackLayoutState stackState)
			{
				if (args.Action == NotifyCollectionChangedAction.Reset)
				{
					stackState.OnElementSizesReset();
				}

				var flowAlgorithm = stackState.FlowAlgorithm;
				flowAlgorithm.OnItemsSourceChanged(source, args, context);
			}
		}

		// Always invalidate layout to keep the view accurate.
		InvalidateLayout();
	}

	// #pragma endregion

	// #pragma region IStackLayoutOverrides

	/// <summary>
	/// Retrieves an anchor info for the realization rect.
	/// </summary>
	protected virtual FlowLayoutAnchorInfo GetAnchorForRealizationRect(Size availableSize, VirtualizingLayoutContext context)
	{
		int anchorIndex = -1;
		double offset = double.NaN;

		// Constants
		int itemsCount = context.ItemCount;

		if (itemsCount > 0)
		{
			var realizationRect = context.RealizationRect;
			var stackState = GetAsStackState(context.LayoutState);
			var lastExtent = stackState.FlowAlgorithm.LastExtent();

			double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + m_itemSpacing;
			double realizationWindowOffsetInExtent = MajorStart(realizationRect) - MajorStart(lastExtent);
			double majorSize = MajorSize(lastExtent) == 0 ? Math.Max(0.0, averageElementSize * itemsCount - m_itemSpacing) : MajorSize(lastExtent);

			if (MajorSize(realizationRect) >= 0 &&
				// MajorSize = 0 will account for when a nested repeater is outside the realization rect but still being measured. Also,
				// note that if we are measuring this repeater, then we are already realizing an element to figure out the size, so we could
				// just keep that element alive. It also helps in XYFocus scenarios to have an element realized for XYFocus to find a candidate
				// in the navigating direction.
				realizationWindowOffsetInExtent + MajorSize(realizationRect) >= 0 && realizationWindowOffsetInExtent <= majorSize)
			{
				anchorIndex = (int)(realizationWindowOffsetInExtent / averageElementSize);
				anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, anchorIndex));
				offset = anchorIndex * averageElementSize + MajorStart(lastExtent);
			}
		}

		return new FlowLayoutAnchorInfo(anchorIndex, offset);
	}

	/// <summary>
	/// Retrieves the extent of the layout based on the current realized elements.
	/// </summary>
	protected virtual Rect GetExtent(
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
		var stackState = GetAsStackState(context.LayoutState);
		double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + m_itemSpacing;

		SetMinorSize(ref extent, stackState.MaxArrangeBounds);
		SetMajorSize(ref extent, Math.Max(0.0f, (float)(itemsCount * averageElementSize - m_itemSpacing)));

		if (itemsCount > 0)
		{
			if (firstRealized != null)
			{
				MUX_ASSERT(lastRealized != null);

				SetMajorStart(ref extent, MajorStart(firstRealizedLayoutBounds) - firstRealizedItemIndex * averageElementSize);
				var remainingItems = itemsCount - lastRealizedItemIndex - 1;
				SetMajorSize(ref extent, MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) + remainingItems * averageElementSize);
			}
			else
			{
				REPEATER_TRACE_INFO("%*s: \tEstimating extent with no realized elements.  \n",
					context.Indent,
					LayoutId);
			}
		}
		else
		{
			MUX_ASSERT(firstRealizedItemIndex == -1);
			MUX_ASSERT(lastRealizedItemIndex == -1);
		}

		REPEATER_TRACE_INFO("%*s: \tExtent is (%.0f,%.0f). Based on average %.0f. \n",
			context.Indent,
			LayoutId, extent.Width, extent.Height, averageElementSize);

		return extent;
	}

	/// <summary>
	/// Called when an element has been measured.
	/// </summary>
	protected virtual void OnElementMeasured(
		UIElement element,
		int index,
		Size availableSize,
		Size measureSize,
		Size desiredSize,
		Size provisionalArrangeSize,
		VirtualizingLayoutContext context)
	{
		if (context is VirtualizingLayoutContext virtualContext)
		{
			var stackState = GetAsStackState(virtualContext.LayoutState);
			var provisionalArrangeSizeWinRt = provisionalArrangeSize;
			stackState.OnElementMeasured(
				index,
				Major(provisionalArrangeSizeWinRt),
				Minor(provisionalArrangeSizeWinRt));
		}
	}

	// #pragma endregion

	// #pragma region IFlowLayoutAlgorithmDelegates

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context)
		=> availableSize;

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context)
		=> desiredSize;

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
		=> true;

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
		Size availableSize,
		VirtualizingLayoutContext context)
		=> GetAnchorForRealizationRect(availableSize, context);

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
		int targetIndex,
		Size availableSize,
		VirtualizingLayoutContext context)
	{
		double offset = double.NaN;
		int index = -1;
		int itemsCount = context.ItemCount;

		if (targetIndex >= 0 && targetIndex < itemsCount)
		{
			index = targetIndex;
			var stackState = GetAsStackState(context.LayoutState);
			double averageElementSize = GetAverageElementSize(availableSize, context, stackState) + m_itemSpacing;
			offset = index * averageElementSize + MajorStart(stackState.FlowAlgorithm.LastExtent());
		}

		return new FlowLayoutAnchorInfo(index, offset);
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
		=> GetExtent(
			availableSize,
			context,
			firstRealized,
			firstRealizedItemIndex,
			firstRealizedLayoutBounds,
			lastRealized,
			lastRealizedItemIndex,
			lastRealizedLayoutBounds);

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnElementMeasured(
		UIElement element,
		int index,
		Size availableSize,
		Size measureSize,
		Size desiredSize,
		Size provisionalArrangeSize,
		VirtualizingLayoutContext context)
		=> OnElementMeasured(
			element,
			index,
			availableSize,
			measureSize,
			desiredSize,
			provisionalArrangeSize,
			context);

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(
		int startIndex,
		int countInLine,
		double lineSize,
		VirtualizingLayoutContext context)
	{
	}

	void IFlowLayoutAlgorithmDelegates.Algorithm_OnLayoutRoundFactorChanged(VirtualizingLayoutContext context)
	{
		if (GetAsStackState(context.LayoutState) is StackLayoutState stackState)
		{
			stackState.OnElementSizesReset();
			InvalidateLayout();
		}
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

			//Note: For StackLayout Vertical Orientation means we have a Vertical ScrollOrientation.
			//Horizontal Orientation means we have a Horizontal ScrollOrientation.
			var scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Horizontal : ScrollOrientation.Vertical;
			SetScrollOrientation(scrollOrientation);

			UpdateIndexBasedLayoutOrientation(orientation);
		}
		else if (property == SpacingProperty)
		{
			m_itemSpacing = (double)args.NewValue;
		}

		InvalidateLayout();
	}

	// #pragma region private helpers

	// Returns values that do not include m_itemSpacing.
	private double GetAverageElementSize(
		Size availableSize,
		VirtualizingLayoutContext context,
		StackLayoutState stackState)
	{
		if (context.ItemCount == 0)
		{
			return default;
		}

		if (stackState.TotalElementsMeasured == 0)
		{
			var tmpElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
			stackState.FlowAlgorithm.MeasureElement(tmpElement, 0, availableSize, context);
			context.RecycleElement(tmpElement);
		}

		MUX_ASSERT(stackState.TotalElementsMeasured > 0);

		double averageElementSize = stackState.TotalElementSize / stackState.TotalElementsMeasured;

		if (!stackState.AreElementsMeasuredRegular)
		{
			averageElementSize = Math.Round(averageElementSize);
		}

#if !__SKIA__
		// Uno workaround: cache for viewport-invalidation optimization on native targets
		stackState.Uno_LastKnownAverageElementSize = averageElementSize;
#endif

		return averageElementSize;
	}

	private void UpdateIndexBasedLayoutOrientation(Orientation orientation)
		=> SetIndexBasedLayoutOrientation(orientation == Orientation.Horizontal
			? IndexBasedLayoutOrientation.LeftToRight
			: IndexBasedLayoutOrientation.TopToBottom);

	// #pragma endregion
}
