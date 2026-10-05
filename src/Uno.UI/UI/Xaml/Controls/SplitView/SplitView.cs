// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference SplitView.cpp, SplitView_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Content))]
public partial class SplitView : Control
{
	// Index table as follows: [DisplayMode][Placement][IsOpen]
	private static readonly string[,,] s_visualStateTable =
	{
		// Overlay
		{
			{ "Closed", "OpenOverlayLeft" },
			{ "Closed", "OpenOverlayRight" }
		},

		// Inline
		{
			{ "Closed", "OpenInlineLeft" },
			{ "Closed", "OpenInlineRight" }
		},

		// CompactOverlay
		{
			{ "ClosedCompactLeft", "OpenCompactOverlayLeft" },
			{ "ClosedCompactRight", "OpenCompactOverlayRight" }
		},

		// CompactInline
		{
			{ "ClosedCompactLeft", "OpenInlineLeft" },
			{ "ClosedCompactRight", "OpenInlineRight" }
		}
	};

	public event TypedEventHandler<SplitView, object>? PaneClosed;
	public event TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs>? PaneClosing;
	public event TypedEventHandler<SplitView, object>? PaneOpened;
	public event TypedEventHandler<SplitView, object>? PaneOpening;

	private RectangleGeometry? _paneClipRectangle;
	private UIElement? _paneRoot;
	private UIElement? _contentRoot;
	private UIElement? _lightDismissLayer;
	private VisualStateGroup? _displayModeStates;
	private bool _isDisplayModeStateChangedRegistered;

	private WeakReference<DependencyObject>? _previousFocusedElementWeakRef;
	private FocusState _previousFocusState = FocusState.Unfocused;

	private bool _isPaneClosingByLightDismiss;
	private bool _isPaneOpeningOrClosing;
	private double _paneMeasuredLength;

	public SplitView()
	{
		DefaultStyleKey = typeof(SplitView);

		TemplateSettings = new SplitViewTemplateSettings(this);

		SizeChanged += OnSizeChanged;
	}

	#region CompactPaneLength DependencyProperty

	public double CompactPaneLength
	{
		get => (double)GetValue(CompactPaneLengthProperty);
		set => SetValue(CompactPaneLengthProperty, Boxer.Box(value));
	}

	public static DependencyProperty CompactPaneLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(CompactPaneLength),
			typeof(double),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: (double)48,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region Content DependencyProperty

	public UIElement Content
	{
		get => (UIElement)GetValue(ContentProperty);
		set => SetValue(ContentProperty, value);
	}

	public static DependencyProperty ContentProperty { get; } =
		DependencyProperty.Register(
			nameof(Content),
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region Pane DependencyProperty

	public UIElement Pane
	{
		get => (UIElement)GetValue(PaneProperty);
		set => SetValue(PaneProperty, value);
	}

	public static DependencyProperty PaneProperty { get; } =
		DependencyProperty.Register(
			nameof(Pane),
			typeof(UIElement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region DisplayMode DependencyProperty

	public SplitViewDisplayMode DisplayMode
	{
		get => (SplitViewDisplayMode)GetValue(DisplayModeProperty);
		set => SetValue(DisplayModeProperty, value);
	}

	public static DependencyProperty DisplayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(DisplayMode),
			typeof(SplitViewDisplayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: SplitViewDisplayMode.Overlay,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region IsPaneOpen DependencyProperty

	public bool IsPaneOpen
	{
		get => (bool)GetValue(IsPaneOpenProperty);
		set => SetValue(IsPaneOpenProperty, value);
	}

	// The MSDN docs wrongly state that the default value is true, it is actually false.
	public static DependencyProperty IsPaneOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsPaneOpen),
			typeof(bool),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				BoolBoxes.False,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region OpenPaneLength DependencyProperty

	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		set => SetValue(OpenPaneLengthProperty, Boxer.Box(value));
	}

	public static DependencyProperty OpenPaneLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(OpenPaneLength),
			typeof(double),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				defaultValue: (double)320,
				options: FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region PaneBackground DependencyProperty

	public Brush PaneBackground
	{
		get => (Brush)GetValue(PaneBackgroundProperty);
		set => SetValue(PaneBackgroundProperty, value);
	}

	public static DependencyProperty PaneBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PaneBackground),
			typeof(Brush),
			typeof(SplitView),
			new FrameworkPropertyMetadata(SolidColorBrushHelper.Transparent));

	#endregion

	#region PanePlacement DependencyProperty

	public SplitViewPanePlacement PanePlacement
	{
		get => (SplitViewPanePlacement)GetValue(PanePlacementProperty);
		set => SetValue(PanePlacementProperty, value);
	}

	public static DependencyProperty PanePlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(PanePlacement),
			typeof(SplitViewPanePlacement),
			typeof(SplitView),
			new FrameworkPropertyMetadata(
				SplitViewPanePlacement.Left,
				FrameworkPropertyMetadataOptions.AffectsMeasure));

	#endregion

	#region LightDismissOverlayMode DependencyProperty

	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
		set => SetValue(LightDismissOverlayModeProperty, value);
	}

	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(SplitView),
			new FrameworkPropertyMetadata(LightDismissOverlayMode.Auto));

	#endregion

	#region TemplateSettings DependencyProperty

	public SplitViewTemplateSettings TemplateSettings
	{
		get => (SplitViewTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(SplitViewTemplateSettings),
			typeof(SplitView),
			new FrameworkPropertyMetadata(null));

	#endregion

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		// CSplitView::OnPropertyChanged
		if (args.Property == DisplayModeProperty)
		{
			RestoreSavedFocusElement();
			UpdateVisualState(useTransitions: true);
		}
		else if (args.Property == PanePlacementProperty || args.Property == LightDismissOverlayModeProperty)
		{
			UpdateVisualState(useTransitions: true);
		}
		else if (args.Property == OpenPaneLengthProperty || args.Property == CompactPaneLengthProperty)
		{
			UpdateTemplateSettings();

			// Force the bindings in our VisualState animations to refresh by intentionally
			// passing in false for 'useTransitions.'
			UpdateVisualState(useTransitions: false);
		}

		// SplitView::OnPropertyChanged2
		if (args.Property == IsPaneOpenProperty)
		{
			OnIsPaneOpenChanged((bool)args.NewValue);
		}
		else if (args.Property == DisplayModeProperty)
		{
			OnDisplayModeChanged();
		}
	}

	protected override void OnApplyTemplate()
	{
		UnregisterEventHandlers();

		// Clear any hold-overs from the previous template.
		_paneClipRectangle = null;
		_contentRoot = null;
		_paneRoot = null;
		_lightDismissLayer = null;

		if (_displayModeStates is not null)
		{
			_displayModeStates.CurrentStateChanged -= OnDisplayModeStateChanged;
			_displayModeStates = null;
		}
		_isDisplayModeStateChangedRegistered = false;

		base.OnApplyTemplate();

		_paneClipRectangle = GetTemplateChild("PaneClipRectangle") as RectangleGeometry;
		_contentRoot = GetTemplateChild("ContentRoot") as UIElement;
		_paneRoot = GetTemplateChild("PaneRoot") as UIElement;
		_lightDismissLayer = GetTemplateChild("LightDismissLayer") as UIElement;

		// TODO Uno: SplitViewPaneAutomationPeer and SplitViewLightDismissAutomationPeer are not assigned to the
		// PaneRoot and LightDismissLayer parts, and drag-and-drop pass-through is not set on the light dismiss layer.

		RegisterEventHandlers();
		UpdateTemplateSettings();
		UpdateVisualState(useTransitions: true);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		// Measure the pane content so that we can use the desired size in cases
		// where open pane length is set to Auto.
		if (Pane is { } paneElement)
		{
			paneElement.Measure(availableSize);

			_paneMeasuredLength = paneElement.DesiredSize.Width;
		}

		var desiredSize = base.MeasureOverride(availableSize);

		UpdateTemplateSettings();

		return desiredSize;
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var newFinalSize = base.ArrangeOverride(finalSize);

		if (_paneClipRectangle is not null)
		{
			_paneClipRectangle.Rect = new Rect(0, 0, (float)GetOpenPaneLength(), newFinalSize.Height);
		}

		return newFinalSize;
	}

	private protected override void ChangeVisualState(bool useTransitions)
	{
		// DisplayModeStates
		{
			var displayMode = DisplayMode;
			var placement = PanePlacement;
			var isPaneOpen = IsPaneOpen;

			// Look up the visual state based on display mode, placement and, ispaneopen state.
			var visualStateName = s_visualStateTable[(int)displayMode, (int)placement, isPaneOpen ? 1 : 0];
			GoToState(useTransitions, visualStateName);
		}

		// OverlayVisibilityStates
		{
			var isOverlayVisible = ResolveIsOverlayVisible();
			GoToState(useTransitions, isOverlayVisible ? "OverlayVisible" : "OverlayNotVisible");
		}
	}

	// LightDismissOverlayHelper::ResolveIsOverlayVisibleForControl
	private bool ResolveIsOverlayVisible()
	{
		var overlayMode = LightDismissOverlayMode;

		return overlayMode == LightDismissOverlayMode.Auto
			? SharedHelpers.IsOnXbox()
			: overlayMode == LightDismissOverlayMode.On;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		var prevSize = args.PreviousSize;

		// Light dismiss only if we're not setting our initial size.
		if ((prevSize.Width != 0 || prevSize.Height != 0) && CanLightDismiss())
		{
			TryCloseLightDismissiblePane();
		}
	}

	private void OnDisplayModeStateChanged(object? sender, VisualStateChangedEventArgs args)
	{
		// Only respond to visual state changes between opened and closed states.
		// We could get state changes between opened states if display mode is changed (such as going from Compact to Overlay)
		// while the pane is open.
		if (_isPaneOpeningOrClosing)
		{
			_isPaneOpeningOrClosing = false;

			OnPaneOpenedOrClosed(IsPaneOpen);
		}
	}

	private void OnIsPaneOpenChanged(bool isOpen)
	{
		RegisterForDisplayModeStatesChangedEvent();

		_isPaneOpeningOrClosing = true;

		UpdateVisualState();

		if (isOpen)
		{
			PaneOpening?.Invoke(this, null!);

			OnPaneOpening();

			// TODO Uno: Back button integration (BackButtonIntegration_RegisterListener) is not supported.
		}
		else
		{
			// Raises the PaneClosing event and restores focus to whichever element had focus when the pane opened.
			OnPaneClosing();
		}

		// If the display modes states changing event was not registered, then
		// do the opened/closed work here instead. This could be the case if
		// the SplitView has been re-templated to remove the 'DisplayModeStates'
		// state group.
		if (!_isDisplayModeStateChangedRegistered)
		{
			OnPaneOpenedOrClosed(isOpen);
		}
	}

	private void OnDisplayModeChanged()
	{
		// TODO Uno: WinUI sets up or tears down the outer dismiss layer here (also on Loaded/Unloaded and IsPaneOpen changes):
		// a popup around the SplitView bounds that light-dismisses the pane when the SplitView doesn't cover the window.
		// The pane is also not closed on XamlRoot changes.
	}

	private void RegisterForDisplayModeStatesChangedEvent()
	{
		if (!_isDisplayModeStateChangedRegistered)
		{
			if (GetTemplateChild("DisplayModeStates") is VisualStateGroup displayModeStates)
			{
				displayModeStates.CurrentStateChanged += OnDisplayModeStateChanged;
				_displayModeStates = displayModeStates;
				_isDisplayModeStateChangedRegistered = true;
			}
		}
	}

	private void OnPaneOpenedOrClosed(bool isPaneOpen)
	{
		if (isPaneOpen)
		{
			PaneOpened?.Invoke(this, null!);

			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Show, this);
		}
		else
		{
			OnPaneClosed();

			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Hide, this);
		}
	}

	private bool IsLightDismissible() =>
		DisplayMode != SplitViewDisplayMode.Inline &&
		DisplayMode != SplitViewDisplayMode.CompactInline;

	private bool CanLightDismiss() => IsPaneOpen && !_isPaneClosingByLightDismiss && IsLightDismissible();

	private double GetOpenPaneLength()
	{
		var openPaneLength = OpenPaneLength;

		// Support Auto/NaN for open pane length to size to the pane content.
		if (double.IsNaN(openPaneLength))
		{
			openPaneLength = _paneMeasuredLength;
		}

		return openPaneLength;
	}

	private void TryCloseLightDismissiblePane()
	{
		var args = new SplitViewPaneClosingEventArgs();

		// Raise the closing event to give the app a chance to cancel.
		// TODO Uno: WinUI raises PaneClosing and PaneClosed asynchronously (fRaiseSync FALSE).
		PaneClosing?.Invoke(this, args);

		// Queue up a deferred UI thread executor that will actually close the pane
		// based on whether it was canceled or not.
		DispatcherQueue.TryEnqueue(() =>
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

		// Flag that we're attempting to close so that we don't queue up multiple of these messages.
		_isPaneClosingByLightDismiss = true;
	}

	private void OnCancelClosing() => _isPaneClosingByLightDismiss = false;

	private void UpdateTemplateSettings()
	{
		var templateSettings = TemplateSettings;

		templateSettings.OpenPaneLength = GetOpenPaneLength();
		templateSettings.CompactPaneLength = CompactPaneLength;
	}

	private void RegisterEventHandlers()
	{
		KeyDown += OnSplitViewKeyDown;

		if (_lightDismissLayer is not null)
		{
			_lightDismissLayer.PointerReleased += OnLightDismissLayerPointerReleased;
		}
	}

	private void UnregisterEventHandlers()
	{
		KeyDown -= OnSplitViewKeyDown;

		if (_lightDismissLayer is not null)
		{
			_lightDismissLayer.PointerReleased -= OnLightDismissLayerPointerReleased;
		}
	}

	private void OnPaneOpening()
	{
		// Try to focus the pane if it's light-dismissible.
		if (IsLightDismissible() && _paneRoot is not null)
		{
			SetFocusToPane();
		}
	}

	private void OnPaneClosing()
	{
		// If the closing flag isn't set, then we're not closing due to some light-dismissible
		// action but rather are closing because the app explicitly set IsPaneOpen = false.
		// In this case, we haven't fired the PaneClosing event yet, so do it now before we
		// fire the PaneClosed event.  Note, this closing action is not cancelable, so we
		// don't care if the app sets the Cancel property on the closing event args.
		if (!_isPaneClosingByLightDismiss)
		{
			PaneClosing?.Invoke(this, new SplitViewPaneClosingEventArgs());
		}

		if (IsLightDismissible())
		{
			RestoreSavedFocusElement();
		}
	}

	private void OnPaneClosed()
	{
		_isPaneClosingByLightDismiss = false;

		PaneClosed?.Invoke(this, null!);
	}

	private void SetFocusToPane()
	{
		// Store weak reference to the previously focused element.
		var focusManager = VisualTree.GetFocusManagerForElement(this);
		if (focusManager?.FocusedElement is { } previousFocusedElement)
		{
			_previousFocusedElementWeakRef = new WeakReference<DependencyObject>(previousFocusedElement);
			_previousFocusState = focusManager.GetRealFocusStateForFocusedElement();
		}

		if (_previousFocusState == FocusState.Unfocused)
		{
			// We will give the pane focus using the same focus state as that of the currently focused element
			// If there is no currently focused element we will fall back to Programmatic focus state.
			_previousFocusState = FocusState.Programmatic;
		}

		// Put focus on the pane.
		// We'll use the previous focus state when setting focus to the pane.
		_paneRoot?.Focus(_previousFocusState, animateIfBringIntoView: false);
	}

	private void RestoreSavedFocusElement()
	{
		if (_previousFocusedElementWeakRef is not null)
		{
			var wasFocusRestored = false;

			// Restore focus to our cached element.
			if (_previousFocusedElementWeakRef.TryGetTarget(out var previousFocusedElement))
			{
				var focusManager = VisualTree.GetFocusManagerForElement(previousFocusedElement);
				if (focusManager is not null && FocusProperties.IsFocusable(previousFocusedElement))
				{
					var result = focusManager.SetFocusedElement(new FocusMovement(previousFocusedElement, FocusNavigationDirection.None, _previousFocusState));
					wasFocusRestored = result.WasMoved;
				}
			}

			// If we failed to restore focus, then try to focus an item in the content area.
			if (!wasFocusRestored && _contentRoot is not null)
			{
				_contentRoot.Focus(_previousFocusState, animateIfBringIntoView: false);
			}

			// Reset our saved focus information.
			_previousFocusedElementWeakRef = null;
			_previousFocusState = FocusState.Unfocused;
		}
	}

	private void OnSplitViewKeyDown(object sender, KeyRoutedEventArgs args)
	{
		// Only consume the Back Key/Trap the focus within the pane
		// If Pane is open and the Display mode is either Overlay or CompatOverlay
		if (CanLightDismiss())
		{
			switch (args.OriginalKey)
			{
				case VirtualKey.Escape:
				case VirtualKey.GamepadB:
					args.Handled = true;
					TryCloseLightDismissiblePane();
					break;
			}

			// TODO Uno: Gamepad XY focus trapping within the pane, tab-stop processing that keeps focus in an open
			// light-dismissible pane, and the compact-mode gamepad navigation between pane and content are not ported.
		}
	}

	private void OnLightDismissLayerPointerReleased(object sender, PointerRoutedEventArgs args)
	{
		if (CanLightDismiss())
		{
			TryCloseLightDismissiblePane();

			args.Handled = true;
		}
	}
}
