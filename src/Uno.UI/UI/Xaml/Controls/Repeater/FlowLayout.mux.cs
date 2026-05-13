// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayout.cpp, commit 4b206bce3

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayout
{
	public FlowLayout()
	{
		LayoutId = "FlowLayout";

		UpdateIndexBasedLayoutOrientation(Orientation.Horizontal);
	}

	// #pragma region IVirtualizingLayoutOverrides

	/// <inheritdoc />
	protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
	{
		var state = context.LayoutState;
		FlowLayoutState flowState = null;
		if (state != null)
		{
			flowState = GetAsFlowState(state);
		}

		if (flowState == null)
		{
			if (state != null)
			{
				throw new InvalidOperationException("LayoutState must derive from FlowLayoutState.");
			}

			// Custom deriving layouts could potentially be stateful.
			// If that is the case, we will just create the base state required by FlowLayout ourselves.
			flowState = new FlowLayoutState();
		}

		flowState.InitializeForContext(context, this);
	}

	/// <inheritdoc />
	protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
	{
		var flowState = GetAsFlowState(context.LayoutState);
		flowState.UninitializeForContext(context);
	}

	/// <inheritdoc />
	protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
	{
		var desiredSize = GetFlowAlgorithm(context).Measure(
			availableSize,
			context,
			true, /* isWrapping*/
			m_minItemSpacing,
			m_lineSpacing,
			uint.MaxValue /* maxItemsPerLine */,
			GetScrollOrientation(),
			true /* isVirtualizationEnabled */,
			LayoutId);
		return desiredSize;
	}

	/// <inheritdoc />
	protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
	{
		var value = GetFlowAlgorithm(context).Arrange(
			finalSize,
			context,
			true, /* isWrapping */
			(FlowLayoutAlgorithm.LineAlignment)m_lineAlignment,
			LayoutId);
		return value;
	}

	/// <inheritdoc />
	protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
	{
		GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
		// Always invalidate layout to keep the view accurate.
		InvalidateLayout();
	}

	// #pragma endregion

	// #pragma region IFlowLayoutOverrides

	/// <summary>
	/// Retrieves the size to use when measuring the element at the specified index.
	/// </summary>
	protected virtual Size GetMeasureSize(int index, Size availableSize)
		=> availableSize;

	/// <summary>
	/// Retrieves the size to use as the desired size for arranging the element at the specified index.
	/// </summary>
	protected virtual Size GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize)
		=> desiredSize;

	/// <summary>
	/// Determines whether a line break should occur before the specified index.
	/// </summary>
	protected virtual bool ShouldBreakLine(int index, double remainingSpace)
		=> remainingSpace < 0;

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
			var state = context.LayoutState;
			var flowState = GetAsFlowState(state);
			var lastExtent = flowState.FlowAlgorithm.LastExtent();

			double averageItemsPerLine = 0;
			double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + m_lineSpacing;
			MUX_ASSERT(averageItemsPerLine != 0);

			double extentMajorSize = MajorSize(lastExtent) == 0 ? (itemsCount / averageItemsPerLine) * averageLineSize : MajorSize(lastExtent);
			if (itemsCount > 0 &&
				MajorSize(realizationRect) > 0 &&
				DoesRealizationWindowOverlapExtent(realizationRect, MinorMajorRect((float)MinorStart(lastExtent), (float)MajorStart(lastExtent), (float)Minor(availableSize), (float)extentMajorSize)))
			{
				double realizationWindowStartWithinExtent = MajorStart(realizationRect) - MajorStart(lastExtent);
				int lineIndex = Math.Max(0, (int)(realizationWindowStartWithinExtent / averageLineSize));
				anchorIndex = (int)(lineIndex * averageItemsPerLine);

				// Clamp it to be within valid range
				anchorIndex = Math.Max(0, Math.Min(itemsCount - 1, anchorIndex));
				offset = lineIndex * averageLineSize + MajorStart(lastExtent);
			}
		}

		return new FlowLayoutAnchorInfo(anchorIndex, offset);
	}

	/// <summary>
	/// Retrieves an anchor info for the specified target element index.
	/// </summary>
	protected virtual FlowLayoutAnchorInfo GetAnchorForTargetElement(int targetIndex, Size availableSize, VirtualizingLayoutContext context)
	{
		double offset = double.NaN;
		int index = -1;
		int itemsCount = context.ItemCount;

		if (targetIndex >= 0 && targetIndex < itemsCount)
		{
			index = targetIndex;
			var state = context.LayoutState;
			var flowState = GetAsFlowState(state);
			double averageItemsPerLine = 0;
			double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + m_lineSpacing;
			int lineIndex = (int)(targetIndex / averageItemsPerLine);
			offset = lineIndex * averageLineSize + MajorStart(flowState.FlowAlgorithm.LastExtent());
		}

		return new FlowLayoutAnchorInfo(index, offset);
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

		int itemsCount = context.ItemCount;

		if (itemsCount > 0)
		{
			var availableSizeMinor = (float)Minor(availableSize);
			var state = context.LayoutState;
			var flowState = GetAsFlowState(state);
			double averageItemsPerLine = 0;
			double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + m_lineSpacing;

			MUX_ASSERT(averageItemsPerLine != 0);
			if (firstRealized != null)
			{
				MUX_ASSERT(lastRealized != null);
				int linesBeforeFirst = (int)(firstRealizedItemIndex / averageItemsPerLine);
				double extentMajorStart = MajorStart(firstRealizedLayoutBounds) - linesBeforeFirst * averageLineSize;
				SetMajorStart(ref extent, extentMajorStart);
				int remainingItems = itemsCount - lastRealizedItemIndex - 1;
				int remainingLinesAfterLast = (int)(remainingItems / averageItemsPerLine);
				double extentMajorSize = MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) + remainingLinesAfterLast * averageLineSize;
				SetMajorSize(ref extent, extentMajorSize);

				// If the available size is infinite, we will have realized all the items in one line.
				// In that case, the extent in the non virtualizing direction should be based on the
				// right/bottom of the last realized element.
				SetMinorSize(ref extent, availableSizeMinor.IsFinite() ? availableSizeMinor : Math.Max(0.0f, MinorEnd(lastRealizedLayoutBounds)));
			}
			else
			{
				// We dont have anything realized. make an educated guess.
				int numLines = (int)Math.Ceiling(itemsCount / averageItemsPerLine);
				extent =
					availableSizeMinor.IsFinite()
						? MinorMajorRect(0, 0, availableSizeMinor, Math.Max(0.0f, (float)(numLines * averageLineSize - m_lineSpacing)))
						: MinorMajorRect(
							0,
							0,
							Math.Max(0.0f, (float)((Minor(flowState.SpecialElementDesiredSize) + m_minItemSpacing) * itemsCount - m_minItemSpacing)),
							Math.Max(0.0f, (float)(averageLineSize - m_lineSpacing)));
				REPEATER_TRACE_INFO("%*s: \tEstimating extent with no realized elements. \n", context.Indent, LayoutId);
			}

			REPEATER_TRACE_INFO(
				"%*s: \tExtent is {%.0f,%.0f}. Based on average line size {%.0f} and average items per line {%.0f}. \n",
				context.Indent, LayoutId, extent.Width, extent.Height, averageLineSize, averageItemsPerLine);
		}
		else
		{
			MUX_ASSERT(firstRealizedItemIndex == -1);
			MUX_ASSERT(lastRealizedItemIndex == -1);

			REPEATER_TRACE_INFO("%*s: \tExtent is {%.0f,%.0f}. ItemCount is 0 \n",
				context.Indent, LayoutId, extent.Width, extent.Height);
		}

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
	}

	/// <summary>
	/// Called when a line has been arranged.
	/// </summary>
	protected virtual void OnLineArranged(
		int startIndex,
		int countInLine,
		double lineSize,
		VirtualizingLayoutContext context)
	{
		REPEATER_TRACE_INFO("%*s: \tOnLineArranged startIndex:%d Count:%d LineHeight:%d \n",
			context.Indent, LayoutId, startIndex, countInLine, lineSize);

		var flowState = GetAsFlowState(context.LayoutState);
		flowState.OnLineArranged(startIndex, countInLine, lineSize, context);
	}

	// #pragma endregion

	// #pragma region IFlowLayoutAlgorithmDelegates

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context)
		=> GetMeasureSize(index, availableSize);

	Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context)
		=> GetProvisionalArrangeSize(index, measureSize, desiredSize);

	bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
		=> ShouldBreakLine(index, remainingSpace);

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
		Size availableSize,
		VirtualizingLayoutContext context)
		=> GetAnchorForRealizationRect(availableSize, context);

	FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
		int targetIndex,
		Size availableSize,
		VirtualizingLayoutContext context)
		=> GetAnchorForTargetElement(targetIndex, availableSize, context);

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
		=> OnLineArranged(
			startIndex,
			countInLine,
			lineSize,
			context);

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

			//Note: For FlowLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
			//i.e. the properties are the inverse of each other.
			var scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal;
			SetScrollOrientation(scrollOrientation);

			UpdateIndexBasedLayoutOrientation(orientation);
		}
		else if (property == MinItemSpacingProperty)
		{
			m_minItemSpacing = (double)args.NewValue;
		}
		else if (property == LineSpacingProperty)
		{
			m_lineSpacing = (double)args.NewValue;
		}
		else if (property == LineAlignmentProperty)
		{
			m_lineAlignment = (FlowLayoutLineAlignment)args.NewValue;
		}

		InvalidateLayout();
	}

	// #pragma region private helpers

	private double GetAverageLineInfo(
		Size availableSize,
		VirtualizingLayoutContext context,
		FlowLayoutState flowState,
		out double avgCountInLine)
	{
		// default to 1 item per line with 0 size
		double avgLineSize = 0;
		avgCountInLine = 1;

		MUX_ASSERT(context.ItemCount > 0);
		if (flowState.TotalLinesMeasured == 0)
		{
			var tmpElement = context.GetOrCreateElementAt(0, ElementRealizationOptions.ForceCreate | ElementRealizationOptions.SuppressAutoRecycle);
			var desiredSize = flowState.FlowAlgorithm.MeasureElement(tmpElement, 0, availableSize, context);
			context.RecycleElement(tmpElement);

			int estimatedCountInLine = Math.Max(1, (int)(Minor(availableSize) / Minor(desiredSize)));
			flowState.OnLineArranged(0, estimatedCountInLine, Major(desiredSize), context);
			flowState.SpecialElementDesiredSize = desiredSize;
		}

		avgCountInLine = Math.Max(1.0, flowState.TotalItemsPerLine / flowState.TotalLinesMeasured);
		avgLineSize = Math.Round(flowState.TotalLineSize / flowState.TotalLinesMeasured);

#if !__SKIA__
		flowState.Uno_LastKnownAverageLineSize = avgLineSize;
#endif

		return avgLineSize;
	}

	private void UpdateIndexBasedLayoutOrientation(Orientation orientation)
		=> SetIndexBasedLayoutOrientation(orientation == Orientation.Horizontal
			? IndexBasedLayoutOrientation.LeftToRight
			: IndexBasedLayoutOrientation.TopToBottom);

	// #pragma endregion
}
