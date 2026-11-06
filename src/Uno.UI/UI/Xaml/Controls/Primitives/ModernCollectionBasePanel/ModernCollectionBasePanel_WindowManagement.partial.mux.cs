// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_WindowManagement_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

// Uncomment to output ModernCollectionBasePanel debugging information
//#define MCBP_DEBUG

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	// Determine if a point is inside the window, or is before or after it in the virtualizing direction.
	private RelativePosition GetReferenceDirectionFromWindow(
		Rect referenceRect,
		Rect window)
	{
		float firstReferenceEdge = PointFromRectInVirtualizingDirection(referenceRect);
		float lastReferenceEdge = firstReferenceEdge + SizeFromRectInVirtualizingDirection(referenceRect);
		float firstWindowEdge = PointFromRectInVirtualizingDirection(window);
		float lastWindowEdge = firstWindowEdge + SizeFromRectInVirtualizingDirection(window);

		RelativePosition result;

		if (lastReferenceEdge < firstWindowEdge)
		{
			result = RelativePosition.Before;
		}
		else if (lastWindowEdge < firstReferenceEdge)
		{
			result = RelativePosition.After;
		}
		else
		{
			result = RelativePosition.Inside;
		}

		return result;
	}

	// Calculates the visible window by analyzing our layout and our parent ScrollViewer's state.
	private Rect CalculateVisibleWindowFromScrollPoint(
		Point newScrollPoint,
		float zoomFactor)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		global::System.Diagnostics.Debug.Assert(spScrollViewer is not null);

		double viewWidth = 0;
		double viewHeight = 0;
		Rect scrollViewport = default;

		viewWidth = spScrollViewer!.ViewportWidth;
		viewHeight = spScrollViewer.ViewportHeight;

		scrollViewport.X = newScrollPoint.X;
		scrollViewport.Y = newScrollPoint.Y;
		scrollViewport.Width = (float)viewWidth;
		scrollViewport.Height = (float)viewHeight;

		// Build our own visible window
		Rect pVisibleWindow = CalculateVisibleWindowFromScrollData(scrollViewport, zoomFactor);

		m_windowState.validWindowCalculation = true;

		return pVisibleWindow;
	}

	// Calculates the visible window from a set of scroll data
	private Rect CalculateVisibleWindowFromScrollData(
		Rect scrollViewport,
		float zoomFactor)
	{
		Orientation orientation;
		Rect ourVisibleWindow = default;

		orientation = m_spLayoutStrategy.VirtualizationDirection;

		// Get the scroll content's viewport, projected onto our panel
		ourVisibleWindow.X = scrollViewport.X / zoomFactor;
		ourVisibleWindow.Y = scrollViewport.Y / zoomFactor;

		// Until XAML converges on whether or not to reflow and remeasure based on zoom factors,
		// I'm going to ignore the zoom factor in the non-virtualizing direction.
		// All virtualization code will see the original non-zoomed window size in the regard
		switch (orientation)
		{
			case Orientation.Horizontal:
				ourVisibleWindow.Width = (float)scrollViewport.Width / zoomFactor;
				ourVisibleWindow.Height = (float)m_windowState.lastAvailableSize.Height;
				break;

			case Orientation.Vertical:
				ourVisibleWindow.Width = (float)m_windowState.lastAvailableSize.Width;
				ourVisibleWindow.Height = (float)scrollViewport.Height / zoomFactor;
				break;
		}

		// ItemsPresenter may host a header / lead padding that we need to account for,
		// so in this case we subtract our layout position relative to our visual parent.
		ourVisibleWindow.X -= m_originFromItemsPresenter.X;
		ourVisibleWindow.Y -= m_originFromItemsPresenter.Y;

		return ourVisibleWindow;
	}

	// Using the current visible window, calculate the realization window (taking into
	// account buffer space, etc).
	//
	// allowCache should be true during normal measure passes and false otherwise when initially setting the realization window.
	// The allowCache parameter allows us to incrementally build a cache by inflating the realization window during
	// measure passes.  When true, we will increase the realization window by CacheBufferPerSideInflationPixelDelta.
	// When false, we will reset the cache buffers to 0.
	private void SetRealizationWindowFromVisibleWindow(
		bool allowCache)
	{
		Size bufferSize = default;
		Orientation panningOrientation;
		double visibleWindowLength = 0.0;
		double maxCacheBufferPerSide = 0.0;
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		bool barDragging = false;
		bool isLastElementRealized = false;
		float panelExtent = 0.0f;

		// Determine the maximum amount of cache buffer per side.
		panningOrientation = m_spLayoutStrategy.VirtualizationDirection;
		switch (panningOrientation)
		{
			case Orientation.Horizontal:
				visibleWindowLength = m_windowState.GetVisibleWindow().Width;
				break;

			case Orientation.Vertical:
				visibleWindowLength = m_windowState.GetVisibleWindow().Height;
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}
		maxCacheBufferPerSide = (m_cacheLength * visibleWindowLength) / 2;
		global::System.Diagnostics.Debug.Assert(maxCacheBufferPerSide >= 0);

		if (spScrollViewer is not null)
		{
			barDragging = spScrollViewer.IsThumbDragging;

			if (barDragging)
			{
				ResetCacheBuffers();
			}
		}


		// Include the cache buffers if applicable.
		//
		// If we are mouse-dragging, all scroll movement is going to happen synchronously with ticks, so
		// any buffers we create is just adding wasted work that slows down our responsiveness
		if (allowCache)
		{
			bool shouldProcessCache = true;

			shouldProcessCache = CanIncreaseCacheLength();

			// Increment the current cache buffer per side.  Cap it at the max cache buffer per side.
			if (shouldProcessCache)
			{
				m_windowState.currentCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;
				m_windowState.currentCacheBufferPerSide = Math.Min(m_windowState.currentCacheBufferPerSide, maxCacheBufferPerSide);

				// The realization window might change and we might need to correct for estimation errors.
				// In this case, we should also shift the window by the correction amount.
				m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
			}

			switch (panningOrientation)
			{
				case Orientation.Horizontal:
					bufferSize.Width = (float)m_windowState.currentCacheBufferPerSide;
					break;

				case Orientation.Vertical:
					bufferSize.Height = (float)m_windowState.currentCacheBufferPerSide;
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false);
					break;
			}
		}


		// The realization window should always be large enough to accommodate a buffer on each end
		Rect realizationWindow = default;
		realizationWindow.Width = m_windowState.GetVisibleWindow().Width + 2 * bufferSize.Width;
		realizationWindow.Height = m_windowState.GetVisibleWindow().Height + 2 * bufferSize.Height;

		// Put a buffer above/left of the visible window, but the realization window won't go negative
		realizationWindow.X = Math.Max(m_windowState.GetVisibleWindow().X - bufferSize.Width, 0.0f);
		realizationWindow.Y = Math.Max(m_windowState.GetVisibleWindow().Y - bufferSize.Height, 0.0f);

		// The realization window should not be bigger than the panel's extent.
		// Otherwise, we might end up with disconnected containers or headers in scenarios where
		// we scroll outside of the panel (e.g. to a large header/footer/padding).
		// We check to see if the last item is realized and make sure that the realized window dimensions include the realized item
		IsLastElementRealized(out isLastElementRealized, out panelExtent);
		if (isLastElementRealized)
		{
			// if panelExtent is smaller than the visibleWindowLength (width or height depending on orientation), we adjust it to be the visibleWindowLength
			// for the case when we are inserting items, we want to make sure that all items have a legitimate chance of being inside the visible window
			// otherwise, items that are inserted before the last item such that the last item is pushed outside the current visible window are not all visible
			// and the last item will not be visible as well
			panelExtent = Math.Max(panelExtent, (float)visibleWindowLength);

			SetSizeFromRectInVirtualizingDirection(
				ref realizationWindow,
				Math.Min(SizeFromRectInVirtualizingDirection(realizationWindow), panelExtent));

			SetPointFromRectInVirtualizingDirection(
				ref realizationWindow,
				Math.Min(
					PointFromRectInVirtualizingDirection(realizationWindow),
					panelExtent - SizeFromRectInVirtualizingDirection(realizationWindow)));
		}

		m_windowState.SetRealizationWindow(realizationWindow);

#if !HAS_UNO // TODO Uno: no telemetry provider.
		TraceLoggingProviderWrite(
			XamlTelemetry, "ModernCollectionBasePanel_RealizationWindow",
			TraceLoggingUInt64(reinterpret_cast<uint64_t>(this), "ObjectPointer"),
			TraceLoggingWideString(GetHandle()->m_strName.GetBuffer(), "Name"),
			TraceLoggingFloat32(realizationWindow.X, "X"),
			TraceLoggingFloat32(realizationWindow.Y, "Y"),
			TraceLoggingFloat32(realizationWindow.Width, "Width"),
			TraceLoggingFloat32(realizationWindow.Height, "Height"),
			TraceLoggingLevel(WINEVENT_LEVEL_VERBOSE));
#endif

		// We are finished building the realization window because we are allowing cache
		// and the cache is at its max.
		m_windowState.cachePotentialReached = m_windowState.currentCacheBufferPerSide >= maxCacheBufferPerSide;

		if (!m_windowState.cachePotentialReached || !allowCache)
		{
			// make sure we are registered with budget manager so we get an opportunity to fix this
			if (!m_isRegisteredForCallbacks)
			{
				BuildTreeService spBuildTree = DXamlCore.Current.GetBuildTreeService();
				spBuildTree.RegisterWork(this);
			}
			global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);
		}
	}

	// infinity looses the piece of information that would allow wrapping controls (like textblock, wrapgrid, image) to properly size.
	// it does however convey that 'feel free to take as much as you need though'. We have decided to keep the constraint, but
	// have frameworkelement not clip in certain situations.
	internal override bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation orientation)
	{
		Orientation virtualizingDirection;

		virtualizingDirection = m_spLayoutStrategy.VirtualizationDirection;

		return virtualizingDirection == orientation;
	}

	internal partial class WindowState
	{
		internal WindowState()
		{
			m_currentRealizationWindow = default;
			m_visibleWindowFromScrollState = default;
			m_visibleWindowCoerced = RectUtil.CreateEmptyRect();
			validWindowCalculation = false;
			m_lastScrollOffset = default;
			m_lastZoomFactor = 1.0f;
			m_lastScrollWasMouseLargeClick = false;
			m_lastScrollWasForward = false;
			currentTickNumber = -1;
			firstVisibleItemIndexOfPreviousTick = -1;
			m_panningDirectionBase = PanelScrollingDirection.None;
			currentCacheBufferPerSide = 0.0;
			allowWindowShiftOnEstimationErrorCorrection = false;
			m_estimationCorrectionPending = false;

			lastAvailableSize.Width = lastAvailableSize.Height = double.PositiveInfinity;
		}

		// Returns the current adjusment resulting for the possible CoercedVisibleWindow
		// Note : protected by apiset, called only for Vertical Orientation
		internal double GetCurrentAdjustmentY()
		{
			if (RectUtil.GetIsEmpty(m_visibleWindowCoerced))
			{
				return 0.0;
			}
			else
			{
				return m_visibleWindowFromScrollState.Y - m_visibleWindowCoerced.Y;
			}
		}

		internal void ApplyAdjustment(Point correction)
		{
			m_currentRealizationWindow.X += correction.X;
			m_currentRealizationWindow.Y += correction.Y;

			// If we don't currently have a command, this action constitutes initiating one
			if (RectUtil.GetIsEmpty(m_visibleWindowCoerced))
			{
				m_visibleWindowCoerced = m_visibleWindowFromScrollState;
			}

			m_visibleWindowCoerced.X += correction.X;
			m_visibleWindowCoerced.Y += correction.Y;
		}

		internal Rect GetVisibleWindow()
		{
			if (RectUtil.GetIsEmpty(m_visibleWindowCoerced))
			{
				return m_visibleWindowFromScrollState;
			}
			else
			{
				return m_visibleWindowCoerced;
			}
		}

		internal void SetScrollStateVisibleWindow(
			Rect newWindowFromScrollState,
			out bool shouldInvalidate)
		{
			// Get the current visible window, based on whether we have an override from a command
			Rect currentVisibleWindow = GetVisibleWindow();

			// If the window has changed, we'll invalidate
			if (!DoubleUtil.AreClose(currentVisibleWindow.X, newWindowFromScrollState.X) ||
				!DoubleUtil.AreClose(currentVisibleWindow.Y, newWindowFromScrollState.Y) ||
				!DoubleUtil.AreClose(currentVisibleWindow.Width, newWindowFromScrollState.Width) ||
				!DoubleUtil.AreClose(currentVisibleWindow.Height, newWindowFromScrollState.Height))
			{
				shouldInvalidate = true;
			}
			else
			{
				shouldInvalidate = false;
			}

			m_visibleWindowFromScrollState = newWindowFromScrollState;
		}

		internal bool HasCoercedWindow() => !RectUtil.GetIsEmpty(m_visibleWindowCoerced);

		internal void ClearCoercedWindow() => m_visibleWindowCoerced = RectUtil.CreateEmptyRect();

		internal void CorrectVisibleWindowForMouseLargeClick(ModernCollectionBasePanel panel)
		{
			if (m_lastScrollWasMouseLargeClick)
			{
				WindowCommand func = (ref Rect pNewVisibleWindow) =>
				{
					pNewVisibleWindow = GetVisibleWindow();

					float actualHeaderOutsideVisibleWindow = 0;
					actualHeaderOutsideVisibleWindow = panel.GetRealHeaderExtentOutsideVisibleWindow();
					pNewVisibleWindow.Y += actualHeaderOutsideVisibleWindow * (m_lastScrollWasForward ? -1 : 1);
				};

				m_command = func;
				m_lastScrollWasMouseLargeClick = false;
				panel.InvalidateMeasure();
			}
		}
	}

	// Clear our understanding of our parent ScrollViewer.
	private void DetachPanelComponents()
	{
		ScrollViewer? sv = null;
		m_wrScrollViewer?.TryGetTarget(out sv);
		if (sv is not null)
		{
			m_epScrollViewerViewChangingHandler.Disposable = null;
			m_epScrollViewerViewChangedHandler.Disposable = null;
			m_epScrollViewerSizeChangedHandler.Disposable = null;
		}

		RemoveStickyHeaders();

		m_wrScrollViewer = null;
		m_wrItemsPresenter = null;
	}

	// Called when our ScrollViewer's has changed. Update our current visible window.
	private void OnScrollViewChanged(
		object? pSender,
		ScrollViewerViewChangedEventArgs pArgs)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		if (spScrollViewer is not null)
		{
			double horizontalOffset = 0.0f;
			double verticalOffset = 0.0f;
			float zoomFactor = m_windowState.m_lastZoomFactor;

			horizontalOffset = spScrollViewer.HorizontalOffset;
			verticalOffset = spScrollViewer.VerticalOffset;

			if (!spScrollViewer.IsInDirectManipulationZoom)
			{
				Rect visibleWindow = default;
				bool shouldInvalidate = false;

				zoomFactor = spScrollViewer.ZoomFactor;

				m_windowState.m_lastScrollOffset.X = (float)horizontalOffset;
				m_windowState.m_lastScrollOffset.Y = (float)verticalOffset;
				m_windowState.m_lastZoomFactor = zoomFactor;

				visibleWindow = CalculateVisibleWindowFromScrollPoint(m_windowState.m_lastScrollOffset, m_windowState.m_lastZoomFactor);
				BeginTrackingOnViewportSizeChange(m_windowState.GetVisibleWindow(), visibleWindow);
				m_windowState.SetScrollStateVisibleWindow(visibleWindow, out shouldInvalidate);
				SetRealizationWindowFromVisibleWindow(false /* allowCache */);
				if (shouldInvalidate)
				{
					// If the visible window starts before or at offset 0 in the virtualizing direction (e.g. looking at the header),
					// then we consider that the intent is to show the beginning of the list and realize starting from the first visible element.
					if (PointFromRectInVirtualizingDirection(visibleWindow) > 0.0f)
					{
						m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
					}

					InvalidateMeasure();
				}
				else
				{
					// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
					if (m_bUseStickyHeaders)
					{
						Size finalSize = new Size(visibleWindow.Width, visibleWindow.Height);
						UpdateStickyHeaders(finalSize);
					}
				}

#if HAS_UNO
				// TODO Uno: ViewChanging is never raised and no DManip drives the sticky headers, so every view change moves them.
				if (shouldInvalidate && m_bUseStickyHeaders)
				{
					UpdateStickyHeaders(new Size(visibleWindow.Width, visibleWindow.Height));
				}
#endif

				// Make sure that we don't setup transitions during manipulation.
				bool isIntermediate;
				isIntermediate = pArgs.IsIntermediate;
				IsIgnoringTransitions = isIntermediate;
			}
		}
	}

	// Called when our ScrollViewer's view changes. Update our current visible window.
	private void OnScrollViewChanging(
		object? pSender,
		ScrollViewerViewChangingEventArgs pArgs)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		if (spScrollViewer is not null)
		{
			ScrollViewerView spIView;
			double horizontalOffset = 0.0f;
			double verticalOffset = 0.0f;
			float zoomFactor = m_windowState.m_lastZoomFactor;

			ScrollViewerViewChangingEventArgs pArgsNoRef = pArgs;
			spIView = pArgsNoRef.NextView;

			horizontalOffset = spIView.HorizontalOffset;
			verticalOffset = spIView.VerticalOffset;

			if (!spScrollViewer.IsInDirectManipulationZoom)
			{
				Rect visibleWindow = default;
				bool shouldInvalidate = false;
				double viewportHeight = 0.0f;
				double scrolledBy = 0.0f;
				bool scrolledByAViewport = false;

				m_windowState.m_lastScrollWasMouseLargeClick = false;

				viewportHeight = spScrollViewer.ViewportHeight;

				// Did we scroll by a viewport and what direction did we scroll
				scrolledBy = DoubleUtil.Abs(m_windowState.m_lastScrollOffset.Y - verticalOffset);
				scrolledByAViewport = DoubleUtil.AreWithinTolerance(scrolledBy, viewportHeight, 1.0);

				// capture the scroll direction - forward or backward
				m_windowState.m_lastScrollWasForward = m_windowState.m_lastScrollOffset.Y < verticalOffset;


				zoomFactor = spIView.ZoomFactor;

				m_windowState.m_lastScrollOffset.X = (float)horizontalOffset;
				m_windowState.m_lastScrollOffset.Y = (float)verticalOffset;
				m_windowState.m_lastZoomFactor = zoomFactor;

				visibleWindow = CalculateVisibleWindowFromScrollPoint(m_windowState.m_lastScrollOffset, m_windowState.m_lastZoomFactor);
				BeginTrackingOnViewportSizeChange(m_windowState.GetVisibleWindow(), visibleWindow);
				UpdateViewportOffsetDelta(m_windowState.GetVisibleWindow(), visibleWindow);
				m_windowState.SetScrollStateVisibleWindow(visibleWindow, out shouldInvalidate);
				SetRealizationWindowFromVisibleWindow(false /* allowCache */);
				if (shouldInvalidate)
				{
					// If the visible window starts before or at offset 0 in the virtualizing direction (e.g. looking at the header),
					// then we consider that the intent is to show the beginning of the list and realize starting from the first visible element.
					if (PointFromRectInVirtualizingDirection(visibleWindow) > 0.0f)
					{
						m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
					}
					IsIgnoringTransitions = true;
					InvalidateMeasure();

					// What we are trying to identify is that if this view change
					// was initiated by the user clicking on the vertical scrollbar
					// large increase or decrease buttons. We need to do correction to
					// the visible window in this particular case when using sticky headers.
					// This is because the sticky header is occluding part of the visible
					// window - hence when the user clicks on these buttons we end up
					// skipping items if we do not account for the occluded part of the
					// visible window.
					// Unfortunately - there is no easy way to figure out if it was a
					// large increase/decrease which caused the view change without
					// tightly coupling with the ScrollViewer's scollbar - instead
					// we use the following check to get the same result.

					// Note: PageUp/Down and ScrollItemIntoView will not come into this
					// code path since the visible window is already set and shouldInvalidate
					// will be false causing us to skip this code block.

					// we are using sticky headers (and)
					// we scrolled by a viewport [large increase/decrease causes that] (and)
					// we are not in direct manipulation (and)
					// we are not thumb dragging
					if (m_bUseStickyHeaders &&
						scrolledByAViewport &&
						!spScrollViewer.IsInDirectManipulation)
					{
						bool isThumbDragging = false;

						isThumbDragging = spScrollViewer.IsThumbDragging;
						if (!isThumbDragging)
						{
							// we need to adjust the visible window and account
							// for the part occluded by the sticky header
							m_windowState.m_lastScrollWasMouseLargeClick = true;
						}
					}
				}

#if HAS_UNO
				// TODO Uno: no DManip drives the sticky headers, so every view change moves them.
				if (m_bUseStickyHeaders)
				{
					UpdateStickyHeaders(new Size(visibleWindow.Width, visibleWindow.Height));
				}
#endif
			}
		}
	}


	private void OnScrollViewerSizeChanged(
		object? pSender,
		SizeChangedEventArgs pArgs)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		if (spScrollViewer is not null)
		{
			Rect ourVisibleWindow = default;
			ourVisibleWindow = CalculateVisibleWindowFromScrollPoint(m_windowState.m_lastScrollOffset, m_windowState.m_lastZoomFactor);
			BeginTrackingOnViewportSizeChange(m_windowState.GetVisibleWindow(), ourVisibleWindow);
			m_windowState.SetScrollStateVisibleWindow(ourVisibleWindow);
			SetRealizationWindowFromVisibleWindow(false /* allowCache */);

			InvalidateMeasure();
		}
	}


	// Walk up the parent chain and find our ScrollViewer.
	// Attach its OnScrollViewChanging and ScrollViewChanged to our OnScrollViewChange method.
	// Also attach to our ItemsPresenter.
	private void AttachPanelComponents()
	{
		// Walk up the parent chain and tally up all the scroll viewers
		DependencyObject? spCurrentAsDO = this;
		DependencyObject? spParentAsDO;

		while (spCurrentAsDO is not null)
		{
			spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

			if (spParentAsDO is not null)
			{
				ScrollViewer? spNodeScrollViewer = spParentAsDO as ScrollViewer;
				if (spNodeScrollViewer is not null)
				{
					// Bind event handlers to the correct methods, using a pass-through functor
					spNodeScrollViewer.ViewChanging += OnScrollViewChanging;
					m_epScrollViewerViewChangingHandler.Disposable = Disposable.Create(() => spNodeScrollViewer.ViewChanging -= OnScrollViewChanging);
					spNodeScrollViewer.ViewChanged += OnScrollViewChanged;
					m_epScrollViewerViewChangedHandler.Disposable = Disposable.Create(() => spNodeScrollViewer.ViewChanged -= OnScrollViewChanged);
					spNodeScrollViewer.SizeChanged += OnScrollViewerSizeChanged;
					m_epScrollViewerSizeChangedHandler.Disposable = Disposable.Create(() => spNodeScrollViewer.SizeChanged -= OnScrollViewerSizeChanged);

					m_wrScrollViewer = new WeakReference<ScrollViewer>(spNodeScrollViewer);

					break;
				}
				else if (spParentAsDO is ItemsPresenter spParentAsItemsPresenter)
				{
					m_wrItemsPresenter = new WeakReference<ItemsPresenter>(spParentAsItemsPresenter);
				}
			}
			spCurrentAsDO = spParentAsDO;
		}

		// At this point we won't have gotten any ViewChanging or ViewChanged events, so we need to get a window ourselves.
		PrimeVisibleWindow();
		SetRealizationWindowFromVisibleWindow(false);
		InvalidateMeasure();

#if DEBUG
		spParentAsDO = VisualTreeHelper.GetParent(this);
		if (spParentAsDO is not null)
		{
			ItemsPresenter? spParentAsItemsPresenter = spParentAsDO as ItemsPresenter;
			if (spParentAsItemsPresenter is null)
			{
				ScrollContentPresenter? spParentAsScrollContentPresenter = spParentAsDO as ScrollContentPresenter;
				if (spParentAsScrollContentPresenter is null)
				{
					// Unsupported case.  Right now ModernPanel only supports:
					//  1) ItemsControl, which is detected by checking if the parent is an ItemsPresenter
					//  2) Directly parenting under a ScrollViewer, as in the Hub case.
					//     If parent is not SCP in the Hub case, our transform and window detection code may be broken.
					global::System.Diagnostics.Debug.Assert(false);
				}
			}
		}
#endif
	}

	// Before we get any ScrollViewChanging or ScrollViewChanged events, we need to proactively get a window
	// (instead of waiting for an event to tell us of a window).
	// This method gets a window from the ScrollViewer's offset, if possible. If a ScrollViewer isn't
	// available, we go with a default window.
	// TODO Uno: WinUI always finds its RootScrollViewer, whose viewport is the window. Uno has none, so an
	// ItemsControl without a ScrollViewer in its template uses what it's given, or the window if that's unbounded.
	private static double GetVisibleLengthWithoutScrollViewer(double availableLength, double? windowLength)
		=> !double.IsInfinity(availableLength) ? availableLength : windowLength ?? 0;

	private void PrimeVisibleWindow(bool allowTrackingOnViewportSizeChange = false)
	{
		Rect ourVisibleWindow = default;

		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		if (spScrollViewer is not null)
		{
			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;
			float zoomFactor = 1.0f;

			horizontalOffset = spScrollViewer.HorizontalOffset;
			verticalOffset = spScrollViewer.VerticalOffset;
			zoomFactor = spScrollViewer.ZoomFactor;

			m_windowState.m_lastScrollOffset.X = (float)horizontalOffset;
			m_windowState.m_lastScrollOffset.Y = (float)verticalOffset;
			m_windowState.m_lastZoomFactor = zoomFactor;

			ourVisibleWindow = CalculateVisibleWindowFromScrollPoint(m_windowState.m_lastScrollOffset, zoomFactor);
		}
		else
		{

			// Haven't yet been attached to ScrollViewers. Should just bail out and use a predetermined window.
			// We'll default to 0 pixels size in the virtualization direction, and give it the panel's available size
			// in the non-virtualizing direction
			Orientation panningOrientation;
			panningOrientation = m_spLayoutStrategy.VirtualizationDirection;

			switch (panningOrientation)
			{
				case Orientation.Horizontal:
					ourVisibleWindow.Width = GetVisibleLengthWithoutScrollViewer(m_windowState.lastAvailableSize.Width, XamlRoot?.Size.Width);
					ourVisibleWindow.Height = m_windowState.lastAvailableSize.Height;
					break;

				case Orientation.Vertical:
					ourVisibleWindow.Width = m_windowState.lastAvailableSize.Width;
					ourVisibleWindow.Height = GetVisibleLengthWithoutScrollViewer(m_windowState.lastAvailableSize.Height, XamlRoot?.Size.Height);
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false);
					ourVisibleWindow.Width = 0;
					ourVisibleWindow.Height = 0;
					break;
			}

			// TODO Uno: see GetVisibleLengthWithoutScrollViewer.
			m_windowState.validWindowCalculation = IsInLiveTree;
		}

		if (allowTrackingOnViewportSizeChange)
		{
			BeginTrackingOnViewportSizeChange(m_windowState.GetVisibleWindow(), ourVisibleWindow);
		}

		m_windowState.SetScrollStateVisibleWindow(ourVisibleWindow);
	}

	// Resets the current cache buffer to 0
	private protected void ResetCacheBuffers()
	{
		m_windowState.currentCacheBufferPerSide = 0.0;

		m_windowState.cachePotentialReached = false;

		// make sure we are registered with budget manager so we get an opportunity to fix this
		if (!m_isRegisteredForCallbacks)
		{
			BuildTreeService spBuildTree = DXamlCore.Current.GetBuildTreeService();
			spBuildTree.RegisterWork(this);
		}
		global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);
	}

	// This method works in two stages:
	// Stage 1: Determine if the new window is disconnected from either our headers or containers
	//          and figure out the closest valid elements from which to estimate
	// Stage 2: Calculate new anchor indices by estimating how many groups/items it would take for
	//          us to get from the old realized elements to the new window
	private void DetectAndHandleDisconnectedView()
	{
		bool windowDisconnectedFromContainers = false;
		int disconnectedReferenceItemIndex = -1;
		Rect disconnectedReferenceItemRect = default;
		RelativePosition referenceItemPositionFromWindow = default;

		bool windowDisconnectedFromHeaders = false;
		int disconnectedReferenceGroupIndex = -1;
		Rect disconnectedReferenceGroupRect = default;
		RelativePosition referenceGroupPositionFromWindow = default;

		Rect windowToFill = m_windowState.GetRealizationWindow();

		// If we are tracking an element, the viewport will shift to follow it
		// and, consequentially, we can't end up with a disconnected view.
		if (m_viewportBehavior.isTracking)
		{
#if DEBUG
			Rect futureRealizationWindow = GetRealizationWindowAfterViewportShift();
			referenceItemPositionFromWindow = GetReferenceDirectionFromWindow(
				m_viewportBehavior.elementBounds,
				futureRealizationWindow);

			global::System.Diagnostics.Debug.Assert(referenceItemPositionFromWindow == RelativePosition.Inside);
#endif
			if (m_viewportBehavior.type == ElementType.GroupHeader &&
				m_containerManager.GetValidContainerCount() > 0)
			{
				int startItemIndex;
				m_cacheManager.GetGroupInformationFromGroupIndex(
					m_viewportBehavior.index,
					out startItemIndex,
					out _ /* pItemCountInGroup */);

				// We are tracking a group header, which means we are going to use it as an anchor during layout.
				// While generating forward or backward from our header anchor, it's very common
				// to iterate over the realized containers block. Thus, it's important that there is
				// no gap between our anchor header and the contiguous block of containers.
				// If we detect such gap, we will call RemoveMeasureLeftOvers to clear it.
				// If we don't, we would break the contiguousness invariant and crash during
				// the Generate run.

				int firstValidContainerItemIndex = m_containerManager.GetItemIndexFromValidIndex(0);
				if (firstValidContainerItemIndex > startItemIndex)
				{
					// There is a gap after the anchor that we need to clear.
					CollectionIterator gapRemovalIterator = new(m_cacheManager);
					gapRemovalIterator.Init(firstValidContainerItemIndex, ElementType.ItemContainer);
					RemoveMeasureLeftOvers(gapRemovalIterator, true /* goForward */);
				}
				else
				{
					int lastValidContainerItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);
					if (lastValidContainerItemIndex < startItemIndex - 1)
					{
						// There is a gap before the anchor that we need to clear.
						CollectionIterator gapRemovalIterator = new(m_cacheManager);
						gapRemovalIterator.Init(lastValidContainerItemIndex, ElementType.ItemContainer);
						RemoveMeasureLeftOvers(gapRemovalIterator, false /* goForward */);
					}
				}
			}

			return;
		}

		// Let's see if any of the items are still in view
		if (m_containerManager.GetValidContainerCount() > 0)
		{
			VirtualizationInformation pFirstContainerInfo;
			VirtualizationInformation pLastContainerInfo;
			UIElement? spContainer;
			Rect tempRect;

			spContainer = m_containerManager.GetContainerAtValidIndex(0);
			pFirstContainerInfo = GetVirtualizationInformationFromElement(spContainer!)!;

			spContainer = m_containerManager.GetContainerAtValidIndex(m_containerManager.GetValidContainerCount() - 1);
			pLastContainerInfo = GetVirtualizationInformationFromElement(spContainer!)!;

			tempRect = pFirstContainerInfo.Bounds;

			referenceItemPositionFromWindow = GetReferenceDirectionFromWindow(tempRect, windowToFill);
			if (referenceItemPositionFromWindow == RelativePosition.After)
			{
				// Our first item is now beyond the window
				windowDisconnectedFromContainers = true;
				disconnectedReferenceItemIndex = m_containerManager.GetItemIndexFromValidIndex(0);
				disconnectedReferenceItemRect = tempRect;
			}

			if (!windowDisconnectedFromContainers && referenceItemPositionFromWindow == RelativePosition.Before)
			{
				// Check the last item
				tempRect = pLastContainerInfo.Bounds;
				referenceItemPositionFromWindow = GetReferenceDirectionFromWindow(tempRect, windowToFill);
				if (referenceItemPositionFromWindow == RelativePosition.Before)
				{
					// Our last item is before the window
					windowDisconnectedFromContainers = true;
					disconnectedReferenceItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);
					disconnectedReferenceItemRect = tempRect;
				}
			}
		}
		else
		{
			if (m_cacheManager.GetTotalItemCount() > 0)
			{
				// We haven't realized any item containers, but they exist in the collection. Mark them as disconnected
				windowDisconnectedFromContainers = true;

				Point locationOfFirstElement;
				locationOfFirstElement = m_spLayoutStrategy.GetPositionOfFirstElement();

				disconnectedReferenceItemIndex = 0;
				disconnectedReferenceItemRect.X = locationOfFirstElement.X;
				disconnectedReferenceItemRect.Y = locationOfFirstElement.Y;
				disconnectedReferenceItemRect.Width = 0;
				disconnectedReferenceItemRect.Height = 0;
				referenceItemPositionFromWindow = RelativePosition.Before;
			}
		}

		// Now let's see if our headers are disconnected
		if (m_containerManager.GetValidHeaderCount() > 0)
		{
			VirtualizationInformation pFirstHeaderInfo;
			VirtualizationInformation pLastHeaderInfo;
			UIElement? spHeader;
			Rect tempRect;

			spHeader = m_containerManager.GetHeaderAtValidIndex(0);
			pFirstHeaderInfo = GetVirtualizationInformationFromElement(spHeader!)!;

			spHeader = m_containerManager.GetHeaderAtValidIndex(m_containerManager.GetValidHeaderCount() - 1);
			pLastHeaderInfo = GetVirtualizationInformationFromElement(spHeader!)!;

			tempRect = pFirstHeaderInfo.Bounds;

			referenceGroupPositionFromWindow = GetReferenceDirectionFromWindow(tempRect, windowToFill);
			if (referenceGroupPositionFromWindow == RelativePosition.After)
			{
				// Our first item is now beyond the window
				windowDisconnectedFromHeaders = true;
				disconnectedReferenceGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(0);
				disconnectedReferenceGroupRect = tempRect;
			}

			if (!windowDisconnectedFromHeaders && referenceGroupPositionFromWindow == RelativePosition.Before)
			{
				// Check the last item
				tempRect = pLastHeaderInfo.Bounds;
				referenceGroupPositionFromWindow = GetReferenceDirectionFromWindow(tempRect, windowToFill);
				if (referenceGroupPositionFromWindow == RelativePosition.Before)
				{
					// Our last item is before the window
					windowDisconnectedFromHeaders = true;
					disconnectedReferenceGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);
					disconnectedReferenceGroupRect = tempRect;
				}
			}
		}
		else
		{
			if (m_cacheManager.GetTotalLayoutGroupCount() > 0)
			{
				// We haven't realized any headers, but groups exist in the collection.
				windowDisconnectedFromHeaders = true;

				Point locationOfFirstElement;
				locationOfFirstElement = m_spLayoutStrategy.GetPositionOfFirstElement();

				disconnectedReferenceGroupIndex = 0;
				disconnectedReferenceGroupRect.X = locationOfFirstElement.X;
				disconnectedReferenceGroupRect.Y = locationOfFirstElement.Y;
				disconnectedReferenceGroupRect.Width = 0;
				disconnectedReferenceGroupRect.Height = 0;
				referenceGroupPositionFromWindow = RelativePosition.Before;
			}
		}

		// Now that we know if we've disconnected from our headers and/or items, let's decide what and where to anchor
		if (m_cacheManager.IsGrouping())
		{
			if (windowDisconnectedFromHeaders && windowDisconnectedFromContainers)
			{
				// Nothing overlaps the new window. Throw it all away and get an anchor group
				int itemIndexAtReferenceHeader;
				int itemCountInReferenceHeader;
				m_cacheManager.GetGroupInformationFromGroupIndex(disconnectedReferenceGroupIndex, out itemIndexAtReferenceHeader, out itemCountInReferenceHeader);

				EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
				headerRef.ElementIndex = disconnectedReferenceGroupIndex;
				headerRef.ElementBounds = disconnectedReferenceGroupRect;

				EstimationReference containerRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
				containerRef.ElementIndex = disconnectedReferenceItemIndex;
				containerRef.ElementBounds = disconnectedReferenceItemRect;

				RecycleAllContainersAndHeaders();

				GenerateAnchorsForWindow(windowToFill, headerRef, containerRef);
			}
			else if (windowDisconnectedFromHeaders)
			{
				if (m_containerManager.GetValidContainerCount() > 0)
				{
					// Only the headers are disconnected, but the containers still overlap the new window.
					// This could happen if we are moving around in a gigantic group.
					// There's nothing to do here. Just keep walking the containers.
					// GenerateForward/Backward will handle headers as they encounter the need
				}
				else
				{
					// We have no valid containers, but our headers went disconnected
					// We need to re-anchor a header
					int itemIndexAtReferenceHeader;
					int itemCountInReferenceHeader;
					m_cacheManager.GetGroupInformationFromGroupIndex(disconnectedReferenceGroupIndex, out itemIndexAtReferenceHeader, out itemCountInReferenceHeader);

					EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
					headerRef.ElementIndex = disconnectedReferenceGroupIndex;
					headerRef.ElementBounds = disconnectedReferenceGroupRect;

					RecycleAllContainersAndHeaders();

					GenerateAnchorsForWindow(windowToFill, headerRef, null);
				}
			}
			else if (windowDisconnectedFromContainers)
			{
				global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() > 0);

				if (m_containerManager.GetValidContainerCount() > 0)
				{
					// The containers are all useless to us, but we have headers hitting the window.
					// The headers may be floating, but they may have unrealized items that are now relevant
					// Recycle all the existing containers and see if there is a nonempty group in the window
					// with which we can generate an appropriate anchor item index, so we don't have to generate
					// a ridiculous distance back to the window

					UIElement? spHeader;
					EstimationReference containerRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
					containerRef.ElementIndex = disconnectedReferenceItemIndex;
					containerRef.ElementBounds = disconnectedReferenceItemRect;

					int groupIndex;
					int itemIndexInGroup;
					int itemCountInGroup;
					m_cacheManager.GetGroupInformationFromItemIndex(disconnectedReferenceItemIndex, out groupIndex, out itemIndexInGroup, out itemCountInGroup);

					int validHeaderIndex = m_containerManager.GetValidHeaderIndexFromGroupIndex(groupIndex);
					global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidHeaderIndexWithinBounds(validHeaderIndex));
					spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);

					EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
					headerRef.ElementIndex = groupIndex;
					headerRef.ElementBounds = GetBoundsFromElement(spHeader!);

					RecycleAllContainersAndHeaders();

					GenerateAnchorsForWindow(windowToFill, headerRef, containerRef);
				}
				else
				{
					// We have no containers at all, but the headers aren't disconnected
					// Some of the headers may be far outside the window, so lets get the best candidate
					int validHeaderIndex = 0;
					UIElement? spCandidateHeader;
					Rect candidateHeaderBounds = default;
					RelativePosition candidatePosition = default;
					bool foundCandidate = false;

					// Since the headers aren't disconnected, we know the first header must be either before or inside the window
					// So let's walk the headers until we reach the window or reach the end of our valid collection
					while (!foundCandidate && validHeaderIndex < m_containerManager.GetValidHeaderCount())
					{
						spCandidateHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
						candidateHeaderBounds = GetBoundsFromElement(spCandidateHeader!);
						candidatePosition = GetReferenceDirectionFromWindow(candidateHeaderBounds, windowToFill);

						switch (candidatePosition)
						{
							case RelativePosition.Inside:
								// Bingo!
								foundCandidate = true;
								break;

							case RelativePosition.After:
								// We went from a header that was before the window, to one that was after the window.
								// The headers "straddle" the window. So, let's back up and use the one before the window,
								// because that's the group whose items, if any, will hit the window
								--validHeaderIndex;
								foundCandidate = true;
								break;

							case RelativePosition.Before:
								// Haven't found it yet, keep looking
								++validHeaderIndex;
								break;
						}
					}

					if (!foundCandidate)
					{
						// If we got here, we're wrong because the last header was before the window
						// Therefore, headers should have shown up as disconnected
						global::System.Diagnostics.Debug.Assert(false);
						return;
					}

					// Now, we're only interested in recycling and anchoring if any of the "interesting" groups are nonempty
					int firstItemIndexInGroup;
					int itemCountInGroup;
					int groupIndex;
					foundCandidate = false;

					while (!foundCandidate && validHeaderIndex < m_containerManager.GetValidHeaderCount() && candidatePosition != RelativePosition.After)
					{
						spCandidateHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
						if (spCandidateHeader is not null)
						{
							candidateHeaderBounds = GetBoundsFromElement(spCandidateHeader);
							candidatePosition = GetReferenceDirectionFromWindow(candidateHeaderBounds, windowToFill);

							if (candidatePosition != RelativePosition.After)
							{
								// See if this is a nonempty group from which to anchor an item
								groupIndex = m_containerManager.GetGroupIndexFromValidIndex(validHeaderIndex);
								m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out firstItemIndexInGroup, out itemCountInGroup);
								if (itemCountInGroup > 0)
								{
									foundCandidate = true;
								}
								else
								{
									// Keep looking...
									++validHeaderIndex;
								}
							}
						}
						else
						{
							// The header is not realized. Keep looking...
							++validHeaderIndex;
						}
					}

					// We found a nonempty group that wasn't after the window, nor was it more than one header in front of the window
					// Looks like we finally found a good place to generate an anchor item
					if (foundCandidate)
					{
						EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
						headerRef.ElementIndex = m_containerManager.GetGroupIndexFromValidIndex(validHeaderIndex);
						headerRef.ElementBounds = candidateHeaderBounds;

						RecycleAllContainersAndHeaders();

						GenerateAnchorsForWindow(windowToFill, headerRef, null);
					}
				}
			}
			else
			{
				// Nothing is disconnected. Looks like there's nothing to do here
			}
		}
		else
		{
			if (windowDisconnectedFromContainers)
			{
				EstimationReference containerRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
				containerRef.ElementIndex = disconnectedReferenceItemIndex;
				containerRef.ElementBounds = disconnectedReferenceItemRect;

				RecycleAllContainersAndHeaders();

				GenerateAnchorsForWindow(windowToFill, null, containerRef);
			}
		}
	}

	// Given a window to fill with containers and headers, estimate
	// the location of the anchor container and header (if any),
	// call into ICG2 to get the container and header, set both of
	// their bounds, and add to valid containers and headers.
	private void GenerateAnchorsForWindow(
		Rect windowToFill,
		EstimationReference? headerReference,
		EstimationReference? containerReference)
	{
		// Ideally, we'll make these params optional on the strategy and pass the pointers through, but for now, we'll just have to
		// create default objects in lieu of nullptrs until codegen supports this behavior. But we'll do the translation
		// here to avoid inflicting this pain everywhere else
		EstimationReference resolvedHeaderReference = headerReference ?? CreateDefaultEstimationReference(ElementType.GroupHeader);
		EstimationReference resolvedContainerReference = containerReference ?? CreateDefaultEstimationReference(ElementType.ItemContainer);

		resolvedHeaderReference.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, resolvedHeaderReference.ElementIndex);

		// We should not be tracking any particular element in this case.
		if (m_viewportBehavior.isTracking)
		{
			m_viewportBehavior.Reset();
		}

		if (m_cacheManager.IsGrouping())
		{
			Size desiredSize;
			Rect headerRect;
			Size measureSizeForHeader;
			int targetLayoutGroupIndex;
			UIElement? spNewHeader;
			LayoutReference refInfo = CreateDefaultLayoutReference();

			m_spLayoutStrategy.EstimateElementIndex(
				ElementType.GroupHeader,
				resolvedHeaderReference,
				resolvedContainerReference,
				windowToFill,
				out headerRect,
				out targetLayoutGroupIndex);

			global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() == 0);
			global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalLayoutGroupCount() > 0);

			// Constrain the index
			targetLayoutGroupIndex = Math.Max(0, targetLayoutGroupIndex);
			targetLayoutGroupIndex = Math.Min(targetLayoutGroupIndex, m_cacheManager.GetTotalLayoutGroupCount() - 1);

			int targetGroupIndex = m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, targetLayoutGroupIndex);
			measureSizeForHeader = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, targetGroupIndex, windowToFill);

			// Generate the anchor header
			spNewHeader = m_icg2.GenerateHeaderAtGroupIndex(targetGroupIndex);
			m_containerManager.PlaceInValidHeaders(targetGroupIndex, spNewHeader);
			PrepareHeaderViaItemsHost(targetGroupIndex, measureSizeForHeader, spNewHeader!);

			// Store its desired position
			VirtualizationInformation pHeaderInfo = GetVirtualizationInformationFromElement(spNewHeader!)!;

			// Now, if we have a nonempty group, let's generate a suitable anchor item container
			int firstItemInAnchorGroup;
			int itemCountInAnchorGroup;
			m_cacheManager.GetGroupInformationFromGroupIndex(targetGroupIndex, out firstItemInAnchorGroup, out itemCountInAnchorGroup);
			if (itemCountInAnchorGroup > 0)
			{
				int targetItemIndex;
				UIElement? spNewContainer;
				Rect containerRect;
				Size measureSizeForContainer;

				EstimationReference headerEstimationInfo = CreateDefaultEstimationReference(ElementType.GroupHeader);
				headerEstimationInfo.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, targetGroupIndex);
				headerEstimationInfo.ElementBounds = headerRect;

				m_spLayoutStrategy.EstimateElementIndex(
					ElementType.ItemContainer,
					headerEstimationInfo,
					CreateDefaultEstimationReference(ElementType.ItemContainer),
					windowToFill,
					out containerRect,
					out targetItemIndex);

				// Constrain the index
				targetItemIndex = Math.Max(firstItemInAnchorGroup, targetItemIndex);
				targetItemIndex = Math.Min(targetItemIndex, firstItemInAnchorGroup + itemCountInAnchorGroup - 1);

				measureSizeForContainer = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, targetItemIndex, windowToFill);

				// Generate the anchor container
				spNewContainer = m_icg2.GenerateContainerAtIndex(targetItemIndex);
				m_containerManager.PlaceInValidContainers(targetItemIndex, spNewContainer);
				PrepareContainerViaItemsHost(targetItemIndex, measureSizeForContainer, spNewContainer!);

				// Measure it so we can get accurate ideas for placement
				spNewContainer!.Measure(measureSizeForContainer);
				desiredSize = spNewContainer.DesiredSize;

				refInfo.ReferenceBounds = containerRect;
				refInfo.ReferenceIsHeader = false;
				refInfo.RelativeLocation = ReferenceIdentity.Myself;
				containerRect = m_spLayoutStrategy.GetElementBounds(
					ElementType.ItemContainer,
					targetItemIndex,
					desiredSize,
					refInfo,
					windowToFill);

				// Store its desired position
				SetBoundsForElement(spNewContainer, containerRect);

				pHeaderInfo.Bounds = headerRect;
			}
			else
			{
				pHeaderInfo.Bounds = headerRect;
			}
			// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
			if (m_bUseStickyHeaders)
			{
				ConfigureStickyHeader(spNewHeader!, targetGroupIndex, headerRect);
			}
		}
		else
		{
			Rect containerRect;
			Size measureSize;
			Size desiredSize;
			int targetItemIndex;
			UIElement? spNewContainer;
			LayoutReference refInfo = CreateDefaultLayoutReference();

			m_spLayoutStrategy.EstimateElementIndex(
				ElementType.ItemContainer,
				CreateDefaultEstimationReference(ElementType.GroupHeader),
				resolvedContainerReference,
				windowToFill,
				out containerRect,
				out targetItemIndex);

			global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidContainerCount() == 0);
			global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalItemCount() > 0);

			// Constrain the index
			targetItemIndex = Math.Max(0, targetItemIndex);
			targetItemIndex = Math.Min(targetItemIndex, m_cacheManager.GetTotalItemCount() - 1);

			measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, targetItemIndex, windowToFill);

			// Generate the anchor item
			spNewContainer = m_icg2.GenerateContainerAtIndex(targetItemIndex);
			m_containerManager.PlaceInValidContainers(targetItemIndex, spNewContainer);
			PrepareContainerViaItemsHost(targetItemIndex, measureSize, spNewContainer!);

			// Measure it so we can get accurate ideas for placement
			spNewContainer!.Measure(measureSize);
			desiredSize = spNewContainer.DesiredSize;

			refInfo.ReferenceBounds = containerRect;
			refInfo.ReferenceIsHeader = false;
			refInfo.RelativeLocation = ReferenceIdentity.Myself;
			containerRect = m_spLayoutStrategy.GetElementBounds(
				ElementType.ItemContainer,
				targetItemIndex,
				desiredSize,
				refInfo,
				windowToFill);

			// Store its desired position
			SetBoundsForElement(spNewContainer, containerRect);
		}
	}

	// Given a group index and its reference, estimate its position,
	// call into ICG2 to get a header for the group, place the header
	// in the valid headers, and sets the header's bounds to the estimated
	// position.
	private void GenerateAnchorForGroup(
		int groupIndex,
		EstimationReference? headerReference,
		EstimationReference? containerReference)
	{
		global::System.Diagnostics.Debug.Assert(m_cacheManager.IsGrouping());

		UIElement? spNewHeader;
		Rect headerRect;
		Size measureSize;

		// Ideally, we'll make these params optional on the strategy and pass the pointers through, but for now, we'll just have to
		// create default objects in lieu of nullptrs until codegen supports this behavior. But we'll do the translation
		// here to avoid inflicting this pain everywhere else
		EstimationReference resolvedHeaderReference = headerReference ?? CreateDefaultEstimationReference(ElementType.GroupHeader);
		EstimationReference resolvedContainerReference = containerReference ?? CreateDefaultEstimationReference(ElementType.ItemContainer);

		resolvedHeaderReference.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, resolvedHeaderReference.ElementIndex);

		measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, groupIndex, m_windowState.GetRealizationWindow());

		headerRect = m_spLayoutStrategy.EstimateElementBounds(
			ElementType.GroupHeader,
			m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, groupIndex),
			resolvedHeaderReference,
			resolvedContainerReference,
			m_windowState.GetRealizationWindow());

		global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() == 0);
		global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalGroupCount() > 0);

		// Generate the anchor header
		spNewHeader = m_icg2.GenerateHeaderAtGroupIndex(groupIndex);
		m_containerManager.PlaceInValidHeaders(groupIndex, spNewHeader);
		PrepareHeaderViaItemsHost(groupIndex, measureSize, spNewHeader!);

		// Store its desired position
		SetBoundsForElement(spNewHeader!, headerRect);

		// Validate that the anchor didn't overestimate and land outside the valid bounds
		CorrectForEstimationErrors(false /* adjustWindow */);

		// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
		if (m_bUseStickyHeaders)
		{
			ConfigureStickyHeader(spNewHeader!, groupIndex, headerRect);
		}
	}

	// Given an item index and its references, estimate its position,
	// call into ICG2 to get a container, place the container
	// in the valid containers, and sets the container's bounds to the estimated
	// position.
	private void GenerateAnchorForItem(
		int itemIndex,
		EstimationReference? headerReference,
		EstimationReference? containerReference)
	{
		UIElement? spNewContainer;
		Rect containerRect;
		Size measureSize;

		// Ideally, we'll make these params optional on the strategy and pass the pointers through, but for now, we'll just have to
		// create default objects in lieu of nullptrs until codegen supports this behavior. But we'll do the translation
		// here to avoid inflicting this pain everywhere else
		EstimationReference resolvedHeaderReference = headerReference ?? CreateDefaultEstimationReference(ElementType.GroupHeader);
		EstimationReference resolvedContainerReference = containerReference ?? CreateDefaultEstimationReference(ElementType.ItemContainer);

		resolvedHeaderReference.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, resolvedHeaderReference.ElementIndex);

		measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, itemIndex, m_windowState.GetRealizationWindow());

		containerRect = m_spLayoutStrategy.EstimateElementBounds(
			ElementType.ItemContainer,
			itemIndex,
			resolvedHeaderReference,
			resolvedContainerReference,
			m_windowState.GetRealizationWindow());

		global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidContainerCount() == 0);
		global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalItemCount() > 0);

		// Generate the anchor container
		spNewContainer = m_icg2.GenerateContainerAtIndex(itemIndex);
		m_containerManager.PlaceInValidContainers(itemIndex, spNewContainer);
		PrepareContainerViaItemsHost(itemIndex, measureSize, spNewContainer!);

		// Store its desired position
		SetBoundsForElement(spNewContainer!, containerRect);

		// Validate that the anchor didn't overestimate and land outside the valid bounds
		CorrectForEstimationErrors(false /* adjustWindow */);
	}

	// Returns whether the last element of the data source was realized.
	// If that's the case, it will also return the panel's extent.
	private void IsLastElementRealized(
		out bool result,
		out float panelExtent)
	{
		bool checkIfLastContainerIsRealized = !m_cacheManager.IsGrouping();
		float localPanelExtent = 0.0f;

		result = false;

		// Note: we don't care much about the case where there is no items or groups in
		// the collection, so we will just return FALSE in that case.

		if (m_cacheManager.IsGrouping() && m_containerManager.GetValidHeaderCount() > 0)
		{
			UIElement? spHeader;

			int lastGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);
			spHeader = m_containerManager.GetHeaderAtValidIndex(m_containerManager.GetValidHeaderCount() - 1);

			if (m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, lastGroupIndex) == m_cacheManager.GetTotalLayoutGroupCount() - 1 &&
				spHeader is not null)
			{
				int itemsInGroup = 0;
				Rect bounds = default;
				m_cacheManager.GetGroupInformationFromGroupIndex(lastGroupIndex, out _, out itemsInGroup);

				if (itemsInGroup > 0)
				{
					checkIfLastContainerIsRealized = true;
				}
				else
				{
					result = true;
				}

				bounds = GetBoundsFromElement(spHeader);
				localPanelExtent = PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds);
			}
		}

		if (checkIfLastContainerIsRealized && m_containerManager.GetValidContainerCount() > 0)
		{
			UIElement? spContainer;
			Rect bounds = default;

			int lastItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);
			spContainer = m_containerManager.GetContainerAtValidIndex(m_containerManager.GetValidContainerCount() - 1);

			if (lastItemIndex == m_cacheManager.GetTotalItemCount() - 1 &&
				spContainer is not null)
			{
				result = true;
			}

			bounds = GetBoundsFromElement(spContainer!);
			localPanelExtent = Math.Max(localPanelExtent, PointFromRectInVirtualizingDirection(bounds) + SizeFromRectInVirtualizingDirection(bounds));
		}

		if (result)
		{
			panelExtent = localPanelExtent;
		}
		else
		{
			panelExtent = 0.0f;
		}
	}

	// Estimate the extent of our panel
	// Since the last realized header and container represent the extent realized so far, we can
	// get more accurate results by estimating the remaining unrealized extent and tacking it on to the end of the realized extent
	private Size EstimatePanelExtent()
	{
		Rect windowConstraint = m_windowState.GetRealizationWindow();

		Size extent = default;
		if (m_cacheManager.IsGrouping())
		{
			// Get the last header and/or container, then ask the layout strategy to estimate the remaining unrealized extent
			if (m_containerManager.GetValidHeaderCount() > 0)
			{
				UIElement? spChild;
				EstimationReference headerEstimationRef = CreateDefaultEstimationReference(ElementType.GroupHeader);

				spChild = m_containerManager.GetHeaderAtValidIndex(m_containerManager.GetValidHeaderCount() - 1);

				int lastGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);

				int firstItemIndexInLastGroup;
				int itemsInLastGroup;
				m_cacheManager.GetGroupInformationFromGroupIndex(lastGroupIndex, out firstItemIndexInLastGroup, out itemsInLastGroup);

				headerEstimationRef.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, lastGroupIndex);
				headerEstimationRef.ElementBounds = GetBoundsFromElement(spChild!);

				if (m_containerManager.GetValidContainerCount() > 0)
				{
					EstimationReference containerEstimationRef = CreateDefaultEstimationReference(ElementType.ItemContainer);

					spChild = m_containerManager.GetContainerAtValidIndex(m_containerManager.GetValidContainerCount() - 1);
					int lastItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);

					containerEstimationRef.ElementIndex = lastItemIndex;
					containerEstimationRef.ElementBounds = GetBoundsFromElement(spChild!);

					extent = m_spLayoutStrategy.EstimatePanelExtent(
						headerEstimationRef,
						containerEstimationRef,
						windowConstraint);
				}
				else
				{
					extent = m_spLayoutStrategy.EstimatePanelExtent(
						headerEstimationRef,
						CreateDefaultEstimationReference(ElementType.ItemContainer),
						windowConstraint);
				}
			}
			else
			{
				// If there are any groups in the collection we should have generated at least one header
				global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalLayoutGroupCount() == 0);
				extent.Width = 0;
				extent.Height = 0;
			}
		}
		else
		{
			// No headers, so just work with the last container
			if (m_containerManager.GetValidContainerCount() > 0)
			{
				UIElement? spChild;

				spChild = m_containerManager.GetContainerAtValidIndex(m_containerManager.GetValidContainerCount() - 1);
				int lastContainerItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);

				EstimationReference containerEstimationRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
				containerEstimationRef.ElementIndex = lastContainerItemIndex;
				containerEstimationRef.ElementBounds = GetBoundsFromElement(spChild!);

				extent = m_spLayoutStrategy.EstimatePanelExtent(
					CreateDefaultEstimationReference(ElementType.GroupHeader),
					containerEstimationRef,
					windowConstraint);
			}
			else
			{
				// If there are any items in the collection, we should have generated at least one container
				global::System.Diagnostics.Debug.Assert(m_cacheManager.GetTotalItemCount() == 0);
				extent.Width = 0;
				extent.Height = 0;
			}
		}

		m_elementCountAtLastMeasure = m_cacheManager.IsGrouping() ? m_cacheManager.GetTotalLayoutGroupCount() : m_cacheManager.GetTotalItemCount();
		return extent;
	}

	// Estimates are inevitably wrong, so... when they are wrong, we need to adjust our containers' placement
	// on the panel so users can scroll exactly to the first/last elements, no more, no less. So, when we start
	// getting close to the edge, we need to see if we are wrong and perform the shift.
	private void CorrectForEstimationErrors(bool adjustWindow = true)
	{
		float neededCorrection = 0.0f;

		if (m_cacheManager.IsGrouping() && m_containerManager.GetValidHeaderCount() > 0)
		{
			UIElement? spElement;
			spElement = m_containerManager.GetHeaderAtValidIndex(0);
			Rect firstHeaderRect = GetBoundsFromElement(spElement!);
			int realizedGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(0);
			int realizedLayoutGroupIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, realizedGroupIndex);

			// Let's see who our first header is, and where its bounds are.
			// We only need to correct if realized the first header or if we realized before the
			// expected location of the first element.
			if (realizedLayoutGroupIndex == 0)
			{
				neededCorrection = -(PointFromRectInVirtualizingDirection(firstHeaderRect));
			}
			else
			{
				Point positionOfFirstElement;
				positionOfFirstElement = m_spLayoutStrategy.GetPositionOfFirstElement();
				if (PointFromRectInVirtualizingDirection(firstHeaderRect) < PointFromPointInVirtualizingDirection(positionOfFirstElement) ||
					m_windowState.m_estimationCorrectionPending)
				{
					EstimationReference headerReference = CreateDefaultEstimationReference(ElementType.GroupHeader);
					headerReference.ElementIndex = realizedLayoutGroupIndex;
					headerReference.ElementBounds = firstHeaderRect;

					EstimationReference containerReference = CreateDefaultEstimationReference(ElementType.ItemContainer);
					if (m_containerManager.GetValidContainerCount() > 0)
					{
						spElement = m_containerManager.GetContainerAtValidIndex(0);
						containerReference.ElementIndex = m_containerManager.GetItemIndexFromValidIndex(0);
						containerReference.ElementBounds = GetBoundsFromElement(spElement!);
					}

					Rect estimatedBoundsOfFirstUnrealizedHeader;
					// Ask the strategy how far we are off
					estimatedBoundsOfFirstUnrealizedHeader = m_spLayoutStrategy.EstimateElementBounds(
						ElementType.GroupHeader,
						0 /* elementIndex */,
						headerReference,
						containerReference,
						m_windowState.GetRealizationWindow());

					neededCorrection = Math.Max(
						-(PointFromRectInVirtualizingDirection(firstHeaderRect)),
						-(PointFromRectInVirtualizingDirection(estimatedBoundsOfFirstUnrealizedHeader)));

					m_windowState.m_estimationCorrectionPending = false;
				}
			}
		}
		else if (m_containerManager.GetValidContainerCount() > 0)
		{
			// This shouldn't be possible if we're grouping
			global::System.Diagnostics.Debug.Assert(!m_cacheManager.IsGrouping());

			// We only have containers, so find out where the first one is
			UIElement? spContainer;
			int realizedItemIndex = m_containerManager.GetItemIndexFromValidIndex(0);

			spContainer = m_containerManager.GetContainerAtItemIndex(realizedItemIndex);
			Rect firstContainerRect = GetBoundsFromElement(spContainer!);

			if (realizedItemIndex == 0)
			{
				neededCorrection = -(PointFromRectInVirtualizingDirection(firstContainerRect));
			}
			else
			{
				Point positionOfFirstElement;
				positionOfFirstElement = m_spLayoutStrategy.GetPositionOfFirstElement();

				if (PointFromRectInVirtualizingDirection(firstContainerRect) < PointFromPointInVirtualizingDirection(positionOfFirstElement)
					|| m_windowState.m_estimationCorrectionPending)
				{
					EstimationReference headerReference = CreateDefaultEstimationReference(ElementType.GroupHeader);
					EstimationReference containerReference = CreateDefaultEstimationReference(ElementType.ItemContainer);
					containerReference.ElementIndex = realizedItemIndex;
					containerReference.ElementBounds = firstContainerRect;

					Rect estimatedBoundsOfFirstUnrealizedContainer;
					// Ask the strategy how far we are off
					estimatedBoundsOfFirstUnrealizedContainer = m_spLayoutStrategy.EstimateElementBounds(
						ElementType.ItemContainer,
						0 /* elementIndex */,
						headerReference,
						containerReference,
						m_windowState.GetRealizationWindow());

					neededCorrection = Math.Max(
						-(PointFromRectInVirtualizingDirection(firstContainerRect)),
						-(PointFromRectInVirtualizingDirection(estimatedBoundsOfFirstUnrealizedContainer)));

					m_windowState.m_estimationCorrectionPending = false;
				}
			}
		}

		// If the needed correction is non-zero, run through all our elements and apply the correction to their bounds
		if (!DoubleUtil.IsZero(neededCorrection))
		{
			Point neededCorrectionPoint = default;
			SetPointFromPointInVirtualizingDirection(ref neededCorrectionPoint, neededCorrection);

			for (int validHeaderIndex = 0; validHeaderIndex < m_containerManager.GetValidHeaderCount(); ++validHeaderIndex)
			{
				UIElement? spHeader;
				spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
				if (spHeader is not null)
				{
					VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spHeader)!;
					Rect bounds = pVirtualizationInfo.Bounds;
					bounds.X += neededCorrectionPoint.X;
					bounds.Y += neededCorrectionPoint.Y;
					pVirtualizationInfo.Bounds = bounds;
				}
			}
			for (int validContainerIndex = 0; validContainerIndex < m_containerManager.GetValidContainerCount(); ++validContainerIndex)
			{
				UIElement? spContainer;
				spContainer = m_containerManager.GetContainerAtValidIndex(validContainerIndex);
				if (spContainer is not null)
				{
					VirtualizationInformation pVirtualizationInfo = GetVirtualizationInformationFromElement(spContainer)!;
					Rect bounds = pVirtualizationInfo.Bounds;
					bounds.X += neededCorrectionPoint.X;
					bounds.Y += neededCorrectionPoint.Y;
					pVirtualizationInfo.Bounds = bounds;
				}
			}

			// Apply this adjustment to the tracked bounds as well
			if (m_viewportBehavior.isTracking)
			{
				m_viewportBehavior.elementBounds.X += neededCorrectionPoint.X;
				m_viewportBehavior.elementBounds.Y += neededCorrectionPoint.Y;
			}

			if (adjustWindow)
			{
				if (m_windowState.allowWindowShiftOnEstimationErrorCorrection)
				{
					// Adjust our viewing rect
					m_windowState.ApplyAdjustment(neededCorrectionPoint);
				}
				else if (Math.Abs(neededCorrectionPoint.X) > 1 || Math.Abs(neededCorrectionPoint.Y) > 1)
				{
					// We've shuffled items to the side, but are not moving our window. This could leave a gap.
					// So, to eliminate it, we'll run another pass to make sure we continue generating to the edge
					if (m_windowState.m_command is null)
					{
						// Just create a no-op command that returns the currently visible window
						WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
						{
							pNewVisibleWindow = this.m_windowState.GetVisibleWindow();
							m_windowState.allowWindowShiftOnEstimationErrorCorrection = false;
						};
						m_windowState.m_command = func;
					}
				}
			}
		}

		m_windowState.allowWindowShiftOnEstimationErrorCorrection = false;
	}

	// Scrolls our ScrollViewer to the given area (synchronous).
	private bool SetScrollViewerOffsetsTo(Rect targetRect, bool animate)
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		bool issued = false;

		if (spScrollViewer is not null)
		{
			bool handled = false;
			float horizontalOffset = (float)targetRect.X;
			float verticalOffset = (float)targetRect.Y;

			double? spHorizontalOffsetReference = null;
			double? spVerticalOffsetReference = null;
			Orientation orientation;

			orientation = m_spLayoutStrategy.VirtualizationDirection;

			// Account for our offset from the ItemsPresenter (i.e. listview.header)
			horizontalOffset += (float)m_originFromItemsPresenter.X;
			verticalOffset += (float)m_originFromItemsPresenter.Y;

			// Adjust for zoom factor
			// We do this after adjust for the layout slot, because the layout slot (list.header) is in our non-zoomed coordinates
			// but the ScrollViewer deals with zoomed coordinates

			// Until XAML converges on whether or not to reflow and remeasure based on zoom factors,
			// I'm going to ignore the zoom factor in the non-virtualizing direction.
			// All virtualization code will see the original non-zoomed window size in the regard
			switch (orientation)
			{
				case Orientation.Horizontal:
					spHorizontalOffsetReference = horizontalOffset * m_windowState.m_lastZoomFactor;
					break;

				case Orientation.Vertical:
					spVerticalOffsetReference = verticalOffset * m_windowState.m_lastZoomFactor;
					break;
			}

			handled = spScrollViewer.ChangeViewWithOptionalAnimation(
				spHorizontalOffsetReference,
				spVerticalOffsetReference,
				null  /*pZoomFactor*/,
				!animate /*disableAnimation*/);

			issued = true;
		}

		return issued;
	}

	private protected void ProcessOrientationChange()
	{
		IGeneratorHost? spIHost = m_cacheManager.GetItemsHost();

		// if the orientation changes but we are not even hooked up yet, we can fully ignore
		// it, since it will have no effect (measure will just automatically do the correct thing)
		if (spIHost is not null)
		{
			// when orientation is changed, we expect the available size to change as well, so
			// at the beginning of measure we will invalidate the validWindowCalculation probably
			m_windowState.validWindowCalculation = false;

			// remove any commands, because they are not going to make sense anymore
			m_windowState.m_command = null;

			// we want to keep the current item in the same location
			// but only at the next measure will we have a correct available size
			// so we need to create a command and defer this just like a regular ScrollItemIntoView
			{
				WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
				{
					pNewVisibleWindow = DoProcessOrientation();
				};

				m_windowState.m_command = func;
			}
		}

		// there is magic happening in the layout where some components will change
		// dimensions to infinite. But since only we are dirty, in the first pass of layout
		// we will not get that code executed properly, ending up with infinity/infinity.
		// which is a terrible situation to be in for a virtualizing panel (it will de-virtualize everything).
		// Ultimately this will fix itself because the desiredsize of this panel will change which will change
		// the desiredsize of the itemspresenter, which will invalidate the scrollcontentpresenter.

		// The solution is to simply be proactive and immediately invalidate the scrollcontentpresenter so that we
		// are assured of a great measure with valid values next time around.


		if (spIHost is not null)
		{
			DependencyObject? spHostAsDO = spIHost as DependencyObject;
			DependencyObject? spCurrentAsDO = this;

			while (spCurrentAsDO is not null)
			{
				DependencyObject? spParentAsDO = VisualTreeHelper.GetParent(spCurrentAsDO);

				if (spParentAsDO is not null)
				{
					// anything in the tree is going to be a uielement.

					((UIElement)spParentAsDO).InvalidateMeasure();

					// is this our itemscontrol?
					if (ReferenceEquals(spParentAsDO, spHostAsDO))
					{
						break;
					}
				}
				spCurrentAsDO = spParentAsDO;
			}
		}

		InvalidateMeasure();
	}

	// Triggers a synchronous layout if a command exists.
	private void FlushWindowStateCommand()
	{
		if (m_windowState.m_command is not null)
		{
			InvalidateMeasure();
			UpdateLayout();
		}
	}

	internal void ScrollRectIntoView(
		Rect targetRect,
		bool forceSynchronous)
	{
		// If a command already exists, flush it and force a synchronous layout.
		FlushWindowStateCommand();

		{
			// This is a pretty simple command, so we'll just throw it in a lambda.
			// If this starts getting complicated, spin it off into a method
			WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
			{
				// Place the window around the rect
				pNewVisibleWindow = targetRect;

				// Cancels tracking if we were tracking.
				m_viewportBehavior.Reset();

				// this move can bring us into an edge that needs correction
				m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
			};
			m_windowState.m_command = func;
		}

		InvalidateMeasure();
		if (forceSynchronous)
		{
			UpdateLayout();
		}
	}

	internal void ScrollItemIntoView(
		uint index,
		ScrollIntoViewAlignment alignment,
		double offset,
		bool forceSynchronous,
		bool animate = false)
	{
		// If a command already exists, flush it and force a synchronous layout.
		FlushWindowStateCommand();

		if (animate && m_containerManager.IsItemConnected(index))
		{
			global::System.Diagnostics.Debug.Assert(m_windowState.m_command is null);
			global::System.Diagnostics.Debug.Assert(offset == 0);
			DoScrollItemIntoView((int)index, alignment, 0 /*offset*/, true /*animate*/, out _ /*pComputedWindow*/);
		}
		else
		{
			{
				// We can't correctly prepare a new container outside of the visual tree.
				// The template won't get applied and we end up raising CCC with a null
				// args.ItemContainer.ContentTemplateRoot

				if (IsInLiveTree &&
					IsItemsHostRegistered() &&
					!m_containerManager.HasFocusedContainer() &&
					!m_layoutInProgress)
				{
					// if nothing is focused, and index is connected, set that as focused container
					// if not connected, create a container.
					// we also do not want to do this during layout because we could end up preparing containers while
					// layout is in the middle of preparing containers.
					UIElement? spContainer;
					if (m_containerManager.IsItemConnected(index))
					{
						spContainer = m_containerManager.GetContainerAtItemIndex((int)index);
					}
					else
					{
						Size measureSize;

						using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of method

						// try on both sides
						EnsureRecycleContainerCandidate((int)index, false /*fromFront*/);
						EnsureRecycleContainerCandidate((int)index, true /*fromFront*/);
						if (m_containerManager.HasFocusedContainer())
						{
							// EnsureRecycleContainerCandidate could set the stored focused
							// container based on who actually has focus. We don't care what
							// currently has focus - we want to set focus on the scrolled item.
							ReleaseStoredFocusedContainer();
						}

						spContainer = m_icg2.GenerateContainerAtIndex((int)index);
						m_containerManager.PlaceInGarbageRegionIfNotInChildren(spContainer!);

						measureSize = m_spLayoutStrategy.GetElementMeasureSize(
							ElementType.ItemContainer,
							(int)index,
							m_windowState.GetRealizationWindow());
						PrepareContainerViaItemsHost((int)index, measureSize, spContainer!);
					}

					m_containerManager.SetFocusedContainer((int)index, spContainer!);
					m_containerManager.m_wasLastScrollIntoViewForHeader = false;
				}

				if (m_containerManager.m_isScrollIntoViewInProgress)
				{
					// We got a ScrollIntoView while we are processing a ScrollIntoView.
					// This is not a scenario we support, let's leave a trace to help
					// us debug apps that do this.
					TraceGuardFailure("ScrollItemIntoViewReentrancy");
				}

				// We need to drop a command breadcrumb and run the virtualization pass to create the element
				WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
				{
					m_containerManager.m_isScrollIntoViewInProgress = true;
					DoScrollItemIntoView((int)index, alignment, offset, false /*animate*/, out pNewVisibleWindow);
					m_containerManager.m_isScrollIntoViewPending = false;
					// Verify that it is still valid to keep the stored focused container.
					// It can be cleared if the container is in the valid range or it is still focused
					VerifyStoredFocusedContainer();
					m_containerManager.m_isScrollIntoViewInProgress = false;
				};

				m_windowState.m_command = func;
				m_containerManager.m_isScrollIntoViewPending = true;
			}

			InvalidateMeasure();
			if (forceSynchronous)
			{
				UpdateLayout();
			}
		}
	}

	// Provide a focus candidate. If a scroll into view happens, the panel will
	// keep that as a focus candidate. The listview can ask for this candidate
	// when it needs to focus.
	internal void GetFocusCandidate(
		out int index,
		out int groupIndex,
		out UIElement? container)
	{
		index = -1;
		groupIndex = -1;
		container = null;

		if (m_containerManager.m_wasLastScrollIntoViewForHeader)
		{
			m_containerManager.GetFocusedHeaderContainer(out groupIndex, out container);
		}
		else
		{
			m_containerManager.GetFocusedContainer(out index, out container);
		}
	}

	internal void ScrollGroupHeaderIntoView(
		uint index,
		ScrollIntoViewAlignment alignment,
		double offset,
		bool forceSynchronous,
		bool animate = false,
		uint neighboringItemIndex = 0) // Valid when 'animate' is True
	{
		// If a command already exists, flush it and force a synchronous layout.
		FlushWindowStateCommand();

		// Animations are only supported when moving to a realized group header and all items in between are also realized.
		if (animate && m_containerManager.IsGroupHeaderConnected(index, neighboringItemIndex))
		{
			global::System.Diagnostics.Debug.Assert(m_windowState.m_command is null);
			global::System.Diagnostics.Debug.Assert(offset == 0);
			DoScrollGroupHeaderIntoView((int)index, alignment, 0 /*offset*/, true /*animate*/, out _ /*pComputedWindow*/);
		}
		else
		{
			{
				// We can't correctly prepare a new container outside of the visual tree.
				// The template won't get applied and we end up raising CCC with a null
				// args.ItemContainer.ContentTemplateRoot

				if (IsInLiveTree &&
					IsItemsHostRegistered() &&
					!m_containerManager.HasFocusedContainer() &&
					!m_layoutInProgress)
				{
					// Releases when it goes out of scope at the end of method
					using var strongCache = m_cacheManager.CacheStrongRefs();

					// if nothing is focused, and index is connected, set that as focused container
					// if not connected, create a container
					UIElement? spContainer;
					index = (uint)m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, (int)index);
					index = (uint)m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, (int)index);
					if (m_containerManager.IsGroupHeaderRealized(index))
					{
						spContainer = m_containerManager.GetHeaderAtGroupIndex((int)index);
					}
					else
					{
						spContainer = m_icg2.GenerateHeaderAtGroupIndex((int)index);
						m_containerManager.PlaceInGarbageRegionIfNotInChildren(spContainer!);
					}

					m_containerManager.SetFocusedHeaderContainer((int)index, spContainer!);
					m_containerManager.m_wasLastScrollIntoViewForHeader = true;
				}

				if (m_containerManager.m_isScrollIntoViewInProgress)
				{
					// We got a ScrollIntoView while we are processing a ScrollIntoView.
					// This is not a scenario we support, let's leave a trace to help
					// us debug apps that do this.
					TraceGuardFailure("ScrollGroupHeaderIntoViewReentrancy");
				}

				// We need to drop a command breadcrumb and run the virtualization pass to create the element
				WindowState.WindowCommand func = (ref Rect pNewVisibleWindow) =>
				{
					m_containerManager.m_isScrollIntoViewInProgress = true;
					DoScrollGroupHeaderIntoView((int)index, alignment, offset, false /*animate*/, out pNewVisibleWindow);
					// Reset the stored focused header once it is in view.
					m_containerManager.ResetFocusedHeaderContainer(true);
					m_containerManager.m_isScrollIntoViewInProgress = false;
				};

				m_windowState.m_command = func;
			}

			InvalidateMeasure();
			if (forceSynchronous)
			{
				UpdateLayout();
			}
		}
	}

	// Determine the window to show, based on either the user scrolling, or responding to a specific request
	// such as key navigation or ScrollIntoView, and then generate the appropriate anchors for the new view
	private void DetermineWindowAndAnchors()
	{
		Rect newVisibleWindow = new Rect(-1.0f, -1.0f, -1.0f, -1.0f);
		if (m_windowState.m_command is not null)
		{
			// Copy over the command on the stack in case it gets
			// overwritten during the call. This may prevent proper
			// execution of the rest of the original command.
			var command = m_windowState.m_command;
			m_windowState.m_command = null;
			// If we have a pending window command, let's execute it
			command(ref newVisibleWindow);
		}
		else
		{
			BeginTrackingOnRefresh(ref newVisibleWindow);
		}

		if (!RectUtil.GetIsEmpty(newVisibleWindow))
		{
			m_windowState.SetCoercedVisibleWindow(newVisibleWindow);

			// We should let ItemsPresenter know about this new window in case we
			// need to load the footer.
			ItemsPresenter? spItemsPresenter = null;
			m_wrItemsPresenter?.TryGetTarget(out spItemsPresenter);
			ScrollViewer? spScrollViewer = null;
			m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

			if (spItemsPresenter is not null && spScrollViewer is not null)
			{
				double viewportWidth = 0.0;
				double viewportHeight = 0.0;

				viewportWidth = spScrollViewer.ViewportWidth;
				viewportHeight = spScrollViewer.ViewportHeight;

				Rect futureViewportRect = new Rect(
						/* X = */ (newVisibleWindow.X + m_originFromItemsPresenter.X) * m_windowState.m_lastZoomFactor,
						/* Y = */ (newVisibleWindow.Y + m_originFromItemsPresenter.Y) * m_windowState.m_lastZoomFactor,
						/* Width = */ (float)viewportWidth,
						/* Height = */ (float)viewportHeight);

				spItemsPresenter.DelayLoadFooter(futureViewportRect, false /* updateLayout */);
			}
		}

		SetRealizationWindowFromVisibleWindow(true);
		DetectAndHandleDisconnectedView();
	}

	private Rect DoProcessOrientation()
	{
		Rect pComputedWindow = BeginTrackingOnOrientationChange();

		// also, since we are not 'tracking' the first visible container, we should reset the cache
		ResetCacheBuffers();

		return pComputedWindow;
	}

	// This method actually performs the heavy lifting of ScrollItemIntoView.
	private void DoScrollItemIntoView(
		int itemIndex,
		ScrollIntoViewAlignment alignment,
		double offset,
		bool animate,
		out Rect computedWindow)
	{
		UIElement? spContainer;
		Rect containerBounds = default;
		Rect windowToFill = m_windowState.GetVisibleWindow();
		Size infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);
		Rect pComputedWindow = RectUtil.CreateEmptyRect();
		Rect headerBounds = default; // needed for sticky headers
		bool needReEstimation = false;

		// there could have been a change in the collection since the original scrollitemintoview command
		// was issued. Let's see if itemindex is still valid
		if (itemIndex < 0 || itemIndex >= m_cacheManager.GetTotalItemCount())
		{
			// When an animation is requested, the target item is expected to be connected so this branch should not be entered.
			global::System.Diagnostics.Debug.Assert(!animate);

			// collection has changed, this command is no longer executable
			// we don't know how to calculate a window, so an empty rect will keep the current window unchanged
			goto Cleanup;
		}

		// If the item is already in the valid collection, just adjust its position
		if (m_containerManager.IsItemConnected((uint)itemIndex))
		{
			spContainer = m_containerManager.GetContainerAtItemIndex(itemIndex);

			double deltaY = 0.0;

			// This item's container is already generated, so let's just make sure it is visible at the desired alignment
			containerBounds = GetBoundsFromElement(spContainer!);

			// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
			if (m_bUseStickyHeaders)
			{
				// with sticky headers, we need more: the item has to be below its group's header too
				int indexOfGroup = 0;
				UIElement? spHeader;
				m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out indexOfGroup, out _, out _);
				int validIndex = m_containerManager.GetValidHeaderIndexFromGroupIndex(indexOfGroup);
				spHeader = m_containerManager.GetHeaderAtValidIndex(validIndex);
				headerBounds = GetBoundsFromElement(spHeader!);
				deltaY = headerBounds.Height;
			}

			// If the container is completely inside the screen, and they request default alignment, we're done!
			if (alignment == ScrollIntoViewAlignment.Default &&
				m_windowState.GetVisibleWindow().X <= containerBounds.X &&
				containerBounds.X + containerBounds.Width <= m_windowState.GetVisibleWindow().X + m_windowState.GetVisibleWindow().Width &&
				(m_windowState.GetVisibleWindow().Y + deltaY) <= containerBounds.Y &&
				containerBounds.Y + containerBounds.Height <= m_windowState.GetVisibleWindow().Y + m_windowState.GetVisibleWindow().Height)
			{
				// Nothing to do here, apparently.
				goto Cleanup;
			}

			if (!animate)
			{
				// Since the window may change, reset the cache buffers to only show what's going to be visible first.
				// The cache will rebuild asynchronously around the new visible window.
				ResetCacheBuffers();
			}
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(!animate);
			global::System.Diagnostics.Debug.Assert(0 <= itemIndex && itemIndex < m_cacheManager.GetTotalItemCount());

			int anchorItemIndexInGroup = 0;
			if (m_cacheManager.IsGrouping())
			{
				// See if our target group is already created.
				// If so, we'll just use its info as our reference data
				UIElement? spGroupAnchor;

				int targetGroupIndex;
				int itemCountInTargetGroup;
				m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out targetGroupIndex, out anchorItemIndexInGroup, out itemCountInTargetGroup);

				EstimationReference headerEstimationInfo = CreateDefaultEstimationReference(ElementType.GroupHeader);
				if (m_containerManager.GetValidHeaderCount() > 0)
				{
					int lowGroupBound = m_containerManager.GetGroupIndexFromValidIndex(0);
					int highGroupBound = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);
					if (targetGroupIndex < lowGroupBound)
					{
						headerEstimationInfo.ElementIndex = lowGroupBound;
					}
					else if (targetGroupIndex > highGroupBound)
					{
						headerEstimationInfo.ElementIndex = highGroupBound;
					}
					else
					{
						headerEstimationInfo.ElementIndex = targetGroupIndex;
					}

					UIElement? spReferenceHeader;
					spReferenceHeader = m_containerManager.GetHeaderAtGroupIndex(headerEstimationInfo.ElementIndex);

					// If our reference header turned out to be a sentinel, give up and use the first generated header
					if (spReferenceHeader is null)
					{
						headerEstimationInfo.ElementIndex = lowGroupBound;
						spReferenceHeader = m_containerManager.GetHeaderAtGroupIndex(lowGroupBound);
					}

					headerEstimationInfo.ElementBounds = GetBoundsFromElement(spReferenceHeader!);
				}
				else
				{
					headerEstimationInfo.ElementIndex = 0;
					headerEstimationInfo.ElementBounds = default;

					// We re-estimate if there are no valid containers. Even if there is one valid
					// container we will not re-estimate. If we decide to be more agressive we can move this
					// outside the else clause.
					needReEstimation = true;
				}

				// Now we're ready to recycle and bring in the relevant group
				RecycleAllContainersAndHeaders();

				GenerateAnchorForGroup(targetGroupIndex, headerEstimationInfo, null);

				// Now, get that group anchor, and use it to estimate the location of the item
				spGroupAnchor = m_containerManager.GetHeaderAtGroupIndex(targetGroupIndex);

				// Measure it so we get the correct alignment
				{
					Size desiredSize;
					LayoutReference refInfo = CreateDefaultLayoutReference();
					refInfo.ReferenceIsHeader = true;
					refInfo.RelativeLocation = ReferenceIdentity.Myself;
					refInfo.ReferenceBounds = GetBoundsFromElement(spGroupAnchor!);

					spGroupAnchor!.Measure(infiniteBounds);
					desiredSize = spGroupAnchor.DesiredSize;
					headerBounds = m_spLayoutStrategy.GetElementBounds(
						ElementType.GroupHeader,
						m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, targetGroupIndex),
						desiredSize,
						refInfo,
						windowToFill);

					SetBoundsForElement(spGroupAnchor, headerBounds);
				}

				headerEstimationInfo.ElementIndex = targetGroupIndex;
				headerEstimationInfo.ElementBounds = GetBoundsFromElement(spGroupAnchor);

				GenerateAnchorForItem(itemIndex, headerEstimationInfo, null);
			}
			else // not grouping
			{
				anchorItemIndexInGroup = itemIndex;
				EstimationReference containerInfo = CreateDefaultEstimationReference(ElementType.ItemContainer);
				if (m_containerManager.GetValidContainerCount() > 0)
				{
					int firstItemIndex = m_containerManager.GetItemIndexFromValidIndex(0);
					int lastItemIndex = m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1);
					int referenceItemIndex;
					if (itemIndex < firstItemIndex)
					{
						referenceItemIndex = firstItemIndex;
					}
					else if (lastItemIndex < itemIndex)
					{
						referenceItemIndex = lastItemIndex;
					}
					else
					{
						// The item we're trying to scroll must have been a sentinel.
						// Rather than give up and hunt all over for a non-sentinel, just bail and use the the first realized item as the reference
						referenceItemIndex = firstItemIndex;
					}
					UIElement? spReferenceContainer;
					spReferenceContainer = m_containerManager.GetContainerAtItemIndex(referenceItemIndex);

					containerInfo.ElementIndex = referenceItemIndex;
					containerInfo.ElementBounds = GetBoundsFromElement(spReferenceContainer!);
				}
				else
				{
					containerInfo.ElementIndex = 0;
					containerInfo.ElementBounds = default;

					// We re-estimate if there are no valid containers. Even if there is one valid
					// container we will not re-estimate. If we decide to be more agressive we can move this
					// outside the else clause.
					needReEstimation = true;
				}

				RecycleAllContainersAndHeaders();

				GenerateAnchorForItem(itemIndex, null, containerInfo);
			}

			global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(itemIndex)));

			spContainer = m_containerManager.GetContainerAtItemIndex(itemIndex);
			containerBounds = GetBoundsFromElement(spContainer!);

			// Let's measure it to ensure proper alignment
			{
				Size desiredSize;
				LayoutReference refInfo = CreateDefaultLayoutReference();
				refInfo.ReferenceIsHeader = false;
				refInfo.RelativeLocation = ReferenceIdentity.Myself;
				refInfo.ReferenceBounds = containerBounds;

				spContainer!.Measure(infiniteBounds);
				desiredSize = spContainer.DesiredSize;
				containerBounds = m_spLayoutStrategy.GetElementBounds(
					ElementType.ItemContainer,
					itemIndex,
					desiredSize,
					refInfo,
					windowToFill);

				SetBoundsForElement(spContainer, containerBounds);
			}

			// We performed a scrollinto view that could cause the estimation to be incorrect.
			// For example, we could have done a scroll into view right after setting the itemsource,
			// in which case we would have realized just the first item and then based that estimate
			// to estimate the anchor. In this case just using the first item size for estimation leads
			// to a pretty bad estimate. Setting this flag will cause us to re-evaluate the estimates
			// and correct in the next measure after we have generated more items - which means we have
			// seen more containers and hence have a better estimate. See bug 1405633 for more details
			// on this scenario.
			m_windowState.m_estimationCorrectionPending = needReEstimation;
		}

		// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
		if (m_bUseStickyHeaders)
		{
			// Enlarge the window to include the header's height
			containerBounds.Y -= headerBounds.Height;
			containerBounds.Height += headerBounds.Height;
		}

		// Place the window around the anchor
		pComputedWindow = PlaceWindowAroundElementRect(m_windowState.GetVisibleWindow(), containerBounds, alignment, offset);

		if (animate)
		{
			// Issue the animation request.
			bool issued = false;
			issued = SetScrollViewerOffsetsTo(pComputedWindow, true /*animate*/);
		}
		else
		{
			// this move can bring us into an edge that needs correction
			m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
		}

		// Cancels tracking if we were tracking.
		m_viewportBehavior.Reset();

	Cleanup:
		computedWindow = pComputedWindow;
	}

	// This method actually performs the heavy lifting of ScrollGroupHeaderIntoView.
	private void DoScrollGroupHeaderIntoView(
		int groupIndex,
		ScrollIntoViewAlignment alignment,
		double offset,
		bool animate,
		out Rect computedWindow)
	{
		UIElement? spHeader = null;
		Rect headerBounds = default;
		Rect windowToFill = m_windowState.GetVisibleWindow();
		Size infiniteBounds = new Size(double.PositiveInfinity, double.PositiveInfinity);
		bool headerConnected = false;
		int referenceGroupIndex = -1;
		bool needReEstimation = false;

		Rect pComputedWindow = RectUtil.CreateEmptyRect();

		// deal with scrolling to an empty group
		groupIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, groupIndex);
		groupIndex = m_cacheManager.LayoutIndexToDataIndex(ElementType.GroupHeader, groupIndex);

		// there could have been a change in the collection since the original scrollgroupintoview command
		// was issued. Let's see if groupindex is still valid
		if (groupIndex < 0 || groupIndex >= m_cacheManager.GetTotalGroupCount())
		{
			// When an animation is requested, the target group is expected to be connected so this branch should not be entered.
			global::System.Diagnostics.Debug.Assert(!animate);

			// collection has changed, this command is no longer executable
			// we don't know how to calculate a window, so an empty rect will keep the current window unchanged
			goto Cleanup;
		}

		// Find out if we've already realized this header, and if not, who the closest header is
		if (m_containerManager.GetValidHeaderCount() > 0)
		{
			int firstValidGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(0);
			int lastValidGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);

			if (groupIndex < firstValidGroupIndex)
			{
				headerConnected = false;
				referenceGroupIndex = firstValidGroupIndex;
			}
			else if (lastValidGroupIndex < groupIndex)
			{
				headerConnected = false;
				referenceGroupIndex = lastValidGroupIndex;
			}
			if (firstValidGroupIndex <= groupIndex && groupIndex <= lastValidGroupIndex)
			{
				spHeader = m_containerManager.GetHeaderAtGroupIndex(groupIndex);
				if (spHeader is not null)
				{
					headerConnected = true;
					referenceGroupIndex = groupIndex;
				}
				else
				{
					headerConnected = false;
					referenceGroupIndex = firstValidGroupIndex;
				}
			}
		}
		else
		{
			headerConnected = false;
			referenceGroupIndex = 0;
		}

		// If the header is already in the valid collection, just adjust its position
		if (headerConnected)
		{
			// This group's header is already generated, so let's just make sure it is visible at the desired alignment
			headerBounds = GetBoundsFromElement(spHeader!);

			// If the header is completely inside the screen, and they request default alignment, we're done!
			if (alignment == ScrollIntoViewAlignment.Default &&
				m_windowState.GetVisibleWindow().X <= headerBounds.X &&
				headerBounds.X + headerBounds.Width <= m_windowState.GetVisibleWindow().X + m_windowState.GetVisibleWindow().Width &&
				m_windowState.GetVisibleWindow().Y <= headerBounds.Y &&
				headerBounds.Y + headerBounds.Height <= m_windowState.GetVisibleWindow().Y + m_windowState.GetVisibleWindow().Height)
			{
				// Nothing to do here, apparently.
				goto Cleanup;
			}

			if (!animate)
			{
				// Since the window may change, reset the cache buffers to only show what's going to be visible first.
				// The cache will rebuild asynchronously around the new visible window.
				ResetCacheBuffers();
			}
		}
		else
		{
			global::System.Diagnostics.Debug.Assert(!animate);
			// Get a reference to estimate from and generate an anchor
			global::System.Diagnostics.Debug.Assert(0 <= groupIndex && groupIndex < m_cacheManager.GetTotalGroupCount() || m_cacheManager.GetTotalGroupCount() == 0);
			global::System.Diagnostics.Debug.Assert(m_cacheManager.IsGrouping());

			EstimationReference headerInfo = CreateDefaultEstimationReference(ElementType.GroupHeader);
			if (m_containerManager.GetValidHeaderCount() > 0)
			{
				UIElement? spReferenceHeader;

				global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(referenceGroupIndex)));
				spReferenceHeader = m_containerManager.GetHeaderAtValidIndex(m_containerManager.GetValidHeaderIndexFromGroupIndex(referenceGroupIndex));

				headerInfo.ElementIndex = referenceGroupIndex;
				headerInfo.ElementBounds = GetBoundsFromElement(spReferenceHeader!);
			}
			else
			{
				headerInfo.ElementIndex = 0;
				headerInfo.ElementBounds = default;

				// we are disconnected and we need to estimate. In this case let's re-estimate
				// position in the panel once we have more items realized.
				needReEstimation = true;
			}

			RecycleAllContainersAndHeaders();

			GenerateAnchorForGroup(groupIndex, headerInfo, null);

			// Now that we've generated the anchor, let's grab it and measure it so we can accurately place the window
			global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(groupIndex)));

			spHeader = m_containerManager.GetHeaderAtGroupIndex(groupIndex);
			headerBounds = GetBoundsFromElement(spHeader!);

			{
				Size desiredSize;
				LayoutReference refInfo = CreateDefaultLayoutReference();
				refInfo.ReferenceIsHeader = true;
				refInfo.RelativeLocation = ReferenceIdentity.Myself;
				refInfo.ReferenceBounds = headerBounds;

				spHeader!.Measure(infiniteBounds);
				desiredSize = spHeader.DesiredSize;
				headerBounds = m_spLayoutStrategy.GetElementBounds(
					ElementType.GroupHeader,
					m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, groupIndex),
					desiredSize,
					refInfo,
					windowToFill);

				SetBoundsForElement(spHeader, headerBounds);
			}

			m_windowState.m_estimationCorrectionPending = needReEstimation;
		}

		// Place the window around the anchor
		pComputedWindow = PlaceWindowAroundElementRect(m_windowState.GetVisibleWindow(), headerBounds, alignment, offset);

		if (animate)
		{
			// Issue the animation request.
			bool issued = false;
			issued = SetScrollViewerOffsetsTo(pComputedWindow, true /*animate*/);
		}
		else
		{
			// this move can bring us into an edge that needs correction
			m_windowState.allowWindowShiftOnEstimationErrorCorrection = true;
		}

		// Cancels tracking if we were tracking.
		m_viewportBehavior.Reset();

	Cleanup:
		computedWindow = pComputedWindow;
	}

	// Helper method to place a window around an element's rect
	private Rect PlaceWindowAroundElementRect(
		Rect sourceWindowRect,
		Rect elementRectToAlignTo,
		ScrollIntoViewAlignment alignment,
		double offset)
	{
		Rect result = sourceWindowRect;

		Orientation orientation;
		orientation = m_spLayoutStrategy.VirtualizationDirection;

		switch (orientation)
		{
			case Orientation.Horizontal:
				switch (alignment)
				{
					case ScrollIntoViewAlignment.Leading:
						result.X = elementRectToAlignTo.X;
						break;

					case ScrollIntoViewAlignment.Default:
					default:
						// First see if the right edge needs to be brought in.
						// If not, or if this forces the left edge outside the window, bring it in
						if (sourceWindowRect.X + sourceWindowRect.Width < elementRectToAlignTo.X + elementRectToAlignTo.Width)
						{
							result.X = elementRectToAlignTo.X + elementRectToAlignTo.Width - sourceWindowRect.Width;
						}
						if (elementRectToAlignTo.X < result.X)
						{
							result.X = elementRectToAlignTo.X;
						}
						break;
				}

				result.X += (float)offset;
				break;

			case Orientation.Vertical:
				switch (alignment)
				{
					case ScrollIntoViewAlignment.Leading:
						result.Y = elementRectToAlignTo.Y;
						break;

					case ScrollIntoViewAlignment.Default:
					default:
						// First see if the bottom edge needs to be brought in.
						// If not, or if this forces the top edge outside the window, bring it in
						if (sourceWindowRect.Y + sourceWindowRect.Height < elementRectToAlignTo.Y + elementRectToAlignTo.Height)
						{
							result.Y = elementRectToAlignTo.Y + elementRectToAlignTo.Height - sourceWindowRect.Height;
						}
						if (elementRectToAlignTo.Y < result.Y)
						{
							result.Y = elementRectToAlignTo.Y;
						}
						break;
				}

				result.Y += (float)offset;
				break;
		}

		return result;
	}

	// Recycles alls containers (and headers if grouping) and removes them from the cache manager.
	private void RecycleAllContainersAndHeaders()
	{
		IGeneratorHost? spHost = m_cacheManager.GetItemsHost();

		// Ensure we protect focused and pinned elements
		for (int typeIndex = 0; typeIndex < ElementType_Count; ++typeIndex)
		{
			ElementType type = (ElementType)typeIndex;
			int validElementCount = m_containerManager.GetValidElementCount(type);

			for (int validIndex = 0; validIndex < validElementCount; ++validIndex)
			{
				int dataIndex = m_containerManager.GetDataIndexFromValidIndex(type, validIndex);
				UIElement? spElement;

				// If it's already pinned/focused, nothing to do
				if (!m_containerManager.GetIsIndexPinned(type, dataIndex) && !m_containerManager.GetIsIndexFocused(type, dataIndex))
				{
					spElement = m_containerManager.GetElementAtDataIndex(type, dataIndex);
					if (spElement is not null)
					{
						// See if this element needs to be stored as focused
						bool hasFocus = false;
						hasFocus = m_cacheManager.IsFocusedChild(spElement);

						if (hasFocus && typeIndex == (int)ElementType.ItemContainer)
						{
							if (m_containerManager.HasFocusedContainer())
							{
								// this item is focused, but there is a stored focused container. There
								// was a scroll into view which stored the focused container but that was not
								// focused, so dump the stored focused container.
								ReleaseStoredFocusedContainer();
							}

							m_containerManager.SetFocusedContainer(dataIndex, spElement);
						}
						else
						{
							bool canRecycle = true;
							canRecycle = spHost!.CanRecycleContainer(spElement);
							if (!canRecycle)
							{
								m_containerManager.RegisterPinnedElement(type, dataIndex, spElement);
							}
						}
					}
				}
			}
		}

		if (m_cacheManager.IsGrouping())
		{
			m_icg2.RecycleAllHeaders();
			m_containerManager.RemoveAllValidHeaders();
		}

		m_icg2.RecycleAllContainers();
		m_containerManager.RemoveAllValidContainers();

		ResetCacheBuffers();

		// reset all the caches that we know for sure are going to be invalid now
		m_firstCacheIndexBase = -1;
		m_firstVisibleIndexBase = -1;
		m_lastVisibleIndexBase = -1;
		m_lastCacheIndexBase = -1;
		m_firstCacheGroupIndexBase = -1;
		m_firstVisibleGroupIndexBase = -1;
		m_lastVisibleGroupIndexBase = -1;
		m_lastCacheGroupIndexBase = -1;
	}

	// determines whether we are in a position to increase the cache
	private bool CanIncreaseCacheLength()
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		bool shouldProcessCache = true;

		// 1. Make sure we are not panning
		if (shouldProcessCache && spScrollViewer is not null)
		{
			if (spScrollViewer.IsInManipulation)
			{
				shouldProcessCache = false;
			}
			else
			{
				bool barDragging = false;
				barDragging = spScrollViewer.IsThumbDragging;
				shouldProcessCache = !barDragging;
			}
		}

		// 2. Make sure we are not working on showing content
		if (shouldProcessCache)
		{
			IGeneratorHost? spHost;
			ITreeBuilder? spHostAsTreeBuilder;

			// Make sure queue of items to prepare in visible window is empty.
			spHost = m_cacheManager.GetItemsHost();

			// we do that by looking at our host, the listview, which is the one driving
			// content setting
			spHostAsTreeBuilder = spHost as ITreeBuilder;
			if (spHostAsTreeBuilder is not null)
			{
				bool registeredForCallbacks = false;
				registeredForCallbacks = spHostAsTreeBuilder.IsRegisteredForCallbacks;

				shouldProcessCache = !registeredForCallbacks;
			}
		}

		// 3. Make sure we are not bogged down
		if (shouldProcessCache)
		{
			BudgetManager spBudget;
			int timeElapsedInMS = 0;

			spBudget = DXamlCore.Current.GetBudgetManager();
			timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick();

			shouldProcessCache = timeElapsedInMS < PerformCacheInflationWhenTimeAvailable;
		}

		// 4. Go hardcore, we don't even want to process cache if there is _any_ input in the Message Queue!
#if !HAS_UNO // TODO Uno: no frame scheduler to ask about pending high priority frames.
		if (shouldProcessCache)
		{
			CCoreServices pCore = null;

			pCore = DXamlCore.GetCurrent().GetHandle();
			if (pCore)
			{
				ITickableFrameScheduler pFrameScheduler = pCore.GetBrowserHost().GetFrameScheduler();
				if (pFrameScheduler)
				{
					// building up cache is regarded as a task that we only do when idle.
					// if this is a high priority frame, it was requested by an invalidation outside of the
					// regular tick, presumably because input has occurred that has invalidated layout.
					// In that case, we do not want to build up cache.
					// But the ITreeBuilder implementation of the panel will have returned that it has more
					// work to do, so hopefully we will get a new chance soon.
					shouldProcessCache = !pFrameScheduler.IsHighPriority();
				}
			}
		}
#endif

		return shouldProcessCache;
	}

	internal bool IsBuildTreeSuspended => Visibility == Visibility.Collapsed || !AreAllAncestorsVisible();

	// the async version of doWork that is being called by NWDrawTree
	internal bool BuildTree()
	{
		bool pWorkLeft;

		if (IsMeasureDirty)
		{
			// If we are measure dirty, we take ourselves out of the build tree service queue.
			// During the next measure we will re-evaluate and register work if cache needs to be expanded.
			// Consider the case where the panel is collapsed while having work in the build tree
			// service queue. We will come here repeatedly and say we have work left and invalidate measure
			// but measure will not do anything since we are collapsed, and we keep ticking.
			// This stops that from happening.
			pWorkLeft = false;
		}
		else
		{
			if (!m_windowState.cachePotentialReached)
			{
				// great, we are given an opportunity to work on the caches. First figure out if we want to.
				bool shouldProcessCache = true;

				shouldProcessCache = CanIncreaseCacheLength();

				if (shouldProcessCache)
				{
					// he needs to be invalidated so he can actually increase the cache.
					// note how we only do this if we did absolutely no work
					InvalidateMeasure();

					// since after buildtree from the core we will call updatelayout,
					// the panel will get an opportunity to increase the cache.
					// The panel will have logic to either do the work or not.
					// Notice how we only did this invalidate if we did not do any other work for this
					// panel.
				}
			}

			// notice the approach here:
			// We check whether we can build up cache right now
			//   If so: invalidate
			//   If not: do nothing
			// Regardless, we will return that we have work left for as long as work hasn't been finished,
			// and we are in the live tree.

			pWorkLeft = !m_windowState.cachePotentialReached && IsInLiveTree;
		}

#if MCBP_DEBUG
		global::System.Diagnostics.Debug.WriteLine($"MCBP_DEBUG[{GetHashCode()}]: BuildTreeImpl. exit workLeft={pWorkLeft}");
#endif
		return pWorkLeft;
	}

	private void CoerceWindowToExtent(ref Rect visibleWindow)
	{
		ScrollViewer? scrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out scrollViewer);
		if (scrollViewer is not null)
		{
			double extentWidth;
			double extentHeight;
			float zoomFactor;
			extentWidth = scrollViewer.ExtentWidth;
			extentHeight = scrollViewer.ExtentHeight;
			zoomFactor = scrollViewer.ZoomFactor;

			Rect extent = new Rect(
				-m_originFromItemsPresenter.X,
				-m_originFromItemsPresenter.Y,
				(float)(extentWidth / zoomFactor),
				(float)(extentHeight / zoomFactor));

			// The window's size in the virtualizing direction cannot be bigger than the extent.
			SetSizeFromRectInVirtualizingDirection(ref visibleWindow, Math.Min(SizeFromRectInVirtualizingDirection(visibleWindow), SizeFromRectInVirtualizingDirection(extent)));

			// The window can't go beyond the beginning of the extent.
			if (PointFromRectInVirtualizingDirection(visibleWindow) < PointFromRectInVirtualizingDirection(extent))
			{
				SetPointFromRectInVirtualizingDirection(ref visibleWindow, PointFromRectInVirtualizingDirection(extent));
			}
			else
			{
				// The window can't go beyond the end of the extent.
				float bottomDelta =
					(PointFromRectInVirtualizingDirection(visibleWindow) + SizeFromRectInVirtualizingDirection(visibleWindow)) -
					(PointFromRectInVirtualizingDirection(extent) + SizeFromRectInVirtualizingDirection(extent));
				if (bottomDelta > 0.0f)
				{
					SetPointFromRectInVirtualizingDirection(ref visibleWindow, PointFromRectInVirtualizingDirection(visibleWindow) - bottomDelta);
				}
			}
		}
	}
}
