#if !__SKIA__ && !__IOS__ && !__MACCATALYST__
#nullable enable

namespace Uno.UI.NativeMenu;

// Default implementation for platforms that don't support native menus (Android, WebAssembly native, etc.)
public sealed partial class NativeMenuBar
{
	static partial void IsNativeMenuSupportedPartial(ref bool isSupported)
	{
		// Native menus are not supported on this platform
		isSupported = false;
	}

	partial void ApplyNativeMenuPartial()
	{
		// No-op for unsupported platforms
	}
}
#endif
