// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls
{
	internal class ViewportManagerWithPlatformFeatures : ViewportManager
	{

		// Pixel delta by which to inflate the cache buffer on each side.  Rather than fill the entire
		// cache buffer all at once, we chunk the work to make the UI thread more responsive.  We inflate
		// the cache buffer from 0 to a max value determined by the Maximum[Horizontal,Vertical]CacheLength
		// properties.
		private const double CacheBufferPerSideInflationPixelDelta = 40.0;


		private ItemsRepeater m_owner;

		private bool m_ensuredScroller;
		private IScrollAnchorProvider m_scroller;

		private UIElement m_makeAnchorElement;
		private bool m_isAnchorOutsideRealizedRange;  // Value is only valid when m_makeAnchorElement is set.

		private IAsyncAction m_cacheBuildAction;

		private Rect m_visibleWindow;
		private Rect m_layoutExtent;
		// This is the expected shift by the layout.
		private Point m_expectedViewportShift;
		// This is what is pending and not been accounted for. 
		// Sometimes the scrolling surface cannot service a shift (for example
		// it is already at the top and cannot shift anymore.)
		private Point m_pendingViewportShift;
		// Unshiftable shift amount that this view manager can
		// handle on its own to fake it to the layout as if the shift
		// actually happened. This can happen in cases where no scrollviewer
		// in the parent chain can scroll in the shift direction.
		private Point m_unshiftableShift;


		// Realization window cache fields
		private double m_maximumHorizontalCacheLength = 2.0;
		private double m_maximumVerticalCacheLength = 2.0;
		private double m_horizontalCacheBufferPerSide;
		private double m_verticalCacheBufferPerSide;

		private bool m_isBringIntoViewInProgress;
		// For non-virtualizing layouts, we do not need to keep
		// updating viewports and invalidating measure often. So when
		// a non virtualizing layout is used, we stop doing all that work.
		private bool m_managingViewportDisabled;

		// Event tokens
		//winrt::FrameworkElement::EffectiveViewportChanged_revoker m_effectiveViewportChangedRevoker { };
		//winrt::FrameworkElement::LayoutUpdated_revoker m_layoutUpdatedRevoker { };
		private IDisposable m_layoutUpdatedRevoker;

		//winrt::Windows::UI::Xaml::Media::CompositionTarget::Rendering_revoker m_renderingToken { };

		private bool HasScroller => m_scroller != null;

		public ViewportManagerWithPlatformFeatures(ItemsRepeater owner)
		{
			// ItemsRepeater is not fully constructed yet. Don't interact with it.

			m_owner = owner;
			m_scroller = owner;
			m_makeAnchorElement = owner;
			m_cacheBuildAction = owner;
		}

		public override UIElement MadeAnchor => m_makeAnchorElement;

		public override UIElement SuggestedAnchor
		{
			get
			{
				// The element generated during the ItemsRepeater.MakeAnchor call has precedence over the next tick.
				UIElement suggestedAnchor = m_makeAnchorElement;
				UIElement owner = m_owner;

				if (suggestedAnchor == null)
				{
					var anchorElement = m_scroller?.CurrentAnchor;

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

		public override Point GetOrigin() => new Point(m_layoutExtent.X, m_layoutExtent.Y);

		Rect GetLayoutVisibleWindowDiscardAnchor()
		{
			var visibleWindow = m_visibleWindow;

			if (HasScroller)
			{
				visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X + m_unshiftableShift.X;
				visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y + m_unshiftableShift.Y;
			}

			return visibleWindow;
		}

		public override Rect GetLayoutVisibleWindow()
		{
			var visibleWindow = m_visibleWindow;

			if (m_makeAnchorElement != null)
			{
				// The anchor is not necessarily laid out yet. Its position should default
				// to zero and the layout origin is expected to change once layout is done.
				// Until then, we need a window that's going to protect the anchor from
				// getting recycled.
				visibleWindow.X = 0.0f;
				visibleWindow.Y = 0.0f;
			}
			else if (HasScroller)
			{
				visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X + m_unshiftableShift.X;
				visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y + m_unshiftableShift.Y;
			}

			return visibleWindow;
		}

		public override Rect GetLayoutRealizationWindow()
		{
			var realizationWindow = GetLayoutVisibleWindow();
			if (HasScroller)
			{
				realizationWindow.X -= (float)(m_horizontalCacheBufferPerSide);
				realizationWindow.Y -= (float)(m_verticalCacheBufferPerSide);
				realizationWindow.Width += (float)(m_horizontalCacheBufferPerSide) * 2.0f;
				realizationWindow.Height += (float)(m_verticalCacheBufferPerSide) * 2.0f;
			}

			return realizationWindow;
		}

		public override void SetLayoutExtent(Rect extent)
		{
			m_expectedViewportShift.X += m_layoutExtent.X - extent.X;
			m_expectedViewportShift.Y += m_layoutExtent.Y - extent.Y;

			// We tolerate viewport imprecisions up to 1 pixel to avoid invaliding layout too much.
			if (Math.Abs(m_expectedViewportShift.X) > 1 || Math.Abs(m_expectedViewportShift.Y) > 1)
			{
				REPEATER_TRACE_INFO("%ls: \tExpecting viewport shift of (%.0f,%.0f) \n", GetLayoutId(), m_expectedViewportShift.X, m_expectedViewportShift.Y);

				// There are cases where we might be expecting a shift but not get it. We will
				// be waiting for the effective viewport event but if the scroll viewer is not able
				// to perform the shift (perhaps because it cannot scroll in negative offset),
				// then we will end up not realizing elements in the visible 
				// window. To avoid this, we register to layout updated for this layout pass. If we 
				// get an effective viewport, we know we have a new viewport and we unregister from
				// layout updated. If we get the layout updated handler, then we know that the 
				// scroller was unable to perform the shift and we invalidate measure and unregister
				// from the layout updated event.
				if (m_layoutUpdatedRevoker == null)
				{
					m_layoutUpdatedRevoker = Disposable.Create(() => m_owner.LayoutUpdated -= OnLayoutUpdated);
					m_owner.LayoutUpdated += OnLayoutUpdated;
				}
			}

			m_layoutExtent = extent;
			m_pendingViewportShift = m_expectedViewportShift;

			// We just finished a measure pass and have a new extent.
			// Let's make sure the scrollers will run its arrange so that they track the anchor.
			if (m_scroller != null)
			{
				(m_scroller as UIElement).InvalidateArrange();
			}
		}

		public override void OnLayoutChanged(bool isVirtualizing)
		{
			m_managingViewportDisabled = !isVirtualizing;

			m_layoutExtent = default;
			m_expectedViewportShift = default;
			m_pendingViewportShift = default;

			if (m_managingViewportDisabled)
			{
				m_effectiveViewportChangedRevoker.revoke();
			}
			else if (!m_effectiveViewportChangedRevoker)
			{
				m_effectiveViewportChangedRevoker = m_owner.EffectiveViewportChanged(auto_revoke,  {
					this, &ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged
				});
			}

			m_unshiftableShift =  {
			}
			;
			ResetCacheBuffer();
		}

		public override void OnElementPrepared(UIElement element)
		{
			// If we have an anchor element, we do not want the
			// scroll anchor provider to start anchoring some other element.
			element.CanBeScrollAnchor = true;
		}

		public override void OnElementCleared(UIElement element)
		{
			element.CanBeScrollAnchor = false;
		}

		public override void OnOwnerMeasuring()
		{
			// This is because of a bug that causes effective viewport to not 
			// fire if you register during arrange.
			// Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
			EnsureScroller();
		}

		public override void OnOwnerArranged()
		{
			m_expectedViewportShift = default;

			if (!m_managingViewportDisabled)
			{
				// This is because of a bug that causes effective viewport to not 
				// fire if you register during arrange.
				// Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
				// EnsureScroller();

				if (HasScroller)
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

		void OnLayoutUpdated(object sender, object args)
		{
			m_layoutUpdatedRevoker?.Dispose();
			m_layoutUpdatedRevoker = null;

			if (m_managingViewportDisabled)
			{
				return;
			}

			// We were expecting a viewport shift but we never got one and we are not going to in this
			// layout pass. We likely will never get this shift, so lets assume that we are never going to get it and
			// adjust our expected shift to track that. One case where this can happen is when there is no scrollviewer
			// that can scroll in the direction where the shift is expected.
			if (m_pendingViewportShift.X != 0 || m_pendingViewportShift.Y != 0)
			{
				REPEATER_TRACE_INFO("%ls: \tLayout Updated with pending shift %.0f %.0f- invalidating measure \n",
					GetLayoutId(),
					m_pendingViewportShift.X,
					m_pendingViewportShift.Y);

				// Assume this is never going to come.
				m_unshiftableShift.X += m_pendingViewportShift.X;
				m_unshiftableShift.Y += m_pendingViewportShift.Y;
				m_pendingViewportShift = default;
				m_expectedViewportShift = default;

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
				foreach (var child in m_owner.Children)
				{
					if (child.CanBeScrollAnchor && child != targetChild)
					{
						child.CanBeScrollAnchor = false;
					}
				}

				// Register to rendering event to go back to how things were before where any child can be the anchor.
				m_isBringIntoViewInProgress = true;
				if (!m_renderingToken)
				{
					Windows.UI.Xaml.Media.CompositionTarget compositionTarget{
						null
					}
					;
					m_renderingToken = compositionTarget.Rendering(auto_revoke,  {
						this, &ViewportManagerWithPlatformFeatures.OnCompositionTargetRendering
					});
				}
			}
		}

		UIElement GetImmediateChildOfRepeater(UIElement descendant)
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
				throw InvalidOperationException("OnBringIntoViewRequested called with args.target element not under the ItemsRepeater that recieved the call");
			}

			return targetChild;
		}

		void OnCompositionTargetRendering(object sender, object args)
		{
			global::System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);

			m_renderingToken.revoke();

			m_isBringIntoViewInProgress = false;
			m_makeAnchorElement = null;

			// Now that the item has been brought into view, we can let the anchor provider pick a new anchor.
			foreach (var child in m_owner.Children)
			{
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
			m_effectiveViewportChangedRevoker.revoke();
			m_ensuredScroller = false;
		}

		void OnCacheBuildActionCompleted()
		{
			m_cacheBuildAction = null;
			if (!m_managingViewportDisabled)
			{
				m_owner.InvalidateMeasure();
			}
		}

		void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			global::System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);
			REPEATER_TRACE_INFO("%ls: \tEffectiveViewportChanged event callback \n", GetLayoutId());
			UpdateViewport(args.EffectiveViewport);

			m_pendingViewportShift = default;
			m_unshiftableShift = default;
			if (m_visibleWindow == new Rect())
			{
				// We got cleared.
				m_layoutExtent = default;
			}

			// We got a new viewport, we dont need to wait for layout updated anymore to 
			// see if our request for a pending shift was handled.
			m_layoutUpdatedRevoker.revoke();
		}

		void EnsureScroller()
		{
			if (!m_ensuredScroller)
			{
				ResetScrollers();

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

				if (m_scroller == null)
				{
					// We usually update the viewport in the post arrange handler. But, since we don't have
					// a scroller, let's do it now.
					UpdateViewport(new Rect( ));
				}
				else if (!m_managingViewportDisabled)
				{
					m_effectiveViewportChangedRevoker = m_owner.EffectiveViewportChanged(auto_revoke,  {
						this, &ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged
					});
				}

				m_ensuredScroller = true;
			}
		}

		void UpdateViewport(Rect viewport)
		{
			global::System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);
			var previousVisibleWindow = m_visibleWindow;
			REPEATER_TRACE_INFO("%ls: \tEffective Viewport: (%.0f,%.0f,%.0f,%.0f).(%.0f,%.0f,%.0f,%.0f). \n",
				GetLayoutId(),
				previousVisibleWindow.X, previousVisibleWindow.Y, previousVisibleWindow.Width, previousVisibleWindow.Height,
				viewport.X, viewport.Y, viewport.Width, viewport.Height);

			var currentVisibleWindow = viewport;

			if (-currentVisibleWindow.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
				-currentVisibleWindow.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
			{
				REPEATER_TRACE_INFO("%ls: \tViewport is invalid. visible window cleared. \n", GetLayoutId());
				// We got cleared.
				m_visibleWindow =  default;
			}
			else
			{
				REPEATER_TRACE_INFO("%ls: \tUsed Viewport: (%.0f,%.0f,%.0f,%.0f).(%.0f,%.0f,%.0f,%.0f). \n",
					GetLayoutId(),
					previousVisibleWindow.X, previousVisibleWindow.Y, previousVisibleWindow.Width, previousVisibleWindow.Height,
					currentVisibleWindow.X, currentVisibleWindow.Y, currentVisibleWindow.Width, currentVisibleWindow.Height);
				m_visibleWindow = currentVisibleWindow;
			}

			TryInvalidateMeasure();
		}

		void ResetCacheBuffer()
		{
			m_horizontalCacheBufferPerSide = 0.0;
			m_verticalCacheBufferPerSide = 0.0;

			if (!m_managingViewportDisabled)
			{
				// We need to start building the realization buffer again.
				RegisterCacheBuildWork();
			}
		}

		void ValidateCacheLength(double cacheLength)
		{
			if (cacheLength < 0.0 || double.IsInfinity(cacheLength) || double.IsInfinity(cacheLength))
			{
				throw new ArgumentOutOfRangeException("The maximum cache length must be equal or superior to zero.");
			}
		}

		void RegisterCacheBuildWork()
		{
			global::System.Diagnostics.Debug.Assert(!m_managingViewportDisabled);
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

		string GetLayoutId()
			=> m_owner.Layout?.LayoutId;

		[Conditional("TRACE")]
		private static void REPEATER_TRACE_INFO(string text, params object[] args)
			=> Console.WriteLine(text, args);
	}
}
