// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ApplicationBarService.h, ApplicationBarService.cpp, ApplicationBarService_Partial.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Controls;

// Indicates which AppBar is priority for focus
internal enum AppBarTabPriority
{
	Top,
	Bottom
}

/// <summary>
/// Service class that provides the system implementation for managing
/// many app bars and the edgy event. One instance per XamlRoot.
/// ApplicationBarService is responsible for hosting the top and bottom appbars
/// and toggling all appbars in response to system events (edgy) as well as
/// making sure appbars are closed in response to lightdismiss.
/// </summary>
internal class ApplicationBarService
{
	private const long OpeningDurationMs = 467;
	private const long ClosingDurationMs = 167;

	private WeakReference<XamlRoot>? _weakXamlRoot;

	// the host of the dismiss layer and the wrappers
	private Popup? m_tpPopupHost;
	private Grid? m_tpDismissLayer;

	// wrappers that will host the appbars
	private Border? m_tpTopBarHost;
	private Border? m_tpBottomBarHost;

	// transparent brush, used to toggle on and off the hittesting behavior of the grid
	private SolidColorBrush? m_tpTransparentBrush;

	// list of registered appbars (weak references)
	private readonly List<WeakReference<AppBar>> m_applicationBars = new();

	// map of unloading appbars to track unload transitions
	private readonly Dictionary<FrameworkElement, Action> m_unloadingAppbars = new();

	// cache the previous bounds
	private Rect m_bounds;

	// Holds a weak reference to the most recent element focused on the main content before any appbar got opened.
	private ManagedWeakReference? m_previousFocusedElementWeakRef;

	// Focus state when returning focus after dismiss
	private FocusState m_focusReturnState = FocusState.Unfocused;

	// The only case where top app bar should not get focus as soon as it opens is the case where
	// bottom app bar will get opened at the same time.
	private bool m_shouldTopGetFocus = true;

	// Light dismiss layer and the popup host preserve their current state when this flag is set.
	private bool m_suspendLightDismissLayerState;

	// Focus appbars after all appbars which are loading have loaded
	private int m_appBarsLoading;

	// Overlay
	private bool m_isOverlayVisible;
	private Storyboard? m_overlayOpeningStoryboard;
	private Storyboard? m_overlayClosingStoryboard;

	// Window activated handler
	private bool m_hasWindowActivatedHandler;

	internal ApplicationBarService()
	{
		Initialize();
	}

	internal void SetXamlRoot(XamlRoot xamlRoot)
	{
		_weakXamlRoot = new WeakReference<XamlRoot>(xamlRoot);
	}

	private void Initialize()
	{
		var popup = new Popup();
		var grid = new Grid();
		var topHost = new Border();
		var bottomHost = new Border();
		var transparentBrush = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

		m_tpPopupHost = popup;
		m_tpDismissLayer = grid;
		m_tpTopBarHost = topHost;
		m_tpBottomBarHost = bottomHost;
		m_tpTransparentBrush = transparentBrush;

		// our popup will host the grid
		m_tpPopupHost.Child = m_tpDismissLayer;

		// initialize dismiss layer size
		var bounds = TryGetBoundsFromXamlRoot();
		m_tpDismissLayer.Width = bounds.Width;
		m_tpDismissLayer.Height = bounds.Height;

		// hookup pointer presses to the dismiss layer
		m_tpDismissLayer.PointerPressed += OnDismissLayerPressed;
		m_tpDismissLayer.PointerReleased += OnDismissLayerPointerReleased;
		m_tpDismissLayer.RightTapped += OnDismissLayerRightTapped;

		// setup alignments
		m_tpTopBarHost.VerticalAlignment = VerticalAlignment.Top;
		m_tpTopBarHost.HorizontalAlignment = HorizontalAlignment.Stretch;
		m_tpBottomBarHost.VerticalAlignment = VerticalAlignment.Bottom;
		m_tpBottomBarHost.HorizontalAlignment = HorizontalAlignment.Stretch;

		// add hosts to dismiss layer
		m_tpDismissLayer.Children.Add(m_tpTopBarHost);
		m_tpDismissLayer.Children.Add(m_tpBottomBarHost);

		// start without shield on
		UpdateDismissLayer();

		// start with popup closed
		EvaluatePopupState();
	}

	private Rect TryGetBoundsFromXamlRoot()
	{
		if (_weakXamlRoot is not null && _weakXamlRoot.TryGetTarget(out var xamlRoot))
		{
			return new Rect(0, 0, xamlRoot.Size.Width, xamlRoot.Size.Height);
		}

		return new Rect(0, 0, 0, 0);
	}

	// Registers an applicationbar so it can be toggled globally
	internal void RegisterApplicationBar(AppBar appBar, AppBarMode mode)
	{
		m_applicationBars.Add(new WeakReference<AppBar>(appBar));

		AddApplicationBarToVisualTree(appBar, mode);

		// Clear transitions on initial registration
		if (mode == AppBarMode.Top)
		{
			m_tpTopBarHost?.ClearValue(Border.ChildTransitionsProperty);
		}
		else if (mode == AppBarMode.Bottom)
		{
			m_tpBottomBarHost?.ClearValue(Border.ChildTransitionsProperty);
			// TODO Uno: UIElement.TransitionsProperty (EdgeUIThemeTransition) not available
			// m_tpBottomBarHost?.ClearValue(UIElement.TransitionsProperty);
		}

		if (appBar.IsOpen)
		{
			OpenApplicationBar(appBar, mode);
		}

		// Set up Window.Activated handler if not already done
		if (!m_hasWindowActivatedHandler)
		{
			var window = DXamlCore.Current.GetAssociatedWindow(appBar);
			if (window is not null)
			{
				window.Activated += OnWindowActivated;
				m_hasWindowActivatedHandler = true;
			}
		}
	}

	internal void UnregisterApplicationBar(AppBar appBar)
	{
		// Close and remove from visual tree
		if (m_tpTopBarHost?.Child == appBar)
		{
			CloseApplicationBar(appBar, AppBarMode.Top);
			RemoveApplicationBarFromVisualTree(appBar, AppBarMode.Top);
		}

		if (m_tpBottomBarHost?.Child == appBar)
		{
			CloseApplicationBar(appBar, AppBarMode.Bottom);
			RemoveApplicationBarFromVisualTree(appBar, AppBarMode.Bottom);
		}

		// Remove from list
		for (int i = m_applicationBars.Count - 1; i >= 0; i--)
		{
			if (m_applicationBars[i].TryGetTarget(out var registeredBar) && registeredBar == appBar)
			{
				m_applicationBars.RemoveAt(i);
				break;
			}
		}

		// Stop listening for Window.Activated if no more bars
		if (m_applicationBars.Count == 0 && m_hasWindowActivatedHandler)
		{
			var window = DXamlCore.Current.GetAssociatedWindow(appBar);
			if (window is not null)
			{
				window.Activated -= OnWindowActivated;
				m_hasWindowActivatedHandler = false;
			}
		}
	}

	internal bool IsAppBarRegistered(AppBar appBar)
	{
		foreach (var weakRef in m_applicationBars)
		{
			if (weakRef.TryGetTarget(out var registeredBar) && registeredBar == appBar)
			{
				return true;
			}
		}

		return false;
	}

	internal void GetTopAndBottomAppBars(out AppBar? topAppBar, out AppBar? bottomAppBar)
	{
		GetTopAndBottomAppBars(openAppBarsOnly: false, out topAppBar, out bottomAppBar, out _);
	}

	internal void GetTopAndBottomOpenAppBars(out AppBar? topAppBar, out AppBar? bottomAppBar, out bool isAnyLightDismiss)
	{
		GetTopAndBottomAppBars(openAppBarsOnly: true, out topAppBar, out bottomAppBar, out isAnyLightDismiss);
	}

	private void GetTopAndBottomAppBars(bool openAppBarsOnly, out AppBar? topAppBar, out AppBar? bottomAppBar, out bool isAnyLightDismiss)
	{
		topAppBar = null;
		bottomAppBar = null;
		isAnyLightDismiss = false;

		if (m_tpTopBarHost?.Child is AppBar top)
		{
			if (!openAppBarsOnly || top.IsOpen)
			{
				isAnyLightDismiss = isAnyLightDismiss || !top.IsSticky;
				topAppBar = top;
			}
		}

		if (m_tpBottomBarHost?.Child is AppBar bottom)
		{
			if (!openAppBarsOnly || bottom.IsOpen)
			{
				isAnyLightDismiss = isAnyLightDismiss || !bottom.IsSticky;
				bottomAppBar = bottom;
			}
		}
	}

	// Focus the applicationbar
	internal void FocusApplicationBar(AppBar appBar, FocusState focusState)
	{
		if (m_appBarsLoading > 0)
		{
			m_appBarsLoading--;
		}

		if (m_appBarsLoading == 0)
		{
			GetTopAndBottomOpenAppBars(out var topAppBar, out var bottomAppBar, out var isAnyLightDismiss);

			var priority = m_shouldTopGetFocus ? AppBarTabPriority.Top : AppBarTabPriority.Bottom;
			var newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, priority, startFromEnd: false);

			bool focusUpdated = false;
			if (newTabStop is not null)
			{
				focusUpdated = appBar.SetFocusedElement(newTabStop, focusState, animateIfBringIntoView: false);
			}

			if (!focusUpdated)
			{
				focusUpdated = appBar.Focus(focusState);
			}

			// If focus was still not updated and there is a light dismiss appbar, clear the focus
			if (!focusUpdated && isAnyLightDismiss)
			{
				var focusManager = VisualTree.GetFocusManagerForElement(appBar);
				focusManager?.ClearFocus();
			}
		}
	}

	// Toggles Application Bars
	internal void ToggleApplicationBars()
	{
		int visibleCount = 0;
		int hiddenCount = 0;
		bool valueToSet = true;
		bool bottomAppBarGetsOpened = false;
		bool topAppBarGetsOpened = false;
		var barsToToggle = new List<AppBar>();

		m_shouldTopGetFocus = true;

		foreach (var weakRef in m_applicationBars)
		{
			if (!weakRef.TryGetTarget(out var appBar))
			{
				continue;
			}

			// We don't want to toggle CommandBars
			if (appBar is CommandBar)
			{
				continue;
			}

			if (appBar.IsOpen)
			{
				visibleCount++;
			}
			else
			{
				hiddenCount++;
			}

			if (appBar.Mode == AppBarMode.Bottom && !appBar.IsOpen)
			{
				bottomAppBarGetsOpened = true;
			}
			else if (appBar.Mode == AppBarMode.Top && !appBar.IsOpen)
			{
				topAppBarGetsOpened = true;
			}

			barsToToggle.Add(appBar);
		}

		// Show all unless they are all currently shown
		if (hiddenCount == 0 && visibleCount > 0)
		{
			valueToSet = false;
		}

		if (bottomAppBarGetsOpened && topAppBarGetsOpened)
		{
			MUX_ASSERT(valueToSet);
			m_shouldTopGetFocus = false;
		}

		foreach (var bar in barsToToggle)
		{
			bar.IsOpen = valueToSet;
		}
	}

	private void EvaluatePopupState()
	{
		if (m_tpPopupHost is null || m_tpTopBarHost is null || m_tpBottomBarHost is null)
		{
			return;
		}

		if (!m_suspendLightDismissLayerState)
		{
			var topChild = m_tpTopBarHost.Child;
			var bottomChild = m_tpBottomBarHost.Child;
			bool isOpen = m_tpPopupHost.IsOpen;
			bool hasAppBars = topChild is not null || bottomChild is not null;

			if (m_unloadingAppbars.Count == 0 && !hasAppBars)
			{
				if (isOpen)
				{
					m_tpPopupHost.IsOpen = false;
				}
			}
			else
			{
				if (!isOpen)
				{
					TryGetBounds(out _);
					m_tpPopupHost.IsOpen = true;
				}
			}
		}
	}

	private bool TryGetBounds(out bool boundsChanged)
	{
		boundsChanged = false;

		if (_weakXamlRoot is null || !_weakXamlRoot.TryGetTarget(out var xamlRoot) || m_tpDismissLayer is null || m_tpPopupHost is null)
		{
			return false;
		}

		var bounds = DXamlCore.Current.GetContentBoundsForElement(m_tpPopupHost);

		if (bounds.Width != m_bounds.Width || bounds.Height != m_bounds.Height)
		{
			boundsChanged = true;
			m_tpDismissLayer.Width = bounds.Width;
			m_tpDismissLayer.Height = bounds.Height;
		}

		if (bounds.X != m_bounds.X || bounds.Y != m_bounds.Y)
		{
			boundsChanged = true;
			m_tpDismissLayer.Margin = new Thickness(bounds.X, bounds.Y, 0, 0);
		}

		m_bounds = bounds;
		return true;
	}

	private bool ShouldDismissNonStickyAppBars()
	{
		bool hasBounds = !(m_bounds.X == 0 && m_bounds.Y == 0 && m_bounds.Width == 0 && m_bounds.Height == 0);
		TryGetBounds(out bool boundsChanged);
		return hasBounds && boundsChanged;
	}

	internal void OnBoundsChanged(bool inputPaneChange = false)
	{
		bool shouldDismiss = ShouldDismissNonStickyAppBars();

		if (shouldDismiss && !inputPaneChange)
		{
			CloseAllNonStickyAppBars();
		}
	}

	// Check if the focused element is on the top/bottom appbars, if it is not
	// save current focused element's weak reference
	internal void SaveCurrentFocusedElement(AppBar appBar)
	{
		var focusedElement = appBar.GetFocusedElement();

		if (focusedElement is not null)
		{
			if (m_previousFocusedElementWeakRef is not null)
			{
				WeakReferencePool.ReturnWeakReference(this, m_previousFocusedElementWeakRef);
			}

			m_previousFocusedElementWeakRef = WeakReferencePool.RentWeakReference(this, focusedElement);
		}
	}

	// Try to focus the previously saved element
	internal void FocusSavedElement(AppBar appBar)
	{
		var focusState = m_focusReturnState == FocusState.Unfocused
			? FocusState.Programmatic
			: m_focusReturnState;

		DependencyObject? elementToBeFocused = null;

		if (m_previousFocusedElementWeakRef is not null &&
			m_previousFocusedElementWeakRef.IsAlive &&
			m_previousFocusedElementWeakRef.Target is DependencyObject savedElement)
		{
			elementToBeFocused = savedElement;
		}

		bool focusUpdated = false;
		if (elementToBeFocused is not null)
		{
			focusUpdated = appBar.SetFocusedElement(elementToBeFocused, focusState, animateIfBringIntoView: false);
		}

		if (!focusUpdated)
		{
			var focusManager = VisualTree.GetFocusManagerForElement(appBar);
			focusManager?.ClearFocus();
		}
	}

	internal void OpenApplicationBar(AppBar appBar, AppBarMode mode)
	{
		bool isRegistered = IsAppBarRegistered(appBar);

		ReevaluateIsOverlayVisible();

		if (m_isOverlayVisible)
		{
			PlayOverlayOpeningAnimation();
		}

		if (isRegistered)
		{
			// Opening an AppBar should close any open FlyoutBase.
			CloseOpenFlyouts();
		}
	}

	internal void CloseApplicationBar(AppBar appBar, AppBarMode mode)
	{
		bool isRegistered = IsAppBarRegistered(appBar);

		if (m_isOverlayVisible)
		{
			PlayOverlayClosingAnimation();
		}

		if (isRegistered)
		{
			CloseOpenFlyouts();
		}
	}

	internal void HandleApplicationBarClosedDisplayModeChange(AppBar appBar, AppBarMode mode)
	{
		bool isRegistered = IsAppBarRegistered(appBar);

		if (isRegistered)
		{
			Border? host = null;

			if (mode == AppBarMode.Top)
			{
				host = m_tpTopBarHost;
			}
			else if (mode == AppBarMode.Bottom)
			{
				host = m_tpBottomBarHost;
			}

			if (host is not null)
			{
				bool appBarIsInVisualTree = host.Child is not null && host.Child == appBar;

				if (!appBarIsInVisualTree)
				{
					AddApplicationBarToVisualTree(appBar, mode);
				}

				// Clear transitions
				if (mode == AppBarMode.Top)
				{
					m_tpTopBarHost?.ClearValue(Border.ChildTransitionsProperty);
				}
				else if (mode == AppBarMode.Bottom)
				{
					m_tpBottomBarHost?.ClearValue(Border.ChildTransitionsProperty);
					// TODO Uno: UIElement.TransitionsProperty (EdgeUIThemeTransition) not available
					// m_tpBottomBarHost?.ClearValue(UIElement.TransitionsProperty);
				}
			}
		}
	}

	internal bool CloseAllNonStickyAppBars()
	{
		bool isAnyAppBarClosed = false;

		foreach (var weakRef in m_applicationBars)
		{
			if (weakRef.TryGetTarget(out var appBar))
			{
				if (appBar.IsOpen && !appBar.IsSticky)
				{
					appBar.IsOpen = false;
					isAnyAppBarClosed = true;
				}
			}
		}

		return isAnyAppBarClosed;
	}

	internal void UpdateDismissLayer()
	{
		if (m_tpTopBarHost is null || m_tpBottomBarHost is null || m_tpDismissLayer is null)
		{
			return;
		}

		if (!m_suspendLightDismissLayerState)
		{
			bool activated = false;

			if (m_tpTopBarHost.Child is AppBar topBar)
			{
				if (topBar.IsOpen && !topBar.IsSticky)
				{
					activated = true;
				}
			}

			if (!activated && m_tpBottomBarHost.Child is AppBar bottomBar)
			{
				if (bottomBar.IsOpen && !bottomBar.IsSticky)
				{
					activated = true;
				}
			}

			if (activated)
			{
				m_tpDismissLayer.Background = m_tpTransparentBrush;
			}
			else
			{
				m_tpDismissLayer.ClearValue(Panel.BackgroundProperty);
			}
		}
	}

	internal void SetFocusReturnState(FocusState focusState)
	{
		m_focusReturnState = focusState;
	}

	internal void ResetFocusReturnState()
	{
		m_focusReturnState = FocusState.Unfocused;
	}

	private void AddApplicationBarToVisualTree(AppBar appBar, AppBarMode mode)
	{
		m_appBarsLoading++;

		bool isRegistered = IsAppBarRegistered(appBar);

		if (isRegistered)
		{
			Border? host = null;

			if (mode == AppBarMode.Top)
			{
				host = m_tpTopBarHost;
			}
			else if (mode == AppBarMode.Bottom)
			{
				host = m_tpBottomBarHost;
			}

			if (host is not null)
			{
				TryGetBounds(out _);

				// Set app bar focus state to be assumed when loaded
				appBar.SetOnLoadFocusState(m_focusReturnState);

				// Clear the height, which was locked after closing appbar
				host.ClearValue(FrameworkElement.HeightProperty);

				// Clear properties set by previous AppBar
				ClearAppBarOwnerPropertiesOnHost(host);

				// Set the content
				host.Child = appBar;
				SetAppBarOwnerPropertiesOnHost(host);

				UpdateDismissLayer();
				EvaluatePopupState();
			}
		}
	}

	private void RemoveApplicationBarFromVisualTree(AppBar appBar, AppBarMode mode)
	{
		if (m_appBarsLoading > 0)
		{
			m_appBarsLoading--;
		}

		bool isRegistered = IsAppBarRegistered(appBar);

		if (isRegistered)
		{
			Border? host = null;
			UIElement? currentChild = null;

			if (mode == AppBarMode.Top && m_tpTopBarHost is not null)
			{
				currentChild = m_tpTopBarHost.Child;
				host = m_tpTopBarHost;
			}
			else if (mode == AppBarMode.Bottom && m_tpBottomBarHost is not null)
			{
				currentChild = m_tpBottomBarHost.Child;
				host = m_tpBottomBarHost;
			}

			if (currentChild is not null && host is not null)
			{
				if (currentChild is FrameworkElement appBarFE)
				{
					// Listen for unloaded to track unloading transition
					void onUnloaded(object sender, RoutedEventArgs e)
					{
						appBarFE.Unloaded -= onUnloaded;
						m_unloadingAppbars.Remove(appBarFE);
						EvaluatePopupState();
					}

					appBarFE.Unloaded += onUnloaded;
					m_unloadingAppbars[appBarFE] = () => appBarFE.Unloaded -= onUnloaded;

					// Lock host height so unload transition is visible
					host.Height = host.ActualHeight;
				}

				host.Child = null;
			}

			UpdateDismissLayer();
			EvaluatePopupState();
		}
	}

	// Dismiss layer handlers
	private void OnDismissLayerPressed(object sender, PointerRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(sender as UIElement);
		if (pointerPoint.Properties.IsLeftButtonPressed)
		{
			m_suspendLightDismissLayerState = true;
			SetFocusReturnState(FocusState.Pointer);
			CloseAllNonStickyAppBars();
			ResetFocusReturnState();
			args.Handled = true;
		}
	}

	private void OnDismissLayerPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		if (!args.Handled && m_suspendLightDismissLayerState)
		{
			m_suspendLightDismissLayerState = false;
			UpdateDismissLayer();
			EvaluatePopupState();
			args.Handled = true;
		}
	}

	private void OnDismissLayerRightTapped(object sender, RightTappedRoutedEventArgs args)
	{
		if (args.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
		{
			return;
		}

		if (!args.Handled)
		{
			SetFocusReturnState(FocusState.Pointer);
			ToggleApplicationBars();
			ResetFocusReturnState();
			args.Handled = true;
		}
	}

	private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
	{
		if (args.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
		{
			SetFocusReturnState(FocusState.Programmatic);
			CloseAllNonStickyAppBars();
			ResetFocusReturnState();
		}
	}

	// Propagate owner's FlowDirection and Language to host
	private void SetAppBarOwnerPropertiesOnHost(Border host)
	{
		if (host.Child is AppBar appBar)
		{
			var owner = appBar.GetOwner();
			if (owner is not null)
			{
				host.FlowDirection = owner.FlowDirection;
				host.Language = owner.Language;
			}
		}
	}

	private void ClearAppBarOwnerPropertiesOnHost(Border host)
	{
		host.ClearValue(FrameworkElement.FlowDirectionProperty);
		host.ClearValue(FrameworkElement.LanguageProperty);
	}

	// Tab stop processing for cycling focus between AppBars and content
	internal TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject? focusedElement,
		DependencyObject? candidateTabStopElement,
		bool isBackward)
	{
		var result = new TabStopProcessingResult
		{
			NewTabStop = null,
			IsOverriden = false,
		};

		GetTopAndBottomAppBars(openAppBarsOnly: false, out var topAppBar, out var bottomAppBar, out var isAnyAppBarLightDismiss);

		bool isTopAppBarOpen = topAppBar?.IsOpen == true;
		bool isBottomAppBarOpen = bottomAppBar?.IsOpen == true;

		bool isTopAppBarVisibleWhenClosed = topAppBar is not null &&
			topAppBar.ClosedDisplayMode != AppBarClosedDisplayMode.Hidden;
		bool isBottomAppBarVisibleWhenClosed = bottomAppBar is not null &&
			bottomAppBar.ClosedDisplayMode != AppBarClosedDisplayMode.Hidden;
		bool isAnyAppBarVisibleWhenClosed = isTopAppBarVisibleWhenClosed || isBottomAppBarVisibleWhenClosed;

		// If no AppBar exists that is currently visible, don't suggest a focus element
		if ((topAppBar is null || (!isTopAppBarOpen && !isTopAppBarVisibleWhenClosed)) &&
			(bottomAppBar is null || (!isBottomAppBarOpen && !isBottomAppBarVisibleWhenClosed)))
		{
			return result;
		}

		DependencyObject? newTabStop = null;

		if (focusedElement is null)
		{
			newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Bottom, isBackward);
		}
		else
		{
			bool isInTopAppBar = topAppBar?.ContainsElement(focusedElement) == true;
			bool isInBottomAppBar = !isInTopAppBar && bottomAppBar?.ContainsElement(focusedElement) == true;

			var focusManager = VisualTree.GetFocusManagerForElement(focusedElement);

			if (!isBackward)
			{
				if (isInTopAppBar)
				{
					if (GetShouldExitAppBar(topAppBar!, focusedElement, isBackward))
					{
						if (isAnyAppBarLightDismiss && !isAnyAppBarVisibleWhenClosed)
						{
							newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Bottom, isBackward);
						}
						else
						{
							newTabStop = focusManager?.GetFirstFocusableElementFromRoot(isBackward);
						}
					}
				}
				else if (isInBottomAppBar)
				{
					if (GetShouldExitAppBar(bottomAppBar!, focusedElement, isBackward))
					{
						newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, null, AppBarTabPriority.Top, isBackward);
						if (newTabStop is null)
						{
							if (isAnyAppBarLightDismiss && !isAnyAppBarVisibleWhenClosed)
							{
								newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Top, isBackward);
							}
							else
							{
								newTabStop = focusManager?.GetFirstFocusableElementFromRoot(isBackward);
							}
						}
					}
				}
				else
				{
					if (GetShouldEnterAppBar(focusedElement, isBackward))
					{
						newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Bottom, isBackward);
					}
				}
			}
			else // isBackward
			{
				if (isInTopAppBar)
				{
					if (GetShouldExitAppBar(topAppBar!, focusedElement, isBackward))
					{
						newTabStop = GetFirstFocusableElementFromAppBars(null, bottomAppBar, AppBarTabPriority.Bottom, isBackward);
						if (newTabStop is null)
						{
							if (isAnyAppBarLightDismiss && !isAnyAppBarVisibleWhenClosed)
							{
								newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Bottom, isBackward);
							}
							else
							{
								newTabStop = focusManager?.GetFirstFocusableElementFromRoot(isBackward);
							}
						}
					}
				}
				else if (isInBottomAppBar)
				{
					if (GetShouldExitAppBar(bottomAppBar!, focusedElement, isBackward))
					{
						if (isAnyAppBarLightDismiss && !isAnyAppBarVisibleWhenClosed)
						{
							newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Top, isBackward);
						}
						else
						{
							newTabStop = focusManager?.GetFirstFocusableElementFromRoot(isBackward);
						}
					}
				}
				else
				{
					if (GetShouldEnterAppBar(focusedElement, isBackward))
					{
						newTabStop = GetFirstFocusableElementFromAppBars(topAppBar, bottomAppBar, AppBarTabPriority.Top, isBackward);
					}
				}
			}
		}

		if (newTabStop is not null)
		{
			result.NewTabStop = newTabStop;
			result.IsOverriden = true;
		}

		return result;
	}

	// Gets focusable element from appbars depending on priority and direction
	internal DependencyObject? GetFirstFocusableElementFromAppBars(
		AppBar? topAppBar,
		AppBar? bottomAppBar,
		AppBarTabPriority tabPriority,
		bool startFromEnd)
	{
		DependencyObject? newTabStop = null;

		if (tabPriority == AppBarTabPriority.Bottom)
		{
			if (bottomAppBar is not null)
			{
				newTabStop = startFromEnd
					? FocusManager.FindLastFocusableElement(bottomAppBar)
					: FocusManager.FindFirstFocusableElement(bottomAppBar);
			}

			if (newTabStop is null && topAppBar is not null)
			{
				newTabStop = startFromEnd
					? FocusManager.FindLastFocusableElement(topAppBar)
					: FocusManager.FindFirstFocusableElement(topAppBar);
			}
		}
		else // AppBarTabPriority.Top
		{
			if (topAppBar is not null)
			{
				newTabStop = startFromEnd
					? FocusManager.FindLastFocusableElement(topAppBar)
					: FocusManager.FindFirstFocusableElement(topAppBar);
			}

			if (newTabStop is null && bottomAppBar is not null)
			{
				newTabStop = startFromEnd
					? FocusManager.FindLastFocusableElement(bottomAppBar)
					: FocusManager.FindFirstFocusableElement(bottomAppBar);
			}
		}

		return newTabStop;
	}

	// Decides if focus should enter an appbar
	private bool GetShouldEnterAppBar(DependencyObject focusedElement, bool shiftPressed)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(focusedElement);
		if (focusManager is null)
		{
			return false;
		}

		var focusableElementFromRoot = focusManager.GetFirstFocusableElementFromRoot(!shiftPressed);
		return focusableElementFromRoot == focusedElement;
	}

	// Decides if focus should leave the appbar
	private bool GetShouldExitAppBar(AppBar appBar, DependencyObject focusedElement, bool shiftPressed)
	{
		DependencyObject? focusableElement;

		if (shiftPressed)
		{
			focusableElement = FocusManager.FindFirstFocusableElement(appBar);
		}
		else
		{
			focusableElement = FocusManager.FindLastFocusableElement(appBar);
		}

		return focusedElement == focusableElement;
	}

	internal void GetAppBarStatus(
		out bool isTopOpen, out bool isTopSticky, out double topWidth, out double topHeight,
		out bool isBottomOpen, out bool isBottomSticky, out double bottomWidth, out double bottomHeight)
	{
		isTopOpen = false;
		isTopSticky = false;
		topWidth = 0;
		topHeight = 0;
		isBottomOpen = false;
		isBottomSticky = false;
		bottomWidth = 0;
		bottomHeight = 0;

		if (m_tpTopBarHost?.Child is AppBar topAppBar)
		{
			isTopOpen = topAppBar.IsOpen;
			isTopSticky = topAppBar.IsSticky;
			topWidth = topAppBar.ActualWidth;
			topHeight = topAppBar.ActualHeight;
		}

		if (m_tpBottomBarHost?.Child is AppBar bottomAppBar)
		{
			isBottomOpen = bottomAppBar.IsOpen;
			isBottomSticky = bottomAppBar.IsSticky;
			bottomWidth = bottomAppBar.ActualWidth;
			bottomHeight = bottomAppBar.ActualHeight;
		}
	}

	// Overlay methods
	private void ReevaluateIsOverlayVisible()
	{
		GetTopAndBottomOpenAppBars(out var topAppBar, out var bottomAppBar, out _);

		bool isOverlayVisible = false;

		if (topAppBar is not null)
		{
			isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(topAppBar);
		}

		if (!isOverlayVisible && bottomAppBar is not null)
		{
			isOverlayVisible = LightDismissOverlayHelper.ResolveIsOverlayVisibleForControl(bottomAppBar);
		}

		if (isOverlayVisible != m_isOverlayVisible)
		{
			m_isOverlayVisible = isOverlayVisible;

			if (m_isOverlayVisible)
			{
				if (m_overlayOpeningStoryboard is null || m_overlayClosingStoryboard is null)
				{
					CreateOverlayAnimations();
				}

				UpdateTargetForOverlayAnimations();
			}
			else
			{
				// Stop overlay mode
				m_overlayOpeningStoryboard?.Stop();
				m_overlayClosingStoryboard?.Stop();
			}
		}
	}

	private void CreateOverlayAnimations()
	{
		if (m_tpDismissLayer is null)
		{
			return;
		}

		// Create opening animation (0 -> 1 opacity)
		m_overlayOpeningStoryboard = CreateOpacityAnimation(
			OpeningDurationMs,
			startValue: 0.0,
			endValue: 1.0,
			controlPoint1: new Point(0.1, 0.9),
			controlPoint2: new Point(0.2, 1.0),
			target: m_tpDismissLayer);

		// Create closing animation (1 -> 0 opacity)
		m_overlayClosingStoryboard = CreateOpacityAnimation(
			ClosingDurationMs,
			startValue: 1.0,
			endValue: 0.0,
			controlPoint1: new Point(0.2, 0.0),
			controlPoint2: new Point(0.0, 1.0),
			target: m_tpDismissLayer);

		if (m_overlayClosingStoryboard is not null)
		{
			m_overlayClosingStoryboard.Completed += (s, e) =>
			{
				ReevaluateIsOverlayVisible();
			};
		}
	}

	private static Storyboard CreateOpacityAnimation(
		long durationMs,
		double startValue,
		double endValue,
		Point controlPoint1,
		Point controlPoint2,
		UIElement target)
	{
		var doubleAnimation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTargetProperty(doubleAnimation, "Opacity");

		var firstKeyFrame = new DiscreteDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = startValue,
		};
		doubleAnimation.KeyFrames.Add(firstKeyFrame);

		var keySpline = new KeySpline
		{
			ControlPoint1 = controlPoint1,
			ControlPoint2 = controlPoint2,
		};

		var secondKeyFrame = new SplineDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)),
			Value = endValue,
			KeySpline = keySpline,
		};
		doubleAnimation.KeyFrames.Add(secondKeyFrame);

		var storyboard = new Storyboard();
		Storyboard.SetTarget(storyboard, target);
		storyboard.Children.Add(doubleAnimation);

		return storyboard;
	}

	private void UpdateTargetForOverlayAnimations()
	{
		MUX_ASSERT(m_isOverlayVisible);

		// In WinUI, this targets the popup's overlay element.
		// In Uno, we target the dismiss layer directly since Popup.OverlayElement is not available.
		var targetElement = m_tpDismissLayer;

		if (targetElement is not null)
		{
			if (m_overlayOpeningStoryboard is not null)
			{
				m_overlayOpeningStoryboard.Stop();
				Storyboard.SetTarget(m_overlayOpeningStoryboard, targetElement);
			}

			if (m_overlayClosingStoryboard is not null)
			{
				m_overlayClosingStoryboard.Stop();
				Storyboard.SetTarget(m_overlayClosingStoryboard, targetElement);
			}
		}
	}

	private void PlayOverlayOpeningAnimation()
	{
		MUX_ASSERT(m_isOverlayVisible);

		m_overlayClosingStoryboard?.Stop();
		m_overlayOpeningStoryboard?.Begin();
	}

	private void PlayOverlayClosingAnimation()
	{
		MUX_ASSERT(m_isOverlayVisible);

		m_overlayOpeningStoryboard?.Stop();
		m_overlayClosingStoryboard?.Begin();
	}

	private static void CloseOpenFlyouts()
	{
		// Close any open flyouts when AppBar opens/closes
		var openFlyouts = FlyoutBase.OpenFlyouts;
		for (int i = openFlyouts.Count - 1; i >= 0; i--)
		{
			openFlyouts[i].Hide();
		}
	}
}
