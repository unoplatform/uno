// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollViewer_Partial.h, commit 5f9e85113

//  Abstract:
//      Represents a scrollable area that can contain other visible elements.

// Uncomment for DManip debug outputs.
//#define DM_DEBUG

// Uncomment for anchoring debug outputs.
//#define ANCHORING_DEBUG

#nullable disable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	// Represents a scrollable area that can contain other visible elements.
	partial class ScrollViewer // : ContentControl, IScrollOwner — base/interface declared on the main partial.
	{
		// #region Header constants (from C++ macros)

		// Default physical amount to scroll with Up/Down/Left/Right key
		internal const double ScrollViewerLineDelta = 16.0;

		// This value comes from WHEEL_DELTA defined in WinUser.h. It represents the universal default mouse wheel delta.
		internal const int ScrollViewerDefaultMouseWheelDelta = 120;

		// These macros compute how many integral pixels need to be scrolled based on the viewport size and mouse wheel delta.
		// - First the maximum between 48 and 15% of the viewport size is picked.
		// - Then that number is multiplied by (mouse wheel delta/120), 120 being the universal default value.
		// - Finally if the resulting number is larger than the viewport size, then that viewport size is picked instead.
		internal static double GetVerticalScrollWheelDelta(Size size, double delta)
			=> Math.Min(Math.Floor(size.Height), Math.Round(delta * Math.Max(48.0, Math.Round(size.Height * 0.15, 0)) / 120.0, 0));

		internal static double GetHorizontalScrollWheelDelta(Size size, double delta)
			=> Math.Min(Math.Floor(size.Width), Math.Round(delta * Math.Max(48.0, Math.Round(size.Width * 0.15, 0)) / 120.0, 0));

		// Minimum value of MinZoomFactor, ZoomFactor and MaxZoomFactor
		// ZoomFactor can be manipulated to a slightly smaller value, but
		// will jump back to 0.1 when the manipulation completes.
		internal const float ScrollViewerMinimumZoomFactor = 0.1f;

		// Tolerated rounding delta in pixels between requested scroll offset and
		// effective value. Used to handle non-DM-driven scrolls.
		internal const float ScrollViewerScrollRoundingTolerance = 0.05f;

		// Tolerated rounding delta in pixels between requested scroll offset and
		// effective value for cases where IScrollInfo is implemented by a
		// IManipulationDataProvider provider. Used to handle non-DM-driven scrolls.
		internal const float ScrollViewerScrollRoundingToleranceForProvider = 1.0f;

		// Delta required between the current scroll offsets and target scroll offsets
		// in order to warrant a call to BringIntoViewport instead of
		// SetOffsetsWithExtents, SetHorizontalOffset, SetVerticalOffset.
		internal const float ScrollViewerScrollRoundingToleranceForBringIntoViewport = 0.001f;

		// Tolerated rounding delta in between requested zoom factor and
		// effective value. Used to handle non-DM-driven zooms.
		internal const float ScrollViewerZoomExtentRoundingTolerance = 0.001f;

		// Tolerated rounding delta in between old and new zoom factor
		// in DM delta handling.
		internal const float ScrollViewerZoomRoundingTolerance = 0.000001f;

		// Delta required between the current zoom factor and target zoom factor
		// in order to warrant a call to BringIntoViewport instead of ZoomToFactor.
		internal const float ScrollViewerZoomRoundingToleranceForBringIntoViewport = 0.00001f;

		// When a snap point is within this tolerance of the ScrollViewer's extent
		// minus its viewport we nudge the snap point back into place.
		internal const float ScrollViewerSnapPointLocationTolerance = 0.0001f;

		// If a ScrollViewer is going to reflow around docked CoreInputView occlussions
		// by shrinking its viewport, we want to at least guarantee that it will keep
		// an appropriate size.
		internal const float ScrollViewerMinHeightToReflowAroundOcclusions = 32.0f;

		// #endregion

#if __SKIA__
#pragma warning disable CS0067 // Event never used
#pragma warning disable CS0169 // Field never used
#pragma warning disable CS0414 // Field assigned but never used
#pragma warning disable CS0649 // Field never assigned
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Private member is unused
#pragma warning disable IDE0052 // Private member can be removed because its value is never used
#pragma warning disable IDE0060 // Remove unused parameter

		// #region Private fields ported from ScrollViewer_Partial.h

		// Indicates whether the parent handles mouse buttons itself.
		// Note: matches existing m_templatedParentHandlesMouseButton in MuxInternal.cs (kept here for parity with C++).
		// private bool m_templatedParentHandlesMouseButton;

		// Indicates whether the parent handles scrolling itself.
		// Note: existing TemplatedParentHandlesScrolling property in MuxInternal.cs serves this purpose.
		// private bool m_templatedParentHandlesScrolling;

		// Apply our layout adjustments using a storyboard so that we don't stomp over template or user
		// provided values.  When we stop the storyboard, it will restore the previous values.
		private Storyboard m_trLayoutAdjustmentsForOcclusionsStoryboard;

		// Reference to the template root.
		private FrameworkElement m_trElementRoot;

		// Reference to the horizontal ScrollBar child.
		private ScrollBar m_trElementHorizontalScrollBar;

		// Reference to the vertical ScrollBar child.
		private ScrollBar m_trElementVerticalScrollBar;

		// Reference to the ScrollBar separator child.
		private UIElement m_tpElementScrollBarSeparator;

		// Manipulatable element exposed through IDirectManipulationContainerHandler
		private UIElement m_trManipulatableElement;

		// The main scrollable region.
		private ManagedWeakReference m_wrScrollInfo;

		// The touched element set during touch-based manipulation initialization.
		private ManagedWeakReference m_wrPointedElement;

		// A flag indicating whether we're currently in the measure pass.
		private bool m_inMeasure;

		// Flags indicating whether the horizontal and vertical ScrollBars are
		// visible.
		private Visibility m_scrollVisibilityX;
		private Visibility m_scrollVisibilityY;

		// Cached copies of the HorizontalOffset and VerticalOffset properties.
		private double m_xOffset;
		private double m_yOffset;

		// Minimal values of the HorizontalOffset and VerticalOffset properties.
		private double m_xMinOffset;
		private double m_yMinOffset;

		// Pixel-based versions of m_xOffset and m_yOffset for DM support.
		private double m_xPixelOffset;
		private double m_yPixelOffset;

		// Pixel-based unbound versions of m_xOffset and m_yOffset.
		private double m_unboundHorizontalOffset;
		private double m_unboundVerticalOffset;

		// Cached copies of the ViewportHeight and ViewportWidth properties.
		private double m_xViewport;
		private double m_yViewport;

		// Pixel-based versions of m_xViewport and m_yViewport for DM support.
		private double m_xPixelViewport;
		private double m_yPixelViewport;

		// Cached copies of the ExtentHeight and ExtentWidth properties.
		private double m_xExtent;
		private double m_yExtent;

		// Pixel-based versions of m_xExtent and m_yExtent for DM support.
		private double m_xPixelExtent;
		private double m_yPixelExtent;

		// Used by internal controls to apply a pseudo-LayoutTransform
		// to the ScrollViewer.Content element.
		// This layout size is treated by the ScrollContentPresenter as the desiredsize of its child.
		private Size m_layoutSize;

		// Latest availableSize provided to MeasureOverride. Used by the inner ScrollContentPresenter
		// when its SizesContentToTemplatedParent property is set to True. That available size replaces
		// infinity as the available size for its Content.
		private Size m_latestAvailableSize;

		// Event registration tokens for events attached to template parts so
		// the handlers can be removed if we apply a new template.
		// Uno: replaced with SerialDisposables so we can unhook on retemplate.
		private readonly SerialDisposable m_HorizontalScrollToken = new();
		private readonly SerialDisposable m_horizontalThumbDragStartedToken = new();
		private readonly SerialDisposable m_horizontalThumbDragCompletedToken = new();
		private readonly SerialDisposable m_VerticalScrollToken = new();
		private readonly SerialDisposable m_verticalThumbDragStartedToken = new();
		private readonly SerialDisposable m_verticalThumbDragCompletedToken = new();
		private readonly SerialDisposable m_verticalScrollbarPointerEnteredToken = new();
		private readonly SerialDisposable m_verticalScrollbarPointerExitedToken = new();
		private readonly SerialDisposable m_horizontalScrollbarPointerEnteredToken = new();
		private readonly SerialDisposable m_horizontalScrollbarPointerExitedToken = new();
		private readonly SerialDisposable m_coreInputViewOcclusionsChangedToken = new();

		// Whether we are in a state where we want to prevent the normal fade-out of the scrolling indicators.
		private bool m_keepIndicatorsShowing;

		// Whether a pointer is over the scrollbars. If the scrollbar does not exist then the value here will be false.
		private bool m_isPointerOverVerticalScrollbar;
		private bool m_isPointerOverHorizontalScrollbar;

		// Whether the first layout pass has completed and the control has been loaded.
		private bool m_isLoaded;

		// Whether we are currently dragging a thumb
		private bool m_isDraggingThumb;
		private double m_horizontalOffsetCached;
		private double m_verticalOffsetCached;

		// DirectManipulation-related storage

		// ScrollViewer content width and height requested during the latest DirectManipulation feedback processing
		private double m_contentWidthRequested;
		private double m_contentHeightRequested;

		// Horizontal and vertical offsets requested during the latest DirectManipulation feedback processing
		private double m_xPixelOffsetRequested;
		private double m_yPixelOffsetRequested;

		// These variables are used to keep track of which zoom properties have been updated/coerced, and when to
		// call the corresponding property changed methods.
		private uint m_cLevelsFromRootCallForZoom;
		private float m_initialMaxZoomFactor;
		private float m_initialZoomFactor;
		private float m_requestedMaxZoomFactor;
		private float m_requestedZoomFactor;
		private float m_preDirectManipulationOffsetX;
		private float m_preDirectManipulationOffsetY;
		private float m_preDirectManipulationZoomFactor;

		// Value used when the ScrollViewer contains a IManipulationDataProvider while its non-virtualizing alignment is center or far,
		// and the panel in that direction is smaller than the viewport.
		private float m_preDirectManipulationNonVirtualizedTranslationCorrection;

		// Value of the HorizontalScrollMode property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private ScrollMode m_currentHorizontalScrollMode;

		// Value of the VerticalScrollMode property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private ScrollMode m_currentVerticalScrollMode;

		// Value of the ZoomMode property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private ZoomMode m_currentZoomMode;

		// Value of the horizontal scrollbar visibility property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private ScrollBarVisibility m_currentHorizontalScrollBarVisibility;

		// Value of the vertical scrollbar visibility property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private ScrollBarVisibility m_currentVerticalScrollBarVisibility;

		// Value of the DManip horizontal alignment when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private DMAlignment m_currentHorizontalAlignment;

		// Value of the DManip vertical alignment when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private DMAlignment m_currentVerticalAlignment;

		// Value of the IsHorizontalRailEnabled property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private bool m_currentIsHorizontalRailEnabled;

		// Value of the IsVerticalRailEnabled property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private bool m_currentIsVerticalRailEnabled;

		// Value of the IsScrollInertiaEnabled property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private bool m_currentIsScrollInertiaEnabled;

		// Value of the IsZoomInertiaEnabled property when a DM manip starts. This will be the value used until the end
		// of that manipulation.
		private bool m_currentIsZoomInertiaEnabled;

		// IScrollSnapPointsInfo implementation of the ScrollContentPresenter's content
		private IScrollSnapPointsInfo m_trScrollSnapPointsInfo;

		// Event token for the IPSPI HorizontalSnapPointsChanged/VerticalSnapPointsChanged events
		// Uno: replaced with SerialDisposables.
		private readonly SerialDisposable m_HorizontalSnapPointsChangedToken = new();
		private readonly SerialDisposable m_VerticalSnapPointsChangedToken = new();

		// Callback object used by a single listener that wants to be aware of DM state changes
		// TODO Uno: Original C++ uses raw pointer; Uno uses interface reference.
		private IDirectManipulationStateChangeHandler m_pDMStateChangeHandler;

		// HANDLE m_hManipulationHandler — Native HANDLE in C++; Uno uses an object reference for the handler.
		// TODO Uno: Original C++ stores a raw HANDLE bound to the input manager. In Uno, manipulation
		// dispatch is performed via GestureRecognizer + InputManager; this field is unused on managed targets.
		private object m_hManipulationHandler;

		// Last values returned by ScrollViewer::get_CanManipulateElements.
		// Used to determine if IDirectManipulationContainerHandler.NotifyCanManipulateElements
		// needs to be called when a DM-affecting characteristic changes.
		private bool m_canManipulateElementsByTouch;   // True means user can manipulated content with touch
		private bool m_canManipulateElementsNonTouch;  // True means user can move content with keyboard or mouse wheel
		private bool m_canManipulateElementsWithBringIntoViewport;      // True means developer can move content with programmatic call to BringIntoViewport
		private bool m_canManipulateElementsWithAsyncBringIntoViewport; // True means BringIntoViewport can perform asynchronous view changes

		// Last values returned by ScrollViewer::GetManipulationViewport.
		// Used to determine if IDirectManipulationContainerHandler.NotifyViewportChanged
		// needs to be called when a DM-affecting characteristic changes.
		private DMConfigurations m_touchConfiguration;    // DM configuration used for touch-based manipulations
		private DMConfigurations m_nonTouchConfiguration; // DM configuration used for keyboard/mouse wheel operations

		// Last alignment values returned by ScrollViewer::GetManipulationPrimaryContent.
		// Used to determine if ScrollViewer::OnContentAlignmentAffectingPropertyChanged()
		// must call OnPrimaryContentAffectingPropertyChanged or not.
		private DMAlignment m_activeHorizontalAlignment;
		private DMAlignment m_activeVerticalAlignment;

		// Temporary workaround for DManip bug 799346
		private float m_overridingMinZoomFactor;
		private float m_overridingMaxZoomFactor;

		// Stores the DManip viewport state as of the last notification.
		private DMManipulationState m_dmanipState;

		// Set to True in the DMManipulationStarting notification processed in NotifyManipulationProgress.
		// Reset to False in the DMManipulationCompleted notification.
		private bool m_isInDirectManipulation;

		// Set to True when a call to StopInertialManipulation interrupted a manipulation in inertia phase.
		private bool m_isDirectManipulationStopped;

		// Set to True in the DMManipulationCompleted notification processed in NotifyManipulationProgress.
		// Reset to False in the DMManipulationStarting notification, or in PostDirectManipulationLayoutRefreshed when a layout refresh occurs.
		private bool m_isInDirectManipulationCompletion;

		// Set to True when a DirectManipulation-driven zoom factor change occurred during
		// the current manipulation.
		private bool m_isInDirectManipulationZoom;

		// Set to True when a synchronization between the XAML transform and DManip transform is in progress (i.e. XAML is pushing an updated
		// transform to DManip with a ZoomToRect call).
		private bool m_isInDirectManipulationSync;

		// Set to True when a ChangeView's BringIntoViewport call is in progress.
		private bool m_isInChangeViewBringIntoViewport;

		// Set to True when a ZoomToFactor call changed the zoom factor and DManip is being updated accordingly.
		private bool m_isInZoomFactorSync;

		// Note: m_isInConstantVelocityPan exists in MuxInternal partial; suppressed here.

		private bool m_isManipulationHandlerInterestedInNotifications;

		// Indicates whether the zoom factor change is triggered by a DM feedback or a programmatic call.
		private bool m_isDirectManipulationZoomFactorChange;

		// Set to True when the zoom factor is programmatically changed during a DM manipulation
		// Reset at the end of that manipulation
		private bool m_isDirectManipulationZoomFactorChangeIgnored;

		// Set to True when any unexpected horizontal or vertical
		// offset change must not be reported to the manipulation handler.
		private bool m_isOffsetChangeIgnored;

		// Set to True when a configurations-affecting property is changed during a manipulation.
		// In this case the new configurations need to to be pushed to the ManipulationHandler
		// once the ongoing manipulation completes.
		private bool m_areViewportConfigurationsInvalid;

		// Set to True during a manipulation when the manipulability of the elements might be FALSE
		// once the ongoing manipulation completes.
		private bool m_isCanManipulateElementsInvalid;

		// Set to True during a manipulation when at least one ScrollBar's IsIgnoringUserInput flag needs to be updated
		// once the ongoing manipulation completes.
		private bool m_isScrollBarIgnoringUserInputInvalid;

		// Whether the pointer left button pressed or not.
		// Note: existing m_isPointerLeftButtonPressed exists in MuxInternal partial.

		// In OnPointerReleased, we want to call Focus and Handle the event.
		// If a right-tap is pending on PointerReleased, we must defer these actions
		// to RightTappedUnhandled so context menus function properly.
		private bool m_shouldFocusOnRightTapUnhandled;

		// Whether the mouse scrolling indicators are currently showing.
		private bool m_showingMouseIndicators;

		// Whether we are in a state that should show mouse indicators for scrolling (as opposed to panning indicators).
		private bool m_preferMouseIndicators;

		// Whether we are blocking all indicators (during SeZo switch)
		private bool m_blockIndicators;

		// Set to True when the respective three fields m_targetHorizontalOffset/m_targetVerticalOffset/m_targetZoomFactor
		// contain up-to-date values.
		private bool m_isTargetHorizontalOffsetValid;
		private bool m_isTargetVerticalOffsetValid;
		private bool m_isTargetZoomFactorValid;

		// Values representing the transform about to be applied - used for the ViewChanging notifications
		private double m_targetHorizontalOffset;
		private double m_targetVerticalOffset;
		private float m_targetZoomFactor;

		// Values representing the latest view requested by ChangeView. When no view change is pending, this trio is equal to (-1, -1, -1)
		// When a ViewChanging event is raised, the values go back to -1.
		private double m_targetChangeViewHorizontalOffset = -1;
		private double m_targetChangeViewVerticalOffset = -1;
		private float m_targetChangeViewZoomFactor = -1;

		// Values representing the end of inertia transform - used for the ViewChanging notifications
		private double m_inertiaEndHorizontalOffset;
		private double m_inertiaEndVerticalOffset;
		private float m_inertiaEndZoomFactor;

		// Indicates whether the inertia end transform above contains valid values that can be exposed in the ViewChanging event
		private bool m_isInertiaEndTransformValid;

		// Indicates whether the ScrollViewer is operating in inertia mode. Any ViewChanging notification, not matter its trigger,
		// uses m_isInertial to populate its ScrollViewerViewChanging.IsInertial property.
		private bool m_isInertial;

		// Set to True when we are batching up HorizontalOffset, VerticalOffset & ZoomFactor change notifications
		// into a single ViewChanging event.
		private bool m_isViewChangingDelayed;

		// Set to True when we are attempting to batch up HorizontalOffset, VerticalOffset & ZoomFactor change notifications
		// into a single ViewChanged event.
		private bool m_isViewChangedDelayed;

		// Set to True when the latest delayed ViewChanged event uses the IsIntermediate flag.
		private bool m_isDelayedViewChangedIntermediate;

		// Set to True when any ViewChanged event raised in the HorizontalOffset, VerticalOffset and ZoomFactor property
		// change handlers needs to specify ScrollViewerViewChangedEventArgs.IsIntermediate==True.
		private bool m_isInIntermediateViewChangedMode;

		// Set to True when a ViewChanged event is raised while the control is in Intermediate mode (i.e. m_isInIntermediateViewChangedMode==True)
		// Determines whether ViewChanged needs to be raised with ScrollViewerViewChangedEventArgs.IsIntermediate==False when
		// m_isInIntermediateViewChangedMode switches back to False.
		private bool m_isViewChangedRaisedInIntermediateMode;

		// When strictly positive, it prevents the ViewChanging event from being raised. It is delayed and raised
		// when m_iViewChangingDelay reaches 0.
		private int m_iViewChangingDelay;

		// When strictly positive, it prevents the ViewChanged event from being raised. It is delayed and raised
		// when m_iViewChangedDelay reaches 0.
		private int m_iViewChangedDelay;

		// Whether or not the ScrollViewer should mark as Handled the next PointerWheelChanged message.
		// This is a TEMPORARY measure for internal IScrollInfo implementations that want to allow wheel
		// messages to keep routing.
		private bool m_handleScrollInfoWheelEvent;

		// We cannot invalidate the grandchild directly. So this property is
		// informing that we are invalidating the child so the grandchild can
		// use it.
		private bool m_inChildInvalidateMeasure;

		// A value indicating whether the ScrollViewer should ignore any input
		// that would be used by a SemanticZoom to navigate between views.
		private bool m_ignoreSemanticZoomNavigationInput;

		// Set to True when this ScrollViewer successfully marked a header as associated.
		private bool m_isTopLeftHeaderAssociated;
		private bool m_isTopHeaderAssociated;
		private bool m_isLeftHeaderAssociated;

		// Overpan mode for the horizontal direction.
		private DMOverpanMode m_horizontalOverpanMode;

		// Overpan mode for the vertical direction.
		private DMOverpanMode m_verticalOverpanMode;

		// Work around for ScrollViewer / ScrollContentPresenter bug Windows Blue 358691
		// Used only by Hub to force "Near" vertical alignment
		private bool m_isNearVerticalAlignmentForced;

		// Note: m_arePointerWheelEventsIgnored handled by ArePointerWheelEventsIgnored property in MuxInternal partial.

		private bool m_isRequestBringIntoViewIgnored;

		// Indicates whether the NoIndicator visual state has a Storyboard for which a completion event was hooked up.
		private bool m_hasNoIndicatorStateStoryboardCompletedHandler;

		// Set to True when our layout engine treats a Stretch alignment as a Left/Top alignment.
		// This typically occurs for text controls.
		private bool m_isHorizontalStretchAlignmentTreatedAsNear;
		private bool m_isVerticalStretchAlignmentTreatedAsNear;

		// Fields used for scenarios of WPB bugs 261102 and 342668.
		private byte m_cForcePanXConfiguration; // GetManipulationConfigurations artificially includes PanX configuration when m_cForcePanXConfiguration > 0
		private byte m_cForcePanYConfiguration; // GetManipulationConfigurations artificially includes PanY configuration when m_cForcePanYConfiguration > 0

		// Event Handler for BringIntoViewRequestedEvent
		private TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> m_bringIntoViewRequestedHandler;

		// A value indicating whether the ScrollViewer should ignore any input
		// whatsoever (so that it does not conflict with the sezo animation).
		internal bool m_inSemanticZoomAnimation;

		// Allow to set focus on ScrollViewer itself. For example, Flyout inner ScrollViewer.
		// Note: m_isFocusableOnFlyoutScrollViewer exists in MuxInternal partial.

		// Reference to the ScrollContentPresenter child.
		// Cross-platform code already populates `_presenter` in OnApplyTemplate; this field is kept in sync
		// with that on Skia for parity with the C++ source.
		private ScrollContentPresenter m_trElementScrollContentPresenter;

		// #endregion

		// #region Anchoring fields (declared at the bottom of the C++ class)

		// We cache these values at the end of ArrangeOverride since the one's tracked by ScrollViewer's extent
		// and viewport properties get set at weird times and is unreliable to use for tracking shifts.
		// NOTE: Already present on the existing ScrollViewer.Anchoring partial. Kept here for parity.
		// internal double m_unzoomedExtentWidth;
		// internal double m_unzoomedExtentHeight;
		// internal double m_viewportWidth;
		// internal double m_viewportHeight;

		// TrackerPtr<wfc::IVector<xaml::UIElement*>> m_anchorCandidates;
		// TrackerPtr<wfc::IVector<xaml::UIElement*>> m_anchorCandidatesForArgs;
		// bool m_useCandidatesFromArgs;
		// bool m_isAnchorElementDirty;
		// TrackerPtr<xaml::IUIElement> m_anchorElement;
		// wf::Rect m_anchorElementBounds;
		// TrackerPtr<xaml_controls::IAnchorRequestedEventArgs> m_anchorRequestedEventArgs;
		// double m_pendingViewportShiftX;
		// double m_pendingViewportShiftY;

		// #endregion

		// #region Inline methods from the C++ header (those with bodies in ScrollViewer_Partial.h)

		internal bool IsManipulationHandlerReady() => m_hManipulationHandler != null;

		// Note: IsInDirectManipulation() and IsInManipulation() are exposed via the existing MuxInternal partial.
		internal bool IsInDirectManipulationCore() => m_isInDirectManipulation;

		// Skia-side bridges: SCP.Managed.cs IDirectManipulationHandler events flip the
		// underlying m_isInDirectManipulation flag so the rest of the SV state machine
		// (snap-points reaction, OnPrimaryContentChanged DM gates, etc.) behaves
		// correctly during touch-driven scroll. The Phase-4 DM adapter port will replace
		// these with full HandleManipulationStarting/Delta/Completed invocations.
		internal void NotifyDirectManipulationStarting()
		{
			m_isInDirectManipulation = true;
			m_isDirectManipulationStopped = false;
		}

		internal void NotifyDirectManipulationCompleted()
		{
			m_isInDirectManipulation = false;
		}

		// A direct manipulation in inertia phase can be interrupted via a call to StopInertialManipulation().
		// At that point the m_isDirectManipulationStopped flag is set to True and IsInUnstoppedManipulation()
		// starts to return False instead of True. Once the state change to DMManipulationCompleted is processed,
		// m_isInDirectManipulation is set to False.
		internal bool IsInUnstoppedManipulation() =>
			(m_isInDirectManipulation && !m_isDirectManipulationStopped) || m_isInConstantVelocityPan;

		internal bool IsInDirectManipulationCompletion() => m_isInDirectManipulationCompletion;

		internal bool IsInDirectManipulationZoom() => m_isInDirectManipulationZoom;

		internal float GetPreDirectManipulationOffsetX() => m_preDirectManipulationOffsetX;

		internal float GetPreDirectManipulationOffsetY() => m_preDirectManipulationOffsetY;

		internal float GetPreDirectManipulationZoomFactor() => m_preDirectManipulationZoomFactor;

		internal double GetPixelHorizontalOffset() => m_xPixelOffset;

		internal double GetPixelVerticalOffset() => m_yPixelOffset;

		internal double GetUnboundHorizontalOffset() => m_unboundHorizontalOffset;

		internal double GetUnboundVerticalOffset() => m_unboundVerticalOffset;

		internal Size GetLayoutSize() => m_layoutSize;

		// Used by the inner ScrollContentPresenter when its SizesContentToTemplatedParent property is set to True.
		internal Size GetLatestAvailableSize() => m_latestAvailableSize;

		// Workaround for Windows Phone Blue bug 273985.
		// TODO: Threshold task 946804 - Re-evaluate the need for workaround in Pivot ctrl when DManip-on-DComp is on
		internal void SetIsNearVerticalAlignmentForcedImpl(bool enabled) => m_isNearVerticalAlignmentForced = enabled;

		// Removes the potential duplicate DMConfigurationPanInertia flag. The duplication occurs when both horizontal and vertical
		// panning are allowed, and pan inertia is turned on.
		private static DMConfigurations RemoveDuplicatePanInertia(
			bool isScrollInertiaEnabled,
			DMConfigurations panXConfiguration,
			DMConfigurations panYConfiguration)
		{
			if (isScrollInertiaEnabled && panXConfiguration != DMConfigurations.None && panYConfiguration != DMConfigurations.None)
			{
				return (DMConfigurations)((int)panXConfiguration + (int)panYConfiguration - (int)DMConfigurations.PanInertia);
			}
			return (DMConfigurations)((int)panXConfiguration + (int)panYConfiguration);
		}

		// TODO Uno: DXamlCore::ShouldUseDynamicScrollbars equivalent — likely always true in Uno.
		private static bool IsConscious() => true;

		// Inline header virtuals — root-ScrollViewer specializations override these. Default to false
		// since regular ScrollViewers are never the root ScrollViewer.
		protected virtual bool IsRootScrollViewer() => false;
		protected virtual bool IsRootScrollViewerAllowImplicitStyle() => false;
		protected virtual bool IsInputPaneShow() => false;

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
