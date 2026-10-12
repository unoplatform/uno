// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference UniformGridLayout.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

partial class UniformGridLayout
{
	private UniformGridLayoutState GetAsGridState(object state) => state as UniformGridLayoutState;

	private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context)
		=> GetAsGridState(context.LayoutState).FlowAlgorithm;

	private void InvalidateLayout() => base.InvalidateMeasure();

	private double LineSpacing() => Orientation == Orientation.Horizontal ? m_minRowSpacing : m_minColumnSpacing;

	private double MinItemSpacing() => Orientation == Orientation.Horizontal ? m_minColumnSpacing : m_minRowSpacing;

	// Fields
	private double m_minItemWidth = double.NaN;
	private double m_minItemHeight = double.NaN;
	private double m_minRowSpacing;
	private double m_minColumnSpacing;
	private UniformGridLayoutItemsJustification m_itemsJustification = UniformGridLayoutItemsJustification.Start;
	private UniformGridLayoutItemsStretch m_itemsStretch = UniformGridLayoutItemsStretch.None;
	private uint m_maximumRowsOrColumns = uint.MaxValue;

	// !!! WARNING !!!
	// Any storage here needs to be related to layout configuration.
	// layout specific state needs to be stored in UniformGridLayoutState.
}
