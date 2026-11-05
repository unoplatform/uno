// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase_partial.h, tag winui3/release/2.5.1, commit ba3a8d59e

using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class FlyoutBase
{
	internal enum InputPaneState
	{
		InputPaneHidden,
		InputPaneShowing,
	}

	private const FlyoutPlacementMode g_defaultPlacementMode = FlyoutPlacementMode.Top;

	// Offsets for PopupThemeTransition
	private const double g_entranceThemeOffset = 50.0;

	// Margin between FlyoutBase and window's edge, AppBar and placement target.
	internal const float FlyoutMargin = 4;

	// In the case where Placement has not been set explicitly, gives the control a chance
	// to change the Placement value based on other conditions.
	private protected virtual void AutoAdjustPlacement(ref MajorPlacementMode pPlacement) { }

	internal void DisablePresenterResizing() => m_allowPresenterResizing = false;

	internal bool UsePickerFlyoutTheme
	{
		get => m_usePickerFlyoutTheme;
		set => m_usePickerFlyoutTheme = value;
	}

	private protected bool m_isPositionedAtPoint;

	private bool m_openingWindowedInProgress;

	// Cached bounds of placement target. Used in cases where the placement target leaves the visual
	// tree before the flyout is displayed, for example, if the placement target was inside another flyout.
	private Rect m_placementTargetBounds;

	// Cached result of a call to CalculatePlacementTargetBoundsPrivate indicating whether placement fallback
	// should be allowed.
	private bool m_allowPlacementFallbacks;

	// Indicates if presenter has been resized (not due to IHM showing) to fit and resize event should be ignored.
	private bool m_presenterResized;

	// Indicates if the flyout should use the PickerFlyoutThemeTransition.
	private bool m_usePickerFlyoutTheme;

	// Allows us to remember the height of the InputPane even after it has dismissed.
	private double m_cachedInputPaneHeight;

	// Placement target and placement mode saved while the flyout is open
	private MajorPlacementMode m_majorPlacementMode = GetMajorPlacementFromPlacement(g_defaultPlacementMode);
	private PreferredJustification m_preferredJustification = PreferredJustification.Center;

	private Control m_tpPresenter;

	private FlyoutBase m_tpParentFlyout;
	private FlyoutMetadata m_childFlyoutMetadata;

	// Set the target position to show the flyout at the specified position
	private bool m_isTargetPositionSet;

	// when there is not enough space to place the presenter, we'll resize it. default is true.
	private bool m_allowPresenterResizing = true;

	// Indicates if we've overridden the value of the FlyoutPresenter's requested theme.
	private bool m_isFlyoutPresenterRequestedThemeOverridden;

	private Point m_targetPoint;
	private Rect m_exclusionRect = DirectUI.RectUtil.CreateEmptyRect();
	private InputDeviceType m_inputDeviceTypeUsedToOpen;

	// Old values of the target position properties to tell whether we should no-op
	// upon being told to re-show a flyout.
	private bool m_wasTargetPositionSet;
	private Point m_lastTargetPoint;

	private bool m_isLightDismissOverlayEnabled = true;

	private bool m_shouldTakeFocus = true;
	private bool m_shouldHideIfPointerMovesAway;
	private bool m_shouldOverlayPassThroughAllInput;
	private bool m_ownsOverlayInputPassThroughElement;
	private bool m_isPositionedForDateTimePicker;

	private bool m_hasPlacementOverride;
	private FlyoutPlacementMode m_placementOverride = FlyoutPlacementMode.Top;

	private bool m_isRootVisualPointerMovedAttached;
	private ManagedWeakReference m_wrRootVisual;

	private bool m_openingCanceled;

	// m_epPlacementTargetUnloadedHandler and m_epPlacementTargetActualThemeChangedHandler
	private readonly SerialDisposable m_placementTargetSubscriptions = new();
}
