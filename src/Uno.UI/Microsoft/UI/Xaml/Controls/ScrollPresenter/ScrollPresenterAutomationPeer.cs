// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls.Primitives;

using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Automation.Peers;

internal partial class ScrollPresenterAutomationPeer : FrameworkElementAutomationPeer
{
	private const double s_minimumPercent = 0.0;
	private const double s_maximumPercent = 100.0;
	//private const double s_noScroll = -1.0;

	private double m_horizontalScrollPercent = ScrollPatternIdentifiers.NoScroll;
	private double m_verticalScrollPercent = ScrollPatternIdentifiers.NoScroll;
	private double m_horizontalViewSize = s_maximumPercent;
	private double m_verticalViewSize = s_maximumPercent;
	private bool m_horizontallyScrollable = false;
	private bool m_verticallyScrollable = false;

	public ScrollPresenterAutomationPeer(ScrollPresenter owner)
		: base(owner)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(owner, TRACE_MSG_METH_PTR, METH_NAME, this, owner);
	}

	// IAutomationPeerOverrides implementation

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		return AutomationControlType.Pane;
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		return GetPatternCoreImpl(patternInterface);
	}

	// IScrollProvider implementation

	// Request to scroll horizontally and vertically by the specified amount.
	// The ability to call this method and simultaneously scroll horizontally
	// and vertically provides simple panning support.
#if false // UNO TODO
	private void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(Owner(), TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::ScrollAmountToString(horizontalAmount).c_str(), TypeLogging::ScrollAmountToString(verticalAmount).c_str());

		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		bool scrollHorizontally = horizontalAmount != ScrollAmount.NoAmount;
		bool scrollVertically = verticalAmount != ScrollAmount.NoAmount;

		bool isHorizontallyScrollable = HorizontallyScrollable();
		bool isVerticallyScrollable = VerticallyScrollable();

		if (!(scrollHorizontally && !isHorizontallyScrollable) && !(scrollVertically && !isVerticallyScrollable))
		{
			var scrollPresenter = GetScrollPresenter();
			bool isInvalidOperation = false;

			switch (horizontalAmount)
			{
				case ScrollAmount.LargeDecrement:
					scrollPresenter.PageLeft();
					break;
				case ScrollAmount.LargeIncrement:
					scrollPresenter.PageRight();
					break;
				case ScrollAmount.SmallDecrement:
					scrollPresenter.LineLeft();
					break;
				case ScrollAmount.SmallIncrement:
					scrollPresenter.LineRight();
					break;
				case ScrollAmount.NoAmount:
					break;
				default:
					isInvalidOperation = true;
					break;
			}

			if (!isInvalidOperation)
			{
				switch (verticalAmount)
				{
					case ScrollAmount.LargeDecrement:
						scrollPresenter.PageUp();
						return;
					case ScrollAmount.SmallDecrement:
						scrollPresenter.LineUp();
						return;
					case ScrollAmount.SmallIncrement:
						scrollPresenter.LineDown();
						return;
					case ScrollAmount.LargeIncrement:
						scrollPresenter.PageDown();
						return;
					case ScrollAmount.NoAmount:
						return;
				}
			}
		}

		throw new InvalidOperationException("Cannot perform the operation.");
	}

	// Request to set the current horizontal and Vertical scroll position by percent (0-100).
	// Passing in the value of "-1", represented by the constant "NoScroll", will indicate that scrolling
	// in that direction should be ignored.
	// The ability to call this method and simultaneously scroll horizontally and vertically provides simple panning support.
	private void SetScrollPercent(double horizontalPercent, double verticalPercent)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(Owner(), TRACE_MSG_METH_DBL_DBL, METH_NAME, this, horizontalPercent, verticalPercent);

		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		bool scrollHorizontally = horizontalPercent != s_noScroll;
		bool scrollVertically = verticalPercent != s_noScroll;

		if (!scrollHorizontally && !scrollVertically)
		{
			return;
		}

		bool isHorizontallyScrollable = HorizontallyScrollable();
		bool isVerticallyScrollable = VerticallyScrollable();

		if ((scrollHorizontally && !isHorizontallyScrollable) || (scrollVertically && !isVerticallyScrollable))
		{
			throw new InvalidOperationException("Cannot perform the operation.");
		}

		if ((scrollHorizontally && (horizontalPercent < s_minimumPercent || horizontalPercent > s_maximumPercent)) ||
			(scrollVertically && (verticalPercent < s_minimumPercent || verticalPercent > s_maximumPercent)))
		{
			throw new ArgumentException();
		}

		var scrollPresenter = GetScrollPresenter();

		if (scrollHorizontally && !scrollVertically)
		{
			double maxOffset = scrollPresenter.ScrollableWidth;

			scrollPresenter.ScrollToHorizontalOffset(maxOffset * horizontalPercent / s_maximumPercent);
		}
		else if (scrollVertically && !scrollHorizontally)
		{
			double maxOffset = scrollPresenter.ScrollableHeight;

			scrollPresenter.ScrollToVerticalOffset(maxOffset * verticalPercent / s_maximumPercent);
		}
		else
		{
			double maxHorizontalOffset = scrollPresenter.ScrollableWidth;
			double maxVerticalOffset = scrollPresenter.ScrollableHeight;

			scrollPresenter.ScrollToOffsets(
				maxHorizontalOffset * horizontalPercent / s_maximumPercent, maxVerticalOffset * verticalPercent / s_maximumPercent);
		}
	}

	private double HorizontalScrollPercent()
	{
		MUX_ASSERT(m_horizontalScrollPercent == get_HorizontalScrollPercentImpl());

		return m_horizontalScrollPercent;
	}

	private double VerticalScrollPercent()
	{
		MUX_ASSERT(m_verticalScrollPercent == get_VerticalScrollPercentImpl());

		return m_verticalScrollPercent;
	}

	// Returns the horizontal percentage of the entire extent that is currently viewed.
	private double HorizontalViewSize()
	{
		MUX_ASSERT(m_horizontalViewSize == get_HorizontalViewSizeImpl());

		return m_horizontalViewSize;
	}

	// Returns the vertical percentage of the entire extent that is currently viewed.
	private double VerticalViewSize()
	{
		MUX_ASSERT(m_verticalViewSize == get_VerticalViewSizeImpl());

		return m_verticalViewSize;
	}

	private bool HorizontallyScrollable()
	{
		MUX_ASSERT(m_horizontallyScrollable == get_HorizontallyScrollableImpl());

		return m_horizontallyScrollable;
	}

	private bool VerticallyScrollable()
	{
		MUX_ASSERT(m_verticallyScrollable == get_VerticallyScrollableImpl());

		return m_verticallyScrollable;
	}
#endif

	// Raise relevant Scroll Pattern events for UIAutomation clients.
	internal void UpdateScrollPatternProperties()
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(Owner(), TRACE_MSG_METH, METH_NAME, this);

		double newHorizontalScrollPercent = get_HorizontalScrollPercentImpl();
		double newVerticalScrollPercent = get_VerticalScrollPercentImpl();
		double newHorizontalViewSize = get_HorizontalViewSizeImpl();
		double newVerticalViewSize = get_VerticalViewSizeImpl();
		bool newHorizontallyScrollable = get_HorizontallyScrollableImpl();
		bool newVerticallyScrollable = get_VerticallyScrollableImpl();

		if (newHorizontallyScrollable != m_horizontallyScrollable)
		{
			bool oldHorizontallyScrollable = m_horizontallyScrollable;
			m_horizontallyScrollable = newHorizontallyScrollable;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.HorizontallyScrollableProperty,
				oldHorizontallyScrollable,
				newHorizontallyScrollable);
		}

		if (newVerticallyScrollable != m_verticallyScrollable)
		{
			bool oldVerticallyScrollable = m_verticallyScrollable;
			m_verticallyScrollable = newVerticallyScrollable;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.HorizontallyScrollableProperty,
				oldVerticallyScrollable,
				newVerticallyScrollable);
		}

		if (newHorizontalViewSize != m_horizontalViewSize)
		{
			double oldHorizontalViewSize = m_horizontalViewSize;
			m_horizontalViewSize = newHorizontalViewSize;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.HorizontalViewSizeProperty,
				oldHorizontalViewSize,
				newHorizontalViewSize);
		}

		if (newVerticalViewSize != m_verticalViewSize)
		{
			double oldVerticalViewSize = m_verticalViewSize;
			m_verticalViewSize = newVerticalViewSize;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.VerticalViewSizeProperty,
				oldVerticalViewSize,
				newVerticalViewSize);
		}

		if (newHorizontalScrollPercent != m_horizontalScrollPercent)
		{
			double oldHorizontalScrollPercent = m_horizontalScrollPercent;
			m_horizontalScrollPercent = newHorizontalScrollPercent;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.HorizontalScrollPercentProperty,
				oldHorizontalScrollPercent,
				newHorizontalScrollPercent);
		}

		if (newVerticalScrollPercent != m_verticalScrollPercent)
		{
			double oldVerticalScrollPercent = m_verticalScrollPercent;
			m_verticalScrollPercent = newVerticalScrollPercent;
			RaisePropertyChangedEvent(
				ScrollPatternIdentifiers.VerticalScrollPercentProperty,
				oldVerticalScrollPercent,
				newVerticalScrollPercent);
		}
	}

	private double get_HorizontalScrollPercentImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return GetScrollPercent(
			scrollPresenter.GetZoomedExtentWidth(),
			scrollPresenter.ViewportWidth,
			GetScrollPresenter().HorizontalOffset);
	}

	private double get_VerticalScrollPercentImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return GetScrollPercent(
			scrollPresenter.GetZoomedExtentHeight(),
			scrollPresenter.ViewportHeight,
			GetScrollPresenter().VerticalOffset);
	}

	// Returns the horizontal percentage of the entire extent that is currently viewed.
	private double get_HorizontalViewSizeImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return GetViewPercent(
			scrollPresenter.GetZoomedExtentWidth(),
			scrollPresenter.ViewportWidth);
	}

	// Returns the vertical percentage of the entire extent that is currently viewed.
	private double get_VerticalViewSizeImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return GetViewPercent(
			scrollPresenter.GetZoomedExtentHeight(),
			scrollPresenter.ViewportHeight);
	}

	private bool get_HorizontallyScrollableImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return scrollPresenter.ScrollableWidth > 0.0;
	}

	private bool get_VerticallyScrollableImpl()
	{
		ScrollPresenter scrollPresenter = GetScrollPresenter();

		return scrollPresenter.ScrollableHeight > 0.0;
	}

	private object GetPatternCoreImpl(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Scroll)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	private ScrollPresenter GetScrollPresenter()
	{
		UIElement owner = Owner;
		return owner as ScrollPresenter;
	}

	private double GetViewPercent(double zoomedExtent, double viewport)
	{
		MUX_ASSERT(zoomedExtent >= 0.0);
		MUX_ASSERT(viewport >= 0.0);

		if (zoomedExtent == 0.0)
		{
			return s_maximumPercent;
		}

		return Math.Min(s_maximumPercent, (viewport / zoomedExtent * s_maximumPercent));
	}

	private double GetScrollPercent(double zoomedExtent, double viewport, double offset)
	{
		MUX_ASSERT(zoomedExtent >= 0.0);
		MUX_ASSERT(viewport >= 0.0);

		if (viewport >= zoomedExtent)
		{
			return ScrollPatternIdentifiers.NoScroll;
		}

		double scrollPercent = offset / (zoomedExtent - viewport) * s_maximumPercent;

		scrollPercent = Math.Max(scrollPercent, s_minimumPercent);
		scrollPercent = Math.Min(scrollPercent, s_maximumPercent);

		return scrollPercent;
	}

	// ~ScrollPresenterAutomationPeer()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
