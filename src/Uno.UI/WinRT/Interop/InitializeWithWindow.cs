using System;

namespace WinRT.Interop;

public static class InitializeWithWindow
{
	public static void Initialize(object target, IntPtr hwnd)
	{
		// Intentionally a no-op, will need to be implemented
		// when multi-window support is added #8341.
	}
}
