#nullable enable

namespace Uno.UI.NativeMenu;

/// <summary>
/// Base class for items that can appear in a native menu.
/// </summary>
public abstract class NativeMenuItemBase
{
	/// <summary>
	/// Gets or sets whether this menu item is visible.
	/// </summary>
	public bool IsVisible { get; set; } = true;
}
