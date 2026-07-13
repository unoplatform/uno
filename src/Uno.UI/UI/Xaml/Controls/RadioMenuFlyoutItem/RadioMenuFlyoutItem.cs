// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RadioMenuFlyoutItem.cpp, commit 69097129a853c65a16447aade4c82576d4724b1a
using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls
{
	// In WinUI, RadioMenuFlyoutItem derives publicly from MenuFlyoutItem but secretly from ToggleMenuFlyoutItem.
	// C# has no such double inheritance, so we derive from MenuFlyoutItem (matching the public WinUI base) and
	// duplicate the check/toggle rendering behaviour ToggleMenuFlyoutItem would otherwise provide.
	public partial class RadioMenuFlyoutItem : MenuFlyoutItem
	{
		[ThreadStatic]
		private static Dictionary<string, WeakReference<RadioMenuFlyoutItem>> s_selectionMap;

		// Copies of IsChecked & GroupName so Unloaded cleanup uses the values at registration time
		// even if the dependency properties have since changed.
		private bool m_isChecked;
		private string m_groupName = "";

#if HAS_UNO
		// Uno-specific: the GroupName under which we last registered ourselves in
		// s_selectionMap. Used to clean up a stale registration when GroupName changes
		// after IsChecked (the common case is the XAML attribute order
		// IsChecked="True" GroupName="...") so unrelated items in the default ""
		// group cannot incorrectly uncheck us.
		// Tracked upstream at: https://github.com/microsoft/microsoft-ui-xaml/issues/11098
		private string m_lastRegisteredGroupName;
#endif

		public RadioMenuFlyoutItem()
		{
			if (s_selectionMap == null)
			{
				// Ensure that this object exists
				s_selectionMap = new Dictionary<string, WeakReference<RadioMenuFlyoutItem>>();
			}

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;

			this.SetDefaultStyleKey();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Register the item in the selection map now that all XAML-applied properties (e.g. GroupName)
			// are guaranteed to be set. This handles the case where IsChecked is set before GroupName
			// during XAML parsing, which would otherwise leave the item registered under an empty group name.
			UpdateCheckedItemInGroup();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			// If this is the checked item, remove it from the lookup.
			if (m_isChecked)
			{
#if HAS_UNO
				// Uno-specific: erase via the key we actually registered under (may differ from
				// m_groupName if GroupName changed after we registered) and only if the entry
				// still points to this instance, so we never remove another item's registration.
				// See https://github.com/microsoft/microsoft-ui-xaml/issues/11098
				var key = m_lastRegisteredGroupName ?? m_groupName;
				if (s_selectionMap is { } map
					&& key is not null
					&& map.TryGetValue(key, out var weak)
					&& weak.TryGetTarget(out var target)
					&& target == this)
				{
					map.Remove(key);
				}
#else
				s_selectionMap?.Remove(m_groupName);
#endif
			}
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;
			if (property == IsCheckedProperty)
			{
				m_isChecked = IsChecked;

				UpdateVisualState();

				if (IsChecked)
				{
					UpdateCheckedItemInGroup();
				}

				if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
				{
					if (GetOrCreateAutomationPeer() is ToggleMenuFlyoutItemAutomationPeer toggleAutomationPeer)
					{
						toggleAutomationPeer.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
					}
				}
			}
			else if (property == GroupNameProperty)
			{
				m_groupName = GroupName;
			}
		}

		// Reserve the check/toggle column in the parent MenuFlyoutPresenter, as ToggleMenuFlyoutItem does.
		// Otherwise a menu containing only RadioMenuFlyoutItems would not reserve space for the checkmark.
		internal override bool HasToggle() => true;

		// Performs appropriate actions upon a mouse/keyboard invocation of a RadioMenuFlyoutItem.
		// A radio item is always checked on invocation; it is never toggled off by user interaction.
		internal override void Invoke()
		{
			IsChecked = true;

			base.Invoke();
		}

		private void UpdateCheckedItemInGroup()
		{
			if (IsChecked)
			{
				var groupName = GroupName;
				if (s_selectionMap.TryGetValue(groupName, out var previousCheckedItemWeak))
				{
					if (previousCheckedItemWeak.TryGetTarget(out var previousCheckedItem))
					{
						if (previousCheckedItem != this)
						{
							// Uncheck the previously checked item.
							previousCheckedItem.IsChecked = false;
						}
					}
				}

#if HAS_UNO
				// Uno-specific: remove any stale self-registration under a previous GroupName
				// before re-adding, so e.g. an XAML-pre-checked item that later moved to a real
				// group no longer leaks under the empty "" key.
				// See https://github.com/microsoft/microsoft-ui-xaml/issues/11098
				if (m_lastRegisteredGroupName is { } prevKey && prevKey != groupName)
				{
					if (s_selectionMap.TryGetValue(prevKey, out var staleWeak)
						&& staleWeak.TryGetTarget(out var staleTarget)
						&& staleTarget == this)
					{
						s_selectionMap.Remove(prevKey);
					}
				}
				m_lastRegisteredGroupName = groupName;
#endif

				s_selectionMap[groupName] = new WeakReference<RadioMenuFlyoutItem>(this);
			}
		}

		// Change to the correct visual state for the RadioMenuFlyoutItem.
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
				isKeyboardPresent = DXamlCore.Current.IsKeyboardPresent;
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

		// Radio menu items expose the same toggle automation surface as ToggleMenuFlyoutItem,
		// matching WinUI where RadioMenuFlyoutItem secretly derives from ToggleMenuFlyoutItem.
		protected override AutomationPeer OnCreateAutomationPeer() => new ToggleMenuFlyoutItemAutomationPeer(this);

		private static void OnAreCheckStatesEnabledPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue is true)
			{
				if (sender is MenuFlyoutSubItem subMenu)
				{
					// Every time the submenu is loaded, see if it contains a checked RadioMenuFlyoutItem or not.
					subMenu.Loaded += (object sender, RoutedEventArgs _) =>
					{
						bool isAnyItemChecked = false;
						foreach (var item in subMenu.Items)
						{
							if (item is RadioMenuFlyoutItem radioItem)
							{
								isAnyItemChecked = isAnyItemChecked || radioItem.IsChecked;
							}
						}
						VisualStateManager.GoToState(subMenu, isAnyItemChecked ? "Checked" : "Unchecked", false);
					};
				}
			}
		}
	}
}
