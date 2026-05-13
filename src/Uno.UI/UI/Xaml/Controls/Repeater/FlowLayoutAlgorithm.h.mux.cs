// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayoutAlgorithm.h, commit 4b206bce3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayoutAlgorithm
{
	// Types
	internal enum LineAlignment
	{
		Start,
		Center,
		End,
		SpaceAround,
		SpaceBetween,
		SpaceEvenly,
	}

	// Methods
	internal Rect LastExtent() => m_lastExtent;

	// Types
	private enum GenerateDirection
	{
		Forward,
		Backward,
	}

	// Fields
	private readonly ElementManager m_elementManager = new();
	private Size m_lastAvailableSize;
	private double m_lastItemSpacing;
	private double m_layoutRoundFactor;
	private bool m_collectionChangePending;
	private VirtualizingLayoutContext m_context;
	private IFlowLayoutAlgorithmDelegates m_algorithmCallbacks;
	private Rect m_lastExtent;
	private int m_firstRealizedDataIndexInsideRealizationWindow = -1;
	private int m_lastRealizedDataIndexInsideRealizationWindow = -1;

	// If the scroll orientation is the same as the folow orientation
	// we will only have one line since we will never wrap. In that case
	// we do not want to align the line. We could potentially switch the
	// meaning of line alignment in this case, but I'll hold off on that
	// feature until someone asks for it - This is not a common scenario
	// anyway.
	private bool m_scrollOrientationSameAsFlow;
}
