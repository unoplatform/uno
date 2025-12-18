#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using Windows.System;

namespace Uno.UI.NativeMenu;

public sealed partial class NativeMenuBar
{
	static partial void IsNativeMenuSupportedPartial(ref bool isSupported)
	{
		// UIMenu bar support is available on iOS 13+ and Mac Catalyst
		isSupported = UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
	}

	partial void ApplyNativeMenuPartial()
	{
		// On iOS, menu bar integration requires buildMenu(with:) to be called from the UIApplicationDelegate.
		// This implementation stores the menu structure to be used when the system requests menu building.
		NativeMenuBridgeiOS.SetMenuItems(_items.ToList());
	}
}

/// <summary>
/// Bridge class to provide menu structure to the iOS UIApplicationDelegate.
/// </summary>
internal static class NativeMenuBridgeiOS
{
	private static List<NativeMenuItem>? _menuItems;

	internal static void SetMenuItems(List<NativeMenuItem> items)
	{
		_menuItems = items;
	}

	/// <summary>
	/// Builds the native iOS menu from the configured menu items.
	/// Call this from your AppDelegate's BuildMenu method.
	/// </summary>
	/// <param name="builder">The UIMenuBuilder provided by iOS.</param>
	public static void BuildMenu(IUIMenuBuilder builder)
	{
		if (_menuItems == null || !_menuItems.Any())
		{
			return;
		}

		foreach (var menuItem in _menuItems.Where(m => m.IsVisible))
		{
			var uiMenu = CreateUIMenu(menuItem);
			if (uiMenu != null)
			{
				builder.InsertSiblingMenu(uiMenu, UIMenuIdentifier.Window.GetConstant(), UIMenuInsertionPoint.AfterSibling);
			}
		}
	}

	private static UIMenu? CreateUIMenu(NativeMenuItem menuItem)
	{
		if (string.IsNullOrEmpty(menuItem.Text))
		{
			return null;
		}

		var children = new List<UIMenuElement>();

		foreach (var child in menuItem.Items.Where(i => i.IsVisible))
		{
			var element = CreateUIMenuElement(child);
			if (element != null)
			{
				children.Add(element);
			}
		}

		return UIMenu.Create(
			title: menuItem.Text!,
			image: null,
			identifier: null,
			options: UIMenuOptions.None,
			children: children.ToArray());
	}

	private static UIMenuElement? CreateUIMenuElement(NativeMenuItemBase item)
	{
		return item switch
		{
			NativeMenuSeparator => null, // Separators are handled via inline display option
			NativeMenuToggleItem toggle => CreateUIAction(toggle),
			NativeMenuItem menu when menu.Items.Any() => CreateUIMenu(menu),
			NativeMenuItem action => CreateUIAction(action),
			_ => null
		};
	}

	private static UIAction CreateUIAction(NativeMenuItem menuItem)
	{
		var action = UIAction.Create(
			title: menuItem.Text ?? string.Empty,
			image: null,
			identifier: null,
			handler: _ => menuItem.RaiseInvoked());

		if (!menuItem.IsEnabled)
		{
			action.Attributes = UIMenuElementAttributes.Disabled;
		}

		return action;
	}

	private static UIAction CreateUIAction(NativeMenuToggleItem toggleItem)
	{
		var action = UIAction.Create(
			title: toggleItem.Text ?? string.Empty,
			image: null,
			identifier: null,
			handler: _ => toggleItem.RaiseInvoked());

		if (toggleItem.IsChecked)
		{
			action.State = UIMenuElementState.On;
		}

		if (!toggleItem.IsEnabled)
		{
			action.Attributes = UIMenuElementAttributes.Disabled;
		}

		return action;
	}

	private static UIKeyModifierFlags ConvertModifiers(VirtualKeyModifiers modifiers)
	{
		var flags = UIKeyModifierFlags.None;

		if ((modifiers & VirtualKeyModifiers.Control) != 0)
		{
			flags |= UIKeyModifierFlags.Control;
		}
		if ((modifiers & VirtualKeyModifiers.Shift) != 0)
		{
			flags |= UIKeyModifierFlags.Shift;
		}
		if ((modifiers & VirtualKeyModifiers.Menu) != 0)
		{
			flags |= UIKeyModifierFlags.Alternate;
		}
		// Note: On macOS/iOS, Command is the primary modifier, but Windows uses Control
		// Map Control to Command for more natural macOS/iOS behavior
		if ((modifiers & VirtualKeyModifiers.Windows) != 0)
		{
			flags |= UIKeyModifierFlags.Command;
		}

		return flags;
	}
}
