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
	private readonly object _nextRenderParamsLock = new();
	private RenderParams? _nextRenderParams;

	void IXamlRootHost.InvalidateRender()
	{
		if (_renderer is not null && (this as IXamlRootHost).RootElement is { XamlRoot.LastRenderedFrame: { } lastRenderedFrame } rootElement)
		{
			using var lockDisposable = X11Helper.XLock(TopX11Window.Display);
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(TopX11Window.Display, TopX11Window.Window, ref attributes);

			var scale = rootElement?.XamlRoot is { } root
				? root.RasterizationScale
				: 1;

			lock (_nextRenderParamsLock)
			{
				_nextRenderParams = new RenderParams(lastRenderedFrame.frame, lastRenderedFrame.nativeElementClipPath, (float)scale);
			}

			if (Interlocked.Exchange(ref _renderingScheduled, 1) == 0)
			{
				_renderingEventLoop.Schedule(() =>
				{
					Volatile.Write(ref _renderingScheduled, 0);
					if (_nextRenderParams is { } renderParams)
					{
						_renderer.Render(renderParams.Picture, renderParams.NativeClippingPath, renderParams.Scale);
					}
				});
			}
		}
	}

	private readonly record struct RenderParams(
		SKPicture Picture,
		SKPath NativeClippingPath,
		float Scale);
}
