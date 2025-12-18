#nullable enable

using System.Linq;

namespace Uno.UI.NativeMenu;

// Default implementation for platforms that don't support native menus
// On Skia platforms, this uses the registered INativeMenuBarProvider extension if available
public sealed partial class NativeMenuBar
{
#if !__IOS__ && !__MACCATALYST__
	static partial void IsNativeMenuSupportedPartial(ref bool isSupported)
	{
		// Check if a Skia-specific provider has been registered
		var provider = NativeMenuBarSkiaSupport.GetProvider();
		isSupported = provider?.IsSupported ?? false;
	}

	partial void ApplyNativeMenuPartial()
	{
		// Use the registered Skia provider if available
		var provider = NativeMenuBarSkiaSupport.GetProvider();
		provider?.Apply(_items.ToList());
	}
#endif
}

