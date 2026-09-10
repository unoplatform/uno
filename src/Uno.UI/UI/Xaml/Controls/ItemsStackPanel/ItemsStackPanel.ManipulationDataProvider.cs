// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference OrientedVirtualizingPanel_Partial.cpp, commit 3c9c168844

#if !IS_UNIT_TESTS

using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsStackPanel : IManipulationDataProvider
	{
		private VirtualizingPanelLayout ManipulationLayout
		{
			get
			{
				CreateLayoutIfNeeded();
				return _layout;
			}
		}

		Orientation IManipulationDataProvider.PhysicalOrientation
			=> ManipulationLayout.ManipulationPhysicalOrientation;

		void IManipulationDataProvider.UpdateInManipulation(bool isInManipulation, bool isInLiveTree, double nonVirtualizingOffset)
			=> ManipulationLayout.UpdateInManipulation(isInManipulation, isInLiveTree, nonVirtualizingOffset);

		void IManipulationDataProvider.SetZoomFactor(float zoomFactor)
			=> ManipulationLayout.SetManipulationZoomFactor(zoomFactor);

		double IManipulationDataProvider.ComputePixelExtent(bool ignoreZoomFactor)
			=> ManipulationLayout.ComputeManipulationPixelExtent(ignoreZoomFactor);

		double IManipulationDataProvider.ComputePixelOffset(bool isForHorizontalOrientation)
			=> ManipulationLayout.ComputeManipulationPixelOffset(isForHorizontalOrientation);

		double IManipulationDataProvider.ComputeLogicalOffset(bool isForHorizontalOrientation, ref double pixelDelta)
			=> ManipulationLayout.ComputeManipulationLogicalOffset(isForHorizontalOrientation, ref pixelDelta);

		Size IManipulationDataProvider.GetSizeOfFirstVisibleChild()
			=> ManipulationLayout.GetSizeOfFirstVisibleChild();
	}
}

#endif
