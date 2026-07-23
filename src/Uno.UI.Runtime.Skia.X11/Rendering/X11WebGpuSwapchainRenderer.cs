using System;
using Microsoft.UI.Xaml.Media;
using Silk.NET.WebGPU;
using Uno.Foundation.Logging;
using Uno.UI.Composition.WebGpu;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

// WebGPU X11 renderer using a real wgpu SWAPCHAIN (VK_KHR_xlib_surface): renders directly into the
// acquired surface texture and presents it — no offscreen readback / XPutImage. Selected by
// UNO_WEBGPU=swapchain. (Requires WSI support from the Vulkan driver; software lavapipe may or may not.)
internal sealed unsafe class X11WebGpuSwapchainRenderer : X11Renderer
{
	private readonly WebGpuDevice _device;
	private readonly WebGpuRenderBackend _backend;
	private Silk.NET.WebGPU.Surface* _surface;
	private WebGpuRenderSurface? _target; // owns the depth/stencil; color View set per frame
	private int _w, _h;
	private bool _configured;

	public X11WebGpuSwapchainRenderer(IXamlRootHost host, X11Window x11Window) : base(host, x11Window)
	{
		// Swapchain surfaces here (lavapipe/X11) expose Bgra8Unorm(Srgb), not Rgba8Unorm — so build the
		// backend pipelines for Bgra8Unorm to match the swapchain image (avoids a color-format validation error).
		_device = new WebGpuDevice(TextureFormat.Bgra8Unorm);
		_backend = new WebGpuRenderBackend(_device);
		CompositionTarget.RenderBackend = _backend;

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

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info("X11WebGpuSwapchainRenderer: wgpu Xlib surface + swapchain present");
		}
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		_target?.Dispose();
		_target = new WebGpuRenderSurface(_device, width, height, externalColor: true);

		SurfaceCapabilities caps = default;
		_device.W.SurfaceGetCapabilities(_surface, _device.Adapter, ref caps);
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			var fmts = new System.Text.StringBuilder();
			for (nuint i = 0; i < caps.FormatCount; i++) { fmts.Append(caps.Formats[i]).Append(' '); }
			var alphas = new System.Text.StringBuilder();
			for (nuint i = 0; i < caps.AlphaModeCount; i++) { alphas.Append(caps.AlphaModes[i]).Append(' '); }
			this.Log().Info($"wgpu surface caps: formats=[{fmts}] alphaModes=[{alphas}]");
		}

		// Configure with the device's pipeline format (Bgra8Unorm) if the surface offers it; else its preferred.
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

	public override void Render()
	{
		if (_host is X11XamlRootHost { Closed.IsCompleted: true })
		{
			return;
		}
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
		{
			return;
		}

		XWindowAttributes attr = default;
		using (X11Helper.XLock(_x11Window.Display))
		{
			_ = XLib.XGetWindowAttributes(_x11Window.Display, _x11Window.Window, ref attr);
		}
		Configure(Math.Max(1, attr.width), Math.Max(1, attr.height));

		SurfaceTexture st = default;
		_device.W.SurfaceGetCurrentTexture(_surface, ref st);
		if (st.Status != SurfaceGetCurrentTextureStatus.Success || st.Texture is null)
		{
			return; // backbuffer not ready this tick
		}
		var view = _device.W.TextureCreateView(st.Texture, null);
		_target!.View = view;

		_ = compositionTarget.OnNativePlatformFrameRequested(_target, _ => _target!);

		_device.W.SurfacePresent(_surface);
		_device.W.TextureViewRelease(view);
		_device.W.TextureRelease(st.Texture);
	}

	protected override SkiaSharp.SKSurface UpdateSize(int width, int height) => throw new NotSupportedException("X11WebGpuSwapchainRenderer does not use SKSurface.");

	protected override void Flush() { }

	public override void Dispose()
	{
		_target?.Dispose();
		if (_surface is not null) { _device.W.SurfaceRelease(_surface); _surface = null; }
		_device.Dispose();
	}
}
