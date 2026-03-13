// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutSubItem_Partial.h, tag winui3/release/1.8.1, commit cd3b7ad0eca

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutSubItem
{
	internal Popup GetPopup() => m_tpPopup;

	internal Control GetMenuFlyoutPresenter() => m_tpPresenter;

	// ISubMenuOwner implementation
	bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => true;

	// Collection of the sub menu item
	private IList<MenuFlyoutItemBase> m_tpItems;

	// Popup for the MenuFlyoutSubItem
	private Popup m_tpPopup;

	// Presenter for the MenuFlyoutSubItem
	private Control m_tpPresenter;

	// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
	private Transition m_tpMenuPopupThemeTransition;

	// Event pointer for the Loaded event
	private readonly SerialDisposable m_epLoadedHandler = new();

	// Event pointer for the size changed on the MenuFlyoutSubItem's presenter
	private readonly SerialDisposable m_epPresenterSizeChangedHandler = new();

	// Helper to which to delegate cascading menu functionality.
	private CascadingMenuHelper m_menuHelper;

	// Weak reference the parent that owns the menu that this item belongs to.
	private ManagedWeakReference m_wrParentOwner;

	private bool _itemsSourceRefreshPending;
}
