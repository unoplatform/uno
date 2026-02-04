using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal abstract class X11Renderer : IDisposable
{
	private int _renderCount;
	private SKColor _background = SKColors.White;
	private SKSurface? _surface;
	private X11AirspaceRenderHelper? _airspaceHelper;
	private readonly IXamlRootHost _host;
	protected readonly X11Window _x11Window;

	protected X11Renderer(IXamlRootHost host, X11Window x11Window)
	{
		_host = host;
		_x11Window = x11Window;
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	public void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		var display = _x11Window.Display;
		var window = _x11Window.Window;
		using var lockDisposable = X11Helper.XLock(display);

		if (_host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}

		MakeCurrent();

		_surface?.Canvas.Clear(_background);
		var nativeElementClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			_surface = UpdateSize((int)size.Width, (int)size.Height);
			_surface.Canvas.Clear(_background);
			_airspaceHelper?.Dispose();
			_airspaceHelper = new X11AirspaceRenderHelper(display, window, (int)size.Width, (int)size.Height);
			return _surface.Canvas;
		});

		_airspaceHelper?.XShapeClip(nativeElementClipPath);
		Flush();
		_ = XLib.XFlush(display);
	}

	protected abstract SKSurface UpdateSize(int width, int height);
	protected virtual void MakeCurrent() { }
	protected abstract void Flush();
	public abstract void Dispose();
}
