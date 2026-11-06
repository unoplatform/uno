// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemsPresenter_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsPresenter
#if !HAS_UNO // TODO Uno: IManipulationDataProvider and IScrollOwner have no Uno consumer yet
	: IManipulationDataProvider, IScrollOwner
#endif
{
	internal ContentControl? HeaderContainer => HeaderContentControl;

	internal ContentControl? FooterContainer => FooterContentControl;

	// Used by GetFirstVisiblePart callers.
	internal enum ItemsPresenterParts
	{
		Header,
		Panel,
		Footer
	}

	// Special indices used when GetSentinelItemSize is called.
	private const int PanelSentinelIndex = -3;
	private const int FooterSentinelIndex = -2;
	private const int TrailingPaddingSentinelIndex = -1;
	private const int LeadingPaddingSentinelIndex = 0;
	private const int HeaderSentinelIndex = 1;

#if !HAS_UNO // TODO Uno: ScrollData backs the IScrollInfo and IScrollOwner implementations
	private ScrollData m_ScrollData;
#endif

	// Header and footer: see HeaderContentControl and FooterContentControl.

	private bool m_bCanLoadFooter;                       // Specifies whether we can load the footer.
	private WeakReference<ScrollViewer>? m_wrScrollViewer; // Weak ref to our ScrollViewer.

	// Specifies how close from the footer we should be before
	// we load it.
	private const double FooterDelayLoadOffset = 200.0;

	// Used to retrieve the location of the header for GetFirstVisiblePart.
	private Rect m_lastHeaderArrangeRect;
	// Used to retrieve the location of the footer for the footer delay loading
	// feature and for GetFirstVisiblePart.
	private Rect m_lastFooterArrangeRect;
	// Alongside m_lastHeaderArrangeRect and m_lastFooterArrangeRect, used to determine
	// if we need to notify that the snap points might have changed.
	private Rect m_lastPanelArrangeRect;

	private bool m_bNotifyHorizontalSnapPointsChanges;     // True when NotifySnapPointsChanged needs to be called when horizontal snap points change
	private bool m_bNotifyVerticalSnapPointsChanges;       // True when NotifySnapPointsChanged needs to be called when vertical snap points change
	private bool m_bNotifiedHorizontalSnapPointsChanges;   // True when NotifySnapPointsChanged was already called once and horizontal snap points have not been accessed since
	private bool m_bNotifiedVerticalSnapPointsChanges;     // True when NotifySnapPointsChanged was already called once and vertical snap points have not been accessed since
	private bool m_bAreSnapPointsKeysHorizontal;           // True when the snap point keys are for horizontal snap points
	private double m_leadingSnapPointKey;                  // Top/left padding dimension used to compute regular and irregular snap points
	private double m_trailingSnapPointKey;                 // Bottom/right padding dimension used to compute regular and irregular snap points
	private double m_leadingMarginSnapPointKey;            // Top/left panel's margin dimension used to compute regular and irregular snap points
	private double m_trailingMarginSnapPointKey;           // Bottom/right panel's margin dimension used to compute regular and irregular snap points
	private float m_irregularSnapPointKeysOffset;          // Dimension of the unrealized children ahead of the realized children ones
	private float m_regularSnapPointKey;                   // Unique identifier for regular snap points (independent of snap point alignment)
	private int m_cIrregularSnapPointKeys;                 // Number of irregular snap point keys
	private float[]? m_pIrregularSnapPointKeys;            // Unique identifiers for irregular snap points (independent of snap point alignment)

#if !HAS_UNO // TODO Uno: only the IScrollOwner and IManipulationDataProvider paths read the zoom factor
	// this field will be used later as we finish discovering all scenarios where we need to propagate new zoom factor.
	// IScrollOwner.GetZoomFactor API will go away. And for now we use result from that API with ASSERT comparing with the field.
	private float m_fZoomFactor = 1;                       // Zoom factor from SetZoomFactor
#endif

	// IScrollSnapPointsInfo implementation of the ItemsPresenter's panel
	private IScrollSnapPointsInfo? m_tpScrollSnapPointsInfo;

	// Event handlers for the IPSPI HorizontalSnapPointsChanged/VerticalSnapPointsChanged events
	private EventHandler<object>? m_spHorizontalSnapPointsChangedEventHandler;
	private EventHandler<object>? m_spVerticalSnapPointsChangedEventHandler;

	// Backing fields of the HorizontalSnapPointsChanged/VerticalSnapPointsChanged events.
	private EventHandler<object>? m_horizontalSnapPointsChanged;
	private EventHandler<object>? m_verticalSnapPointsChanged;

	// Attach to the ScrollViewer on loaded event and unattach on unloaded: see Initialize.

	internal static double OffsetToIndex(double offset)
	{
		return Math.Max(0, offset - 2);
	}

	internal static double IndexToOffset(int index)
	{
		return index >= 0 ? index + 2 : 0;
	}
}
