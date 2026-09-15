// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlowLayoutState.h, commit 4b206bce3

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class FlowLayoutState
{
	internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
	internal double TotalLineSize => m_totalLineSize;
	internal int TotalLinesMeasured => m_totalLinesMeasured;
	internal double TotalItemsPerLine => m_totalItemsPerLine;

	internal Size SpecialElementDesiredSize
	{
		get => m_specialElementDesiredSize;
		set => m_specialElementDesiredSize = value;
	}

	private readonly FlowLayoutAlgorithm m_flowAlgorithm = new();
	private double[] m_lineSizeEstimationBuffer = new double[BufferSize];
	private double[] m_itemsPerLineEstimationBuffer = new double[BufferSize];
	private double m_totalLineSize;
	private int m_totalLinesMeasured;
	private double m_totalItemsPerLine;
	private Size m_specialElementDesiredSize;

	private const int BufferSize = 100;
}
