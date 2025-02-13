// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutSubItem_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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

public partial class MenuFlyoutSubItem : MenuFlyoutItemBase, ISubMenuOwner
{
	void PrepareState()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: PrepareState.", this));
#endif // MFSI_DEBUG

		//base.PrepareState();

		// Create the sub menu items collection and set the owner
		var spItems = new MenuFlyoutItemBaseCollection(this);
		m_tpItems = spItems;
		Items = spItems;

		m_menuHelper = new CascadingMenuHelper();
		m_menuHelper.Initialize(this);
	}

#if false
	void DisconnectFrameworkPeerCore()
	{
		// Ensure the clean up the items whenever MenuFlyoutSubItem is disconnected
		//if (m_tpItems.GetAsCoreDO() != null)
		//{
		//	(Collection_Clear((CCollection*)(m_tpItems.GetAsCoreDO())));
		//	(Collection_SetOwner((CCollection*)(m_tpItems.GetAsCoreDO()), null));
		//}

		// (MenuFlyoutSubItemGenerated.DisconnectFrameworkPeerCore());
	}
#endif

	protected override void OnApplyTemplate()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnApplyTemplate.", this));
#endif // MFSI_DEBUG

		m_menuHelper.OnApplyTemplate();
	}

	// PointerEntered event handler that shows the MenuFlyoutSubItem
	// whenever the pointer is over to the
	// In case of touch, the MenuFlyoutSubItem will be shown by
	// PointerReleased event.

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		base.OnPointerEntered(args);

		UpdateParentOwner(null /*parentMenuFlyoutPresenter*/);
		m_menuHelper.OnPointerEntered(args);
	}

	// PointerExited event handler that ensures the close MenuFlyoutSubItem
	// whenever the pointer over is out of the current MenuFlyoutSubItem or
	// out of the main presenter. If the exited point is on MenuFlyoutSubItem
	// or sub presenter position, we want to keep the opened

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		base.OnPointerExited(args);

#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnPointerExited.", this));
#endif // MFSI_DEBUG

		bool parentIsSubMenu = false;
		MenuFlyoutPresenter parentPresenter = GetParentMenuFlyoutPresenter();

		if (parentPresenter != null)
		{
			parentIsSubMenu = parentPresenter.IsSubPresenter;
		}

		m_menuHelper.OnPointerExited(args, parentIsSubMenu);
	}

	// PointerPressed event handler that ensures the pressed state.
	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		base.OnPointerPressed(args);
		m_menuHelper.OnPointerPressed(args);
	}

	// PointerReleased event handler that shows MenuFlyoutSubItem in
	// case of touch input.
	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		base.OnPointerReleased(args);
		m_menuHelper.OnPointerReleased(args);
	}

	protected override void OnGotFocus(RoutedEventArgs args)
	{
		base.OnGotFocus(args);
		m_menuHelper.OnGotFocus(args);
	}


	protected override void OnLostFocus(RoutedEventArgs args)
	{
		base.OnLostFocus(args);
		m_menuHelper.OnLostFocus(args);
	}

	// KeyDown event handler that handles the keyboard navigation between
	// the menu items and shows the MenuFlyoutSubItem in case of hitting
	// the enter or right arrow key.

	protected override void OnKeyDown(KeyRoutedEventArgs args)
	{
		base.OnKeyDown(args);

		bool handled = args.Handled;
		bool shouldHandleEvent = false;

		if (!handled)
		{
			MenuFlyoutPresenter spParentPresenter = GetParentMenuFlyoutPresenter();

			if (spParentPresenter != null)
			{
				var key = args.Key;

				// Navigate each item with the arrow down or up key
				if (key == VirtualKey.Down || key == VirtualKey.Up)
				{
					spParentPresenter.HandleUpOrDownKey(key == VirtualKey.Down);
					UpdateVisualState();

					// If we handle the event here, it won't get handled in m_menuHelper.OnKeyDown,
					// so we'll do that afterwards.
					shouldHandleEvent = true;
				}
			}
		}

		m_menuHelper.OnKeyDown(args);
		args.Handled = shouldHandleEvent;
	}


	protected override void OnKeyUp(KeyRoutedEventArgs args)
	{
		base.OnKeyUp(args);
		m_menuHelper.OnKeyUp(args);
	}

	// Ensure the creating the popup and menu presenter to show the
	private void EnsurePopupAndPresenter()
	{
		if (m_tpPopup is null)
		{
			UIElement spPresenterAsUI;
			FrameworkElement spPresenterAsFE;
			Popup spPopup = new();
			spPopup.IsSubMenu = true;
			spPopup.IsLightDismissEnabled = false;

			var spParentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();
			if (spParentMenuFlyoutPresenter != null)
			{
				var spParentMenuFlyout = spParentMenuFlyoutPresenter.GetParentMenuFlyout();
				// Set the windowed Popup if the MenuFlyout is set the windowed Popup
				// TODO Uno: Windowed popup support
				if (spParentMenuFlyout is not null) //&& spParentMenuFlyout.IsWindowedPopup())
				{
					//spPopup.SetIsWindowed();

					// Ensure the sub menu is the windowed Popup
					// global::System.Diagnostics.Debug.Assert((spPopup.IsWindowed())

					XamlRoot xamlRoot = XamlRoot.GetForElement(spParentMenuFlyoutPresenter);
					if (xamlRoot is not null)
					{
						spPopup.XamlRoot = xamlRoot;
					}
				}
			}

			var spPresenter = CreateSubPresenter();
			spPresenterAsUI = spPresenter;

			if (spParentMenuFlyoutPresenter != null)
			{
				int parentDepth = spParentMenuFlyoutPresenter.GetDepth();
				(spPresenter as MenuFlyoutPresenter).SetDepth(parentDepth + 1);
			}

			spPopup.Child = spPresenterAsUI as FrameworkElement;

			m_tpPresenter = spPresenter;
			m_tpPopup = spPopup;

			((ItemsControl)m_tpPresenter).ItemsSource = m_tpItems;

			spPresenterAsFE = spPresenter;

			spPresenterAsFE.SizeChanged += OnPresenterSizeChanged;
			m_epPresenterSizeChangedHandler.Disposable = Disposable.Create(() => spPresenterAsFE.SizeChanged -= OnPresenterSizeChanged);

			m_menuHelper.SetSubMenuPresenter(spPresenter);
		}
	}

	private void ForwardPresenterProperties(
		MenuFlyout pOwnerMenuFlyout,
		MenuFlyoutPresenter pParentMenuFlyoutPresenter,
		MenuFlyoutPresenter pSubMenuFlyoutPresenter)
	{
		var spSubMenuFlyoutPresenter = pSubMenuFlyoutPresenter;
		DependencyObject spThisAsDO = this;

		global::System.Diagnostics.Debug.Assert(pOwnerMenuFlyout != null && pParentMenuFlyoutPresenter != null && pSubMenuFlyoutPresenter != null);

		Control spSubMenuFlyoutPresenterAsControl = spSubMenuFlyoutPresenter;

		// Set the sub presenter style from the MenuFlyout's presenter style
		Style spStyle = pOwnerMenuFlyout.MenuFlyoutPresenterStyle;

		if (spStyle != null)
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
		// Also set the popup's theme. If there is a SystemBackdrop on the menu, it'll be watching the theme on the popup
		// itself rather than the presenter set as the popup's child.
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
#if HAS_UNO_WINUI // This only applies to WinUI
		// Set the popup's SystemBackdrop from the parent flyout's SystemBackdrop. Note that the top-level menu is a
		// MenuFlyout with a SystemBackdrop property, but submenus are MenyFlyoutSubItems with no SystemBackdrop property,
		// so we just use the one set on the top-level menu. SystemBackdrop can handle having multiple parents, and will
		// keep a separate controller/configuration object per place it's used.
		var flyoutSystemBackdrop = ownerMenuFlyout.SystemBackdrop;
		var popupSystemBackdrop = m_tpPopup.SystemBackdrop;
		if (flyoutSystemBackdrop != popupSystemBackdrop)
		{
			m_tpPopup.SystemBackdrop = flyoutSystemBackdrop;
		}
#endif
	}

	// Ensure that any currently open MenuFlyoutSubItems are closed
	private void EnsureCloseExistingSubItems()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: EnsureCloseExistingSubItems.", this));
#endif // MFSI_DEBUG

		MenuFlyoutPresenter spParentPresenter;

		spParentPresenter = GetParentMenuFlyoutPresenter();
		if (spParentPresenter != null)
		{
			IMenuPresenter openedSubPresenter;

			openedSubPresenter = (spParentPresenter as IMenuPresenter).SubPresenter;
			if (openedSubPresenter != null)
			{
				ISubMenuOwner subMenuOwner;

				subMenuOwner = openedSubPresenter.Owner;
				if (subMenuOwner != null && subMenuOwner != this)
				{
					openedSubPresenter.CloseSubMenu();
				}
			}
		}


	}

	internal bool IsOpen => m_tpPopup?.IsOpen ?? false;

	private Control CreateSubPresenter()
	{
		MenuFlyoutPresenter spPresenter = new();

		// Specify the sub MenuFlyoutPresenter
		spPresenter.IsSubPresenter = true;
		(spPresenter as IMenuPresenter).Owner = this;

		return spPresenter;
	}

	private void UpdateParentOwner(MenuFlyoutPresenter parentMenuFlyoutPresenter)
	{
		var parentPresenter = parentMenuFlyoutPresenter;
		if (parentPresenter is null)
		{
			parentPresenter = GetParentMenuFlyoutPresenter();
		}

		if (parentPresenter != null)
		{
			ISubMenuOwner parentSubMenuOwner;
			parentSubMenuOwner = (parentPresenter as IMenuPresenter).Owner;

			if (parentSubMenuOwner != null)
			{
				((ISubMenuOwner)this).ParentOwner = parentSubMenuOwner;
			}
		}
	}

	// Set the popup open or close status for MenuFlyoutSubItem and ensure the
	// focus to the current presenter.
	private void SetIsOpen(bool isOpen)
	{
		bool isOpened = false;

		isOpened = m_tpPopup.IsOpen;

		if (isOpen != isOpened)
		{
			(m_tpPresenter as IMenuPresenter).Owner = isOpen ? this : null;

			MenuFlyoutPresenter parentPresenter;
			parentPresenter = GetParentMenuFlyoutPresenter();

			if (parentPresenter != null)
			{
				(parentPresenter as IMenuPresenter).SubPresenter = isOpen ? m_tpPresenter as MenuFlyoutPresenter : null;

				IMenu owningMenu;
				owningMenu = (parentPresenter as IMenuPresenter).OwningMenu;

				if (owningMenu != null)
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

			// Set the focus to the displayed sub menu presenter when MenuFlyoutSubItem is opened and
			// set the focus back to the original sub item when the displayed sub menu presenter is closed.
			if (isOpen)
			{
				// Set the focus to the displayed sub menu presenter to navigate the each sub items
				this.SetFocusedElement(m_tpPresenter, FocusState.Programmatic, false);
			}
			else
			{
				// Set the focus to the sub menu item
				this.SetFocusedElement(this, FocusState.Programmatic, false);
			}

			UpdateVisualState();
		}
	}

	internal void Open() => m_menuHelper.OpenSubMenu();

	internal void Close() => m_menuHelper.CloseSubMenu();

	private protected override void ChangeVisualState(bool bUseTransitions)
	{
		bool hasToggleMenuItem = false;
		bool hasIconMenuItem = false;
		bool bIsPopupOpened = false;
		bool showSubMenuOpenedState = false;
		MenuFlyoutPresenter spPresenter;

		var bIsEnabled = IsEnabled;
		var focusState = FocusState;
		var shouldBeNarrow = GetShouldBeNarrow();

		spPresenter = GetParentMenuFlyoutPresenter();
		if (spPresenter != null)
		{
			hasToggleMenuItem = spPresenter.GetContainsToggleItems();
			hasIconMenuItem = spPresenter.GetContainsIconItems();
		}

		if (m_tpPopup != null)
		{
			bIsPopupOpened = m_tpPopup.IsOpen;
		}

		var isDelayCloseTimerRunning = m_menuHelper.IsDelayCloseTimerRunning();
		if (bIsPopupOpened && !isDelayCloseTimerRunning)
		{
			showSubMenuOpenedState = true;
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
		else if (m_menuHelper.IsPressed)
		{
			VisualStateManager.GoToState(this, "Pressed", bUseTransitions);
		}
		else if (m_menuHelper.IsPointerOver)
		{
			VisualStateManager.GoToState(this, "PointerOver", bUseTransitions);
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


	}

	// MenuFlyoutSubItem's presenter size changed event handler that
	// adjust the sub presenter position to the proper space area
	// on the available window rect.
	private void OnPresenterSizeChanged(object pSender, SizeChangedEventArgs args)
	{
		if (m_tpMenuPopupThemeTransition == null && ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Animation.PopupThemeTransition"))
		{
			MenuFlyoutPresenter parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

			// Get how many sub menus deep we are. We need this number to know what kind of Z
			// offset to use for displaying elevation. The menus aren't parented in the visual
			// hierarchy so that has to be applied with an additional transform.
			int depth = 1;
			if (parentMenuFlyoutPresenter != null)
			{
				depth = parentMenuFlyoutPresenter.GetDepth() + 1;
			}

			Transition spMenuPopupChildTransition = MenuFlyout.PreparePopupThemeTransitionsAndShadows(m_tpPopup, 0.67 /* closedRatioConstant */, depth);
			((MenuPopupThemeTransition)spMenuPopupChildTransition).Direction = AnimationDirection.Top;
			m_tpMenuPopupThemeTransition = spMenuPopupChildTransition;
		}

		m_menuHelper.OnPresenterSizeChanged(pSender, args, m_tpPopup as Popup);

		// Update the OpenedLength property of the ThemeTransition.
		double openedLength = (m_tpPresenter as Control).ActualHeight;

		if (m_tpMenuPopupThemeTransition is MenuPopupThemeTransition menuTransition)
		{
			menuTransition.OpenedLength = openedLength;
		}
	}

#if false // Never called in WinUI
	private void ClearStateFlags()
	{
		m_menuHelper.ClearStateFlags();
	}
#endif

	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs args)
	{
		m_menuHelper.OnIsEnabledChanged(args);
	}

	protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
	{
		m_menuHelper.OnVisibilityChanged();
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new MenuFlyoutSubItemAutomationPeer(this);

	private protected override string GetPlainText() => Text;

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
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: PrepareSubMenu.", this));
#endif // MFSI_DEBUG

		EnsurePopupAndPresenter();

		global::System.Diagnostics.Debug.Assert(m_tpPopup != null);
		global::System.Diagnostics.Debug.Assert(m_tpPresenter != null);
	}

	void ISubMenuOwner.OpenSubMenu(Point position)
	{
		EnsurePopupAndPresenter();
		EnsureCloseExistingSubItems();

		MenuFlyout parentMenuFlyout = null;
		MenuFlyoutPresenter parentMenuFlyoutPresenter;
		parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

		if (parentMenuFlyoutPresenter != null)
		{
			IMenu owningMenu;
			owningMenu = (parentMenuFlyoutPresenter as IMenuPresenter).OwningMenu;
			(m_tpPresenter as IMenuPresenter).OwningMenu = owningMenu;

			parentMenuFlyout = parentMenuFlyoutPresenter.GetParentMenuFlyout();

			if (parentMenuFlyout != null)
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
			// Note: This is the call that propagates the SystemBackdrop object set on either this FlyoutBase or its
			// MenuFlyoutPresenter to the Popup. A convenient place to do this is in ForwardTargetPropertiesToPresenter, but we
			// see cases where the MenuFlyoutPresenter's SystemBackdrop property is null until it enters the tree via the
			// Popup::Open call above. So trying to propagate it before opening the popup actually finds no SystemBackdrop, and
			// the popup is left with a transparent background. Do the propagation after the popup opens instead. Windowed
			// popups support having a backdrop set after the popup is open.
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

	void ISubMenuOwner.ClosePeerSubMenus()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: ClosePeerSubMenus.", this));
#endif // MFSI_DEBUG

		EnsureCloseExistingSubItems();

	}

	void ISubMenuOwner.CloseSubMenu()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: CloseSubMenu.", this));
#endif // MFSI_DEBUG

		SetIsOpen(false);

	}

	void ISubMenuOwner.CloseSubMenuTree()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: CloseSubMenuTree.", this));
#endif // MFSI_DEBUG

		m_menuHelper.CloseChildSubMenus();

	}

	void ISubMenuOwner.DelayCloseSubMenu()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: DelayCloseSubMenu.", this));
#endif // MFSI_DEBUG

		m_menuHelper.DelayCloseSubMenu();
	}

	void ISubMenuOwner.CancelCloseSubMenu()
	{
#if MFSI_DEBUG
		IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: CancelCloseSubMenu.", this));
#endif // MFSI_DEBUG

		m_menuHelper.CancelCloseSubMenu();
	}

	void ISubMenuOwner.RaiseAutomationPeerExpandCollapse(bool isOpen)
	{
		var isListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);
		if (isListener)
		{
			var spAutomationPeer = GetOrCreateAutomationPeer();
			if (spAutomationPeer is not null)
			{
				(spAutomationPeer as MenuFlyoutSubItemAutomationPeer).RaiseExpandCollapseAutomationEvent(isOpen);
			}
		}
	}

	internal void QueueRefreshItemsSource()
	{
		// The items source might change multiple times in a single tick, so we'll coalesce the refresh
		// into a single event once all of the changes have completed.
		if (m_tpPresenter is not null && !_itemsSourceRefreshPending)
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

					var thisMenuFlyoutSubItem = wrThis.Target as MenuFlyoutSubItem;

					if (thisMenuFlyoutSubItem is not null)
					{
						thisMenuFlyoutSubItem.RefreshItemsSource();
					}
				});

			_itemsSourceRefreshPending = true;
		}
	}

	private void RefreshItemsSource()
	{
		_itemsSourceRefreshPending = false;

		global::System.Diagnostics.Debug.Assert(m_tpPresenter is not null);

		// Setting the items source to null and then back to Items causes the presenter to pick up any changes.
		((ItemsControl)m_tpPresenter).ItemsSource = null;
		((ItemsControl)m_tpPresenter).ItemsSource = m_tpItems;
	}
}
