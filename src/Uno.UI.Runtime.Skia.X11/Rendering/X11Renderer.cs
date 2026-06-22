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
	private readonly RetainedLayer _retainedLayer = new();
	private X11AirspaceRenderHelper? _airspaceHelper;
	private readonly IXamlRootHost _host;
	protected readonly X11Window _x11Window;

	protected X11Renderer(IXamlRootHost host, X11Window x11Window)
	{
		_host = host;
		_x11Window = x11Window;
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	protected virtual bool UsesRetainedLayer => false;

	protected virtual GRContext? GpuContext => null;

	protected void DisposeRetainedLayer() => _retainedLayer.Dispose();

	public void Render()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		var display = _x11Window.Display;
		var window = _x11Window.Window;

		if (_host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}

		using (X11Helper.XLock(display))
		{
			MakeCurrent();
		}

		var useLayer = UsesRetainedLayer;
		var renderCanvas = useLayer ? _retainedLayer.Surface?.Canvas : _surface?.Canvas;

		var nativeElementClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(renderCanvas, size =>
		{
			_surface?.Dispose();
			SKSurface renderSurface;
			using (X11Helper.XLock(display))
			{
				_surface = UpdateSize((int)size.Width, (int)size.Height);
				renderSurface = useLayer
					? _retainedLayer.EnsureSurface(GpuContext!, (int)size.Width, (int)size.Height, _background)
					: _surface;
			}
			if (!useLayer)
			{
				renderSurface.Canvas.Clear(_background);
			}
			_airspaceHelper?.Dispose();
			_airspaceHelper = new X11AirspaceRenderHelper(display, window, (int)size.Width, (int)size.Height);
			return renderSurface.Canvas;
		});

		if (useLayer && _surface is { } surface)
		{
			_retainedLayer.Present(surface);
		}

		_airspaceHelper?.XShapeClip(nativeElementClipPath);

		using (X11Helper.XLock(display))
		{
			Flush();
			_ = XLib.XFlush(display);
		}
	}

	protected abstract SKSurface UpdateSize(int width, int height);
	protected virtual void MakeCurrent() { }
	protected abstract void Flush();
	public abstract void Dispose();
}
