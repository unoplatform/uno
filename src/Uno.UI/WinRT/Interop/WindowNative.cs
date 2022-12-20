#if HAS_UNO_WINUI
using System;

namespace WinRT.Interop;

public static class WindowNative
{
	public static IntPtr GetWindowHandle(object target)
	{
		// Intentionally a no-op, will need to be implemented
		// when multi-window support is added #8341.
		return IntPtr.Zero;
	}
}
#endif
