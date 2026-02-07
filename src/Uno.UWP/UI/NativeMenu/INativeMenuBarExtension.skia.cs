#if __SKIA__
#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Uno.UI.NativeMenu;

/// <summary>
/// Interface for platform-specific native menu bar implementations for Skia targets.
/// </summary>
internal interface INativeMenuBarExtension
{
	/// <summary>
	/// Gets a value indicating whether native menu bar is supported on this platform.
	/// </summary>
	bool IsSupported { get; }

	/// <summary>
	/// Applies the initial menu structure to the native menu bar.
	/// </summary>
	/// <param name="items">The collection of top-level menu items to apply.</param>
	void Apply(ObservableCollection<NativeMenuItem> items);

	/// <summary>
	/// Subscribes to menu changes for automatic propagation.
	/// </summary>
	/// <param name="items">The collection of top-level menu items.</param>
	void SubscribeToChanges(ObservableCollection<NativeMenuItem> items);

	/// <summary>
	/// Handles changes to the top-level menu items collection.
	/// </summary>
	/// <param name="items">The collection of top-level menu items.</param>
	/// <param name="e">The collection change event args.</param>
	void OnItemsChanged(ObservableCollection<NativeMenuItem> items, NotifyCollectionChangedEventArgs e);

	/// <summary>
	/// Handles property changes on a menu item.
	/// </summary>
	/// <param name="item">The menu item that changed.</param>
	/// <param name="propertyName">The name of the changed property.</param>
	void OnMenuItemPropertyChanged(NativeMenuItemBase item, string? propertyName);

	/// <summary>
	/// Handles changes to a menu item's children collection.
	/// </summary>
	/// <param name="parent">The parent menu item.</param>
	/// <param name="e">The collection change event args.</param>
	void OnSubItemsChanged(NativeMenuItem parent, NotifyCollectionChangedEventArgs e);
}
#endif
