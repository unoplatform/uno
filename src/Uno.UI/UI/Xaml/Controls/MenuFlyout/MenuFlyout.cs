#pragma warning disable 649
#pragma warning disable 414 // assigned but its value is never used

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Items))]
public partial class MenuFlyout : FlyoutBase, IMenu
{
	private readonly MenuFlyoutItemBaseCollection m_tpItems;

	// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
	// UNO TODO private Transition m_tpMenuPopupThemeTransition = null;

	private WeakReference m_wrParentMenu;

	private bool m_openWindowed = false; // TODO Uno specific: Always false in Uno for now.

	//private bool m_openingWindowedInProgress;

	public MenuFlyout()
	{
		m_isPositionedAtPoint = true;
		m_inputDeviceTypeUsedToOpen = FocusInputDeviceKind.None;

		PrepareState();
	}

	// Prepares object's state
	private void PrepareState()
	{
		base.PrepareState();

		MenuFlyoutItemBaseCollection spItems = new();
		CoreImports.Collection_SetOwner(spItems, this);
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

	internal void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		if (e.Property == MenuFlyoutPresenterStyleProperty &&
			GetPresenter() != null)
		{
			SetPresenterStyle(GetPresenter(), e.NewValue as Style);
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
		openDelayed = false;
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
		Control presenter = GetPresenter();
		MenuFlyoutPresenter menuFlyoutPresenter = presenter as MenuFlyoutPresenter;

		if (menuFlyoutPresenter is not null)
		{
			IMenu parentMenu = (this as IMenu).ParentMenu as IMenu;

			(menuFlyoutPresenter as IMenuPresenter).OwningMenu = parentMenu != null ? parentMenu : this;
			menuFlyoutPresenter.UpdateTemplateSettings();
		}

		base.OnOpening();

		if (menuFlyoutPresenter is not null)
		{
			// Reset the presenter's ItemsSource.  Since Items is not an IObservableVector, we don't
			// automatically respond to changes within the vector.  Clearing the property when the presenter
			// unloads and resetting it before we reopen ensures any changes to Items are reflected
			// when the MenuFlyoutPresenter shows.  It also allows sharing of MenuFlyouts; since MenuFlyoutItemBases
			// are UIElements they must be unparented when leaving the tree before they can be inserted elsewhere.
			menuFlyoutPresenter.ItemsSource = m_tpItems;

			AutomationPeer.RaiseEventIfListener(menuFlyoutPresenter, AutomationEvents.MenuOpened);
		}
	}

	internal override void OnClosing(ref bool cancel)
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

	internal FocusInputDeviceKind InputDeviceTypeUsedToOpen => m_inputDeviceTypeUsedToOpen;

	internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnDataContextChanged(e);

		SetFlyoutItemsDataContext();
	}

	private void SetFlyoutItemsDataContext()
	{
		// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout.
		Items?.ForEach(item => item?.SetValue(
			UIElement.DataContextProperty,
			this.DataContext,
			precedence: DependencyPropertyValuePrecedences.Inheritance
		));
	}

	
	public void ShowAt(UIElement targetElement, Point point)
	{
		ShowAtCore((FrameworkElement)targetElement, new FlyoutShowOptions { Position = point });
	}


	


	

	

#if false
	void PreparePopupTheme(
		Popup pPopup,
		MajorPlacementMode placementMode,
		FrameworkElement pPlacementTarget)
	{
		bool areOpenCloseAnimationsEnabled = false;
		areOpenCloseAnimationsEnabled = AreOpenCloseAnimationsEnabled;

		if (!areOpenCloseAnimationsEnabled)
		{
			return;
		}

		// On Threshold, we use the MenuPopupThemeTransition for MenuFlyouts.
		double openedLength = 0;

		// UNO TODO
		//if (m_tpMenuPopupThemeTransition == null)
		//{
		//	Transition spMenuPopupChildTransition;
		//	spMenuPopupChildTransition = PreparePopupThemeTransitionsAndShadows(pPopup, 0.5 /* closedRatioConstant */, 0 /* depth */);
		//	m_tpMenuPopupThemeTransition = spMenuPopupChildTransition;
		//}

		openedLength = ((Control)GetPresenter()).ActualHeight;

		// UNO TODO
		// (m_tpMenuPopupThemeTransition as MenuPopupThemeTransition).OpenedLength = openedLength;
		var direction = (placementMode == MajorPlacementMode.Top) ? AnimationDirection.Bottom : AnimationDirection.Top;

		// UNO TODO
		// (m_tpMenuPopupThemeTransition as MenuPopupThemeTransition).Direction = direction);
	}

	///*static*/
	//Transition PreparePopupThemeTransitionsAndShadows(
	//	 Popup popup,
	//	double closedRatioConstant,
	//	uint depth)
	//{
	//	Transition spTransition;
	//	TransitionCollection spTransitionCollection;
	//	MenuPopupThemeTransition spMenuPopupChildTransition;

	//	*transition = null;

	//	(ctl.make(&spMenuPopupChildTransition));
	//	(ctl.make(&spTransitionCollection));

	//	spMenuPopupChildTransition.ClosedRatio = closedRatioConstant;

	//	FrameworkElement overlayElement;
	//	overlayElement = popup.OverlayElement;
	//	spMenuPopupChildTransition.SetOverlayElement(overlayElement);

	//	spTransition = spMenuPopupChildTransition;
	//	spTransitionCollection.Append(spTransition));

	//	// UNO TODO Windowed Popups are not supported
	//	//
	//	// For windowed popups, the transition needs to target the grandchild of the popup.
	//	// Otherwise, the transition LTE is going to live in the main window and get clipped.
	//	//if ((CPopup*)(popup.GetHandle()).IsWindowed())
	//	//{
	//	//	UIElement popupChild;
	//	//	popupChild = popup.Child;

	//	//	if (popupChild)
	//	//	{
	//	//		int childrenCount;
	//	//		(VisualTreeHelper.GetChildrenCountStatic((UIElement*)(popupChild), &childrenCount));

	//	//		if (childrenCount == 1)
	//	//		{
	//	//			xaml.IDependencyObject popupGrandChildAsDO;
	//	//			(VisualTreeHelper.GetChildStatic((UIElement*)(popupChild), 0, &popupGrandChildAsDO));

	//	//			if (popupGrandChildAsDO)
	//	//			{
	//	//				UIElement popupGrandChildAsUE;
	//	//				(popupGrandChildAsDO.As(&popupGrandChildAsUE));
	//	//				(popupGrandChildAsUE.Transitions = spTransitionCollection);
	//	//				(popupGrandChildAsUE.InvalidateMeasure());
	//	//			}
	//	//		}
	//	//	}
	//	//}
	//	//else
	//	{
	//		popup.ChildTransitions = spTransitionCollection;
	//	}
	//}

	void AutoAdjustPlacement(MajorPlacementMode pPlacement)
	{
		// UNO TODO
		// Rect windowRect = default;
		// (DXamlCore.GetCurrent().GetContentBoundsForElement(GetHandle(), &windowRect));
	}

	void ShowAtImpl(UIElement pTargetElement, Point targetPoint)
	{
		var targetElement = pTargetElement;

		var showOptions = new FlyoutShowOptions();
		showOptions.Position = targetPoint;

		try
		{
			m_openingWindowedInProgress = true;

			ShowAt(targetElement, showOptions);
		}
		finally
		{
			m_openingWindowedInProgress = false;
		}
	}
#endif

	void CacheInputDeviceTypeUsedToOpen(UIElement pTargetElement)
	{
		// UNO TODO
		//CContentRoot* contentRoot = VisualTree.GetContentRootForElement(pTargetElement);
		//InputDeviceTypeUsedToOpen = contentRoot.GetInputManager().GetLastInputDeviceType();
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

	void OnProcessKeyboardAcceleratorsImpl(ProcessKeyboardAcceleratorEventArgs pArgs)
	{
		if (m_tpItems != null)
		{
			var itemCount = m_tpItems.Count;
			for (int i = 0; i < itemCount; i++)
			{
				MenuFlyoutItemBase spItem = m_tpItems[i];
				(spItem as MenuFlyoutItemBase).TryInvokeKeyboardAccelerator(pArgs);
			}
		}
	}
#endif

	IMenu IMenu.ParentMenu
	{
		get => m_wrParentMenu?.Target as IMenu;
		set
		{
			m_wrParentMenu = new WeakReference(value);

			// If we have a parent menu, then we want to disable the light-dismiss overlay -
			// in that circumstance, the parent menu will have a light-dismiss overlay that we'll use instead.
			IsLightDismissOverlayEnabled = value == null;

			Control presenter = GetPresenter();
			MenuFlyoutPresenter menuFlyoutPresenter = presenter as MenuFlyoutPresenter;

			if (menuFlyoutPresenter is { })
			{
				menuFlyoutPresenter.IsSubPresenter = value != null;
			}
		}
	}

	void IMenu.Close()
	{
		Hide();
	}

#if false
	bool IsWindowedPopup()
	{
		return false;

		// UNO TODO
		// return CPopup.DoesPlatformSupportWindowedPopup(DXamlCore.GetCurrent().GetHandle()) && (FlyoutBase.IsWindowedPopup() || m_openingWindowedInProgress);
	}
#endif
}
