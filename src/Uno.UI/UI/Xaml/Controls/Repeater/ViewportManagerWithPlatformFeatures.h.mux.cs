// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewportManagerWithPlatformFeatures.h, commit 4b206bce3

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

// Manages the virtualization windows (visible/realization). This class essentially
// does the equivalent behavior of ViewportManager class except that here we use
// EffectiveViewport and ScrollAnchoring features added to the framework in RS5.
// We also do not use the IRepeaterScrollingSurface internal API used by ViewManager.
partial class ViewportManagerWithPlatformFeatures
{
	public override double HorizontalCacheLength
	{
		get => m_maximumHorizontalCacheLength;
		set => SetHorizontalCacheLength(value);
	}

	public override double VerticalCacheLength
	{
		get => m_maximumVerticalCacheLength;
		set => SetVerticalCacheLength(value);
	}

	public override Rect GetLayoutExtent() => m_layoutExtent;

	public override Point GetOrigin() => new Point(m_layoutExtent.X, m_layoutExtent.Y);

	public override UIElement MadeAnchor => m_makeAnchorElement;

	private bool HasScroller() => m_scroller != null;

	ItemsRepeater m_owner;

	bool m_ensuredScroller;
	IScrollAnchorProvider m_scroller;

	List<UIElement> m_preparedElements = new List<UIElement>();
	List<UIElement> m_preparedAndArrangedElements = new List<UIElement>();

	UIElement m_makeAnchorElement;
	bool m_isAnchorOutsideRealizedRange;  // Value is only valid when m_makeAnchorElement is set.

	bool m_skipScrollAnchorRegistrationsDuringNextMeasurePass;
	bool m_skipScrollAnchorRegistrationsDuringNextArrangePass;

	// Uno specific: Tracks the outstanding cache build work. WinUI uses a plain bool
	// m_cacheBuildActionOutstanding alongside a CoreDispatcher IdleHandler; Uno keeps a
	// reference to the IAsyncAction so it can be cancelled if needed.
	IAsyncAction m_cacheBuildAction;

	Rect m_lastLayoutRealizationWindow;
	Rect m_visibleWindow;
	Rect m_layoutExtent;

	// This is the expected shift by the layout.
	Point m_expectedViewportShift;

	// This is what is pending and not been accounted for.
	// Sometimes the scrolling surface cannot service a shift (for example
	// it is already at the top and cannot shift anymore.)
	Point m_pendingViewportShift;

	// Unshiftable shift amount that this view manager can
	// handle on its own to fake it to the layout as if the shift
	// actually happened. This can happen in cases where no scroller
	// in the parent chain can scroll in the shift direction.
	Point m_unshiftableShift;

#pragma warning disable 414 // Assigned but not used: TODO Uno: ScrollPresenter event handlers (OnScrollPresenterScrollStarting etc.) are not yet ported.
	int m_lastScrollPresenterViewChangeCorrelationId = -1;
#pragma warning restore 414

	// Realization window cache fields
	double m_maximumHorizontalCacheLength = 2.0;
	double m_maximumVerticalCacheLength = 2.0;
	double m_horizontalCacheBufferPerSide;
	double m_verticalCacheBufferPerSide;

#pragma warning disable 414 // Assigned but not used field: As WinUI!
	bool m_isBringIntoViewInProgress;
#pragma warning restore 414

	// For non-virtualizing layouts, we do not need to keep
	// updating viewports and invalidating measure often. So when
	// a non virtualizing layout is used, we stop doing all that work.
	bool m_managingViewportDisabled;

	// Event tokens.
	// TODO Uno: WinUI uses auto-revoke handles for the IScrollPresenter
	// ScrollStarting/ScrollCompleted/ZoomStarting/ZoomCompleted events, which are not yet
	// ported. The EffectiveViewport, LayoutUpdated and CompositionTarget.Rendering revokers
	// are represented as IDisposable SerialDisposable-like tokens.
	IDisposable m_effectiveViewportChangedRevoker;
	IDisposable m_layoutUpdatedRevoker;
	IDisposable m_renderingToken;
}
