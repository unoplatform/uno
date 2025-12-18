#nullable enable

using System;
using System.Windows.Input;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Represents a toggleable (checkable) menu item.
/// </summary>
public sealed class NativeMenuToggleItem : NativeMenuItemBase
{
	/// <summary>
	/// Gets or sets the text displayed for this menu item.
	/// </summary>
	public string? Text { get; set; }

	/// <summary>
	/// Gets or sets whether this toggle item is checked.
	/// </summary>
	public bool IsChecked { get; set; }

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
	/// Occurs when the menu item is invoked.
	/// </summary>
	public event EventHandler<NativeMenuItemInvokedEventArgs>? Invoked;

	internal void RaiseInvoked()
	{
		IsChecked = !IsChecked;
		Invoked?.Invoke(this, new NativeMenuItemInvokedEventArgs(this));

		if (Command?.CanExecute(CommandParameter) == true)
		{
			Command.Execute(CommandParameter);
		}
	}
}
