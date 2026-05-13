// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayout.h, commit 4b206bce3

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayout
{
	private FlowLayoutState GetAsFlowState(object state) => state as FlowLayoutState;

	private void InvalidateLayout() => InvalidateMeasure();

	private FlowLayoutAlgorithm GetFlowAlgorithm(VirtualizingLayoutContext context)
		=> GetAsFlowState(context.LayoutState).FlowAlgorithm;

	private bool DoesRealizationWindowOverlapExtent(Rect realizationWindow, Rect extent)
		=> MajorEnd(realizationWindow) >= MajorStart(extent) && MajorStart(realizationWindow) <= MajorEnd(extent);

	// Fields
	private double m_lineSpacing;
	private double m_minItemSpacing;
	private FlowLayoutLineAlignment m_lineAlignment = FlowLayoutLineAlignment.Start;

	// !!! WARNING !!!
	// Any storage here needs to be related to layout configuration.
	// layout specific state needs to be stored in FlowLayoutState.
}
