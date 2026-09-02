// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsPresenter_IManipulationDataProvider.cpp, commit dc46907e92

using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollContentPresenter
	{
		private sealed class ManipulationDataProviderScrollInfo : IScrollInfo, IManipulationDataProvider
		{
			private readonly ScrollContentPresenter _presenter;

			internal ManipulationDataProviderScrollInfo(ScrollContentPresenter presenter, IManipulationDataProvider provider)
			{
				_presenter = presenter;
				Provider = provider;
			}

			internal IManipulationDataProvider Provider { get; }

			public Orientation PhysicalOrientation => Provider.PhysicalOrientation;

			public void UpdateInManipulation(bool isInManipulation, bool isInLiveTree, double nonVirtualizingOffset)
			{
				if (!isInManipulation && nonVirtualizingOffset >= 0.0)
				{
					if (PhysicalOrientation == Orientation.Horizontal)
					{
						_presenter.SetVerticalOffset(nonVirtualizingOffset);
					}
					else
					{
						_presenter.SetHorizontalOffset(nonVirtualizingOffset);
					}
				}

				Provider.UpdateInManipulation(isInManipulation, isInLiveTree, nonVirtualizingOffset);
			}

			public void SetZoomFactor(float zoomFactor) => Provider.SetZoomFactor(zoomFactor);

			public double ComputePixelExtent(bool ignoreZoomFactor)
			{
				var providerExtent = Provider.ComputePixelExtent(ignoreZoomFactor);
				var presenterExtent = PhysicalOrientation == Orientation.Horizontal
					? _presenter.GetExtentWidth()
					: _presenter.GetExtentHeight();

				if (ignoreZoomFactor && _presenter.m_fLastZoomFactorApplied != 0.0f)
				{
					presenterExtent /= _presenter.m_fLastZoomFactorApplied;
				}

				// Uno's virtualizing layouts already expose pixel offsets. The SCP extent additionally
				// includes the ItemsPresenter header and padding, so retain whichever estimate is larger.
				return global::System.Math.Max(providerExtent, presenterExtent);
			}

			public double ComputePixelOffset(bool isForHorizontalOrientation) => Provider.ComputePixelOffset(isForHorizontalOrientation);

			public double ComputeLogicalOffset(bool isForHorizontalOrientation, ref double pixelDelta)
				=> Provider.ComputeLogicalOffset(isForHorizontalOrientation, ref pixelDelta);

			public Size GetSizeOfFirstVisibleChild() => Provider.GetSizeOfFirstVisibleChild();

			public bool GetCanVerticallyScroll() => _presenter.GetCanVerticallyScroll();
			public void PutCanVerticallyScroll(bool value) => _presenter.PutCanVerticallyScroll(value);
			public bool GetCanHorizontallyScroll() => _presenter.GetCanHorizontallyScroll();
			public void PutCanHorizontallyScroll(bool value) => _presenter.PutCanHorizontallyScroll(value);
			public double GetExtentWidth() => _presenter.GetExtentWidth();
			public double GetExtentHeight() => _presenter.GetExtentHeight();
			public double GetViewportWidth() => _presenter.GetViewportWidth();
			public double GetViewportHeight() => _presenter.GetViewportHeight();
			public double GetHorizontalOffset() => _presenter.GetHorizontalOffset();
			public double GetVerticalOffset() => _presenter.GetVerticalOffset();
			public double GetMinHorizontalOffset() => _presenter.GetMinHorizontalOffset();
			public double GetMinVerticalOffset() => _presenter.GetMinVerticalOffset();
			public IScrollOwner GetScrollOwner() => _presenter.GetScrollOwner();
			public void PutScrollOwner(IScrollOwner value) => _presenter.PutScrollOwner(value);
			public void LineUp() => _presenter.LineUp();
			public void LineDown() => _presenter.LineDown();
			public void LineLeft() => _presenter.LineLeft();
			public void LineRight() => _presenter.LineRight();
			public void PageUp() => _presenter.PageUp();
			public void PageDown() => _presenter.PageDown();
			public void PageLeft() => _presenter.PageLeft();
			public void PageRight() => _presenter.PageRight();
			public void MouseWheelUp(uint mouseWheelDelta) => ((IScrollInfo)_presenter).MouseWheelUp(mouseWheelDelta);
			public void MouseWheelDown(uint mouseWheelDelta) => ((IScrollInfo)_presenter).MouseWheelDown(mouseWheelDelta);
			public void MouseWheelLeft(uint mouseWheelDelta) => ((IScrollInfo)_presenter).MouseWheelLeft(mouseWheelDelta);
			public void MouseWheelRight(uint mouseWheelDelta) => ((IScrollInfo)_presenter).MouseWheelRight(mouseWheelDelta);
			public void SetHorizontalOffset(double offset) => _presenter.SetHorizontalOffset(offset);
			public void SetVerticalOffset(double offset) => _presenter.SetVerticalOffset(offset);
			public Rect MakeVisible(UIElement visual, Rect rectangle) => _presenter.MakeVisible(visual, rectangle);

			public Rect MakeVisible(
				UIElement visual,
				Rect rectangle,
				bool useAnimation,
				double horizontalAlignmentRatio,
				double verticalAlignmentRatio,
				double offsetX,
				double offsetY,
				out double appliedOffsetX,
				out double appliedOffsetY)
				=> _presenter.MakeVisible(
					visual,
					rectangle,
					useAnimation,
					horizontalAlignmentRatio,
					verticalAlignmentRatio,
					offsetX,
					offsetY,
					out appliedOffsetX,
					out appliedOffsetY);
		}
	}
}
