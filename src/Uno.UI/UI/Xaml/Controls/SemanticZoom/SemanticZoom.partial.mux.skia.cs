// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoom_Partial.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class SemanticZoom
{
	// Initializes a new instance of the SemanticZoom class.
	public SemanticZoom()
	{
		this.SetDefaultStyleKey();

		m_zoomOutButtonHideTimer = DispatcherQueue.CreateTimer();
		m_zoomOutButtonHideTimer.Interval = TimeSpan.FromMilliseconds(100);
		m_zoomOutButtonHideTimer.IsRepeating = false;
		m_zoomOutButtonHideTimer.Tick += OnZoomOutButtonHideTimerTick;

		Unloaded += OnUnloaded;
	}

	// Associate the SemanticZoom with an ISemanticZoomInformation view.
	private void InitializeSemanticZoomInformation(
		ISemanticZoomInformation? oldView,
		ISemanticZoomInformation? newView,
		bool isZoomedInView)
	{
		if (oldView is not null)
		{
			oldView.SemanticZoomOwner = null;
			oldView.IsActiveView = false;
			oldView.IsZoomedInView = true;
		}

		if (newView is not null)
		{
			newView.SemanticZoomOwner = this;
			newView.IsZoomedInView = isZoomedInView;
			newView.IsActiveView = isZoomedInView == IsZoomedInViewActive;
		}

		if (m_isProcessingViewChange)
		{
			m_viewsChangedDuringViewChange = true;
			m_isPendingViewChange = true;
		}
	}

	private void OnIsZoomedInViewActiveChanged(bool oldValue, bool newValue)
	{
		if (m_restoringActiveView)
		{
			return;
		}

		if (!CanChangeViews)
		{
			try
			{
				m_restoringActiveView = true;
				SetValue(IsZoomedInViewActiveProperty, oldValue);
			}
			finally
			{
				m_restoringActiveView = false;
			}

			return;
		}

		RaiseToggleStatePropertyChangedEvent(oldValue, newValue);

		if (m_isInitializing)
		{
			// If a change was already scheduled, cancel the view change so we do not
			// toggle to the incorrect view.
			m_isPendingViewChange = !m_isPendingViewChange;
			return;
		}

		if (m_isProcessingViewChange)
		{
			m_isPendingViewChange = true;
			return;
		}

		// Skia does not currently provide a reliable completion signal for theme storyboards.
		// Snap between views so lifecycle completion has one authoritative owner.
		ChangeViews(useTransitions: false);
	}

	private void OnIsZoomOutButtonEnabledChanged(bool value)
	{
		m_isZoomOutButtonEnabled = value;
		UpdateZoomOutButton();
	}

	protected override void OnApplyTemplate()
	{
		m_templateSubscriptions.Disposable = null;
		base.OnApplyTemplate();

		m_tpZoomedInPresenterPart = GetTemplateChild<ContentPresenter>(c_zoomedInPresenterName);
		m_tpZoomedOutPresenterPart = GetTemplateChild<ContentPresenter>(c_zoomedOutPresenterName);
		m_tpScrollViewer = GetTemplateChild<ScrollViewer>(c_scrollViewerName);
		m_tpZoomOutButton = GetTemplateChild<Button>(c_zoomOutButtonName);

		var registrations = new CompositeDisposable();
		m_templateSubscriptions.Disposable = registrations;

		if (m_tpZoomOutButton is { } zoomOutButton)
		{
			zoomOutButton.Click += OnZoomOutButtonClick;
			registrations.Add(() => zoomOutButton.Click -= OnZoomOutButtonClick);
		}

		if (m_tpScrollViewer is { } scrollViewer)
		{
			scrollViewer.BringIntoViewOnFocusChange = false;
		}

		m_isInitializing = false;

		if (m_isPendingViewChange)
		{
			ChangeViews(useTransitions: false);
		}
		else
		{
			UpdateActivePresenters();
		}

		m_isPendingViewChange = false;
		UpdateZoomOutButton();
	}

	/// <summary>
	/// Switches the control between the zoomed-in and zoomed-out views.
	/// </summary>
	public void ToggleActiveView()
	{
		if (!CanChangeViews)
		{
			return;
		}

		var newValue = !IsZoomedInViewActive;
		IsZoomedInViewActive = newValue;
	}

	internal bool ToggleActiveViewFromHeaderItem()
	{
		var oldValue = IsZoomedInViewActive;
		ToggleActiveView();
		return oldValue != IsZoomedInViewActive;
	}

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

			ToggleActiveView();
		}
		finally
		{
			m_isProcessingKeyboardInput = false;
			m_isProcessingPointerInput = false;
		}
	}

	private void ChangeViews(bool useTransitions)
	{
		CompletePendingViewChange();

		m_isProcessingViewChange = true;

		// IsZoomedInViewActive has already been updated by the time this method is called.
		var changingToZoomedOutView = !IsZoomedInViewActive;
		var sourceView = changingToZoomedOutView ? ZoomedInView : ZoomedOutView;
		var destinationView = changingToZoomedOutView ? ZoomedOutView : ZoomedInView;
		var sourcePresenter = changingToZoomedOutView ? m_tpZoomedInPresenterPart : m_tpZoomedOutPresenterPart;
		var destinationPresenter = changingToZoomedOutView ? m_tpZoomedOutPresenterPart : m_tpZoomedInPresenterPart;

		try
		{
			sourceView?.InitializeViewChange();
			destinationView?.InitializeViewChange();

			var sourceItem = new SemanticZoomLocation();
			var destinationItem = new SemanticZoomLocation();

			if (m_emulatingGesture)
			{
				sourceItem.ZoomPoint = TransformZoomPointToView(sourceView);
				destinationItem.ZoomPoint = TransformZoomPointToView(destinationView);
			}

			sourceView?.StartViewChangeFrom(sourceItem, destinationItem);

			if (sourcePresenter is not null && destinationPresenter is not null)
			{
				destinationItem.Bounds = sourcePresenter
					.TransformToVisual(destinationPresenter)
					.TransformBounds(sourceItem.Bounds);
			}
			else
			{
				destinationItem.Bounds = sourceItem.Bounds;
			}

			destinationView?.StartViewChangeTo(sourceItem, destinationItem);

			var args = new SemanticZoomViewChangedEventArgs
			{
				IsSourceZoomedInView = changingToZoomedOutView,
				SourceItem = sourceItem,
				DestinationItem = destinationItem,
			};

			ViewChangeStarted?.Invoke(this, args);

			if (sourceView is not null)
			{
				sourceView.IsActiveView = false;
			}

			if (destinationView is not null)
			{
				destinationView.IsActiveView = true;
			}

			if (sourcePresenter is not null)
			{
				sourcePresenter.Visibility = Visibility.Visible;
				sourcePresenter.IsHitTestVisible = false;
			}

			if (destinationPresenter is not null)
			{
				destinationPresenter.Visibility = Visibility.Visible;
				destinationPresenter.IsHitTestVisible = false;
			}

			destinationView?.MakeVisible(args.DestinationItem);

			if (destinationPresenter is not null)
			{
				destinationPresenter.IsHitTestVisible = true;
			}

			sourceView?.CompleteViewChangeFrom(args.SourceItem, args.DestinationItem);
			destinationView?.CompleteViewChangeTo(args.SourceItem, args.DestinationItem);

			m_tpCompletedArgs = args;
			m_tpSourceView = sourceView;
			m_tpDestinationView = destinationView;
			m_tpSourcePresenter = sourcePresenter;
			m_tpDestinationPresenter = destinationPresenter;

			HideZoomOutButton(useTransitions: false);
			CompletePendingViewChange();
		}
		finally
		{
			if (m_isProcessingViewChange)
			{
				AbortPendingViewChange();
			}
		}
	}

	private Point TransformZoomPointToView(ISemanticZoomInformation? view)
	{
		if (view is UIElement element)
		{
			return TransformToVisual(element).TransformPoint(m_zoomPoint);
		}

		return default;
	}

	private void CompletePendingViewChange()
	{
		if (!m_isProcessingViewChange || m_tpCompletedArgs is null)
		{
			return;
		}
		if (m_tpSourcePresenter is not null)
		{
			m_tpSourcePresenter.Visibility = Visibility.Collapsed;
			m_tpSourcePresenter.IsHitTestVisible = false;
		}

		if (m_tpDestinationPresenter is not null)
		{
			m_tpDestinationPresenter.Visibility = Visibility.Visible;
			m_tpDestinationPresenter.IsHitTestVisible = true;
		}

		var args = m_tpCompletedArgs;
		var sourceView = m_tpSourceView;
		var destinationView = m_tpDestinationView;

		m_tpCompletedArgs = null;
		m_tpSourceView = null;
		m_tpDestinationView = null;
		m_tpSourcePresenter = null;
		m_tpDestinationPresenter = null;
		var callbacksSucceeded = false;
		try
		{
			try
			{
				ViewChangeCompleted?.Invoke(this, args);
			}
			finally
			{
				try
				{
					sourceView?.CompleteViewChange();
				}
				finally
				{
					destinationView?.CompleteViewChange();
				}
			}

			try
			{
				if (sourceView is not null &&
					!ReferenceEquals(sourceView, ZoomedInView) &&
					!ReferenceEquals(sourceView, ZoomedOutView))
				{
					sourceView.IsActiveView = false;
				}
			}
			finally
			{
				if (destinationView is not null &&
					!ReferenceEquals(destinationView, ZoomedInView) &&
					!ReferenceEquals(destinationView, ZoomedOutView))
				{
					destinationView.IsActiveView = false;
				}
			}

			callbacksSucceeded = true;
		}
		finally
		{
			m_isProcessingViewChange = false;
			var runPendingViewChange =
				m_viewsChangedDuringViewChange ||
				(m_isPendingViewChange && !AreViewRolesCurrent());
			m_isPendingViewChange = false;
			m_viewsChangedDuringViewChange = false;

			if (runPendingViewChange && callbacksSucceeded)
			{
				ChangeViews(useTransitions: false);
			}
			else
			{
				ReconcileCurrentView();
			}

			UpdateZoomOutButton();
		}
	}

	private void AbortPendingViewChange()
	{
		m_tpCompletedArgs = null;
		m_tpSourceView = null;
		m_tpDestinationView = null;
		m_tpSourcePresenter = null;
		m_tpDestinationPresenter = null;
		m_isPendingViewChange = false;
		m_viewsChangedDuringViewChange = false;
		m_isProcessingViewChange = false;

		ReconcileCurrentView();
		UpdateZoomOutButton();
	}

	private void ReconcileCurrentView()
	{
		if (ZoomedInView is not null)
		{
			ZoomedInView.IsActiveView = IsZoomedInViewActive;
		}

		if (ZoomedOutView is not null)
		{
			ZoomedOutView.IsActiveView = !IsZoomedInViewActive;
		}

		UpdateActivePresenters();
	}

	private bool AreViewRolesCurrent()
		=> (ZoomedInView is null || ZoomedInView.IsActiveView == IsZoomedInViewActive) &&
			(ZoomedOutView is null || ZoomedOutView.IsActiveView != IsZoomedInViewActive);

	private void UpdateActivePresenters()
	{
		if (m_tpZoomedInPresenterPart is not null)
		{
			m_tpZoomedInPresenterPart.Visibility =
				IsZoomedInViewActive ? Visibility.Visible : Visibility.Collapsed;
			m_tpZoomedInPresenterPart.IsHitTestVisible = IsZoomedInViewActive;
		}

		if (m_tpZoomedOutPresenterPart is not null)
		{
			m_tpZoomedOutPresenterPart.Visibility =
				IsZoomedInViewActive ? Visibility.Collapsed : Visibility.Visible;
			m_tpZoomedOutPresenterPart.IsHitTestVisible = !IsZoomedInViewActive;
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer()
		=> new SemanticZoomAutomationPeer(this);

	internal FrameworkElement? AutomationGetActivePresenter()
		=> IsZoomedInViewActive ? m_tpZoomedInPresenterPart : m_tpZoomedOutPresenterPart;

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		if (args.Handled || !CanChangeViews)
		{
			return;
		}

		try
		{
			m_isProcessingKeyboardInput = true;

			var modifiers = args.KeyboardModifiers & ~VirtualKeyModifiers.Shift;
			if (modifiers != VirtualKeyModifiers.Control)
			{
				return;
			}

			if (IsZoomedInViewActive &&
				(args.Key == VirtualKey.Subtract || (int)args.Key == 189))
			{
				IsZoomedInViewActive = false;
				args.Handled = true;
			}
			else if (!IsZoomedInViewActive &&
				(args.Key == VirtualKey.Add || (int)args.Key == 187))
			{
				IsZoomedInViewActive = true;
				args.Handled = true;
			}
		}
		finally
		{
			m_isProcessingKeyboardInput = false;
		}
	}

	protected override void OnPointerWheelChanged(PointerRoutedEventArgs args)
	{
		base.OnPointerWheelChanged(args);

		if (args.Handled ||
			!CanChangeViews ||
			m_tpScrollViewer is not { ZoomMode: not ZoomMode.Disabled })
		{
			return;
		}

		var modifiers = CoreImports.Input_GetKeyboardModifiers();
		if ((modifiers & VirtualKeyModifiers.Control) == 0)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(this);
		m_zoomPoint = pointerPoint.Position;

		try
		{
			m_isProcessingPointerInput = true;
			m_emulatingGesture = true;

			if (!IsZoomedInViewActive && pointerPoint.Properties.MouseWheelDelta > 0)
			{
				IsZoomedInViewActive = true;
				args.Handled = true;
			}
			else if (IsZoomedInViewActive && pointerPoint.Properties.MouseWheelDelta < 0)
			{
				IsZoomedInViewActive = false;
				args.Handled = true;
			}
		}
		finally
		{
			m_emulatingGesture = false;
			m_isProcessingPointerInput = false;
		}
	}

	protected override void OnPointerMoved(PointerRoutedEventArgs args)
	{
		base.OnPointerMoved(args);

		if (!m_isProcessingViewChange &&
			IsZoomedInViewActive &&
			m_isZoomOutButtonEnabled &&
		args.Pointer.PointerDeviceType is not PointerDeviceType.Touch)
		{
			ShowZoomOutButton();
		}
	}

	private void OnZoomOutButtonClick(object sender, RoutedEventArgs args)
		=> ToggleActiveViewWithFocusState(FocusState.Pointer);

	private void ShowZoomOutButton()
	{
		if (!m_isZoomOutButtonEnabled || !IsZoomedInViewActive || m_tpZoomOutButton is null)
		{
			return;
		}

		m_tpZoomOutButton.Visibility = Visibility.Visible;
		VisualStateManager.GoToState(this, c_zoomOutButtonVisibleState, useTransitions: true);

		m_zoomOutButtonHideTimer.Stop();
		m_zoomOutButtonHideTimer.Start();
	}

	private void HideZoomOutButton(bool useTransitions)
	{
		m_zoomOutButtonHideTimer.Stop();
		VisualStateManager.GoToState(this, c_zoomOutButtonHiddenState, useTransitions);
	}

	private void UpdateZoomOutButton()
	{
		if (m_tpZoomOutButton is null)
		{
			return;
		}

		var shouldBeAvailable = m_isZoomOutButtonEnabled && IsZoomedInViewActive;
		m_tpZoomOutButton.Visibility =
			shouldBeAvailable ? Visibility.Visible : Visibility.Collapsed;

		if (!shouldBeAvailable)
		{
			HideZoomOutButton(useTransitions: false);
		}
	}

	private void OnZoomOutButtonHideTimerTick(
		Microsoft.UI.Dispatching.DispatcherQueueTimer sender,
		object args)
		=> HideZoomOutButton(useTransitions: true);

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		m_zoomOutButtonHideTimer.Stop();
		CompletePendingViewChange();
	}

	private void RaiseToggleStatePropertyChangedEvent(bool oldValue, bool newValue)
	{
		if (FrameworkElementAutomationPeer.FromElement(this) is SemanticZoomAutomationPeer peer)
		{
			peer.RaiseToggleStatePropertyChangedEvent(oldValue, newValue);
		}
	}

	internal bool TryGetFocusState(out FocusState focusState)
	{
		focusState = FocusState.Programmatic;

		if (XamlRoot is null ||
			FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focusedElement)
		{
			return false;
		}

		if (focusedElement is UIElement focusedUIElement &&
			focusedUIElement.FocusState != FocusState.Unfocused)
		{
			focusState = focusedUIElement.FocusState;
		}

		return focusedElement == this || this.IsAncestorOf(focusedElement);
	}

}
