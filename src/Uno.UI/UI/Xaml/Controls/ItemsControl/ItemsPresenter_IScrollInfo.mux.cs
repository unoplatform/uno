// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_IScrollInfo.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

#if !HAS_UNO // TODO Uno: Uno's ScrollViewer does not use IScrollInfo with the ItemsPresenter

using System;
using DirectUI;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
{
	// IScrollInfo interface implementation

	public bool CanVerticallyScroll
	{
		get
		{
			var value = true;

			if (Panel is IScrollInfo spScrollInfo)
			{
				value = spScrollInfo.CanVerticallyScroll;
			}

			value = value && m_ScrollData.m_canVerticallyScroll;

			return value;
		}
		set
		{
			if (m_ScrollData.m_canVerticallyScroll != value)
			{
				m_ScrollData.m_canVerticallyScroll = value;
			}

			if (Panel is IScrollInfo spScrollInfo)
			{
				spScrollInfo.CanVerticallyScroll = value;
			}
		}
	}

	public bool CanHorizontallyScroll
	{
		get
		{
			var value = true;

			if (Panel is IScrollInfo spScrollInfo)
			{
				value = spScrollInfo.CanHorizontallyScroll;
			}

			value = value && m_ScrollData.m_canHorizontallyScroll;

			return value;
		}
		set
		{
			if (m_ScrollData.m_canHorizontallyScroll != value)
			{
				m_ScrollData.m_canHorizontallyScroll = value;
			}

			if (Panel is IScrollInfo spScrollInfo)
			{
				spScrollInfo.CanHorizontallyScroll = value;
			}
		}
	}

	public double ExtentWidth => m_ScrollData.m_extent.Width;

	public double ExtentHeight => m_ScrollData.m_extent.Height;

	public double ViewportWidth => m_ScrollData.m_viewport.Width;

	public double ViewportHeight => m_ScrollData.m_viewport.Height;

	public double HorizontalOffset => m_ScrollData.m_ComputedOffset.X;

	public double VerticalOffset => m_ScrollData.m_ComputedOffset.Y;

	// MinHorizontalOffset is the minimal horizontal offset of the scrolled content
	public double MinHorizontalOffset => m_ScrollData.m_MinOffset.X;

	// MinVerticalOffset is the minimal vertical offset of the scrolled content
	public double MinVerticalOffset => m_ScrollData.m_MinOffset.Y;

	public object? ScrollOwner
	{
		get => m_ScrollData.ScrollOwner;
		set
		{
			var spNewOwner = value as IScrollOwner;
			var spOwner = m_ScrollData.ScrollOwner;

			if (spOwner != spNewOwner)
			{
				m_ScrollData.ClearLayout();
				m_ScrollData.ScrollOwner = spNewOwner;
			}

			if (Panel is IScrollInfo spScrollInfo)
			{
				var spInnerOwner = spScrollInfo.ScrollOwner;
				if (spInnerOwner != (object)this)
				{
					spScrollInfo.ScrollOwner = this;
				}
			}
		}
	}

	public void LineUp()
	{
		double offset = 0.0;
		double pixelDelta = -ScrollViewerLineDelta;
		TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
		SetVerticalOffset(offset);
	}

	public void LineDown()
	{
		double offset = 0.0;
		double pixelDelta = ScrollViewerLineDelta;
		TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
		SetVerticalOffset(offset);
	}

	public void LineLeft()
	{
		double offset = 0.0;
		double pixelDelta = -ScrollViewerLineDelta;
		TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
		SetHorizontalOffset(offset);
	}

	public void LineRight()
	{
		double offset = 0.0;
		double pixelDelta = ScrollViewerLineDelta;
		TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
		SetHorizontalOffset(offset);
	}

	public void PageUp()
	{
		double offset = 0.0;
		var desiredSize = DesiredSize;
		double pixelDelta = 0;

		pixelDelta = -desiredSize.Height;

		TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
		SetVerticalOffset(offset);
	}

	public void PageDown()
	{
		double offset = 0.0;
		var desiredSize = DesiredSize;
		double pixelDelta = 0;

		pixelDelta = desiredSize.Height;

		TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
		SetVerticalOffset(offset);
	}

	public void PageLeft()
	{
		double offset = 0.0;
		var desiredSize = DesiredSize;
		double pixelDelta = 0;

		pixelDelta = -desiredSize.Width;

		TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
		SetHorizontalOffset(offset);
	}

	public void PageRight()
	{
		double offset = 0.0;
		var desiredSize = DesiredSize;
		double pixelDelta = 0;

		pixelDelta = desiredSize.Width;

		TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
		SetHorizontalOffset(offset);
	}

	public void MouseWheelUp(uint mouseWheelDelta)
	{
		var passToScrollOwner = ShouldPassWheelMessageToScrollOwner;

		if (passToScrollOwner)
		{
			PassWheelMessageToScrollOwner(ZoomDirection.In);
		}
		else
		{
			double offset = 0.0;

			var size = DesiredSize;
			var orientation = PhysicalOrientation;
			if (orientation == Orientation.Vertical)
			{
				double pixelDelta = -GetVerticalScrollWheelDelta(size, mouseWheelDelta);
				TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
				SetVerticalOffset(offset);
			}
			else
			{
				double pixelDelta = -GetHorizontalScrollWheelDelta(size, mouseWheelDelta);
				TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
				SetHorizontalOffset(offset);
			}
		}
	}

	public void MouseWheelDown(uint mouseWheelDelta)
	{
		var passToScrollOwner = ShouldPassWheelMessageToScrollOwner;

		if (passToScrollOwner)
		{
			PassWheelMessageToScrollOwner(ZoomDirection.Out);
		}
		else
		{
			double offset = 0.0;

			var size = DesiredSize;
			var orientation = PhysicalOrientation;
			if (orientation == Orientation.Vertical)
			{
				double pixelDelta = GetVerticalScrollWheelDelta(size, mouseWheelDelta);
				TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
				SetVerticalOffset(offset);
			}
			else
			{
				double pixelDelta = GetHorizontalScrollWheelDelta(size, mouseWheelDelta);
				TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
				SetHorizontalOffset(offset);
			}
		}
	}

	public void MouseWheelLeft(uint mouseWheelDelta)
	{
		var passToScrollOwner = ShouldPassWheelMessageToScrollOwner;

		if (passToScrollOwner)
		{
			PassWheelMessageToScrollOwner(ZoomDirection.In);
		}
		else
		{
			double offset = 0.0;

			var invertForRTL = FlowDirection == FlowDirection.RightToLeft;

			var size = DesiredSize;
			var orientation = PhysicalOrientation;
			if (orientation == Orientation.Vertical)
			{
				double pixelDelta = -GetVerticalScrollWheelDelta(size, mouseWheelDelta);
				TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
				SetVerticalOffset(offset);
			}
			else
			{
				double pixelDelta = (invertForRTL ? GetHorizontalScrollWheelDelta(size, mouseWheelDelta) : -GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
				TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
				SetHorizontalOffset(offset);
			}
		}
	}

	public void MouseWheelRight(uint mouseWheelDelta)
	{
		var passToScrollOwner = ShouldPassWheelMessageToScrollOwner;

		if (passToScrollOwner)
		{
			PassWheelMessageToScrollOwner(ZoomDirection.Out);
		}
		else
		{
			double offset = 0.0;

			var invertForRTL = FlowDirection == FlowDirection.RightToLeft;

			var size = DesiredSize;
			var orientation = PhysicalOrientation;
			if (orientation == Orientation.Vertical)
			{
				double pixelDelta = GetVerticalScrollWheelDelta(size, mouseWheelDelta);
				TranslateVerticalPixelDeltaToOffset(m_ScrollData.OffsetY, ref pixelDelta, out offset);
				SetVerticalOffset(offset);
			}
			else
			{
				double pixelDelta = (invertForRTL ? -GetHorizontalScrollWheelDelta(size, mouseWheelDelta) : GetHorizontalScrollWheelDelta(size, mouseWheelDelta));
				TranslateHorizontalPixelDeltaToOffset(m_ScrollData.OffsetX, ref pixelDelta, out offset);
				SetHorizontalOffset(offset);
			}
		}
	}

	public void SetHorizontalOffset(double offset)
	{
		var isHorizontal = IsHorizontal();
		var extentWidth = ExtentWidth;
		var viewportWidth = ViewportWidth;

		var scrollX = ScrollContentPresenter.ValidateInputOffset(offset, m_ScrollData.m_MinOffset.X, isHorizontal ? double.MaxValue : extentWidth - viewportWidth);

		if (!DoubleUtil.AreClose(scrollX, m_ScrollData.OffsetX))
		{
			m_ScrollData.OffsetX = scrollX;

			var spScrollInfo = Panel as IScrollInfo;

			if (isHorizontal)
			{
				scrollX -= 2;
			}

			if (spScrollInfo is not null)
			{
				spScrollInfo.SetHorizontalOffset(scrollX);
			}

			var bIsInDMZoom = IsInDirectManipulationZoom();
			if (!bIsInDMZoom)
			{
				InvalidateMeasure();
			}
		}
	}

	public void SetVerticalOffset(double offset)
	{
		var isHorizontal = IsHorizontal();
		var extentHeight = ExtentHeight;
		var viewportHeight = ViewportHeight;

		var scrollY = ScrollContentPresenter.ValidateInputOffset(offset, m_ScrollData.m_MinOffset.Y, !isHorizontal ? double.MaxValue : extentHeight - viewportHeight);

		if (!DoubleUtil.AreClose(scrollY, m_ScrollData.OffsetY))
		{
			m_ScrollData.OffsetY = scrollY;

			var spScrollInfo = Panel as IScrollInfo;

			if (!isHorizontal)
			{
				scrollY -= 2;
			}

			if (spScrollInfo is not null)
			{
				spScrollInfo.SetVerticalOffset(scrollY);
			}

			var bIsInDMZoom = IsInDirectManipulationZoom();
			if (!bIsInDMZoom)
			{
				InvalidateMeasure();
			}
		}
	}

	public Rect MakeVisible(
		// The element that should become visible.
		UIElement visual,
		// A rectangle representing in the visual's coordinate space to
		// make visible.
		Rect rectangle,
		// When set to True, the DManip ZoomToRect method is invoked.
		bool useAnimation,
		double horizontalAlignmentRatio,
		double verticalAlignmentRatio,
		double offsetX,
		double offsetY,
		out double appliedOffsetX,
		out double appliedOffsetY)
	{
		Rect resultRectangle = default;
		appliedOffsetX = 0;
		appliedOffsetY = 0;

		if (Panel is IScrollInfo spScrollInfo)
		{
			resultRectangle = spScrollInfo.MakeVisible(
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

		return resultRectangle;
	}

	// End of IScrollInfo interface implementation

	private bool ShouldPassWheelMessageToScrollOwner
	{
		get
		{
			var modifiers = Control.GetKeyboardModifiers();

			return (modifiers & VirtualKeyModifiers.Control) != 0;
		}
	}

	private void PassWheelMessageToScrollOwner(ZoomDirection zoomDirection)
	{
		var spOwner = m_ScrollData.ScrollOwner;
		if (spOwner is not null)
		{
			spOwner.ProcessPureInertiaInputMessage(zoomDirection);
		}
	}
}

#endif
