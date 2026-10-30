// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference StackLayout.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayout
{
	private StackLayoutState GetAsStackState(object state) => state as StackLayoutState;

	private void InvalidateLayout() => base.InvalidateMeasure();

	private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context)
		=> GetAsStackState(context.LayoutState).FlowAlgorithm;

	// Fields
	private double m_itemSpacing;

	// !!! WARNING !!!
	// Any storage here needs to be related to layout configuration.
	// layout specific state needs to be stored in StackLayoutState.
}
