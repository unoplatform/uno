// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference UniformGridLayoutState.h, commit 4b206bce3

namespace Microsoft.UI.Xaml.Controls;

partial class UniformGridLayoutState
{
	internal FlowLayoutAlgorithm FlowAlgorithm => m_flowAlgorithm;
	internal double EffectiveItemWidth => m_effectiveItemWidth;
	internal double EffectiveItemHeight => m_effectiveItemHeight;

	private readonly FlowLayoutAlgorithm m_flowAlgorithm = new();
	private bool m_isEffectiveSizeValid;
	private double m_effectiveItemWidth;
	private double m_effectiveItemHeight;
}
