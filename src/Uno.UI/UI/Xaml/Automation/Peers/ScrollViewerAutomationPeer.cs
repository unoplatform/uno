// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewerAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ScrollViewer types to UI Automation.
/// </summary>
public partial class ScrollViewerAutomationPeer : FrameworkElementAutomationPeer, IScrollProvider
{
	private const double minimumPercent = 0.0f;
	private const double maximumPercent = 100.0f;
	private const double noScroll = -1.0f;

	public ScrollViewerAutomationPeer(ScrollViewer owner) : base(owner)
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

	protected override string GetClassNameCore()
	{
		return "ScrollViewer";
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Pane;
	}

	//-------------------------------------------------------------------
	//  IScrollProvider
	//-------------------------------------------------------------------

	// Request to scroll horizontally and vertically by the specified amount.
	// The ability to call this method and simultaneously scroll horizontally
	// and vertically provides simple panning support.
	public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
	{
		ScrollViewer pOwner = null;
		bool bHorizontallyScrollable = false;
		bool bVerticallyScrollable = false;
		bool bIsEnabled;
		bool scrollHorizontally = horizontalAmount != ScrollAmount.NoAmount;
		bool scrollVertically = verticalAmount != ScrollAmount.NoAmount;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			throw new ElementNotEnabledException();
		}

		bHorizontallyScrollable = HorizontallyScrollable;
		bVerticallyScrollable = VerticallyScrollable;
		if (scrollHorizontally && !bHorizontallyScrollable || scrollVertically && !bVerticallyScrollable)
		{
			//IFC(static_cast<HRESULT>(UIA_E_INVALIDOPERATION));
			return;
		}

		pOwner = (ScrollViewer)Owner;

		switch (horizontalAmount)
		{
			case ScrollAmount.LargeDecrement:
				pOwner.PageLeft();
				break;
			case ScrollAmount.SmallDecrement:
				pOwner.LineLeft();
				break;
			case ScrollAmount.SmallIncrement:
				pOwner.LineRight();
				break;
			case ScrollAmount.LargeIncrement:
				pOwner.PageRight();
				break;
			case ScrollAmount.NoAmount:
				break;
			default:
				//IFC(static_cast<HRESULT>(UIA_E_INVALIDOPERATION));
				return;
		}

		switch (verticalAmount)
		{
			case ScrollAmount.LargeDecrement:
				pOwner.PageUp();
				break;
			case ScrollAmount.SmallDecrement:
				pOwner.LineUp();
				break;
			case ScrollAmount.SmallIncrement:
				pOwner.LineDown();
				break;
			case ScrollAmount.LargeIncrement:
				pOwner.PageDown();
				break;
			case ScrollAmount.NoAmount:
				break;
			default:
				//IFC(static_cast<HRESULT>(UIA_E_INVALIDOPERATION));
				return;
		}
	}

	// Request to set the current horizontal and Vertical scroll position by percent (0-100).
	// Passing in the value of "-1", represented by the constant "NoScroll", will indicate that scrolling
	// in that direction should be ignored.
	// The ability to call this method and simultaneously scroll horizontally and vertically provides simple panning support.
	public void SetScrollPercent(double horizontalPercent, double verticalPercent)
	{
		ScrollViewer pOwner = null;
		double extentWidth = 0.0;
		double extentHeight = 0.0;
		double viewportWidth = 0.0;
		double viewportHeight = 0.0;
		double minHorizontalOffset = 0.0;
		double minVerticalOffset = 0.0;
		double nettAvailable = 0.0;
		bool bHorizontallyScrollable = false;
		bool bVerticallyScrollable = false;
		bool bIsEnabled;
		bool scrollHorizontally = horizontalPercent != noScroll;
		bool scrollVertically = verticalPercent != noScroll;

		bIsEnabled = IsEnabled();
		if (!bIsEnabled)
		{
			throw new ElementNotEnabledException();
		}

		bHorizontallyScrollable = HorizontallyScrollable;
		bVerticallyScrollable = VerticallyScrollable;
		if (scrollHorizontally && !bHorizontallyScrollable || scrollVertically && !bVerticallyScrollable)
		{
			//IFC(static_cast<HRESULT>(UIA_E_INVALIDOPERATION));
			return;
		}

		if ((scrollHorizontally && (horizontalPercent < minimumPercent || horizontalPercent > maximumPercent))
			|| (scrollVertically && (verticalPercent < minimumPercent || verticalPercent > maximumPercent)))
		{
			throw new ArgumentException();
		}

		pOwner = (ScrollViewer)Owner;

		// Make sure to take minimum offset into account in either dimension to add to actual offset,
		// minimum offset is new concept introduced for headered controls.
		if (scrollHorizontally)
		{
			extentWidth = pOwner.ExtentWidth;
			viewportWidth = pOwner.ViewportWidth;
			minHorizontalOffset = pOwner.MinHorizontalOffset;
			nettAvailable = DoubleUtil.Max(0.0, (extentWidth - viewportWidth - minHorizontalOffset));
			pOwner.ScrollToHorizontalOffset((nettAvailable * (double)(horizontalPercent * 0.01)) + minHorizontalOffset);
		}

		if (scrollVertically)
		{
			extentHeight = pOwner.ExtentHeight;
			viewportHeight = pOwner.ViewportHeight;
			minVerticalOffset = pOwner.MinVerticalOffset;
			nettAvailable = DoubleUtil.Max(0.0, (extentHeight - viewportHeight - minVerticalOffset));
			pOwner.ScrollToVerticalOffset((nettAvailable * (double)(verticalPercent * 0.01)) + minVerticalOffset);
		}
	}

	// True if control can scroll horizontally
	public bool HorizontallyScrollable
	{
		get
		{
			double extentWidth = 0.0;
			double viewportWidth = 0.0;
			double minHorizontalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			bool pValue = false;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				extentWidth = pOwner.ExtentWidth;
				viewportWidth = pOwner.ViewportWidth;
				minHorizontalOffset = pOwner.MinHorizontalOffset;
				pValue = AutomationIsScrollable(extentWidth, viewportWidth, minHorizontalOffset);
			}

			return pValue;
		}
	}

	public double HorizontalScrollPercent
	{
		get
		{
			double extentWidth = 0.0;
			double viewportWidth = 0.0;
			double horizontalOffset = 0.0;
			double minHorizontalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			double pValue = noScroll;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				// Make sure to take minimum offset into account in horizontal dimension to subtract from actual offset,
				// minimum offset is new concept introduced for headered controls.
				extentWidth = pOwner.ExtentWidth;
				viewportWidth = pOwner.ViewportWidth;
				horizontalOffset = pOwner.HorizontalOffset;
				minHorizontalOffset = pOwner.MinHorizontalOffset;
				pValue = AutomationGetScrollPercent(extentWidth, viewportWidth, horizontalOffset, minHorizontalOffset);
			}

			return pValue;
		}
	}

	// Equal to the horizontal percentage of the entire control that is currently viewable.
	public double HorizontalViewSize
	{
		get
		{
			double extentWidth = 0.0;
			double viewportWidth = 0.0;
			double minHorizontalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			double pValue = 100.0;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				extentWidth = pOwner.ExtentWidth;
				viewportWidth = pOwner.ViewportWidth;
				minHorizontalOffset = pOwner.MinHorizontalOffset;
				pValue = AutomationGetViewSize(extentWidth, viewportWidth, minHorizontalOffset);
			}

			return pValue;
		}
	}

	public bool VerticallyScrollable
	{
		get
		{
			double extentHeight = 0.0;
			double viewportHeight = 0.0;
			double minVerticalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			bool pValue = false;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				extentHeight = pOwner.ExtentHeight;
				viewportHeight = pOwner.ViewportHeight;
				minVerticalOffset = pOwner.MinVerticalOffset;
				pValue = AutomationIsScrollable(extentHeight, viewportHeight, minVerticalOffset);
			}

			return pValue;
		}
	}

	public double VerticalScrollPercent
	{
		get
		{
			double extentHeight = 0.0;
			double viewportHeight = 0.0;
			double verticalOffset = 0.0;
			double minVerticalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			double pValue = noScroll;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				// Make sure to take minimum offset into account in veritcal dimension to subtract from actual offset,
				// minimum offset is new concept introduced for headered controls.
				extentHeight = pOwner.ExtentHeight;
				viewportHeight = pOwner.ViewportHeight;
				verticalOffset = pOwner.VerticalOffset;
				minVerticalOffset = pOwner.MinVerticalOffset;

				pValue = AutomationGetScrollPercent(extentHeight, viewportHeight, verticalOffset, minVerticalOffset);
			}

			return pValue;
		}
	}

	public double VerticalViewSize
	{
		get
		{
			double extentHeight = 0.0;
			double viewportHeight = 0.0;
			double minVerticalOffset = 0.0;
			ScrollViewer pOwner = null;
			//IScrollInfo pScrollInfo = null;

			double pValue = 100.0;
			pOwner = (ScrollViewer)Owner;

			//pScrollInfo = ((ScrollViewer)pOwner).ScrollInfo;
			//if (pScrollInfo is not null)
			{
				extentHeight = pOwner.ExtentHeight;
				viewportHeight = pOwner.ViewportHeight;
				minVerticalOffset = pOwner.MinVerticalOffset;
				pValue = AutomationGetViewSize(extentHeight, viewportHeight, minVerticalOffset);
			}

			return pValue;
		}
	}

	// Raise Relevant Scroll Pattern related events to UIAutomation Clients for various changes related to scrolling.
	internal void RaiseAutomationEvents(double extentX,
		double extentY,
		double viewportX,
		double viewportY,
		double minOffsetX,
		double minOffsetY,
		double offsetX,
		double offsetY)
	{

		bool oldScrollable = false;
		bool newScrollable = false;
		double oldViewSize = 0.0;
		double newViewSize = 0.0;
		double oldScrollPercent = 0.0;
		double newScrollPercent = 0.0;
		//CValue valueOld;
		//CValue valueNew;

		//CAutomationPeer* pHandle = static_cast<CAutomationPeer*>(GetHandle());

		oldScrollable = AutomationIsScrollable(extentX, viewportX, minOffsetX);
		newScrollable = HorizontallyScrollable;
		if (oldScrollable != newScrollable)
		{
			//CValueBoxer::BoxValue(&valueOld, oldScrollable);
			//CValueBoxer::BoxValue(&valueNew, newScrollable);
			//CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APHorizontallyScrollableProperty, valueOld, valueNew);
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontallyScrollableProperty, oldScrollable, newScrollable);
		}

		oldScrollable = AutomationIsScrollable(extentY, viewportY, minOffsetY);
		newScrollable = VerticallyScrollable;
		if (oldScrollable != newScrollable)
		{
			//IFC(CValueBoxer::BoxValue(&valueOld, oldScrollable));
			//IFC(CValueBoxer::BoxValue(&valueNew, newScrollable));
			//IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APVerticallyScrollableProperty, valueOld, valueNew));
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.VerticallyScrollableProperty, oldScrollable, newScrollable);
		}

		oldViewSize = AutomationGetViewSize(extentX, viewportX, minOffsetX);
		newViewSize = HorizontalViewSize;
		if (oldViewSize != newViewSize)
		{
			//IFC(CValueBoxer::BoxValue(&valueOld, oldViewSize));
			//IFC(CValueBoxer::BoxValue(&valueNew, newViewSize));
			//IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APHorizontalViewSizeProperty, valueOld, valueNew));
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontalViewSizeProperty, oldViewSize, newViewSize);
		}

		oldViewSize = AutomationGetViewSize(extentY, viewportY, minOffsetY);
		newViewSize = VerticalViewSize;
		if (oldViewSize != newViewSize)
		{
			//IFC(CValueBoxer::BoxValue(&valueOld, oldViewSize));
			//IFC(CValueBoxer::BoxValue(&valueNew, newViewSize));
			//IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APVerticalViewSizeProperty, valueOld, valueNew));
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.VerticalViewSizeProperty, oldViewSize, newViewSize);
		}

		oldScrollPercent = AutomationGetScrollPercent(extentX, viewportX, offsetX, minOffsetX);
		newScrollPercent = HorizontalScrollPercent;
		if (oldScrollPercent != newScrollPercent)
		{
			//IFC(CValueBoxer::BoxValue(&valueOld, oldScrollPercent));
			//IFC(CValueBoxer::BoxValue(&valueNew, newScrollPercent));
			//IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APHorizontalScrollPercentProperty, valueOld, valueNew));
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.HorizontalScrollPercentProperty, oldScrollPercent, newScrollPercent);
		}

		oldScrollPercent = AutomationGetScrollPercent(extentY, viewportY, offsetY, minOffsetY);
		newScrollPercent = VerticalScrollPercent;
		if (oldScrollPercent != newScrollPercent)
		{
			//IFC(CValueBoxer::BoxValue(&valueOld, oldScrollPercent));
			//IFC(CValueBoxer::BoxValue(&valueNew, newScrollPercent));
			//IFC(CoreImports::AutomationRaiseAutomationPropertyChanged(pHandle, UIAXcp::APAutomationProperties::APVerticalScrollPercentProperty, valueOld, valueNew));
			this.RaisePropertyChangedEvent(ScrollPatternIdentifiers.VerticalScrollPercentProperty, oldScrollPercent, newScrollPercent);
		}
	}

	private static bool AutomationIsScrollable(double extent, double viewport, double minOffset)
	{
		return DoubleUtil.GreaterThan(extent, viewport + minOffset);
	}

	private double AutomationGetScrollPercent(double extent, double viewport, double actualOffset, double minOffset)
	{
		double retVal = noScroll;
		//ScrollViewer pOwner = null;

		//pOwner = (ScrollViewer)Owner;

		if (!AutomationIsScrollable(extent, viewport, minOffset))
		{
			return retVal;
		}

		actualOffset = DoubleUtil.Max(0.0, (actualOffset - minOffset));
		retVal = (actualOffset * 100.0 / (extent - viewport - minOffset));
		return retVal;
	}

	private static double AutomationGetViewSize(double extent, double viewport, double minOffset)
	{
		double nettExtent = 0.0;

		nettExtent = DoubleUtil.Max(0.0, (extent - minOffset));
		if (DoubleUtil.IsZero(nettExtent)) { return 100.0; }
		return DoubleUtil.Min(100.0, (viewport * 100.0 / nettExtent));
	}

	// Override for the ScrollViewerAutomationPeer doesn't allow creating
	// elements from collapsed vertical or horizontal template.
	private protected override bool ChildIsAcceptable(UIElement element)
	{
		ScrollViewer pOwner = null;
		UIElement pElementHorizontalScrollBar = null;
		UIElement pElementVerticalScrollBar = null;
		Visibility visibility = Visibility.Collapsed;

		bool bchildIsAcceptable = base.ChildIsAcceptable(element);
		if (bchildIsAcceptable)
		{
			pOwner = (ScrollViewer)Owner;

			pElementHorizontalScrollBar = pOwner.ElementHorizontalScrollBar;
			pElementVerticalScrollBar = pOwner.ElementVerticalScrollBar;

			if (element == pElementHorizontalScrollBar || element == pElementVerticalScrollBar)
			{
				visibility = element.Visibility;
				bchildIsAcceptable = visibility == Visibility.Visible;
			}
		}

		return bchildIsAcceptable;
	}
}
