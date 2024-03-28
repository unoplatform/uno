using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuFlyoutSubItem : MenuFlyoutItemBase, ISubMenuOwner
	{
		// Popup for the MenuFlyoutSubItem
		Popup m_tpPopup;

		// Presenter for the MenuFlyoutSubItem
		Control m_tpPresenter;

		// In Threshold, MenuFlyout uses the MenuPopupThemeTransition.
		// UNO TODO Transition m_tpMenuPopupThemeTransition = null;

		// Event pointer for the Loaded event
		// IDisposable m_epLoadedHandler;

		// Event pointer for the size changed on the MenuFlyoutSubItem's presenter
		IDisposable m_epPresenterSizeChangedHandler;

		// Helper to which to delegate cascading menu functionality.
		CascadingMenuHelper m_menuHelper;

		// Weak reference the parent that owns the menu that this item belongs to.
		private WeakReference m_wrParentOwner;

		DependencyObjectCollection<MenuFlyoutItemBase> m_tpItems;

		public string Text
		{
			get => (string)this.GetValue(TextProperty) ?? "";
			set => SetValue(TextProperty, value);
		}

		public IList<MenuFlyoutItemBase> Items => m_tpItems;

		public IconElement Icon
		{
			get => (IconElement)this.GetValue(IconProperty);
			set => this.SetValue(IconProperty, value);
		}

		public static Windows.UI.Xaml.DependencyProperty TextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(MenuFlyoutSubItem),
			new FrameworkPropertyMetadata(default(string)));

		public static Windows.UI.Xaml.DependencyProperty IconProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon",
			typeof(IconElement),
			typeof(MenuFlyoutSubItem),
			new FrameworkPropertyMetadata(default(IconElement)));

		public MenuFlyoutSubItem()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: ", this));
#endif // MFSI_DEBUG

			PrepareState();

			DefaultStyleKey = typeof(MenuFlyoutSubItem);
		}

		void PrepareState()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: PrepareState.", this));
#endif // MFSI_DEBUG

			// Create the sub menu items collection and set the owner
			m_tpItems = new DependencyObjectCollection<MenuFlyoutItemBase>(this);

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

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnPointerEntered.", this));
#endif // MFSI_DEBUG

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

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnPointerPressed.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnPointerPressed(args);
		}

		// PointerReleased event handler that shows MenuFlyoutSubItem in
		// case of touch input.

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnPointerReleased.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnPointerReleased(args);

		}


		protected override void OnGotFocus(RoutedEventArgs args)
		{
			base.OnGotFocus(args);

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnGotFocus.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnGotFocus(args);
		}


		protected override void OnLostFocus(RoutedEventArgs args)
		{
			base.OnLostFocus(args);

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnLostFocus.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnLostFocus(args);
		}

		// KeyDown event handler that handles the keyboard navigation between
		// the menu items and shows the MenuFlyoutSubItem in case of hitting
		// the enter or right arrow key.

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnKeyDown.", this));
#endif // MFSI_DEBUG

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

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnKeyUp.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnKeyUp(args);
		}

		// Ensure the creating the popup and menu presenter to show the
		void EnsurePopupAndPresenter()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: EnsurePopupAndPresenter.", this));
#endif // MFSI_DEBUG

			if (m_tpPopup == null)
			{
				MenuFlyoutPresenter spParentMenuFlyoutPresenter = null;
				Control spPresenter;
				UIElement spPresenterAsUI;
				FrameworkElement spPresenterAsFE;
				Popup spPopup;

				spPopup = new Popup();
				spPopup.IsSubMenu = true;
				spPopup.IsLightDismissEnabled = false;

				spParentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();
#if false // UNO TODO Windowed Popup is not available
				if (spParentMenuFlyoutPresenter != null)
				{
					spParentMenuFlyout = spParentMenuFlyoutPresenter.GetParentMenuFlyout();
					// Set the windowed Popup if the MenuFlyout is set the windowed Popup
					if (spParentMenuFlyout && spParentMenuFlyout.IsWindowedPopup())
					{
						ASSERT((CPopup*)(spPopup as Popup.GetHandle()).DoesPlatformSupportWindowedPopup(DXamlCore.GetCurrent().GetHandle()));

						((CPopup*)(spPopup as Popup.GetHandle()).SetIsWindowed());

						// Ensure the sub menu is the windowed Popup
						ASSERT((CPopup*)(spPopup as Popup.GetHandle()).IsWindowed());

						xaml.IXamlRoot xamlRoot = XamlRoot.GetForElementStatic(spParentMenuFlyoutPresenter);
						if (xamlRoot)
						{
							(spPopup as Popup.XamlRoot = xamlRoot);
						}
					}
				}
#endif

				spPresenter = CreateSubPresenter();
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
				m_epPresenterSizeChangedHandler = Disposable.Create(() => spPresenterAsFE.SizeChanged -= OnPresenterSizeChanged);

				m_menuHelper.SetSubMenuPresenter(spPresenter as Control);
			}
		}

		void ForwardPresenterProperties(
			MenuFlyout pOwnerMenuFlyout,
			MenuFlyoutPresenter pParentMenuFlyoutPresenter,
			MenuFlyoutPresenter pSubMenuFlyoutPresenter)
		{
			Style spStyle;
			ElementTheme parentPresenterTheme;
			object spDataContext;
			FrameworkElement spPopupAsFE;
			MenuFlyoutPresenter spSubMenuFlyoutPresenter = pSubMenuFlyoutPresenter;
			Control spSubMenuFlyoutPresenterAsControl;
			DependencyObject spThisAsDO = this;

			global::System.Diagnostics.Debug.Assert(pOwnerMenuFlyout != null && pParentMenuFlyoutPresenter != null && pSubMenuFlyoutPresenter != null);

			spSubMenuFlyoutPresenterAsControl = spSubMenuFlyoutPresenter;

			// Set the sub presenter style from the MenuFlyout's presenter style
			spStyle = pOwnerMenuFlyout.MenuFlyoutPresenterStyle;

			if (spStyle != null)
			{
				((Control)pSubMenuFlyoutPresenter).Style = spStyle;
			}
			else
			{
				((Control)pSubMenuFlyoutPresenter).ClearValue(FrameworkElement.StyleProperty);
			}

			// Set the sub presenter's RequestTheme from the parent presenter's RequestTheme
			parentPresenterTheme = pParentMenuFlyoutPresenter.RequestedTheme;
			pSubMenuFlyoutPresenter.RequestedTheme = parentPresenterTheme;

			// Set the sub presenter's DataContext from the parent presenter's DataContext
			spDataContext = pParentMenuFlyoutPresenter.DataContext;
			pSubMenuFlyoutPresenter.DataContext = spDataContext;

			// Set the sub presenter's FlowDirection from the current sub menu item's FlowDirection
			var flowDirection = FlowDirection;
			pSubMenuFlyoutPresenter.FlowDirection = flowDirection;

			// Set the popup's FlowDirection from the current FlowDirection
			spPopupAsFE = m_tpPopup;
			spPopupAsFE.FlowDirection = flowDirection;

			// Set the sub presenter's Language from the parent presenter's Language
			pSubMenuFlyoutPresenter.Language = pParentMenuFlyoutPresenter.Language;

			// Set the sub presenter's IsTextScaleFactorEnabledInternal from the parent presenter's IsTextScaleFactorEnabledInternal
			var isTextScaleFactorEnabled = pParentMenuFlyoutPresenter.IsTextScaleFactorEnabledInternal;
			pSubMenuFlyoutPresenter.IsTextScaleFactorEnabledInternal = isTextScaleFactorEnabled;

			ElementSoundMode soundMode = ElementSoundPlayerService.Instance.GetEffectiveSoundMode(spThisAsDO as DependencyObject);

			(spSubMenuFlyoutPresenterAsControl as Control).ElementSoundMode = soundMode;
		}

		// Ensure that any currently open MenuFlyoutSubItems are closed
		void EnsureCloseExistingSubItems()
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

		bool IsOpen => m_tpPopup?.IsOpen ?? false;

		Control
		CreateSubPresenter()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: CreateSubPresenter.", this));
#endif // MFSI_DEBUG

			MenuFlyoutPresenter spPresenter = new MenuFlyoutPresenter();

			// Specify the sub MenuFlyoutPresenter
			(spPresenter as MenuFlyoutPresenter).IsSubPresenter = true;
			(spPresenter as IMenuPresenter).Owner = this;

			return spPresenter;
		}

		void UpdateParentOwner(MenuFlyoutPresenter parentMenuFlyoutPresenter)
		{
			MenuFlyoutPresenter parentPresenter = parentMenuFlyoutPresenter;
			if (parentPresenter == null)
			{
				parentPresenter = GetParentMenuFlyoutPresenter();
			}
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: UpdateParentOwner - parentPresenter=0x%p.", this, parentPresenter));
#endif // MFSI_DEBUG

			if (parentPresenter != null)
			{
				ISubMenuOwner parentSubMenuOwner;
				parentSubMenuOwner = (parentPresenter as IMenuPresenter).Owner;

#if MFSI_DEBUG
				IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: UpdateParentOwner - parentSubMenuOwner=0x%p.", this, parentSubMenuOwner));
#endif // MFSI_DEBUG

				if (parentSubMenuOwner != null)
				{
					((ISubMenuOwner)this).ParentOwner = parentSubMenuOwner;
				}
			}
		}

		// Set the popup open or close status for MenuFlyoutSubItem and ensure the
		// focus to the current presenter.
		void SetIsOpen(bool isOpen)
		{
			bool isOpened = false;

			isOpened = m_tpPopup.IsOpen;

#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: SetIsOpen isOpen=%d, isOpened=%d.", this, isOpen, isOpened));
#endif // MFSI_DEBUG

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

				// UNO TODO
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
					m_tpPresenter.Focus(FocusState.Programmatic);

					// UNO TODO
					// (DependencyObject.SetFocusedElement(
					// 	spPresenterAsDO as DependencyObject,
					// 	xaml.FocusState_Programmatic,
					// 	false /*animateIfBringIntoView*/,
					// 	&focusUpdated));
				}
				else
				{
					// Set the focus to the sub menu item
					this.Focus(FocusState.Programmatic);

					// UNO TODO
					//(DependencyObject.SetFocusedElement(
					//	spThisAsDO as DependencyObject,
					//	xaml.FocusState_Programmatic,
					//	false /*animateIfBringIntoView*/,
					//	&focusUpdated));
				}

				UpdateVisualState();
			}


		}

		internal void Open()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: Open.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OpenSubMenu();
		}

		internal void Close()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: Close.", this));
#endif // MFSI_DEBUG

			m_menuHelper.CloseSubMenu();

		}

		private protected override void ChangeVisualState(bool bUseTransitions)
		{
			bool hasToggleMenuItem = false;
			bool hasIconMenuItem = false;
			bool bIsPopupOpened = false;
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

			// CommonStates
			if (!bIsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
			}
			else if (bIsPopupOpened)
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
		void OnPresenterSizeChanged(
		object pSender,
		SizeChangedEventArgs args)
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnPresenterSizeChanged.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnPresenterSizeChanged(pSender, args, m_tpPopup as Popup);

#if false // UNO TODO
			if (m_tpMenuPopupThemeTransition == null)
			{

				MenuFlyoutPresenter parentMenuFlyoutPresenter;
				parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

				// Get how many sub menus deep we are. We need this number to know what kind of Z
				// offset to use for displaying elevation. The menus aren't parented in the visual
				// hierarchy so that has to be applied with an additional transform.
				int depth = 1;
				if (parentMenuFlyoutPresenter != null)
				{
					depth = parentMenuFlyoutPresenter.GetDepth() + 1;
				}

				Transition spMenuPopupChildTransition;
				(MenuFlyout.PreparePopupThemeTransitionsAndShadows((Popup*)(m_tpPopup), 0.67 /* closedRatioConstant */, depth, &spMenuPopupChildTransition));
				(spMenuPopupChildTransition as MenuPopupThemeTransition.Direction = xaml_primitives.AnimationDirection_Top);
				m_tpMenuPopupThemeTransition = spMenuPopupChildTransition;
			}

			// Update the OpenedLength property of the ThemeTransition.
			double openedLength = (m_tpPresenter as Control).ActualHeight;

			(m_tpMenuPopupThemeTransition as MenuPopupThemeTransition).OpenedLength = openedLength;
#endif
		}

#if false
		void ClearStateFlags()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: ClearStateFlags.", this));
#endif // MFSI_DEBUG

			m_menuHelper.ClearStateFlags();

		}

		void OnIsEnabledChanged(/*IsEnabledChangedEventArgs* args*/)
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnIsEnabledChanged.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnIsEnabledChanged();
		}

		void OnVisibilityChanged()
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OnVisibilityChanged.", this));
#endif // MFSI_DEBUG

			m_menuHelper.OnVisibilityChanged();

		}
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			//*ppAutomationPeer = null;
			//MenuFlyoutSubItemAutomationPeer spAutomationPeer;
			//(ActivationAPI.ActivateAutomationInstance(KnownTypeIndex.MenuFlyoutSubItemAutomationPeer, GetHandle(), spAutomationPeer.GetAddressOf()));
			//(spAutomationPeer.Owner = this);
			//*ppAutomationPeer = spAutomationPeer.Detach();

			return null;
		}

		private protected override string GetPlainText() => Text;

		bool ISubMenuOwner.IsSubMenuOpen => IsOpen;

		ISubMenuOwner ISubMenuOwner.ParentOwner
		{
			get => m_wrParentOwner?.Target as ISubMenuOwner;
			set => m_wrParentOwner = new WeakReference(value);
		}

		bool ISubMenuOwner.IsSubMenuPositionedAbsolutely => true;

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
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: OpenSubMenu.", this));
#endif // MFSI_DEBUG

			EnsurePopupAndPresenter();
			EnsureCloseExistingSubItems();

			MenuFlyoutPresenter parentMenuFlyoutPresenter;
			parentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();

			if (parentMenuFlyoutPresenter != null)
			{
				IMenu owningMenu;
				owningMenu = (parentMenuFlyoutPresenter as IMenuPresenter).OwningMenu;
				(m_tpPresenter as IMenuPresenter).OwningMenu = owningMenu;

				MenuFlyout parentMenuFlyout;
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


		}

		void ISubMenuOwner.PositionSubMenu(Point position)
		{
#if MFSI_DEBUG
			IGNOREHR(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "MFSI[0x%p]: PositionSubMenu - (%f, %f).", this, position.X, position.Y));
#endif // MFSI_DEBUG

			if (position.X != float.NegativeInfinity)
			{
				m_tpPopup.HorizontalOffset = position.X;
			}

			if (position.Y != float.NegativeInfinity)
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
			// UNO TODO
			//AutomationPeer spAutomationPeer;
			//bool isListener = false;

			//AutomationPeer.ListenerExistsHelper(xaml_automation_peers.AutomationEvents_PropertyChanged, &isListener);
			//if (isListener)
			//{
			//	(GetOrCreateAutomationPeer(&spAutomationPeer));
			//	if (spAutomationPeer)
			//	{
			//		(spAutomationPeer as MenuFlyoutSubItemAutomationPeer.RaiseExpandCollapseAutomationEvent(isOpen));
			//	}
			//}
		}
	}
}
