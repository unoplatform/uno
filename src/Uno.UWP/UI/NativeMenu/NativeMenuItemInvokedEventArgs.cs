#nullable enable

using System;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Event arguments for when a native menu item is invoked.
/// </summary>
public sealed class NativeMenuItemInvokedEventArgs : EventArgs
{
	internal NativeMenuItemInvokedEventArgs(NativeMenuItemBase item)
	{
		Item = item;
	}

	/// <summary>
	/// Gets the menu item that was invoked.
	/// </summary>
	public NativeMenuItemBase Item { get; }
}
