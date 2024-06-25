// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyout_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

#if HAS_UNO_WINUI
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	/// <summary>
	/// Initializes a new instance of the MenuFlyout class.
	/// </summary>
	public MenuFlyout()
	{
		m_isPositionedAtPoint = true;
		m_inputDeviceTypeUsedToOpen = Uno.UI.Xaml.Input.InputDeviceType.None;

		PrepareState();

#if HAS_UNO // Uno specific: Simulate enter/leave lifecycle events
		ListenToParentLifecycle();
#endif
	}

#if HAS_UNO // TODO: Uno specific - workaround for the lack of support for Enter/Leave on DOs.
	private ParentVisualTreeListener _parentVisualTreeListener;

	private void ListenToParentLifecycle()
	{
		_parentVisualTreeListener = new ParentVisualTreeListener(
			this,
			() => EnterImpl(null, new EnterParams(true)),
			() => LeaveImpl(null, new LeaveParams(true)));
	}
#endif

	// Prepares object's state
	private void PrepareState()
	{
		//base.PrepareState();

		MenuFlyoutItemBaseCollection spItems = new(this);
		m_tpItems = spItems;

		Items = spItems;
	}

	/*
	 _Check_return_ HRESULT MenuFlyout::DisconnectFrameworkPeerCore()
{
    HRESULT hr = S_OK;

    //
    // DXAML Structure
    // ---------------
    //   DXAML::MenuFlyout (this)   -------------->   DXAML::MenuFlyoutItemBase
    //      |                           |
    //      |                           +--------->   DXAML::MenuFlyoutItemBase
    //      V
    //   DXAML::MenuFlyoutItemBaseCollection (m_tpItems)
    //
    // CORE Structure
    // --------------
    //   Core::CControl (this)              < - - +         < - - +
    //            |                               :               :
    //            V                               :               :
    //   Core::CMenuFlyoutItemBaseCollection  - - + (m_pOwner)    :
    //      |                  |                                  :
    //      V                  V                                  :
    //   Core::CControl   Core::CControl      - - - - - - - - - - + (m_pParent)
    //
    // To clear the m_pParent association of the MenuFlyoutItemBase, we have to clear the
    // Core::CMenuFlyoutItemBaseCollection's children, which calls SetParent(NULL) on each of its
    // children. Once this association to MenuFlyout is broken, we can safely destroy MenuFlyout.
    //

    // clear the children in the MenuFlyoutItemBaseCollection
    if (m_tpItems.GetAsCoreDO() != nullptr)
    {
       IFC(CoreImports::Collection_Clear(static_cast<CCollection*>(m_tpItems.GetAsCoreDO())));
       IFC(CoreImports::Collection_SetOwner(static_cast<CCollection*>(m_tpItems.GetAsCoreDO()), nullptr));
    }

    IFC(MenuFlyoutGenerated::DisconnectFrameworkPeerCore());

Cleanup:
    RRETURN(hr);
}
	 */

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == MenuFlyoutPresenterStyleProperty &&
			GetPresenter() is { } presenter)
		{
			SetPresenterStyle(presenter, args.NewValue as Style);
		}
	}

	protected override Control CreatePresenter()
	{
		MenuFlyoutPresenter presenter = new();
		presenter.SetParentMenuFlyout(this);

		var style = MenuFlyoutPresenterStyle;
		SetPresenterStyle(presenter, style);

		return presenter;
	}

	private protected override void ShowAtCore(FrameworkElement pPlacementTarget, FlyoutShowOptions showOptions)
	{
		// TODO Uno: OpenDelayed is not available yet
		// openDelayed = false;

		if (m_openWindowed)
		{
			SetIsWindowedPopup();
		}
		else
		{
			m_openWindowed = true;
		}

		var placementTarget = pPlacementTarget;
		CacheInputDeviceTypeUsedToOpen(placementTarget);

		base.ShowAtCore(pPlacementTarget, showOptions);
	}

	// Raise Opening event.
	private protected override void OnOpening()
	{
		// Update the TemplateSettings as it is about to open.
		var presenter = GetPresenter();
		var menuFlyoutPresenter = presenter as MenuFlyoutPresenter;

		if (menuFlyoutPresenter is not null)
		{
			IMenu parentMenu = (this as IMenu).ParentMenu as IMenu;

			(menuFlyoutPresenter as IMenuPresenter).OwningMenu = parentMenu != null ? parentMenu : this;
			menuFlyoutPresenter.UpdateTemplateSettings();
		}

		base.OnOpening();

		// Reset the presenter's ItemsSource.  Since Items is not an IObservableVector, we don't
		// automatically respond to changes within the vector.  Clearing the property when the presenter
		// unloads and resetting it before we reopen ensures any changes to Items are reflected
		// when the MenuFlyoutPresenter shows.  It also allows sharing of MenuFlyouts; since MenuFlyoutItemBases
		// are UIElements they must be unparented when leaving the tree before they can be inserted elsewhere.
		menuFlyoutPresenter.ItemsSource = m_tpItems;

		AutomationPeer.RaiseEventIfListener(menuFlyoutPresenter, AutomationEvents.MenuOpened);
	}

	private protected override void OnClosing(ref bool cancel)
	{
		base.OnClosing(ref cancel);

		if (!cancel)
		{
			CloseSubMenu();
		}
	}

	private protected override void OnClosed()
	{
		CloseSubMenu();

		AutomationPeer.RaiseEventIfListener(GetPresenter(), AutomationEvents.MenuClosed);

		base.OnClosed();

		if (GetPresenter() is MenuFlyoutPresenter presenter)
		{
			presenter.m_iFocusedIndex = -1;
			presenter.ItemsSource = null;
		}
	}

	private void CloseSubMenu()
	{
		var presenter = (MenuFlyoutPresenter)GetPresenter();

		if (presenter is not null)
		{
			var subPresenter = (presenter as IMenuPresenter).SubPresenter;

			if (subPresenter != null)
			{
				subPresenter.CloseSubMenu();
			}
		}
	}

#if false // TODO Uno: Unused for now
	private void PreparePopupTheme(Popup pPopup, MajorPlacementMode placementMode, FrameworkElement pPlacementTarget)
	{
		// UNO TODO
		//BOOLEAN areOpenCloseAnimationsEnabled = FALSE;
		//IFC_RETURN(get_AreOpenCloseAnimationsEnabled(&areOpenCloseAnimationsEnabled));

		//if (!areOpenCloseAnimationsEnabled)
		//{
		//	return S_OK;
		//}

		//double openedLength = 0;
		//xaml_primitives::AnimationDirection direction = xaml_primitives::AnimationDirection_Bottom;

		//if (!m_tpMenuPopupThemeTransition)
		//{
		//	ctl::ComPtr<xaml_animation::ITransition> spMenuPopupChildTransition;
		//	IFC_RETURN(MenuFlyout::PreparePopupThemeTransitionsAndShadows(pPopup, 0.5 /* closedRatioConstant */, 0 /* depth */, &spMenuPopupChildTransition));
		//	SetPtrValue(m_tpMenuPopupThemeTransition, spMenuPopupChildTransition.Get());
		//}

		//IFC_RETURN(static_cast<Control*>(GetPresenter())->get_ActualHeight(&openedLength));
		//IFC_RETURN(m_tpMenuPopupThemeTransition.Cast<MenuPopupThemeTransition>()->put_OpenedLength(openedLength));
		//direction = (placementMode == MajorPlacementMode::Top) ? xaml_primitives::AnimationDirection_Bottom : xaml_primitives::AnimationDirection_Top;
		//IFC_RETURN(m_tpMenuPopupThemeTransition.Cast<MenuPopupThemeTransition>()->put_Direction(direction));

		//return S_OK;
	}
#endif

	internal static Transition PreparePopupThemeTransitionsAndShadows(Popup popup, double closedRatioConstant, int depth)
	{
		return null;
		// UNO TODO
		//ctl::ComPtr<ITransition> spTransition;
		//ctl::ComPtr<TransitionCollection> spTransitionCollection;
		//ctl::ComPtr<MenuPopupThemeTransition> spMenuPopupChildTransition;

		//*transition = nullptr;

		//IFC_RETURN(ctl::make(&spMenuPopupChildTransition));
		//IFC_RETURN(ctl::make(&spTransitionCollection));

		//IFC_RETURN(spMenuPopupChildTransition->put_ClosedRatio(closedRatioConstant));

		//ctl::ComPtr<xaml::IFrameworkElement> overlayElement;
		//IFC_RETURN(popup->get_OverlayElement(&overlayElement));
		//spMenuPopupChildTransition->SetOverlayElement(overlayElement.Get());

		//IFC_RETURN(spMenuPopupChildTransition.As(&spTransition));
		//IFC_RETURN(spTransitionCollection->Append(spTransition.Get()));

		//// For windowed popups, the transition needs to target the grandchild of the popup.
		//// Otherwise, the transition LTE is going to live in the main window and get clipped.
		//if (static_cast<CPopup*>(popup->GetHandle())->IsWindowed())
		//{
		//	ctl::ComPtr<IUIElement> popupChild;
		//	IFC_RETURN(popup->get_Child(&popupChild));

		//	if (popupChild)
		//	{
		//		int childrenCount;
		//		IFC_RETURN(VisualTreeHelper::GetChildrenCountStatic(static_cast<UIElement*>(popupChild.Get()), &childrenCount));

		//		if (childrenCount == 1)
		//		{
		//			ctl::ComPtr<xaml::IDependencyObject> popupGrandChildAsDO;
		//			IFC_RETURN(VisualTreeHelper::GetChildStatic(static_cast<UIElement*>(popupChild.Get()), 0, &popupGrandChildAsDO));

		//			if (popupGrandChildAsDO)
		//			{
		//				ctl::ComPtr<IUIElement> popupGrandChildAsUE;
		//				IFC_RETURN(popupGrandChildAsDO.As(&popupGrandChildAsUE));
		//				IFC_RETURN(popupGrandChildAsUE->put_Transitions(spTransitionCollection.Get()));
		//				IFC_RETURN(popupGrandChildAsUE->InvalidateMeasure());
		//			}
		//		}
		//	}
		//}
		//else
		//{
		//	IFC_RETURN(popup->put_ChildTransitions(spTransitionCollection.Get()));
		//}

		//*transition = spMenuPopupChildTransition.Detach();
		//return S_OK;
	}

#if false // Unused in WinUI
	private void UpdatePresenterVisualState(MajorPlacementMode placement, bool doForceTransitions)
	{
		//base.UpdatePresenterVisualState(placement);

		// MenuFlyoutPresenter has different visual states depending on the flyout's placement.
		//if (doForceTransitions)
		//{
		//	// In order to play the storyboards of the visual states that belong to the
		//	// MenuFlyoutPresenter, we need to force a visual state transition.
		//	GetPresenter().ResetVisualState();
		//}

		//GetPresenter().UpdateVisualStateForPlacement(placement);
	}

	private void AutoAdjustPlacement(MajorPlacementMode pPlacement)
	{
		// UNO TODO
		// Rect windowRect = default;
		// (DXamlCore.GetCurrent().GetContentBoundsForElement(GetHandle(), &windowRect));
	}

	private void UpdatePresenterVisualState(MajorPlacementMode placement) => UpdatePresenterVisualState(placement, true);
#endif

	//internal override void ShowAt(FrameworkElement placementTarget)
	//{
	//	m_openWindowed = false;
	//	ShowAtCore(placementTarget, new FlyoutShowOptions());
	//}

	/// <summary>
	/// Shows the flyout placed at the specified offset in relation to the specified target element.
	/// </summary>
	/// <param name="targetElement">The element to use as the flyout's placement target.</param>
	/// <param name="point">The point at which to offset the flyout from the specified target element.</param>
	public void ShowAt(UIElement targetElement, Point point)
	{
		var showOptions = new FlyoutShowOptions();
		showOptions.Position = point;

		ShowAt(targetElement, showOptions);
	}

	internal InputDeviceType InputDeviceTypeUsedToOpen => m_inputDeviceTypeUsedToOpen;

	private void CacheInputDeviceTypeUsedToOpen(UIElement pTargetElement)
	{
		ContentRoot contentRoot = VisualTree.GetContentRootForElement(pTargetElement);
		m_inputDeviceTypeUsedToOpen = contentRoot.InputManager.LastInputDeviceType;
	}

#if false
	// Callback for ShowAt() from core layer
	void ShowAtStatic(MenuFlyout pCoreMenuFlyout,
		UIElement pCoreTarget,
	   Point point)
	{
		Debug.Assert(pCoreMenuFlyout != null);
		Debug.Assert(pCoreTarget != null);

		DependencyObject target = pCoreTarget;

		pCoreMenuFlyout.ShowAtImpl(target as FrameworkElement, point);
	}
#endif

	protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
		if (m_tpItems != null)
		{
			for (int i = 0; i < m_tpItems.Count; i++)
			{
				MenuFlyoutItemBase spItem = m_tpItems[i];
				spItem.TryInvokeKeyboardAccelerator(args);
			}
		}
	}

	IMenu IMenu.ParentMenu
	{
		get => m_wrParentMenu?.IsAlive == true ? m_wrParentMenu.Target as IMenu : null;
		set
		{
			m_wrParentMenu = WeakReferencePool.RentWeakReference(this, value);

			// If we have a parent menu, then we want to disable the light-dismiss overlay -
			// in that circumstance, the parent menu will have a light-dismiss overlay that we'll use instead.
			IsLightDismissOverlayEnabled = value is null;

			EnsurePopupAndPresenter();

			var presenter = GetPresenter();
			MenuFlyoutPresenter menuFlyoutPresenter = presenter as MenuFlyoutPresenter;

			bool isSubMenu = value is not null;

			// TODO:MZ: Needed?
			// m_tpPopup.IsSubMenu = isSubMenu;
			if (menuFlyoutPresenter is { })
			{
				menuFlyoutPresenter.IsSubPresenter = isSubMenu;
			}
		}
	}

	void IMenu.Close()
	{
		Hide();
	}

	internal void QueueRefreshItemsSource()
	{
		// The items source might change multiple times in a single tick, so we'll coalesce the refresh
		// into a single event once all of the changes have completed.
		if (GetPresenter() is not null && !m_itemsSourceRefreshPending)
		{
			var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			var wrThis = WeakReferencePool.RentSelfWeakReference(this);

			dispatcherQueue.TryEnqueue(
				() =>
				{
					if (!wrThis.IsAlive)
					{
						return;
					}

					var thisMenuFlyoutSubItem = wrThis.Target as MenuFlyout;

					if (thisMenuFlyoutSubItem is not null)
					{
						thisMenuFlyoutSubItem.RefreshItemsSource();
					}
				});

			m_itemsSourceRefreshPending = true;
		}
	}

	private void RefreshItemsSource()
	{
		m_itemsSourceRefreshPending = false;

		var presenter = GetPresenter();

		global::System.Diagnostics.Debug.Assert(presenter is not null);

		// Setting the items source to null and then back to Items causes the presenter to pick up any changes.
		if (presenter is MenuFlyoutPresenter menuFlyoutPresenter)
		{
			menuFlyoutPresenter.ItemsSource = null;
			menuFlyoutPresenter.ItemsSource = m_tpItems;
		}
	}
}
