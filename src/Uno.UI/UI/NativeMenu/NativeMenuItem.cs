#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Represents a menu item that can have child items (submenu).
/// </summary>
public sealed class NativeMenuItem : NativeMenuItemBase
{
	private readonly ObservableCollection<NativeMenuItemBase> _items;

	/// <summary>
	/// Initializes a new instance of the <see cref="NativeMenuItem"/> class.
	/// </summary>
	public NativeMenuItem()
	{
		_items = new ObservableCollection<NativeMenuItemBase>();
	}

	/// <summary>
	/// Gets or sets the text displayed for this menu item.
	/// </summary>
	public string? Text { get; set; }

	/// <summary>
	/// Gets or sets the icon for this menu item.
	/// </summary>
	public IconSource? Icon { get; set; }

	/// <summary>
	/// Gets or sets the command to execute when this menu item is invoked.
	/// </summary>
	public ICommand? Command { get; set; }

	/// <summary>
	/// Gets or sets the command parameter.
	/// </summary>
	public object? CommandParameter { get; set; }

	/// <summary>
	/// Gets or sets whether this menu item is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the keyboard accelerator for this menu item.
	/// </summary>
	public KeyboardAccelerator? KeyboardAccelerator { get; set; }

	/// <summary>
	/// Gets the collection of child menu items (submenu).
	/// </summary>
	public IList<NativeMenuItemBase> Items => _items;

	/// <summary>
	/// Occurs when the menu item is invoked.
	/// </summary>
	public event EventHandler<NativeMenuItemInvokedEventArgs>? Invoked;

	internal void RaiseInvoked()
	{
		Invoked?.Invoke(this, new NativeMenuItemInvokedEventArgs(this));

		if (Command?.CanExecute(CommandParameter) == true)
		{
			Command.Execute(CommandParameter);
		}
	}
}
