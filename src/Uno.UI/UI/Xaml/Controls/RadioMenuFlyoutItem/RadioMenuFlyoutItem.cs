// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RadioMenuFlyoutItem.cpp, commit 69097129a853c65a16447aade4c82576d4724b1a
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls
{
	// UNO Docs:
	// In WinUI 2.6. RadioMenuFlyoutItem derives publically from MenuFlyoutItem, but secretly from ToggleMenuFlyoutItem.
	// Since we can't do that in C#, the important functionality are in ToggleMenuFlyoutItem, we derive from it.
	public partial class RadioMenuFlyoutItem : ToggleMenuFlyoutItem
	{
		[ThreadStatic]
		private static Dictionary<string, WeakReference<RadioMenuFlyoutItem>> s_selectionMap;

		// Copies of IsChecked & GroupName so Unloaded cleanup uses the values at registration time
		// even if the dependency properties have since changed.
		private bool m_isChecked;
		private string m_groupName = "";

		private bool m_isSafeUncheck;

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
			this.RegisterPropertyChangedCallback(ToggleMenuFlyoutItem.IsCheckedProperty, OnInternalIsCheckedChanged);


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
				if (base.IsChecked != IsChecked)
				{
					m_isSafeUncheck = true;
					base.IsChecked = IsChecked;
					m_isSafeUncheck = false;
					UpdateCheckedItemInGroup();
				}
				m_isChecked = IsChecked;
			}
			else if (property == GroupNameProperty)
			{
				m_groupName = GroupName;
			}
		}

		private void OnInternalIsCheckedChanged(DependencyObject sender, DependencyProperty args)
		{
			if (!base.IsChecked)
			{
				if (m_isSafeUncheck)
				{
					// The uncheck is due to another radio button being checked -- that's all right.
					IsChecked = false;
				}
				else
				{
					// The uncheck is due to user interaction -- not allowed.
					base.IsChecked = true;
				}
			}
			else if (!IsChecked)
			{
				IsChecked = true;
				UpdateCheckedItemInGroup();
			}
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
