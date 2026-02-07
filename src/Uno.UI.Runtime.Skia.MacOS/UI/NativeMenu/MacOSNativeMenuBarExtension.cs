#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.Foundation.Extensibility;
using Uno.UI.NativeMenu;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// macOS Skia implementation of native menu bar using NSMenu.
/// </summary>
internal class MacOSNativeMenuBarExtension : INativeMenuBarExtension
{
	private static readonly MacOSNativeMenuBarExtension _instance = new();
	private ObservableCollection<NativeMenuItem>? _currentItems;

	private MacOSNativeMenuBarExtension()
	{
	}

	public static void Register()
	{
		ApiExtensibility.Register(typeof(INativeMenuBarExtension), _ => _instance);
	}

	public bool IsSupported => true;

	public void Apply(ObservableCollection<NativeMenuItem> items)
	{
		_currentItems = items;
		RebuildEntireMenu(items);
	}

	public void SubscribeToChanges(ObservableCollection<NativeMenuItem> items)
	{
		// The NativeMenuBar already subscribes to changes and forwards them to this extension.
		// This method is called to inform us that change tracking is now active.
		_currentItems = items;
	}

	public void OnItemsChanged(ObservableCollection<NativeMenuItem> items, NotifyCollectionChangedEventArgs e)
	{
		// For top-level menu changes, rebuild the entire menu bar
		// NSMenu doesn't have a simple way to insert/remove items at the menu bar level
		RebuildEntireMenu(items);
	}

	public void OnMenuItemPropertyChanged(NativeMenuItemBase item, string? propertyName)
	{
		// For property changes, we need to rebuild the menu structure
		// macOS NSMenu doesn't easily support in-place updates of menu items
		// A more sophisticated implementation could track menu item handles and update them
		if (_currentItems != null)
		{
			RebuildEntireMenu(_currentItems);
		}
	}

	public void OnSubItemsChanged(NativeMenuItem parent, NotifyCollectionChangedEventArgs e)
	{
		// For submenu changes, rebuild the entire menu
		// A more sophisticated implementation could rebuild only the affected submenu
		if (_currentItems != null)
		{
			RebuildEntireMenu(_currentItems);
		}
	}

	private void RebuildEntireMenu(ObservableCollection<NativeMenuItem> items)
	{
		// Clear callbacks before rebuilding
		NativeMenuCallbackManager.ClearCallbacks();

		// Clear existing menus first
		NativeMenuInterop.uno_menu_bar_clear();

		foreach (var menuItem in items.Where(m => m.IsVisible))
		{
			var menuHandle = CreateNSMenu(menuItem);
			if (menuHandle != nint.Zero)
			{
				NativeMenuInterop.uno_menu_bar_add_menu(menuHandle, menuItem.Text ?? string.Empty);
			}
		}
	}

	private static nint CreateNSMenu(NativeMenuItem menuItem)
	{
		if (string.IsNullOrEmpty(menuItem.Text))
		{
			return nint.Zero;
		}

		var menuHandle = NativeMenuInterop.uno_menu_create(menuItem.Text!);
		if (menuHandle == nint.Zero)
		{
			return nint.Zero;
		}

		foreach (var child in menuItem.Items.Where(i => i.IsVisible))
		{
			AddMenuElement(menuHandle, child);
		}

		return menuHandle;
	}

	private static void AddMenuElement(nint menuHandle, NativeMenuItemBase item)
	{
		switch (item)
		{
			case NativeMenuSeparator:
				NativeMenuInterop.uno_menu_add_separator(menuHandle);
				break;

			case NativeMenuToggleItem toggle:
				NativeMenuInterop.uno_menu_add_toggle_item(
					menuHandle,
					toggle.Text ?? string.Empty,
					toggle.IsChecked,
					toggle.IsEnabled,
					NativeMenuCallbackManager.RegisterCallback(() => toggle.RaiseInvoked()));
				break;

			case NativeMenuItem menu when menu.Items.Any():
				var submenuHandle = CreateNSMenu(menu);
				if (submenuHandle != nint.Zero)
				{
					NativeMenuInterop.uno_menu_add_submenu(menuHandle, submenuHandle, menu.Text ?? string.Empty);
				}
				break;

			case NativeMenuItem action:
				NativeMenuInterop.uno_menu_add_item(
					menuHandle,
					action.Text ?? string.Empty,
					action.IsEnabled,
					NativeMenuCallbackManager.RegisterCallback(() => action.RaiseInvoked()));
				break;
		}
	}
}

/// <summary>
/// Manages callbacks from native menu items to managed code.
/// </summary>
internal static class NativeMenuCallbackManager
{
	private static readonly object _lock = new();
	private static readonly Dictionary<int, Action> _callbacks = new();
	private static int _nextCallbackId;
	private static bool _callbacksRegistered;

	public static int RegisterCallback(Action callback)
	{
		ArgumentNullException.ThrowIfNull(callback);

		EnsureCallbacksRegistered();

		lock (_lock)
		{
			var id = _nextCallbackId++;
			_callbacks[id] = callback;
			return id;
		}
	}

	public static void ClearCallbacks()
	{
		lock (_lock)
		{
			_callbacks.Clear();
			_nextCallbackId = 0;
		}
	}

	private static unsafe void EnsureCallbacksRegistered()
	{
		if (_callbacksRegistered)
		{
			return;
		}

		_callbacksRegistered = true;
		NativeMenuInterop.uno_menu_set_item_callback(&OnMenuItemInvoked);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
	private static void OnMenuItemInvoked(int callbackId)
	{
		Action? callback;
		lock (_lock)
		{
			_callbacks.TryGetValue(callbackId, out callback);
		}

		callback?.Invoke();
	}
}

internal static partial class NativeMenuInterop
{
	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_menu_bar_clear();

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_menu_bar_add_menu(nint menuHandle, string title);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial nint uno_menu_create(string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static partial void uno_menu_add_separator(nint menuHandle);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_menu_add_item(nint menuHandle, string title, [MarshalAs(UnmanagedType.I1)] bool enabled, int callbackId);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_menu_add_toggle_item(nint menuHandle, string title, [MarshalAs(UnmanagedType.I1)] bool isChecked, [MarshalAs(UnmanagedType.I1)] bool enabled, int callbackId);

	[LibraryImport("libUnoNativeMac.dylib", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial void uno_menu_add_submenu(nint parentMenuHandle, nint submenuHandle, string title);

	[LibraryImport("libUnoNativeMac.dylib")]
	internal static unsafe partial void uno_menu_set_item_callback(delegate* unmanaged[Cdecl]<int, void> callback);
}
