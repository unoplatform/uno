// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ToggleMenuFlyoutItem_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Devices.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class ToggleMenuFlyoutItem
{
	// Change to the correct visual state for the MenuFlyoutItem.
	private protected override void ChangeVisualState(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool bUseTransitions)
	{
		bool hasIconMenuItem = false;
		bool hasMenuItemWithKeyboardAcceleratorText = false;
		bool isKeyboardPresent = false;

		var isPressed = IsPointerPressed;
		var isPointerOver = IsPointerOver;
		var isEnabled = IsEnabled;
		var isChecked = IsChecked;
		var focusState = FocusState;
		var shouldBeNarrow = GetShouldBeNarrow();
		var spPresenter = GetParentMenuFlyoutPresenter();

		if (spPresenter is not null)
		{
			hasIconMenuItem = spPresenter.GetContainsIconItems();
			hasMenuItemWithKeyboardAcceleratorText = spPresenter.GetContainsItemsWithKeyboardAcceleratorText();
		}

		// We only care about finding if we have a keyboard if we also have a menu item with accelerator text,
		// since if we don't have any menu items with accelerator text, we won't be showing any accelerator text anyway.
		if (hasMenuItemWithKeyboardAcceleratorText)
		{
			isKeyboardPresent = DXamlCore.Current.IsKeyboardPresent; // TODO MZ new KeyboardCapabilities().KeyboardPresent != 0;
		}

		// CommonStates
		if (!isEnabled)
		{
			VisualStateManager.GoToState(this, "Disabled", bUseTransitions);
		}
		else if (isPressed)
		{
			VisualStateManager.GoToState(this, "Pressed", bUseTransitions);
		}
		else if (isPointerOver)
		{
			VisualStateManager.GoToState(this, "PointerOver", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "Normal", bUseTransitions);
		}

		// FocusStates
		if (FocusState.Unfocused != focusState && isEnabled)
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

		// CheckStates
		if (isChecked && hasIconMenuItem)
		{
			VisualStateManager.GoToState(this, "CheckedWithIcon", bUseTransitions);
		}
		else if (hasIconMenuItem)
		{
			VisualStateManager.GoToState(this, "UncheckedWithIcon", bUseTransitions);
		}
		else if (isChecked)
		{
			VisualStateManager.GoToState(this, "Checked", bUseTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "Unchecked", bUseTransitions);
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

	// Performs appropriate actions upon a mouse/keyboard invocation of a MenuFlyoutItem.
	internal override void Invoke()
	{
		IsChecked = !IsChecked;

		base.Invoke();
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == IsCheckedProperty)
		{
			UpdateVisualState();

			var automationListener = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged);
			if (automationListener)
			{
				var spAutomationPeer = GetOrCreateAutomationPeer();

				if (spAutomationPeer is ToggleMenuFlyoutItemAutomationPeer spToggleButtonAutomationPeer)
				{
					spToggleButtonAutomationPeer.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
				}
			}
		}
	}

	// Create ToggleMenuFlyoutItemAutomationPeer to represent the ToggleMenuFlyoutItem.
	protected override AutomationPeer OnCreateAutomationPeer() => new ToggleMenuFlyoutItemAutomationPeer(this);
}
