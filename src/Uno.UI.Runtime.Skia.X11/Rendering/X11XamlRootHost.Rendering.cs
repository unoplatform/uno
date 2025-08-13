using System.Threading;
using Microsoft.UI.Dispatching;
using SkiaSharp;
using Uno.Helpers;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private readonly EventLoop _renderingEventLoop;
	private int _renderingScheduled;

	void IXamlRootHost.InvalidateRender()
	{
		if (_renderer is not null && (this as IXamlRootHost).RootElement is { } rootElement)
		{
			using var lockDisposable = X11Helper.XLock(TopX11Window.Display);
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(TopX11Window.Display, TopX11Window.Window, ref attributes);

			if (Interlocked.Exchange(ref _renderingScheduled, 1) == 0)
			{
				_renderingEventLoop.Schedule(() =>
				{
					Volatile.Write(ref _renderingScheduled, 0);
					_renderer.Render();
				});
			}
		}
	}
}
