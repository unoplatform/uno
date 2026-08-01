#nullable enable

using System;
using Silk.NET.WebGPU;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;

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
	private Surface* _surface;
	private WebGpuRenderSurface? _target;   // owns the depth/stencil + MSAA; color View set per frame
	private Texture* _currentTexture;
	private TextureView* _currentView;
	private int _w, _h;
	private bool _configured;

	public X11WebGpuGraphicsContext(X11Window x11Window)
	{
		_x11Window = x11Window;

		// Swapchain surfaces here (lavapipe/X11) expose Bgra8Unorm, not Rgba8Unorm — build the backend pipelines
		// for Bgra8Unorm to match the swapchain image (avoids a color-format validation error).
		_device = new WebGpuDevice(TextureFormat.Bgra8Unorm);

		var xlib = new SurfaceDescriptorFromXlibWindow
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromXlibWindow },
			Display = (void*)_x11Window.Display,
			Window = (ulong)_x11Window.Window,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&xlib };
		_surface = _device.W.InstanceCreateSurface(_device.Inst, ref desc);
		if (_surface is null)
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
		if (_currentView is not null)
		{
			return _target!;
		}

		SurfaceTexture st = default;
		_device.W.SurfaceGetCurrentTexture(_surface, ref st);
		if (st.Status != SurfaceGetCurrentTextureStatus.Success || st.Texture is null)
		{
			// Backbuffer not ready this tick; hand back the target with its previous view (present will no-op).
			return _target!;
		}

		_currentTexture = st.Texture;
		_currentView = _device.W.TextureCreateView(st.Texture, null);
		_target!.View = _currentView;
		return _target;
	}

	public void Present()
	{
		if (_currentView is null)
		{
			return;
		}
		_device.W.SurfacePresent(_surface);
		_device.W.TextureViewRelease(_currentView);
		_device.W.TextureRelease(_currentTexture);
		_currentView = null;
		_currentTexture = null;
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		_currentView = null;   // any pending acquisition is invalidated by reconfiguring the surface
		_currentTexture = null;
		_target?.Dispose();
		_target = new WebGpuRenderSurface(_device, width, height, externalColor: true);

		SurfaceCapabilities caps = default;
		_device.W.SurfaceGetCapabilities(_surface, _device.Adapter, ref caps);
		var format = _device.ColorFormat;
		bool supported = false;
		for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { supported = true; break; } }
		if (!supported && caps.FormatCount > 0) { format = caps.Formats[0]; }
		var alphaMode = caps.AlphaModeCount > 0 ? caps.AlphaModes[0] : CompositeAlphaMode.Auto;

		var cfg = new SurfaceConfiguration
		{
			Device = _device.Dev,
			Format = format,
			Usage = TextureUsage.RenderAttachment,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = PresentMode.Fifo,
			AlphaMode = alphaMode,
		};
		_device.W.SurfaceConfigure(_surface, ref cfg);
		_configured = true;
	}

	public void Dispose()
	{
		_target?.Dispose();
		if (_surface is not null) { _device.W.SurfaceRelease(_surface); _surface = null; }
		_device.Dispose();
	}
}
