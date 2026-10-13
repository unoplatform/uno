#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Wayland;

internal sealed class WaylandApplicationViewExtension(object owner, WaylandConnection connection) : IApplicationViewExtension
{
	private readonly ApplicationView _owner = (ApplicationView)owner;

	// The size is in view pixels. It is a request: a compositor-chosen size (maximized, tiled, fullscreen) wins.
	public bool TryResizeView(Size size)
	{
		if (AppWindow.GetFromWindowId(_owner.WindowId) is not { } appWindow
			|| Microsoft.UI.Xaml.Window.GetFromAppWindow(appWindow) is not { } window
			|| connection.FindWindow(window) is not { } wrapper)
		{
			return false;
		}
		wrapper.Resize(new SizeInt32((int)Math.Round(size.Width * wrapper.Scale), (int)Math.Round(size.Height * wrapper.Scale)));
		return true;
	}
}
