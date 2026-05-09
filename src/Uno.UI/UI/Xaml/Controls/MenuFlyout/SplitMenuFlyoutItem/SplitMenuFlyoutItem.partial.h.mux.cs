// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\SplitMenuFlyoutItem_Partial.h, commit 5f9e85113

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitMenuFlyoutItem
{
	internal Popup GetPopup() => m_tpPopup;

	internal Control GetMenuFlyoutPresenter() => m_tpPresenter;

	// Used by SplitMenuFlyoutItemAutomationPeer to access the primary button.
	internal ButtonBase PrimaryButton => m_tpPrimaryButton;

	// Called by MenuFlyoutPresenter to indicate upward navigation focus
	internal void SetFocusComingFromUpwardNavigation() => m_focusComingFromUpwardNavigation = true;

	// ISubMenuOwner implementation
	bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => true;

	// Template part names
	private const string c_primaryButtonName = "PrimaryButton";
	private const string c_secondaryButtonName = "SecondaryButton";

	// Collection of the sub menu item
	private IList<MenuFlyoutItemBase> m_tpItems;

	// Popup for the SplitMenuFlyoutItem
	private Popup m_tpPopup;

	// Presenter for the SplitMenuFlyoutItem
	private Control m_tpPresenter;

	// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
	private Transition m_tpMenuPopupThemeTransition;

	// Event pointer for the Loaded event
	private readonly SerialDisposable m_epLoadedHandler = new();

	// Event pointer for the size changed on the SplitMenuFlyoutItem's presenter
	private readonly SerialDisposable m_epPresenterSizeChangedHandler = new();

	// Helper to which to delegate cascading menu functionality.
	private CascadingMenuHelper m_menuHelper;

	// Weak reference the parent that owns the menu that this item belongs to.
	private ManagedWeakReference m_wrParentOwner;

	// Template parts
	private ButtonBase m_tpPrimaryButton;
	private ButtonBase m_tpSecondaryButton;

	// Event handlers for template parts
	private readonly SerialDisposable m_epPrimaryButtonPointerEnteredHandler = new();
	private readonly SerialDisposable m_epPrimaryButtonPointerExitedHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonPointerEnteredHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonPointerExitedHandler = new();
	private readonly SerialDisposable m_epPrimaryButtonClickHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonClickHandler = new();
	private readonly SerialDisposable m_epPrimaryButtonGotFocusHandler = new();
	private readonly SerialDisposable m_epPrimaryButtonLostFocusHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonGotFocusHandler = new();
	private readonly SerialDisposable m_epSecondaryButtonLostFocusHandler = new();

	private bool m_itemsSourceRefreshPending;
	private bool m_focusComingFromSubmenu;
	private bool m_focusComingFromUpwardNavigation;
	private bool m_hasSecondaryButtonCustomAutomationName;
}
