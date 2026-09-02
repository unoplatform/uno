// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoom_Partial.h, commit dc46907e92

//  Abstract:
//      Represents a scrollable area that can contain either a ZoomedInView of
//      content or a ZoomedOutView used to navigate around the content via zoom
//      gestures.

#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Represents a scrollable area that can contain either a normal view of
// content or a SemanticZoom used to navigate around the content via zoom
// gestures.
partial class SemanticZoom
{
	private enum SemanticZoomPhase
	{
		SemanticZoomPhase_Idle,
		// we are switching because of an API call (mousewheel, api or a click).
		// we will stay in this phase until we have completed the animation
		SemanticZoomPhase_API_SwitchingViews,
		// we are switching because of DM: we have been notified of a zoomchange
		// and we have passed the threshold.
		SemanticZoomPhase_DM_SwitchingViews,
		// we have let go of the fingers and currently we are animating the
		// viewport from that location/zf to the regular zf
		SemanticZoomPhase_DM_CompletingViews
	}

	// Reference to the template part hosting the ZoomedInView.
	private FrameworkElement? m_tpZoomedInPresenterPart;

	// Reference to the template part hosting the ZoomedOutView.
	private FrameworkElement? m_tpZoomedOutPresenterPart;

	// Reference to the template parts used to offset the views and apply scales
	private CompositeTransform? m_tpZoomedOutTransform;
	private CompositeTransform? m_tpZoomedInTransform;
	private CompositeTransform? m_tpManipulatedElementTransform;

	// Reference to the scrollViewer
	private ScrollViewer? m_tpScrollViewer;

	// Reference to the ZoomOutButton template part
	private Button? m_tpZoomOutButton;

	// Reference to a throw-away timer that will trigger visibility on the alternate view (perf)
	private DispatcherTimer? m_tpAlternateViewTimer;

	// This field is set to TRUE while this SemanticZoom is being initialized (according to the ISupportInitialize
	// interface). It is FALSE during all other times.
	private bool m_isInitializing;

	// If the IsZoomedInViewActive property changes while SemanticZoom is still being initialized, this flag will
	// be set and the view change postponed. When initialization completes (EndInit), the view is changed to the
	// correct one. This avoids the problems incurred when the view is changed when one or both views are not
	// initialized yet.
	private bool m_isPendingViewChange;

	// Whether we are currently processing a keyboard input event.
	private bool m_isProcessingKeyboardInput;

	// Whether we are currently processing a pointer input event.
	private bool m_isProcessingPointerInput;

	// Whether we are currently cancelling a JumpList (ex: processing back button)
	private bool m_isCancellingJumpList;

	// args to use when the view change is complete. Created during ViewChange.
	private SemanticZoomViewChangedEventArgs? m_tpCompletedArgs;

	// pre-calculated caches for the zoompoints used for the different views
	private Point m_zoomPoint;                 // actual centerpoint as registered
	private Point m_zoomPointForZoomedInView;  // point as used foro the zoomedinview
	private Point m_zoomPointForZoomedOutView; // point as used foro the zoomedoutview

	// indicates which view was active when we started zooming
	private bool m_zoomOriginatesFromZoomedInView;
	// naming is identical to DUI implementation, please keep as-is
	private float _upperThresholdLow;
	private float _upperThresholdHigh;
	private float _lowerThresholdLow;
	private float _lowerThresholdHigh;

	// These fields indicate whether we are able to animate to a particular state
	private bool m_isZoomedInViewAnimationHooked;
	private bool m_isZoomedOutViewAnimationHooked;

	// indicates whether we are imitating gestures (ctrl-mousewheel)
	private bool m_emulatingGesture;

	// the phase we are in, this can be something like idle, 'changing because of API calls'
	private SemanticZoomPhase m_changePhase;
	// hack to allow us to temporarily lock the phase so that the property change method
	// will not force it to API. Used when calling the property change because of DM input (fingers).
	private bool m_phaseChangeLockDuringViewSwitch;

	private Point m_manipulatedElementOffset;

	// indicates that we have performed the Initialize calls, which means we will have to close with Complete calls
	private bool m_calledInitializeViewChangeSinceManipulationStart;

	// the value that we compare against to determine if zoom is truly occurring.
	// this is important in the 'interrupted' scenarios where we were in a zoom animation and the
	// user has put his finger down again to for instance start dragging. The cumulative factor now
	// is just what the zoom happened to be at the moment of interruption, because that value is
	// only reset at the start of viewport creation.
	private float m_cumulativeZoomFactorAtStartOfManipulation;

	// Set when we call ChangeViews(), and cleared in OnViewChangeCompleted().
	private bool m_isProcessingViewChange;

	// Cached value of IsZoomOutButtonEnabled property, to avoid access penalty when proccessing OnPointerMoved().
	private bool m_isZoomOutButtonEnabled;

	// Prevents creation of an automation peer until
	// requested by automation as an optimization.
	private bool m_hasAutomationPeer;

	// Keyboard and Pointer input can cause a change in focus inside SemanticZoom.
	// While inside of a method handling input processing, we set a flag indicating whether
	// that input is from keyboard or a flag indicating whether that input is from pointer.
	// Then, when Focus(FocusState) gets called, we check these flags to determine the
	// correct FocusState to pass in.
	internal bool GetIsProcessingKeyboardInput() => m_isProcessingKeyboardInput;

	internal bool GetIsProcessingPointerInput() => m_isProcessingPointerInput;

	internal bool GetIsCancellingJumpList() => m_isCancellingJumpList;

	// in several location we have to calculate how the zoomedinview is centered
	private double ZoomedInCenteringCorrectionX(bool inZoomedInView, double viewportWidth) =>
		inZoomedInView ? viewportWidth * 0.5 : 0;

	private double ZoomedInCenteringCorrectionY(bool inZoomedInView, double viewportHeight) =>
		inZoomedInView ? viewportHeight * 0.5 : 0;

	private void ClearCompletedEventArgs() => m_tpCompletedArgs = null;
}
