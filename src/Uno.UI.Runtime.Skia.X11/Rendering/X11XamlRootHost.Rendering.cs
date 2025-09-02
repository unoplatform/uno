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
	private int _renderingScheduled;

	void IXamlRootHost.InvalidateRender()
	{
		if (DispatcherQueue.Main.HasThreadAccess)
		{
			var rootElement = (this as IXamlRootHost).RootElement;
			if (!SkiaRenderHelper.CanRecordPicture(rootElement))
			{
				rootElement?.XamlRoot?.QueueInvalidateRender();
				return;
			}

			XamlRootMap.GetRootForHost(this)?.VisualTree.ContentRoot.CompositionTarget.PaintFrame();

			if (Interlocked.Exchange(ref _renderingScheduled, 1) == 0)
			{
				_renderingEventLoop.Schedule(() =>
				{
					Volatile.Write(ref _renderingScheduled, 0);
					_renderer?.Render();
				});
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
