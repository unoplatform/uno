// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/dxaml/lib/IScrollInfo.h, commit 5f9e85113

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace DirectUI
{
	// Internal Uno port of the WPF/internal-WinUI IScrollInfo contract used between
	// ScrollContentPresenter (or any other panel that participates in scrolling)
	// and the owning ScrollViewer. WinUI uses this to abstract logical vs pixel
	// scrolling — when an inner panel implements IScrollInfo (e.g. virtualizing
	// stack panels), ScrollViewer delegates scroll operations to it instead of
	// scrolling its content via the ScrollContentPresenter.
	internal interface IScrollInfo
	{
		// Property that controls how scroll info measures its child during layout. If true, it
		// measures child at infinite space in this dimension.
		bool CanVerticallyScroll { get; set; }

		// Property that controls how scroll info measures its child during layout. If true, it
		// measures child at infinite space in this dimension.
		bool CanHorizontallyScroll { get; set; }

		// Gets the horizontal size of the extent.
		double ExtentWidth { get; }

		// Gets the vertical size of the extent.
		double ExtentHeight { get; }

		// Gets the horizontal size of the viewport for this content.
		double ViewportWidth { get; }

		// Gets the vertical size of the viewport for this content.
		double ViewportHeight { get; }

		// Gets the horizontal offset of the scrolled content.
		double HorizontalOffset { get; }

		// Gets the vertical offset of the scrolled content.
		double VerticalOffset { get; }

		// Gets the minimal horizontal offset of the scrolled content.
		double MinHorizontalOffset { get; }

		// Gets the minimal vertical offset of the scrolled content.
		double MinVerticalOffset { get; }

		// ScrollOwner is the container that controls any scrollbars, headers, etc...
		// that are dependent on this IScrollInfo's properties. Implementers of
		// IScrollInfo should call InvalidateScrollInfo() on this object when
		// properties change.
		object ScrollOwner { get; set; }

		// Scroll content by one line to the top.
		void LineUp();

		// Scroll content by one line to the bottom.
		void LineDown();

		// Scroll content by one line to the left.
		void LineLeft();

		// Scroll content by one line to the right.
		void LineRight();

		// Scroll content by one page to the top.
		void PageUp();

		// Scroll content by one page to the bottom.
		void PageDown();

		// Scroll content by one page to the left.
		void PageLeft();

		// Scroll content by one page to the right.
		void PageRight();

		// IScrollInfo mouse wheel scroll implementations, based on delta value.
		void MouseWheelUp(uint mouseWheelDelta);

		void MouseWheelDown(uint mouseWheelDelta);

		void MouseWheelLeft(uint mouseWheelDelta);

		void MouseWheelRight(uint mouseWheelDelta);

		// Set the HorizontalOffset to the passed value.
		void SetHorizontalOffset(double offset);

		// Set the VerticalOffset to the passed value.
		void SetVerticalOffset(double offset);

		// This scrolls to make the rectangle in the UIElement's coordinate space visible.
		Rect MakeVisible(UIElement visual, Rect rectangle);
	}
}
