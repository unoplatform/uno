#if __SKIA__
#nullable enable

using System.Linq;
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
		GetExtension()?.Apply(_items.ToList());
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
