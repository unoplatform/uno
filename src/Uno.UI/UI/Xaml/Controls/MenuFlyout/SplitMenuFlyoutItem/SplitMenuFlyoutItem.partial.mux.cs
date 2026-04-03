// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\SplitMenuFlyoutItem_Partial.cpp, commit 5f9e85113

using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using FocusManager = Microsoft.UI.Xaml.Input.FocusManager;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.System;

#if HAS_UNO_WINUI
using MUXDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
#else
using MUXDispatcherQueue = Windows.System.DispatcherQueue;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class SplitMenuFlyoutItem
{
	private void PrepareState()
	{
		// Create the sub menu items collection and set the owner
		var spItems = new MenuFlyoutItemBaseCollection(this);
		m_tpItems = spItems;
		Items = spItems;

		m_menuHelper = new CascadingMenuHelper();
		m_menuHelper.Initialize(this);
	}

	protected override void OnApplyTemplate()
	{
		// Unhook old event handlers if they exist
		UnhookTemplate();

		m_hasSecondaryButtonCustomAutomationName = false;

		base.OnApplyTemplate();

		// Get template parts
		m_tpPrimaryButton = GetTemplateChild(c_primaryButtonName) as ButtonBase;
		m_tpSecondaryButton = GetTemplateChild(c_secondaryButtonName) as ButtonBase;

		// Set automation name for the secondary button
		if (m_tpSecondaryButton is not null)
		{
			var secondaryButtonAutomationName = AutomationProperties.GetName(m_tpSecondaryButton);

			if (string.IsNullOrEmpty(secondaryButtonAutomationName))
			{
				bool isOpen = IsOpen;
				SetSecondaryButtonAutomationName(isOpen);
			}
			else
			{
				// Since a custom Automation Name is set in Xaml markup, do not overwrite it here, or in SetIsOpen.
				m_hasSecondaryButtonCustomAutomationName = true;
			}
		}

		if (m_tpPrimaryButton is not null)
		{
			// Setting AutomationId on the primary button to allow peer comparison
			// Used by AutomationPeer when GetChildren is called.
			AutomationProperties.SetAutomationId(m_tpPrimaryButton, "SplitMenuFlyoutItemPrimaryButton");

			// Promote primary button's AutomationProperties.Name to the SplitMenuFlyoutItem if not already set.
			var automationName = AutomationProperties.GetName(this);

			if (string.IsNullOrEmpty(automationName))
			{
				var buttonAutomationName = AutomationProperties.GetName(m_tpPrimaryButton);
				if (!string.IsNullOrEmpty(buttonAutomationName))
				{
					AutomationProperties.SetName(this, buttonAutomationName);
				}
			}

			// Set the events source of the primary button peer to the SplitMenuFlyoutItem's automation peer.
			// This ensures that when focus moves to the primary button, the narrator will announce
			// the SplitMenuFlyoutItem's expand/collapse state and position in set, rather than
			// just announcing the button itself. [ See Expander control for more reference. ]
			var primaryButtonAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(m_tpPrimaryButton);

			if (primaryButtonAutomationPeer is not null)
			{
				var splitMenuFlyoutItemPeer = GetOrCreateAutomationPeer();

				if (splitMenuFlyoutItemPeer is not null)
				{
					var eventsSourceAP = splitMenuFlyoutItemPeer.EventsSource;
					if (eventsSourceAP is not null)
					{
						primaryButtonAutomationPeer.EventsSource = eventsSourceAP;
					}
					else
					{
						primaryButtonAutomationPeer.EventsSource = splitMenuFlyoutItemPeer;
					}
				}
			}
		}

		HookTemplate();
		m_menuHelper.OnApplyTemplate();
		UpdateVisualState(false);
	}

	private void HookTemplate()
	{
		if (m_tpPrimaryButton is not null)
		{
			m_tpPrimaryButton.PointerEntered += OnPrimaryButtonPointerEntered;
			m_epPrimaryButtonPointerEnteredHandler.Disposable = Disposable.Create(() => m_tpPrimaryButton.PointerEntered -= OnPrimaryButtonPointerEntered);

			m_tpPrimaryButton.PointerExited += OnPrimaryButtonPointerExited;
			m_epPrimaryButtonPointerExitedHandler.Disposable = Disposable.Create(() => m_tpPrimaryButton.PointerExited -= OnPrimaryButtonPointerExited);

			((ButtonBase)m_tpPrimaryButton).Click += OnPrimaryButtonClick;
			m_epPrimaryButtonClickHandler.Disposable = Disposable.Create(() => ((ButtonBase)m_tpPrimaryButton).Click -= OnPrimaryButtonClick);

			m_tpPrimaryButton.GotFocus += OnPrimaryButtonGotFocus;
			m_epPrimaryButtonGotFocusHandler.Disposable = Disposable.Create(() => m_tpPrimaryButton.GotFocus -= OnPrimaryButtonGotFocus);

			m_tpPrimaryButton.LostFocus += OnPrimaryButtonLostFocus;
			m_epPrimaryButtonLostFocusHandler.Disposable = Disposable.Create(() => m_tpPrimaryButton.LostFocus -= OnPrimaryButtonLostFocus);
		}

		if (m_tpSecondaryButton is not null)
		{
			m_tpSecondaryButton.PointerEntered += OnSecondaryButtonPointerEntered;
			m_epSecondaryButtonPointerEnteredHandler.Disposable = Disposable.Create(() => m_tpSecondaryButton.PointerEntered -= OnSecondaryButtonPointerEntered);

			m_tpSecondaryButton.PointerExited += OnSecondaryButtonPointerExited;
			m_epSecondaryButtonPointerExitedHandler.Disposable = Disposable.Create(() => m_tpSecondaryButton.PointerExited -= OnSecondaryButtonPointerExited);

			((ButtonBase)m_tpSecondaryButton).Click += OnSecondaryButtonClick;
			m_epSecondaryButtonClickHandler.Disposable = Disposable.Create(() => ((ButtonBase)m_tpSecondaryButton).Click -= OnSecondaryButtonClick);

			m_tpSecondaryButton.GotFocus += OnSecondaryButtonGotFocus;
			m_epSecondaryButtonGotFocusHandler.Disposable = Disposable.Create(() => m_tpSecondaryButton.GotFocus -= OnSecondaryButtonGotFocus);

			m_tpSecondaryButton.LostFocus += OnSecondaryButtonLostFocus;
			m_epSecondaryButtonLostFocusHandler.Disposable = Disposable.Create(() => m_tpSecondaryButton.LostFocus -= OnSecondaryButtonLostFocus);
		}
	}

	private void UnhookTemplate()
	{
		m_epPrimaryButtonPointerEnteredHandler.Disposable = null;
		m_epPrimaryButtonPointerExitedHandler.Disposable = null;
		m_epPrimaryButtonClickHandler.Disposable = null;
		m_epPrimaryButtonGotFocusHandler.Disposable = null;
		m_epPrimaryButtonLostFocusHandler.Disposable = null;
		m_tpPrimaryButton = null;

		m_epSecondaryButtonPointerEnteredHandler.Disposable = null;
		m_epSecondaryButtonPointerExitedHandler.Disposable = null;
		m_epSecondaryButtonClickHandler.Disposable = null;
		m_epSecondaryButtonGotFocusHandler.Disposable = null;
		m_epSecondaryButtonLostFocusHandler.Disposable = null;
		m_tpSecondaryButton = null;
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);
		UpdateParentOwner(null);
		// Don't open submenu here - let button-specific handlers control this
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		base.OnPointerExited(args);

		bool parentIsSubMenu = false;
		MenuFlyoutPresenter parentPresenter = GetParentMenuFlyoutPresenter();

		if (parentPresenter is not null)
		{
			parentIsSubMenu = parentPresenter.IsSubPresenter;
		}

		m_menuHelper.OnPointerExited(args, parentIsSubMenu);
	}

	protected override void OnGotFocus(RoutedEventArgs args)
	{
		base.OnGotFocus(args);

		// When the SplitMenuFlyoutItem receives focus via keyboard,
		// redirect focus to the appropriate button.
		var focusState = FocusState;

		bool hasPrimaryFocus = HasPrimaryButtonFocus();
		bool hasSecondaryFocus = HasSecondaryButtonFocus();

		// If the focus is already set on any button, do not change it.
		if ((focusState == FocusState.Keyboard || focusState == FocusState.Programmatic) &&
			!hasPrimaryFocus && !hasSecondaryFocus)
		{
			// Focus coming from submenu closure or upward navigation - set focus to secondary button
			bool focusSecondaryButton = (m_focusComingFromSubmenu || m_focusComingFromUpwardNavigation) && m_tpSecondaryButton is not null;
			if (focusSecondaryButton || m_tpPrimaryButton is not null)
			{
				SetButtonFocus(focusSecondaryButton, focusState);
			}
		}

		m_focusComingFromSubmenu = false;
		m_focusComingFromUpwardNavigation = false;
		m_menuHelper.OnGotFocus(args);
	}

	protected override void OnLostFocus(RoutedEventArgs args)
	{
		base.OnLostFocus(args);
		m_menuHelper.OnLostFocus(args);
	}

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var key = args.Key;

		bool isLeftKey = (key == VirtualKey.Left || key == VirtualKey.GamepadDPadLeft || key == VirtualKey.GamepadLeftThumbstickLeft);
		bool isRightKey = (key == VirtualKey.Right || key == VirtualKey.GamepadDPadRight || key == VirtualKey.GamepadLeftThumbstickRight);
		bool isUpKey = (key == VirtualKey.Up || key == VirtualKey.GamepadDPadUp || key == VirtualKey.GamepadLeftThumbstickUp);
		bool isDownKey = (key == VirtualKey.Down || key == VirtualKey.GamepadDPadDown || key == VirtualKey.GamepadLeftThumbstickDown);

		// Internal navigation between primary and secondary buttons logic
		bool primaryHasFocus = HasPrimaryButtonFocus();
		bool secondaryHasFocus = HasSecondaryButtonFocus();

		if ((isLeftKey || isUpKey) && secondaryHasFocus)
		{
			SetButtonFocus(false, FocusState.Keyboard);
			args.Handled = true;
			return;
		}
		else if ((isRightKey || isDownKey) && primaryHasFocus)
		{
			SetButtonFocus(true, FocusState.Keyboard);
			args.Handled = true;
			return;
		}

		// Forward to external navigation logic if not handled internally
		bool shouldHandleEvent = MenuFlyout.KeyProcess.KeyDown(key, this);

		// Similar to MenuFlyoutSubItem, we want to allow MenuHelper
		// to process the event even when external navigation takes place.
		m_menuHelper.OnKeyDown(args);
		args.Handled = shouldHandleEvent;
	}

	protected override void OnKeyUp(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var key = args.Key;

		if (key == VirtualKey.Enter ||
			key == VirtualKey.Space ||
			key == VirtualKey.GamepadA)
		{
			bool primaryHasFocus = HasPrimaryButtonFocus();
			bool secondaryHasFocus = HasSecondaryButtonFocus();

			if (primaryHasFocus)
			{
				OnPrimaryButtonClick(m_tpPrimaryButton, null);
				args.Handled = true;
				return;
			}
			else if (secondaryHasFocus)
			{
				OnSecondaryButtonClick(m_tpSecondaryButton, null);
				args.Handled = true;
				return;
			}
		}

		// Forward to parent handler (this method is called by MenuFlyoutItem)
		// We are using it here directly to prevent setting the event as handled.
		bool shouldHandleEvent = MenuFlyout.KeyProcess.KeyUp(key, this);

		// Similar to KeyDown, allow MenuHelper to process the event even if the control
		// should handle the event.
		m_menuHelper.OnKeyUp(args);
		args.Handled = shouldHandleEvent;
	}

	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs args)
	{
		m_menuHelper.OnIsEnabledChanged(args);
	}

	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		m_menuHelper.OnVisibilityChanged();
	}

	private protected override void ChangeVisualState(bool bUseTransitions)
	{
		bool hasToggleMenuItem = false;
		bool hasIconMenuItem = false;
		bool hasMenuItemWithKeyboardAcceleratorText = false;
		bool shouldBeNarrow = GetShouldBeNarrow();
		bool isPrimaryButtonPressed = false;
		bool isSecondaryButtonPressed = false;
		bool bIsPopupOpened = false;
		bool showSubMenuOpenedState = false;
		bool isKeyboardPresent = false;

		var bIsEnabled = IsEnabled;
		var focusState = FocusState;

		bool isPrimaryButtonPointerOver = false;
		bool isSecondaryButtonPointerOver = false;

		if (m_tpPrimaryButton is not null)
		{
			isPrimaryButtonPressed = m_tpPrimaryButton.IsPressed;
			isPrimaryButtonPointerOver = m_tpPrimaryButton.IsPointerOver;
		}
		if (m_tpSecondaryButton is not null)
		{
			isSecondaryButtonPressed = m_tpSecondaryButton.IsPressed;
			isSecondaryButtonPointerOver = m_tpSecondaryButton.IsPointerOver;
		}

		MenuFlyoutPresenter spPresenter = GetParentMenuFlyoutPresenter();
		if (spPresenter is not null)
		{
			hasToggleMenuItem = spPresenter.GetContainsToggleItems();
			hasIconMenuItem = spPresenter.GetContainsIconItems();
			hasMenuItemWithKeyboardAcceleratorText = spPresenter.GetContainsItemsWithKeyboardAcceleratorText();
		}

		// Check if submenu is opened (following MenuFlyoutSubItem pattern)
		if (m_tpPopup is not null)
		{
			bIsPopupOpened = m_tpPopup.IsOpen;
		}

		var isDelayCloseTimerRunning = m_menuHelper.IsDelayCloseTimerRunning();
		if (bIsPopupOpened && !isDelayCloseTimerRunning)
		{
			showSubMenuOpenedState = true;
		}

		// We only care about finding if we have a keyboard if we also have a menu item with accelerator text,
		// since if we don't have any menu items with accelerator text, we won't be showing any accelerator text anyway.
		if (hasMenuItemWithKeyboardAcceleratorText)
		{
			isKeyboardPresent = DXamlCore.Current.IsKeyboardPresent;
		}

		// CommonStates
		if (!bIsEnabled)
		{
			VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
		}
		else if (showSubMenuOpenedState)
		{
			VisualStateManager.GoToState(this, "SubMenuOpened", bUseTransitions);
		}
		else if (isPrimaryButtonPressed)
		{
			// Primary button is being pressed
			VisualStateManager.GoToState(this, "PrimaryPressed", bUseTransitions);
		}
		else if (isSecondaryButtonPressed)
		{
			// Secondary button is being pressed
			VisualStateManager.GoToState(this, "SecondaryPressed", bUseTransitions);
		}
		else if (isPrimaryButtonPointerOver)
		{
			// Pointer is over primary button area
			VisualStateManager.GoToState(this, "PrimaryPointerOver", bUseTransitions);
		}
		else if (isSecondaryButtonPointerOver)
		{
			// Pointer is over secondary button area
			VisualStateManager.GoToState(this, "SecondaryPointerOver", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "Normal", bUseTransitions);
		}

		// FocusStates
		if (FocusState.Unfocused != focusState && bIsEnabled)
		{
			if (FocusState.Pointer == focusState)
			{
				VisualStateManager.GoToState(this, "PointerFocused", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Focused", bUseTransitions);
			}
		}
		else
		{
			VisualStateManager.GoToState(this, "Unfocused", bUseTransitions);
		}

		// CheckPlaceholderStates
		if (hasToggleMenuItem && hasIconMenuItem)
		{
			VisualStateManager.GoToState(this, "CheckAndIconPlaceholder", bUseTransitions);
		}
		else if (hasToggleMenuItem)
		{
			VisualStateManager.GoToState(this, "CheckPlaceholder", bUseTransitions);
		}
		else if (hasIconMenuItem)
		{
			VisualStateManager.GoToState(this, "IconPlaceholder", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "NoPlaceholder", bUseTransitions);
		}

		// PaddingSizeStates
		if (shouldBeNarrow)
		{
			VisualStateManager.GoToState(this, "NarrowPadding", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "DefaultPadding", bUseTransitions);
		}

		// KeyboardAcceleratorTextStates
		// As per the design team, the requirement is to not show the keyboard accelerator text
		// for this control. However, the developers may want to show it in their custom styles.
		if (hasMenuItemWithKeyboardAcceleratorText && isKeyboardPresent)
		{
			VisualStateManager.GoToState(this, "KeyboardAcceleratorTextVisible", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "KeyboardAcceleratorTextCollapsed", bUseTransitions);
		}
	}

	// Property changed callbacks
	private void OnSubMenuPresenterStyleChanged(DependencyPropertyChangedEventArgs args)
	{
		if (m_tpPresenter is not null)
		{
			var spStyle = args.NewValue as Style;
			SetPresenterStyle(m_tpPresenter as Control, spStyle);
		}
	}

	private void OnSubMenuItemStyleChanged(DependencyPropertyChangedEventArgs args)
	{
		ApplySubMenuItemStyleToItems();
	}

	// TODO Uno: WinUI handles Text and AutomationProperties.Name property changes via OnPropertyChanged2.
	// In Uno, we rely on the DP system to raise the change. This may need additional hookup
	// if the secondary button automation name doesn't update when Text changes.

	// Create SplitMenuFlyoutItemAutomationPeer to represent the SplitMenuFlyoutItem.
	protected override AutomationPeer OnCreateAutomationPeer() => new SplitMenuFlyoutItemAutomationPeer(this);

	internal void QueueRefreshItemsSource()
	{
		// The items source might change multiple times in a single tick, so we'll coalesce the refresh
		// into a single event once all of the changes have completed.
		if (m_tpPresenter is not null && !m_itemsSourceRefreshPending)
		{
			var dispatcherQueue = MUXDispatcherQueue.GetForCurrentThread();

			var wrThis = WeakReferencePool.RentSelfWeakReference(this);

			dispatcherQueue.TryEnqueue(
				() =>
				{
					if (!wrThis.IsAlive)
					{
						return;
					}

					var thisSplitMenuFlyoutItem = wrThis.Target as SplitMenuFlyoutItem;

					if (thisSplitMenuFlyoutItem is not null)
					{
						thisSplitMenuFlyoutItem.RefreshItemsSource();
						thisSplitMenuFlyoutItem.ApplySubMenuItemStyleToItems();
					}
				});

			m_itemsSourceRefreshPending = true;
		}
	}

	private void RefreshItemsSource()
	{
		m_itemsSourceRefreshPending = false;

		global::System.Diagnostics.Debug.Assert(m_tpPresenter is not null);

		// Setting the items source to null and then back to Items causes the presenter to pick up any changes.
		((ItemsControl)m_tpPresenter).ItemsSource = null;
		((ItemsControl)m_tpPresenter).ItemsSource = m_tpItems;
	}

	// ISubMenuOwner implementation
	bool ISubMenuOwner.IsSubMenuOpen => IsOpen;

	ISubMenuOwner ISubMenuOwner.ParentOwner
	{
		get => m_wrParentOwner?.Target as ISubMenuOwner;
		set => m_wrParentOwner = WeakReferencePool.RentWeakReference(this, value);
	}

	void ISubMenuOwner.SetSubMenuDirection(bool isSubMenuDirectionUp)
	{
		if (m_tpMenuPopupThemeTransition is not null)
		{
			((MenuPopupThemeTransition)m_tpMenuPopupThemeTransition).Direction = isSubMenuDirectionUp ?
				AnimationDirection.Bottom : AnimationDirection.Top;
		}
	}

	void ISubMenuOwner.PrepareSubMenu()
	{
		EnsurePopupAndPresenter();

		global::System.Diagnostics.Debug.Assert(m_tpPopup is not null);
		global::System.Diagnostics.Debug.Assert(m_tpPresenter is not null);
	}

	void ISubMenuOwner.OpenSubMenu(Point position)
	{
		EnsurePopupAndPresenter();
		EnsureCloseExistingSubItems();

		MenuFlyout parentMenuFlyout = null;
		MenuFlyoutPresenter parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

		if (parentMenuFlyoutPresenter is not null)
		{
			IMenu owningMenu = (parentMenuFlyoutPresenter as IMenuPresenter).OwningMenu;
			(m_tpPresenter as IMenuPresenter).OwningMenu = owningMenu;

			parentMenuFlyout = parentMenuFlyoutPresenter.GetParentMenuFlyout();

			if (parentMenuFlyout is not null)
			{
				// Update the TemplateSettings before it is opened.
				(m_tpPresenter as MenuFlyoutPresenter).SetParentMenuFlyout(parentMenuFlyout);
				(m_tpPresenter as MenuFlyoutPresenter).UpdateTemplateSettings();

				// Forward the parent presenter's properties to the sub presenter
				ForwardPresenterProperties(
					parentMenuFlyout,
					parentMenuFlyoutPresenter,
					m_tpPresenter as MenuFlyoutPresenter);
			}
		}

		m_tpPopup.HorizontalOffset = position.X;
		m_tpPopup.VerticalOffset = position.Y;
		SetIsOpen(true);

		if (parentMenuFlyout is not null)
		{
			ForwardSystemBackdropToPopup(parentMenuFlyout);
		}
	}

	void ISubMenuOwner.PositionSubMenu(Point position)
	{
		if (position.X != double.NegativeInfinity)
		{
			m_tpPopup.HorizontalOffset = position.X;
		}

		if (position.Y != double.NegativeInfinity)
		{
			m_tpPopup.VerticalOffset = position.Y;
		}
	}

	void ISubMenuOwner.ClosePeerSubMenus() => EnsureCloseExistingSubItems();

	void ISubMenuOwner.CloseSubMenu() => SetIsOpen(false);

	void ISubMenuOwner.CloseSubMenuTree() => m_menuHelper.CloseChildSubMenus();

	void ISubMenuOwner.DelayCloseSubMenu() => m_menuHelper.DelayCloseSubMenu();

	void ISubMenuOwner.CancelCloseSubMenu() => m_menuHelper.CancelCloseSubMenu();

	void ISubMenuOwner.RaiseAutomationPeerExpandCollapse(bool isOpen)
	{
		var isListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
		if (isListener)
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is SplitMenuFlyoutItemAutomationPeer peer)
			{
				peer.RaiseExpandCollapseAutomationEvent(isOpen);
			}
		}
	}

	internal void Open() => m_menuHelper.OpenSubMenu();

	internal void Close() => m_menuHelper.CloseSubMenu();

	internal bool IsOpen => m_tpPopup?.IsOpen ?? false;

	// Helper methods delegated to CascadingMenuHelper
	private Control CreateSubPresenter()
	{
		MenuFlyoutPresenter spPresenter = new();

		// Specify the sub MenuFlyoutPresenter
		spPresenter.IsSubPresenter = true;
		(spPresenter as IMenuPresenter).Owner = this;

		return spPresenter;
	}

	private void EnsurePopupAndPresenter()
	{
		if (m_tpPopup is null)
		{
			Popup spPopup = new();
			spPopup.IsSubMenu = true;
			spPopup.IsLightDismissEnabled = false;

			var spParentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();
			if (spParentMenuFlyoutPresenter is not null)
			{
				var spParentMenuFlyout = spParentMenuFlyoutPresenter.GetParentMenuFlyout();
				// TODO Uno: Windowed popup support
				if (spParentMenuFlyout is not null)
				{
					XamlRoot xamlRoot = XamlRoot.GetForElement(spParentMenuFlyoutPresenter);
					if (xamlRoot is not null)
					{
						spPopup.XamlRoot = xamlRoot;
					}
				}
			}

			var spPresenter = CreateSubPresenter();

			if (spParentMenuFlyoutPresenter is not null)
			{
				int parentDepth = spParentMenuFlyoutPresenter.GetDepth();
				(spPresenter as MenuFlyoutPresenter).SetDepth(parentDepth + 1);
			}

			spPopup.Child = spPresenter as FrameworkElement;

			m_tpPresenter = spPresenter;
			m_tpPopup = spPopup;

			((ItemsControl)m_tpPresenter).ItemsSource = m_tpItems;

			// Apply SubMenuItemStyle to all items
			ApplySubMenuItemStyleToItems();

			FrameworkElement spPresenterAsFE = spPresenter;
			spPresenterAsFE.SizeChanged += OnPresenterSizeChanged;
			m_epPresenterSizeChangedHandler.Disposable = Disposable.Create(() => spPresenterAsFE.SizeChanged -= OnPresenterSizeChanged);

			m_menuHelper.SetSubMenuPresenter(spPresenter);
		}
	}

	private void UpdateParentOwner(MenuFlyoutPresenter parentMenuFlyoutPresenter)
	{
		var parentPresenter = parentMenuFlyoutPresenter;
		if (parentPresenter is null)
		{
			parentPresenter = GetParentMenuFlyoutPresenter();
		}

		if (parentPresenter is not null)
		{
			ISubMenuOwner parentSubMenuOwner = (parentPresenter as IMenuPresenter).Owner;

			if (parentSubMenuOwner is not null)
			{
				((ISubMenuOwner)this).ParentOwner = parentSubMenuOwner;
			}
		}
	}

	private void SetIsOpen(bool isOpen)
	{
		bool isOpened = m_tpPopup?.IsOpen ?? false;

		if (isOpen != isOpened)
		{
			(m_tpPresenter as IMenuPresenter).Owner = isOpen ? this : null;

			MenuFlyoutPresenter parentPresenter = GetParentMenuFlyoutPresenter();

			if (parentPresenter is not null)
			{
				(parentPresenter as IMenuPresenter).SubPresenter = isOpen ? m_tpPresenter as MenuFlyoutPresenter : null;

				IMenu owningMenu = (parentPresenter as IMenuPresenter).OwningMenu;

				if (owningMenu is not null)
				{
					(m_tpPresenter as IMenuPresenter).OwningMenu = isOpen ? owningMenu : null;
				}

				UpdateParentOwner(parentPresenter);
			}

			VisualTree visualTree = VisualTree.GetForElement(this);
			if (visualTree is not null)
			{
				// Put the popup on the same VisualTree as this flyout sub item to make sure it shows up in the right place
				m_tpPopup.SetVisualTree(visualTree);
			}

			// Set the popup open or close state
			m_tpPopup.IsOpen = isOpen;

			// Update the secondary button automation name based on the new state
			if (m_tpSecondaryButton is not null && !m_hasSecondaryButtonCustomAutomationName)
			{
				SetSecondaryButtonAutomationName(isOpen);
			}

			// Set the focus to the displayed sub menu presenter when opened and
			// set the focus back to the original sub item when closed.
			if (isOpen)
			{
				// Set the focus to the displayed sub menu presenter to navigate the each sub items
				this.SetFocusedElement(m_tpPresenter, FocusState.Programmatic, false);
			}
			else
			{
				// Mark that focus is coming from submenu closure
				m_focusComingFromSubmenu = true;

				// Set the focus to the sub menu item
				this.SetFocusedElement(this, FocusState.Programmatic, false);
			}

			UpdateVisualState();
		}
	}

	private void ClearStateFlags()
	{
		m_focusComingFromSubmenu = false;
		m_focusComingFromUpwardNavigation = false;
		m_menuHelper.ClearStateFlags();
	}

	private void OnPresenterSizeChanged(object pSender, SizeChangedEventArgs args)
	{
		if (m_tpMenuPopupThemeTransition is null &&
			ApiInformation.IsTypePresent("Uno.UI,Microsoft.UI.Xaml.Media.Animation.PopupThemeTransition"))
		{
			MenuFlyoutPresenter parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

			// Get how many sub menus deep we are. We need this number to know what kind of Z
			// offset to use for displaying elevation. The menus aren't parented in the visual
			// hierarchy so that has to be applied with an additional transform.
			int depth = 1;
			if (parentMenuFlyoutPresenter is not null)
			{
				depth = parentMenuFlyoutPresenter.GetDepth() + 1;
			}

			Transition spMenuPopupChildTransition = MenuFlyout.PreparePopupThemeTransitionsAndShadows(m_tpPopup, 0.67 /* closedRatioConstant */, depth);
			((MenuPopupThemeTransition)spMenuPopupChildTransition).Direction = AnimationDirection.Top;
			m_tpMenuPopupThemeTransition = spMenuPopupChildTransition;
		}

		m_menuHelper.OnPresenterSizeChanged(pSender, args, m_tpPopup);

		// Update the OpenedLength property of the ThemeTransition.
		double openedLength = (m_tpPresenter as Control).ActualHeight;

		if (m_tpMenuPopupThemeTransition is MenuPopupThemeTransition menuTransition)
		{
			menuTransition.OpenedLength = openedLength;
		}
	}

	private static void SetPresenterStyle(Control pPresenter, Style pStyle)
	{
		global::System.Diagnostics.Debug.Assert(pPresenter is not null);

		if (pStyle is not null)
		{
			pPresenter.Style = pStyle;
		}
		else
		{
			pPresenter.ClearValue(FrameworkElement.StyleProperty);
		}
	}

	private void ApplySubMenuItemStyleToItems()
	{
		// Get the SubMenuItemStyle
		var spSubMenuItemStyle = SubMenuItemStyle;

		// Apply style to all items in the collection
		if (m_tpItems is not null)
		{
			for (int i = 0; i < m_tpItems.Count; i++)
			{
				var spItemBase = m_tpItems[i];

				if (spItemBase is not null)
				{
					ApplySubMenuItemStyleToItem(spItemBase, spSubMenuItemStyle);
				}
			}
		}
	}

	// Apply SubMenuItemStyle to a single item, following the ItemsControl pattern
	private static void ApplySubMenuItemStyleToItem(MenuFlyoutItemBase pItem, Style pStyle)
	{
		if (pItem is not FrameworkElement spItemAsFE)
		{
			return;
		}

		// Check if style is already set locally (user explicitly set it)
		var spLocalStyle = spItemAsFE.ReadLocalValue(FrameworkElement.StyleProperty);
		bool isUnsetValue = spLocalStyle == DependencyProperty.UnsetValue;

		bool isStyleSetFromSplitMenuItem = spItemAsFE.IsStyleSetFromItemsControl;

		// Only apply our style if no local style is set, or if we previously set the style
		if (isUnsetValue || isStyleSetFromSplitMenuItem)
		{
			if (pStyle is not null)
			{
				// Apply the SubMenuItemStyle
				spItemAsFE.Style = pStyle;
				spItemAsFE.IsStyleSetFromItemsControl = true;
			}
			else
			{
				// Clear the style if SubMenuItemStyle is null
				spItemAsFE.ClearValue(FrameworkElement.StyleProperty);
				spItemAsFE.IsStyleSetFromItemsControl = false;
			}
		}
	}

	private void ForwardPresenterProperties(
		MenuFlyout pOwnerMenuFlyout,
		MenuFlyoutPresenter pParentMenuFlyoutPresenter,
		MenuFlyoutPresenter pSubMenuFlyoutPresenter)
	{
		var spSubMenuFlyoutPresenter = pSubMenuFlyoutPresenter;
		DependencyObject spThisAsDO = this;

		global::System.Diagnostics.Debug.Assert(pOwnerMenuFlyout is not null && pParentMenuFlyoutPresenter is not null && pSubMenuFlyoutPresenter is not null);

		Control spSubMenuFlyoutPresenterAsControl = spSubMenuFlyoutPresenter;

		// Set the sub presenter style - prioritize SplitMenuFlyoutItem's SubMenuPresenterStyle over MenuFlyout's MenuFlyoutPresenterStyle
		Style spStyle = SubMenuPresenterStyle;

		if (spStyle is null)
		{
			// Fall back to the MenuFlyout's presenter style if no SubMenuPresenterStyle is set
			spStyle = pOwnerMenuFlyout.MenuFlyoutPresenterStyle;
		}

		if (spStyle is not null)
		{
			pSubMenuFlyoutPresenter.Style = spStyle;
		}
		else
		{
			pSubMenuFlyoutPresenter.ClearValue(FrameworkElement.StyleProperty);
		}

		// Set the sub presenter's RequestTheme from the parent presenter's RequestTheme
		ElementTheme parentPresenterTheme = pParentMenuFlyoutPresenter.RequestedTheme;
		pSubMenuFlyoutPresenter.RequestedTheme = parentPresenterTheme;

		// Set the sub presenter's DataContext from the parent presenter's DataContext
		object spDataContext = pParentMenuFlyoutPresenter.DataContext;
		pSubMenuFlyoutPresenter.DataContext = spDataContext;

		// Set the sub presenter's FlowDirection from the current sub menu item's FlowDirection
		var flowDirection = FlowDirection;
		pSubMenuFlyoutPresenter.FlowDirection = flowDirection;

		// Set the popup's FlowDirection from the current FlowDirection
		FrameworkElement spPopupAsFE = m_tpPopup;
		spPopupAsFE.FlowDirection = flowDirection;
		// Also set the popup's theme.
		spPopupAsFE.RequestedTheme = parentPresenterTheme;

		// Set the sub presenter's Language from the parent presenter's Language
		pSubMenuFlyoutPresenter.Language = pParentMenuFlyoutPresenter.Language;

		// Set the sub presenter's IsTextScaleFactorEnabledInternal from the parent presenter's IsTextScaleFactorEnabledInternal
		var isTextScaleFactorEnabled = pParentMenuFlyoutPresenter.IsTextScaleFactorEnabledInternal;
		pSubMenuFlyoutPresenter.IsTextScaleFactorEnabledInternal = isTextScaleFactorEnabled;

		ElementSoundMode soundMode = ElementSoundPlayerService.Instance.GetEffectiveSoundMode(spThisAsDO);

		spSubMenuFlyoutPresenterAsControl.ElementSoundMode = soundMode;
	}

	private void ForwardSystemBackdropToPopup(MenuFlyout ownerMenuFlyout)
	{
#if HAS_UNO_WINUI
		var flyoutSystemBackdrop = ownerMenuFlyout.SystemBackdrop;
		var popupSystemBackdrop = m_tpPopup.SystemBackdrop;
		if (flyoutSystemBackdrop != popupSystemBackdrop)
		{
			m_tpPopup.SystemBackdrop = flyoutSystemBackdrop;
		}
#endif
	}

	private void EnsureCloseExistingSubItems()
	{
		MenuFlyoutPresenter spParentPresenter = GetParentMenuFlyoutPresenter();
		if (spParentPresenter is not null)
		{
			IMenuPresenter openedSubPresenter = (spParentPresenter as IMenuPresenter).SubPresenter;
			if (openedSubPresenter is not null)
			{
				ISubMenuOwner subMenuOwner = openedSubPresenter.Owner;
				if (subMenuOwner is not null && subMenuOwner != this)
				{
					openedSubPresenter.CloseSubMenu();
				}
			}
		}
	}

	// Primary button event handlers
	private void OnPrimaryButtonPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		// Close any open submenu when hovering over primary button
		bool isOpen = IsOpen;
		if (isOpen)
		{
			m_menuHelper.DelayCloseSubMenu();
		}

		UpdateVisualState();
	}

	private void OnPrimaryButtonPointerExited(object sender, PointerRoutedEventArgs args)
	{
		UpdateVisualState();
	}

	// Secondary button event handlers
	private void OnSecondaryButtonPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		m_menuHelper.OnPointerEntered(args);
	}

	private void OnSecondaryButtonPointerExited(object sender, PointerRoutedEventArgs args)
	{
		// Close submenu when exiting secondary button
		bool parentIsSubMenu = false;
		MenuFlyoutPresenter parentPresenter = GetParentMenuFlyoutPresenter();

		if (parentPresenter is not null)
		{
			parentIsSubMenu = parentPresenter.IsSubPresenter;
		}

		m_menuHelper.OnPointerExited(args, parentIsSubMenu);
	}

	// Primary button click handler - delegates to the inherited Invoke method
	private void OnPrimaryButtonClick(object sender, RoutedEventArgs args)
	{
		Invoke();

		// There is an intermittent issue where the primary button remains in the
		// "PointerOver" visual state after clicking it. This probably happens because
		// on click the Flyout closes immediately, so the PointerExited event doesn't get
		// raised. To workaround this, we explicitly reset the button states here.
		ResetButtonStates();
	}

	// Secondary button click handler - toggles the submenu
	private void OnSecondaryButtonClick(object sender, RoutedEventArgs args)
	{
		bool isOpen = IsOpen;

		if (isOpen)
		{
			m_menuHelper.CloseSubMenu();
		}
		else
		{
			m_menuHelper.OpenSubMenu();
		}
	}

	private void OnPrimaryButtonGotFocus(object sender, RoutedEventArgs args) => UpdateVisualState();

	private void OnPrimaryButtonLostFocus(object sender, RoutedEventArgs args) => UpdateVisualState();

	private void OnSecondaryButtonGotFocus(object sender, RoutedEventArgs args) => UpdateVisualState();

	private void OnSecondaryButtonLostFocus(object sender, RoutedEventArgs args) => UpdateVisualState();

	// Sets focus to either the primary or secondary button
	private void SetButtonFocus(bool focusSecondaryButton, FocusState focusState)
	{
		var targetButton = focusSecondaryButton ? m_tpSecondaryButton : m_tpPrimaryButton;

		if (targetButton is not null)
		{
			this.SetFocusedElement(targetButton, focusState, false);
			UpdateVisualState();
		}
	}

	// Helper method to check if a specific button has focus
	private bool HasButtonFocus(ButtonBase button)
	{
		if (button is null)
		{
			return false;
		}

		var focusedElement = XamlRoot is not null ?
			FocusManager.GetFocusedElement(XamlRoot) :
			null;
		return focusedElement == button;
	}

	private void ResetButtonStates()
	{
		// TODO Uno: In WinUI, this clears the IsPointerOver state on the primary button.
		// In Uno, ButtonBase.IsPointerOver may not be directly settable.
		// This is a workaround for the intermittent PointerOver visual state issue.
		UpdateVisualState();
	}

	private bool HasPrimaryButtonFocus() => HasButtonFocus(m_tpPrimaryButton);

	private bool HasSecondaryButtonFocus() => HasButtonFocus(m_tpSecondaryButton);

	private void SetSecondaryButtonAutomationName(bool isSubMenuOpen)
	{
		if (m_tpSecondaryButton is null)
		{
			return;
		}

		string ownerAutomationName = AutomationProperties.GetName(this);

		if (string.IsNullOrEmpty(ownerAutomationName))
		{
			ownerAutomationName = GetPlainText();
		}

		// Use the appropriate localized string based on state
		string resourceKey = isSubMenuOpen ? "UIA_LESS_BUTTON_FOR_OWNER" : "UIA_MORE_BUTTON_FOR_OWNER";

		string secondaryButtonAutomationName = DirectUI.DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(resourceKey);

		// Replace %s placeholder with owner name
		if (!string.IsNullOrEmpty(secondaryButtonAutomationName))
		{
			secondaryButtonAutomationName = string.Format(
				secondaryButtonAutomationName.Replace("%s", "{0}"),
				ownerAutomationName ?? string.Empty);
		}

		AutomationProperties.SetName(m_tpSecondaryButton, secondaryButtonAutomationName);
	}

	internal override string GetPlainText() => Text;
}
