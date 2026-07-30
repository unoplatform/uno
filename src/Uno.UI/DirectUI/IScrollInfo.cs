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
	// Method-shaped to match the C++ ABI directly (CanVerticallyScroll() / PutCanVerticallyScroll
	// rather than C#-style properties). This keeps the WinUI port 1:1 with the C++ source so that
	// behavior preserved by source-order can be cross-checked at the call site.
	internal interface IScrollInfo
	{
		// Property that controls how scroll info measures its child during layout. If true, it
		// measures child at infinite space in this dimension.
		bool GetCanVerticallyScroll();
		void PutCanVerticallyScroll(bool value);

		// Property that controls how scroll info measures its child during layout. If true, it
		// measures child at infinite space in this dimension.
		bool GetCanHorizontallyScroll();
		void PutCanHorizontallyScroll(bool value);

		// Gets the horizontal size of the extent.
		double GetExtentWidth();

		// Gets the vertical size of the extent.
		double GetExtentHeight();

		// Gets the horizontal size of the viewport for this content.
		double GetViewportWidth();

		// Gets the vertical size of the viewport for this content.
		double GetViewportHeight();

		// Gets the horizontal offset of the scrolled content.
		double GetHorizontalOffset();

		// Gets the vertical offset of the scrolled content.
		double GetVerticalOffset();

		// Gets the minimal horizontal offset of the scrolled content.
		double GetMinHorizontalOffset();

		// Gets the minimal vertical offset of the scrolled content.
		double GetMinVerticalOffset();

		// ScrollOwner is the container that controls any scrollbars, headers, etc...
		// that are dependent on this IScrollInfo's properties. Implementers of
		// IScrollInfo should call InvalidateScrollInfo() on this object when
		// properties change.
		IScrollOwner GetScrollOwner();
		void PutScrollOwner(IScrollOwner value);

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

		// This scrolls to make the rectangle in the UIElement's coordinate space visible.
		// Alignment ratios are either NaN (i.e. no alignment to apply) or between
		// 0 and 1. For instance when the alignment ratio is 0, the near edge of
		// the 'rectangle' needs to align with the near edge of the viewport.
		// 'offset' is an additional amount of scrolling requested, beyond the
		// normal amount to bring the target into view and potentially align it.
		// That additional offset is only applied when the 'rectangle' does not
		// step outside the extents.
		// The 'appliedOffset' returned specifies how much of 'offset' was applied
		// so that potential parent bring-into-view contributors can attempt to
		// apply the remainder offset.
		Rect MakeVisible(
			UIElement visual,
			Rect rectangle,
			bool useAnimation,
			double horizontalAlignmentRatio,
			double verticalAlignmentRatio,
			double offsetX,
			double offsetY,
			out double appliedOffsetX,
			out double appliedOffsetY);
	}
}
