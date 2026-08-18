using Uno.UI.Composition.Drawing;
using System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal partial class Win32WindowWrapper
{
	private RenderThread? _renderThread;

	/// <summary>EXPERIMENTAL opt-in (Win32RenderingBackend.WebGpu). Set before the window is created.</summary>
	internal static bool PreferWebGpu;

	// The negotiated graphics context that drives the per-frame loop (AcquireRenderTarget → render → Present).
	private ISwapChain _context = null!;

	public event EventHandler<IGeometry>? RenderingNegativePathReevaluated; // not necessarily changed

	// Wake the render thread directly rather than via InvalidateRect/WM_PAINT. A synthesized
	// WM_PAINT is the lowest-priority Win32 message, so the dispatcher's own posted messages
	// (e.g. a WaitForIdle loop) outrank it in GetMessage and can starve it indefinitely —
	// freezing the present and any per-present animation tick. OS-driven repaints
	// (resize/uncover/show) still arrive through WM_PAINT. SignalNewFrame coalesces bursts.
	void IXamlRootHost.InvalidateRender() => _renderThread?.SignalNewFrame();

	/// <summary>
	/// Creates the context for a negotiated kind, reusing the already-created kind-agnostic HWND. A kind returns
	/// null when unavailable or disabled, so negotiation falls through to the next (ultimately Software).
	/// </summary>
	private ISwapChain? CreateWindowAndContext(GraphicsContextKind kind)
	{
		var scale = (float)(RasterizationScale == 0 ? 1 : RasterizationScale);
		return kind switch
		{
			GraphicsContextKind.OpenGL => (FeatureConfiguration.Rendering.UseOpenGLOnWin32 ?? true)
				? Win32OpenGLGraphicsContext.TryCreate(_hwnd)
				: null,
			GraphicsContextKind.Vulkan => TryCreateVulkan(),
			GraphicsContextKind.Software => new Win32SoftwareGraphicsContext(_hwnd),
			GraphicsContextKind.WebGpu => global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateWin32(_hwnd, Win32Helper.GetModuleHInstance(), scale),
			_ => null,
		};
	}

	/// <summary>
	/// Vulkan is opt-in on Win32: decline when off, and swallow a creation failure so negotiation falls through.
	/// </summary>
	private ISwapChain? TryCreateVulkan()
	{
		if (!FeatureConfiguration.Rendering.UseVulkanOnWin32)
		{
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
	}

	private void InitializeRenderThread()
	{
		_renderThread = new RenderThread(
			drawFrame: DrawFrame,
			onClipPathUpdated: clipPath =>
			{
				NativeDispatcher.Main.Enqueue(() =>
					RenderingNegativePathReevaluated?.Invoke(this, clipPath),
					NativeDispatcherPriority.Normal);
			});
	}

	/// <summary>
	/// Called on the render thread. Drives the frame through the shared loop and returns the native-element clip
	/// path, or null when there is no frame to present yet.
	/// </summary>
	private IGeometry? DrawFrame()
	{
		var ct = ((IXamlRootHost)this).RootElement?.Visual.CompositionTarget as CompositionTarget;
		if (ct is null || _rendererDisposed)
		{
			return null;
		}

		if (!PInvoke.GetClientRect(_hwnd, out _))
		{
			this.LogError()?.Error($"{nameof(PInvoke.GetClientRect)} failed: {Win32Helper.GetErrorMessage()}");
			return null;
		}

		return ct.OnNativePlatformFrameRequested(_context);
	}
}
