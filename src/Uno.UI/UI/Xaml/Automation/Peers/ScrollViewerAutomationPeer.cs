// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
using System;
using DirectUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class ScrollViewerAutomationPeer : FrameworkElementAutomationPeer, Provider.IScrollProvider
{
	private const double MinimumPercent = 0.0f;
	private const double MaximumPercent = 100.0f;
	private const double NoScroll = -1.0f;

	public ScrollViewerAutomationPeer(Controls.ScrollViewer owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Scroll)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.ScrollViewer);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Pane;


	//-------------------------------------------------------------------
	//  IScrollProvider
	//-------------------------------------------------------------------

	// Request to scroll horizontally and vertically by the specified amount.
	// The ability to call this method and simultaneously scroll horizontally
	// and vertically provides simple panning support.
	public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		var scrollHorizontally = horizontalAmount != ScrollAmount.NoAmount;
		var scrollVertically = verticalAmount != ScrollAmount.NoAmount;

		if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
		{
			throw new InvalidOperationException();
		}

		var owner = Owner as Controls.ScrollViewer;

		switch (horizontalAmount)
		{
			case ScrollAmount.LargeDecrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->PageLeft());
				break;
			case ScrollAmount.SmallDecrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->LineLeft());
				break;
			case ScrollAmount.SmallIncrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->LineRight());
				break;
			case ScrollAmount.LargeIncrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->PageRight());
				break;
			case ScrollAmount.NoAmount:
				break;
			default:
				throw new InvalidOperationException();
		}

		switch (verticalAmount)
		{
			case ScrollAmount.LargeDecrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->PageUp());
				break;
			case ScrollAmount.SmallDecrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->LineUp());
				break;
			case ScrollAmount.SmallIncrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->LineDown());
				break;
			case ScrollAmount.LargeIncrement:
				//		IFC((static_cast<ScrollViewer*>(pOwner))->PageDown());
				break;
			case ScrollAmount.NoAmount:
				break;
			default:
				throw new InvalidOperationException();
		}
	}

	// Request to set the current horizontal and Vertical scroll position by percent (0-100).
	// Passing in the value of "-1", represented by the constant "NoScroll", will indicate that scrolling
	// in that direction should be ignored.
	// The ability to call this method and simultaneously scroll horizontally and vertically provides simple panning support.
	public void SetScrollPercent(double horizontalPercent, double verticalPercent)
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		var scrollHorizontally = horizontalPercent != NoScroll;
		var scrollVertically = verticalPercent != NoScroll;

		if (scrollHorizontally && !HorizontallyScrollable || scrollVertically && !VerticallyScrollable)
		{
			throw new InvalidOperationException();
		}

		if ((scrollHorizontally && (horizontalPercent < MinimumPercent || horizontalPercent > MaximumPercent))
			|| (scrollVertically && (verticalPercent < MinimumPercent || verticalPercent > MaximumPercent)))
		{
			throw new ArgumentException();
		}

		var owner = Owner as Controls.ScrollViewer;

		// Make sure to take minimum offset into account in either dimension to add to actual offset,
		// minimum offset is new concept introduced for headered controls.
		if (scrollHorizontally)
		{
			var netAvailable = Math.Max(0.0, (owner.ExtentWidth - owner.ViewportWidth - owner.MinHorizontalOffset));
			owner.ScrollToHorizontalOffset((netAvailable * (horizontalPercent * 0.01)) + owner.MinHorizontalOffset);
		}

		if (scrollVertically)
		{
			var netAvailable = Math.Max(0.0, (owner.ExtentHeight - owner.ViewportHeight - owner.MinVerticalOffset));
			owner.ScrollToVerticalOffset((netAvailable * (verticalPercent * 0.01)) + owner.MinVerticalOffset);
		}
	}

	// True if control can scroll horizontally
	public bool HorizontallyScrollable
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentWidth = 0.0;
			//DOUBLE viewportWidth = 0.0;
			//DOUBLE minHorizontalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = FALSE;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));
			//if (pScrollInfo)
			//{
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentWidth(&extentWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportWidth(&viewportWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinHorizontalOffset(&minHorizontalOffset));
			//	*pValue = AutomationIsScrollable(extentWidth, viewportWidth, minHorizontalOffset);
			//}
		}
	}

	// Get the current horizontal scroll position
	public double HorizontalScrollPercent
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentWidth = 0.0;
			//DOUBLE viewportWidth = 0.0;
			//DOUBLE horizontalOffset = 0.0;
			//DOUBLE minHorizontalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = noScroll;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));
			//if (pScrollInfo)
			//{
			//	// Make sure to take minimum offset into account in horizontal dimension to subtract from actual offset,
			//	// minimum offset is new concept introduced for headered controls.
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentWidth(&extentWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportWidth(&viewportWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_HorizontalOffset(&horizontalOffset));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinHorizontalOffset(&minHorizontalOffset));
			//	*pValue = AutomationGetScrollPercent(extentWidth, viewportWidth, horizontalOffset, minHorizontalOffset);
			//}
		}
	}

	// Equal to the horizontal percentage of the entire control that is currently viewable.
	public double HorizontalViewSize
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentWidth = 0.0;
			//DOUBLE viewportWidth = 0.0;
			//DOUBLE minHorizontalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = 100.0;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));
			//if (pScrollInfo)
			//{
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentWidth(&extentWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportWidth(&viewportWidth));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinHorizontalOffset(&minHorizontalOffset));
			//	*pValue = AutomationGetViewSize(extentWidth, viewportWidth, minHorizontalOffset);
			//}
		}
	}

	// True if control can scroll vertically
	public bool VerticallyScrollable
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentHeight = 0.0;
			//DOUBLE viewportHeight = 0.0;
			//DOUBLE minVerticalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = FALSE;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));
			//if (pScrollInfo)
			//{
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentHeight(&extentHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportHeight(&viewportHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinVerticalOffset(&minVerticalOffset));
			//	*pValue = AutomationIsScrollable(extentHeight, viewportHeight, minVerticalOffset);
			//}
		}
	}

	public double VerticalScrollPercent
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentHeight = 0.0;
			//DOUBLE viewportHeight = 0.0;
			//DOUBLE verticalOffset = 0.0;
			//DOUBLE minVerticalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = noScroll;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);
			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));

			//if (pScrollInfo)
			//{
			//	// Make sure to take minimum offset into account in veritcal dimension to subtract from actual offset,
			//	// minimum offset is new concept introduced for headered controls.
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentHeight(&extentHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportHeight(&viewportHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_VerticalOffset(&verticalOffset));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinVerticalOffset(&minVerticalOffset));

			//	*pValue = AutomationGetScrollPercent(extentHeight, viewportHeight, verticalOffset, minVerticalOffset);
			//}
		}
	}

	// Equal to the vertical percentage of the entire control that is currently viewable.
	public double VerticalViewSize
	{
		get
		{
			//HRESULT hr = S_OK;
			//DOUBLE extentHeight = 0.0;
			//DOUBLE viewportHeight = 0.0;
			//DOUBLE minVerticalOffset = 0.0;
			//xaml::IUIElement* pOwner = NULL;
			//IScrollInfo* pScrollInfo = NULL;

			//IFCPTR(pValue);
			//*pValue = 100.0;
			//IFC(get_Owner(&pOwner));
			//IFCPTR(pOwner);

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ScrollInfo(&pScrollInfo));
			//if (pScrollInfo)
			//{
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ExtentHeight(&extentHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_ViewportHeight(&viewportHeight));
			//	IFC((static_cast<ScrollViewer*>(pOwner))->get_MinVerticalOffset(&minVerticalOffset));
			//	*pValue = AutomationGetViewSize(extentHeight, viewportHeight, minVerticalOffset);
			//}
		}
	}

	private bool AutomationIsScrollable(double extent, double viewport, double minOffset)
		=> DoubleUtil.GreaterThan(extent, viewport + minOffset);

	private double AutomationGetScrollPercent(double extent, double viewport, double actualOffset, double minOffset)
	{
		if (!AutomationIsScrollable(extent, viewport, minOffset))
		{
			return NoScroll;
		}

		actualOffset = Math.Max(0.0, actualOffset - minOffset);
		return (actualOffset * 100.0 / (extent - viewport - minOffset));
	}

	private double AutomationGetViewSize(double extent, double viewport, double minOffset)
	{
		if (!AutomationIsScrollable(extent, viewport, minOffset))
		{
			return MaximumPercent;
		}

		return (viewport * 100.0 / (extent - minOffset));
	}

	// Override for the ScrollViewerAutomationPeer doesn't allow creating
	// elements from collapsed vertical or horizontal template.
	private protected override bool ChildIsAcceptable(UIElement element)
	{
		var childIsAcceptable = base.ChildIsAcceptable(element);

		if (childIsAcceptable)
		{
			var owner = Owner as Controls.ScrollViewer;

			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ElementHorizontalScrollBar(&pElementHorizontalScrollBar));
			//IFC((static_cast<ScrollViewer*>(pOwner))->get_ElementVerticalScrollBar(&pElementVerticalScrollBar));

			//if (pElement == pElementHorizontalScrollBar || pElement == pElementVerticalScrollBar)
			//{
			//	IFC((static_cast<UIElement*>(pElement))->get_Visibility(&visibility));
			//	*bchildIsAcceptable = visibility == xaml::Visibility_Visible;
			//}
		}
	}
}
