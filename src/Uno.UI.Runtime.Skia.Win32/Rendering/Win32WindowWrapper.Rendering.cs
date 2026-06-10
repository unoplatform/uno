using System;
using Windows.Win32;
using Windows.Win32.Foundation;
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

	unsafe void IXamlRootHost.InvalidateRender()
	{
		// Mark the window dirty and let Windows coalesce it into a WM_PAINT, whose handler
		// signals the render thread to present. Presenting here too would double-present the
		// same content (and re-block on VSync for GL), so WM_PAINT is the single present path,
		// like WPF's HwndTarget (InvalidateRect here, present on WM_PAINT).
		if (!PInvoke.InvalidateRect(_hwnd, default(RECT*), false))
		{
			this.LogError()?.Error($"{nameof(PInvoke.InvalidateRect)} failed: {Win32Helper.GetErrorMessage()}");
		}
	}

	private void ReinitializeRenderer()
	{
		_renderer.Reinitialize();
		_surface?.Dispose();
		_surface = null;
	}

	private void InitializeRenderThread()
	{
		_renderThread = new RenderThread(
			_renderer,
			drawFrame: DrawFrame,
			onClipPathUpdated: clipPath =>
			{
				NativeDispatcher.Main.Enqueue(() =>
					RenderingNegativePathReevaluated?.Invoke(this, clipPath),
					NativeDispatcherPriority.Normal);
			});
	}

	/// <summary>
	/// Called on the render thread. Replays the last recorded SKPicture to
	/// the canvas and returns the clip path and client dimensions for CopyPixels.
	/// Returns null when there is no frame to present yet — this avoids a wasted
	/// CopyPixels (BitBlt + DwmFlush, or SwapBuffers presenting an uninitialised
	/// back buffer) for WM_PAINT messages that arrive before the first synchronous
	/// render.
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

		// _surface is created lazily inside the resizeFunc only when there's an actual
		// frame to draw. If it's still null, the CompositionTarget has not recorded
		// anything yet — nothing to present.
		if (_surface is null)
		{
			return null;
		}

		if (!PInvoke.GetClientRect(_hwnd, out RECT clientRect))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return null;
		}

		return (clipPath, clientRect.Width, clientRect.Height);
	}
}
