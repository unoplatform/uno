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
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuFlyout : FlyoutBase, IMenu
	{
		private readonly ObservableVector<MenuFlyoutItemBase> m_tpItems;

		// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
		// UNO TODO private Transition m_tpMenuPopupThemeTransition = null;

		private WeakReference m_wrParentMenu;

		private bool m_openWindowed = true;

		//private bool m_openingWindowedInProgress;

		public MenuFlyout()
		{
			m_isPositionedAtPoint = true;
			InputDeviceTypeUsedToOpen = FocusInputDeviceKind.None;

			m_tpItems = new ObservableVector<MenuFlyoutItemBase>();

			Items = m_tpItems;
		}

		internal FocusInputDeviceKind InputDeviceTypeUsedToOpen { get; set; }

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

		#region Items DependencyProperty

		public IList<MenuFlyoutItemBase> Items
		{
			get => (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty);
			private set => this.SetValue(ItemsProperty, value);
		}

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuFlyoutItemBase>),
				typeof(MenuFlyout),
				new FrameworkPropertyMetadata(defaultValue: null)
			);

		#endregion

		public Style MenuFlyoutPresenterStyle
		{
			get => (Style)this.GetValue(MenuFlyoutPresenterStyleProperty);
			set => SetValue(MenuFlyoutPresenterStyleProperty, value);
		}

		public static DependencyProperty MenuFlyoutPresenterStyleProperty { get; } =
		DependencyProperty.Register(
			"MenuFlyoutPresenterStyle",
			typeof(Style),
			typeof(MenuFlyout),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, (s, e) => (s as MenuFlyout).OnPropertyChanged2(s, e)));

		public void ShowAt(UIElement targetElement, Point point)
		{
			ShowAtCore((FrameworkElement)targetElement, new FlyoutShowOptions { Position = point });
		}

		private void OnPropertyChanged2(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			if (e.Property == MenuFlyoutPresenterStyleProperty &&
				GetPresenter() != null)
			{
				SetPresenterStyle(GetPresenter(), e.NewValue as Style);
			}
		}

		protected override Control CreatePresenter()
		{
			MenuFlyoutPresenter presenter = new MenuFlyoutPresenter();
			Style style;

			presenter.SetParentMenuFlyout(this);

			style = MenuFlyoutPresenterStyle;
			SetPresenterStyle(presenter, style);

			return presenter;
		}

		private protected override void ShowAtCore(FrameworkElement pPlacementTarget, FlyoutShowOptions showOptions)
		{
			m_openWindowed = false;

			base.ShowAtCore(pPlacementTarget, showOptions);

			if (m_openWindowed)
			{
				// UNO TODO
				// SetIsWindowedPopup();
			}
			else
			{
				m_openWindowed = true;
			}

			var placementTarget = pPlacementTarget;
			CacheInputDeviceTypeUsedToOpen(placementTarget);
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

		private protected override void OnClosed()
		{
			base.OnClosed();

			CloseSubMenu();

			AutomationPeer.RaiseEventIfListener(GetPresenter(), AutomationEvents.MenuClosed);

			if (GetPresenter() is MenuFlyoutPresenter presenter)
			{
				presenter.m_iFocusedIndex = -1;
				presenter.ItemsSource = null;
			}
		}

		void CloseSubMenu()
		{
			MenuFlyoutPresenter presenter = (MenuFlyoutPresenter)GetPresenter();

			if (presenter != null)
			{
				IMenuPresenter subPresenter = (presenter as IMenuPresenter).SubPresenter;

				if (subPresenter != null)
				{
					subPresenter.CloseSubMenu();
				}
			}
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
#endif

		protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
		{
			if (m_tpItems != null)
			{
				var itemCount = m_tpItems.Count;
				for (int i = 0; i < itemCount; i++)
				{
					MenuFlyoutItemBase spItem = m_tpItems[i];
					(spItem as MenuFlyoutItemBase).TryInvokeKeyboardAccelerator(args);
				}
			}
		}

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
}
