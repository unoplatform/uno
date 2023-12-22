#if HAS_UNO_WINUI || WINUI_WINDOWING
using System;
using Microsoft.UI.Xaml;

namespace WinRT.Interop;

public static class WindowNative
{
	public static IntPtr GetWindowHandle(object target)
	{
		if (target is Window window)
		{
			return new IntPtr((long)window.AppWindow.Id.Value);
		}

		return IntPtr.Zero;
	}
}
#endif
