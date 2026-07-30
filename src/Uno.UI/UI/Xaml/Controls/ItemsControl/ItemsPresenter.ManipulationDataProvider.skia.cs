// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsPresenter_IManipulationDataProvider.cpp, commit dc46907e92

#if !IS_UNIT_TESTS

using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsPresenter : IManipulationDataProvider
	{
		private float _manipulationZoomFactor = 1.0f;

		Orientation IManipulationDataProvider.PhysicalOrientation
			=> (Panel as IManipulationDataProvider)?.PhysicalOrientation ?? Orientation;

		void IManipulationDataProvider.UpdateInManipulation(
			bool isInManipulation,
			bool isInLiveTree,
			double nonVirtualizingOffset)
		{
			if (isInLiveTree)
			{
				InvalidateMeasure();
			}

			(Panel as IManipulationDataProvider)?.UpdateInManipulation(
				isInManipulation,
				isInLiveTree,
				nonVirtualizingOffset);
		}

		void IManipulationDataProvider.SetZoomFactor(float zoomFactor)
		{
			_manipulationZoomFactor = zoomFactor;
			(Panel as IManipulationDataProvider)?.SetZoomFactor(zoomFactor);
			InvalidateMeasure();
		}

		double IManipulationDataProvider.ComputePixelExtent(bool ignoreZoomFactor)
		{
			var provider = Panel as IManipulationDataProvider;
			var extent = provider?.ComputePixelExtent(ignoreZoomFactor) ?? 0.0;
			var zoomFactor = ignoreZoomFactor ? 1.0 : _manipulationZoomFactor;
			var headerSize = HeaderContentControl?.DesiredSize ?? default;
			var padding = AppliedPadding;

			return extent + (Orientation == Orientation.Horizontal
				? headerSize.Width + padding.Left + padding.Right
				: headerSize.Height + padding.Top + padding.Bottom) * zoomFactor;
		}

		double IManipulationDataProvider.ComputePixelOffset(bool isForHorizontalOrientation)
			=> (Panel as IManipulationDataProvider)?.ComputePixelOffset(isForHorizontalOrientation) ?? 0.0;

		double IManipulationDataProvider.ComputeLogicalOffset(
			bool isForHorizontalOrientation,
			ref double pixelDelta)
		{
			if (Panel is IManipulationDataProvider provider)
			{
				return provider.ComputeLogicalOffset(isForHorizontalOrientation, ref pixelDelta);
			}

			var logicalOffset = pixelDelta;
			pixelDelta = 0.0;
			return logicalOffset;
		}

		Size IManipulationDataProvider.GetSizeOfFirstVisibleChild()
			=> (Panel as IManipulationDataProvider)?.GetSizeOfFirstVisibleChild() ?? default;
	}
}

#endif
