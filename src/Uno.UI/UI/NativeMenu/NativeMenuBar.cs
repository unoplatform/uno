#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Represents the application's native menu bar for integration with OS-specific menu systems.
/// </summary>
public sealed partial class NativeMenuBar
{
	private static NativeMenuBar? _instance;
	private readonly ObservableCollection<NativeMenuItem> _items;

	private NativeMenuBar()
	{
		_items = new ObservableCollection<NativeMenuItem>();
	}

	/// <summary>
	/// Gets the default native menu bar instance for the application.
	/// </summary>
	/// <returns>The default NativeMenuBar instance, or null if native menus are not supported on the current platform.</returns>
	public static NativeMenuBar? GetDefault()
	{
		if (!IsNativeMenuSupported())
		{
			return null;
		}

		return _instance ??= new NativeMenuBar();
	}

	/// <summary>
	/// Gets the collection of top-level menu items.
	/// </summary>
	public IList<NativeMenuItem> Items => _items;

	/// <summary>
	/// Gets or sets a value indicating whether the native menu bar is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Applies the current menu structure to the native OS menu system.
	/// </summary>
	public void Apply()
	{
		if (IsEnabled)
		{
			ApplyNativeMenu();
		}
	}

	/// <summary>
	/// Platform-specific check for native menu support.
	/// </summary>
	static partial void IsNativeMenuSupportedPartial(ref bool isSupported);

	/// <summary>
	/// Platform-specific implementation of menu application.
	/// </summary>
	partial void ApplyNativeMenuPartial();

	private static bool IsNativeMenuSupported()
	{
		bool isSupported = false;
		IsNativeMenuSupportedPartial(ref isSupported);
		return isSupported;
	}

	private void ApplyNativeMenu()
	{
		ApplyNativeMenuPartial();
	}
}
