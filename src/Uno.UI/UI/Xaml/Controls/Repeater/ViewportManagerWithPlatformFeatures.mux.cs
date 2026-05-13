// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewportManagerWithPlatformFeatures.cpp, commit 4b206bce3

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ViewportManagerWithPlatformFeatures
{
	// Pixel delta by which to inflate the cache buffer on each side.  Rather than fill the entire
	// cache buffer all at once, we chunk the work to make the UI thread more responsive.  We inflate
	// the cache buffer from 0 to a max value determined by the Maximum[Horizontal,Vertical]CacheLength
	// properties.
	private const double CacheBufferPerSideInflationPixelDelta = 40.0;

	public ViewportManagerWithPlatformFeatures(ItemsRepeater owner)
	{
		// ItemsRepeater is not fully constructed yet. Don't interact with it.
		m_owner = owner;
	}

	public override UIElement SuggestedAnchor
	{
		get
		{
			// The element generated during the ItemsRepeater.MakeAnchor call has precedence over the next tick.
			UIElement suggestedAnchor = m_makeAnchorElement;

			if (suggestedAnchor == null)
			{
				var anchorElement = m_scroller?.CurrentAnchor;

				if (anchorElement != null)
				{
					// We can't simply return anchorElement because, in case of nested ItemsRepeaters, it may not
					// be a direct child of ours, or even an indirect child. We need to walk up the tree starting
					// from anchorElement to figure out what child of ours (if any) to use as the suggested element.
					UIElement owner = m_owner;
					var child = anchorElement;
					var parent = CachedVisualTreeHelpers.GetParent(child) as UIElement;
					while (parent != null)
					{
						if (parent == owner)
						{
							suggestedAnchor = child;
							break;
						}

						child = parent;
						parent = CachedVisualTreeHelpers.GetParent(parent) as UIElement;
					}
				}
			}

			return suggestedAnchor;
		}
	}

	private void SetHorizontalCacheLength(double value)
	{
		if (m_maximumHorizontalCacheLength != value)
		{
			ValidateCacheLength(value);
			m_maximumHorizontalCacheLength = value;
			ResetCacheBuffer();
		}
	}

	private void SetVerticalCacheLength(double value)
	{
		if (m_maximumVerticalCacheLength != value)
		{
			ValidateCacheLength(value);
			m_maximumVerticalCacheLength = value;
			ResetCacheBuffer();
		}
	}

#if false
	// TODO Uno: GetLayoutVisibleWindowDiscardAnchor is declared in WinUI but has no callers.
	Rect GetLayoutVisibleWindowDiscardAnchor()
	{
		var visibleWindow = m_visibleWindow;

		if (HasScroller())
		{
			visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X + m_unshiftableShift.X;
			visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y + m_unshiftableShift.Y;
		}

		return visibleWindow;
	}
#endif

	public override Rect GetLayoutVisibleWindow()
	{
		var visibleWindow = m_visibleWindow;

		if (m_makeAnchorElement != null && m_isAnchorOutsideRealizedRange)
		{
			// The anchor is not necessarily laid out yet. Its position should default
			// to zero and the layout origin is expected to change once layout is done.
			// Until then, we need a window that's going to protect the anchor from
			// getting recycled.

			// Also, we only want to mess with the realization rect iff the anchor is not inside it.
			// If we fiddle with an anchor that is already inside the realization rect,
			// shifting the realization rect results in repeater, layout and scroller thinking that it needs to act upon StartBringIntoView.
			// We do NOT want that!

			visibleWindow.X = 0.0f;
			visibleWindow.Y = 0.0f;
		}
		else if (HasScroller())
		{
			visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X + m_unshiftableShift.X;
			visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y + m_unshiftableShift.Y;
		}

		return visibleWindow;
	}

	public override Rect GetLayoutRealizationWindow()
	{
		var realizationWindow = GetLayoutVisibleWindow();
		if (HasScroller())
		{
			realizationWindow.X -= (float)m_horizontalCacheBufferPerSide;
			realizationWindow.Y -= (float)m_verticalCacheBufferPerSide;
			realizationWindow.Width += (float)m_horizontalCacheBufferPerSide * 2.0f;
			realizationWindow.Height += (float)m_verticalCacheBufferPerSide * 2.0f;
		}

		return realizationWindow;
	}

	private void SetVisibleWindow(Rect visibleWindow)
	{
		if (visibleWindow.X != m_visibleWindow.X || visibleWindow.Y != m_visibleWindow.Y ||
			visibleWindow.Width != m_visibleWindow.Width || visibleWindow.Height != m_visibleWindow.Height)
		{
			m_visibleWindow = visibleWindow;
		}
	}

	private void SetLastLayoutRealizationWindow(Rect layoutRealizationWindow)
	{
		if (layoutRealizationWindow.X != m_lastLayoutRealizationWindow.X || layoutRealizationWindow.Y != m_lastLayoutRealizationWindow.Y ||
			layoutRealizationWindow.Width != m_lastLayoutRealizationWindow.Width || layoutRealizationWindow.Height != m_lastLayoutRealizationWindow.Height)
		{
			m_lastLayoutRealizationWindow = layoutRealizationWindow;
		}
	}

	private void SetPendingViewportShift(Point pendingViewportShift)
	{
		if (pendingViewportShift.X != m_pendingViewportShift.X || pendingViewportShift.Y != m_pendingViewportShift.Y)
		{
			m_pendingViewportShift = pendingViewportShift;
		}
	}

	private void SetExpectedViewportShift(double expectedViewportShiftX, double expectedViewportShiftY)
	{
		if (expectedViewportShiftX != m_expectedViewportShift.X || expectedViewportShiftY != m_expectedViewportShift.Y)
		{
			m_expectedViewportShift = new Point(expectedViewportShiftX, expectedViewportShiftY);
		}
	}

	private void SetUnshiftableShift(double unshiftableShiftX, double unshiftableShiftY)
	{
		if (unshiftableShiftX != m_unshiftableShift.X || unshiftableShiftY != m_unshiftableShift.Y)
		{
			m_unshiftableShift = new Point(unshiftableShiftX, unshiftableShiftY);
		}
	}

	private void SetLastScrollPresenterViewChangeCorrelationId(int correlationId)
	{
		if (correlationId != m_lastScrollPresenterViewChangeCorrelationId)
		{
			m_lastScrollPresenterViewChangeCorrelationId = correlationId;
		}
	}

	public override void SetLayoutExtent(Rect layoutExtent)
	{
		// UNO specific
		// On Uno (especially Android) the InvalidateArrange at the bottom of this method will actually cause an invalidate measure.
		// But this SetLayoutExtent method is invoked on each measure ... so it will cause a layout cycle!
		if (m_layoutExtent == layoutExtent)
		{
			return;
		}

		double expectedViewportShiftX = m_expectedViewportShift.X + m_layoutExtent.X - layoutExtent.X;
		double expectedViewportShiftY = m_expectedViewportShift.Y + m_layoutExtent.Y - layoutExtent.Y;

		SetExpectedViewportShift(expectedViewportShiftX, expectedViewportShiftY);

		// We tolerate viewport imprecisions up to 1 pixel to avoid invaliding layout too much.
		if (Math.Abs(m_expectedViewportShift.X) > 1.0 || Math.Abs(m_expectedViewportShift.Y) > 1.0)
		{
			// There are cases where we might be expecting a shift but not get it. We will
			// be waiting for the effective viewport event but if the scroller is not able
			// to perform the shift (perhaps because it cannot scroll in negative offset),
			// then we will end up not realizing elements in the visible window.
			// To avoid this, we register to layout updated for this layout pass. If we
			// get an effective viewport, we know we have a new viewport and we unregister from
			// layout updated. If we get the layout updated handler, then we know that the
			// scroller was unable to perform the shift and we invalidate measure and unregister
			// from the layout updated event.
			if (m_layoutUpdatedRevoker == null)
			{
				m_layoutUpdatedRevoker = Disposable.Create(() =>
				{
					m_owner.LayoutUpdated -= OnLayoutUpdated;
					m_layoutUpdatedRevoker = null;
				});
				m_owner.LayoutUpdated += OnLayoutUpdated;
			}
		}

		if (layoutExtent.X != m_layoutExtent.X || layoutExtent.Y != m_layoutExtent.Y ||
			layoutExtent.Width != m_layoutExtent.Width || layoutExtent.Height != m_layoutExtent.Height)
		{
			m_layoutExtent = layoutExtent;
		}

		SetPendingViewportShift(m_expectedViewportShift);

		if (m_scroller != null)
		{
			// We just finished a measure pass and have a new extent.
			// Let's make sure the scroller will run its arrange so that it tracks the anchor.
			(m_scroller as UIElement).InvalidateArrange();
		}
	}

	private void ResetLayoutExtent()
	{
		if (m_layoutExtent.X != 0.0f || m_layoutExtent.Y != 0.0f ||
			m_layoutExtent.Width != 0.0f || m_layoutExtent.Height != 0.0f)
		{
			m_layoutExtent = default;
		}
	}

	private void ResetVisibleWindow()
	{
		if (m_visibleWindow.X != 0.0f || m_visibleWindow.Y != 0.0f ||
			m_visibleWindow.Width != 0.0f || m_visibleWindow.Height != 0.0f)
		{
			m_visibleWindow = default;
		}
	}

	private void ResetLastLayoutRealizationWindow()
	{
		if (m_lastLayoutRealizationWindow.X != 0.0f || m_lastLayoutRealizationWindow.Y != 0.0f ||
			m_lastLayoutRealizationWindow.Width != 0.0f || m_lastLayoutRealizationWindow.Height != 0.0f)
		{
			m_lastLayoutRealizationWindow = default;
		}
	}

	private void ResetExpectedViewportShift()
	{
		if (m_expectedViewportShift.X != 0.0f || m_expectedViewportShift.Y != 0.0f)
		{
			m_expectedViewportShift = default;
		}
	}

	private void ResetPendingViewportShift()
	{
		if (m_pendingViewportShift.X != 0.0f || m_pendingViewportShift.Y != 0.0f)
		{
			m_pendingViewportShift = default;
		}
	}

	private void ResetUnshiftableShift()
	{
		if (m_unshiftableShift.X != 0.0f || m_unshiftableShift.Y != 0.0f)
		{
			m_unshiftableShift = default;
		}
	}

	private void ResetLastScrollPresenterViewChangeCorrelationId()
	{
		if (m_lastScrollPresenterViewChangeCorrelationId != -1)
		{
			m_lastScrollPresenterViewChangeCorrelationId = -1;
		}
	}

	// #ifdef DBG
	// TODO Uno: OnScrollViewerViewChangingDbg / OnScrollViewerViewChangedDbg are tracing-only and not ported.
	// #endif // DBG

	public override void OnLayoutChanged(bool isVirtualizing)
	{
		m_managingViewportDisabled = !isVirtualizing;

		ResetLayoutExtent();
		ResetExpectedViewportShift();
		ResetPendingViewportShift();

		if (m_managingViewportDisabled)
		{
			m_effectiveViewportChangedRevoker?.Dispose();
		}
		else if (m_effectiveViewportChangedRevoker == null
#if !UNO_HAS_ENHANCED_LIFECYCLE
			// Uno workaround: [Perf] Do not listen for viewport update if nothing to render!
			&& m_owner.ItemsSourceView?.Count > 0
#endif
			)
		{
			m_effectiveViewportChangedRevoker = Disposable.Create(() =>
			{
				m_owner.EffectiveViewportChanged -= OnEffectiveViewportChanged;
				m_effectiveViewportChangedRevoker = null;
			});
			m_owner.EffectiveViewportChanged += OnEffectiveViewportChanged;
		}

		ResetUnshiftableShift();
		ResetCacheBuffer();
	}

	public override void OnElementPrepared(UIElement element)
	{
		// The newly prepared element is not registered as an anchor candidate right away. It first needs to be arranged at least once so its position
		// tracked by the scroller is valid.
		// The element is first put into a list of prepared elements. Then once arranged it is put into a list of prepared+arranged elements. Then finally
		// at the beginning of the following measure pass, it is registered as an anchor candidate.

		if (!m_preparedElements.Contains(element))
		{
			m_preparedElements.Add(element);
		}
	}

	public override void OnElementCleared(UIElement element)
	{
		// Remove the element from the prepared and prepared+arranged lists so it no longer gets registered as an anchor candidate for the scroller
		// after the next arrange pass.

		m_preparedElements.Remove(element);
		m_preparedAndArrangedElements.Remove(element);

		if (element.CanBeScrollAnchor)
		{
			// Unregister the element as an anchor candidate since it was already declared as such.
			element.CanBeScrollAnchor = false;
		}
	}

	public override void OnOwnerMeasuring()
	{
		// This is because of a bug that causes effective viewport to not
		// fire if you register during arrange.
		// Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
		EnsureScroller();

		Rect currentLayoutRealizationWindow = GetLayoutRealizationWindow();

		if ((m_horizontalCacheBufferPerSide != 0.0 || m_verticalCacheBufferPerSide != 0.0) &&
			(m_lastLayoutRealizationWindow.Width <= 0.0f || m_lastLayoutRealizationWindow.Height <= 0.0f ||
			 currentLayoutRealizationWindow.Width <= 0.0f || currentLayoutRealizationWindow.Height <= 0.0f ||
			 !SharedHelpers.DoRectsIntersect(m_lastLayoutRealizationWindow, currentLayoutRealizationWindow)))
		{
			// Two consecutive measure passes use disconnected realization windows.
			// Reset the potential cache buffer so that it regrows from scratch.
			ResetLayoutRealizationWindowCacheBuffer();
		}

		SetLastLayoutRealizationWindow(currentLayoutRealizationWindow);

		if (m_skipScrollAnchorRegistrationsDuringNextMeasurePass)
		{
			m_skipScrollAnchorRegistrationsDuringNextMeasurePass = false;
		}
		else if (!m_skipScrollAnchorRegistrationsDuringNextArrangePass)
		{
			// Now that a new measure pass is starting, register the previously arranged elements as anchor candidates for the scroller.
			RegisterPreparedAndArrangedElementsAsScrollAnchorCandidates();
		}

#if !UNO_HAS_ENHANCED_LIFECYCLE
		// Uno workaround: Perf
		_uno_viewportUsedInLastMeasure = m_visibleWindow;
#endif
	}

	public override void OnOwnerArranged()
	{
		if (!m_skipScrollAnchorRegistrationsDuringNextMeasurePass)
		{
			if (m_skipScrollAnchorRegistrationsDuringNextArrangePass)
			{
				m_skipScrollAnchorRegistrationsDuringNextArrangePass = false;
			}
			else
			{
				// Now that an arrange pass completed, register the prepared elements as arranged. They will be registered as anchor candidates
				// during the next measure pass.
				RegisterPreparedElementsAsArranged();
			}
		}

		ResetExpectedViewportShift();

		// TODO Uno: Port the m_unshiftableShift / scroller offset reconciliation that requires
		// the ScrollViewer.HorizontalOffset / ScrollPresenter.HorizontalOffset values.

		if (!m_managingViewportDisabled)
		{
			// This is because of a bug that causes effective viewport to not
			// fire if you register during arrange.
			// Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
			// EnsureScroller();

			if (HasScroller())
			{
				double maximumHorizontalCacheBufferPerSide = m_maximumHorizontalCacheLength * m_visibleWindow.Width / 2.0;
				double maximumVerticalCacheBufferPerSide = m_maximumVerticalCacheLength * m_visibleWindow.Height / 2.0;

				bool continueBuildingCache =
					m_horizontalCacheBufferPerSide < maximumHorizontalCacheBufferPerSide ||
					m_verticalCacheBufferPerSide < maximumVerticalCacheBufferPerSide;

				if (continueBuildingCache)
				{
					m_horizontalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;
					m_verticalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;

					m_horizontalCacheBufferPerSide = Math.Min(m_horizontalCacheBufferPerSide, maximumHorizontalCacheBufferPerSide);
					m_verticalCacheBufferPerSide = Math.Min(m_verticalCacheBufferPerSide, maximumVerticalCacheBufferPerSide);

					// Since we grow the cache buffer at the end of the arrange pass,
					// we need to register work even if we just reached cache potential.
					RegisterCacheBuildWork();
				}
			}
		}
	}

	private void OnLayoutUpdated(object sender, object args)
	{
		m_layoutUpdatedRevoker?.Dispose();

		if (m_managingViewportDisabled)
		{
			return;
		}

		// We were expecting a viewport shift but we never got one and we are not going to in this
		// layout pass. We likely will never get this shift, so lets assume that we are never going to get it and
		// adjust our expected shift to track that. One case where this can happen is when there is no scroller
		// that can scroll in the direction where the shift is expected.
		if (m_pendingViewportShift.X != 0.0f || m_pendingViewportShift.Y != 0.0f)
		{
			REPEATER_TRACE_INFO("%ls: \tLayout Updated with pending shift %.0f %.0f- invalidating measure \n",
				GetLayoutId(),
				m_pendingViewportShift.X,
				m_pendingViewportShift.Y);

			// Assume this is never going to come.
			SetUnshiftableShift(m_unshiftableShift.X + m_pendingViewportShift.X, m_unshiftableShift.Y + m_pendingViewportShift.Y);
			ResetPendingViewportShift();
			ResetExpectedViewportShift();

			TryInvalidateMeasure();
		}
	}

	public override void OnMakeAnchor(UIElement anchor, bool isAnchorOutsideRealizedRange)
	{
		m_makeAnchorElement = anchor;
		m_isAnchorOutsideRealizedRange = isAnchorOutsideRealizedRange;
	}

	public override void OnBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		if (!m_managingViewportDisabled)
		{
			// We do not animate bring-into-view operations where the anchor is disconnected because
			// it doesn't look good (the blank space is obvious because the layout can't keep track
			// of two realized ranges while the animation is going on).
			if (m_isAnchorOutsideRealizedRange)
			{
				args.AnimationDesired = false;
			}

			// During the time between a bring into view request and the element coming into view we do not
			// want the anchor provider to pick some anchor and jump to it. Instead we want to anchor on the
			// element that is being brought into view. We can do this by making just that element as a potential
			// anchor candidate and ensure no other element of this repeater is an anchor candidate.
			// Once the layout pass is done and we render the frame, the element will be in frame and we can
			// switch back to letting the anchor provider pick a suitable anchor.

			// get the targetChild - i.e the immediate child of this repeater that is being brought into view.
			// Note that the element being brought into view could be a descendant.
			var targetChild = GetImmediateChildOfRepeater(args.TargetElement);

			// Make sure that only the target child can be the anchor during the bring into view operation.
			UnregisterScrollAnchorCandidates(targetChild /*exceptionElement*/, false /*registerAsPreparedAndArrangedElements*/);

			// Register to rendering event to go back to how things were before where any child can be the anchor.
			m_isBringIntoViewInProgress = true;
			if (m_renderingToken == null)
			{
				m_renderingToken = Disposable.Create(() =>
				{
					Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnCompositionTargetRendering;
					m_renderingToken = null;
				});
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnCompositionTargetRendering;
			}
		}
	}

	private UIElement GetImmediateChildOfRepeater(UIElement descendant)
	{
		UIElement targetChild = descendant;
		UIElement parent = CachedVisualTreeHelpers.GetParent(descendant) as UIElement;
		while (parent != null && parent != (DependencyObject)m_owner)
		{
			targetChild = parent;
			parent = CachedVisualTreeHelpers.GetParent(parent) as UIElement;
		}

		if (parent == null)
		{
			throw new InvalidOperationException("OnBringIntoViewRequested called with args.target element not under the ItemsRepeater that recieved the call");
		}

		return targetChild;
	}

	private void OnCompositionTargetRendering(object sender, object args)
	{
		MUX_ASSERT(!m_managingViewportDisabled);

		m_renderingToken?.Dispose();

		m_isBringIntoViewInProgress = false;

		if (m_makeAnchorElement != null)
		{
			m_makeAnchorElement = null;

			if (m_isAnchorOutsideRealizedRange)
			{
				m_isAnchorOutsideRealizedRange = false;

				// During the bring-into-view operation, the layout anchor was positioned at the top/left
				// of the viewport (see ViewportManagerWithPlatformFeatures::GetLayoutVisibleWindow()).
				// Now it may move within the viewport and require different items to be generated given
				// its final position. Thus a new measure pass is requested.
				TryInvalidateMeasure();
			}
		}

		// Now that the item has been brought into view, we can let the anchor provider pick a new anchor.
		foreach (var child in m_owner.Children)
		{
			if (child is null)
			{
				continue;
			}

			if (!child.CanBeScrollAnchor)
			{
				var info = ItemsRepeater.GetVirtualizationInfo(child);
				if (info.IsRealized && info.IsHeldByLayout)
				{
					child.CanBeScrollAnchor = true;
				}
			}
		}
	}

	public override void ResetScrollers()
	{
		m_scroller = null;
		// TODO Uno: ScrollPresenter ScrollStarting/ScrollCompleted/ZoomStarting/ZoomCompleted revokers not ported.
		m_effectiveViewportChangedRevoker?.Dispose();
		m_isAnchorOutsideRealizedRange = false;
		m_skipScrollAnchorRegistrationsDuringNextMeasurePass = false;
		m_skipScrollAnchorRegistrationsDuringNextArrangePass = false;
		m_ensuredScroller = false;
		ResetExpectedViewportShift();
		ResetPendingViewportShift();
		ResetUnshiftableShift();
		ResetLastScrollPresenterViewChangeCorrelationId();
	}

	private void OnCacheBuildActionCompleted()
	{
		m_cacheBuildAction = null;
		if (!m_managingViewportDisabled)
		{
			m_owner.InvalidateMeasure();
		}
	}

	// TODO Uno: Detect an imminent ScrollPresenter view change triggered by a ScrollTo, ScrollBy, ZoomTo, or ZoomBy
	// call. The C++ source implements OnScrollPresenterScrollStarting, OnScrollPresenterZoomStarting,
	// OnScrollPresenterViewChangeStarting, OnScrollPresenterScrollCompleted, OnScrollPresenterZoomCompleted,
	// OnScrollPresenterViewChangeCompleted to anticipate the destination view and re-realize items around it before
	// the Compositor moves the ScrollPresenter.Content visual. These handlers depend on IScrollPresenter2 events
	// (ScrollStarting/ZoomStarting) and the m_skipScrollAnchorRegistrations* flags + m_lastScrollPresenterViewChangeCorrelationId.

	private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		MUX_ASSERT(!m_managingViewportDisabled);
		REPEATER_TRACE_INFO("%ls: \tEffectiveViewportChanged event callback \n", GetLayoutId());

		if (m_lastScrollPresenterViewChangeCorrelationId != -1)
		{
			return;
		}

		bool invalidateMeasure = false;
		bool invalidatedMeasure = UpdateViewport(args.EffectiveViewport);
		Point emptyPoint = default;
		Rect emptyRect = default;

		if (m_pendingViewportShift != emptyPoint)
		{
			ResetPendingViewportShift();
			invalidateMeasure = true;
		}

		if (m_unshiftableShift != emptyPoint)
		{
			ResetUnshiftableShift();
			invalidateMeasure = true;
		}

		if (m_visibleWindow == emptyRect && m_layoutExtent != emptyRect)
		{
			// We got cleared.
			ResetLayoutExtent();
			invalidateMeasure = true;
		}

		if (invalidateMeasure && !invalidatedMeasure)
		{
			TryInvalidateMeasure();
		}

		// We got a new viewport, we don't need to wait for layout updated anymore to
		// see if our request for a pending shift was handled.
		m_layoutUpdatedRevoker?.Dispose();
	}

	private void EnsureScroller()
	{
		if (!m_ensuredScroller)
		{
			ResetScrollers();

#if !UNO_HAS_ENHANCED_LIFECYCLE
			// Uno workaround: [Perf] Do not listen for viewport update if nothing to render!
			if (m_owner.ItemsSourceView?.Count <= 0)
			{
				return;
			}
#endif

			var parent = CachedVisualTreeHelpers.GetParent(m_owner);
			while (parent != null)
			{
				if (parent is IScrollAnchorProvider scroller)
				{
					m_scroller = scroller;
					break;
				}

				parent = CachedVisualTreeHelpers.GetParent(parent);
			}

			/*
			Uno workaround:
				This a workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/2349.
				While this bugs occurs only for `Popup` on WinUI, it might occurs even in the "main" visual tree on uno.
				The reason is that they do have a `ScrollViewer` at the root of the app on WinUI (to support the SIP), but we don't in Uno.

			if (m_scroller == null)
			{
				// We usually update the viewport in the post arrange handler. But, since we don't have
				// a scroller, let's do it now.
				UpdateViewport(new Rect( ));
			}
			else*/
			if (!m_managingViewportDisabled)
			{
				// When the ItemsRepeater is hosted in a Popup, m_scroller is null, but ItemsRepeater.EffectiveViewportChanged will still get raised.
				m_effectiveViewportChangedRevoker = Disposable.Create(() =>
				{
					m_owner.EffectiveViewportChanged -= OnEffectiveViewportChanged;
					m_effectiveViewportChangedRevoker = null;
				});
				m_owner.EffectiveViewportChanged += OnEffectiveViewportChanged;
			}

			// TODO Uno: Subscribe to IScrollPresenter ScrollStarting/ScrollCompleted/ZoomStarting/ZoomCompleted events.
			// The IScrollPresenter implementation in Uno does not currently expose the v2 ScrollStarting/ZoomStarting
			// events required to anticipate the destination view in OnScrollPresenterScrollStarting/OnScrollPresenterZoomStarting.

			m_ensuredScroller = true;
		}
	}

	// Returns True when m_visibleWindow changes and TryInvalidateMeasure is invoked.
	private bool UpdateViewport(Rect effectiveViewport)
	{
		// Disabled for non-virtualizing layout in RadioButtons, may need to be revisited (https://github.com/unoplatform/uno/issues/4752)
		// MUX_ASSERT(!m_managingViewportDisabled);

		if (-effectiveViewport.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
			-effectiveViewport.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
		{
			REPEATER_TRACE_INFO("%ls: \tViewport is invalid. visible window cleared. \n", GetLayoutId());
			// We got cleared.
			ResetVisibleWindow();
		}
		else
		{
			const float roundingTolerance = 0.01f;

			if (Math.Abs(m_visibleWindow.X - effectiveViewport.X) > roundingTolerance ||
				Math.Abs(m_visibleWindow.Y - effectiveViewport.Y) > roundingTolerance ||
				Math.Abs(m_visibleWindow.Width - effectiveViewport.Width) > roundingTolerance ||
				Math.Abs(m_visibleWindow.Height - effectiveViewport.Height) > roundingTolerance)
			{
				SetVisibleWindow(effectiveViewport);

#if !UNO_HAS_ENHANCED_LIFECYCLE
				// Uno workaround [BEGIN]: For perf considerations, do not invalidate the tree on each viewport update
				// (Viewport updates are quite frequent, this would cause lot of unnecessary layout pass which would impact scroll perf, especially on Android).
				if (m_owner.Layout is VirtualizingLayout vl // If not a VirtualizingLayout, we actually don't have to re-measure items!
					&& vl.IsSignificantViewportChange(m_owner.LayoutState, _uno_viewportUsedInLastMeasure, m_visibleWindow))
				// Uno workaround [END]
#endif
				{
					TryInvalidateMeasure();
					return true;
				}
			}
		}

		return false;
	}

	public override void ResetLayoutRealizationWindowCacheBuffer()
	{
		ResetCacheBuffer(false /*registerCacheBuildWork*/);
	}

	private void ResetCacheBuffer(bool registerCacheBuildWork = true)
	{
		m_horizontalCacheBufferPerSide = 0.0;
		m_verticalCacheBufferPerSide = 0.0;

		if (!m_managingViewportDisabled && registerCacheBuildWork)
		{
			// We need to start building the realization buffer again.
			RegisterCacheBuildWork();
		}
	}

	private void ValidateCacheLength(double cacheLength)
	{
		if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsNaN(cacheLength))
		{
			throw new ArgumentOutOfRangeException("The maximum cache length must be equal or superior to zero.");
		}
	}

	private void RegisterPreparedElementsAsArranged()
	{
		if (m_preparedElements.Count == 0)
		{
			return;
		}

		foreach (var preparedElement in m_preparedElements)
		{
			if (!m_preparedAndArrangedElements.Contains(preparedElement))
			{
				m_preparedAndArrangedElements.Add(preparedElement);
			}
		}

		m_preparedElements.Clear();
	}

	private void RegisterPreparedAndArrangedElementsAsScrollAnchorCandidates()
	{
		if (m_preparedAndArrangedElements.Count == 0)
		{
			return;
		}

		foreach (var anchorCandidate in m_preparedAndArrangedElements)
		{
			if (!anchorCandidate.CanBeScrollAnchor)
			{
				var info = ItemsRepeater.GetVirtualizationInfo(anchorCandidate);

				if (info.IsRealized && info.IsHeldByLayout)
				{
					anchorCandidate.CanBeScrollAnchor = true;
				}
			}
		}

		m_preparedAndArrangedElements.Clear();
	}

	private void RegisterCacheBuildWork()
	{
		MUX_ASSERT(!m_managingViewportDisabled);
		if (m_owner.Layout != null &&
			m_cacheBuildAction == null)
		{
			// We capture 'owner' (a strong refernce on ItemsRepeater) to make sure ItemsRepeater is still around
			// when the async action completes. By protecting ItemsRepeater, we also ensure that this instance
			// of ViewportManager (referenced by 'this' pointer) is valid because the lifetime of ItemsRepeater
			// and ViewportManager is the same (see ItemsRepeater.m_viewportManager).
			// We can't simply hold a strong reference on ViewportManager because it's not a COM object.
			var strongOwner = m_owner;
			m_cacheBuildAction = m_owner.Dispatcher.RunIdleAsync(_ => OnCacheBuildActionCompleted());
		}
	}

	private void TryInvalidateMeasure()
	{
		// Don't invalidate measure if we have an invalid window.
		if (m_visibleWindow != new Rect()
#if !UNO_HAS_ENHANCED_LIFECYCLE
			// Uno workaround: [Perf] Do not invalidate measure if nothing to render!
			&& m_owner.ItemsSourceView?.Count > 0
#endif
			)
		{
			// We invalidate measure instead of just invalidating arrange because
			// we don't invalidate measure in UpdateViewport if the view is changing to
			// avoid layout cycles.
			REPEATER_TRACE_INFO("%ls: \tInvalidating measure due to viewport change. \n", GetLayoutId());
#if __ANDROID__
			// Uno workaround:
			// This method is mainly used by the UpdateViewport but also the OnLayoutUpdated.
			// Both are usually being invoked while in the Arrange phase, but on Android we cannot invalidate the layout while arranging it,
			// we have to defer the invalidate.
			// Note: We use RunAnimation to get it as soon as possible.
			// Note: UpdateViewport might also be invoked on Load, but in that case we expect either the viewport to not change,
			//		 either the layout is pending anyway, so we should not have an extra useless layout pass.
			_ = m_owner.Dispatcher.RunAnimation(() => m_owner.InvalidateMeasure());
#else
			m_owner.InvalidateMeasure();
#endif
		}
	}

	private void UnregisterScrollAnchorCandidates(UIElement exceptionElement, bool registerAsPreparedAndArrangedElements)
	{
		foreach (var child in m_owner.Children)
		{
			if (child is null)
			{
				continue;
			}

			if (child.CanBeScrollAnchor && child != exceptionElement)
			{
				child.CanBeScrollAnchor = false;

				if (registerAsPreparedAndArrangedElements)
				{
					m_preparedAndArrangedElements.Add(child);
				}
			}
		}
	}

	private string GetLayoutId()
		=> m_owner.Layout?.LayoutId;

	// #ifdef DBG
	// TODO Uno: TraceFieldsDbg / TraceScrollerDbg are tracing-only helpers and are not ported.
	// #endif // DBG
}
