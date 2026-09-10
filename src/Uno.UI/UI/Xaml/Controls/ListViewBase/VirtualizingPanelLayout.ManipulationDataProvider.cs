// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference OrientedVirtualizingPanel_Partial.cpp, commit 3c9c168844

#if !IS_UNIT_TESTS

using System;
using System.Linq;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public abstract partial class VirtualizingPanelLayout
	{
		private bool _isInManipulation;
		private float _manipulationZoomFactor = 1.0f;

		internal Orientation ManipulationPhysicalOrientation => ScrollOrientation;

		internal void UpdateInManipulation(bool isInManipulation, bool isInLiveTree, double nonVirtualizingOffset)
		{
			_isInManipulation = isInManipulation;

			if (isInLiveTree)
			{
				OwnerPanel.InvalidateMeasure();

				if (!isInManipulation)
				{
					// Since measure invalidation is deferred during a manipulation, force the final
					// extent and realized range to be published synchronously when it completes.
					OwnerPanel.UpdateLayout();
				}
			}
		}

		internal void SetManipulationZoomFactor(float zoomFactor)
		{
			if (_manipulationZoomFactor != zoomFactor)
			{
				_manipulationZoomFactor = zoomFactor;
				if (!_isInManipulation)
				{
					OwnerPanel.InvalidateMeasure();
				}
			}
		}

		internal double ComputeManipulationPixelExtent(bool ignoreZoomFactor)
		{
			var extent = Math.Max(0.0, EstimatePanelExtent());
			return ignoreZoomFactor ? extent : extent * _manipulationZoomFactor;
		}

		internal double ComputeManipulationPixelOffset(bool isForHorizontalOrientation)
			=> isForHorizontalOrientation
				? ScrollViewer?.HorizontalOffset ?? 0.0
				: ScrollViewer?.VerticalOffset ?? 0.0;

		internal double ComputeManipulationLogicalOffset(bool isForHorizontalOrientation, ref double pixelDelta)
		{
			var logicalOffset = ComputeManipulationPixelOffset(isForHorizontalOrientation) + pixelDelta;
			pixelDelta = 0.0;
			return logicalOffset;
		}

		internal Size GetSizeOfFirstVisibleChild()
			=> _materializedLines.FirstOrDefault()?.FirstView.DesiredSize ?? default;
	}
}

#endif
