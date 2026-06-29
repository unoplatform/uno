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

	// Wake the render thread directly rather than via InvalidateRect/WM_PAINT. A synthesized
	// WM_PAINT is the lowest-priority Win32 message, so the dispatcher's own posted messages
	// (e.g. a WaitForIdle loop) outrank it in GetMessage and can starve it indefinitely —
	// freezing the present and any per-present animation tick. OS-driven repaints
	// (resize/uncover/show) still arrive through WM_PAINT. SignalNewFrame coalesces bursts.
	void IXamlRootHost.InvalidateRender() => _renderThread?.SignalNewFrame();

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
	/// Called on the render thread. Replays the last recorded SKPicture and returns the clip
	/// path and client dimensions for CopyPixels, or null when there is no frame to present
	/// yet (avoids presenting an uninitialised back buffer before the first render).
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

		// _surface is created lazily inside resizeFunc; still null means the CompositionTarget
		// has not recorded anything yet — nothing to present.
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
