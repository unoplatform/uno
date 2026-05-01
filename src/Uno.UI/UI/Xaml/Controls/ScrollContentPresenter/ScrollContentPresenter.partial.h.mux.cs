// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollContentPresenter_Partial.h, commit 5f9e85113

// Uncomment for DManip debug outputs.
//#define DM_DEBUG

#nullable disable

using DirectUI;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	// Displays the content of a ScrollViewer control.
	partial class ScrollContentPresenter // : ContentPresenter — base declared on the main partial.
	{
#if __SKIA__
#pragma warning disable CS0067 // Event never used
#pragma warning disable CS0169 // Field never used
#pragma warning disable CS0414 // Field assigned but never used
#pragma warning disable CS0649 // Field never assigned
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Private member is unused
#pragma warning disable IDE0052 // Private member can be removed because its value is never used
#pragma warning disable IDE0060 // Remove unused parameter

		// #region Private fields ported from ScrollContentPresenter_Partial.h

		// Clipping rectangle used to replace WPF's GetLayoutClip virtual
		private RectangleGeometry m_tpClippingRectangle;

		// Flag indicating whether the Clip has been set.
		private bool m_isClipPropertySet;

		// Flags indicating whether the individual headers were added as
		// children of this ScrollContentPresenter.
		private bool m_isTopLeftHeaderChild;
		private bool m_isTopHeaderChild;
		private bool m_isLeftHeaderChild;

		private bool m_isTabIndexSet;
		private int m_tabIndex;

		// Tracked references to the three header elements.
		private UIElement m_trTopLeftHeader;
		private UIElement m_trTopHeader;
		private UIElement m_trLeftHeader;

		// Reference to the main scrollable region being presented (it could
		// be this).
		// Note: existing _scroller field already stores a weak reference; reuse it.
		// private ManagedWeakReference m_wrScrollInfo;

		// The state necessary to scroll the content.
		private ScrollData m_pScrollData;

		// InputPaneThemeTransition*
		private Microsoft.UI.Xaml.Media.Animation.Transition m_tpInputPaneThemeTransition;

		// Flag indicating whether the m_isInputPaneShow has been set.
		private bool m_isInputPaneShow;

		// this field will be used later as we finish discovering all scenarios where we need to propagate new zoom factor.
		// IScrollOwner.GetZoomFactor API will go away. And for now we use result from that API with ASSERT comparing with the field.
		// Zoom factor from SetZoomFactor
		private float m_fZoomFactor = 1.0f;

		// Zoom factor applied in the most recent layout pass.
		private float m_fLastZoomFactorApplied = 1.0f;

		// Flag indicating whether this instance is used by a semantic zoom control
		private bool m_isSemanticZoomPresenter;

		// Extents that were not pushed to the ScrollViewer in ScrollContentPresenter::MeasureOverride because
		// the m_isChildActualWidthUsedAsExtent/m_isChildActualWidthUsedAsExtent flags were set to True.
		private Size m_unpublishedExtentSize;

		// Set to False by default. Exceptionally set to True when the Content's actual size is used as the IScrollInfo extent.
		private bool m_isChildActualWidthUsedAsExtent;
		private bool m_isChildActualHeightUsedAsExtent;

		// Set to True by default. Exceptionally set to False when m_isChildActualWidthUsedAsExtent is set to True and the IScrollInfo extent width
		// is not yet up-to-date when ScrollViewer::InvalidateScrollInfo is invoked.
		private bool m_isChildActualWidthUpdated = true;
		// Set to True by default. Exceptionally set to False when m_isChildActualHeightUsedAsExtent is set to True and the IScrollInfo extent height
		// is not yet up-to-date when ScrollViewer::InvalidateScrollInfo is invoked.
		private bool m_isChildActualHeightUpdated = true;

		// When a scroll offset change is requested, the primary notification mechanism for carrying out the change
		// is by invalidating arrange. However, if a child requests a scroll during arrange, the invalidation is never
		// seen, as the flag is cleared in the core after our arrange completes. This allows us to make another arrange pass.
		private bool m_scrollRequested;

		// #endregion

		// #region Inline methods from the C++ header (those with bodies in ScrollContentPresenter_Partial.h)

		// Overriding this method and returning TRUE in order to navigate among automation children
		// of content and headers in reverse order.
		// TODO Uno: AreAutomationPeerChildrenReversed exists on UIElement; SCP-specific override TBD when AP work lands.
		// internal bool AreAutomationPeerChildrenReversed() => true;

		// Returns the zoom factor applied in the most recent layout pass.
		internal float GetLastZoomFactorApplied() => m_fLastZoomFactorApplied;

		internal bool IsTopLeftHeaderChild_New => m_isTopLeftHeaderChild;
		internal bool IsTopHeaderChild_New => m_isTopHeaderChild;
		internal bool IsLeftHeaderChild_New => m_isLeftHeaderChild;

		// Return true if the child's actual Width size is used for the
		// extent exposed through IScrollInfo
		internal bool IsChildActualWidthUsedAsExtent() => m_isChildActualWidthUsedAsExtent;

		// Return true if the child's actual Height size is used for the
		// extent exposed through IScrollInfo
		internal bool IsChildActualHeightUsedAsExtent() => m_isChildActualHeightUsedAsExtent;

		// Called from the ScrollViewer's InvalidateScrollInfo() implementation when IsChildActualWidthUsedAsExtent() above returned True.
		// Is used to determine whether the IScrollInfo extent width is already up-to-date or not. It may not be up-to-date in a MeasureOverride VerifyScrollData call.
		internal bool IsChildActualWidthUpdated()
		{
			global::System.Diagnostics.Debug.Assert(m_isChildActualWidthUsedAsExtent);
			return m_isChildActualWidthUpdated;
		}

		// Called from the ScrollViewer's InvalidateScrollInfo() implementation when IsChildActualHeightUsedAsExtent() above returned True.
		// Is used to determine whether the IScrollInfo extent height is already up-to-date or not. It may not be up-to-date in a MeasureOverride VerifyScrollData call.
		internal bool IsChildActualHeightUpdated()
		{
			global::System.Diagnostics.Debug.Assert(m_isChildActualHeightUsedAsExtent);
			return m_isChildActualHeightUpdated;
		}

		// #endregion

#pragma warning restore IDE0060
#pragma warning restore IDE0052
#pragma warning restore IDE0051
#pragma warning restore IDE0044
#pragma warning restore CS0649
#pragma warning restore CS0414
#pragma warning restore CS0169
#pragma warning restore CS0067
#endif
	}
}
