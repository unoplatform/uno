// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItem_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Windows.Foundation;
using ICommand = System.Windows.Input.ICommand;

#if HAS_UNO_WINUI
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutItem
{
	// Prepares object's state
	private void Initialize()
	{
		//base.Initialize();
		Loaded += (s, e) => ClearStateFlags();


#if HAS_UNO // Ensure Enter/Leave are called.
		Loaded += (s, e) => EnterImpl(true, false, false, false);
		Unloaded += (s, e) => LeaveImpl(true, false, false, false);
#endif
	}

	// Apply a template to the
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		var keyboardAcceleratorTextBlock = this.GetTemplateChild("KeyboardAcceleratorTextBlock") as TextBlock;
		m_tpKeyboardAcceleratorTextBlock = keyboardAcceleratorTextBlock;

		SuppressIsEnabled(false);
		UpdateCanExecute();

		InitializeKeyboardAcceleratorText();

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
			var spPointerPoint = pArgs.GetCurrentPoint(this);
			var spPointerProperties = spPointerPoint.Properties;
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

			if (m_shouldPerformActions)
			{
				GestureModes gestureFollowing = GestureModes.None;

				m_bIsPressed = false;

				UpdateVisualState();
				gestureFollowing = pArgs.GestureFollowing;

				// Note that we are intentionally NOT handling the args
				// if we do not fall through here because basically we are no_opting in that case.
				if (gestureFollowing != GestureModes.RightTapped)
				{
					pArgs.Handled = true;
					PerformPointerUpAction();
				}
			}
		}
	}

	private protected override void OnRightTappedUnhandled(RightTappedRoutedEventArgs pArgs)
	{
		base.OnRightTappedUnhandled(pArgs);

		var handled = pArgs.Handled;
		if (!handled)
		{
			PerformPointerUpAction();
			pArgs.Handled = true;
		}
	}

	// Contains the logic to be employed if we decide to handle pointer released.
	private void PerformPointerUpAction()
	{
		if (m_shouldPerformActions)
		{
			Focus(FocusState.Pointer);
			Invoke();
		}

		m_shouldPerformActions = false;
	}

	// Performs appropriate actions upon a mouse/keyboard invocation of a
	internal virtual void Invoke()
	{
		// Create the args
		var spArgs = new RoutedEventArgs();
		spArgs.OriginalSource = this;

		// Raise the event
		Click?.Invoke(this, spArgs);

		AutomationPeer.RaiseEventIfListener(this, AutomationEvents.InvokePatternOnInvoked);

		// Execute command associated with the button
		ExecuteCommand();

		bool shouldPreventDismissOnPointer = PreventDismissOnPointer;
		if (!shouldPreventDismissOnPointer)
		{
			// Close the MenuFlyout.
			var spParentMenuFlyoutPresenter = GetParentMenuFlyoutPresenter();
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
	private void ExecuteCommand()
	{
		var spCommand = Command;

		if (spCommand is not null)
		{
			var spCommandParameter = CommandParameter;
			var canExecute = spCommand.CanExecute(spCommandParameter);
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

		m_bIsPointerOver = true;

		var spParentPresenter = GetParentMenuFlyoutPresenter();
		if (spParentPresenter is not null)
		{
			IMenuPresenter subPresenter;

			subPresenter = (spParentPresenter as IMenuPresenter).SubPresenter;
			if (subPresenter != null)
			{
				ISubMenuOwner subPresenterOwner;

				subPresenterOwner = subPresenter.Owner;

				if (subPresenterOwner is not null)
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
		base.OnIsEnabledChanged(e);

		if (!e.NewValue)
		{
			ClearStateFlags();
		}
		else
		{
			UpdateVisualState();
		}
	}

	// Called when the control got focus.
	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		base.OnGotFocus(pArgs);

		UpdateVisualState();
	}

	// LostFocus event handler.
	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		base.OnLostFocus(pArgs);

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
		base.OnKeyDown(pArgs);

		var handled = pArgs.Handled;
		if (!handled)
		{
			var key = pArgs.Key;
			handled = MenuFlyout.KeyProcess.KeyDown(key, this);
			pArgs.Handled = handled;
		}
	}

	// KeyUp event handler.
	protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
	{
		base.OnKeyUp(pArgs);

		var handled = pArgs.Handled;
		if (!handled)
		{
			var key = (pArgs.Key);
			MenuFlyout.KeyProcess.KeyUp(key, this);
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
		else if (args.Property == KeyboardAcceleratorTextOverrideProperty)
		{
			// If KeyboardAcceleratorTextOverride is being set outside of us internally setting the value,
			// then we no longer own its value, and we should not override it with anything.
			if (!m_isSettingKeyboardAcceleratorTextOverride)
			{
				m_ownsKeyboardAcceleratorTextOverride = false;
			}
		}
	}

	// Update the visual states when the Visibility property is changed.
	private void OnVisibilityChanged()
	{
		Visibility visibility = Visibility;

		if (Visibility.Visible != visibility)
		{
			ClearStateFlags();
		}
	}

	private void EnterImpl(bool isLive, bool skipNameRegistration, bool coercedIsEnabled, bool useLayoutRounding)
	{
		//base.EnterImpl(isLive, skipNameRegistration, coercedIsEnabled, useLayoutRounding);

		if (isLive)
		{
			var command = Command;

			if (command is not null)
			{
				m_epCanExecuteChangedHandler.Disposable = null;
				void OnCanExecuteChanged(object sender, EventArgs args)
				{
					UpdateCanExecute();
				};

				command.CanExecuteChanged += OnCanExecuteChanged;
				m_epCanExecuteChangedHandler.Disposable = Disposable.Create(() => command.CanExecuteChanged -= OnCanExecuteChanged);
			}

			// In case we missed an update to CanExecute while the CanExecuteChanged handler was unhooked,
			// we need to update our value now.
			UpdateCanExecute();
		}
	}

	private void LeaveImpl(bool isLive, bool skipNameRegistration, bool coercedIsEnabled, bool useLayoutRounding)
	{
		//base.LeaveImpl(isLive, skipNameRegistration, coercedIsEnabled, useLayoutRounding);

		if (isLive && m_epCanExecuteChangedHandler.Disposable is not null)
		{
			var spCommand = Command;

			if (spCommand is not null)
			{
				m_epCanExecuteChangedHandler.Disposable = null;
			}
		}
	}

	// Called when the Command property changes.
	private void OnCommandChanged(object pOldValue, object pNewValue)
	{
		// Remove handler for CanExecuteChanged from the old value
		m_epCanExecuteChangedHandler.Disposable = null;

		if (pOldValue is not null)
		{
			if (pOldValue is XamlUICommand oldCommandAsUICommand)
			{
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, TextProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, IconProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, KeyboardAcceleratorsProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, AccessKeyProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, AutomationProperties.HelpTextProperty);
				CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, ToolTipService.ToolTipProperty);
			}
		}

		// Subscribe to the CanExecuteChanged event on the new value
		if (pNewValue is not null)
		{
			var spNewCommand = pNewValue as ICommand;

			void OnCanExecuteUpdated(object sender, object args)
			{
				UpdateCanExecute();
			}

			spNewCommand.CanExecuteChanged += OnCanExecuteUpdated;
			m_epCanExecuteChangedHandler.Disposable = Disposable.Create(() => spNewCommand.CanExecuteChanged -= OnCanExecuteUpdated);

			if (spNewCommand is XamlUICommand newCommandAsUICommand)
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
	private void UpdateCanExecute()
	{
		bool canExecute = true;

		var spCommand = Command;
		if (spCommand is not null)
		{
			var spCommandParameter = CommandParameter;
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

		var spPresenter = GetParentMenuFlyoutPresenter();
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
			isKeyboardPresent = DXamlCore.Current.IsKeyboardPresent;
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
	private void ClearStateFlags()
	{
		m_bIsPressed = false;
		m_bIsPointerLeftButtonDown = false;
		m_bIsPointerOver = false;
		m_bIsSpaceOrEnterKeyDown = false;
		m_bIsNavigationAcceptOrGamepadAKeyDown = false;

		UpdateVisualState();
	}

	// Create MenuFlyoutItemAutomationPeer to represent the
	protected override AutomationPeer OnCreateAutomationPeer() => new MenuFlyoutItemAutomationPeer(this);

	private protected override string GetPlainText() => Text;

	internal Size GetKeyboardAcceleratorTextDesiredSize()
	{
		var desiredSize = new Size(0, 0);

		if (!m_isTemplateApplied)
		{
			bool templateApplied = ApplyTemplate();
			m_isTemplateApplied = templateApplied;
		}

		if (m_tpKeyboardAcceleratorTextBlock is not null)
		{
			m_tpKeyboardAcceleratorTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			desiredSize = m_tpKeyboardAcceleratorTextBlock.DesiredSize;
			var margin = m_tpKeyboardAcceleratorTextBlock.Margin;

			desiredSize.Width -= (float)(margin.Left + margin.Right);
			desiredSize.Height -= (float)(margin.Top + margin.Bottom);
		}

		return desiredSize;
	}

	private void InitializeKeyboardAcceleratorText()
	{
		// If we have no keyboard accelerator text already provided by the app,
		// then we'll see if we can construct it ourselves based on keyboard accelerators
		// set on this item.  For example, if a keyboard accelerator with key "S" and modifier "Control"
		// is set, then we'll convert that into the keyboard accelerator text "Ctrl+S".
		if (m_ownsKeyboardAcceleratorTextOverride)
		{
			var keyboardAcceleratorTextOverride = KeyboardAccelerator.GetStringRepresentationForUIElement(this);

			// If we were able to get a string representation from keyboard accelerators,
			// then we should now set that as the value of KeyboardAcceleratorText.
			if (keyboardAcceleratorTextOverride is not null)
			{
				using var guard = Disposable.Create(() =>
				{
					m_isSettingKeyboardAcceleratorTextOverride = false;
				});

				m_isSettingKeyboardAcceleratorTextOverride = true;
				KeyboardAcceleratorTextOverride = keyboardAcceleratorTextOverride;
			}
		}
	}

	internal void UpdateTemplateSettings(double maxKeyboardAcceleratorTextWidth)
	{
		if (m_maxKeyboardAcceleratorTextWidth != maxKeyboardAcceleratorTextWidth)
		{
			m_maxKeyboardAcceleratorTextWidth = maxKeyboardAcceleratorTextWidth;

			MenuFlyoutItemTemplateSettings templateSettings = TemplateSettings;

			if (templateSettings is null)
			{
				MenuFlyoutItemTemplateSettings templateSettingsImplementation = new();
				TemplateSettings = templateSettingsImplementation;
				templateSettings = templateSettingsImplementation;
			}

			templateSettings.KeyboardAcceleratorTextMinWidth = m_maxKeyboardAcceleratorTextWidth;
		}
	}
}
