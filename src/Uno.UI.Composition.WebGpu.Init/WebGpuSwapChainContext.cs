// Shared on-window WebGPU context for NATIVE hosts (X11, Win32, macOS, …). The WebGPU swapchain semantics are
// identical across them — only the platform surface source differs — so hosts create a device + a surface (via
// the CreateXxxSurface factories) and this type owns acquire/configure/present. The browser is separate: it
// presents implicitly and blits (no wgpuSurfacePresent), so it has its own context.
#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe class WebGpuSwapChainContext : ISwapChain, IWebGpuDeviceContext
{
	private readonly WebGpuInitDevice _device;
	private IntPtr _surface;
	private WebGpuSwapchainTarget? _target;
	// The scene renders (MSAA) and resolves into this OFFSCREEN single-sample texture; Present() then blits it into
	// the acquired swapchain image. A direct MSAA-resolve straight into the swapchain texture does NOT composite on
	// several native surfaces (and SwiftShader in the browser) — a render pass that TARGETS the swapchain image (the
	// blit) does. The MSAA colour + depth the scene renders into are the RENDER BACKEND's (it allocates them to match
	// this resolve target); this host owns only the single-sample resolve texture + the present/blit.
	private IntPtr _presentTex;
	private IntPtr _presentView;
	private bool _frameAcquired;
	private int _w, _h;
	private bool _configured;
	private WGPUTextureFormat _surfaceFormat;

	// Fullscreen-triangle blit pipeline: samples _presentView and draws it into the swapchain image.
	private IntPtr _blitModule;
	private IntPtr _blitPipe;
	private IntPtr _blitBgl;
	private WGPUTextureFormat _blitFormat;   // the swapchain format _blitPipe was built for (rebuild if it changes)

	private const string BlitWgsl = @"
@group(0) @binding(0) var t: texture_2d<f32>;
@group(0) @binding(1) var s: sampler;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) i: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[i];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> { return textureSampleLevel(t, s, i.uv, 0.0); }";

	// Same blit, but for an *_Srgb swapchain format. The scene's colours are already sRGB-encoded in the plain-UNORM
	// offscreen; an sRGB render target would apply linear->sRGB a SECOND time on store (washed-out output, broken
	// acrylic). Pre-decode sRGB->linear here so the target's automatic re-encode reproduces the original value
	// (alpha is linear in sRGB formats — pass it through). Reproduces the desktop straight-copy result.
	private const string BlitWgslSrgb = @"
@group(0) @binding(0) var t: texture_2d<f32>;
@group(0) @binding(1) var s: sampler;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) i: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[i];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
fn s2l(c: f32) -> f32 { if (c <= 0.04045) { return c / 12.92; } return pow((c + 0.055) / 1.055, 2.4); }
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  let c = textureSampleLevel(t, s, i.uv, 0.0);
  return vec4<f32>(s2l(c.r), s2l(c.g), s2l(c.b), c.a);
}";

	private static bool IsSrgb(WGPUTextureFormat f)
		=> f == WGPUTextureFormat.RGBA8UnormSrgb || f == WGPUTextureFormat.BGRA8UnormSrgb;

	/// <param name="createSurface">Builds the wgpu surface for the platform window, given the instance handle
	/// (use one of the CreateXxxSurface factories).</param>
	public WebGpuSwapChainContext(WGPUTextureFormat colorFormat, Func<IntPtr, IntPtr> createSurface)
	{
		_device = new WebGpuInitDevice(colorFormat);
		_surface = createSurface(_device.Inst);
		if (_surface == IntPtr.Zero)
		{
			throw new InvalidOperationException("Failed to create the wgpu surface for this window.");
		}
	}

	// The host context IS the neutral device context (it owns the device it created); the backend adopts it.
	nint IWebGpuDeviceContext.Instance => _device.Inst;
	nint IWebGpuDeviceContext.Adapter => _device.Adapter;
	nint IWebGpuDeviceContext.Device => _device.Dev;
	nint IWebGpuDeviceContext.Queue => _device.Q;
	uint IWebGpuDeviceContext.ColorFormat => (uint)_device.ColorFormat;
	uint IWebGpuDeviceContext.SampleCount => _device.MsaaSamples;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		Configure(width, height);
		_frameAcquired = true;
		// The backend renders + resolves into _presentView (offscreen); Present() blits that to the swapchain image.
		return _target!;
	}

	public void Present()
	{
		if (!_frameAcquired || _target is null)
		{
			return;
		}
		_frameAcquired = false;
		var pr = _device.Profiler;
		var tPresent = WebGpuProfiler.T();
		EnsureBlitPipeline();

		// Acquire the swapchain image at present time (after the scene render) and blit the offscreen frame into it.
		var tAcquire = WebGpuProfiler.T();
		WGPUSurfaceTexture st = default;
		wgpuSurfaceGetCurrentTexture(_surface, &st);
		if ((st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessOptimal
				&& st.Status != WGPUSurfaceGetCurrentTextureStatus.SuccessSuboptimal)
			|| st.Texture == IntPtr.Zero)
		{
			_configured = false;   // surface lost / out of date — reconfigure next frame
			pr?.Acquire(tAcquire); pr?.Presented(tPresent); pr?.FrameEnd();
			return;
		}

		var view = wgpuTextureCreateView(st.Texture, null);
		pr?.Acquire(tAcquire);

		var tBlit = WebGpuProfiler.T();
		var entries = stackalloc WGPUBindGroupEntry[2];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = _presentView };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _device.Smp };
		var bgd = new WGPUBindGroupDescriptor { Layout = _blitBgl, EntryCount = 2, Entries = entries };
		var bg = wgpuDeviceCreateBindGroup(_device.Dev, &bgd);

		var enc = wgpuDeviceCreateCommandEncoder(_device.Dev, null);
		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = view, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);
		WebGpuTrace.Pass("present-blit", _w, _h, 1, true);
		wgpuRenderPassEncoderSetPipeline(pass, _blitPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		WebGpuTrace.Draw("blit", 3);
		wgpuRenderPassEncoderEnd(pass);
		WebGpuTrace.PassEnd();
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_device.Q, 1, (IntPtr)(&cb));
		pr?.Blit(tBlit);

		var tSurface = WebGpuProfiler.T();
		wgpuSurfacePresent(_surface);
		pr?.Surface(tSurface);

		wgpuBindGroupRelease(bg);
		wgpuCommandEncoderRelease(enc);
		wgpuTextureViewRelease(view);
		wgpuTextureRelease(st.Texture);
		pr?.Presented(tPresent);
		pr?.FrameEnd();
	}

	private void EnsureBlitPipeline()
	{
		if (_blitPipe != IntPtr.Zero && _blitFormat == _surfaceFormat)
		{
			return;
		}
		if (_blitPipe != IntPtr.Zero) { wgpuRenderPipelineRelease(_blitPipe); _blitPipe = IntPtr.Zero; }
		if (_blitModule != IntPtr.Zero) { wgpuShaderModuleRelease(_blitModule); _blitModule = IntPtr.Zero; }
		_blitFormat = _surfaceFormat;
		var code = Utf8(IsSrgb(_surfaceFormat) ? BlitWgslSrgb : BlitWgsl);
		var wgsl = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var smd = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&wgsl };
		_blitModule = wgpuDeviceCreateShaderModule(_device.Dev, &smd);

		var vs = Utf8("vs");
		var fs = Utf8("fs");
		// Target must match the configured swapchain format (not necessarily the device's offscreen format).
		var target = new WGPUColorTargetState { Format = _surfaceFormat, Blend = null, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = _blitModule, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = _blitModule, EntryPoint = vs, BufferCount = 0 },
			Fragment = &fsState,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = null,
			Layout = IntPtr.Zero,
		};
		_blitPipe = wgpuDeviceCreateRenderPipeline(_device.Dev, &pd);
		_blitBgl = wgpuRenderPipelineGetBindGroupLayout(_blitPipe, 0);
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		if (_presentView != IntPtr.Zero) { wgpuTextureViewRelease(_presentView); _presentView = IntPtr.Zero; }
		if (_presentTex != IntPtr.Zero) { wgpuTextureDestroy(_presentTex); _presentTex = IntPtr.Zero; }

		// Offscreen single-sample resolve target: the scene's MSAA pass (owned by the render backend) resolves into
		// this, and it is sampled by the present blit. TextureBinding so the blit can sample it; CopySrc so
		// ReadPixels/RenderTargetBitmap can read it.
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = _device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc,
		};
		_presentTex = wgpuDeviceCreateTexture(_device.Dev, &td);
		_presentView = wgpuTextureCreateView(_presentTex, null);
		_target = new WebGpuSwapchainTarget(_presentTex, _presentView, width, height,
			_device.ColorFormat == WGPUTextureFormat.BGRA8Unorm ? GraphicsColorFormat.Bgra8888 : GraphicsColorFormat.Rgba8888);

		WGPUSurfaceCapabilities caps = default;
		wgpuSurfaceGetCapabilities(_surface, _device.Adapter, &caps);
		_surfaceFormat = _device.ColorFormat;
		bool supported = false;
		for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == _surfaceFormat) { supported = true; break; } }
		if (!supported && caps.FormatCount > 0) { _surfaceFormat = caps.Formats[0]; }
		var alphaMode = caps.AlphaModeCount > 0 ? caps.AlphaModes[0] : WGPUCompositeAlphaMode.Auto;

		var presentMode = WGPUPresentMode.Fifo;
		var envPm = System.Environment.GetEnvironmentVariable("UNO_WEBGPU_PRESENT");
		if (!string.IsNullOrEmpty(envPm))
		{
			var want = envPm.ToLowerInvariant() switch
			{
				"mailbox" => WGPUPresentMode.Mailbox,
				"immediate" => WGPUPresentMode.Immediate,
				"fiforelaxed" => WGPUPresentMode.FifoRelaxed,
				_ => WGPUPresentMode.Fifo,
			};
			for (nuint i = 0; i < caps.PresentModeCount; i++) { if (caps.PresentModes[i] == want) { presentMode = want; break; } }
		}
		var cfg = new WGPUSurfaceConfiguration
		{
			Device = _device.Dev,
			Format = _surfaceFormat,
			Usage = WGPUTextureUsage.RenderAttachment,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = presentMode,
			AlphaMode = alphaMode,
		};
		wgpuSurfaceConfigure(_surface, &cfg);
		System.Console.WriteLine($"[webgpu] surface {width}x{height} format={_surfaceFormat} present={presentMode}");
		_configured = true;
	}

	private static WGPUStringView Utf8(string s)
		=> new() { Data = System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(s), Length = (nuint)System.Text.Encoding.UTF8.GetByteCount(s) };

	public void Dispose()
	{
		if (_presentView != IntPtr.Zero) { wgpuTextureViewRelease(_presentView); _presentView = IntPtr.Zero; }
		if (_presentTex != IntPtr.Zero) { wgpuTextureDestroy(_presentTex); _presentTex = IntPtr.Zero; }
		if (_surface != IntPtr.Zero) { wgpuSurfaceRelease(_surface); _surface = IntPtr.Zero; }
		_device.Dispose();
	}

	// ---- Platform surface factories (build the chained surface source + create the wgpu surface) ----

	public static IntPtr CreateXlibSurface(IntPtr instance, IntPtr display, ulong window)
	{
		var xlib = new WGPUSurfaceSourceXlibWindow
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceXlibWindow },
			Display = display,
			Window = window,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&xlib };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateHwndSurface(IntPtr instance, IntPtr hinstance, IntPtr hwnd)
	{
		var hwndSrc = new WGPUSurfaceSourceWindowsHWND
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceWindowsHWND },
			Hinstance = hinstance,
			Hwnd = hwnd,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&hwndSrc };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateAndroidSurface(IntPtr instance, IntPtr aNativeWindow)
	{
		var android = new WGPUSurfaceSourceAndroidNativeWindow
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceAndroidNativeWindow },
			Window = aNativeWindow,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&android };
		return wgpuInstanceCreateSurface(instance, &desc);
	}

	public static IntPtr CreateMetalSurface(IntPtr instance, IntPtr metalLayer)
	{
		var metal = new WGPUSurfaceSourceMetalLayer
		{
			Chain = new WGPUChainedStruct { SType = WGPUSType.SurfaceSourceMetalLayer },
			Layer = metalLayer,
		};
		var desc = new WGPUSurfaceDescriptor { NextInChain = (WGPUChainedStruct*)&metal };
		return wgpuInstanceCreateSurface(instance, &desc);
	}
}

/// <summary>The neutral WebGPU render target the host hands the backend: the single-sample resolve colour texture
/// (host-owned; presented/read back). The backend allocates its own MSAA colour + depth to match and resolves into
/// <see cref="ColorView"/>. Lifetime is the swapchain context's (recreated on resize).</summary>
internal sealed class WebGpuSwapchainTarget : IWebGpuRenderTarget
{
	public WebGpuSwapchainTarget(nint colorTexture, nint colorView, int width, int height, GraphicsColorFormat colorFormat)
	{
		ColorTexture = colorTexture;
		ColorView = colorView;
		Width = width;
		Height = height;
		ColorFormat = colorFormat;
	}

	public nint ColorTexture { get; }
	public nint ColorView { get; }
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat { get; }
	public void Dispose() { }   // the WebGpuSwapChainContext owns _presentTex/_presentView
}
