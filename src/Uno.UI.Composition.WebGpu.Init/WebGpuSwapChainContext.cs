// Shared on-window WebGPU context for NATIVE hosts (X11, Win32, macOS, …). The WebGPU swapchain semantics are
// identical across them — only the platform surface source differs — so hosts create a device + a surface (via
// the CreateXxxSurface factories) and this type owns acquire/configure/present. The browser is separate: it
// presents implicitly and blits (no wgpuSurfacePresent), so it has its own context.
#nullable enable

using System;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu.SilkCompat;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>Builds the platform window's surface on the given instance.</summary>
internal unsafe delegate Surface* SurfaceFactory(Instance* instance);

internal sealed unsafe class WebGpuSwapChainContext : ISwapChain, IWebGpuDeviceContext
{
	private static WebGPU Api => SilkCompatContext.Api;
	private static Wgpu Wgpu => SilkCompatContext.Wgpu;

	private readonly WebGpuInitDevice _device;
	private Surface* _surface;
	private WebGpuSwapchainTarget? _target;
	// The scene renders into this OFFSCREEN texture; Present() then blits it into
	// the acquired swapchain image. A direct render into the swapchain texture does NOT composite on
	// several native surfaces (and SwiftShader in the browser) — a render pass that TARGETS the swapchain image (the
	// blit) does. The MSAA colour + depth the scene renders into are the RENDER BACKEND's (it allocates them to match
	// this resolve target); this host owns only the single-sample resolve texture + the present/blit.
	private Texture* _presentTex;
	private TextureView* _presentView;
	// The blit bind group only depends on _presentView + the device sampler, so it's rebuilt on reconfigure
	// rather than per present (bind-group creation showed up in frame profiles).
	private BindGroup* _blitBg;
	private bool _frameAcquired;
	private int _w, _h;
	private bool _configured;
	private TextureFormat _surfaceFormat;

	// Fullscreen-triangle blit pipeline: samples _presentView and draws it into the swapchain image.
	private ShaderModule* _blitModule;
	private RenderPipeline* _blitPipe;
	private BindGroupLayout* _blitBgl;
	private TextureFormat _blitFormat;   // the swapchain format _blitPipe was built for (rebuild if it changes)

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

	private static bool IsSrgb(TextureFormat f)
		=> f == TextureFormat.Rgba8UnormSrgb || f == TextureFormat.Bgra8UnormSrgb;

	/// <param name="createSurface">Builds the wgpu surface for the platform window, given the instance handle
	/// (use one of the CreateXxxSurface factories).</param>
	public WebGpuSwapChainContext(TextureFormat colorFormat, SurfaceFactory createSurface)
	{
		_device = WebGpuInitDevice.GetShared(colorFormat, createSurface, out _surface);
		if (_surface == null)
		{
			throw new InvalidOperationException("Failed to create the wgpu surface for this window.");
		}

		// The adapter was chosen for the first window; a later one it can't present to would abort the process on
		// configure, so refuse it here instead.
		SurfaceCapabilities caps = default;
		Api.SurfaceGetCapabilities(_surface, _device.Adapter, &caps);
		var formatCount = caps.FormatCount;
		Api.SurfaceCapabilitiesFreeMembers(caps);
		if (formatCount == 0)
		{
			Api.SurfaceRelease(_surface);
			_surface = null;
			throw new InvalidOperationException("WebGPU: the adapter cannot present to this window.");
		}
	}

	// The host context is the device context the backend adopts: it owns the device it created.
	WebGPU IWebGpuDeviceContext.Api => Api;
	Instance* IWebGpuDeviceContext.Instance => _device.Inst;
	Adapter* IWebGpuDeviceContext.Adapter => _device.Adapter;
	Device* IWebGpuDeviceContext.Device => _device.Dev;
	Queue* IWebGpuDeviceContext.Queue => _device.Q;
	TextureFormat IWebGpuDeviceContext.ColorFormat => _device.ColorFormat;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	// The scene renders into _presentTex, one offscreen recreated only on resize, and Present() blits all of it into
	// the acquired swapchain image. Last frame's pixels are therefore still in the target when the next frame starts,
	// so the compositor can repaint only the damaged region.
	public bool PreservesContents => true;

	public IRenderTarget AcquireRenderTarget(int width, int height)
	{
		if (_disposed) { return _target!; }
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		Configure(width, height);
		_frameAcquired = true;
		// The backend renders + resolves into _presentView (offscreen); Present() blits that to the swapchain image.
		return _target!;
	}

	/// <summary>
	/// False when the last present was dropped in a way a retry can fix (a timed-out or stale acquire); a host that
	/// retries such frames reads it. A hidden window reports true: nothing can show until it is visible again.
	/// </summary>
	internal bool LastPresentSucceeded { get; private set; }

	// A rendered frame the window has not shown yet. The offscreen keeps it, so the next present blits it again
	// rather than leaving the window behind until something else changes.
	private bool _presentOwed;

	public void Present()
	{
		if (_disposed || _target is null)
		{
			return;
		}

		// Reconfiguring recreates the offscreen, so once the surface is stale only a newly rendered frame can be shown.
		if (!_frameAcquired && (!_presentOwed || !_configured))
		{
			return;
		}
		_frameAcquired = false;
		_presentOwed = true;

		// wgpu's Vulkan swapchain gives the acquire a fixed one-second wait and reports running past it as a lost
		// device (gfx-rs/wgpu#9029). On a CPU rasterizer the frame just submitted can take longer than that (the
		// Android emulator's SwiftShader, which compiles each shader on first use), so let it finish first.
		if (_device.IsSoftwareVulkan)
		{
			_ = Wgpu.DevicePoll(_device.Dev, true, null);
		}

		// Acquire the swapchain image at present time (after the scene render) and blit the offscreen frame into it.
		SurfaceTexture st = default;
		Api.SurfaceGetCurrentTexture(_surface, &st);
		if (st.Status != SurfaceGetCurrentTextureStatus.Success || st.Texture == null)
		{
			// A texture handed back with a failed acquire still references a back buffer, and an unreleased one
			// makes the next resize (D3D12 ResizeBuffers) fail or block the render thread.
			if (st.Texture != null)
			{
				Api.TextureRelease(st.Texture);
			}

			// Only a surface that no longer matches its window needs reconfiguring; a timed-out or occluded
			// acquire just skips this frame. Reconfiguring on those resized the swapchain every frame.
			if (st.Status is SurfaceGetCurrentTextureStatus.Outdated or SurfaceGetCurrentTextureStatus.Lost)
			{
				_configured = false;
			}

			LastPresentSucceeded = st.Status == global::Uno.UI.Composition.WebGpu.SilkCompat.Xlate.SurfaceStatusOccluded;
			return;
		}

		var view = Api.TextureCreateView(st.Texture, null);
		var bg = _blitBg;

		var enc = Api.DeviceCreateCommandEncoder(_device.Dev, null);
		var ca = new RenderPassColorAttachment { DepthSlice = uint.MaxValue, View = view, LoadOp = LoadOp.Clear, StoreOp = StoreOp.Store, ClearValue = default };
		var rp = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = Api.CommandEncoderBeginRenderPass(enc, &rp);
		Api.RenderPassEncoderSetPipeline(pass, _blitPipe);
		Api.RenderPassEncoderSetBindGroup(pass, 0, bg, 0, null);
		Api.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
		Api.RenderPassEncoderEnd(pass);
		// The pass holds a reference to its colour attachment - here, the swapchain's back buffer. Leaking it
		// leaves the buffer referenced, and D3D12 then refuses to resize the swapchain ("window is in use"),
		// which wgpu reports as an invalid surface and panics on.
		Api.RenderPassEncoderRelease(pass);
		var cb = Api.CommandEncoderFinish(enc, null);
		Api.QueueSubmit(_device.Q, 1, &cb);

		Api.SurfacePresent(_surface);
		_presentOwed = false;
		LastPresentSucceeded = true;

		Api.CommandBufferRelease(cb);
		Api.CommandEncoderRelease(enc);
		Api.TextureViewRelease(view);
		Api.TextureRelease(st.Texture);
	}

	private void EnsureBlitPipeline()
	{
		if (_blitPipe != null && _blitFormat == _surfaceFormat)
		{
			return;
		}
		if (_blitPipe != null) { Api.RenderPipelineRelease(_blitPipe); _blitPipe = null; }
		if (_blitModule != null) { Api.ShaderModuleRelease(_blitModule); _blitModule = null; }
		_blitFormat = _surfaceFormat;
		var code = Utf8(IsSrgb(_surfaceFormat) ? BlitWgslSrgb : BlitWgsl);
		var wgsl = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
		var smd = new ShaderModuleDescriptor { NextInChain = (ChainedStruct*)&wgsl };
		_blitModule = Api.DeviceCreateShaderModule(_device.Dev, &smd);

		var vs = Utf8("vs");
		var fs = Utf8("fs");
		// Target must match the configured swapchain format (not necessarily the device's offscreen format).
		var target = new ColorTargetState { Format = _surfaceFormat, Blend = null, WriteMask = ColorWriteMask.All };
		var fsState = new FragmentState { Module = _blitModule, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new RenderPipelineDescriptor
		{
			Vertex = new VertexState { Module = _blitModule, EntryPoint = vs, BufferCount = 0 },
			Fragment = &fsState,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = null,
			Layout = null,
		};
		_blitPipe = Api.DeviceCreateRenderPipeline(_device.Dev, &pd);
		_blitBgl = Api.RenderPipelineGetBindGroupLayout(_blitPipe, 0);
	}

	private void Configure(int width, int height)
	{
		if (_configured && width == _w && height == _h)
		{
			return;
		}
		_w = width;
		_h = height;
		if (_presentView != null) { Api.TextureViewRelease(_presentView); _presentView = null; }
		if (_presentTex != null) { Api.TextureDestroy(_presentTex); Api.TextureRelease(_presentTex); _presentTex = null; }

		// Offscreen single-sample resolve target: the scene's MSAA pass (owned by the render backend) resolves into
		// this, and it is sampled by the present blit. TextureBinding so the blit can sample it; CopySrc so
		// ReadPixels/RenderTargetBitmap can read it.
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = _device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopySrc,
		};
		_presentTex = Api.DeviceCreateTexture(_device.Dev, &td);
		_presentView = Api.TextureCreateView(_presentTex, null);
		_target = new WebGpuSwapchainTarget(_presentView, width, height,
			_device.ColorFormat == TextureFormat.Bgra8Unorm ? GraphicsColorFormat.Bgra8888 : GraphicsColorFormat.Rgba8888);

		SurfaceCapabilities caps = default;
		Api.SurfaceGetCapabilities(_surface, _device.Adapter, &caps);
		// The device's own format, else its channel-swapped UNORM twin (Android's SwiftShader offers RGBA8 but not
		// BGRA8), and only then whatever the surface lists first - which can be an sRGB format the blit must convert to.
		var twin = _device.ColorFormat == TextureFormat.Bgra8Unorm ? TextureFormat.Rgba8Unorm : TextureFormat.Bgra8Unorm;
		_surfaceFormat = IsOffered(caps, _device.ColorFormat) ? _device.ColorFormat
			: IsOffered(caps, twin) ? twin
			: caps.FormatCount > 0 ? caps.Formats[0] : _device.ColorFormat;
		var alphaMode = caps.AlphaModeCount > 0 ? caps.AlphaModes[0] : CompositeAlphaMode.Auto;

		// UNO_WEBGPU_PRESENT picks the present mode (fifo default; immediate/mailbox/fiforelaxed for
		// benchmarking without vsync backpressure), validated against the surface's capabilities.
		var presentMode = Environment.GetEnvironmentVariable("UNO_WEBGPU_PRESENT")?.ToLowerInvariant() switch
		{
			"immediate" => PresentMode.Immediate,
			"mailbox" => PresentMode.Mailbox,
			"fiforelaxed" => PresentMode.FifoRelaxed,
			_ => PresentMode.Fifo,
		};
		if (presentMode != PresentMode.Fifo)
		{
			var modeSupported = false;
			for (nuint i = 0; i < caps.PresentModeCount; i++)
			{
				if (caps.PresentModes[i] == presentMode)
				{
					modeSupported = true;
					break;
				}
			}

			if (!modeSupported)
			{
				presentMode = PresentMode.Fifo; // guaranteed supported
			}
		}

		var cfg = new SurfaceConfiguration
		{
			Device = _device.Dev,
			Format = _surfaceFormat,
			Usage = TextureUsage.RenderAttachment,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = presentMode,
			AlphaMode = alphaMode,
		};
		Api.SurfaceConfigure(_surface, &cfg);

		// The blit pipeline needs the (now final) surface format; the bind group needs the fresh _presentView.
		EnsureBlitPipeline();
		if (_blitBg != null)
		{
			Api.BindGroupRelease(_blitBg);
		}

		var blitEntries = stackalloc BindGroupEntry[2];
		blitEntries[0] = new BindGroupEntry { Binding = 0, TextureView = _presentView };
		blitEntries[1] = new BindGroupEntry { Binding = 1, Sampler = _device.Smp };
		var blitBgd = new BindGroupDescriptor { Layout = _blitBgl, EntryCount = 2, Entries = blitEntries };
		_blitBg = Api.DeviceCreateBindGroup(_device.Dev, &blitBgd);

		var offeredFormats = new System.Text.StringBuilder();
		for (nuint i = 0; i < caps.FormatCount; i++) { offeredFormats.Append(i == 0 ? "" : ",").Append(caps.Formats[i]); }
		System.Console.WriteLine($"[webgpu] surface {width}x{height} format={_surfaceFormat} (offered {offeredFormats}) present={presentMode}");
		_configured = true;
	}

	private static bool IsOffered(in SurfaceCapabilities caps, TextureFormat format)
	{
		for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { return true; } }
		return false;
	}

	private static byte* Utf8(string s)
		=> (byte*)System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8(s);

	/// <summary>
	/// Set by <see cref="Dispose"/>. wgpu aborts the process on a surface call it considers invalid, from inside
	/// the native library where no error callback can intercept it, so a disposed context must stop touching its
	/// surface rather than rely on the error being reported.
	/// </summary>
	private bool _disposed;

	public void Dispose()
	{
		_disposed = true;
		if (_blitBg != null) { Api.BindGroupRelease(_blitBg); _blitBg = null; }
		if (_presentView != null) { Api.TextureViewRelease(_presentView); _presentView = null; }
		if (_presentTex != null) { Api.TextureDestroy(_presentTex); Api.TextureRelease(_presentTex); _presentTex = null; }
		if (_surface != null) { Api.SurfaceRelease(_surface); _surface = null; }
		// The device is shared process-wide and deliberately NOT released here - see WebGpuInitDevice.GetShared.
	}

	// ---- Platform surface factories (build the chained surface source + create the wgpu surface) ----

	public static Surface* CreateXlibSurface(Instance* instance, IntPtr display, ulong window)
	{
		var xlib = new SurfaceDescriptorFromXlibWindow
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromXlibWindow },
			Display = (void*)display,
			Window = window,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&xlib };
		return Api.InstanceCreateSurface(instance, &desc);
	}

	public static Surface* CreateHwndSurface(Instance* instance, IntPtr hinstance, IntPtr hwnd)
	{
		var hwndSrc = new SurfaceDescriptorFromWindowsHWND
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromWindowsHwnd },
			Hinstance = (void*)hinstance,
			Hwnd = (void*)hwnd,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&hwndSrc };
		return Api.InstanceCreateSurface(instance, &desc);
	}

	public static Surface* CreateAndroidSurface(Instance* instance, IntPtr aNativeWindow)
	{
		var android = new SurfaceDescriptorFromAndroidNativeWindow
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromAndroidNativeWindow },
			Window = (void*)aNativeWindow,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&android };
		return Api.InstanceCreateSurface(instance, &desc);
	}

	public static Surface* CreateMetalSurface(Instance* instance, IntPtr metalLayer)
	{
		var metal = new SurfaceDescriptorFromMetalLayer
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromMetalLayer },
			Layer = (void*)metalLayer,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&metal };
		return Api.InstanceCreateSurface(instance, &desc);
	}
}

/// <summary>The WebGPU render target the host hands the backend: the single-sample resolve colour texture
/// (host-owned; presented/read back). The backend allocates its own MSAA colour + depth to match and resolves into
/// <see cref="ColorView"/>. Lifetime is the swapchain context's (recreated on resize).</summary>
internal sealed unsafe class WebGpuSwapchainTarget : IWebGpuRenderTarget
{
	public WebGpuSwapchainTarget(TextureView* colorView, int width, int height, GraphicsColorFormat colorFormat)
	{
		ColorView = colorView;
		Width = width;
		Height = height;
		ColorFormat = colorFormat;
	}

	public TextureView* ColorView { get; }
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat { get; }
	public void Dispose() { }   // the host context owns the underlying resolve texture/view
}
