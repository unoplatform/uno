#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

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
		_items.CollectionChanged += OnItemsCollectionChanged;
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
	public ObservableCollection<NativeMenuItem> Items => _items;

	/// <summary>
	/// Gets or sets a value indicating whether the native menu bar is enabled.
	/// </summary>
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Applies the current menu structure to the native OS menu system.
	/// Call this once after initial menu setup. Subsequent changes are propagated automatically.
	/// </summary>
	public void Apply()
	{
		if (IsEnabled)
		{
			ApplyNativeMenu();
			SubscribeToChanges();
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

	/// <summary>
	/// Platform-specific subscription to menu changes.
	/// </summary>
	partial void SubscribeToChangesPartial();

	/// <summary>
	/// Platform-specific handling of top-level menu collection changes.
	/// </summary>
	partial void OnItemsChangedPartial(NotifyCollectionChangedEventArgs e);

	/// <summary>
	/// Platform-specific handling of menu item property changes.
	/// </summary>
	partial void OnMenuItemPropertyChangedPartial(NativeMenuItemBase item, string? propertyName);

	/// <summary>
	/// Platform-specific handling of submenu collection changes.
	/// </summary>
	partial void OnSubItemsChangedPartial(NativeMenuItem parent, NotifyCollectionChangedEventArgs e);

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

	private void SubscribeToChanges()
	{
		SubscribeToChangesPartial();
	}

	private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (!IsEnabled)
		{
			return;
		}

		// Subscribe/unsubscribe to item changes
		if (e.OldItems != null)
		{
			foreach (NativeMenuItem item in e.OldItems)
			{
				UnsubscribeFromMenuItem(item);
			}
		}

		if (e.NewItems != null)
		{
			foreach (NativeMenuItem item in e.NewItems)
			{
				SubscribeToMenuItem(item);
			}
		}

		OnItemsChangedPartial(e);
	}

	private void SubscribeToMenuItem(NativeMenuItem item)
	{
		item.PropertyChanged += OnMenuItemPropertyChanged;
		item.ItemsChanged += OnMenuItemChildrenChanged;

		// Subscribe to all existing children
		foreach (var child in item.Items)
		{
			SubscribeToMenuItemBase(child);
		}
	}

	private void UnsubscribeFromMenuItem(NativeMenuItem item)
	{
		item.PropertyChanged -= OnMenuItemPropertyChanged;
		item.ItemsChanged -= OnMenuItemChildrenChanged;

		// Unsubscribe from all children
		foreach (var child in item.Items)
		{
			UnsubscribeFromMenuItemBase(child);
		}
	}

	private void SubscribeToMenuItemBase(NativeMenuItemBase item)
	{
		item.PropertyChanged += OnMenuItemPropertyChanged;

		if (item is NativeMenuItem menuItem)
		{
			menuItem.ItemsChanged += OnMenuItemChildrenChanged;
			foreach (var child in menuItem.Items)
			{
				SubscribeToMenuItemBase(child);
			}
		}
	}

	private void UnsubscribeFromMenuItemBase(NativeMenuItemBase item)
	{
		item.PropertyChanged -= OnMenuItemPropertyChanged;

		if (item is NativeMenuItem menuItem)
		{
			menuItem.ItemsChanged -= OnMenuItemChildrenChanged;
			foreach (var child in menuItem.Items)
			{
				UnsubscribeFromMenuItemBase(child);
			}
		}
	}

	private void OnMenuItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (!IsEnabled || sender is not NativeMenuItemBase item)
		{
			return;
		}

		OnMenuItemPropertyChangedPartial(item, e.PropertyName);
	}

	private void OnMenuItemChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (!IsEnabled || sender is not NativeMenuItem parent)
		{
			return;
		}

		// Subscribe/unsubscribe to new/old items
		if (e.OldItems != null)
		{
			foreach (NativeMenuItemBase item in e.OldItems)
			{
				UnsubscribeFromMenuItemBase(item);
			}
		}

		if (e.NewItems != null)
		{
			foreach (NativeMenuItemBase item in e.NewItems)
			{
				SubscribeToMenuItemBase(item);
			}
		}

		OnSubItemsChangedPartial(parent, e);
	}
}
