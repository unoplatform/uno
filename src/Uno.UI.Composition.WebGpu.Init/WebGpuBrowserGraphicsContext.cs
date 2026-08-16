#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// On-canvas WebGPU <see cref="ISwapChain"/> for the browser: owns a <see cref="WebGpuDevice"/> and a wgpu
/// surface bound to the HTML &lt;canvas&gt; (via emdawnwebgpu's canvas-selector source). The device is created in
/// JavaScript (navigator.gpu) and imported into the wgpu handle table by the caller (BrowserRenderer /
/// WebGpuJsInterop), then handed to the constructor.
///
/// Presentation differs from native: the backend renders MSAA and resolves into an OFFSCREEN single-sample
/// texture, which is then COPIED into the canvas' current texture. A direct MSAA-resolve into the canvas texture
/// does not composite on the browser's SwiftShader WebGPU adapter, whereas a plain texture-to-texture copy does;
/// and the browser presents implicitly when control returns to the event loop (no wgpuSurfacePresent).
/// </summary>
internal sealed unsafe class WebGpuBrowserGraphicsContext : ISwapChain, IWebGpuDeviceContext
{
	private readonly WebGpuInitDevice _device;
	private IntPtr _surface;
	private WebGpuSwapchainTarget? _target;
	private IntPtr _presentTex;    // offscreen single-sample resolve target (the backend resolves MSAA into this)
	private IntPtr _presentView;
	private IntPtr _canvasTexture;  // this frame's acquired canvas texture (blit destination)
	private bool _frameAcquired;
	private int _w, _h;
	private bool _configured;

	// Headless verification: with UNO_WEBGPU_READBACK=1, copy the offscreen frame back to CPU once (after content
	// has settled) and log its non-transparent pixel count via an async JS mapAsync — SwiftShader renders WebGPU
	// correctly to a texture even though it can't composite the canvas, so a readback is the only headless proof.
	private static readonly bool _readbackEnabled = Environment.GetEnvironmentVariable("UNO_WEBGPU_READBACK") == "1";
	private bool _readbackInFlight;

	// A 1-sample fullscreen-blit pipeline that samples _presentView into the canvas texture. SwiftShader only
	// composites the canvas from a render pass targeting it directly (not a resolve or a copy), so present blits.
	private IntPtr _blitModule;
	private IntPtr _blitPipe;
	private IntPtr _blitBgl;

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

	public WebGpuBrowserGraphicsContext(WebGpuInitDevice device, string canvasId)
	{
		_device = device;
		CreateSurface(canvasId);
		if (_readbackEnabled) { Console.WriteLine("[webgpu] UNO-READBACK-ENABLED=1"); }
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

	private static WGPUStringView Utf8(string s)
		=> new() { Data = Marshal.StringToCoTaskMemUTF8(s), Length = (nuint)System.Text.Encoding.UTF8.GetByteCount(s) };

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
		return _target!;   // the backend renders/resolves into _presentView (offscreen); Present() blits it to the canvas
	}

	public void Present()
	{
		if (!_frameAcquired)
		{
			return;
		}
		_frameAcquired = false;
		EnsureBlitPipeline();

		// Acquire the canvas texture at present time (after all offscreen rendering) and blit into it — matching the
		// pattern that composits on SwiftShader (render pass into the just-acquired canvas texture).
		WGPUSurfaceTexture st = default;
		wgpuSurfaceGetCurrentTexture(_surface, &st);
		if (st.Texture == IntPtr.Zero) { return; }
		_canvasTexture = st.Texture;

		var canvasView = wgpuTextureCreateView(_canvasTexture, null);

		var entries = stackalloc WGPUBindGroupEntry[2];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = _presentView };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _device.Smp };
		var bgd = new WGPUBindGroupDescriptor { Layout = _blitBgl, EntryCount = 2, Entries = entries };
		var bg = wgpuDeviceCreateBindGroup(_device.Dev, &bgd);

		var enc = wgpuDeviceCreateCommandEncoder(_device.Dev, null);
		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = canvasView, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);
		wgpuRenderPassEncoderSetPipeline(pass, _blitPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_device.Q, 1, (IntPtr)(&cb));

		wgpuBindGroupRelease(bg);
		wgpuTextureViewRelease(canvasView);
		wgpuTextureRelease(_canvasTexture);
		_canvasTexture = IntPtr.Zero;

		// Headless verification (UNO_WEBGPU_READBACK=1): read the freshly-presented offscreen frame back to CPU and
		// log its pixel stats. Present() only runs when a frame was actually drawn, so each present carries new
		// content; re-arming per present (skipping while a map is in flight) means navigating between samples logs
		// each new frame — the only way to observe WebGPU output headless, where SwiftShader can't composite a canvas.
		if (_readbackEnabled && !_readbackInFlight)
		{
			_readbackInFlight = true;
			ReadbackOffscreen();
		}
	}

	private void ReadbackOffscreen()
	{
		int w = _w, h = _h;
		uint unpadded = (uint)(w * 4);
		uint padded = (unpadded + 255u) & ~255u;   // wgpu requires 256-byte row alignment for T2B copies
		ulong total = (ulong)padded * (uint)h;
		var bd = new WGPUBufferDescriptor { Size = (nuint)total, Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead };
		var buf = wgpuDeviceCreateBuffer(_device.Dev, &bd);
		var enc = wgpuDeviceCreateCommandEncoder(_device.Dev, null);
		var src = new WGPUTexelCopyTextureInfo { Texture = _presentTex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
		var dst = new WGPUTexelCopyBufferInfo { Buffer = buf, Layout = new WGPUTexelCopyBufferLayout { Offset = 0, BytesPerRow = padded, RowsPerImage = (uint)h } };
		var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		wgpuCommandEncoderCopyTextureToBuffer(enc, &src, &dst, &ext);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_device.Q, 1, (IntPtr)(&cb));

		// mapAsync must run off the event loop (the in-WASM DevicePoll busy-spin can't yield); hand the buffer ptr to
		// JS. The await lives in a non-unsafe helper — 'await' is illegal inside this unsafe class's members (CS4004).
		_ = WebGpuReadbackReporter.ReportAsync(buf, w, h, (int)padded, () => _readbackInFlight = false);
	}

	private void EnsureBlitPipeline()
	{
		if (_blitPipe != IntPtr.Zero)
		{
			return;
		}
		var code = Utf8(BlitWgsl);
		var wgsl = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var smd = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&wgsl };
		_blitModule = wgpuDeviceCreateShaderModule(_device.Dev, &smd);

		var vs = Utf8("vs");
		var fs = Utf8("fs");
		var target = new WGPUColorTargetState { Format = _device.ColorFormat, Blend = null, WriteMask = WGPUColorWriteMask.All };
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
		_canvasTexture = IntPtr.Zero;
		_target?.Dispose();

		if (_presentView != IntPtr.Zero) { wgpuTextureViewRelease(_presentView); _presentView = IntPtr.Zero; }
		if (_presentTex != IntPtr.Zero) { wgpuTextureRelease(_presentTex); _presentTex = IntPtr.Zero; }

		// Offscreen single-sample resolve target (copied to the canvas each present).
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 },
			Format = _device.ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding
				| (_readbackEnabled ? WGPUTextureUsage.CopySrc : 0),
		};
		_presentTex = wgpuDeviceCreateTexture(_device.Dev, &td);
		_presentView = wgpuTextureCreateView(_presentTex, null);

		_target = new WebGpuSwapchainTarget(_presentView, width, height,
			_device.ColorFormat == WGPUTextureFormat.BGRA8Unorm ? GraphicsColorFormat.Bgra8888 : GraphicsColorFormat.Rgba8888);

		var format = _device.ColorFormat;
		var alphaMode = WGPUCompositeAlphaMode.Opaque;
		// The JS-import device-init path has no adapter handle; skip the caps query and use rgba8unorm + Opaque,
		// both valid for a browser canvas. When an adapter is present (should not happen on the browser today),
		// prefer the surface's reported format/alpha.
		if (_device.Adapter != IntPtr.Zero)
		{
			WGPUSurfaceCapabilities caps = default;
			wgpuSurfaceGetCapabilities(_surface, _device.Adapter, &caps);
			bool supported = false;
			for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { supported = true; break; } }
			if (!supported && caps.FormatCount > 0) { format = caps.Formats[0]; }
			if (caps.AlphaModeCount > 0) { alphaMode = caps.AlphaModes[0]; }
		}

		var cfg = new WGPUSurfaceConfiguration
		{
			Device = _device.Dev,
			Format = format,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopyDst,
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
		if (_presentView != IntPtr.Zero) { wgpuTextureViewRelease(_presentView); _presentView = IntPtr.Zero; }
		if (_presentTex != IntPtr.Zero) { wgpuTextureRelease(_presentTex); _presentTex = IntPtr.Zero; }
		if (_surface != IntPtr.Zero) { wgpuSurfaceRelease(_surface); _surface = IntPtr.Zero; }
		_device.Dispose();
	}
}

/// <summary>Non-unsafe home for the readback's async continuation (await is illegal inside the unsafe graphics
/// context class). Maps the readback buffer off the event loop via JS and logs the offscreen frame's pixel stats.</summary>
internal static class WebGpuReadbackReporter
{
	public static async System.Threading.Tasks.Task ReportAsync(IntPtr buf, int w, int h, int bytesPerRow, Action onDone)
	{
		try
		{
			var opaque = await WebGpuJsInterop.MapReadStatsAsync((int)buf, w, h, bytesPerRow);
			Console.WriteLine($"[webgpu] UNO-READBACK {w}x{h} opaquePixels={opaque} (of {w * h})");
			WGPU.wgpuBufferDestroy(buf);
		}
		finally
		{
			onDone();
		}
	}
}
