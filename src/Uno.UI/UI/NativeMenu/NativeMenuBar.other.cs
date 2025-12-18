#nullable enable

namespace Uno.UI.NativeMenu;

// Default implementation for platforms that don't support native menus
public sealed partial class NativeMenuBar
{
#if !__IOS__ && !__MACCATALYST__
	static partial void IsNativeMenuSupportedPartial(ref bool isSupported)
	{
		// Native menus are not supported on this platform by default
		isSupported = false;
	}

	partial void ApplyNativeMenuPartial()
	{
		// No-op for unsupported platforms
	}
#endif
}
