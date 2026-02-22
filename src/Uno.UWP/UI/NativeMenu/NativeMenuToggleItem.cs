#nullable enable

using System;
using System.Windows.Input;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Represents a toggleable (checkable) menu item.
/// </summary>
public sealed class NativeMenuToggleItem : NativeMenuItemBase
{
	private string? _text;
	private bool _isChecked;
	private ICommand? _command;
	private object? _commandParameter;
	private bool _isEnabled = true;

	/// <summary>
	/// Gets or sets the text displayed for this menu item.
	/// </summary>
	public string? Text
	{
		get => _text;
		set => SetProperty(ref _text, value);
	}

	/// <summary>
	/// Gets or sets whether this toggle item is checked.
	/// </summary>
	public bool IsChecked
	{
		get => _isChecked;
		set => SetProperty(ref _isChecked, value);
	}

	/// <summary>
	/// Gets or sets the command to execute when this menu item is invoked.
	/// </summary>
	public ICommand? Command
	{
		get => _command;
		set => SetProperty(ref _command, value);
	}

	/// <summary>
	/// Gets or sets the command parameter.
	/// </summary>
	public object? CommandParameter
	{
		get => _commandParameter;
		set => SetProperty(ref _commandParameter, value);
	}

	/// <summary>
	/// Gets or sets whether this menu item is enabled.
	/// </summary>
	public bool IsEnabled
	{
		get => _isEnabled;
		set => SetProperty(ref _isEnabled, value);
	}

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
