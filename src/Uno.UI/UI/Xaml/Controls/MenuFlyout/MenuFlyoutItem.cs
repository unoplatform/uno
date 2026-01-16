using System;
using Uno.Client;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using ICommand = System.Windows.Input.ICommand;
using Microsoft.UI.Xaml.Markup;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Text))]
	public partial class MenuFlyoutItem : MenuFlyoutItemBase
	{
		// Whether the pointer is currently over the MenuFlyoutItem
		bool m_bIsPointerOver = true;

		// Whether the pointer is currently pressed over the MenuFlyoutItem
		internal bool m_bIsPressed = true;

		// Whether the pointer's left button is currently down.
		internal bool m_bIsPointerLeftButtonDown = true;

		// True if the SPACE or ENTER key is currently pressed, false otherwise.
		internal bool m_bIsSpaceOrEnterKeyDown = true;

		// True if the NAVIGATION_ACCEPT or GAMEPAD_A vkey is currently pressed, false otherwise.
		internal bool m_bIsNavigationAcceptOrGamepadAKeyDown = true;

		// On pointer released we perform some actions depending on control. We decide to whether to perform them
		// depending on some parameters including but not limited to whether released is followed by a pressed, which
		// mouse button is pressed, what type of pointer is it etc. This bool keeps our decision.
		bool m_shouldPerformActions = true;

		// Event pointer for the ICommand.CanExecuteChanged event.
		IDisposable m_epCanExecuteChangedHandler;

		// UNO TODO
		// Event pointer for the Loaded event.
		// IDisposable m_epLoadedHandler;

		// UNO TODO
		// IDisposable m_epMenuFlyoutItemClickEventCallback;

		double m_maxKeyboardAcceleratorTextWidth;
		TextBlock m_tpKeyboardAcceleratorTextBlock;

		bool m_isTemplateApplied;

		#region CommandParameter

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public static DependencyProperty CommandParameterProperty { get; } =
			DependencyProperty.Register(
				"CommandParameter", typeof(object),
				typeof(Controls.MenuFlyoutItem),
				new FrameworkPropertyMetadata(default(object)));

		#endregion

		#region Command

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public static DependencyProperty CommandProperty { get; } =
			DependencyProperty.Register(
				name: nameof(Command),
				propertyType: typeof(ICommand),
				ownerType: typeof(MenuFlyoutItem),
				typeMetadata: new FrameworkPropertyMetadata(default(ICommand)));

		#endregion

		#region Text

		public string Text
		{
			get { return (string)GetValue(TextProperty) ?? ""; }
			set { SetValue(TextProperty, value); }
		}

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				name: nameof(Text),
				propertyType: typeof(string),
				ownerType: typeof(MenuFlyoutItem),
				typeMetadata: new FrameworkPropertyMetadata(default(string)));

		#endregion

		public IconElement Icon
		{
			get => (IconElement)this.GetValue(IconProperty);
			set => this.SetValue(IconProperty, value);
		}

		public static DependencyProperty IconProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Icon),
			propertyType: typeof(IconElement),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(IconElement)));

		public string KeyboardAcceleratorTextOverride
		{
			get => (string)this.GetValue(KeyboardAcceleratorTextOverrideProperty) ?? "";
			set => this.SetValue(KeyboardAcceleratorTextOverrideProperty, value);
		}

		public static DependencyProperty KeyboardAcceleratorTextOverrideProperty { get; } =
		DependencyProperty.Register(
			name: nameof(KeyboardAcceleratorTextOverride),
			propertyType: typeof(string),
			ownerType: typeof(MenuFlyoutItem),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

		public MenuFlyoutItemTemplateSettings TemplateSettings { get; internal set; }

#pragma warning disable CS0108
		public event RoutedEventHandler Click;
#pragma warning restore CS0108

		internal void InvokeClick()
		{
			Click?.Invoke(this, new RoutedEventArgs(this));
			Command.ExecuteIfPossible(this.CommandParameter);
		}

		public MenuFlyoutItem()
		{
			m_bIsPointerOver = false;
			m_bIsPressed = false;
			m_bIsPointerLeftButtonDown = false;
			m_bIsSpaceOrEnterKeyDown = false;
			m_bIsNavigationAcceptOrGamepadAKeyDown = false;
			m_shouldPerformActions = false;

			DefaultStyleKey = typeof(MenuFlyoutItem);

			Initialize();
		}

		// Prepares object's state
		void Initialize()
		{
			Loaded += (s, e) => ClearStateFlags();
		}

		// Apply a template to the

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			TextBlock keyboardAcceleratorTextBlock = this.GetTemplateChild("KeyboardAcceleratorTextBlock") as TextBlock;
			m_tpKeyboardAcceleratorTextBlock = keyboardAcceleratorTextBlock;

			SuppressIsEnabled(false);
			UpdateCanExecute();

			// Sync the logical and visual states of the control
			UpdateVisualState();
		}

		// PointerPressed event handler.
		protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerPressed(pArgs);
			var handled = pArgs.Handled;
			if (!handled)
			{
				PointerPoint spPointerPoint;
				PointerPointProperties spPointerProperties;
				spPointerPoint = pArgs.GetCurrentPoint(this);
				spPointerProperties = spPointerPoint.Properties;
				var bIsLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;
				if (bIsLeftButtonPressed)
				{
					m_bIsPointerLeftButtonDown = true;
					m_bIsPressed = true;

					pArgs.Handled = true;
					UpdateVisualState();
				}
			}
		}

		// PointerReleased event handler.
		protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerReleased(pArgs);

			bool handled = false;

			handled = pArgs.Handled;
			if (!handled)
			{
				m_bIsPointerLeftButtonDown = false;

				m_shouldPerformActions = m_bIsPressed && !m_bIsSpaceOrEnterKeyDown && !m_bIsNavigationAcceptOrGamepadAKeyDown;

#if false
				// UNO TODO
				if (m_shouldPerformActions)
				{
					GestureModes gestureFollowing = GestureModes.None;

					m_bIsPressed = false;

					gestureFollowing = ((PointerRoutedEventArgs*)(pArgs).GestureFollowing);

					// Note that we are intentionally NOT handling the args
					// if we do not fall through here because basically we are no_opting in that case.
					if (gestureFollowing != GestureModes.RightTapped)
					{
						pArgs.Handled = true;
						PerformPointerUpAction();
					}
				}
#else
				PerformPointerUpAction();
#endif
			}
		}

		private protected override void OnRightTappedUnhandled(RightTappedRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;
			if (!handled)
			{
				PerformPointerUpAction();
				pArgs.Handled = true;
			}
		}

		// Contains the logic to be employed if we decide to handle pointer released.
		void PerformPointerUpAction()
		{
			if (m_shouldPerformActions)
			{
				Focus(FocusState.Pointer);
				Invoke();
			}
		}

		// Performs appropriate actions upon a mouse/keyboard invocation of a
		internal virtual void Invoke()
		{
			RoutedEventArgs spArgs;
			MenuFlyoutPresenter spParentMenuFlyoutPresenter;

			// Create the args
			spArgs = new RoutedEventArgs();
			spArgs.OriginalSource = this;

			// Raise the event
			Click?.Invoke(this, spArgs);

			// UNO TODO
			//AutomationPeer.RaiseEventIfListener(this, xaml_automation_peers.AutomationEvents_InvokePatternOnInvoked);

			// Execute command associated with the button
			ExecuteCommand();

			bool shouldPreventDismissOnPointer = false; // UNO TODO PreventDismissOnPointer;
			if (!shouldPreventDismissOnPointer)
			{
				// Close the MenuFlyout.
				spParentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();
				if (spParentMenuFlyoutPresenter != null)
				{
					IMenu owningMenu;

					owningMenu = (spParentMenuFlyoutPresenter as IMenuPresenter).OwningMenu;
					if (owningMenu != null)
					{
						// We need to make close all menu flyout sub items before we hide the parent menu flyout,
						// otherwise selecting an MenuFlyoutItem in a sub menu will hide the parent menu a
						// significant amount of time before closing the sub menu.
						(spParentMenuFlyoutPresenter as IMenuPresenter).CloseSubMenu();
						owningMenu.Close();
					}
				}
			}

			ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);
		}

		// void AddProofingItemHandlerStatic(DependencyObject pMenuFlyoutItem,  INTERNAL_EVENT_HANDLER eventHandler)
		//{
		//	DependencyObject peer = pMenuFlyoutItem;

		//	if (peer == null)
		//	{
		//		return;
		//	}

		//	MenuFlyoutItem peerAsMenuFlyoutItem;
		//	(peer.As(&peerAsMenuFlyoutItem));
		//	IFCPTR_RETURN(peerAsMenuFlyoutItem);

		//	(peerAsAddProofingItemHandler(eventHandler));

		//	return S_OK;
		//}

		// void AddProofingItemHandler( INTERNAL_EVENT_HANDLER eventHandler)
		//{
		//	(m_epMenuFlyoutItemClickEventCallback.AttachEventHandler(this, [eventHandler](DependencyObject pSender, DependencyObject pArgs)
		//	{
		//		DependencyObject spSender;
		//		EventArgs spArgs;

		//		IFCPTR_RETURN(pSender);
		//		IFCPTR_RETURN(pArgs);

		//		(ctl.do_query_interface(spSender, pSender));
		//		(ctl.do_query_interface(spArgs, pArgs));

		//		eventHandler(spSender.GetHandle(), spArgs.GetCorePeer());

		//		return S_OK;
		//	}));

		//	return S_OK;
		//}

		// Executes Command if CanExecute() returns true.
		void ExecuteCommand()
		{
			ICommand spCommand = Command;

			if (spCommand != null)
			{
				object spCommandParameter;
				bool canExecute;

				spCommandParameter = CommandParameter;
				canExecute = spCommand.CanExecute(spCommandParameter);
				if (canExecute)
				{
					spCommand.Execute(spCommandParameter);
				}
			}
		}

		// PointerEnter event handler.
		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerEntered(pArgs);

			MenuFlyoutPresenter spParentPresenter;

			m_bIsPointerOver = true;

			spParentPresenter = GetParentMenuFlyoutPresenter();
			if (spParentPresenter != null)
			{
				IMenuPresenter subPresenter;

				subPresenter = (spParentPresenter as IMenuPresenter).SubPresenter;
				if (subPresenter != null)
				{
					ISubMenuOwner subPresenterOwner;

					subPresenterOwner = subPresenter.Owner;

					if (subPresenterOwner != null)
					{
						subPresenterOwner.DelayCloseSubMenu();
					}
				}
			}

			UpdateVisualState();
		}

		// PointerExited event handler.
		protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerExited(pArgs);

			// MenuFlyoutItem does not capture pointer, so PointerExit means the item is no longer pressed.
			m_bIsPressed = false;
			m_bIsPointerLeftButtonDown = false;
			m_bIsPointerOver = false;
			UpdateVisualState();
		}

		// PointerCaptureLost event handler.
		protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerCaptureLost(pArgs);

			// MenuFlyoutItem does not capture pointer, so PointerCaptureLost means the item is no longer pressed.
			m_bIsPressed = false;
			m_bIsPointerLeftButtonDown = false;
			m_bIsPointerOver = false;
			UpdateVisualState();
		}

		// Called when the IsEnabled property changes.
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			if (!e.NewValue)
			{
				ClearStateFlags();
			}
			else
			{
				UpdateVisualState();
			}

			base.OnIsEnabledChanged(e);
		}

		// Called when the control got focus.
		protected override void OnGotFocus(RoutedEventArgs pArgs)
		{
			UpdateVisualState();
		}

		// LostFocus event handler.
		protected override void OnLostFocus(RoutedEventArgs pArgs)
		{
			if (!m_bIsPointerLeftButtonDown)
			{
				m_bIsSpaceOrEnterKeyDown = false;
				m_bIsNavigationAcceptOrGamepadAKeyDown = false;
				m_bIsPressed = false;
			}

			UpdateVisualState();
		}

		// KeyDown event handler.
		protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;
			if (!handled)
			{
				var key = pArgs.Key;
				handled = KeyPressMenuFlyout.KeyDown(key, this);
				pArgs.Handled = handled;
			}
		}

		// KeyUp event handler.
		protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;
			if (!handled)
			{
				var key = (pArgs.Key);
				KeyPressMenuFlyout.KeyUp(key, this);
				pArgs.Handled = true;
			}
		}

		// Handle the custom property changed event and call the OnPropertyChanged2 methods.
		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);
			if (args.Property == UIElement.VisibilityProperty)
			{
				OnVisibilityChanged();
			}
			else if (args.Property == MenuFlyoutItem.CommandProperty)
			{
				OnCommandChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == MenuFlyoutItem.CommandParameterProperty)
			{
				UpdateCanExecute();
			}
		}

		// Update the visual states when the Visibility property is changed.
		private protected override void OnVisibilityChanged()
		{
			Visibility visibility = Visibility;

			if (Visibility.Visible != visibility)
			{
				ClearStateFlags();
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (m_epCanExecuteChangedHandler == null)
			{
				ICommand spCommand = Command;

				if (spCommand != null)
				{
					void OnCanExecuteChanged(object sender, object args)
					{
						UpdateCanExecute();
					}

					spCommand.CanExecuteChanged += OnCanExecuteChanged;

					m_epCanExecuteChangedHandler = Disposable.Create(() => spCommand.CanExecuteChanged -= OnCanExecuteChanged);
				}
			}

			// In case we missed an update to CanExecute while the CanExecuteChanged handler was unhooked,
			// we need to update our value now.
			UpdateCanExecute();
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (m_epCanExecuteChangedHandler != null)
			{
				ICommand spCommand = Command;

				if (spCommand != null)
				{
					m_epCanExecuteChangedHandler.Dispose();
				}
			}
		}

		// Called when the Command property changes.
		void
	   OnCommandChanged(
			object pOldValue,
			object pNewValue)
		{
			// Remove handler for CanExecuteChanged from the old value
			m_epCanExecuteChangedHandler?.Dispose();

			if (pOldValue != null)
			{
				XamlUICommand oldCommand = pOldValue as XamlUICommand;

				if (oldCommand != null)
				{
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, TextProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, IconProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, KeyboardAcceleratorsProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, AccessKeyProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, AutomationProperties.HelpTextProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommand, this, ToolTipService.ToolTipProperty);
				}
			}

			// Subscribe to the CanExecuteChanged event on the new value
			if (pNewValue != null)
			{
				var spNewCommand = pNewValue as ICommand;

				void OnCanExecuteUpdated(object sender, object args)
				{
					UpdateCanExecute();
				}

				spNewCommand.CanExecuteChanged += OnCanExecuteUpdated;
				m_epCanExecuteChangedHandler = Disposable.Create(() => spNewCommand.CanExecuteChanged -= OnCanExecuteUpdated);

				var newCommandAsUICommand = spNewCommand as XamlUICommand;

				if (newCommandAsUICommand != null)
				{
					CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, this, TextProperty);
					CommandingHelpers.BindToIconPropertyIfUnset(newCommandAsUICommand, this, IconProperty);
					CommandingHelpers.BindToKeyboardAcceleratorsIfUnset(newCommandAsUICommand, this);
					CommandingHelpers.BindToAccessKeyIfUnset(newCommandAsUICommand, this);
					CommandingHelpers.BindToDescriptionPropertiesIfUnset(newCommandAsUICommand, this);
				}
			}

			// Coerce the button enabled state with the CanExecute state of the command.
			UpdateCanExecute();
		}

		// Coerces the MenuFlyoutItem's enabled state with the CanExecute state of the Command.
		void UpdateCanExecute()
		{
			ICommand spCommand;
			object spCommandParameter;
			bool canExecute = true;

			spCommand = Command;
			if (spCommand != null)
			{
				spCommandParameter = CommandParameter;
				canExecute = spCommand.CanExecute(spCommandParameter);
			}

			SuppressIsEnabled(!canExecute);
		}

		// Change to the correct visual state for the
		private protected override void ChangeVisualState(
			 // true to use transitions when updating the visual state, false
			 // to snap directly to the new visual state.
			 bool bUseTransitions)
		{
			bool hasToggleMenuItem = false;
			bool hasIconMenuItem = false;
			bool hasMenuItemWithKeyboardAcceleratorText = false;
			bool isKeyboardPresent = false;

			MenuFlyoutPresenter spPresenter = GetParentMenuFlyoutPresenter();
			if (spPresenter != null)
			{
				hasToggleMenuItem = spPresenter.GetContainsToggleItems();
				hasIconMenuItem = spPresenter.GetContainsIconItems();
				hasMenuItemWithKeyboardAcceleratorText = spPresenter.GetContainsItemsWithKeyboardAcceleratorText();
			}

			var bIsEnabled = IsEnabled;
			var focusState = FocusState;
			var shouldBeNarrow = GetShouldBeNarrow();

			// We only care about finding if we have a keyboard if we also have a menu item with accelerator text,
			// since if we don't have any menu items with accelerator text, we won't be showing any accelerator text anyway.
			if (hasMenuItemWithKeyboardAcceleratorText)
			{
				// UNO TODO
				// isKeyboardPresent = DXamlCore.GetCurrent().GetIsKeyboardPresent();
				isKeyboardPresent = true;
			}

			// CommonStates
			if (!bIsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
			}
			else if (m_bIsPressed)
			{
				VisualStateManager.GoToState(this, "Pressed", bUseTransitions);
			}
			else if (m_bIsPointerOver)
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

			// We'll make the accelerator text visible if any item has accelerator text,
			// as this causes the margin to be applied which reserves space, ensuring that accelerator text
			// in one item won't be at the same horizontal position as label text in another item.
			if (hasMenuItemWithKeyboardAcceleratorText && isKeyboardPresent)
			{
				VisualStateManager.GoToState(this, "KeyboardAcceleratorTextVisible", bUseTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "KeyboardAcceleratorTextCollapsed", bUseTransitions);
			}
		}

		// Clear flags relating to the visual state.  Called when IsEnabled is set to false
		// or when Visibility is set to Hidden or Collapsed.
		void ClearStateFlags()
		{
			m_bIsPressed = false;
			m_bIsPointerLeftButtonDown = false;
			m_bIsPointerOver = false;
			m_bIsSpaceOrEnterKeyDown = false;
			m_bIsNavigationAcceptOrGamepadAKeyDown = false;

			UpdateVisualState();
		}

		// Create MenuFlyoutItemAutomationPeer to represent the
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new MenuFlyoutItemAutomationPeer(this);
		}

		internal override string GetPlainText() => Text;

		internal string KeyboardAcceleratorTextOverrideImpl
		{
			get
			{
				var pValue = KeyboardAcceleratorTextOverride;

				// If we have no keyboard accelerator text already provided by the app,
				// then we'll see if we can ruct it ourselves based on keyboard accelerators
				// set on this item.  For example, if a keyboard accelerator with key "S" and modifier "Control"
				// is set, then we'll convert that into the keyboard accelerator text "Ctrl+S".
				if (pValue == null)
				{
					pValue = KeyboardAccelerator.GetStringRepresentationForUIElement(this);

					// If we were able to get a string representation from keyboard accelerators,
					// then we should now set that as the value of KeyboardAcceleratorText.
					if (pValue != null)
					{
						KeyboardAcceleratorTextOverrideImpl = pValue;
					}
				}

				return pValue;
			}
			set
			{
				KeyboardAcceleratorTextOverride = value;
			}
		}

		internal Size GetKeyboardAcceleratorTextDesiredSize()
		{
			var desiredSize = new Size(0, 0);

			if (!m_isTemplateApplied)
			{
				bool templateApplied = ApplyTemplate();
				m_isTemplateApplied = templateApplied;
			}

			if (m_tpKeyboardAcceleratorTextBlock != null)
			{
				Thickness margin;

				m_tpKeyboardAcceleratorTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				desiredSize = m_tpKeyboardAcceleratorTextBlock.DesiredSize;
				margin = m_tpKeyboardAcceleratorTextBlock.Margin;

				desiredSize.Width -= (float)(margin.Left + margin.Right);
				desiredSize.Height -= (float)(margin.Top + margin.Bottom);
			}

			return desiredSize;
		}

		internal void UpdateTemplateSettings(double maxKeyboardAcceleratorTextWidth)
		{
			if (m_maxKeyboardAcceleratorTextWidth != maxKeyboardAcceleratorTextWidth)
			{
				m_maxKeyboardAcceleratorTextWidth = maxKeyboardAcceleratorTextWidth;

				MenuFlyoutItemTemplateSettings templateSettings;
				templateSettings = TemplateSettings;

				if (templateSettings == null)
				{
					MenuFlyoutItemTemplateSettings templateSettingsImplementation = new MenuFlyoutItemTemplateSettings();
					TemplateSettings = templateSettingsImplementation;
					templateSettings = templateSettingsImplementation;
				}

				templateSettings.KeyboardAcceleratorTextMinWidth = m_maxKeyboardAcceleratorTextWidth;
			}
		}

		internal virtual bool HasToggle()
		{
			return false;
		}
	}
}
