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
	private SKSurface? _layerSurface;
	private X11AirspaceRenderHelper? _airspaceHelper;
	private readonly IXamlRootHost _host;
	protected readonly X11Window _x11Window;

	protected X11Renderer(IXamlRootHost host, X11Window x11Window)
	{
		_host = host;
		_x11Window = x11Window;
	}

	public void SetBackgroundColor(SKColor color) => _background = color;

	/// <summary>
	/// True if this renderer's surface preserves the previous frame's pixels between presents, so the
	/// composition layer may repaint only the dirty region. Overridden by the software renderer (which
	/// keeps a persistent backing bitmap). GPU swapchain renderers leave this false until the retained
	/// layer is implemented.
	/// </summary>
	protected virtual bool SurfaceRetainsContents => false;

	/// <summary>
	/// True if this renderer presents through a persistent offscreen layer (used by GPU swapchain
	/// renderers): the frame is rendered onto the retained layer, which is then blitted to the
	/// (non-retaining) window surface each frame. Lets dirty-rectangles work on a swapchain without
	/// per-driver buffer-age handling. Requires <see cref="SurfaceRetainsContents"/> to also be true.
	/// </summary>
	protected virtual bool UsesRetainedLayer => false;

	/// <summary>Creates the persistent offscreen layer surface (GPU-backed) for <see cref="UsesRetainedLayer"/>.</summary>
	protected virtual SKSurface CreateRetainedLayer(int width, int height) => throw new NotSupportedException();

	protected void DisposeRetainedLayer()
	{
		_layerSurface?.Dispose();
		_layerSurface = null;
	}

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

		// In dirty-rectangles mode the render target must retain the previous frame outside the changed
		// region, so we must NOT clear it here; the clipped clear happens in Draw().
		var dirtyRectanglesActive = global::Uno.UI.FeatureConfiguration.Rendering.EnableDirtyRectangles && SurfaceRetainsContents;
		var useLayer = dirtyRectanglesActive && UsesRetainedLayer;

		if (!dirtyRectanglesActive)
		{
			_surface?.Canvas.Clear(_background);
		}

		SKCanvas? renderCanvas;
		Func<Size, SKCanvas> resizeFunc;
		if (useLayer)
		{
			// Draw renders onto the persistent layer (which preserves the previous frame); the layer is
			// blitted to the window surface after.
			renderCanvas = _layerSurface?.Canvas;
			resizeFunc = size =>
			{
				_surface?.Dispose();
				_layerSurface?.Dispose();
				using (X11Helper.XLock(display))
				{
					_surface = UpdateSize((int)size.Width, (int)size.Height);
					_layerSurface = CreateRetainedLayer((int)size.Width, (int)size.Height);
				}
				_layerSurface.Canvas.Clear(_background);
				_airspaceHelper?.Dispose();
				_airspaceHelper = new X11AirspaceRenderHelper(display, window, (int)size.Width, (int)size.Height);
				return _layerSurface.Canvas;
			};
		}
		else
		{
			renderCanvas = _surface?.Canvas;
			resizeFunc = size =>
			{
				_surface?.Dispose();
				using (X11Helper.XLock(display))
				{
					_surface = UpdateSize((int)size.Width, (int)size.Height);
				}
				_surface.Canvas.Clear(_background);
				_airspaceHelper?.Dispose();
				_airspaceHelper = new X11AirspaceRenderHelper(display, window, (int)size.Width, (int)size.Height);
				return _surface.Canvas;
			};
		}

		var nativeElementClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(renderCanvas, resizeFunc, SurfaceRetainsContents);

		if (useLayer && _layerSurface is { } layer && _surface is { } surface)
		{
			// Blit the whole retained layer onto the (non-retaining) window surface each frame, then swap.
			layer.Draw(surface.Canvas, 0, 0, null);
			surface.Canvas.Flush();
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
