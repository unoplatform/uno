#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Represents a menu item that can have child items (submenu).
/// </summary>
public sealed class NativeMenuItem : NativeMenuItemBase
{
	private readonly ObservableCollection<NativeMenuItemBase> _items;
	private string? _text;
	private ICommand? _command;
	private object? _commandParameter;
	private bool _isEnabled = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="NativeMenuItem"/> class.
	/// </summary>
	public NativeMenuItem()
	{
		_items = new ObservableCollection<NativeMenuItemBase>();
		_items.CollectionChanged += OnItemsCollectionChanged;
	}

	/// <summary>
	/// Gets or sets the text displayed for this menu item.
	/// </summary>
	public string? Text
	{
		get => _text;
		set => SetProperty(ref _text, value);
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
	/// Gets the collection of child menu items (submenu).
	/// </summary>
	public ObservableCollection<NativeMenuItemBase> Items => _items;

	/// <summary>
	/// Occurs when the menu item is invoked.
	/// </summary>
	public event EventHandler<NativeMenuItemInvokedEventArgs>? Invoked;

	/// <summary>
	/// Occurs when the child items collection changes.
	/// </summary>
	public event NotifyCollectionChangedEventHandler? ItemsChanged;

	internal void RaiseInvoked()
	{
		Invoked?.Invoke(this, new NativeMenuItemInvokedEventArgs(this));

		if (Command?.CanExecute(CommandParameter) == true)
		{
			Command.Execute(CommandParameter);
		}
	}

	private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		ItemsChanged?.Invoke(this, e);
	}
}
