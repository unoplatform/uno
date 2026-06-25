// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewportManagerDownlevel.cpp, commit 4b206bce3

#pragma warning disable 105 // remove when moving to WinUI tree

using System;
using Windows.Foundation;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ViewportManagerDownLevel
{
	// Pixel delta by which to inflate the cache buffer on each side.  Rather than fill the entire
	// cache buffer all at once, we chunk the work to make the UI thread more responsive.  We inflate
	// the cache buffer from 0 to a max value determined by the Maximum[Horizontal,Vertical]CacheLength
	// properties.
	private const double CacheBufferPerSideInflationPixelDelta = 40.0;

	public ViewportManagerDownLevel(ItemsRepeater owner)
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
			UIElement owner = m_owner;

			if (suggestedAnchor == null)
			{
				// We only care about what the first scrollable scroller is tracking (i.e. inner most).
				// A scroller is considered scrollable if IRepeaterScrollingSurface.IsHorizontallyScrollable
				// or IsVerticallyScroller is true.
				var anchorElement = m_innerScrollableScroller?.AnchorElement;

				if (anchorElement != null)
				{
					// We can't simply return anchorElement because, in case of nested Repeaters, it may not
					// be a direct child of ours, or even an indirect child. We need to walk up the tree starting
					// from anchorElement to figure out what child of ours (if any) to use as the suggested element.

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

	public override double HorizontalCacheLength
	{
		get => m_maximumHorizontalCacheLength;
		set
		{
			if (m_maximumHorizontalCacheLength != value)
			{
				ValidateCacheLength(value);
				m_maximumHorizontalCacheLength = value;
				ResetCacheBuffer();
			}
		}
	}

	public override double VerticalCacheLength
	{
		get => m_maximumVerticalCacheLength;
		set
		{
			if (m_maximumVerticalCacheLength != value)
			{
				ValidateCacheLength(value);
				m_maximumVerticalCacheLength = value;
				ResetCacheBuffer();
			}
		}
	}

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
		else if (HasScrollers)
		{
			visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X;
			visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y;
		}

		return visibleWindow;
	}

	public override Rect GetLayoutRealizationWindow()
	{
		var realizationWindow = GetLayoutVisibleWindow();
		if (HasScrollers)
		{
			realizationWindow.X -= (float)m_horizontalCacheBufferPerSide;
			realizationWindow.Y -= (float)m_verticalCacheBufferPerSide;
			realizationWindow.Width += (float)m_horizontalCacheBufferPerSide * 2.0f;
			realizationWindow.Height += (float)m_verticalCacheBufferPerSide * 2.0f;
		}

		return realizationWindow;
	}

	public override void SetLayoutExtent(Rect extent)
	{
		m_expectedViewportShift.X += m_layoutExtent.X - extent.X;
		m_expectedViewportShift.Y += m_layoutExtent.Y - extent.Y;

		// We tolerate viewport imprecisions up to 1 pixel to avoid invaliding layout too much.
		if (Math.Abs(m_expectedViewportShift.X) > 1f || Math.Abs(m_expectedViewportShift.Y) > 1f)
		{
			REPEATER_TRACE_INFO("%ls: \tExpecting viewport shift of (%.0f,%.0f) \n",
				GetLayoutId(), m_expectedViewportShift.X, m_expectedViewportShift.Y);
		}

		m_layoutExtent = extent;

		// We just finished a measure pass and have a new extent.
		// Let's make sure the scrollers will run its arrange so that they track the anchor.
		var outerScroller = GetOuterScroller();
		if (outerScroller != null) { (outerScroller as UIElement)?.InvalidateArrange(); }
		if (m_horizontalScroller != null && m_horizontalScroller != outerScroller) { (m_horizontalScroller as UIElement)?.InvalidateArrange(); }
		if (m_verticalScroller != null && m_verticalScroller != outerScroller) { (m_verticalScroller as UIElement)?.InvalidateArrange(); }
	}

	public override void OnLayoutChanged(bool isVirtualizing)
	{
		m_managingViewportDisabled = !isVirtualizing;
		m_layoutExtent = default;
		m_expectedViewportShift = default;
		ResetCacheBuffer();
	}

	public override void OnElementCleared(UIElement element)
	{
		if (m_horizontalScroller != null)
		{
			m_horizontalScroller.UnregisterAnchorCandidate(element);
		}

		if (m_verticalScroller != null && m_verticalScroller != m_horizontalScroller)
		{
			m_verticalScroller.UnregisterAnchorCandidate(element);
		}
	}

	public override void OnOwnerMeasuring()
	{
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

		m_lastLayoutRealizationWindow = currentLayoutRealizationWindow;
	}

	public override void OnOwnerArranged()
	{
		if (m_managingViewportDisabled)
		{
			return;
		}

		m_expectedViewportShift = default;

		EnsureScrollers();

		if (HasScrollers)
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
		}
	}

	public override void ResetScrollers()
	{
		// Uno specific: dispose tokens before clearing the list so listeners are detached.
		foreach (var info in m_parentScrollers)
		{
			info.ViewportChangedToken?.Dispose();
			info.PostArrangeToken?.Dispose();
			info.ConfigurationChangedToken?.Dispose();
		}
		m_parentScrollers.Clear();
		m_horizontalScroller = null;
		m_verticalScroller = null;
		m_innerScrollableScroller = null;

		m_ensuredScrollers = false;
	}

	void OnCacheBuildActionCompleted()
	{
		m_cacheBuildAction = null;
		m_owner.InvalidateMeasure();
	}

	void OnViewportChanged(IRepeaterScrollingSurface sender, bool isFinal)
	{
		if (!m_managingViewportDisabled)
		{
			if (isFinal)
			{
				// Note that isFinal will never be true for input based manipulations.
				m_makeAnchorElement = null;
				m_isAnchorOutsideRealizedRange = false;
			}

			TryInvalidateMeasure();
		}
	}

	void OnPostArrange(IRepeaterScrollingSurface sender)
	{
		if (!m_managingViewportDisabled)
		{
			UpdateViewport();

			if (m_visibleWindow == new Rect())
			{
				// We got cleared.
				m_layoutExtent = default;
			}
			else
			{
				// Register our non-recycled children as candidates for element tracking.
				if (m_horizontalScroller != null || m_verticalScroller != null)
				{
					var children = m_owner.Children;
					for (int i = 0; i < children.Count; ++i)
					{
						var element = children[i];
						var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
						if (virtInfo.IsHeldByLayout)
						{
							if (m_horizontalScroller != null)
							{
								m_horizontalScroller.RegisterAnchorCandidate(element);
							}

							if (m_verticalScroller != null && m_verticalScroller != m_horizontalScroller)
							{
								m_verticalScroller.RegisterAnchorCandidate(element);
							}
						}
					}
				}
			}
		}
	}

	void OnConfigurationChanged(IRepeaterScrollingSurface sender)
	{
		m_ensuredScrollers = false;
		TryInvalidateMeasure();
	}

	void EnsureScrollers()
	{
		if (!m_ensuredScrollers)
		{
			ResetScrollers();

			var parent = CachedVisualTreeHelpers.GetParent(m_owner);
			while (parent != null)
			{
				if (parent is IRepeaterScrollingSurface scroller && AddScroller(scroller))
				{
					break;
				}

				parent = CachedVisualTreeHelpers.GetParent(parent);
			}

			if (m_parentScrollers.Count == 0)
			{
				// We usually update the viewport in the post arrange handler. But, since we don't have
				// a scroller, let's do it now.
				UpdateViewport();
			}
			else
			{
				var outerScrollerInfo = m_parentScrollers[m_parentScrollers.Count - 1];
				outerScrollerInfo.Scroller.PostArrange += OnPostArrange;
				outerScrollerInfo.PostArrangeToken = Disposable.Create(() =>
				{
					outerScrollerInfo.Scroller.PostArrange -= OnPostArrange;
				});
				m_parentScrollers[m_parentScrollers.Count - 1] = outerScrollerInfo;
			}

			m_ensuredScrollers = true;
		}
	}

	bool AddScroller(IRepeaterScrollingSurface scroller)
	{
		MUX_ASSERT(!(m_horizontalScroller != null && m_verticalScroller != null));

		bool isHorizontallyScrollable = scroller.IsHorizontallyScrollable;
		bool isVerticallyScrollable = scroller.IsVerticallyScrollable;
		bool allScrollersSet = (m_horizontalScroller != null || isHorizontallyScrollable) && (m_verticalScroller != null || isVerticallyScrollable);
		bool setHorizontalScroller = m_horizontalScroller == null && isHorizontallyScrollable;
		bool setVerticalScroller = m_verticalScroller == null && isVerticallyScrollable;
		bool setInnerScrollableScroller = m_innerScrollableScroller == null && (setHorizontalScroller || setVerticalScroller);

		if (setHorizontalScroller) { m_horizontalScroller = scroller; }
		if (setVerticalScroller) { m_verticalScroller = scroller; }
		if (setInnerScrollableScroller) { m_innerScrollableScroller = scroller; }

		var scrollerInfo = new ScrollerInfo(scroller);

		scroller.ConfigurationChanged += OnConfigurationChanged;
		scrollerInfo.ConfigurationChangedToken = Disposable.Create(() =>
		{
			scroller.ConfigurationChanged -= OnConfigurationChanged;
		});

		if (setHorizontalScroller || setVerticalScroller)
		{
			scroller.ViewportChanged += OnViewportChanged;
			scrollerInfo.ViewportChangedToken = Disposable.Create(() =>
			{
				scroller.ViewportChanged -= OnViewportChanged;
			});
		}

		m_parentScrollers.Add(scrollerInfo);
		return allScrollersSet;
	}

	void UpdateViewport()
	{
		global::System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);

		var previousVisibleWindow = m_visibleWindow;
		var horizontalVisibleWindow =
			m_horizontalScroller != null ?
			m_horizontalScroller.GetRelativeViewport(m_owner) :
			new Rect();
		var verticalVisibleWindow =
			m_verticalScroller != null ?
			(m_verticalScroller == m_horizontalScroller ?
				horizontalVisibleWindow :
				m_verticalScroller.GetRelativeViewport(m_owner)) :
			new Rect();
		var currentVisibleWindow =
			HasScrollers ?
			new Rect(
				m_horizontalScroller != null ? horizontalVisibleWindow.X : verticalVisibleWindow.X,
				m_verticalScroller != null ? verticalVisibleWindow.Y : horizontalVisibleWindow.Y,
				m_horizontalScroller != null ? horizontalVisibleWindow.Width : verticalVisibleWindow.Width,
				m_verticalScroller != null ? verticalVisibleWindow.Height : horizontalVisibleWindow.Height) :
			new Rect(0.0f, 0.0f, float.MaxValue, float.MaxValue);

		if (-currentVisibleWindow.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
			-currentVisibleWindow.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
		{
			// We got cleared.
			m_visibleWindow = default;
		}
		else
		{
			REPEATER_TRACE_INFO("%ls: \tViewport: (%.0f,%.0f,%.0f,%.0f)->(%.0f,%.0f,%.0f,%.0f). \n",
				GetLayoutId(),
				previousVisibleWindow.X, previousVisibleWindow.Y, previousVisibleWindow.Width, previousVisibleWindow.Height,
				currentVisibleWindow.X, currentVisibleWindow.Y, currentVisibleWindow.Width, currentVisibleWindow.Height);
			m_visibleWindow = currentVisibleWindow;
		}

		bool viewportChanged =
			Math.Abs(m_visibleWindow.X - previousVisibleWindow.X) > 1 ||
			Math.Abs(m_visibleWindow.Y - previousVisibleWindow.Y) > 1 ||
			m_visibleWindow.Width != previousVisibleWindow.Width ||
			m_visibleWindow.Height != previousVisibleWindow.Height;

		if (viewportChanged)
		{
			TryInvalidateMeasure();
		}
	}

	public override void ResetLayoutRealizationWindowCacheBuffer()
	{
		ResetCacheBuffer(false /*registerCacheBuildWork*/);
	}

	void ResetCacheBuffer(bool registerCacheBuildWork = true)
	{
		m_horizontalCacheBufferPerSide = 0.0;
		m_verticalCacheBufferPerSide = 0.0;

		if (!m_managingViewportDisabled && registerCacheBuildWork)
		{
			// We need to start building the realization buffer again.
			RegisterCacheBuildWork();
		}
	}

	void ValidateCacheLength(double cacheLength)
	{
		if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsNaN(cacheLength))
		{
			throw new ArgumentOutOfRangeException("The maximum cache length must be equal or superior to zero.");
		}
	}

	void RegisterCacheBuildWork()
	{
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

	void TryInvalidateMeasure()
	{
		// Don't invalidate measure if we have an invalid window.
		if (m_visibleWindow != new Rect())
		{
			// We invalidate measure instead of just invalidating arrange because
			// we don't invalidate measure in UpdateViewport if the view is changing to
			// avoid layout cycles.
			REPEATER_TRACE_INFO("%ls: \tInvalidating measure due to viewport change. \n", GetLayoutId());
			m_owner.InvalidateMeasure();
		}
	}

	IRepeaterScrollingSurface GetOuterScroller()
	{
		IRepeaterScrollingSurface scroller = null;

		if (m_parentScrollers.Count > 0)
		{
			scroller = m_parentScrollers[m_parentScrollers.Count - 1].Scroller;
		}

		return scroller;
	}

	string GetLayoutId()
		=> m_owner.Layout?.LayoutId;
}
