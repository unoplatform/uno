// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewportManagerDownlevel.h, commit 4b206bce3

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Private.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ViewportManagerDownLevel
{
	public override Rect GetLayoutExtent() => m_layoutExtent;

	public override Point GetOrigin() => new Point(m_layoutExtent.X, m_layoutExtent.Y);

	public override void OnElementPrepared(UIElement element) { }

	public override UIElement MadeAnchor => m_makeAnchorElement;

	private bool HasScrollers => m_horizontalScroller != null || m_verticalScroller != null;

	ItemsRepeater m_owner;

	// List of parent scrollers.
	// The list stops when we reach the root scroller OR when both m_horizontalScroller
	// and m_verticalScroller are set. In the latter case, we don't care about the other
	// scroller that we haven't reached yet.
	bool m_ensuredScrollers;
	List<ScrollerInfo> m_parentScrollers = new List<ScrollerInfo>();

	// In order to support the Store scenario (vertical list of horizontal lists),
	// we need to build a synthetic virtualization window by taking the horizontal and
	// vertical components of the viewport from two different scrollers.
	IRepeaterScrollingSurface m_horizontalScroller;
	IRepeaterScrollingSurface m_verticalScroller;
	// Invariant: !m_innerScrollableScroller || m_horizontalScroller == m_innerScrollableScroller || m_verticalScroller == m_innerScrollableScroller.
	IRepeaterScrollingSurface m_innerScrollableScroller;

	UIElement m_makeAnchorElement;
	bool m_isAnchorOutsideRealizedRange;  // Value is only valid when m_makeAnchorElement is set.

	IAsyncAction m_cacheBuildAction;

	Rect m_lastLayoutRealizationWindow;
	Rect m_visibleWindow;
	Rect m_layoutExtent;
	Point m_expectedViewportShift;

	// Realization window cache fields
	double m_maximumHorizontalCacheLength = 2.0;
	double m_maximumVerticalCacheLength = 2.0;
	double m_horizontalCacheBufferPerSide;
	double m_verticalCacheBufferPerSide;

	// For non-virtualizing layouts, we do not need to keep
	// updating viewports and invalidating measure often. So when
	// a non virtualizing layout is used, we stop doing all that work.
	bool m_managingViewportDisabled;

	// Event tokens
	// Uno specific: WinUI uses a PostArrange_revoker auto-revoke; Uno uses IDisposable tokens
	// stored directly inside each ScrollerInfo entry in m_parentScrollers.

	// Stores information about a parent scrolling surface.
	// We subscribe to...
	// - ViewportChanged only on scrollers that are scrollable in at least one direction.
	// - ConfigurationChanged on all scrollers.
	// - PostArrange only on the outer most scroller, because we need to wait for that one
	//   to arrange its children before we can reliably figure out our relative viewport.
	internal struct ScrollerInfo
	{
		private IRepeaterScrollingSurface m_scroller;

		public ScrollerInfo(IRepeaterScrollingSurface scroller)
		{
			m_scroller = scroller;
			ViewportChangedToken = null;
			PostArrangeToken = null;
			ConfigurationChangedToken = null;
		}

		public IRepeaterScrollingSurface Scroller => m_scroller;

		public IDisposable ViewportChangedToken;
		public IDisposable PostArrangeToken;
		public IDisposable ConfigurationChangedToken;
	}
}
