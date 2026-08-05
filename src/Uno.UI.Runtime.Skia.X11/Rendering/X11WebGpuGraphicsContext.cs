#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// On-window WebGPU <see cref="IGraphicsContext"/> for X11: owns a <see cref="WebGpuDevice"/> and a real wgpu
/// swapchain (VK_KHR_xlib_surface), so the WebGPU backend renders straight into the acquired surface texture and
/// presents it (no offscreen readback / XPutImage). <see cref="AcquireRenderTarget"/> configures the surface and
/// hands the backend the current backbuffer as a neutral <see cref="IRenderTarget"/>; <see cref="Present"/> swaps.
/// The device is exposed via <see cref="IWebGpuDeviceContext"/> so the neutral provider needs no X11 type.
/// Requires WSI support from the Vulkan driver (software lavapipe may or may not provide it).
/// </summary>
internal sealed unsafe class X11WebGpuGraphicsContext : IGraphicsContext, IWebGpuDeviceContext
{
	private readonly X11Window _x11Window;
	private readonly WebGpuDevice _device;
	private IntPtr _surface;
	private WebGpuRenderSurface? _target;   // owns the depth/stencil + MSAA; color View set per frame
	private IntPtr _currentTexture;
	private IntPtr _currentView;
	private int _w, _h;
	private bool _configured;

	public X11WebGpuGraphicsContext(X11Window x11Window)
	{
		_x11Window = x11Window;

		// Swapchain surfaces here (lavapipe/X11) expose Bgra8Unorm, not Rgba8Unorm — build the backend pipelines
		// for Bgra8Unorm to match the swapchain image (avoids a color-format validation error).
		_device = new WebGpuDevice(WGPUTextureFormat.BGRA8Unorm);

		var xlib = new WGPUSurfaceSourceXlibWindow
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceXlibWindow },
			Display = (IntPtr)_x11Window.Display,
			Window = (ulong)_x11Window.Window,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&xlib };
		_surface = wgpuInstanceCreateSurface(_device.Inst, &desc);
		if (_surface == IntPtr.Zero)
		{
			throw new InvalidOperationException("Failed to create a wgpu Xlib surface.");
		}
	}

	public WebGpuDevice Device => _device;

	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		Configure(width, height);

		// A swapchain image can be acquired only once per present. The neutral loop may call this more than once
		// per frame (e.g. a resize callback) — return the already-acquired target until Present() releases it.
		if (_currentView != IntPtr.Zero)
		{
			return _target!;
		}

		WGPUSurfaceTexture st = default;
		wgpuSurfaceGetCurrentTexture(_surface, &st);
		if ((st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal
				&& st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal)
			|| st.Texture == IntPtr.Zero)
		{
			// Backbuffer not ready this tick; hand back the target with its previous view (present will no-op).
			return _target!;
		}

		_currentTexture = st.Texture;
		_currentView = wgpuTextureCreateView(st.Texture, null);
		_target!.View = _currentView;
		return _target;
	}

	public void Present()
	{
		if (_currentView == IntPtr.Zero)
		{
			return;
		}
		wgpuSurfacePresent(_surface);
		wgpuTextureViewRelease(_currentView);
		wgpuTextureRelease(_currentTexture);
		_currentView = IntPtr.Zero;
		_currentTexture = IntPtr.Zero;
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		_currentView = IntPtr.Zero;   // any pending acquisition is invalidated by reconfiguring the surface
		_currentTexture = IntPtr.Zero;
		_target?.Dispose();
		_target = new WebGpuRenderSurface(_device, width, height, externalColor: true);

		WGPUSurfaceCapabilities caps = default;
		wgpuSurfaceGetCapabilities(_surface, _device.Adapter, &caps);
		var format = _device.ColorFormat;
		bool supported = false;
		for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { supported = true; break; } }
		if (!supported && caps.FormatCount > 0) { format = caps.Formats[0]; }
		var alphaMode = caps.AlphaModeCount > 0 ? caps.AlphaModes[0] : WGPUCompositeAlphaMode.Auto;

		var cfg = new WGPUSurfaceConfiguration
		{
			Device = _device.Dev,
			Format = format,
			Usage = WGPUTextureUsage.RenderAttachment,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = WGPUPresentMode.Fifo,
			AlphaMode = alphaMode,
		};
		wgpuSurfaceConfigure(_surface, &cfg);
		_configured = true;
	}

	public void Dispose()
	{
		_target?.Dispose();
		if (_surface != IntPtr.Zero) { wgpuSurfaceRelease(_surface); _surface = IntPtr.Zero; }
		_device.Dispose();
	}
}
