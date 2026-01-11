#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Base class for items that can appear in a native menu.
/// </summary>
public abstract class NativeMenuItemBase : INotifyPropertyChanged
{
	private bool _isVisible = true;

	/// <summary>
	/// Gets or sets whether this menu item is visible.
	/// </summary>
	public bool IsVisible
	{
		get => _isVisible;
		set => SetProperty(ref _isVisible, value);
	}

	/// <summary>
	/// Occurs when a property value changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Raises the PropertyChanged event.
	/// </summary>
	/// <param name="propertyName">The name of the property that changed.</param>
	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	/// <summary>
	/// Sets a property value and raises PropertyChanged if the value changed.
	/// </summary>
	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (Equals(field, value))
		{
			return false;
		}

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}
