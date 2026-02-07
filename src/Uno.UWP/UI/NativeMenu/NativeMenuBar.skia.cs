#if __SKIA__
#nullable enable

using System.Collections.Specialized;
using Uno.Foundation.Extensibility;

namespace Uno.UI.NativeMenu;

// Skia implementation that uses ApiExtensibility to get the platform-specific extension
public sealed partial class NativeMenuBar
{
	private static INativeMenuBarExtension? _nativeMenuBarExtension;

	static partial void IsNativeMenuSupportedPartial(ref bool isSupported)
	{
		isSupported = GetExtension()?.IsSupported ?? false;
	}

	partial void ApplyNativeMenuPartial()
	{
		GetExtension()?.Apply(_items);
	}

	partial void SubscribeToChangesPartial()
	{
		GetExtension()?.SubscribeToChanges(_items);
	}

	partial void OnItemsChangedPartial(NotifyCollectionChangedEventArgs e)
	{
		GetExtension()?.OnItemsChanged(_items, e);
	}

	partial void OnMenuItemPropertyChangedPartial(NativeMenuItemBase item, string? propertyName)
	{
		GetExtension()?.OnMenuItemPropertyChanged(item, propertyName);
	}

	partial void OnSubItemsChangedPartial(NativeMenuItem parent, NotifyCollectionChangedEventArgs e)
	{
		GetExtension()?.OnSubItemsChanged(parent, e);
	}

	private static INativeMenuBarExtension? GetExtension()
	{
		if (_nativeMenuBarExtension is null)
		{
			ApiExtensibility.CreateInstance(typeof(NativeMenuBar), out _nativeMenuBarExtension);
		}

		return _nativeMenuBarExtension;
	}
}
#endif
