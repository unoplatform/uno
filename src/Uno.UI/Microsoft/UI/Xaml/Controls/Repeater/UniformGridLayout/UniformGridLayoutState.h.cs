// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	partial class UniformGridLayoutState
	{
		// public
		//void InitializeForContext(
		//	VirtualizingLayoutContext context,
		//		IFlowLayoutAlgorithmDelegates callbacks);
		//void UninitializeForContext(
		//	VirtualizingLayoutContext context);

		public FlowLayoutAlgorithm FlowAlgorithm()
		{
			return m_flowAlgorithm;
		}
		public double EffectiveItemWidth() { return m_effectiveItemWidth; }
		public double EffectiveItemHeight() { return m_effectiveItemHeight; }

		//void EnsureElementSize(
		//	Size availableSize,
		//	VirtualizingLayoutContext context,
		//	double itemWidth,
		//	double itemHeight,
		//	UniformGridLayoutItemsStretch stretch,
		//	Orientation orientation,
		//	double minRowSpacing,
		//	double minColumnSpacing,
		//	uint maxItemsPerLine);

		// private
		FlowLayoutAlgorithm m_flowAlgorithm = new FlowLayoutAlgorithm();

		double m_effectiveItemWidth = 0.0;
		double m_effectiveItemHeight = 0.0;

		//void SetSize(
		//	Size desiredItemSize,
		//	double itemWidth,
		//	double itemHeight,
		//	Size availableSize,
		//	UniformGridLayoutItemsStretch stretch,
		//	Orientation orientation,
		//	double minRowSpacing,
		//	double minColumnSpacing,
		//	uint maxItemsPerLine);
	}
}
