// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitView.cpp, commit 67aeb8f23

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitView
{
	private readonly SerialDisposable m_keyDownRevoker = new();
	private readonly SerialDisposable m_lightDismissLayerPointerReleasedRevoker = new();

	public SplitView()
	{
		DefaultStyleKey = typeof(SplitView);

		TemplateSettings = new SplitViewTemplateSettings();
	}

	protected override void OnApplyTemplate()
	{
		UnregisterCoreEventHandlers();

		// Clear any hold-overs from the previous template.
		m_paneClipRectangle = null;
		m_coreContentRoot = null;
		m_corePaneRoot = null;
		m_coreLightDismissLayer = null;

		base.OnApplyTemplate();

		m_paneClipRectangle = GetTemplateChild(c_paneClipRectangle) as RectangleGeometry;
		m_coreContentRoot = GetTemplateChild(c_contentRoot) as UIElement;
		m_corePaneRoot = GetTemplateChild(c_paneRoot) as UIElement;
		m_coreLightDismissLayer = GetTemplateChild(c_lightDismissLayer) as UIElement;

		// Framework layer OnApplyTemplate additions (from SplitView_Partial.cpp)
		OnApplyTemplatePartial();

		RegisterCoreEventHandlers();
		UpdateTemplateSettings();
		UpdateVisualState(true);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		// Measure the pane content so that we can use the desired size in cases
		// where open pane length is set to Auto.
		var pane = Pane;
		if (pane is not null)
		{
			pane.Measure(availableSize);
			m_paneMeasuredLength = pane.DesiredSize.Width;
		}

		var desiredSize = base.MeasureOverride(availableSize);

		UpdateTemplateSettings();

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var newFinalSize = base.ArrangeOverride(finalSize);

		if (m_paneClipRectangle is not null)
		{
#if __SKIA__
			// On Skia, PaneClipRectangleTransform (TranslateTransform on RectangleGeometry.Transform)
			// is handled by the XAML template animations. We just set the base rect.
			m_paneClipRectangle.Rect = new Rect(0, 0, GetOpenPaneLengthValue(), newFinalSize.Height);
#else
			// Uno specific: Geometry.Transform is not supported on non-Skia targets (issue #3747).
			// Directly set PaneClipRectangle.Rect instead of relying on PaneClipRectangleTransform.
			UpdatePaneClipRectangleNonSkia(newFinalSize);
#endif
		}

		return newFinalSize;
	}

#if !__SKIA__
	private void UpdatePaneClipRectangleNonSkia(Size finalSize)
	{
		if (m_paneClipRectangle is null)
		{
			return;
		}

		var openPaneLength = GetOpenPaneLengthValue();
		var compactLength = CompactPaneLength;
		var displayMode = DisplayMode;
		var placement = PanePlacement;
		var isPaneOpen = IsPaneOpen;

		double clipX = 0;
		double clipWidth = openPaneLength;

		if (!isPaneOpen)
		{
			switch (displayMode)
			{
				case SplitViewDisplayMode.CompactOverlay:
				case SplitViewDisplayMode.CompactInline:
					clipWidth = compactLength;
					if (placement == SplitViewPanePlacement.Right)
					{
						clipX = openPaneLength - compactLength;
					}
					break;
				case SplitViewDisplayMode.Overlay:
				case SplitViewDisplayMode.Inline:
					clipWidth = 0;
					break;
			}
		}

		m_paneClipRectangle.Rect = new Rect(clipX, 0, clipWidth, finalSize.Height);
	}
#endif

	private void OnCompactPaneLengthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		UpdateTemplateSettings();

		// Force the bindings in our VisualState animations to refresh by intentionally
		// passing in false for 'useTransitions.'
		UpdateVisualState(false);
	}

	private void OnOpenPaneLengthPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		UpdateTemplateSettings();

		// Force the bindings in our VisualState animations to refresh by intentionally
		// passing in false for 'useTransitions.'
		UpdateVisualState(false);
	}

	private void OnDisplayModePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		RestoreSavedFocusElement();
		OnDisplayModeChanged();
		UpdateVisualState(true);
	}

	private void OnPanePlacementPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		UpdateVisualState(true);
	}

	private void OnLightDismissOverlayModePropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		UpdateVisualState(true);
	}

	private void OnIsPaneOpenPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		// Handled by the framework layer (SplitView.partial.mux.cs) OnIsPaneOpenChanged
		OnIsPaneOpenChanged((bool)e.NewValue);
	}

	internal bool IsLightDismissible()
	{
		var displayMode = DisplayMode;
		return displayMode != SplitViewDisplayMode.Inline &&
			   displayMode != SplitViewDisplayMode.CompactInline;
	}

	internal bool CanLightDismiss()
	{
		return IsPaneOpen && !m_isPaneClosingByLightDismiss && IsLightDismissible();
	}

	private double GetOpenPaneLengthValue()
	{
		var openPaneLength = OpenPaneLength;

		// Support Auto/NaN for open pane length to size to the pane content.
		if (double.IsNaN(openPaneLength))
		{
			openPaneLength = m_paneMeasuredLength;
		}

		return openPaneLength;
	}

	internal void TryCloseLightDismissiblePane()
	{
		if (!CanLightDismiss())
		{
			return;
		}

		var args = new SplitViewPaneClosingEventArgs();

		// Raise the closing event to give the app a chance to cancel.
		PaneClosing?.Invoke(this, args);

		// Flag that we're attempting to close so that we don't queue up multiple of these messages.
		m_isPaneClosingByLightDismiss = true;

		// Use dispatcher to defer the actual close, allowing the app to process the cancel.
		_ = Dispatcher.RunAsync(
			global::Windows.UI.Core.CoreDispatcherPriority.Normal,
			() =>
			{
				if (args.Cancel)
				{
					OnCancelClosing();
				}
				else
				{
					IsPaneOpen = false;
				}
			});
	}

	private void OnCancelClosing()
	{
		m_isPaneClosingByLightDismiss = false;
	}

	private void OnPaneOpening()
	{
		// Try to focus the pane if it's light-dismissible.
		if (IsLightDismissible() && m_corePaneRoot is not null)
		{
			SetFocusToPane();
		}
	}

	private void OnPaneClosing()
	{
		// If the closing flag isn't set, then we're not closing due to some light-dismissible
		// action but rather are closing because the app explicitly set IsPaneOpen = false.
		// In this case, we haven't fired the PaneClosing event yet, so do it now before we
		// fire the PaneClosed event. Note, this closing action is not cancelable, so we
		// don't care if the app sets the Cancel property on the closing event args.
		if (!m_isPaneClosingByLightDismiss)
		{
			var args = new SplitViewPaneClosingEventArgs();
			PaneClosing?.Invoke(this, args);
		}

		if (IsLightDismissible())
		{
			RestoreSavedFocusElement();
		}
	}

	private void OnPaneClosed()
	{
		m_isPaneClosingByLightDismiss = false;

		// Fire the closed event.
		PaneClosed?.Invoke(this, null);
	}

	private void SetFocusToPane()
	{
		// Store weak reference to the previously focused element.
		if (XamlRoot is { } xamlRoot)
		{
			var prevFocusedElement = FocusManager.GetFocusedElement(xamlRoot) as DependencyObject;
			if (prevFocusedElement is not null)
			{
				m_prevFocusedElementWeakRef = new WeakReference<DependencyObject>(prevFocusedElement);

				if (prevFocusedElement is UIElement prevFocusedUIElement)
				{
					m_prevFocusState = prevFocusedUIElement.FocusState;
				}
			}
		}

		if (m_prevFocusState == FocusState.Unfocused)
		{
			// We will give the pane focus using the same focus state as that of the currently focused element.
			// If there is no currently focused element we will fall back to Programmatic focus state.
			m_prevFocusState = FocusState.Programmatic;
		}

		// Put focus on the pane.
		if (m_corePaneRoot is UIElement paneRoot)
		{
			paneRoot.Focus(m_prevFocusState);
		}
	}

	private void RestoreSavedFocusElement()
	{
		if (m_prevFocusedElementWeakRef is null)
		{
			return;
		}

		bool wasFocusRestored = false;

		if (m_prevFocusedElementWeakRef.TryGetTarget(out var prevFocusedElement) &&
			prevFocusedElement is UIElement prevFocusedUIElement)
		{
			wasFocusRestored = prevFocusedUIElement.Focus(m_prevFocusState);
		}

		// If we failed to restore focus, then try to focus an item in the content area.
		if (!wasFocusRestored && m_coreContentRoot is UIElement contentRoot)
		{
			contentRoot.Focus(m_prevFocusState);
		}

		// Reset our saved focus information.
		m_prevFocusedElementWeakRef = null;
		m_prevFocusState = FocusState.Unfocused;
	}

	private void UpdateTemplateSettings()
	{
		var templateSettings = TemplateSettings;
		if (templateSettings is null)
		{
			return;
		}

		var openPaneLength = GetOpenPaneLengthValue();
		var compactLength = CompactPaneLength;

		templateSettings.OpenPaneLength = openPaneLength;
		templateSettings.NegativeOpenPaneLength = -openPaneLength;
		templateSettings.OpenPaneLengthMinusCompactLength = openPaneLength - compactLength;
		templateSettings.NegativeOpenPaneLengthMinusCompactLength = compactLength - openPaneLength;
		templateSettings.OpenPaneGridLength = new GridLength(openPaneLength, GridUnitType.Pixel);
		templateSettings.CompactPaneGridLength = new GridLength(compactLength, GridUnitType.Pixel);

#if !__SKIA__
		// Uno specific: Update LeftClip/RightClip workaround properties for non-Skia targets
		// where Geometry.Transform is not supported (issue #3747).
		var viewHeight = ActualHeight > 0 ? ActualHeight : 2000;
		templateSettings.LeftClip = new RectangleGeometry { Rect = new Rect(0, 0, compactLength, viewHeight) };
		templateSettings.RightClip = new RectangleGeometry { Rect = new Rect(openPaneLength - compactLength, 0, compactLength, viewHeight) };
#endif
	}

	private void RegisterCoreEventHandlers()
	{
		KeyDown += OnSplitViewKeyDown;
		m_keyDownRevoker.Disposable = Disposable.Create(() => KeyDown -= OnSplitViewKeyDown);

		if (m_coreLightDismissLayer is { } lightDismissLayer)
		{
			lightDismissLayer.PointerReleased += OnLightDismissLayerPointerReleased;
			m_lightDismissLayerPointerReleasedRevoker.Disposable =
				Disposable.Create(() => lightDismissLayer.PointerReleased -= OnLightDismissLayerPointerReleased);
		}
	}

	private void UnregisterCoreEventHandlers()
	{
		m_keyDownRevoker.Disposable = null;
		m_lightDismissLayerPointerReleasedRevoker.Disposable = null;
	}

	private void OnSplitViewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Handled)
		{
			return;
		}

		// Core layer: Handles light-dismiss modes (Overlay, CompactOverlay).
		// Escape/Back closes the pane; gamepad traps focus within pane.
		if (CanLightDismiss())
		{
			switch (e.OriginalKey)
			{
				case VirtualKey.Escape:
				case VirtualKey.GamepadB:
					e.Handled = true;
					TryCloseLightDismissiblePane();
					return;

				case VirtualKey.GamepadDPadLeft:
				case VirtualKey.GamepadLeftThumbstickLeft:
					HandleLightDismissGamepadNavigation(e, FocusNavigationDirection.Left);
					return;

				case VirtualKey.GamepadDPadRight:
				case VirtualKey.GamepadLeftThumbstickRight:
					HandleLightDismissGamepadNavigation(e, FocusNavigationDirection.Right);
					return;

				case VirtualKey.GamepadDPadUp:
				case VirtualKey.GamepadLeftThumbstickUp:
					HandleLightDismissGamepadNavigation(e, FocusNavigationDirection.Up);
					return;

				case VirtualKey.GamepadDPadDown:
				case VirtualKey.GamepadLeftThumbstickDown:
					HandleLightDismissGamepadNavigation(e, FocusNavigationDirection.Down);
					return;
			}
		}

		// Framework layer: Handles compact modes (CompactInline, CompactOverlay).
		// Gamepad navigation between pane and content.
		var displayMode = DisplayMode;
		if (displayMode == SplitViewDisplayMode.CompactInline ||
			displayMode == SplitViewDisplayMode.CompactOverlay)
		{
			HandleCompactModeGamepadNavigation(e);
		}
	}

	private void HandleLightDismissGamepadNavigation(KeyRoutedEventArgs e, FocusNavigationDirection direction)
	{
		e.Handled = true;

		if (m_corePaneRoot is null || XamlRoot is null)
		{
			return;
		}

		var currentFocusedElement = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
		var options = new FindNextElementOptions
		{
			SearchRoot = this
		};
		var candidate = FocusManager.FindNextElement(direction, options);

		if (currentFocusedElement is not null && candidate is UIElement candidateElement)
		{
			var isFocusedElementInPane = IsAncestorOf(m_corePaneRoot, currentFocusedElement);
			if (isFocusedElementInPane)
			{
				bool isCandidateInPane = IsAncestorOf(m_corePaneRoot, candidate);

				// Only set focus to a candidate if it lives in the pane sub-tree;
				// swallow the input otherwise.
				if (isCandidateInPane)
				{
					e.Handled = candidateElement.Focus(FocusState.Keyboard);
				}
			}
		}
	}

	private void OnLightDismissLayerPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (CanLightDismiss())
		{
			TryCloseLightDismissiblePane();
			e.Handled = true;
		}
	}

	internal DependencyObject? ProcessTabStop(
		bool isForward,
		DependencyObject? focusedElement,
		DependencyObject? candidateTabStopElement)
	{
		// Panes that can be light dismissed hold onto focus until they're closed.
		if (!CanLightDismiss() || m_corePaneRoot is null)
		{
			return null;
		}

		if (focusedElement is null)
		{
			return null;
		}

		// If the element losing focus is in our pane, then we evaluate the candidate element to
		// determine whether we need to override it.
		var isFocusedElementInPane = IsAncestorOf(m_corePaneRoot, focusedElement);
		if (!isFocusedElementInPane)
		{
			return null;
		}

		bool doOverrideCandidate;
		if (candidateTabStopElement is not null)
		{
			// If the candidate element isn't in the pane, we need to override it to keep focus within the pane.
			doOverrideCandidate = !IsAncestorOf(m_corePaneRoot, candidateTabStopElement);
		}
		else
		{
			// If there's no candidate, then we need to make sure focus stays within the pane.
			doOverrideCandidate = true;
		}

		if (!doOverrideCandidate)
		{
			return null;
		}

		return isForward
			? FocusManager.FindFirstFocusableElement(m_corePaneRoot)
			: FocusManager.FindLastFocusableElement(m_corePaneRoot);
	}

	internal DependencyObject? GetFirstFocusableElementFromPane()
	{
		if (m_corePaneRoot is null)
		{
			return null;
		}

		return FocusManager.FindFirstFocusableElement(m_corePaneRoot);
	}

	internal DependencyObject? GetLastFocusableElementFromPane()
	{
		if (m_corePaneRoot is null)
		{
			return null;
		}

		return FocusManager.FindLastFocusableElement(m_corePaneRoot);
	}

	private static bool IsAncestorOf(DependencyObject ancestor, DependencyObject descendant)
	{
		if (ancestor is UIElement ancestorElement && descendant is UIElement descendantElement)
		{
			var parent = descendantElement;
			while (parent is not null)
			{
				if (ReferenceEquals(parent, ancestorElement))
				{
					return true;
				}
				parent = VisualTreeHelper.GetParent(parent) as UIElement;
			}
		}
		return false;
	}

	// Partial method to be implemented in the framework layer (SplitView.partial.mux.cs)
	partial void OnApplyTemplatePartial();
}
