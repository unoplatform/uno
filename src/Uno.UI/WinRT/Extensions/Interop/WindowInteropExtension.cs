#nullable enable

using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Services.Store;
using Windows.UI.Popups;
using WinRT.Interop;
using WinRT.Interop.Internal;
using MUXWindowId = Microsoft.UI.WindowId;

namespace Uno.UI.WinRT.Extensions.Interop;

internal sealed class WindowInteropExtension : IWindowInteropExtension
{
	internal static WindowInteropExtension Instance { get; } = new();

	private WindowInteropExtension()
	{
	}

	public IntPtr GetWindowHandle(object target)
	{
		if (target is not Window window)
		{
			throw new InvalidOperationException("The target must be a Window");
		}

		return new IntPtr((long)window.AppWindow.Id.Value);
	}

	public void Initialize(object target, IntPtr hwnd)
	{
		var windowId = new MUXWindowId((ulong)hwnd.ToInt64());

		var appWindow = AppWindow.GetFromWindowId(windowId);
		var window = Window.GetFromAppWindow(appWindow);

		if (target is MessageDialog messageDialog)
		{
			messageDialog.AssociatedWindow = window;
		}
		else if (target is StoreContext storeContext)
		{
			storeContext.AssociatedWindow = window;
		}
	}
}
