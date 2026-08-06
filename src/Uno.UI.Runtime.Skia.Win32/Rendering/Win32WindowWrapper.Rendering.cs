using Uno.UI.Composition.Drawing;
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

	/// <summary>EXPERIMENTAL opt-in (Win32RenderingBackend.WebGpu). Set before the window is created.</summary>
	internal static bool PreferWebGpu;
	// Non-null when the WebGPU backend is active: renders through the shared swapchain context instead of an SKSurface.
	private global::Uno.UI.Composition.WebGpu.WebGpuSwapChainContext? _webgpuContext;

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

		if (_webgpuContext is { } webgpu)
		{
			// WebGPU renders into the HWND swapchain (no SKSurface). Present happens in Win32WebGpuRenderer.CopyPixels.
			var webgpuClip = ct.OnNativePlatformFrameRequested(
				null,
				size => webgpu.AcquireRenderTarget((int)size.Width, (int)size.Height));
			if (!PInvoke.GetClientRect(_hwnd, out RECT webgpuRect))
			{
				this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
				return null;
			}
			return (SkiaGeometryInterop.ToSKPath(webgpuClip), webgpuRect.Width, webgpuRect.Height);
		}

		var clipGeometry = ct.OnNativePlatformFrameRequested(_surface is null ? null : new SkiaRenderTarget(_surface.Canvas), size =>
		{
			_surface?.Dispose();
			_surface = _renderer.UpdateSize((int)size.Width, (int)size.Height);
			return new SkiaRenderTarget(_surface.Canvas);
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

		return (SkiaGeometryInterop.ToSKPath(clipGeometry), clientRect.Width, clientRect.Height);
	}

	/// <summary>
	/// Bridges the neutral WebGPU swapchain context to the Win32 render thread's <see cref="IRenderer"/> contract.
	/// WebGPU renders through the context (not an SKSurface), so UpdateSize is unused; CopyPixels presents the
	/// swapchain (wgpuSurfacePresent). EXPERIMENTAL — not runtime-validated on Linux CI (needs a real Windows GPU).
	/// </summary>
	private sealed class Win32WebGpuRenderer : IRenderer
	{
		private readonly global::Uno.UI.Composition.WebGpu.WebGpuSwapChainContext _context;

		public Win32WebGpuRenderer(global::Uno.UI.Composition.WebGpu.WebGpuSwapChainContext context) => _context = context;

		public void StartPaint() { }
		public void EndPaint() { }
		public SKSurface UpdateSize(int width, int height)
			=> throw new NotSupportedException("The WebGPU renderer presents through the swapchain context, not an SKSurface.");
		public void CopyPixels(int width, int height) => _context.Present();
		public bool IsSoftware() => false;
		public void Reinitialize() { }
		public void UpdateRefreshRate(double fps) { }
		public void Dispose() => _context.Dispose();
	}
}
