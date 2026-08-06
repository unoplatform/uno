#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// On-canvas WebGPU <see cref="IGraphicsContext"/> for the browser: owns a <see cref="WebGpuDevice"/> and a wgpu
/// surface bound to the HTML &lt;canvas&gt; (via emdawnwebgpu's canvas-selector source), so the WebGPU backend
/// renders straight into the acquired surface texture and presents it. The device is created asynchronously
/// (the browser cannot block on requestAdapter/requestDevice) via <see cref="WebGpuDeviceAsync.CreateAsync"/>
/// by the caller and handed to the constructor. Mirrors X11WebGpuGraphicsContext; the only browser-specific
/// parts are the async device init and the canvas-selector surface.
/// </summary>
internal sealed unsafe class WebGpuBrowserGraphicsContext : IGraphicsContext, IWebGpuDeviceContext
{
	private readonly WebGpuDevice _device;
	private IntPtr _surface;
	private WebGpuRenderSurface? _target;
	private IntPtr _currentTexture;
	private IntPtr _currentView;
	private int _w, _h;
	private bool _configured;

	// Takes an already-created device (the caller awaits WebGpuDeviceAsync.CreateAsync — this class is unsafe,
	// which forbids await). The surface is created synchronously here.
	public WebGpuBrowserGraphicsContext(WebGpuDevice device, string canvasId)
	{
		_device = device;
		CreateSurface(canvasId);
	}

	private void CreateSurface(string canvasId)
	{
		var selector = Utf8("#" + canvasId);
		var canvas = new WGPUEmscriptenSurfaceSourceCanvasHTMLSelector
		{
			Chain = new WGPUChainedStruct { SType = SType_EmscriptenSurfaceSourceCanvasHTMLSelector },
			Selector = selector,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&canvas };
		_surface = wgpuInstanceCreateSurface(_device.Inst, &desc);
		if (_surface == IntPtr.Zero)
		{
			throw new InvalidOperationException("Failed to create a wgpu canvas surface.");
		}
	}

	// Persistent UTF-8 for a WGPUStringView (the selector lives only for the create call, but keeping it simple).
	private static WGPUStringView Utf8(string s)
		=> new() { Data = Marshal.StringToCoTaskMemUTF8(s), Length = (nuint)System.Text.Encoding.UTF8.GetByteCount(s) };

	public WebGpuDevice Device => _device;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;
	public bool IsLost => false;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		Configure(width, height);

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
		_currentView = IntPtr.Zero;
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
