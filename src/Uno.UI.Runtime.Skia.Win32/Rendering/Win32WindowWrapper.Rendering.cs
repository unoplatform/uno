using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.OpenGL;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private SKSurface? _surface;
	private RenderThread? _renderThread;

	public event EventHandler<SKPath>? RenderingNegativePathReevaluated; // not necessarily changed

	bool IXamlRootHost.SupportsRenderThrottle => true;

	unsafe void IXamlRootHost.InvalidateRender()
	{
		// Mark the window dirty for WM_PAINT. This handles external repaints (window
		// uncovering, restore from minimized) where Windows generates WM_PAINT.
		// Like WPF (InvalidateRect in HwndTarget) and Avalonia (InvalidateRect in WindowImpl).
		PInvoke.InvalidateRect(_hwnd, default(RECT*), false);

		// Signal the render thread to present the current frame.
		_renderThread?.SignalNewFrame();
	}

	private void ReinitializeRenderer()
	{
		_renderer.Reinitialize();
		_surface?.Dispose();
		_surface = null;
	}

	private void InitializeRenderThread()
	{
		// TryCreateGlRenderer leaves the GL context current on the UI thread
		// (WglCurrentContextDisposable doesn't restore to "no context" when there
		// was none before). Detach it so the render thread can make it current.
		// No-op for the software renderer (no GL context to detach).
		PInvoke.wglMakeCurrent(default, HGLRC.Null);

		_renderThread = new RenderThread(
			_renderer,
			drawFrame: DrawFrame,
			onClipPathUpdated: clipPath =>
			{
				NativeDispatcher.Main.Enqueue(() =>
					RenderingNegativePathReevaluated?.Invoke(this, clipPath),
					NativeDispatcherPriority.Normal);
			},
			onFramePresented: () =>
			{
				NativeDispatcher.Main.Enqueue(
					OnFramePresented,
					NativeDispatcherPriority.Render);
			});
	}

	/// <summary>
	/// Called on the render thread. Replays the last recorded SKPicture to
	/// the canvas and returns the clip path and client dimensions for CopyPixels.
	/// </summary>
	private unsafe (SKPath clipPath, int width, int height)? DrawFrame()
	{
		var ct = ((IXamlRootHost)this).RootElement?.Visual.CompositionTarget as CompositionTarget;
		if (ct is null || _rendererDisposed)
		{
			return null;
		}

		var clipPath = ct.OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_surface?.Dispose();
			_surface = _renderer.UpdateSize((int)size.Width, (int)size.Height);
			return _surface.Canvas;
		});

		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			return null;
		}

		return (clipPath, clientRect.Width, clientRect.Height);
	}

	/// <summary>
	/// Posted to the UI thread by the render thread after a frame is presented.
	/// Clears the throttle so the next FrameTick can be scheduled.
	/// </summary>
	private void OnFramePresented()
	{
		var ct = ((IXamlRootHost)this).RootElement?.Visual.CompositionTarget as CompositionTarget;
		ct?.OnFramePresented();
	}
}
