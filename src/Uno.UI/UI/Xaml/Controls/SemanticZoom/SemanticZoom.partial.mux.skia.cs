// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoom_Partial.cpp, commit dc46907e92

//  Abstract:
//      Represents a scrollable area that can contain either a zoomed in view of
//      content or a zoomed out view used to navigate around the content via zoom
//      gestures.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Internal;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using static Microsoft.UI.Xaml.Controls._Tracing;

// Uncomment to get SemanticZoom debug traces
// #define SEZO_DBG

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	// values that influence the switching point
	// naming to match DUI implementation. Please leave as-is.
	// these values should be kept in sync with the DUI values once they
	// have been chosen
	private const float c_thresholdDeltaMin = 0.05f;
	private const float c_thresholdDeltaMax = 0.95f;
	private const float c_thresholdBufferDeltaMin = 0.01f;
	private const float c_thresholdBufferDeltaMax = 0.10f;
	private const float _upperThresholdDelta = 0.05f;
	private const float _lowerThresholdDelta = 0.05f;
	private const float _thresholdBufferDelta = 0.05f;

	private const float zoomDeltaThreshold = 0.01f; // No semantic switch will occur if the zoom factor has changed less than this value

	// the ScrollViewer will apply this zoomfactor in the zoomed-in view
	private const float _zoomMax = 1.0f;
	// the ScrollViewer will apply this zoomfactor in the zoomed-out view
	private const float _zoomMin = 0.5f;

	// These values are used by tracing to note how a user zoomed in or out
	private const short _zoomedClick = 0;
	private const short _zoomedWheel = 1;
	private const short _zoomedPinch = 2;

	// Calling BringIntoViewport unnecessarily is an issue
	// these constants define the minimum delta we want to see before
	// making the call
	private const double c_minimumZoomDelta = 0.001;
	private const double c_minimumBoundsDelta = 1.0;

	// in several location we have to calculate how the zoomedinview is centered

	// Initializes a new instance of the SemanticZoom class.
	public SemanticZoom()
	{
		// Uno-specific: the default style lives in the WinUI v2 theme resources, so the
		// control has to advertise that dictionary for implicit-style resolution.
		this.SetDefaultStyleKey();

		m_isInitializing = true;
		m_isPendingViewChange = false;
		m_isProcessingKeyboardInput = false;
		m_isProcessingPointerInput = false;
		m_isCancellingJumpList = false;
		m_zoomOriginatesFromZoomedInView = false;
		_upperThresholdLow = 0.0f;
		_upperThresholdHigh = 0.0f;
		_lowerThresholdLow = 0.0f;
		_lowerThresholdHigh = 0.0f;
		m_isZoomedInViewAnimationHooked = false;
		m_isZoomedOutViewAnimationHooked = false;
		m_emulatingGesture = false;
		m_changePhase = SemanticZoomPhase.SemanticZoomPhase_Idle;
		m_phaseChangeLockDuringViewSwitch = false;
		m_calledInitializeViewChangeSinceManipulationStart = false;
		m_cumulativeZoomFactorAtStartOfManipulation = 1.0f;
		m_isProcessingViewChange = false;
		m_isZoomOutButtonEnabled = true;
		m_hasAutomationPeer = false;
		m_zoomPoint = default;
		m_zoomPointForZoomedInView = default;
		m_zoomPointForZoomedOutView = default;
		m_manipulatedElementOffset = default;

		InitializeManagedLifecycle();
	}

	// Destroys an instance of the SemanticZoom class.
	// Managed lifecycle cleanup is performed by OnSemanticZoomUnloaded and
	// CleanupTemplateSubscriptions instead of a finalizer.
	// Makes sure the alternate view timer is stopped.

	// Handles custom property changed events and calls their OnPropertyChanged2
	// methods.
	private void OnPropertyChanged2(DependencyProperty property, object newValue)
	{
		if (property == IsZoomedInViewActiveProperty)
		{
			if (!CanChangeViews)
			{
				throw new InvalidOperationException("The active view cannot be changed when CanChangeViews is false.");
			}

			if (!m_isInitializing)
			{
				// Only change views if we're not in the middle of initialization.
				// initialization is now defined from ctor until template has been applied.
				if (!m_phaseChangeLockDuringViewSwitch)
				{
					m_changePhase = SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews;
				}
				ChangeViews();
			}
			else
			{
				// Otherwise, postpone the view change until EndInit is called.
				// If a change was already scheduled, cancel the view change so we don't
				// toggle to the incorrect view.
				m_isPendingViewChange = !m_isPendingViewChange;
			}
		}
		else if (property == IsZoomOutButtonEnabledProperty)
		{
			m_isZoomOutButtonEnabled = (bool)newValue;
		}
	}

	// Associate the SemanticZoom with an ISemanticZoomInformation view.
	private void InitializeSemanticZoomInformation(
		ISemanticZoomInformation? oldValue,
		ISemanticZoomInformation? newValue,
		bool isZoomedInView)
	{
		if (oldValue is not null)
		{
			oldValue.SemanticZoomOwner = null;
			oldValue.IsActiveView = false;
			oldValue.IsZoomedInView = true;
		}

		if (newValue is not null)
		{
			newValue.SemanticZoomOwner = this;
			newValue.IsZoomedInView = isZoomedInView;
			newValue.IsActiveView = isZoomedInView == IsZoomedInViewActive;
		}
	}

	// Get the view container template parts.
	protected override void OnApplyTemplate()
	{
		// getting to the VisualState

		// reset the flags that indicate whether we have hooked the events
		m_isZoomedInViewAnimationHooked = m_isZoomedOutViewAnimationHooked = false;

		// unhook events on templateparts
		CleanupTemplateSubscriptions(clearParts: true);

		base.OnApplyTemplate();

		m_tpZoomedInPresenterPart = GetTemplateChild("ZoomedInPresenter") as FrameworkElement;
		m_tpZoomedOutPresenterPart = GetTemplateChild("ZoomedOutPresenter") as FrameworkElement;
		m_tpZoomedOutTransform = GetTemplateChild("ZoomedOutTransform") as CompositeTransform;
		m_tpZoomedInTransform = GetTemplateChild("ZoomedInTransform") as CompositeTransform;
		m_tpManipulatedElementTransform = GetTemplateChild("ManipulatedElementTransform") as CompositeTransform;

		// reset the state we are in
		m_changePhase = SemanticZoomPhase.SemanticZoomPhase_Idle;
		m_phaseChangeLockDuringViewSwitch = false;

		// set the thresholds for the first time.
		UpdateThresholds();

		var subscriptions = new CompositeDisposable();
		m_templateSubscriptions.Disposable = subscriptions;

		if (GetTemplateChild("ScrollViewer") is ScrollViewer scrollViewer)
		{
			m_tpScrollViewer = scrollViewer;
			scrollViewer.m_templatedParentHandlesMouseButton = true;
			scrollViewer.RegisterAsSemanticZoomHost();

			// Ignore mouse wheel (except for zooming)
			scrollViewer.ArePointerWheelEventsIgnored = true;
			scrollViewer.SetDirectManipulationStateChangeHandler(this);
			scrollViewer.BringIntoViewOnFocusChange = false;

			// set correction scales
			// Two views:
			// 1. ZoomedInView, we will be at _zoomMax, which is a zoomfactor of 1.
			//    In that zoomfactor, we wish the zoomedinview to visually look like it is unscaled.
			// 2. ZoomedOutView, we will be at _zoomMin, which is a zoomfactor of 0.5.
			//    In that zoomfactor, we wish the zoomedoutview to visually look like it is unscaled.

			// we do this 'trick' so that we can gradually go from a zoomfactor of 1 to 0.5 and bring in the zoomedoutview at some
			// zoomfactor in between (let's say 0.7). At that zoomfactor the zoomedoutview will look as though it is scaled UP.
			// when the user releases the fingers, we will settle at 0.5. At that point the zoomedoutview will look perfect.

			// the trick is to scale everything up by 2 and apply a correction on the zoomedinview
			if (m_tpManipulatedElementTransform is not null)
			{
				// this zooms the whole tree up by 2 (and in generic.xaml the origin is specified to be (0,0) )
				// at zoomfactor 1 this would look weird (but we're faded out anyway).
				m_tpManipulatedElementTransform.ScaleX = 1 / _zoomMin;
				m_tpManipulatedElementTransform.ScaleY = 1 / _zoomMin;
			}
			if (m_tpZoomedInTransform is not null)
			{
				// correction scale so that zoomedinview looks good at at zoomfactor 1
				m_tpZoomedInTransform.ScaleX = _zoomMin;
				m_tpZoomedInTransform.ScaleY = _zoomMin;
			}
			// please look at the SizeChanged handler for the continuation of this setup. That part needs
			// to occur when we have a valid size.
		}

		if (GetTemplateChild("ZoomOutButton") is Button zoomOutButton)
		{
			m_tpZoomOutButton = zoomOutButton;
			zoomOutButton.Click += OnZoomOutButtonClick;
			m_elementZoomOutButtonClickToken.Disposable =
				Disposable.Create(() => zoomOutButton.Click -= OnZoomOutButtonClick);
		}

		// and subscribe to size changed so that we can position our views once we have a size
		SizeChanged += OnSizeChanged;
		m_sizeChangedToken.Disposable = Disposable.Create(() => SizeChanged -= OnSizeChanged);

		// also subscribe to the size of the views changing
		// since it can change without changing the size of this element
		if (m_tpZoomedInPresenterPart is { } zoomedInPresenter)
		{
			zoomedInPresenter.SizeChanged += OnSizeChanged;
			m_zoomedInViewSizeChangedToken.Disposable =
				Disposable.Create(() => zoomedInPresenter.SizeChanged -= OnSizeChanged);
		}

		if (m_tpZoomedOutPresenterPart is { } zoomedOutPresenter)
		{
			zoomedOutPresenter.SizeChanged += OnSizeChanged;
			m_zoomedOutViewSizeChangedToken.Disposable =
				Disposable.Create(() => zoomedOutPresenter.SizeChanged -= OnSizeChanged);
		}

		// Now that XAML parsing is over we will display our proper view.
		// All views start out collapsed, to not pay huge layout costs that might
		// be unnecessary.
		if (m_isPendingViewChange)
		{
			if (m_tpZoomedOutPresenterPart is not null)
			{
				m_tpZoomedOutPresenterPart.Visibility = Visibility.Visible;
			}

			// ChangeViews will show a nice transition when we switch. In this case we need to
			// preempt that by going to the new VisualState immediately.
			// The UpdateVisualState(TRUE) call inside of ChangeViews will be a no-op.
			UpdateVisualState(false);
			ChangeViews();
		}
		else
		{
			// we will just display the content view
			if (m_tpZoomedInPresenterPart is not null)
			{
				m_tpZoomedInPresenterPart.Visibility = Visibility.Visible;
				m_tpZoomedInPresenterPart.IsHitTestVisible = true;
				m_tpZoomedInPresenterPart.SkipFocusSubtree = false;

				if (m_tpZoomedOutPresenterPart is not null)
				{
					m_tpZoomedOutPresenterPart.SkipFocusSubtree = true;
				}
			}

			// Ensure we display the correct view. If IsZoomedInViewActive is set while our template is not yet initialized,
			// we could show the user the incorrect view on startup.
			UpdateVisualState(false);
		}

		if (TryGetVisualState("ZoomInView", out _, out var zoomedInState))
		{
			AddStoryboardCompletedHandler(zoomedInState, ViewChangeAnimationFinished, subscriptions);
			m_isZoomedInViewAnimationHooked = true;
		}

		if (TryGetVisualState("ZoomOutView", out _, out var zoomedOutState))
		{
			AddStoryboardCompletedHandler(zoomedOutState, ViewChangeAnimationFinished, subscriptions);
			m_isZoomedOutViewAnimationHooked = true;
		}

		if (TryGetVisualState("ZoomOutButtonVisible", out var group, out var zoomOutButtonVisibleState))
		{
			// Get button timer transition, mark it essential
			foreach (var transition in group.Transitions)
			{
				if (transition.Storyboard is { } storyboard)
				{
					MakeStoryboardEssential(storyboard);
				}
			}

			AddStoryboardCompletedHandler(zoomOutButtonVisibleState, OnZoomOutButtonVisibleStoryboardCompleted, subscriptions);
		}

		if (TryGetVisualState("ZoomOutButtonVisible", out group, out zoomOutButtonVisibleState))
		{
			// Get button timer transition, mark it essential
			foreach (var transition in group.Transitions)
			{
				if (transition.To == "ZoomOutButtonHidden" &&
					transition.From == "ZoomOutButtonVisible" &&
					transition.Storyboard is { } storyboard)
				{
					MakeStoryboardEssential(storyboard);
				}
			}

			AddStoryboardCompletedHandler(zoomOutButtonVisibleState, OnZoomOutButtonVisibleStoryboardCompleted, subscriptions);
		}

		HideZoomOutButton(false /* bUseTransitions */);

		// create a timer that will trigger the creation of our alternate view
		SetAlternateViewTimer(subscriptions);

		m_isPendingViewChange = false;
		m_isInitializing = false;
	}

	// brings the alternate view into layout.
	private void SetupAlternateView(object? sender, object? args)
	{
		m_tpAlternateViewTimer?.Stop();

		// instead of finding out which view is active, I just set visibility to true
		// on both. Setting a property to the same value causes only little overhead.
		if (m_tpZoomedOutPresenterPart is not null)
		{
			m_tpZoomedOutPresenterPart.Visibility = Visibility.Visible;
		}
		if (m_tpZoomedInPresenterPart is not null)
		{
			m_tpZoomedInPresenterPart.Visibility = Visibility.Visible;
		}
	}

	// setup timer to trigger creation of alternate view
	// sets a timer to bring the currently not active view into layout
	private void SetAlternateViewTimer(CompositeDisposable subscriptions)
	{
		var dispatcherTimer = new DispatcherTimer
		{
			Interval = new TimeSpan(150)
		};
		m_tpAlternateViewTimer = dispatcherTimer;
		dispatcherTimer.Tick += SetupAlternateView;
		subscriptions.Add(Disposable.Create(() => dispatcherTimer.Tick -= SetupAlternateView));
		dispatcherTimer.Start();
	}

	// Change to the correct visual state for the SemanticZoom.
	private protected override void ChangeVisualState(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		// true to use transitions when updating the visual state, false to snap
		// directly to the new visual state.
		bool useTransitions)
	{
		VisualStateManager.GoToState(this, IsZoomedInViewActive ? "ZoomInView" : "ZoomOutView", useTransitions);
	}

	// Toggle the active view.
	private void ToggleActiveViewImpl()
	{
		if (!CanChangeViews)
		{
			throw new InvalidOperationException("The active view cannot be changed when CanChangeViews is false.");
		}

		var isZoomedInViewActive = !IsZoomedInViewActive;
		IsZoomedInViewActive = isZoomedInViewActive;

		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged) &&
			GetOrCreateAutomationPeer() is SemanticZoomAutomationPeer semanticZoomAutomationPeer)
		{
			semanticZoomAutomationPeer.RaiseToggleStatePropertyChangedEvent(!isZoomedInViewActive);
		}

		// Request a play show/hide sound for toggle active view
		ElementSoundPlayerService.RequestInteractionSoundForElementStatic(
			isZoomedInViewActive ? ElementSoundKind.Show : ElementSoundKind.Hide,
			this);
	}

	// Registers or unregisters back key listener based on whether the ZOV is active or not
	private void ToggleBackKeyListener(bool isZoomedOutViewActive)
	{
		if (!DXamlCore.Current.BackButtonSupported)
		{
			return;
		}

		if (isZoomedOutViewActive)
		{
			BackButtonIntegration.RegisterListener(this);
		}
		else
		{
			BackButtonIntegration.UnregisterListener(this);
		}
	}

	// Used to handle back button presses on phone
	internal bool OnBackButtonPressedImpl()
	{
		m_isCancellingJumpList = true;

		try
		{
			ToggleActiveView();
			return true;
		}
		finally
		{
			m_isCancellingJumpList = false;
		}
	}

	bool IBackButtonListener.OnBackButtonPressed() => OnBackButtonPressedImpl();

	// Sets internal flags and then calls ToggleActiveView(), so that when
	// focus changes as a result of ToggleActiveView(), the new item is focused
	// using the specified FocusState
	internal void ToggleActiveViewWithFocusState(FocusState focusState)
	{
		if (!CanChangeViews)
		{
			return;
		}

		try
		{
			if (focusState == FocusState.Keyboard)
			{
				m_isProcessingKeyboardInput = true;
			}
			else if (focusState == FocusState.Pointer)
			{
				m_isProcessingPointerInput = true;
			}
			else
			{
				MUX_ASSERT(false);
			}

			ToggleActiveView();
		}
		finally
		{
			m_isProcessingKeyboardInput = false;
			m_isProcessingPointerInput = false;
		}
	}

	// DirectManipulationStateChangeHandler implementation
	internal void NotifyStateChange(
		DMManipulationState state,
		float xCumulativeTranslation,
		float yCumulativeTranslation,
		float zCumulativeFactor,
		float xCenter,
		float yCenter,
		bool isInertial,
		bool isTouchConfigurationActivated,
		bool isBringIntoViewportConfigurationActivated)
	{
		var inZoomedInView = IsZoomedInViewActive;

		// notice use of viewport width for debugging ease

		try
		{
			switch (state)
			{
				case DMManipulationState.DMManipulationStarting:
					if (m_tpScrollViewer is not null)
					{
						m_tpScrollViewer.m_inSemanticZoomAnimation = true;
					}

					// compare against this factor
					m_cumulativeZoomFactorAtStartOfManipulation = zCumulativeFactor;
					break;

				case DMManipulationState.DMManipulationStarted:
					// stores how we started this manipulation. This is being used during DManipulationDelta
					// to determine which thresholds to use

					// this value is not trustworthy if we did an api change. We have already set the inZoomedInView value
					m_zoomOriginatesFromZoomedInView =
						m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews
							? !inZoomedInView
							: inZoomedInView;
					UpdateThresholds();

					// write down the center point so we might use that information during ChangeView
					m_zoomPoint.X = xCenter;
					m_zoomPoint.Y = yCenter;

					// setting the other zoompoints to (0,0) indicates that they need to be calculated at ChangeView time
					m_zoomPointForZoomedInView = default;
					m_zoomPointForZoomedOutView = default;
					break;

				case DMManipulationState.DMManipulationDelta:
				case DMManipulationState.DMManipulationLastDelta:
					if (state == DMManipulationState.DMManipulationDelta)
					{
						var canChangeViews = CanChangeViews;
						if (canChangeViews &&
							Math.Abs(m_cumulativeZoomFactorAtStartOfManipulation - zCumulativeFactor) > zoomDeltaThreshold &&
							(m_changePhase == SemanticZoomPhase.SemanticZoomPhase_Idle ||
							 m_changePhase == SemanticZoomPhase.SemanticZoomPhase_DM_SwitchingViews) &&
							!isInertial)
						{
							var zoomFactor = m_tpScrollViewer?.ZoomFactor ?? 1.0f;
							var isHandled = false;

							// the cumulative zoomfactor is calculated by the start of the manipulation and treating
							// that zoomfactor as 1.0. This is not what we use to determine viewchange point by.
							// Thresholds are calculated based on the actual zoomfactor.
							if (m_zoomOriginatesFromZoomedInView)
							{
								if (inZoomedInView && zoomFactor < _upperThresholdLow)
								{
									isHandled = true;
								}
								else if (!inZoomedInView && zoomFactor > _upperThresholdHigh)
								{
									isHandled = true;
								}
							}
							else
							{
								if (!inZoomedInView && zoomFactor > _lowerThresholdHigh)
								{
									isHandled = true;
								}
								else if (inZoomedInView && zoomFactor < _lowerThresholdLow)
								{
									isHandled = true;
								}
							}

							if (isHandled)
							{
								// by putting a lock on this, and calling put_IsZoomedInViewActive, we tell the handler to not
								// change the phase. By calling the property, normally we would get a phase of _API_SwitchingViews.
								// This is the only place where that is not correct.
								m_phaseChangeLockDuringViewSwitch = true;
								m_changePhase = SemanticZoomPhase.SemanticZoomPhase_DM_SwitchingViews;
								IsZoomedInViewActive = !inZoomedInView;
							}

							// initialize the viewchange as soon as a DM manipulation starts
							if (canChangeViews && !m_calledInitializeViewChangeSinceManipulationStart)
							{
								// capture that any ChangeView will have been caused by pointer input
								m_isProcessingPointerInput = true;

								var sourceView = inZoomedInView ? ZoomedInView : ZoomedOutView;
								var destinationView = inZoomedInView ? ZoomedOutView : ZoomedInView;
								sourceView?.InitializeViewChange();
								destinationView?.InitializeViewChange();
								m_calledInitializeViewChangeSinceManipulationStart = true;
							}
						}
					}

					if (m_tpScrollViewer is { } scrollViewer)
					{
						// user interrupts when we see that we are not using a BringIntoViewportConfiguration
						// even though we are in a state where we absolutely expect one
						if ((m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews ||
							 m_changePhase == SemanticZoomPhase.SemanticZoomPhase_DM_CompletingViews) &&
							!isBringIntoViewportConfigurationActivated)
						{
							// we interrupt, so we wish to go to idle again.
							// notice that we do not call Initialize again
							// since we are guaranteed a completed state still.
							m_changePhase = SemanticZoomPhase.SemanticZoomPhase_Idle;

							// we have interrupted, potentially a zoom. The factor that we currently are in,
							// should be used to indicate whether we have truly zoomed
							m_cumulativeZoomFactorAtStartOfManipulation = zCumulativeFactor;

							// Temporary workaround for DManip bug 799346
							// Undo the zoom factor boundary adjustments
							scrollViewer.SetDirectManipulationOverridingZoomBoundaries();
						}

						// nicely animate to our final position when the fingers were let go (we entered inertia mode)
						// Inertia may start in DMManipulationDelta or DMManipulationLastDelta
						else if (((state == DMManipulationState.DMManipulationDelta && isInertial) ||
								  state == DMManipulationState.DMManipulationLastDelta) &&
								 (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_Idle ||
								  m_changePhase == SemanticZoomPhase.SemanticZoomPhase_DM_SwitchingViews))
						{
							// Since we live in a stretched up ScrollViewer, we take control by calling BringIntoViewport
							var bounds = CalculateBounds();
							var zoomFactor = scrollViewer.ZoomFactor;
							var offsetX = scrollViewer.HorizontalOffset;
							var offsetY = scrollViewer.VerticalOffset;

							// crucial to only call BringIntoViewport if there is a change to scroll to.
							// if we do not, we get into deadlocks, infinite cycles and asserts.
							if (Math.Abs(zoomFactor - (inZoomedInView ? _zoomMax : _zoomMin)) > c_minimumZoomDelta ||
								Math.Abs(bounds.X * (inZoomedInView ? _zoomMax : _zoomMin) - offsetX) >= c_minimumBoundsDelta ||
								Math.Abs(bounds.Y * (inZoomedInView ? _zoomMax : _zoomMin) - offsetY) >= c_minimumBoundsDelta)
							{
								m_changePhase = SemanticZoomPhase.SemanticZoomPhase_DM_CompletingViews;

								// Temporary workaround for DManip bug 799346
								scrollViewer.SetDirectManipulationOverridingZoomBoundaries();
								scrollViewer.BringIntoViewport(
									bounds,
									false /*skipDuringTouchContact*/,
									false /*skipAnimationWhileRunning*/,
									true /*animate*/);
							}
							else
							{
								// did not have to complete this for some reason, so reset to idle
								m_changePhase = SemanticZoomPhase.SemanticZoomPhase_Idle;
							}
						}
					}
					break;

				case DMManipulationState.DMManipulationCompleted:
					if (m_tpScrollViewer is { } completedScrollViewer)
					{
						// Temporary workaround for DManip bug 799346
						completedScrollViewer.ResetDirectManipulationOverridingZoomBoundaries();
					}

					// a complete comes as either the end of the BringIntoView call by ChangeView
					// or by the completion of a gesture.
					// We complete the session only after we have completely finished our viewchange.
					// This is indicated by a cleared out m_tpCompletedArgs.
					// That means if the fadein/fadeout is still occurring
					// (we have not called OnViewChangeCompleted yet), we should not complete here, but
					// rely on ViewChangeAnimationFinished
					if (m_calledInitializeViewChangeSinceManipulationStart && m_tpCompletedArgs is null)
					{
						m_calledInitializeViewChangeSinceManipulationStart = false;
						var sourceView = inZoomedInView ? ZoomedOutView : ZoomedInView;
						var destinationView = inZoomedInView ? ZoomedInView : ZoomedOutView;
						// Cleanup the views
						sourceView?.CompleteViewChange();
						destinationView?.CompleteViewChange();
					}

					// mark that DM is completely done with the animation.
					m_changePhase = SemanticZoomPhase.SemanticZoomPhase_Idle;

					// get rid of all the corrections.
					if (m_tpZoomedInTransform is not null)
					{
						m_tpZoomedInTransform.TranslateX = 0;
						m_tpZoomedInTransform.TranslateY = 0;
					}

					if (m_tpZoomedOutTransform is not null)
					{
						m_tpZoomedOutTransform.TranslateX = 0;
						m_tpZoomedOutTransform.TranslateY = 0;
					}

					if (m_tpScrollViewer is { } finalScrollViewer)
					{
						var bounds = CalculateBounds();
						finalScrollViewer.ScrollToHorizontalOffsetInternal(bounds.X * (!inZoomedInView ? _zoomMin : 1));
						finalScrollViewer.ScrollToVerticalOffsetInternal(bounds.Y * (!inZoomedInView ? _zoomMin : 1));
						finalScrollViewer.ZoomToFactorInternal(inZoomedInView ? _zoomMax : _zoomMin, true, out _);
					}
					break;
			}
		}
		finally
		{
			m_isProcessingPointerInput = false;
			m_phaseChangeLockDuringViewSwitch = false;
		}

		_ = xCumulativeTranslation;
		_ = yCumulativeTranslation;
		_ = isTouchConfigurationActivated;
	}

	void IDirectManipulationStateChangeHandler.NotifyStateChange(
		DMManipulationState state,
		float xCumulativeTranslation,
		float yCumulativeTranslation,
		float zCumulativeFactor,
		float xCenter,
		float yCenter,
		bool isInertial,
		bool isTouchConfigurationActivated,
		bool isBringIntoViewportConfigurationActivated) =>
		NotifyStateChange(
			state,
			xCumulativeTranslation,
			yCumulativeTranslation,
			zCumulativeFactor,
			xCenter,
			yCenter,
			isInertial,
			isTouchConfigurationActivated,
			isBringIntoViewportConfigurationActivated);

	// Called by SemanticZoomAutomationPeer to Toggle
	internal void AutomationSemanticZoomOnToggle()
	{
		if (CanChangeViews)
		{
			// Jump to focused item through UIAutomation
			ToggleActiveView();
		}
	}

	// Returns the currently displayed presenter for automation.
	internal FrameworkElement? AutomationGetActivePresenter() =>
		IsZoomedInViewActive ? m_tpZoomedInPresenterPart : m_tpZoomedOutPresenterPart;

	// Creates or gets the automation peers for the two presenter's children and
	// reparents them to this control so when automation builds the tree from
	// one of them the tree stays self-consistent.
	private void AutomationReparentPresenters(SemanticZoomAutomationPeer sezoPeer)
	{
		if (m_tpZoomedInPresenterPart is UIElement zoomedInPresenter)
		{
			foreach (var child in sezoPeer.GetAutomationPeerChildren(zoomedInPresenter))
			{
				child.SetParent(sezoPeer);
			}
		}

		if (m_tpZoomedOutPresenterPart is UIElement zoomedOutPresenter)
		{
			foreach (var child in sezoPeer.GetAutomationPeerChildren(zoomedOutPresenter))
			{
				child.SetParent(sezoPeer);
			}
		}
	}

	// Override OnCreateAutomationPeer()
	// Create SemanticZoomAutomationPeer to represent the SemanticZoom.
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		m_hasAutomationPeer = true;
		var automationPeer = new SemanticZoomAutomationPeer(this);
		AutomationReparentPresenters(automationPeer);
		return automationPeer;
	}

	// Handles when a key is pressed down on the SemanticZoom.
	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		m_isProcessingKeyboardInput = true;

		try
		{
			base.OnKeyDown(args);
			if (args.Handled || !CanChangeViews)
			{
				return;
			}

			// Get the current view so we can determine whether the desired key
			// press can navigate to the desired view
			var messageZoomDirection = ScrollViewer.GetKeyboardMessageZoomAction(args.KeyboardModifiers, args.Key);

			if (messageZoomDirection == ZoomDirection.Out)
			{
				// We can only change to the ZoomedOutView if we're already
				// in the ZoomedInView
				if (IsZoomedInViewActive)
				{
					// Update when we've handled the event
					args.Handled = true;
					IsZoomedInViewActive = false;
				}
			}
			else if (messageZoomDirection == ZoomDirection.In)
			{
				// We can only change to the ZoomedInView if we're
				// already in the ZoomedOutView
				if (!IsZoomedInViewActive)
				{
					args.Handled = true;
					IsZoomedInViewActive = true;
				}
			}
		}
		finally
		{
			m_isProcessingKeyboardInput = false;
		}
	}

	// Handles when the mouse wheel spins to change active views.
	protected override void OnPointerWheelChanged(PointerRoutedEventArgs args)
	{
		m_isProcessingPointerInput = true;

		try
		{
			base.OnPointerWheelChanged(args);
			if (args.Handled)
			{
				return;
			}
			// allow zooming using mouse in Win8 desktop apps. Phone does not have mouse in phone blue.
			// in threshold mouse zoom is disabled by default. unless you enable zoom mode on the
			// ScrollViewer
			var shouldAllowMouseZoom = m_tpScrollViewer?.ZoomMode != ZoomMode.Disabled;
			if (!shouldAllowMouseZoom || !CanChangeViews)
			{
				return;
			}

			// Only use Ctrl+Mousewheel to change the zoom
			if ((CoreImports.Input_GetKeyboardModifiers() & VirtualKeyModifiers.Control) == 0)
			{
				return;
			}

			// Get the amount scrolled
			var pointerPoint = args.GetCurrentPoint(this);

			// get the point that we scrolled on
			m_zoomPoint = pointerPoint.Position;

			// Get the current view so we can determine whether the desired zoom gesture
			// can navigate to the desired view
			// set to gesture emulation
			m_emulatingGesture = true;

			// We can only change to the ZoomedInView if we're already in the
			// ZoomedOutView
			if (!IsZoomedInViewActive && pointerPoint.Properties.MouseWheelDelta > 0)
			{
				// Update when we've handled the event
				args.Handled = true;
				IsZoomedInViewActive = true;
			}
			// We can only change to the ZoomedOutView if we're already in the
			// ZoomedInView
			else if (IsZoomedInViewActive && pointerPoint.Properties.MouseWheelDelta < 0)
			{
				args.Handled = true;
				IsZoomedInViewActive = false;
			}
		}
		finally
		{
			m_emulatingGesture = false;
			m_isProcessingPointerInput = false;
		}
	}

	// clamps a value to be within a min and max value
	private static float Clamp(double value, double min, double max)
	{
		if (value >= min && value <= max)
		{
			return (float)value;
		}
		if (value < min)
		{
			return (float)min;
		}

		return (float)max;
	}

	// matches DUI implementation that calculates the threshold points where we switch
	// sets the thresholds
	private void UpdateThresholds()
	{
		var zoomRange = _zoomMax - _zoomMin;
		if (zoomRange > 0)
		{
			var upperDelta = Clamp(zoomRange * _upperThresholdDelta, c_thresholdDeltaMin, c_thresholdDeltaMax);
			var lowerDelta = Clamp(zoomRange * _lowerThresholdDelta, c_thresholdDeltaMin, c_thresholdDeltaMax);
			var thresholdBufferDelta = Clamp(_thresholdBufferDelta, c_thresholdBufferDeltaMin, c_thresholdBufferDeltaMax);

			_upperThresholdLow = _zoomMax - upperDelta - thresholdBufferDelta;
			_upperThresholdHigh = _zoomMax - upperDelta;
			_lowerThresholdLow = _zoomMin + lowerDelta;
			_lowerThresholdHigh = _zoomMin + lowerDelta + thresholdBufferDelta;

			MUX_ASSERT(_upperThresholdLow > _zoomMin && _lowerThresholdHigh < _zoomMax);
		}
		else
		{
			_upperThresholdHigh = _upperThresholdLow = _lowerThresholdHigh = _lowerThresholdLow = _zoomMax;
		}
	}

	// Raise the ViewChangeStarted event.
	private void OnViewChangeStarted(SemanticZoomViewChangedEventArgs e)
	{
		// Raise the event
		ViewChangeStarted?.Invoke(this, e);
	}

	// Raise the ViewChangeCompleted event.
	private void OnViewChangeCompleted(SemanticZoomViewChangedEventArgs e)
	{
		try
		{
			// Raise the event
			ViewChangeCompleted?.Invoke(this, e);
		}
		finally
		{
			m_isProcessingViewChange = false;
		}
	}

	// This method does nothing on Windows no-op (it's under an APISet
	// SemanticZoom_JumpList) to toggle the active view if we're in jump
	// list behavior.
	// Toggle the active view as a result of a HeaderItem tap
	internal bool ToggleActiveViewFromHeaderItem()
	{
		ToggleActiveView();
		return true;
	}

	// Change from one view to another.
	private void ChangeViews()
	{
		// calls to this method are deferred until applytemplate has run
		// therefore spSourcePart and spDestinationPart can be guaranteed to have
		// been initialized.
		if (!CanChangeViews)
		{
			throw new InvalidOperationException("The active view cannot be changed when CanChangeViews is false.");
		}

		m_isProcessingViewChange = true;

		if (SharedHelpers.IsInDesignModeV2())
		{
			// ChangeViews will show a nice transition when we switch. In this case we need to
			// preempt that by going to the new visualstate immediately as we do not want
			// any visual transitions in design mode.
			UpdateVisualState(false);
		}

		// Determine if we're flipping from ZoomedInView to ZoomedOutView (note: This
		// method is only called via the property changed handler when
		// IsZoomedInViewActive is updated - so the value has already been updated
		// before we change the view and we need to flip it here)
		var changingToZoomedOutView = !IsZoomedInViewActive;

		ISemanticZoomInformation? sourceView;
		ISemanticZoomInformation? destinationView;
		FrameworkElement? sourcePart;
		FrameworkElement? destinationPart;

		// Get the source and destination views/parts
		if (changingToZoomedOutView)
		{
			sourceView = ZoomedInView;
			destinationView = ZoomedOutView;
			sourcePart = m_tpZoomedInPresenterPart;
			destinationPart = m_tpZoomedOutPresenterPart;

			if (m_tpZoomedOutTransform is not null)
			{
				m_tpZoomedOutTransform.TranslateX = 0;
				m_tpZoomedOutTransform.TranslateY = 0;
			}
		}
		else
		{
			sourceView = ZoomedOutView;
			destinationView = ZoomedInView;
			sourcePart = m_tpZoomedOutPresenterPart;
			destinationPart = m_tpZoomedInPresenterPart;

			if (m_tpZoomedInTransform is not null)
			{
				m_tpZoomedInTransform.TranslateX = 0;
				m_tpZoomedInTransform.TranslateY = 0;
			}
		}

		ToggleBackKeyListener(changingToZoomedOutView);

		// Initialize the views
		// this has already been taken care of by NotifyStateChange
		// in the case of a DM driven change
		if (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews &&
			!m_calledInitializeViewChangeSinceManipulationStart)
		{
			sourceView?.InitializeViewChange();
			destinationView?.InitializeViewChange();
			m_calledInitializeViewChangeSinceManipulationStart = true;
		}

		// Allow the SemanticZoomInformation views to setup the view change
		var sourceItem = new SemanticZoomLocation();
		var destinationItem = new SemanticZoomLocation();
		var sourceCoordinateSystem = default(Rect);
		var destinationCoordinateSystem = default(Rect);

		// initialize to correct values
		if (m_tpScrollViewer is not null && sourceView is not null && destinationView is not null && !m_isPendingViewChange)
		{
			var zoomedInContent = ZoomedInView as UIElement;
			var zoomedOutContent = ZoomedOutView as UIElement;
			var zoomPointZoomedInView = default(Point);
			var zoomPointZoomedOutView = default(Point);

			// the zoompoint (if relevant) can come from two places:
			// 1. ctrl-mousewheel, in which case it is just a point relative to this element
			//    this point is in layoutpixels, relative to SeZo
			//    We will need to hand it off relative to the correct view in their coordinate system
			//
			// 2. DM (during pinch gesture) in which case it is a point relative to the manipulated element
			//    normalized to DM factor 1 (natural size/layout pixels)

			// track which method was used to zoom
			short zoomType = _zoomedClick;

			if (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_DM_SwitchingViews || m_emulatingGesture)
			{
				if (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews)
				{
					// case 1: point is relative to sezo
					// get the transforms between these two visuals
					if (zoomedInContent is not null)
					{
						m_zoomPointForZoomedInView = TransformToVisual(zoomedInContent).TransformPoint(m_zoomPoint);
					}
					if (zoomedOutContent is not null)
					{
						m_zoomPointForZoomedOutView = TransformToVisual(zoomedOutContent).TransformPoint(m_zoomPoint);
					}

					// no corrections to take in
					zoomPointZoomedInView = m_zoomPointForZoomedInView;
					zoomPointZoomedOutView = m_zoomPointForZoomedOutView;
					zoomType = _zoomedWheel;
				}
				else
				{
					// case 2: point is relative to manipulated element
					var zoomPoint = m_zoomPoint;
					var offsetX = m_tpManipulatedElementTransform?.TranslateX ?? 0;
					var offsetY = m_tpManipulatedElementTransform?.TranslateY ?? 0;
					zoomPoint.X -= offsetX;
					zoomPoint.Y -= offsetY;

					// only calculate once (in case we're doing many changeviews)
					if (DoubleUtil.AreClose(m_zoomPointForZoomedInView.X, 0) &&
						DoubleUtil.AreClose(m_zoomPointForZoomedInView.Y, 0))
					{
						var width = m_tpZoomedInPresenterPart?.ActualWidth ?? 0;
						var height = m_tpZoomedInPresenterPart?.ActualHeight ?? 0;
						m_zoomPointForZoomedInView = zoomPoint;

						// deduct the offsets
						m_zoomPointForZoomedInView.X -= width * _zoomMin;
						m_zoomPointForZoomedInView.Y -= height * _zoomMin;
					}

					zoomPointZoomedInView = m_zoomPointForZoomedInView;

					// and each time correction
					if (m_tpZoomedInTransform is not null)
					{
						zoomPointZoomedInView.X -= m_tpZoomedInTransform.TranslateX / _zoomMin;
						zoomPointZoomedInView.Y -= m_tpZoomedInTransform.TranslateY / _zoomMin;
					}

					// work on the zoomedoutviews point
					if (DoubleUtil.AreClose(m_zoomPointForZoomedOutView.X, 0) &&
						DoubleUtil.AreClose(m_zoomPointForZoomedOutView.Y, 0))
					{
						// there is a factor of 2 involved
						m_zoomPointForZoomedOutView = new Point(zoomPoint.X * _zoomMin, zoomPoint.Y * _zoomMin);
					}

					zoomPointZoomedOutView = m_zoomPointForZoomedOutView;

					// and its correction
					if (m_tpZoomedOutTransform is not null)
					{
						zoomPointZoomedOutView.X -= m_tpZoomedOutTransform.TranslateX;
						zoomPointZoomedOutView.Y -= m_tpZoomedOutTransform.TranslateY;
					}

					zoomType = _zoomedPinch;
				}

				sourceItem.ZoomPoint = changingToZoomedOutView ? zoomPointZoomedInView : zoomPointZoomedOutView;
				destinationItem.ZoomPoint = changingToZoomedOutView ? zoomPointZoomedOutView : zoomPointZoomedInView;
			}

			_ = zoomType;
		}

		// give the source view the chance to determine the item that was being pressed
		// possibly even suggest a destination item
		sourceView?.StartViewChangeFrom(sourceItem, destinationItem);

		// take the output from StartViewChangeFrom and convert coordinate systems between source and destination
		// the difficulty here really is about how you wish to handoff: (A) in the case of a direct manipulation (pinch)
		// we wish to do the handoff immediately, at the zoomfactor that is currently being applied. A
		// simple transformToVisual will suffice.
		// (B) in the case of a programmatic switch (ctrl-mousewheel for instance), we wish to do a handoff
		// where the destination will _endup_ in the location that sourceitem returned.
		if (m_tpScrollViewer is not null && !m_isPendingViewChange)
		{
			// case (A): we wish to have the destination element show up at the current location as
			// dictated by the sourceitem
			sourceCoordinateSystem = sourceItem.Bounds;
			if (sourcePart is not null && destinationPart is not null)
			{
				destinationCoordinateSystem = sourcePart
					.TransformToVisual(destinationPart)
					.TransformBounds(sourceCoordinateSystem);
			}
			else
			{
				destinationCoordinateSystem = sourceCoordinateSystem;
			}

			if (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews &&
				m_tpScrollViewer.Content is FrameworkElement manipulatedElement)
			{
				// case (B): we wish to have the destination element show up at a location such that when we reach
				// the final zoomfactor we will be at the point that the source item is in right now

				// the manipulated element is currently centered and at some zoomfactor (0.5)
				// the distance we have is the distance to the left of that manipulated element

				// calculate the distance to the center
				var distanceToCenter = new Point(
					destinationCoordinateSystem.X - manipulatedElement.ActualWidth / 2,
					destinationCoordinateSystem.Y - manipulatedElement.ActualHeight / 2);
				var zoomFactor = m_tpScrollViewer.ZoomFactor;

				// this is how it would be if the pixels would remain at this zoomfactor
				// however, they will change zoomfactor

				// get the distanceToCenter if we go to our destination zoomfactor
				distanceToCenter.X *= zoomFactor;
				distanceToCenter.X /= changingToZoomedOutView ? _zoomMin : _zoomMax;
				distanceToCenter.Y *= zoomFactor;
				distanceToCenter.Y /= changingToZoomedOutView ? _zoomMin : _zoomMax;

				destinationCoordinateSystem.X = distanceToCenter.X + manipulatedElement.ActualWidth / 2;
				destinationCoordinateSystem.Y = distanceToCenter.Y + manipulatedElement.ActualHeight / 2;
			}

			destinationItem.Bounds = destinationCoordinateSystem;
		}

		// give the destination view the chance to determine a destination item
		destinationView?.StartViewChangeTo(sourceItem, destinationItem);

		// no need to transform this output, since it will already be in the destination coordinate system

		// Raise the ViewChangeStarted event
		var args = new SemanticZoomViewChangedEventArgs
		{
			IsSourceZoomedInView = changingToZoomedOutView,
			SourceItem = sourceItem,
			DestinationItem = destinationItem
		};
		OnViewChangeStarted(args);

		// Make the destination the active view
		if (sourceView is not null)
		{
			sourceView.IsActiveView = false;
		}
		if (destinationView is not null)
		{
			destinationView.IsActiveView = true;
		}

		// Move the destination item into view (we're doing it as we start the
		// transition animation so any animation it does will take place as the
		// cross fade happens)
		destinationItem = args.DestinationItem ?? destinationItem;

		if (destinationView is not null)
		{
			// the main call that will attempt to overlap the two views.
			destinationView.MakeVisible(destinationItem);

			// set correction transforms only during a pinch gesture.
			if (m_changePhase == SemanticZoomPhase.SemanticZoomPhase_DM_SwitchingViews)
			{
				// great, we have gotten a destination container as close to where it needs to be as possible
				// however, it could be that it was not able to completely get the destination container to
				// where it needed to be.

				// the delta that we still have to scroll is now in the destination bounds. We are introducing
				// that distance is in destination views coordinate system
				var remainder = destinationItem.Remainder;
				if (m_tpScrollViewer is not null)
				{
					if (changingToZoomedOutView && m_tpZoomedOutTransform is not null)
					{
						// the zoomedoutview was our target
						if (m_tpScrollViewer.HorizontalScrollMode == ScrollMode.Enabled)
						{
							m_tpZoomedOutTransform.TranslateX = remainder.X;
						}
						if (m_tpScrollViewer.VerticalScrollMode == ScrollMode.Enabled)
						{
							m_tpZoomedOutTransform.TranslateY = remainder.Y;
						}
					}
					else if (!changingToZoomedOutView && m_tpZoomedInTransform is not null)
					{
						// the zoomedinview was our target
						if (m_tpScrollViewer.HorizontalScrollMode == ScrollMode.Enabled)
						{
							m_tpZoomedInTransform.TranslateX = remainder.X * _zoomMin;
						}
						if (m_tpScrollViewer.VerticalScrollMode == ScrollMode.Enabled)
						{
							m_tpZoomedInTransform.TranslateY = remainder.Y * _zoomMin;
						}
					}
				}
			}
		}

		// if programmatic, we will animate ourselves
		if (m_tpScrollViewer is not null &&
			!m_isPendingViewChange &&
			m_changePhase == SemanticZoomPhase.SemanticZoomPhase_API_SwitchingViews &&
			m_tpZoomedInTransform is not null &&
			m_tpZoomedOutTransform is not null)
		{
			var bounds = CalculateBounds();
			if (SharedHelpers.IsInDesignModeV2())
			{
				ResetViewsAndSnapToActiveView();
			}
			else
			{
				m_tpScrollViewer.BringIntoViewport(
					bounds,
					false /*skipDuringTouchContact*/,
					false /*skipAnimationWhileRunning*/,
					true /*animate*/);
			}
		}

		// set hit-test visibility so that the front view (that might not be shown)
		// doesn't eat hits
		if (sourcePart is not null)
		{
			sourcePart.IsHitTestVisible = false;
			// This part is going out of view. We do not want it grabbing focus at that state.
			sourcePart.SkipFocusSubtree = true;
		}
		if (destinationPart is not null)
		{
			destinationPart.IsHitTestVisible = true;
			// This part is coming into view, it can start grabbing the focus once more.
			destinationPart.SkipFocusSubtree = false;
		}

		// Go to the destination's visual state
		HideZoomOutButton(false /* bUseTransitions */);
		UpdateVisualState(true);

		m_tpCompletedArgs = args; // store for later, used when the animation has ended

		// Raise the ViewChangeCompleted event
		sourceItem = args.SourceItem ?? sourceItem;
		destinationItem = args.DestinationItem ?? destinationItem;

		sourceView?.CompleteViewChangeFrom(sourceItem, destinationItem);
		destinationView?.CompleteViewChangeTo(sourceItem, destinationItem);

		args.SourceItem = sourceItem;
		args.DestinationItem = destinationItem;

		// normally we are deferred, but if the animation is not hooked we need to complete immediately
		if (m_isPendingViewChange ||
			(changingToZoomedOutView && !m_isZoomedOutViewAnimationHooked) ||
			(!changingToZoomedOutView && !m_isZoomedInViewAnimationHooked))
		{
			OnViewChangeCompleted(args);

			// we will ultimately always use DM to bring a SemanticZoomView into view.
			// so the complete should occur at the end of that animation.
			// If there was a pending view change or if there is no ScrollViewer in the
			// default template (such as the Phone's), we will not have used DM
			if ((m_isPendingViewChange || m_tpScrollViewer is null) &&
				m_calledInitializeViewChangeSinceManipulationStart)
			{
				m_calledInitializeViewChangeSinceManipulationStart = false;
				sourceView?.CompleteViewChange();
				destinationView?.CompleteViewChange();
			}
			ClearCompletedEventArgs();
		}
	}

	private void ViewChangeAnimationFinished(object? sender, object? args)
	{
		if (m_tpCompletedArgs is not { } completedArgs)
		{
			return;
		}

		// if contentview is active now, the source was the jumpview
		var sourceView = IsZoomedInViewActive ? ZoomedOutView : ZoomedInView;
		var destinationView = IsZoomedInViewActive ? ZoomedInView : ZoomedOutView;

		// this marks the event that we always raise after the
		// animation is done.
		OnViewChangeCompleted(completedArgs);

		// Cleanup the views only when DM session has already completed.
		// if DM has not yet completed, it means the session has not yet
		// ended, and we will call CompleteViewChange from DM.
		//
		// In it's turn, DM (NotifyStateChange) will forego calling CompleteViewChange
		// if it notices the animation has not finished yet (marked by m_tpCompletedArgs != null
		if (m_calledInitializeViewChangeSinceManipulationStart &&
			m_changePhase == SemanticZoomPhase.SemanticZoomPhase_Idle)
		{
			m_calledInitializeViewChangeSinceManipulationStart = false;
			sourceView?.CompleteViewChange();
			destinationView?.CompleteViewChange();
		}

		// clear out the args, this allows NotifyStateChange to complete ViewChanges if needed.
		ClearCompletedEventArgs();
	}

	// Calculate the position and bring the active view to view without
	// animations by re-evaluating the bounds we should be.
	private void ResetViewsAndSnapToActiveView()
	{
		// this continues the setup that was started in OnApplyTemplate. We need to have the ScrollViewers
		// template be expanded and have valid widths on our views.
		if (m_tpScrollViewer is not { } scrollViewer)
		{
			return;
		}

		if (m_hasAutomationPeer && GetOrCreateAutomationPeer() is SemanticZoomAutomationPeer automationPeer)
		{
			AutomationReparentPresenters(automationPeer);
		}

		var inZoomedInView = IsZoomedInViewActive;

		// In some cases (XAML-on-win32) this code can get called during a size-changed event, before the island has a valid
		// rasterization scale.  If the rasterization scale is still unset, fall through and get the monitor size from DisplayInformation.
		// This could be wrong in the case that the island is on a different monitor than the CoreWindow.
		// http://osgvsowi/19285997 Semantic zoom inside an island may initialize itself incorrectly
		var availableMonitorRect = XamlRoot?.VisualTree.VisibleBounds ?? default;
		if (availableMonitorRect.Width == 0 || availableMonitorRect.Height == 0)
		{
			// In OneCoreStrict/OneCoreTransforms mode the CalculateAvailableMonitorRect function isn't available.
			var displayInformation = DisplayInformation.GetForCurrentView();
			var widthPixels = displayInformation.ScreenWidthInRawPixels;
			var heightPixels = displayInformation.ScreenHeightInRawPixels;
			var zoomScale = RootScale.GetRasterizationScaleForElementWithFallback(this);

			// This isn't as good as the non-OneCoreTransforms path yet (CalculateAvailableMonitorRect has more logic than this)
			// but is good enough for now for our purposes.
			// http://osgvsowi/12283211 -- Remove use of win32-based monitor functions, use display regions instead
			availableMonitorRect = new Rect(
				0,
				0,
				widthPixels / zoomScale,
				heightPixels / zoomScale);
		}

		// Picking 6 times the screen size because of x2 for the scaling and 3x to have enough manipulation room around the displayed middle.
		var layoutSize = new Size(availableMonitorRect.Width * 6, availableMonitorRect.Height * 6);

		// this will stretch up the extent manually
		scrollViewer.SetLayoutSize(layoutSize);

		scrollViewer.ComputePixelViewportWidth(null, false, out var viewportWidth);
		scrollViewer.ComputePixelViewportHeight(null, false, out var viewportHeight);
		scrollViewer.ComputePixelExtentWidth(out var extentWidth);
		scrollViewer.ComputePixelExtentHeight(out var extentHeight);

		// the manipulated element is scaled up twice, so takes up two widths
		m_manipulatedElementOffset.X = extentWidth / 2 - viewportWidth;
		m_manipulatedElementOffset.Y = extentHeight / 2 - viewportHeight;

		// so we place it smack in the middle of our stretched up ScrollViewer
		if (m_tpManipulatedElementTransform is not null)
		{
			m_tpManipulatedElementTransform.TranslateX = m_manipulatedElementOffset.X;
			m_tpManipulatedElementTransform.TranslateY = m_manipulatedElementOffset.Y;
		}

		// calculate the horizontal and vertical offsets to scroll the ScrollViewer to. Note that we might end up in a sub-pixel boundary and cause jiggling when you resize
		// the window. To avoid this, we layout round the offsets if layout rounding is enabled (which by default it is).
		var horizontalOffset =
			(m_manipulatedElementOffset.X + ZoomedInCenteringCorrectionX(inZoomedInView, viewportWidth)) *
			(inZoomedInView ? _zoomMax : _zoomMin);
		var verticalOffset =
			(m_manipulatedElementOffset.Y + ZoomedInCenteringCorrectionY(inZoomedInView, viewportHeight)) *
			(inZoomedInView ? _zoomMax : _zoomMin);
		if (UseLayoutRounding)
		{
			horizontalOffset = LayoutRound(horizontalOffset);
			verticalOffset = LayoutRound(verticalOffset);
		}

		// start off scrolled to either the zoomed in or the zoomed out view
		scrollViewer.ScrollToHorizontalOffsetInternal(horizontalOffset);
		scrollViewer.ScrollToVerticalOffsetInternal(verticalOffset);
		scrollViewer.ZoomToFactorInternal(inZoomedInView ? _zoomMax : _zoomMin, true, out _);

		// remove individual corrections
		if (m_tpZoomedInTransform is not null)
		{
			m_tpZoomedInTransform.TranslateX = 0;
			m_tpZoomedInTransform.TranslateY = 0;
		}
		if (m_tpZoomedOutTransform is not null)
		{
			m_tpZoomedOutTransform.TranslateX = 0;
			m_tpZoomedOutTransform.TranslateY = 0;
		}
	}

	// handle the size changed event on the SeZo itself
	// by re-evaluating the bounds we should be at.
	// reacting to size changes on itself
	private void OnSizeChanged(object sender, SizeChangedEventArgs args) => ResetViewsAndSnapToActiveView();

	// helper function to calculate the bounds we would like to be at
	// helper method that will calculate the bounds that we would like to be at
	private Rect CalculateBounds()
	{
		var bounds = default(Rect);
		var inZoomedInView = IsZoomedInViewActive;
		if (m_tpScrollViewer is not null &&
			m_tpZoomedInTransform is not null &&
			m_tpZoomedOutTransform is not null)
		{
			m_tpScrollViewer.ComputePixelViewportWidth(null, false, out var viewportWidth);
			m_tpScrollViewer.ComputePixelViewportHeight(null, false, out var viewportHeight);

			double correctionX;
			double correctionY;
			if (inZoomedInView)
			{
				correctionX = m_tpZoomedInTransform.TranslateX;
				correctionY = m_tpZoomedInTransform.TranslateY;

				// this correction was applied on a surface that was scaled up
				// so will calculate appropriately
				correctionX /= _zoomMin;
				correctionY /= _zoomMin;

				// we are going to _zoomMax
				bounds.Width = viewportWidth / _zoomMax;
				bounds.Height = viewportHeight / _zoomMax;
			}
			else
			{
				correctionX = m_tpZoomedOutTransform.TranslateX;
				correctionY = m_tpZoomedOutTransform.TranslateY;

				// this correction was applied on a surface that was scaled up
				// so will calculate appropriately
				correctionX /= _zoomMin;
				correctionY /= _zoomMin;

				// we are going to _zoomMin
				bounds.Width = viewportWidth / _zoomMin;
				bounds.Height = viewportHeight / _zoomMin;
			}

			bounds.X = m_manipulatedElementOffset.X +
				ZoomedInCenteringCorrectionX(inZoomedInView, viewportWidth) +
				correctionX;
			bounds.Y = m_manipulatedElementOffset.Y +
				ZoomedInCenteringCorrectionY(inZoomedInView, viewportHeight) +
				correctionY;
		}

		return bounds;
	}

	// Called whenever the ZoomOutButton is clicked.
	private void OnZoomOutButtonClick(object sender, RoutedEventArgs args) =>
		ToggleActiveViewWithFocusState(FocusState.Pointer);

	// Helper function to add an EventHandler to the Completed event of the given VisualState.
	private static void AddStoryboardCompletedHandler(
		VisualState state,
		EventHandler<object> handler,
		CompositeDisposable subscriptions)
	{
		if (state.Storyboard is { } storyboard)
		{
			storyboard.Completed += handler;
			subscriptions.Add(Disposable.Create(() => storyboard.Completed -= handler));
		}
	}

	// When the ZoomOutButtonVisible state's Storyboard completes, kicks off the delay-hide transition.
	private void OnZoomOutButtonVisibleStoryboardCompleted(object? sender, object args) =>
		HideZoomOutButton(true /* bUseTransitions */);

	// PointerMoved event handler.
	protected override void OnPointerMoved(PointerRoutedEventArgs args)
	{
		base.OnPointerMoved(args);

		if (!m_isProcessingViewChange &&
			IsZoomedInViewActive &&
			m_isZoomOutButtonEnabled &&
			args.Pointer.PointerDeviceType != PointerDeviceType.Touch)
		{
			// Mouse input dominates. If we are showing panning indicators and then mouse comes into play, mouse indicators win.
			// Even though we are taking action here, we choose not to handle the event and to let it keep routing.
			// We just use this event to detect that the ZoomOutButton needs to be re-shown (and its fade-out reset if
			// it is currently showing).
			// This is consistent with ScrollViewer::OnPointerMoved() displaying the scrolling indicators but marking the args as handled.
			ShowZoomOutButton();
		}
	}

	// Shows the ZoomOutButton.
	private void ShowZoomOutButton()
	{
		MUX_ASSERT(m_isZoomOutButtonEnabled);
		VisualStateManager.GoToState(this, "ZoomOutButtonVisible", true);
	}

	// Hides the ZoomOutButton.
	private void HideZoomOutButton(bool useTransitions)
	{
		VisualStateManager.GoToState(this, "ZoomOutButtonHidden", useTransitions);

		if (m_tpZoomOutButton is not null)
		{
			m_tpZoomOutButton.IsPointerOver = false;
		}
	}

	// Called when the element enters the tree.
	private void EnterImpl(bool isLive, bool skipNameRegistration, bool coercedIsEnabled, bool useLayoutRounding)
	{
		// During CUIElement::LeaveImpl we clear the skip focus subtree.
		// The code below ensures that upon entering back into scope the
		// SemanticZoom will SetSkipFocusSubtree on the correct portions.
		if (isLive)
		{
			if (m_tpZoomedInPresenterPart is not null)
			{
				m_tpZoomedInPresenterPart.SkipFocusSubtree = !IsZoomedInViewActive;
			}

			if (m_tpZoomedOutPresenterPart is not null)
			{
				m_tpZoomedOutPresenterPart.SkipFocusSubtree = IsZoomedInViewActive;
			}
		}

		_ = skipNameRegistration;
		_ = coercedIsEnabled;
		_ = useLayoutRounding;
	}

	private bool TryGetVisualState(string stateName, out VisualStateGroup group, out VisualState state)
	{
		group = null!;
		state = null!;
		var templateRoot = GetTemplateRoot();
		if (templateRoot is null)
		{
			return false;
		}

		foreach (var candidateGroup in VisualStateManager.GetVisualStateGroups(templateRoot))
		{
			foreach (var candidateState in candidateGroup.States)
			{
				if (candidateState.Name == stateName)
				{
					group = candidateGroup;
					state = candidateState;
					return true;
				}
			}
		}

		return false;
	}

	private static void MakeStoryboardEssential(Storyboard storyboard)
	{
		foreach (var timeline in storyboard.Children)
		{
			switch (timeline)
			{
				case DoubleAnimation animation:
					animation.EnableDependentAnimation = true;
					break;
				case DoubleAnimationUsingKeyFrames animation:
					animation.EnableDependentAnimation = true;
					break;
				case ColorAnimation animation:
					animation.EnableDependentAnimation = true;
					break;
				case ColorAnimationUsingKeyFrames animation:
					animation.EnableDependentAnimation = true;
					break;
				case Storyboard nestedStoryboard:
					MakeStoryboardEssential(nestedStoryboard);
					break;
			}
		}
	}

	internal bool TryGetFocusState(out FocusState focusState)
	{
		focusState = FocusState.Programmatic;
		if (XamlRoot is null || FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focusedElement)
		{
			return false;
		}

		for (var current = focusedElement; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, this))
			{
				if (focusedElement is Control focusedControl &&
					focusedControl.FocusState is FocusState.Keyboard or FocusState.Pointer)
				{
					focusState = focusedControl.FocusState;
				}
				return true;
			}
		}

		return false;
	}
}
