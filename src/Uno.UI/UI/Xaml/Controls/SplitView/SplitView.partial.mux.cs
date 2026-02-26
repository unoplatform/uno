// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SplitView_Partial.cpp, commit 69097129a

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.Disposables;
using Windows.Foundation;
using VirtualKey = global::Windows.System.VirtualKey;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitView
{
	// Index table as follows: [DisplayMode][Placement][IsOpen]
	private static readonly string[][][] s_visualStateTable =
	[
		// Overlay
		[
			["Closed", "OpenOverlayLeft"],
			["Closed", "OpenOverlayRight"]
		],

		// Inline
		[
			["Closed", "OpenInlineLeft"],
			["Closed", "OpenInlineRight"]
		],

		// CompactOverlay
		[
			["ClosedCompactLeft", "OpenCompactOverlayLeft"],
			["ClosedCompactRight", "OpenCompactOverlayRight"]
		],

		// CompactInline
		[
			["ClosedCompactLeft", "OpenInlineLeft"],
			["ClosedCompactRight", "OpenInlineRight"]
		]
	];

	private void PrepareState()
	{
		Loaded += OnSplitViewLoaded;
		m_loadedRevoker.Disposable = Disposable.Create(() => Loaded -= OnSplitViewLoaded);

		Unloaded += OnSplitViewUnloaded;
		m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= OnSplitViewUnloaded);

		SizeChanged += OnSplitViewSizeChanged;
		m_sizeChangedRevoker.Disposable = Disposable.Create(() => SizeChanged -= OnSplitViewSizeChanged);
	}

	partial void OnApplyTemplatePartial()
	{
		// Prepare lifecycle event handlers if not already done.
		if (m_loadedRevoker.Disposable is null)
		{
			PrepareState();
		}

		m_tpPaneRoot = null;
		m_tpContentRoot = null;

		if (m_displayModeStateChangedRevoker.Disposable is not null)
		{
			m_displayModeStateChangedRevoker.Disposable = null;
			m_tpDisplayModeStates = null;
		}

		// Get PaneRoot and set automation peer factory
		if (GetTemplateChild(c_paneRoot) is FrameworkElement paneRoot)
		{
			m_tpPaneRoot = paneRoot;
		}

		// Get LightDismissLayer and set automation peer factory
		if (GetTemplateChild(c_lightDismissLayer) is FrameworkElement lightDismissLayer)
		{
			// TODO Uno: Set automation peer factory index for SplitViewLightDismissAutomationPeer
		}

		// Get ContentRoot
		if (GetTemplateChild(c_contentRoot) is FrameworkElement contentRoot)
		{
			m_tpContentRoot = contentRoot;
		}
	}

	private void HandleCompactModeGamepadNavigation(KeyRoutedEventArgs e)
	{
		if (XamlRoot is null)
		{
			return;
		}

		var originalKey = e.OriginalKey;
		if (!IsGamepadNavigationDirection(originalKey))
		{
			return;
		}

		var direction = GetNavigationDirection(originalKey);
		var options = new FindNextElementOptions
		{
			SearchRoot = this
		};
		var candidate = FocusManager.FindNextElement(direction, options);

		if (candidate is not null && m_tpPaneRoot is not null && m_tpContentRoot is not null)
		{
			bool shouldHandle = false;

			// If pane has focus and candidate is from content (or)
			// content has focus and candidate is from the pane
			// Then handle the key and move focus.
			if (IsAncestorOf(m_tpPaneRoot, FocusManager.GetFocusedElement(XamlRoot) as DependencyObject ?? this))
			{
				shouldHandle = IsAncestorOf(m_tpContentRoot, candidate);
			}

			if (!shouldHandle)
			{
				if (IsAncestorOf(m_tpContentRoot, FocusManager.GetFocusedElement(XamlRoot) as DependencyObject ?? this))
				{
					shouldHandle = IsAncestorOf(m_tpPaneRoot, candidate);
				}
			}

			if (shouldHandle && candidate is UIElement candidateElement)
			{
				var focusUpdated = candidateElement.Focus(FocusState.Keyboard);
				e.Handled = focusUpdated;
			}
		}
	}

	private void OnSplitViewLoaded(object sender, RoutedEventArgs e)
	{
		// Creates the Outer Dismiss Layer when we're setting our initial state
		if (IsPaneOpen && CanLightDismiss())
		{
			SetupOuterDismissLayer();
		}

		if (XamlRoot is { } xamlRoot && m_xamlRootChangedRevoker.Disposable is null)
		{
			xamlRoot.Changed += OnXamlRootChanged;
			m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot.Changed -= OnXamlRootChanged);
		}
	}

	private void OnSplitViewUnloaded(object sender, RoutedEventArgs e)
	{
		TeardownOuterDismissLayer();
	}

	private void OnSplitViewSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var prevSize = e.PreviousSize;

		// Light dismiss only if we're not setting our initial size.
		if ((prevSize.Width != 0 || prevSize.Height != 0) && CanLightDismiss())
		{
			TryCloseLightDismissiblePane();
		}
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
	{
		if (CanLightDismiss())
		{
			TryCloseLightDismissiblePane();
		}
	}

	private void OnOuterDismissElementPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (CanLightDismiss())
		{
			TryCloseLightDismissiblePane();
		}
	}

	private void OnDisplayModeStateChanged(object sender, VisualStateChangedEventArgs e)
	{
		// Only respond to visual state changes between opened and closed states.
		// We could get state changes between opened states if display mode is changed
		// (such as going from Compact to Overlay) while the pane is open.
		if (m_isPaneOpeningOrClosing)
		{
			m_isPaneOpeningOrClosing = false;
			OnPaneOpenedOrClosed(IsPaneOpen);
		}
	}

	internal override void UpdateVisualState(bool useTransitions = true)
	{
		// DisplayModeStates
		{
			var displayMode = DisplayMode;
			var placement = PanePlacement;
			var isPaneOpen = IsPaneOpen;

			// Look up the visual state based on display mode, placement and ispaneopen state.
			var visualStateName = s_visualStateTable[
				(int)displayMode]
				[(int)placement]
				[isPaneOpen ? 1 : 0];

#if __APPLE_UIKIT__
			PatchInvalidFinalState(visualStateName);
#endif

			VisualStateManager.GoToState(this, visualStateName, useTransitions);
		}

		// OverlayVisibilityStates
		{
			var isOverlayVisible = ResolveIsOverlayVisible();
			VisualStateManager.GoToState(this, isOverlayVisible ? "OverlayVisible" : "OverlayNotVisible", useTransitions);
		}
	}

	private bool ResolveIsOverlayVisible()
	{
		// The overlay is visible when light-dismissible and overlay mode is not Off.
		if (!CanLightDismiss())
		{
			return false;
		}

		return LightDismissOverlayMode switch
		{
			LightDismissOverlayMode.Auto => true,
			LightDismissOverlayMode.On => true,
			LightDismissOverlayMode.Off => false,
			_ => false,
		};
	}

	private void OnIsPaneOpenChanged(bool isOpen)
	{
		RegisterForDisplayModeStatesChangedEvent();

		m_isPaneOpeningOrClosing = true;

		UpdateVisualState(true);

		if (isOpen)
		{
			// Raise the PaneOpening event.
			PaneOpening?.Invoke(this, null);

			// Call into the core object to set focus to the pane on opening.
			OnPaneOpening();

			// TODO Uno: Uncomment once PR #20764 (back button integration) is merged.
			// BackButtonIntegration.RegisterListener(this);

			SetupOuterDismissLayer();
		}
		else
		{
			// Raise the PaneClosing event and restore focus.
			OnPaneClosing();

			// TODO Uno: Uncomment once PR #20764 (back button integration) is merged.
			// BackButtonIntegration.UnregisterListener(this);

			TeardownOuterDismissLayer();
		}

		// If the display modes states changing event was not registered, then
		// do the opened/closed work here instead. This could be the case if
		// the SplitView has been re-templated to remove the 'DisplayModeStates'
		// state group.
		if (m_displayModeStateChangedRevoker.Disposable is null)
		{
			OnPaneOpenedOrClosed(isOpen);
		}
	}

	private void OnDisplayModeChanged()
	{
		if (CanLightDismiss())
		{
			SetupOuterDismissLayer();
		}
		else
		{
			TeardownOuterDismissLayer();
		}
	}

	private void SetupOuterDismissLayer()
	{
		// If we're not in a light-dismissible state, then we can bail out of
		// setting up this dismiss layer.
		if (!CanLightDismiss())
		{
			return;
		}

		if (!IsLoaded)
		{
			return;
		}

		// To detect input outside of the SplitView's bounds, we create
		// a popup layer that hosts 4 polygonal elements arranged around
		// the SplitView. The Top and Bottom elements are defined by 6
		// points, while the Left and Right elements are defined by 4 points.
		//
		// Pointer handlers are attached to the elements and dismiss the
		// pane if activated.

		Rect windowBounds;
		if (XamlRoot is { } xamlRoot)
		{
			windowBounds = new Rect(0, 0, xamlRoot.Size.Width, xamlRoot.Size.Height);
		}
		else
		{
			return;
		}

		var actualWidth = ActualWidth;
		var actualHeight = ActualHeight;

		GeneralTransform? transformToVisual;
		try
		{
			transformToVisual = TransformToVisual(null);
		}
		catch
		{
			return;
		}

		// Transform all 4 corners of the SplitView's bounds.
		var topLeftCorner = transformToVisual.TransformPoint(new Point(0, 0));
		var topRightCorner = transformToVisual.TransformPoint(new Point(actualWidth, 0));
		var bottomRightCorner = transformToVisual.TransformPoint(new Point(actualWidth, actualHeight));
		var bottomLeftCorner = transformToVisual.TransformPoint(new Point(0, actualHeight));

		if (FlowDirection == FlowDirection.RightToLeft)
		{
			// Swap left/right corners for RTL
			(topLeftCorner, topRightCorner) = (topRightCorner, topLeftCorner);
			(bottomLeftCorner, bottomRightCorner) = (bottomRightCorner, bottomLeftCorner);
		}

		// Determine which elements we need based on the translated corner points.
		bool needsTop = topLeftCorner.Y > 0 || topRightCorner.Y > 0;
		bool needsBottom = bottomLeftCorner.Y < windowBounds.Height || bottomRightCorner.Y < windowBounds.Height;
		bool needsLeft = topLeftCorner.X > 0 || bottomLeftCorner.X > 0;
		bool needsRight = topRightCorner.X < windowBounds.Width || bottomRightCorner.X < windowBounds.Width;

		// If the SplitView doesn't need any of the 4 elements, then it takes up the entire
		// window, so we can bail early.
		if (!needsTop && !needsBottom && !needsLeft && !needsRight)
		{
			return;
		}

		if (m_dismissHostElement is null)
		{
			m_dismissHostElement = new Grid();

			m_dismissHostElement.PointerPressed += OnOuterDismissElementPointerPressed;
			m_dismissLayerPointerPressedRevoker.Disposable =
				Disposable.Create(() => m_dismissHostElement.PointerPressed -= OnOuterDismissElementPointerPressed);
		}

		if (m_outerDismissLayerPopup is null)
		{
			m_outerDismissLayerPopup = new Popup
			{
				Child = m_dismissHostElement
			};
		}

		m_outerDismissLayerPopup.IsOpen = true;

		if (needsTop)
		{
			if (m_topDismissElement is not null)
			{
				m_topDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_topDismissElement = CreatePolygonalPath(m_dismissHostElement, 6);
			}

			UpdatePolygonalPath(m_topDismissElement, new[]
			{
				new Point(0, 0),
				new Point(windowBounds.Width, 0),
				new Point(windowBounds.Width, topRightCorner.Y),
				topRightCorner,
				topLeftCorner,
				new Point(0, topLeftCorner.Y)
			});
		}
		else if (m_topDismissElement is not null)
		{
			m_topDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsBottom)
		{
			if (m_bottomDismissElement is not null)
			{
				m_bottomDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_bottomDismissElement = CreatePolygonalPath(m_dismissHostElement, 6);
			}

			UpdatePolygonalPath(m_bottomDismissElement, new[]
			{
				new Point(0, bottomLeftCorner.Y),
				bottomLeftCorner,
				bottomRightCorner,
				new Point(windowBounds.Width, bottomRightCorner.Y),
				new Point(windowBounds.Width, windowBounds.Height),
				new Point(0, windowBounds.Height)
			});
		}
		else if (m_bottomDismissElement is not null)
		{
			m_bottomDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsLeft)
		{
			if (m_leftDismissElement is not null)
			{
				m_leftDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_leftDismissElement = CreatePolygonalPath(m_dismissHostElement, 4);
			}

			UpdatePolygonalPath(m_leftDismissElement, new[]
			{
				new Point(0, topLeftCorner.Y),
				topLeftCorner,
				bottomLeftCorner,
				new Point(0, bottomLeftCorner.Y)
			});
		}
		else if (m_leftDismissElement is not null)
		{
			m_leftDismissElement.Visibility = Visibility.Collapsed;
		}

		if (needsRight)
		{
			if (m_rightDismissElement is not null)
			{
				m_rightDismissElement.Visibility = Visibility.Visible;
			}
			else
			{
				m_rightDismissElement = CreatePolygonalPath(m_dismissHostElement, 4);
			}

			UpdatePolygonalPath(m_rightDismissElement, new[]
			{
				topRightCorner,
				new Point(windowBounds.Width, topRightCorner.Y),
				new Point(windowBounds.Width, bottomRightCorner.Y),
				bottomRightCorner
			});
		}
		else if (m_rightDismissElement is not null)
		{
			m_rightDismissElement.Visibility = Visibility.Collapsed;
		}
	}

	private void TeardownOuterDismissLayer()
	{
		if (m_outerDismissLayerPopup is not null)
		{
			m_outerDismissLayerPopup.IsOpen = false;
		}
	}

	private void RegisterForDisplayModeStatesChangedEvent()
	{
		if (m_displayModeStateChangedRevoker.Disposable is not null)
		{
			return;
		}

		// Try to get the DisplayModeStates visual state group from the template.
		var rootElement = TemplatedRoot as FrameworkElement;
		if (rootElement is null && VisualTreeHelper.GetChildrenCount(this) > 0)
		{
			rootElement = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
		}

		if (rootElement is not null)
		{
			foreach (var group in VisualStateManager.GetVisualStateGroups(rootElement))
			{
				if (group.Name == "DisplayModeStates")
				{
					m_tpDisplayModeStates = group;
					group.CurrentStateChanged += OnDisplayModeStateChanged;
					m_displayModeStateChangedRevoker.Disposable =
						Disposable.Create(() => group.CurrentStateChanged -= OnDisplayModeStateChanged);
					break;
				}
			}
		}
	}

	private void OnPaneOpenedOrClosed(bool isPaneOpen)
	{
		if (isPaneOpen)
		{
			// Raise the PaneOpened event.
			PaneOpened?.Invoke(this, null);

			// TODO Uno: ElementSoundPlayer is not fully implemented.
			// ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Show, this);
		}
		else
		{
			// Raise the PaneClosed event.
			OnPaneClosed();

			// TODO Uno: ElementSoundPlayer is not fully implemented.
			// ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Hide, this);
		}
	}

	// TODO Uno: Uncomment once PR #20764 (back button integration) is merged.
	// bool IBackButtonListener.OnBackButtonPressed() => OnBackButtonPressedImpl();

#pragma warning disable IDE0051 // Kept for future back button integration (PR #20764)
	private bool OnBackButtonPressedImpl()
#pragma warning restore IDE0051
	{
		if (CanLightDismiss())
		{
			IsPaneOpen = false;
			return true;
		}

		return false;
	}

	private static Path CreatePolygonalPath(Grid hostElement, int numPoints)
	{
		var path = new Path();
		hostElement.Children.Add(path);

		// Set a transparent brush to make sure it's hit-testable.
		path.Fill = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

		// Create the path geometry into which we'll add our figures.
		var pathGeometry = new PathGeometry();
		path.Data = pathGeometry;

		var figure = new PathFigure { IsClosed = true };
		pathGeometry.Figures.Add(figure);

		// The number of segments we have is equal to the number of points minus 1
		// because this is a closed figure.
		for (int i = 0; i < numPoints - 1; i++)
		{
			figure.Segments.Add(new LineSegment());
		}

		return path;
	}

	private static void UpdatePolygonalPath(Path path, Point[] points)
	{
		if (path.Data is not PathGeometry pathGeometry)
		{
			return;
		}

		if (pathGeometry.Figures.Count == 0)
		{
			return;
		}

		var figure = pathGeometry.Figures[0];

		figure.StartPoint = points[0];

		for (int i = 1; i < points.Length; i++)
		{
			if (figure.Segments[i - 1] is LineSegment segment)
			{
				segment.Point = points[i];
			}
		}
	}

	private static bool IsGamepadNavigationDirection(VirtualKey key)
	{
		return key switch
		{
			VirtualKey.GamepadDPadLeft or
			VirtualKey.GamepadLeftThumbstickLeft or
			VirtualKey.GamepadDPadRight or
			VirtualKey.GamepadLeftThumbstickRight or
			VirtualKey.GamepadDPadUp or
			VirtualKey.GamepadLeftThumbstickUp or
			VirtualKey.GamepadDPadDown or
			VirtualKey.GamepadLeftThumbstickDown => true,
			_ => false,
		};
	}

	private static FocusNavigationDirection GetNavigationDirection(VirtualKey key)
	{
		return key switch
		{
			VirtualKey.GamepadDPadLeft or VirtualKey.GamepadLeftThumbstickLeft => FocusNavigationDirection.Left,
			VirtualKey.GamepadDPadRight or VirtualKey.GamepadLeftThumbstickRight => FocusNavigationDirection.Right,
			VirtualKey.GamepadDPadUp or VirtualKey.GamepadLeftThumbstickUp => FocusNavigationDirection.Up,
			VirtualKey.GamepadDPadDown or VirtualKey.GamepadLeftThumbstickDown => FocusNavigationDirection.Down,
			_ => FocusNavigationDirection.None,
		};
	}
}
