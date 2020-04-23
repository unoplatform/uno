// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include <pch.h>
#include <common.h>
#include "ItemsRepeater.common.h"
#include "ViewportManagerWithPlatformFeatures.h"
#include "ItemsRepeater.h"
#include "layout.h"

// Pixel delta by which to inflate the cache buffer on each side.  Rather than fill the entire
// cache buffer all at once, we chunk the work to make the UI thread more responsive.  We inflate
// the cache buffer from 0 to a max value determined by the Maximum[Horizontal,Vertical]CacheLength
// properties.
 double CacheBufferPerSideInflationPixelDelta = 40.0;

ViewportManagerWithPlatformFeatures(ItemsRepeater* owner) :
    m_owner(owner),
    m_scroller(owner),
    m_makeAnchorElement(owner),
    m_cacheBuildAction(owner)
{
    // ItemsRepeater is not fully constructed yet. Don't interact with it.
}

UIElement SuggestedAnchor() const
{
    // The element generated during the ItemsRepeater.MakeAnchor call has precedence over the next tick.
    UIElement suggestedAnchor = m_makeAnchorElement.get();
    UIElement owner = *m_owner;

    if (!suggestedAnchor)
    {
        const var anchorElement =
            m_scroller ?
            m_scroller.get().CurrentAnchor() :
            null;

        if (anchorElement)
        {
            // We can't simply return anchorElement because, in case of nested Repeaters, it may not
            // be a direct child of ours, or even an indirect child. We need to walk up the tree starting
            // from anchorElement to figure out what child of ours (if any) to use as the suggested element.
            var child = anchorElement;
(            var parent = CachedVisualTreeHelpers.GetParent(child) as UIElement);
            while (parent)
            {
                if (parent == owner)
                {
                    suggestedAnchor = child;
                    break;
                }

                child = parent;
(                parent = CachedVisualTreeHelpers.GetParent(parent) as UIElement);
            }
        }
    }

    return suggestedAnchor;
}

void HorizontalCacheLength(double value)
{
    if (m_maximumHorizontalCacheLength != value)
    {
        ValidateCacheLength(value);
        m_maximumHorizontalCacheLength = value;
        ResetCacheBuffer();
    }
}

void VerticalCacheLength(double value)
{
    if (m_maximumVerticalCacheLength != value)
    {
        ValidateCacheLength(value);
        m_maximumVerticalCacheLength = value;
        ResetCacheBuffer();
    }
}

Rect GetLayoutVisibleWindowDiscardAnchor() const
{
    var visibleWindow = m_visibleWindow;

    if (HasScroller())
    {
        visibleWindow.X += m_layoutExtent.X + m_expectedViewportShift.X + m_unshiftableShift.X;
        visibleWindow.Y += m_layoutExtent.Y + m_expectedViewportShift.Y + m_unshiftableShift.Y;
    }

    return visibleWindow;
}

Rect GetLayoutVisibleWindow() const
{
    var visibleWindow = m_visibleWindow;

    if (m_makeAnchorElement)
    {
        // The anchor is not necessarily laid out yet. Its position should default
        // to zero and the layout origin is expected to change once layout is done.
        // Until then, we need a window that's going to protect the anchor from
        // getting recycled.
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

Rect GetLayoutRealizationWindow() const
{
    var realizationWindow = GetLayoutVisibleWindow();
    if (HasScroller())
    {
        realizationWindow.X -= (float)(m_horizontalCacheBufferPerSide);
        realizationWindow.Y -= (float)(m_verticalCacheBufferPerSide);
        realizationWindow.Width += (float)(m_horizontalCacheBufferPerSide) * 2.0f;
        realizationWindow.Height += (float)(m_verticalCacheBufferPerSide) * 2.0f;
    }

    return realizationWindow;
}

void SetLayoutExtent(Rect extent)
{
    m_expectedViewportShift.X += m_layoutExtent.X - extent.X;
    m_expectedViewportShift.Y += m_layoutExtent.Y - extent.Y;

    // We tolerate viewport imprecisions up to 1 pixel to avoid invaliding layout too much.
    if (std.abs(m_expectedViewportShift.X) > 1.f || std.abs(m_expectedViewportShift.Y) > 1.f)
    {
        REPEATER_TRACE_INFO("%ls: \tExpecting viewport shift of (%.0f,%.0f) \n",
            GetLayoutId().data(), m_expectedViewportShift.X, m_expectedViewportShift.Y);

        // There are cases where we might be expecting a shift but not get it. We will
        // be waiting for the effective viewport event but if the scroll viewer is not able
        // to perform the shift (perhaps because it cannot scroll in negative offset),
        // then we will end up not realizing elements in the visible 
        // window. To avoid this, we register to layout updated for this layout pass. If we 
        // get an effective viewport, we know we have a new viewport and we unregister from
        // layout updated. If we get the layout updated handler, then we know that the 
        // scroller was unable to perform the shift and we invalidate measure and unregister
        // from the layout updated event.
        if (!m_layoutUpdatedRevoker)
        {
            m_layoutUpdatedRevoker = m_owner.LayoutUpdated(auto_revoke, { this, &ViewportManagerWithPlatformFeatures.OnLayoutUpdated });
        }
    }

    m_layoutExtent = extent;
    m_pendingViewportShift = m_expectedViewportShift;

    // We just finished a measure pass and have a new extent.
    // Let's make sure the scrollers will run its arrange so that they track the anchor.
    if (m_scroller)
    { 
(        m_scroller as UIElement).InvalidateArrange(); 
    }
}

void OnLayoutChanged(bool isVirtualizing)
{
    m_managingViewportDisabled = !isVirtualizing;

    m_layoutExtent = {};
    m_expectedViewportShift = {};
    m_pendingViewportShift = {};

    if (m_managingViewportDisabled)
    {
        m_effectiveViewportChangedRevoker.revoke();
    }
    else if (!m_effectiveViewportChangedRevoker)
    {
        m_effectiveViewportChangedRevoker = m_owner.EffectiveViewportChanged(auto_revoke, { this, &ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged });
    }

    m_unshiftableShift = {};
    ResetCacheBuffer();
}

void OnElementPrepared(const UIElement& element)
{
    // If we have an anchor element, we do not want the
    // scroll anchor provider to start anchoring some other element.
    element.CanBeScrollAnchor(true);
}

void OnElementCleared(const UIElement& element)
{
    element.CanBeScrollAnchor(false);
}

void OnOwnerMeasuring()
{
    // This is because of a bug that causes effective viewport to not 
    // fire if you register during arrange.
    // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
    EnsureScroller();
}

void OnOwnerArranged()
{
    m_expectedViewportShift = {};

    if (!m_managingViewportDisabled)
    {
        // This is because of a bug that causes effective viewport to not 
        // fire if you register during arrange.
        // Bug 17411076: EffectiveViewport: registering for effective viewport in arrange should invalidate viewport
        // EnsureScroller();

        if (HasScroller())
        {
            const double maximumHorizontalCacheBufferPerSide = m_maximumHorizontalCacheLength * m_visibleWindow.Width / 2.0;
            const double maximumVerticalCacheBufferPerSide = m_maximumVerticalCacheLength * m_visibleWindow.Height / 2.0;

            const bool continueBuildingCache =
                m_horizontalCacheBufferPerSide < maximumHorizontalCacheBufferPerSide ||
                m_verticalCacheBufferPerSide < maximumVerticalCacheBufferPerSide;

            if (continueBuildingCache)
            {
                m_horizontalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;
                m_verticalCacheBufferPerSide += CacheBufferPerSideInflationPixelDelta;

                m_horizontalCacheBufferPerSide = std.min(m_horizontalCacheBufferPerSide, maximumHorizontalCacheBufferPerSide);
                m_verticalCacheBufferPerSide = std.min(m_verticalCacheBufferPerSide, maximumVerticalCacheBufferPerSide);

                // Since we grow the cache buffer at the end of the arrange pass,
                // we need to register work even if we just reached cache potential.
                RegisterCacheBuildWork();
            }
        }
    }
}

void OnLayoutUpdated(IInspectable const& sender, IInspectable const& args)
{
    m_layoutUpdatedRevoker.revoke();
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
            GetLayoutId().data(),
            m_pendingViewportShift.X,
            m_pendingViewportShift.Y);

        // Assume this is never going to come.
        m_unshiftableShift.X += m_pendingViewportShift.X;
        m_unshiftableShift.Y += m_pendingViewportShift.Y;
        m_pendingViewportShift = {};
        m_expectedViewportShift = {};

        TryInvalidateMeasure();
    }
}

void OnMakeAnchor(const UIElement& anchor, const bool isAnchorOutsideRealizedRange)
{
    m_makeAnchorElement.set(anchor);
    m_isAnchorOutsideRealizedRange = isAnchorOutsideRealizedRange;
}

void OnBringIntoViewRequested(const BringIntoViewRequestedEventArgs args)
{
    if (!m_managingViewportDisabled)
    {
        // We do not animate bring-into-view operations where the anchor is disconnected because
        // it doesn't look good (the blank space is obvious because the layout can't keep track
        // of two realized ranges while the animation is going on).
        if (m_isAnchorOutsideRealizedRange)
        {
            args.AnimationDesired(false);
        }

        // During the time between a bring into view request and the element coming into view we do not
        // want the anchor provider to pick some anchor and jump to it. Instead we want to anchor on the
        // element that is being brought into view. We can do this by making just that element as a potential
        // anchor candidate and ensure no other element of this repeater is an anchor candidate.
        // Once the layout pass is done and we render the frame, the element will be in frame and we can
        // switch back to letting the anchor provider pick a suitable anchor.

        // get the targetChild - i.e the immediate child of this repeater that is being brought into view.
        // Note that the element being brought into view could be a descendant.
        const var targetChild = GetImmediateChildOfRepeater(args.TargetElement());

        // Make sure that only the target child can be the anchor during the bring into view operation.
        for (const auto& child : m_owner.Children())
        {
            if (child.CanBeScrollAnchor() && child != targetChild)
            {
                child.CanBeScrollAnchor(false);
            }
        }

        // Register to rendering event to go back to how things were before where any child can be the anchor.
        m_isBringIntoViewInProgress = true;
        if (!m_renderingToken)
        {
            Windows.UI.Xaml.Media.CompositionTarget compositionTarget{ null };
            m_renderingToken = compositionTarget.Rendering(auto_revoke, { this, &ViewportManagerWithPlatformFeatures.OnCompositionTargetRendering });
        }
    }
}

UIElement GetImmediateChildOfRepeater(UIElement const& descendant)
{
    UIElement targetChild = descendant;
(    UIElement parent = CachedVisualTreeHelpers.GetParent(descendant) as UIElement);
    while (parent != null && parent != (DependencyObject)(*m_owner))
    {
        targetChild = parent;
(        parent = CachedVisualTreeHelpers.GetParent(parent) as UIElement);
    }

    if (!parent)
    {
        throw hresult_error(E_FAIL, "OnBringIntoViewRequested called with args.target element not under the ItemsRepeater that recieved the call");
    }

    return targetChild;
}

void OnCompositionTargetRendering(const IInspectable& /*sender*/, const IInspectable& /*args*/)
{
    Debug.Assert(!m_managingViewportDisabled);

    m_renderingToken.revoke();

    m_isBringIntoViewInProgress = false;
    m_makeAnchorElement.set(null);

    // Now that the item has been brought into view, we can let the anchor provider pick a new anchor.
    for (const auto& child : m_owner.Children())
    {
        if (!child.CanBeScrollAnchor())
        {
            var info = ItemsRepeater.GetVirtualizationInfo(child);
            if (info.IsRealized() && info.IsHeldByLayout())
            {
                child.CanBeScrollAnchor(true);
            }
        }
    }
}

void ResetScrollers()
{
    m_scroller.set(null);
    m_effectiveViewportChangedRevoker.revoke();
    m_ensuredScroller = false;
}

void OnCacheBuildActionCompleted()
{
    m_cacheBuildAction.set(null);
    if (!m_managingViewportDisabled)
    {
        m_owner.InvalidateMeasure();
    }
}

void OnEffectiveViewportChanged(FrameworkElement const& sender, EffectiveViewportChangedEventArgs const& args)
{
    Debug.Assert(!m_managingViewportDisabled);
    REPEATER_TRACE_INFO("%ls: \tEffectiveViewportChanged event callback \n", GetLayoutId().data());
    UpdateViewport(args.EffectiveViewport());

    m_pendingViewportShift = {};
    m_unshiftableShift = {};
    if (m_visibleWindow == Rect())
    {
        // We got cleared.
        m_layoutExtent = {};
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

        var parent = CachedVisualTreeHelpers.GetParent(*m_owner);
        while (parent)
        {
            if (const var scroller = parent.try_as<Controls.IScrollAnchorProvider>())
            {
                m_scroller.set(scroller);
                break;
            }

            parent = CachedVisualTreeHelpers.GetParent(parent);
        }

        if (!m_scroller)
        {
            // We usually update the viewport in the post arrange handler. But, since we don't have
            // a scroller, let's do it now.
            UpdateViewport(Rect{});
        }
        else if (!m_managingViewportDisabled)
        {
            m_effectiveViewportChangedRevoker = m_owner.EffectiveViewportChanged(auto_revoke, { this, &ViewportManagerWithPlatformFeatures.OnEffectiveViewportChanged });
        }

        m_ensuredScroller = true;
    }
}

void UpdateViewport(Rect const& viewport)
{
    Debug.Assert(!m_managingViewportDisabled);
    const var previousVisibleWindow = m_visibleWindow;
    REPEATER_TRACE_INFO("%ls: \tEffective Viewport: (%.0f,%.0f,%.0f,%.0f).(%.0f,%.0f,%.0f,%.0f). \n",
        GetLayoutId().data(),
        previousVisibleWindow.X, previousVisibleWindow.Y, previousVisibleWindow.Width, previousVisibleWindow.Height,
        viewport.X, viewport.Y, viewport.Width, viewport.Height);

    var currentVisibleWindow = viewport;

    if (-currentVisibleWindow.X <= ItemsRepeater.ClearedElementsArrangePosition.X &&
        -currentVisibleWindow.Y <= ItemsRepeater.ClearedElementsArrangePosition.Y)
    {
        REPEATER_TRACE_INFO("%ls: \tViewport is invalid. visible window cleared. \n", GetLayoutId().data());
        // We got cleared.
        m_visibleWindow = {};
    }
    else
    {
        REPEATER_TRACE_INFO("%ls: \tUsed Viewport: (%.0f,%.0f,%.0f,%.0f).(%.0f,%.0f,%.0f,%.0f). \n",
            GetLayoutId().data(),
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
    if (cacheLength < 0.0 || std.isinf(cacheLength) || std.isnan(cacheLength))
    {
        throw hresult_invalid_argument("The maximum cache length must be equal or superior to zero.");
    }
}

void RegisterCacheBuildWork()
{
    Debug.Assert(!m_managingViewportDisabled);
    if (m_owner.Layout() &&
        !m_cacheBuildAction)
    {
        // We capture 'owner' (a strong refernce on ItemsRepeater) to make sure ItemsRepeater is still around
        // when the async action completes. By protecting ItemsRepeater, we also ensure that this instance
        // of ViewportManager (referenced by 'this' pointer) is valid because the lifetime of ItemsRepeater
        // and ViewportManager is the same (see ItemsRepeater.m_viewportManager).
        // We can't simply hold a strong reference on ViewportManager because it's not a COM object.
        var strongOwner = m_owner.get_strong();
        m_cacheBuildAction.set(
            m_owner.Dispatcher().RunIdleAsync([this, strongOwner](const IdleDispatchedHandlerArgs&)
            {
                OnCacheBuildActionCompleted();
            }));
    }
}

void TryInvalidateMeasure()
{
    // Don't invalidate measure if we have an invalid window.
    if (m_visibleWindow != Rect())
    {
        // We invalidate measure instead of just invalidating arrange because
        // we don't invalidate measure in UpdateViewport if the view is changing to
        // avoid layout cycles.
        REPEATER_TRACE_INFO("%ls: \tInvalidating measure due to viewport change. \n", GetLayoutId().data());
        m_owner.InvalidateMeasure();
    }
}

string GetLayoutId() const
{
    if (var layout = m_owner.Layout())
    {
(        return layout as Layout).LayoutId();
    }

    return hstring{};
}
