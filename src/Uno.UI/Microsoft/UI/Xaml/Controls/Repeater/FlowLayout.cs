// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class FlowLayout : VirtualizingLayout, IFlowLayoutAlgorithmDelegates
	{
		double m_minRowSpacing;
		double m_minColumnSpacing;
		FlowLayoutLineAlignment m_lineAlignment = FlowLayoutLineAlignment.Start;

		private FlowLayoutState GetAsFlowState(object state)
		{
			return state as FlowLayoutState;
		}

		void InvalidateLayout()
		{
			InvalidateMeasure();
		}

		FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context)
		{
			return GetAsFlowState(context.LayoutState).FlowAlgorithm;
		}

		private bool DoesRealizationWindowOverlapExtent(Rect realizationWindow, Rect extent)
		{
			return MajorEnd(realizationWindow) >= MajorStart(extent) && MajorStart(realizationWindow) <= MajorEnd(extent);
		}

		double LineSpacing => ScrollOrientation == ScrollOrientation.Vertical ? m_minRowSpacing : m_minColumnSpacing;

		double MinItemSpacing => ScrollOrientation == ScrollOrientation.Vertical ? m_minColumnSpacing : m_minRowSpacing;

		public FlowLayout()
		{
			LayoutId = "FlowLayout";
		}

		#region IVirtualizingLayoutOverrides
		protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
		{
			var state = context.LayoutState;
			FlowLayoutState flowState = default;
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

		protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
		{
			var flowState = GetAsFlowState(context.LayoutState);
			flowState.UninitializeForContext(context);
		}

		protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		{
			var desiredSize = GetFlowAlgorithm(context).Measure(
				availableSize,
				context,
				true, /* isWrapping*/
				MinItemSpacing,
				LineSpacing,
				uint.MaxValue /* maxItemsPerLine */,
				ScrollOrientation,
				false /* disableVirtualization */,
				LayoutId);
			return desiredSize;
		}

		protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
		{
			var value = GetFlowAlgorithm(context).Arrange(
				finalSize,
				context,
				true, /* isWrapping */
				(FlowLayoutLineAlignment)(m_lineAlignment),
				LayoutId);
			return value;
		}


		protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
		{
			GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
			// Always invalidate layout to keep the view accurate.
			InvalidateLayout();
		}

		#endregion

		#region IFlowLayoutOverrides

		protected virtual Size GetMeasureSize(int index, Size availableSize)
		{
			return availableSize;
		}

		protected virtual Size GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize)
		{
			return desiredSize;
		}

		protected virtual bool ShouldBreakLine(int index, double remainingSpace)
		{
			return remainingSpace < 0;
		}

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
				var lastExtent = flowState.FlowAlgorithm.LastExtent;

				double averageItemsPerLine = 0;
				double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + LineSpacing;
				MUX_ASSERT(averageItemsPerLine != 0);

				double extentMajorSize = MajorSize(lastExtent) == 0 ? (itemsCount / averageItemsPerLine) * averageLineSize : MajorSize(lastExtent);
				if (itemsCount > 0 &&
					MajorSize(realizationRect) > 0 &&
					DoesRealizationWindowOverlapExtent(realizationRect, MinorMajorRect((float)MinorStart(lastExtent), (float)MajorStart(lastExtent), (float)Minor(availableSize), (float)(extentMajorSize))))
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
				double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + LineSpacing;
				int lineIndex = (int)(targetIndex / averageItemsPerLine);
				offset = lineIndex * averageLineSize + MajorStart(flowState.FlowAlgorithm.LastExtent);
			}

			return new FlowLayoutAnchorInfo(index, offset);
		}

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
			var extent = default(Rect);

			int itemsCount = context.ItemCount;

			if (itemsCount > 0)
			{
				var availableSizeMinor = (float)Minor(availableSize);
				var state = context.LayoutState;
				var flowState = GetAsFlowState(state);
				double averageItemsPerLine = 0;
				double averageLineSize = GetAverageLineInfo(availableSize, context, flowState, out averageItemsPerLine) + LineSpacing;

				MUX_ASSERT(averageItemsPerLine != 0);
				if (firstRealized != null)
				{
					MUX_ASSERT(lastRealized != null);
					int linesBeforeFirst = (int)(firstRealizedItemIndex / averageItemsPerLine);
					double extentMajorStart = MajorStart(firstRealizedLayoutBounds) - linesBeforeFirst * averageLineSize;
					SetMajorStart(ref extent, extentMajorStart);
					int remainingItems = itemsCount - lastRealizedItemIndex - 1;
					int remainingLinesAfterLast = (int)((remainingItems / averageItemsPerLine));
					double extentMajorSize = MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) + remainingLinesAfterLast * averageLineSize;
					SetMajorSize(ref extent, extentMajorSize);

					// If the available size is infinite, we will have realized all the items in one line.
					// In that case, the extent in the non virtualizing direction should be based on the
					// right/bottom of the last realized element.
					SetMinorSize(ref extent, availableSizeMinor.IsFinite() ? availableSizeMinor : Math.Max(0.0f, MinorEnd(lastRealizedLayoutBounds)));
				}
				else
				{
					var lineSpacing = LineSpacing;
					var minItemSpacing = MinItemSpacing;
					// We dont have anything realized. make an educated guess.
					int numLines = (int)Math.Ceiling(itemsCount / averageItemsPerLine);
					extent =
						availableSizeMinor.IsFinite()
							? MinorMajorRect(0, 0, availableSizeMinor, Math.Max(0.0f, (float)(numLines * averageLineSize - lineSpacing)))
							: MinorMajorRect(
								0,
								0,
								Math.Max(0.0f, (float)((Minor(flowState.SpecialElementDesiredSize) + minItemSpacing) * itemsCount - minItemSpacing)),
								Math.Max(0.0f, (float)(averageLineSize - lineSpacing)));
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

		#endregion

		#region IFlowLayoutAlgorithmDelegates

		Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context)
		{
			return GetMeasureSize(index, availableSize);
		}

		Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context)
		{
			return GetProvisionalArrangeSize(index, measureSize, desiredSize);
		}

		bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
		{
			return ShouldBreakLine(index, remainingSpace);
		}

		FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
			Size availableSize,
			VirtualizingLayoutContext context)
		{
			return GetAnchorForRealizationRect(availableSize, context);
		}

		FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
			int targetIndex,

			Size availableSize,
			VirtualizingLayoutContext context)
		{
			return GetAnchorForTargetElement(targetIndex, availableSize, context);
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
			return GetExtent(
				availableSize,
				context,
				firstRealized,
				firstRealizedItemIndex,
				firstRealizedLayoutBounds,
				lastRealized,
				lastRealizedItemIndex,
				lastRealizedLayoutBounds);
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
			OnElementMeasured(
				element,
				index,
				availableSize,
				measureSize,
				desiredSize,
				provisionalArrangeSize,
				context);
		}

		void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(
			int startIndex,
			int countInLine,
			double lineSize,
			VirtualizingLayoutContext context)
		{
			OnLineArranged(
				startIndex,
				countInLine,
				lineSize,
				context);
		}

		#endregion

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;
			if (property == OrientationProperty)
			{
				var orientation = (Orientation)args.NewValue;

				//Note: For FlowLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
				//i.e. the properties are the inverse of each other.
				ScrollOrientation scrollOrientation = (orientation == Orientation.Horizontal) ? ScrollOrientation.Vertical : ScrollOrientation.Horizontal;
				ScrollOrientation = scrollOrientation;
			}
			else if (property == MinColumnSpacingProperty)
			{
				m_minColumnSpacing = (double)args.NewValue;
			}
			else if (property == MinRowSpacingProperty)
			{
				m_minRowSpacing = (double)args.NewValue;
			}
			else if (property == LineAlignmentProperty)
			{
				m_lineAlignment = (FlowLayoutLineAlignment)args.NewValue;
			}

			InvalidateLayout();
		}

		#region private helpers

		double GetAverageLineInfo(

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

			flowState.Uno_LastKnownAverageLineSize = avgLineSize;

			return avgLineSize;
		}

		#endregion

		#region Uno workaround
		/// <inheritdoc />
		protected internal override bool IsSignificantViewportChange(object state, Rect oldViewport, Rect newViewport)
		{
			if (state is FlowLayoutState { Uno_LastKnownAverageLineSize: > 0 } flowState)
			{
				var elementSize = flowState.Uno_LastKnownAverageLineSize;
				return Math.Abs(MajorStart(oldViewport) - MajorStart(newViewport)) > elementSize * 1.5
					|| Math.Abs(MajorEnd(oldViewport) - MajorEnd(newViewport)) > elementSize * 1.5;
			}
			else
			{
				return base.IsSignificantViewportChange(state, oldViewport, newViewport);
			}
		}
		#endregion
	}
}
