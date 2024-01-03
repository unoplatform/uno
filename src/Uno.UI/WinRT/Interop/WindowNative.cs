#if HAS_UNO_WINUI || WINUI_WINDOWING
using System;
using Microsoft.UI.Xaml;

namespace WinRT.Interop;

public static class WindowNative
{
	public static IntPtr GetWindowHandle(object target)
	{
		if (target is not Window window)
		{
			throw new InvalidOperationException("The target must be a Window");
		}

		return new IntPtr((long)window.AppWindow.Id.Value);
	}
}
#endif
