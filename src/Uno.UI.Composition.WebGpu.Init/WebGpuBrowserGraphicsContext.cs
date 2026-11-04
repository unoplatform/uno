#nullable enable

using System;
using System.Runtime.InteropServices;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.WebGpu.SilkCompat;
using Silk.NET.WebGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// On-canvas WebGPU <see cref="ISwapChain"/> for the browser: owns a <see cref="WebGpuDevice"/> and a wgpu
/// surface bound to the HTML &lt;canvas&gt; (via emdawnwebgpu's canvas-selector source). The device is created in
/// JavaScript (navigator.gpu) and imported into the wgpu handle table by the caller (BrowserRenderer /
/// WebGpuJsInterop), then handed to the constructor.
///
/// Presentation differs from native: the backend renders into an OFFSCREEN texture, which is then COPIED into the
/// canvas' current texture. A direct render into the canvas texture
/// does not composite on the browser's SwiftShader WebGPU adapter, whereas a plain texture-to-texture copy does;
/// and the browser presents implicitly when control returns to the event loop (no wgpuSurfacePresent).
/// </summary>
internal sealed unsafe class WebGpuBrowserGraphicsContext : ISwapChain, IWebGpuDeviceContext
{
	private static WebGPU Api => SilkCompatContext.Api;

	private readonly WebGpuInitDevice _device;
	private Surface* _surface;
	private WebGpuSwapchainTarget? _target;
	private Texture* _presentTex;    // the offscreen the backend renders into
	private TextureView* _presentView;
	private Texture* _canvasTexture;  // this frame's acquired canvas texture (blit destination)
	private bool _frameAcquired;
	private int _w, _h;
	private bool _configured;

	// The browser's frame callback is not held back by the GPU, so without a cap a GPU slower than the callback rate
	// (a software adapter) queues frames without bound: a readback then waits behind a minute of queued work.
	private const int MaxFramesInFlight = 2;

	// A 1-sample fullscreen-blit pipeline that samples _presentView into the canvas texture. SwiftShader only
	// composites the canvas from a render pass targeting it directly (not a resolve or a copy), so present blits.
	private ShaderModule* _blitModule;
	private RenderPipeline* _blitPipe;
	private BindGroupLayout* _blitBgl;

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
	}

	private void CreateSurface(string canvasId)
	{
		var selector = Utf8("#" + canvasId);
		// SilkCompat translates the 2024 header's canvas source to emdawnwebgpu's.
		var canvas = new SurfaceDescriptorFromCanvasHTMLSelector
		{
			Chain = new ChainedStruct { SType = SType.SurfaceDescriptorFromCanvasHtmlSelector },
			Selector = selector,
		};
		var desc = new SurfaceDescriptor { NextInChain = (ChainedStruct*)&canvas };
		_surface = Api.InstanceCreateSurface(_device.Inst, &desc);
		if (_surface == null)
		{
			throw new InvalidOperationException("Failed to create a wgpu canvas surface.");
		}
	}

	private static byte* Utf8(string s) => (byte*)Marshal.StringToCoTaskMemUTF8(s);

	WebGPU IWebGpuDeviceContext.Api => Api;
	Instance* IWebGpuDeviceContext.Instance => _device.Inst;
	Adapter* IWebGpuDeviceContext.Adapter => _device.Adapter;
	Device* IWebGpuDeviceContext.Device => _device.Dev;
	Queue* IWebGpuDeviceContext.Queue => _device.Q;
	TextureFormat IWebGpuDeviceContext.ColorFormat => _device.ColorFormat;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;
	public bool IsReadyForFrame => WebGpuJsInterop.PresentedFramesInFlight() < MaxFramesInFlight;

	// The scene renders into _presentTex, recreated only on resize, and Present() blits all of it to the canvas. Last
	// frame's pixels are therefore still there when the next frame starts, so the compositor can repaint only the damage.
	public bool PreservesContents => true;

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
		SurfaceTexture st = default;
		Api.SurfaceGetCurrentTexture(_surface, &st);
		if (st.Texture == null) { return; }
		_canvasTexture = st.Texture;

		var canvasView = Api.TextureCreateView(_canvasTexture, null);

		var entries = stackalloc BindGroupEntry[2];
		entries[0] = new BindGroupEntry { Binding = 0, TextureView = _presentView };
		entries[1] = new BindGroupEntry { Binding = 1, Sampler = _device.Smp };
		var bgd = new BindGroupDescriptor { Layout = _blitBgl, EntryCount = 2, Entries = entries };
		var bg = Api.DeviceCreateBindGroup(_device.Dev, &bgd);

		var enc = Api.DeviceCreateCommandEncoder(_device.Dev, null);
		var ca = new RenderPassColorAttachment { DepthSlice = uint.MaxValue, View = canvasView, LoadOp = LoadOp.Clear, StoreOp = StoreOp.Store, ClearValue = default };
		var rp = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = Api.CommandEncoderBeginRenderPass(enc, &rp);
		Api.RenderPassEncoderSetPipeline(pass, _blitPipe);
		Api.RenderPassEncoderSetBindGroup(pass, 0, bg, 0, null);
		Api.RenderPassEncoderDraw(pass, 3, 1, 0, 0);
		Api.RenderPassEncoderEnd(pass);
		Api.RenderPassEncoderRelease(pass);
		var cb = Api.CommandEncoderFinish(enc, null);
		Api.QueueSubmit(_device.Q, 1, &cb);
		WebGpuJsInterop.TrackPresentedFrame((int)_device.Q);

		Api.CommandBufferRelease(cb);
		Api.CommandEncoderRelease(enc);
		Api.BindGroupRelease(bg);
		Api.TextureViewRelease(canvasView);
		Api.TextureRelease(_canvasTexture);
		_canvasTexture = null;
	}

	private void EnsureBlitPipeline()
	{
		if (_blitPipe != null)
		{
			return;
		}
		var code = Utf8(BlitWgsl);
		var wgsl = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
		var smd = new ShaderModuleDescriptor { NextInChain = (ChainedStruct*)&wgsl };
		_blitModule = Api.DeviceCreateShaderModule(_device.Dev, &smd);

		var vs = Utf8("vs");
		var fs = Utf8("fs");
		var target = new ColorTargetState { Format = _device.ColorFormat, Blend = null, WriteMask = ColorWriteMask.All };
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
		_canvasTexture = null;
		_target?.Dispose();

		if (_presentView != null) { Api.TextureViewRelease(_presentView); _presentView = null; }
		if (_presentTex != null) { Api.TextureRelease(_presentTex); _presentTex = null; }

		// Offscreen single-sample resolve target, created on the JS device and imported into emdawn's handle table.
		var usage = (int)(TextureUsage.RenderAttachment | TextureUsage.TextureBinding);
		_presentTex = (Texture*)WebGpuJsInterop.CreateAndImportOffscreenTexture(_device.JsDeviceObject, width, height, usage);
		_presentView = Api.TextureCreateView(_presentTex, null);
		_target = new WebGpuSwapchainTarget(_presentView, width, height,
			_device.ColorFormat == TextureFormat.Bgra8Unorm ? GraphicsColorFormat.Bgra8888 : GraphicsColorFormat.Rgba8888);

		var format = _device.ColorFormat;
		var alphaMode = CompositeAlphaMode.Opaque;
		// The JS-import device-init path has no adapter handle; skip the caps query and use rgba8unorm + Opaque,
		// both valid for a browser canvas. When an adapter is present (should not happen on the browser today),
		// prefer the surface's reported format/alpha.
		if (_device.Adapter != null)
		{
			SurfaceCapabilities caps = default;
			Api.SurfaceGetCapabilities(_surface, _device.Adapter, &caps);
			bool supported = false;
			for (nuint i = 0; i < caps.FormatCount; i++) { if (caps.Formats[i] == format) { supported = true; break; } }
			if (!supported && caps.FormatCount > 0) { format = caps.Formats[0]; }
			if (caps.AlphaModeCount > 0) { alphaMode = caps.AlphaModes[0]; }
		}

		var cfg = new SurfaceConfiguration
		{
			Device = _device.Dev,
			Format = format,
			Usage = TextureUsage.RenderAttachment | TextureUsage.CopyDst,
			Width = (uint)width,
			Height = (uint)height,
			PresentMode = PresentMode.Fifo,
			AlphaMode = alphaMode,
		};
		Api.SurfaceConfigure(_surface, &cfg);
		_configured = true;
	}

	public void Dispose()
	{
		_target?.Dispose();
		if (_presentView != null) { Api.TextureViewRelease(_presentView); _presentView = null; }
		if (_presentTex != null) { Api.TextureRelease(_presentTex); _presentTex = null; }
		if (_surface != null) { Api.SurfaceRelease(_surface); _surface = null; }
		_device.Dispose();
	}
}
