using Windows.Foundation;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal abstract class X11Renderer(IXamlRootHost host, X11Window x11Window)
{
	private int _renderCount;
	private SKColor _background = SKColors.White;
	private Size _lastSize;
	private SKSurface? _surface;
	private X11AirspaceRenderHelper? _airspaceHelper;
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();

	public void SetBackgroundColor(SKColor color) => _background = color;

	public void Render(SKPicture picture, SKPath nativeClippingPath, float scale)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		var display = x11Window.Display;
		var window = x11Window.Window;
		using var lockDisposable = X11Helper.XLock(display);

		if (host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}

		XWindowAttributes attributes = default;
		_ = XLib.XGetWindowAttributes(display, window, ref attributes);
		var width = attributes.width;
		var height = attributes.height;

		var newSize = new Size(width, height);

		if (newSize.IsEmpty)
		{
			return;
		}

		MakeCurrent();

		if (_lastSize != newSize || _surface is null || _airspaceHelper is null)
		{
			_lastSize = newSize;
			_surface?.Dispose();
			_surface = UpdateSize(width, height, attributes.depth);
			_airspaceHelper?.Dispose();
			_airspaceHelper = new X11AirspaceRenderHelper(display, window, width, height);
		}

		_fpsHelper.Scale = scale;

		SkiaRenderHelper.RenderPicture(
			_surface,
			picture,
			_background,
			_fpsHelper);

		_airspaceHelper.XShapeClip(nativeClippingPath);
		Flush();
		_ = XLib.XFlush(display);
		host.RootElement?.XamlRoot?.InvokeFrameRendered();
	}

	protected abstract SKSurface UpdateSize(int width, int height, int depth);
	protected virtual void MakeCurrent() { }
	protected abstract void Flush();
}
