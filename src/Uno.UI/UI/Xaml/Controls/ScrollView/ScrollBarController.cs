// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollBarController.cpp, tag winui3/release/1.4.2

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

internal sealed partial class ScrollBarController : IScrollController
{
	public ScrollBarController()
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);
	}

	~ScrollBarController()
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		UnhookScrollBarEvent();
		UnhookScrollBarPropertyChanged();
	}

	public void SetScrollBar(ScrollBar scrollBar)
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH, METH_NAME, this);

		UnhookScrollBarEvent();

		m_scrollBar = scrollBar;

		HookScrollBarEvent();
		HookScrollBarPropertyChanged();
	}

	#region IScrollController

	public IScrollControllerPanningInfo PanningInfo => null;

	public bool CanScroll => m_canScroll;

	public bool IsScrollingWithMouse => m_isScrollingWithMouse;

	public void SetIsScrollable(
		bool isScrollable)
	{
		//SCROLLVIEW_TRACE_INFO(
		//	null,
		//	TRACE_MSG_METH_INT,
		//	METH_NAME,
		//	this,
		//	isScrollable);
		m_isScrollable = isScrollable;

		UpdateCanScroll();
	}

	public void SetValues(
		double minOffset,
		double maxOffset,
		double offset,
		double viewportLength)
	{
		//SCROLLVIEW_TRACE_INFO(
		//	null,
		//	L"%s[0x%p](minOffset:%lf, maxOffset:%lf, offset:%lf, viewportLength:%lf, operationsCount:%d)\n",
		//	METH_NAME,
		//	this,
		//	minOffset,
		//	maxOffset,
		//	offset,
		//	viewportLength,
		//	m_operationsCount);

		if (maxOffset < minOffset)
		{
			throw new ArgumentOutOfRangeException("maxOffset cannot be smaller than minOffset.");
		}

		if (viewportLength < 0.0)
		{
			throw new ArgumentOutOfRangeException("viewportLength cannot be negative.");
		}

		offset = Math.Max(minOffset, offset);
		offset = Math.Min(maxOffset, offset);
		m_lastOffset = offset;

		MUX_ASSERT(m_scrollBar is not null);

		if (minOffset < m_scrollBar.Minimum)
		{
			m_scrollBar.Minimum = minOffset;
		}

		if (maxOffset > m_scrollBar.Maximum)
		{
			m_scrollBar.Maximum = maxOffset;
		}

		if (minOffset != m_scrollBar.Minimum)
		{
			m_scrollBar.Minimum = minOffset;
		}

		if (maxOffset != m_scrollBar.Maximum)
		{
			m_scrollBar.Maximum = maxOffset;
		}

		m_scrollBar.ViewportSize = viewportLength;
		m_scrollBar.LargeChange = viewportLength;
		m_scrollBar.SmallChange = Math.Max(1.0, viewportLength / s_defaultViewportToSmallChangeRatio);

		// The ScrollBar Value is only updated when there is no operation in progress otherwise the Scroll
		// event handler, ScrollBarScroll, may initiate a new request impeding the on-going operation.
		if (m_operationsCount == 0 || m_scrollBar.Value < minOffset || m_scrollBar.Value > maxOffset)
		{
			m_scrollBar.Value = offset;
			m_lastScrollBarValue = offset;
		}

		// Potentially changed ScrollBar.Minimum / ScrollBar.Maximum value(s) may have an effect
		// on the read-only IScrollController.CanScroll property.
		UpdateCanScroll();
	}

	public CompositionAnimation GetScrollAnimation(
		int correlationId,
		Vector2 startPosition,
		Vector2 endPosition,
		CompositionAnimation defaultAnimation)
	{
		//SCROLLVIEW_TRACE_INFO(null, TRACE_MSG_METH_INT, METH_NAME, this, correlationId);

		// Using the consumer's default animation.
		return null;
	}

	public void NotifyRequestedScrollCompleted(
		int correlationId)
	{
		//SCROLLVIEW_TRACE_INFO(
		//	null,
		//	TRACE_MSG_METH_INT,
		//	METH_NAME,
		//	this,
		//	correlationId);

		MUX_ASSERT(m_operationsCount > 0);
		m_operationsCount--;

		if (m_operationsCount == 0 && m_scrollBar is not null && m_scrollBar.Value != m_lastOffset)
		{
			m_scrollBar.Value = m_lastOffset;
			m_lastScrollBarValue = m_lastOffset;
		}
	}

	public event TypedEventHandler<IScrollController, ScrollControllerScrollToRequestedEventArgs> ScrollToRequested;
	public event TypedEventHandler<IScrollController, ScrollControllerScrollByRequestedEventArgs> ScrollByRequested;
	public event TypedEventHandler<IScrollController, ScrollControllerAddScrollVelocityRequestedEventArgs> AddScrollVelocityRequested;
	public event TypedEventHandler<IScrollController, object> CanScrollChanged;
	public event TypedEventHandler<IScrollController, object> IsScrollingWithMouseChanged;
	#endregion

	private void HookScrollBarPropertyChanged()
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

#if DEBUG
		MUX_ASSERT(m_scrollBarIndicatorModeChangedToken == 0);
		MUX_ASSERT(m_scrollBarVisibilityChangedToken == 0);
#endif //DBG
		MUX_ASSERT(m_scrollBarIsEnabledChangedToken == 0);

		if (m_scrollBar is not null)
		{
#if DEBUG
			m_scrollBarIndicatorModeChangedToken = m_scrollBar.RegisterPropertyChangedCallback(
				ScrollBar.IndicatorModeProperty, OnScrollBarPropertyChanged);

			m_scrollBarVisibilityChangedToken = m_scrollBar.RegisterPropertyChangedCallback(
				UIElement.VisibilityProperty, OnScrollBarPropertyChanged);
#endif //DBG

			m_scrollBarIsEnabledChangedToken = m_scrollBar.RegisterPropertyChangedCallback(
				Control.IsEnabledProperty, OnScrollBarPropertyChanged);
		}
	}

	private void UnhookScrollBarPropertyChanged()
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollBar is not null)
		{
#if DEBUG
			if (m_scrollBarIndicatorModeChangedToken != 0)
			{
				m_scrollBar.UnregisterPropertyChangedCallback(ScrollBar.IndicatorModeProperty, m_scrollBarIndicatorModeChangedToken);
				m_scrollBarIndicatorModeChangedToken = 0;
			}

			if (m_scrollBarVisibilityChangedToken != 0)
			{
				m_scrollBar.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, m_scrollBarVisibilityChangedToken);
				m_scrollBarVisibilityChangedToken = 0;
			}
#endif //DBG

			if (m_scrollBarIsEnabledChangedToken != 0)
			{
				m_scrollBar.UnregisterPropertyChangedCallback(Control.IsEnabledProperty, m_scrollBarIsEnabledChangedToken);
				m_scrollBarIsEnabledChangedToken = 0;
			}
		}
	}

	private void UpdateCanScroll()
	{
		bool oldCanScroll = m_canScroll;

		m_canScroll =
			m_scrollBar is not null &&
			m_scrollBar.Parent is not null &&
			m_scrollBar.IsEnabled &&
			m_scrollBar.Maximum > m_scrollBar.Minimum &&
			m_isScrollable;

		if (oldCanScroll != m_canScroll)
		{
			RaiseCanScrollChanged();
		}
	}

	private void HookScrollBarEvent()
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		MUX_ASSERT(m_scrollBarScrollToken is null);

		if (m_scrollBar is not null)
		{
			m_scrollBar.Scroll += OnScroll;
			m_scrollBarScrollToken = new();
			m_scrollBarScrollToken.Disposable = Disposable.Create(() => m_scrollBar.Scroll -= OnScroll);
		}
	}

	private void UnhookScrollBarEvent()
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		if (m_scrollBar is not null && m_scrollBarScrollToken is not null)
		{
			m_scrollBarScrollToken.Disposable = null;
			m_scrollBarScrollToken = null;
		}
	}

	private void OnScrollBarPropertyChanged(
		DependencyObject sender,
		DependencyProperty args)
	{
		MUX_ASSERT(m_scrollBar is not null);

		if (args == Control.IsEnabledProperty)
		{
			//SCROLLVIEW_TRACE_VERBOSE(
			//	null,
			//	TRACE_MSG_METH_STR_INT,
			//	METH_NAME,
			//	this,
			//	L"IsEnabled",
			//	m_scrollBar.IsEnabled());

			UpdateCanScroll();
		}
		//#if DEBUG
		//		else if (args == UIElement.VisibilityProperty)
		//		{
		//			SCROLLVIEW_TRACE_VERBOSE(
		//				null,
		//				TRACE_MSG_METH_STR_INT,
		//				METH_NAME,
		//				this,
		//				L"Visibility",
		//				m_scrollBar.Visibility);
		//		}
		//		else if (args == ScrollBar.IndicatorModeProperty)
		//		{
		//			SCROLLVIEW_TRACE_VERBOSE(
		//				null,
		//				TRACE_MSG_METH_STR_STR,
		//				METH_NAME,
		//				this,
		//				L"IndicatorMode",
		//				TypeLogging::ScrollingIndicatorModeToString(m_scrollBar.IndicatorMode()).c_str());
		//		}
		//#endif //DBG
	}

	private void OnScroll(
		object sender,
		ScrollEventArgs args)
	{
		var scrollEventType = args.ScrollEventType;

		//SCROLLVIEW_TRACE_VERBOSE(
		//	null,
		//	TRACE_MSG_METH_STR,
		//	METH_NAME,
		//	this,
		//	TypeLogging::ScrollEventTypeToString(scrollEventType).c_str());

		if (m_scrollBar is null)
		{
			return;
		}

		if (!m_isScrollable && scrollEventType != ScrollEventType.ThumbPosition)
		{
			// This ScrollBar is not interactive. Restore its previous Value.
			m_scrollBar.Value = m_lastScrollBarValue;
			return;
		}

		switch (scrollEventType)
		{
			case ScrollEventType.First:
			case ScrollEventType.Last:
				break;
			case ScrollEventType.EndScroll:
				if (m_isScrollingWithMouse)
				{
					m_isScrollingWithMouse = false;
					RaiseIsScrollingWithMouseChanged();
				}
				break;
			case ScrollEventType.LargeDecrement:
			case ScrollEventType.LargeIncrement:
			case ScrollEventType.SmallDecrement:
			case ScrollEventType.SmallIncrement:
			case ScrollEventType.ThumbPosition:
			case ScrollEventType.ThumbTrack:
				if (scrollEventType == ScrollEventType.ThumbTrack)
				{
					if (!m_isScrollingWithMouse)
					{
						m_isScrollingWithMouse = true;
						RaiseIsScrollingWithMouseChanged();
					}
				}

				bool offsetChangeRequested = false;

				if (scrollEventType == ScrollEventType.ThumbPosition ||
					scrollEventType == ScrollEventType.ThumbTrack)
				{
					offsetChangeRequested = RaiseScrollToRequested(args.NewValue);
				}
				else
				{
					double offsetChange = 0.0;

					switch (scrollEventType)
					{
						case ScrollEventType.LargeDecrement:
							offsetChange = -Math.Min(m_lastScrollBarValue - m_scrollBar.Minimum, m_scrollBar.LargeChange);
							break;
						case ScrollEventType.LargeIncrement:
							offsetChange = Math.Min(m_scrollBar.Maximum - m_lastScrollBarValue, m_scrollBar.LargeChange);
							break;
						case ScrollEventType.SmallDecrement:
							offsetChange = -Math.Min(m_lastScrollBarValue - m_scrollBar.Minimum, m_scrollBar.SmallChange);
							break;
						case ScrollEventType.SmallIncrement:
							offsetChange = Math.Min(m_scrollBar.Maximum - m_lastScrollBarValue, m_scrollBar.SmallChange);
							break;
					}

					// When the requested Value is near the Mininum or Maximum, include a little additional velocity
					// to ensure the extreme value is reached.
					if (args.NewValue - m_scrollBar.Minimum < s_minMaxEpsilon)
					{
						MUX_ASSERT(offsetChange < 0.0);
						offsetChange -= s_minMaxEpsilon;
					}
					else if (m_scrollBar.Maximum - args.NewValue < s_minMaxEpsilon)
					{
						MUX_ASSERT(offsetChange > 0.0);
						offsetChange += s_minMaxEpsilon;
					}

					if (SharedHelpers.IsAnimationsEnabled())
					{
						offsetChangeRequested = RaiseAddScrollVelocityRequested(offsetChange);
					}
					else
					{
						offsetChangeRequested = RaiseScrollByRequested(offsetChange);
					}
				}

				if (!offsetChangeRequested)
				{
					// This request could not be requested, restore the previous Value.
					m_scrollBar.Value = m_lastScrollBarValue;
				}
				break;
		}

		m_lastScrollBarValue = m_scrollBar.Value;
	}

	private bool RaiseScrollToRequested(
		double offset)
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_DBL, METH_NAME, this, offset);
		if (ScrollToRequested is null)
		{
			return false;
		}

		var options = new ScrollingScrollOptions(
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		var scrollToRequestedEventArgs = new ScrollControllerScrollToRequestedEventArgs(
			offset,
			options);

		ScrollToRequested.Invoke(this, scrollToRequestedEventArgs);

		// The CorrelationId property was set by the ScrollToRequested event handler.
		// Typically it is set to a new unique value, but it may also be set to the ID 
		// from the prior request. This occurs when a request is quickly raised before 
		// the prior one was handed off to the Composition layer. The back-to-back requests 
		// are then coalesced into a single operation handed off to the Composition layer.
		int offsetChangeCorrelationId = scrollToRequestedEventArgs.CorrelationId;

		if (offsetChangeCorrelationId != -1)
		{
			// Only increment m_operationsCount when the returned CorrelationId represents a new request that was not coalesced with a pending request. 
			if (offsetChangeCorrelationId != m_lastOffsetChangeCorrelationIdForScrollTo)
			{
				m_lastOffsetChangeCorrelationIdForScrollTo = offsetChangeCorrelationId;
				m_operationsCount++;
			}

			return true;
		}

		return false;
	}

	private bool RaiseScrollByRequested(
		double offsetChange)
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_DBL, METH_NAME, this, offsetChange);

		if (ScrollByRequested is null)
		{
			return false;
		}

		var options = new ScrollingScrollOptions(
			ScrollingAnimationMode.Disabled,
			ScrollingSnapPointsMode.Ignore);

		var scrollByRequestedEventArgs = new ScrollControllerScrollByRequestedEventArgs(
			offsetChange,
			options);

		ScrollByRequested.Invoke(this, scrollByRequestedEventArgs);

		// The CorrelationId property was set by the ScrollByRequested event handler.
		// Typically it is set to a new unique value, but it may also be set to the ID 
		// from the prior request. This occurs when a request is quickly raised before 
		// the prior one was handed off to the Composition layer. The back-to-back requests 
		// are then coalesced into a single operation handed off to the Composition layer.
		int offsetChangeCorrelationId = scrollByRequestedEventArgs.CorrelationId;

		if (offsetChangeCorrelationId != -1)
		{
			// Only increment m_operationsCount when the returned CorrelationId represents a new request that was not coalesced with a pending request. 
			if (offsetChangeCorrelationId != m_lastOffsetChangeCorrelationIdForScrollBy)
			{
				m_lastOffsetChangeCorrelationIdForScrollBy = offsetChangeCorrelationId;
				m_operationsCount++;
			}

			return true;
		}

		return false;
	}

	bool RaiseAddScrollVelocityRequested(
		double offsetChange)
	{
		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH_DBL, METH_NAME, this, offsetChange);

		if (AddScrollVelocityRequested is null)
		{
			return false;
		}

		float? inertiaDecayRate = s_inertiaDecayRate;

		var addScrollVelocityRequestedEventArgs = new ScrollControllerAddScrollVelocityRequestedEventArgs(
			(float)(offsetChange * s_velocityNeededPerPixel),
			inertiaDecayRate);

		AddScrollVelocityRequested.Invoke(this, addScrollVelocityRequestedEventArgs);

		// The CorrelationId property was set by the AddScrollVelocityRequested event handler.
		// Typically it is set to a new unique value, but it may also be set to the ID 
		// from the prior request. This occurs when a request is quickly raised before 
		// the prior one was handed off to the Composition layer. The back-to-back requests 
		// are then coalesced into a single operation handed off to the Composition layer.
		int offsetChangeCorrelationId = addScrollVelocityRequestedEventArgs.CorrelationId;

		if (offsetChangeCorrelationId != -1)
		{
			// Only increment m_operationsCount when the returned CorrelationId represents a new request that was not coalesced with a pending request. 
			if (offsetChangeCorrelationId != m_lastOffsetChangeCorrelationIdForAddScrollVelocity)
			{
				m_lastOffsetChangeCorrelationIdForAddScrollVelocity = offsetChangeCorrelationId;
				m_operationsCount++;
			}

			return true;
		}

		return false;
	}

	private void RaiseCanScrollChanged()
	{
		if (CanScrollChanged is null)
		{
			return;
		}

		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		CanScrollChanged.Invoke(this, null);
	}

	private void RaiseIsScrollingWithMouseChanged()
	{
		if (IsScrollingWithMouseChanged is null)
		{
			return;
		}

		//SCROLLVIEW_TRACE_VERBOSE(null, TRACE_MSG_METH, METH_NAME, this);

		IsScrollingWithMouseChanged.Invoke(this, null);
	}
}
