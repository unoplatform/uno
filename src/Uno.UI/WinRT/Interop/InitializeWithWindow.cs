#if HAS_UNO_WINUI
using System;
using Microsoft.UI.Windowing;
using Windows.UI.Popups;
using MUXWindowId = Microsoft.UI.WindowId;

namespace WinRT.Interop;

public static class InitializeWithWindow
{
	public static void Initialize(object target, IntPtr hwnd)
	{
		var windowId = new MUXWindowId((ulong)hwnd.ToInt64());

		var appWindow = AppWindow.GetFromWindowId(windowId);
		var window = Windows.UI.Xaml.Window.GetFromAppWindow(appWindow);

		if (target is MessageDialog messageDialog)
		{
			messageDialog.AssociatedWindow = window;
		}
	}
}
#endif
