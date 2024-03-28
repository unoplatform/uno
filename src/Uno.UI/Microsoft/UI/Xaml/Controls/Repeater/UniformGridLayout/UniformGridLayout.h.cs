// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{

	partial class UniformGridLayout : VirtualizingLayout
	{
		//		class UniformGridLayout :
		//    public ReferenceTracker<UniformGridLayout, implementation.UniformGridLayoutT, VirtualizingLayout>,
		//    public IFlowLayoutAlgorithmDelegates,
		//    public OrientationBasedMeasures,
		//    public UniformGridLayoutProperties
		//{
		// public
		//UniformGridLayout();

		#region IVirtualizingLayoutOverrides

		//void InitializeForContextCore(VirtualizingLayoutContext context);
		//void UninitializeForContextCore(VirtualizingLayoutContext context);

		//Size MeasureOverride(
		//	VirtualizingLayoutContext context,
		//	Size availableSize);

		//Size ArrangeOverride(
		//	VirtualizingLayoutContext context,
		//	Size finalSize);

		//void OnItemsChangedCore(
		//	VirtualizingLayoutContext context,
		//	IInspectable source,
		//	NotifyCollectionChangedEventArgs args);

		#endregion

		#region IFlowLayoutAlgorithmDelegates

		//Size Algorithm_GetMeasureSize(int index,  const Size availableSize,  const VirtualizingLayoutContext
		//	context) override;
		//Size Algorithm_GetProvisionalArrangeSize(int index,  const Size measureSize, Size desiredSize, const
		//	VirtualizingLayoutContext context) override;
		//bool Algorithm_ShouldBreakLine(int index, double remainingSpace) override;
		//FlowLayoutAnchorInfo Algorithm_GetAnchorForRealizationRect(
		//const Size availableSize, 
		//const VirtualizingLayoutContext context) override;

		//FlowLayoutAnchorInfo Algorithm_GetAnchorForTargetElement(
		//	int targetIndex, 

		//const Size availableSize, 
		//const VirtualizingLayoutContext context) override;
		//Rect Algorithm_GetExtent(const Size availableSize, 
		//const VirtualizingLayoutContext context, 
		//const UIElement firstRealized, 
		//int firstRealizedItemIndex,
		//const Rect firstRealizedLayoutBounds, 
		//const UIElement lastRealized, 
		//int lastRealizedItemIndex,
		//const Rect lastRealizedLayoutBounds) override;
		//void Algorithm_OnElementMeasured(
		//const UIElement  /element/,
		//int /index/,
		//const Size  /availableSize/,
		//const Size  /measureSize/,
		//const Size  /desiredSize/,
		//const Size  /provisionalArrangeSize/,
		//const VirtualizingLayoutContext  /context/)override { }

		//void Algorithm_OnLineArranged(
		//	int /startIndex/,
		//int /countInLine/,
		//double /lineSize/,
		//const VirtualizingLayoutContext  /context/)override { }

		#endregion

		//		void OnPropertyChanged(const DependencyPropertyChangedEventArgs args);

		//// private
		//		// Methods
		//		float GetMinorSizeWithSpacing(VirtualizingLayoutContext context);
		//		float GetMajorSizeWithSpacing(VirtualizingLayoutContext context);

		//		Rect GetLayoutRectForDataIndex(const Size availableSize,  int index, const Rect lastExtent,  const
		//			VirtualizingLayoutContext context);

		UniformGridLayoutState GetAsGridState(object state)
		{
			//( return get_self<UniformGridLayoutState>(state as UniformGridLayoutState)).get_strong();
			return state as UniformGridLayoutState;
		}

		FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context)
		{
			return GetAsGridState(context.LayoutState).FlowAlgorithm();
		}

		void InvalidateLayout()
		{
			base.InvalidateMeasure();
		}

		double LineSpacing()
		{
			return Orientation == Orientation.Horizontal ? m_minRowSpacing : m_minColumnSpacing;

		}

		double MinItemSpacing()
		{
			return Orientation == Orientation.Horizontal ? m_minColumnSpacing : m_minRowSpacing;
		}

		// Fields
		double m_minItemWidth = double.NaN;
		double m_minItemHeight = double.NaN;
		double m_minRowSpacing;
		double m_minColumnSpacing;
		UniformGridLayoutItemsJustification m_itemsJustification = UniformGridLayoutItemsJustification.Start;
		UniformGridLayoutItemsStretch m_itemsStretch = UniformGridLayoutItemsStretch.None;

		uint m_maximumRowsOrColumns = uint.MaxValue;
		// !!! WARNING !!!
		// Any storage here needs to be related to layout configuration.
		// layout specific state needs to be stored in UniformGridLayoutState.
	};
}
