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
<<<<<<< HEAD
		_renderer.Reinitialize();
		_surface?.Dispose();
		_surface = null;
=======
		var scale = (float)(RasterizationScale == 0 ? 1 : RasterizationScale);
		return kind switch
		{
			GraphicsContextKind.OpenGL => Win32OpenGLGraphicsContext.TryCreate(_hwnd),
			GraphicsContextKind.Vulkan => TryCreateVulkan(),
			GraphicsContextKind.Software => new Win32SoftwareGraphicsContext(_hwnd),
			GraphicsContextKind.WebGpu => global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateWin32(_hwnd, Win32Helper.GetModuleHInstance(), scale),
			_ => null,
		};
	}

	/// <summary>
	/// Swallows a Vulkan creation failure so negotiation falls through to the next kind.
	/// </summary>
	private ISwapChain? TryCreateVulkan()
	{
		if (ShouldSuppressVulkanForBackdrop())
		{
			_vulkanSuppressedForBackdrop = true;
			this.LogInfo()?.Info("Skipping Vulkan: a system backdrop is active, and Vulkan cannot present a translucent window on Win32.");
			return null;
		}

		try
		{
			return new Win32VulkanGraphicsContext(_hwnd);
		}
		catch (Exception e)
		{
			this.LogInfo()?.Info($"Vulkan context creation failed ({e.Message}); falling through.");
			return null;
		}
>>>>>>> 6808899 (fix(win32): Show system backdrops on the default renderer)
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
