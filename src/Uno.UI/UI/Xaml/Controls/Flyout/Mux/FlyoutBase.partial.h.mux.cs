// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\FlyoutBase_partial.h, tag winui3/release/1.4.3, commit 685d2bf

//  Abstract:
//      FlyoutBase - base class for Flyout provides the following functionality:
//        * Showing/hiding.
//        * Enforcing requirement that only one FlyoutBase is open at the time.
//        * Placement logic.
//        * Raising events.
//        * Entrance/exit transitions.
//        * Expose Attached property as storage from Flyouts in XAML.

using DirectUI;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase : DependencyObject
{
	private enum PreferredJustification
	{
		Center,
		Top,
		Bottom,
		Left,
		Right
	};

	private void DisablePresenterResizing() { m_allowPresenterResizing; }

	internal Control GetPresenter() => m_tpPresenter;

	private protected virtual void UpdatePresenterVisualState(MajorPlacementMode placementHint)
	{
	}

	// In the case where Placement has not been set explicitly, gives the control a chance
	// to change the Placement value based on other conditions.
	private protected virtual void AutoAdjustPlacement(ref MajorPlacementMode pPlacement)
	{
	}

	bool m_isPositionedAtPoint;

	bool m_openingWindowedInProgress;

	Popup m_tpPopup = null;

	private SerialDisposable m_epPresenterSizeChangedHandler = new();
	private SerialDisposable m_epPresenterLoadedHandler = new();
	private SerialDisposable m_epPresenterUnloadedHandler = new();
	private SerialDisposable m_epPopupLostFocusHandler = new();
	private SerialDisposable m_epPlacementTargetUnloadedHandler = new();

	// Cached bounds of placement target. Used in cases where the placement target leaves the visual
	// tree before the flyout is displayed, for example, if the placement target was inside another flyout.
	Rect m_placementTargetBounds;

	// Cached result of a call to CalculatePlacementTargetBoundsPrivate indicating whether placement fallback
	// should be allowed.
	private bool m_allowPlacementFallbacks;

	// Indicates if presenter has been resized (not due to IHM showing) to fit and resize event should be ignored.
	private bool m_presenterResized;

	// Allows us to remember the height of the InputPane even after it has dismissed.
	private double m_cachedInputPaneHeight;

	// Indicates if the flyout should use the PickerFlyoutThemeTransition.
	private bool m_usePickerFlyoutTheme;

	// Placement target and placement mode saved while the flyout is open
	private FrameworkElement m_tpPlacementTarget;
	private MajorPlacementMode m_majorPlacementMode;
	private PreferredJustification m_preferredJustification = PreferredJustification.Center;

	private Control m_tpPresenter;
	private Transition m_tpThemeTransition;

	private FlyoutBase m_tpParentFlyout;
	private FlyoutMetadata m_childFlyoutMetadata;

	// Set the target position to show the flyout at the specified position
	private bool m_isTargetPositionSet;

	// when there is not enough space to place the presenter, we'll resize it. default is true.
	private bool m_allowPresenterResizing;

	// Indicates if we've overridden the value of the FlyoutPresenter's requested theme.
	private bool m_isFlyoutPresenterRequestedThemeOverridden;

	private Point m_targetPoint;
	private Rect m_exclusionRect;
	private InputDeviceType m_inputDeviceTypeUsedToOpen;

	// Old values of the target position properties to tell whether we should no-op
	// upon being told to re-show a flyout.
	private bool m_wasTargetPositionSet;
	private Point m_lastTargetPoint;

	private bool m_isLightDismissOverlayEnabled;

	private bool m_shouldTakeFocus = true;
	private bool m_shouldHideIfPointerMovesAway;
	private bool m_shouldOverlayPassThroughAllInput;
	private bool m_ownsOverlayInputPassThroughElement;
	private bool m_isPositionedForDateTimePicker;

	private bool m_hasPlacementOverride;
	private FlyoutPlacementMode m_placementOverride = FlyoutPlacementMode.Top;

	private SerialDisposable m_rootVisualPointerMovedToken = new SerialDisposable();

	private ManagedWeakReference m_wrRootVisual;

	private bool m_openingCanceled;
}
