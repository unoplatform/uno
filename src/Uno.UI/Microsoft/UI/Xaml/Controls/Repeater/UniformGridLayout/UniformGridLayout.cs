// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// UniformGridLayout.cpp, commit 1c07867

using System;
using System.Collections.Specialized;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class UniformGridLayout : IFlowLayoutAlgorithmDelegates
	{
		#region IGridLayout

		public UniformGridLayout()
		{
			this.RegisterDisposablePropertyChangedCallback((i, s, e) => OnPropertyChanged(e));
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_UniformGridLayout);
			LayoutId = "UniformGridLayout";
		}

		#endregion

		#region IVirtualizingLayoutOverrides

		protected internal override void InitializeForContextCore(VirtualizingLayoutContext context)
		{
			var state = context.LayoutState;
			UniformGridLayoutState gridState = null;
			if (state is { })
			{
				gridState = GetAsGridState(state);
			}

			if (!(gridState is { }))
			{
				if (state is { })
				{
					//throw hresult_error(E_FAIL, "LayoutState must derive from UniformGridLayoutState.");
					throw new InvalidOperationException("LayoutState must derive from UniformGridLayoutState.");
				}

				// Custom deriving layouts could potentially be stateful.
				// If that is the case, we will just create the base state required by UniformGridLayout ourselves.
				gridState = new UniformGridLayoutState();
			}

			gridState.InitializeForContext(context, this);
		}

		protected internal override void UninitializeForContextCore(VirtualizingLayoutContext context)
		{
			var gridState = GetAsGridState(context.LayoutState);
			gridState.UninitializeForContext(context);
		}

		protected internal override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
		{
			// Set the width and height on the grid state. If the user already set them then use the preset.
			// If not, we have to measure the first element and get back a size which we're going to be using for the rest of the items.
			var gridState = GetAsGridState(context.LayoutState);
			gridState.EnsureElementSize(availableSize, context, m_minItemWidth, m_minItemHeight, m_itemsStretch,
				Orientation, MinRowSpacing, MinColumnSpacing, m_maximumRowsOrColumns);

			var desiredSize = GetFlowAlgorithm(context).Measure(
				availableSize,
				context,
				true, /* isWrapping */
				MinItemSpacing(),
				LineSpacing(),
				m_maximumRowsOrColumns /* maxItemsPerLine */,
				ScrollOrientation,
				false /* disableVirtualization */,
				LayoutId);

			return new Size(desiredSize.Width, desiredSize.Height);
		}

		protected internal override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
		{
			var value = GetFlowAlgorithm(context).Arrange(
				finalSize,
				context,
				true /* isWrapping */,
				(FlowLayoutLineAlignment)(m_itemsJustification),
				LayoutId);
			return new Size(value.Width, value.Height);
		}

		protected internal override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
		{
			GetFlowAlgorithm(context).OnItemsSourceChanged(source, args, context);
			// Always invalidate layout to keep the view accurate.
			InvalidateLayout();
		}

		#endregion

		#region IFlowLayoutAlgorithmDelegates

		Size IFlowLayoutAlgorithmDelegates.Algorithm_GetMeasureSize(int index, Size availableSize, VirtualizingLayoutContext context)

		{
			var gridState = GetAsGridState(context.LayoutState);
			return new Size(
				(float)(gridState.EffectiveItemWidth()), (float)(gridState.EffectiveItemHeight())
			);
		}

		Size IFlowLayoutAlgorithmDelegates.Algorithm_GetProvisionalArrangeSize(
			int index, Size measureSize, Size desiredSize, VirtualizingLayoutContext context)
		{
			var gridState = GetAsGridState(context.LayoutState);
			return new Size(
				(float)(gridState.EffectiveItemWidth()), (float)(gridState.EffectiveItemHeight())
			);
		}

		bool IFlowLayoutAlgorithmDelegates.Algorithm_ShouldBreakLine(int index, double remainingSpace)
		{
			return remainingSpace < 0;
		}

		FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForRealizationRect(
			Size availableSize,
			VirtualizingLayoutContext context)
		{
			Rect bounds = new Rect(
				double.NaN, double.NaN, double.NaN, double.NaN
			);
			int anchorIndex = -1;

			int itemsCount = context.ItemCount;
			var realizationRect = context.RealizationRect;
			if (itemsCount > 0 & MajorSize(realizationRect) > 0)
			{
				var gridState = GetAsGridState(context.LayoutState);
				var lastExtent = gridState.FlowAlgorithm().LastExtent;
				uint itemsPerLine = GetItemsPerLine(availableSize, context);
				double majorSize = (itemsCount / itemsPerLine) * (double)(GetMajorSizeWithSpacing(context));
				double realizationWindowStartWithinExtent =
					(double)(MajorStart(realizationRect) - MajorStart(lastExtent));
				if ((realizationWindowStartWithinExtent + MajorSize(realizationRect)) >= 0 &
					realizationWindowStartWithinExtent <= majorSize)
				{
					double offset = Math.Max(0.0f, MajorStart(realizationRect) - MajorStart(lastExtent));
					int anchorRowIndex = (int)(offset / GetMajorSizeWithSpacing(context));

					anchorIndex = (int)Math.Max(0, Math.Min(itemsCount - 1, anchorRowIndex * itemsPerLine));
					bounds = GetLayoutRectForDataIndex(availableSize, (uint)anchorIndex, lastExtent, context);
				}
			}

			return new FlowLayoutAnchorInfo
			(
				anchorIndex,
				MajorStart(bounds)
			);
		}

		FlowLayoutAnchorInfo IFlowLayoutAlgorithmDelegates.Algorithm_GetAnchorForTargetElement(
			int targetIndex,
			Size availableSize,
			VirtualizingLayoutContext context)
		{
			int index = -1;
			double offset = double.NaN;
			int count = context.ItemCount;
			if (targetIndex >= 0 & targetIndex < count)
			{
				uint itemsPerLine = GetItemsPerLine(availableSize, context);
				var indexOfFirstInLine = (targetIndex / itemsPerLine) * itemsPerLine;
				index = (int)indexOfFirstInLine;
				var state = GetAsGridState(context.LayoutState);
				offset = MajorStart(GetLayoutRectForDataIndex(availableSize, (uint)index,
					state.FlowAlgorithm().LastExtent, context));
			}

			return new FlowLayoutAnchorInfo
			(
				index,
				offset
			);
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
			//UNREFERENCED_PARAMETER(lastRealized);

			var extent = new Rect();

			// Constants
			uint itemsCount = (uint)context.ItemCount;
			var availableSizeMinor = Minor(availableSize);

			uint itemsPerLine =
				Math.Min( // note use of uint s
					Math.Max(1u, availableSizeMinor.IsFinite()
						? (uint)((availableSizeMinor + MinItemSpacing()) / GetMinorSizeWithSpacing(context))
						: itemsCount),
					Math.Max(1u, m_maximumRowsOrColumns));
			float lineSize = GetMajorSizeWithSpacing(context);

			if (itemsCount > 0)
			{
				// Only use all of the space if item stretch is fill, otherwise size layout according to items placed
				SetMinorSize(ref extent,
					availableSizeMinor.IsFinite() && m_itemsStretch == UniformGridLayoutItemsStretch.Fill
						? availableSizeMinor
						: Math.Max(0.0f, itemsPerLine * GetMinorSizeWithSpacing(context) - (float)(MinItemSpacing())));
				SetMajorSize(ref extent, Math.Max(0.0f, (itemsCount / itemsPerLine) * lineSize - (float)(LineSpacing())));

				if (firstRealized is { })
				{
					_Tracing.MUX_ASSERT(lastRealized is { });

					SetMajorStart(ref extent, MajorStart(firstRealizedLayoutBounds) -
										 (firstRealizedItemIndex / itemsPerLine) * lineSize);
					var remainingItems = itemsCount - lastRealizedItemIndex - 1;
					SetMajorSize(ref extent, MajorEnd(lastRealizedLayoutBounds) - MajorStart(extent) +
										(remainingItems / itemsPerLine) * lineSize);
				}
				else
				{
					_Tracing.REPEATER_TRACE_INFO("%ls: \tEstimating extent with no realized elements. \n", LayoutId);
				}
			}
			else
			{
				_Tracing.MUX_ASSERT(firstRealizedItemIndex == -1);
				_Tracing.MUX_ASSERT(lastRealizedItemIndex == -1);
			}

			_Tracing.REPEATER_TRACE_INFO("%ls: \tExtent is (%.0f,%.0f). Based on lineSize %.0f and items per line %.0f. \n",
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
			// Not implemented in WinUI
		}

		void IFlowLayoutAlgorithmDelegates.Algorithm_OnLineArranged(
			int startIndex,
			int countInLine,
			double lineSize,
			VirtualizingLayoutContext context)
		{
			// Not implemented in WinUI
		}

		#endregion

		void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			var property = args.Property;
			if (property == OrientationProperty)
			{
				var orientation = (Orientation)(args.NewValue);

				//Note: For UniformGridLayout Vertical Orientation means we have a Horizontal ScrollOrientation. Horizontal Orientation means we have a Vertical ScrollOrientation.
				//i.e. the properties are the inverse of each other.
				var scrollOrientation = (orientation == Orientation.Horizontal)
					? ScrollOrientation.Vertical
					: ScrollOrientation.Horizontal;
				ScrollOrientation = scrollOrientation;
			}
			else if (property == MinColumnSpacingProperty)
			{
				m_minColumnSpacing = (double)(args.NewValue);
			}
			else if (property == MinRowSpacingProperty)
			{
				m_minRowSpacing = (double)(args.NewValue);
			}
			else if (property == ItemsJustificationProperty)
			{
				m_itemsJustification = (UniformGridLayoutItemsJustification)(args.NewValue);
			}
			else if (property == ItemsStretchProperty)
			{
				m_itemsStretch = (UniformGridLayoutItemsStretch)(args.NewValue);
			}
			else if (property == MinItemWidthProperty)
			{
				m_minItemWidth = (double)(args.NewValue);
			}
			else if (property == MinItemHeightProperty)
			{
				m_minItemHeight = (double)(args.NewValue);
			}
			else if (property == MaximumRowsOrColumnsProperty)
			{
				m_maximumRowsOrColumns = (uint)((int)(args.NewValue));
			}

			InvalidateLayout();
		}

		#region private helpers

		private uint GetItemsPerLine(Size availableSize, VirtualizingLayoutContext context)
		{
			uint itemsPerLine = Math.Min( // note use of unsigned ints
				Math.Max(1u, (uint)((Minor(availableSize) + MinItemSpacing()) / GetMinorSizeWithSpacing(context))),
				Math.Max(1u, m_maximumRowsOrColumns));

			return itemsPerLine;
		}

		float GetMinorSizeWithSpacing(VirtualizingLayoutContext context)
		{
			var minItemSpacing = MinItemSpacing();
			var gridState = GetAsGridState(context.LayoutState);
			return ScrollOrientation == ScrollOrientation.Vertical
				? (float)(gridState.EffectiveItemWidth() + minItemSpacing)
				: (float)(gridState.EffectiveItemHeight() + minItemSpacing);
		}

		float GetMajorSizeWithSpacing(VirtualizingLayoutContext context)
		{
			var lineSpacing = LineSpacing();
			var gridState = GetAsGridState(context.LayoutState);
			return ScrollOrientation == ScrollOrientation.Vertical
				? (float)(gridState.EffectiveItemHeight() + lineSpacing)
				: (float)(gridState.EffectiveItemWidth() + lineSpacing);
		}

		Rect GetLayoutRectForDataIndex(
			Size availableSize,
			uint index,
			Rect lastExtent,
			VirtualizingLayoutContext context)
		{
			uint itemsPerLine = GetItemsPerLine(availableSize, context);
			uint rowIndex = index / itemsPerLine;
			uint indexInRow = index - (rowIndex * itemsPerLine);

			var gridState = GetAsGridState(context.LayoutState);
			Rect bounds = MinorMajorRect(
				indexInRow * GetMinorSizeWithSpacing(context) + MinorStart(lastExtent),
				rowIndex * GetMajorSizeWithSpacing(context) + MajorStart(lastExtent),
				ScrollOrientation == ScrollOrientation.Vertical
					? (float)(gridState.EffectiveItemWidth())
					: (float)(gridState.EffectiveItemHeight()),
				ScrollOrientation == ScrollOrientation.Vertical
					? (float)(gridState.EffectiveItemHeight())
					: (float)(gridState.EffectiveItemWidth()));

			return bounds;
		}

		#endregion
	}
}
