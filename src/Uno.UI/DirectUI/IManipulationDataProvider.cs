// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IManipulationDataProvider.h, commit dc46907e92

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DirectUI
{
	internal interface IManipulationDataProvider
	{
		Orientation PhysicalOrientation { get; }

		void UpdateInManipulation(bool isInManipulation, bool isInLiveTree, double nonVirtualizingOffset);

		void SetZoomFactor(float zoomFactor);

		double ComputePixelExtent(bool ignoreZoomFactor);

		double ComputePixelOffset(bool isForHorizontalOrientation);

		double ComputeLogicalOffset(bool isForHorizontalOrientation, ref double pixelDelta);

		Size GetSizeOfFirstVisibleChild();
	}
}
