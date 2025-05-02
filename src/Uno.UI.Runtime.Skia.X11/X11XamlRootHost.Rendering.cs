using System.Threading;
using Microsoft.UI.Dispatching;
using SkiaSharp;
using Uno.Helpers;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
namespace Uno.WinUI.Runtime.Skia.X11;

internal partial class X11XamlRootHost
{
	private readonly EventLoop _renderingEventLoop;
	private bool _renderingScheduled;

	void IXamlRootHost.InvalidateRender()
	{
		if (DispatcherQueue.Main.HasThreadAccess)
		{
			var canvas = _recorder.BeginRecording(new SKRect(-999999, -999999, 999999, 999999));
			using (new SKAutoCanvasRestore(canvas, true))
			{
				var rootElement = (this as IXamlRootHost).RootElement;
				if (_renderer is not null && rootElement?.Visual is { } rootVisual)
				{
					using var lockDisposable = X11Helper.XLock(TopX11Window.Display);
					XWindowAttributes attributes = default;
					_ = XLib.XGetWindowAttributes(TopX11Window.Display, TopX11Window.Window, ref attributes);
					var width = attributes.width;
					var height = attributes.height;
					var nativeClippingPath = SkiaRenderHelper.RenderRootVisualAndReturnPath(width, height, rootVisual, canvas);
					var picture = _recorder.EndRecording();

					var scale = rootElement?.XamlRoot is { } root
						? root.RasterizationScale
						: 1;

					if (!Interlocked.Exchange(ref _renderingScheduled, true))
					{
						_renderingEventLoop.Schedule(() =>
						{
							_renderingScheduled = false;
							_renderer.Render(picture, nativeClippingPath, (float)scale, (float)scale);
						});
					}
				}
			}
		}
		else
		{
			// We need to be on the dispatcher to render the UI
			// Requeue to request a full update (including possible
			// window size changes)
			NativeDispatcher.Main.DispatchRendering();
		}
	}
}
