// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference StackLayoutState.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

partial class StackLayoutState
{
	internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
	internal double TotalElementSize => m_totalElementSize;
	internal double MaxArrangeBounds => m_maxArrangeBounds;
	internal bool AreElementsMeasuredRegular => m_areElementsMeasuredRegular;
	internal int TotalElementsMeasured => m_totalElementsMeasured;

	private readonly FlowLayoutAlgorithm m_flowAlgorithm = new();
	private double[] m_estimationBuffer = new double[BufferSize];
	private double m_lastElementSize;
	private double m_totalElementSize;
	// During the measure pass, as we measure the elements, we will keep track
	// of the largest arrange bounds in the non-virtualizing direction. This value
	// is going to be used in the calculation of the extent.
	private double m_maxArrangeBounds;
	private bool m_areElementsMeasuredRegular = true;
	private int m_totalElementsMeasured;

	private const int BufferSize = 100;
}
