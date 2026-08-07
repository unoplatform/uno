// Minimal-but-real WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Solid rects + even-odd path fill (stencil-then-cover) consuming IGeometry.StreamFlattened (Skia-less).
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

/// <summary>Async device creation for the browser, where the JS event loop must run for requestAdapter/
/// requestDevice to resolve (WebGpuDevice's blocking ctor would deadlock). Non-unsafe so it can await; the
/// pointer work lives in WebGpuDevice's internal helpers.</summary>
public static class WebGpuDeviceAsync
{
	public static async System.Threading.Tasks.Task<WebGpuDevice> CreateAsync(WGPUTextureFormat colorFormat = WebGpuDevice.DefaultColorFormat)
	{
		var inst = WebGpuDevice.CreateInstancePtr();
		var adapter = await PumpBox(inst, WebGpuDevice.BeginAdapterRequest(inst));
		if (adapter == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: no adapter (browser)."); }
		var dev = await PumpBox(inst, WebGpuDevice.BeginDeviceRequest(adapter));
		if (dev == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: no device (browser)."); }
		return new WebGpuDevice(colorFormat, inst, adapter, dev);
	}

	private static async System.Threading.Tasks.Task<IntPtr> PumpBox(IntPtr inst, GCHandle handle)
	{
		var box = (IntPtr[])handle.Target!;
		for (int i = 0; i < 2000 && box[0] == IntPtr.Zero; i++)
		{
			WebGpuDevice.ProcessEvents(inst);
			await System.Threading.Tasks.Task.Delay(5);   // yield to the JS event loop so navigator.gpu promises resolve
		}
		var result = box[0];
		handle.Free();
		return result;
	}
}

public sealed unsafe class WebGpuDevice : IDisposable
{
	public IntPtr Inst;
	public IntPtr Adapter;
	public IntPtr Dev;
	public IntPtr Q;
	public IntPtr SolidPipe;
	public IntPtr StencilEvenOdd;
	public IntPtr StencilNonZero;
	public IntPtr CoverPipe;
	public IntPtr ImagePipe;
	public IntPtr GradientPipe;
	public IntPtr BlurPipe;              // separable gaussian (fullscreen), single-sample
	public IntPtr BlurBgl;
	public IntPtr CompositeSrcOver;      // composite a layer texture into an MSAA pass (SrcOver / DstIn)
	public IntPtr CompositeDstIn;
	public IntPtr CompositeBgl;         // SrcOver's group(0) layout
	public IntPtr CompositeDstInBgl;    // DstIn's group(0) layout (auto-layouts aren't interchangeable)
	public IntPtr DummyTex;                 // 1x1 placeholder for the clip coverage binding when no path clip
	public WebGpuTexturePool Pool;                // transient offscreen pool (reused across frames)
	public WebGpuBufferPool BufferPool;           // transient vertex/uniform buffer pool (reused across frames)
	// Serializes a whole frame's render (reset → record → submit → poll) on this device. The on-window render
	// loop and off-loop renders (RenderTargetBitmap) share the device's transient pools/caches, so two frames
	// must not overlap or one frame's BeginFrameResources frees the other's in-flight resources (wgpu panics).
	public readonly object RenderGate = new();
	private readonly System.Collections.Generic.List<nint> _pendingBindGroups = new();
	private readonly System.Collections.Generic.List<nint> _pendingBuffers = new();
	private readonly System.Collections.Generic.List<nint> _pendingBundles = new();
	// Per-visual GPU geometry cache, keyed by the recording's immutable command list (reference identity). Owned
	// by the render thread; entries not referenced in a frame are evicted (their recording is gone).
	internal readonly System.Collections.Generic.Dictionary<System.Collections.Generic.List<WebGpuCommand>, WebGpuGeometryCache> GeometryCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
	private readonly System.Collections.Generic.List<System.Collections.Generic.List<WebGpuCommand>> _evict = new();

	// Per-frame bind groups reference the frame's pooled buffers, so they're released at the next frame start once
	// the previous frame's GPU work has completed (present DevicePolls). A cached recording's persistent resources
	// released mid-frame (cache miss) are deferred the same way, since ops already emitted this frame still use
	// them. Pooled buffers/textures are reused (not released). Call once per frame before rebuilding.
	public void BeginFrameResources()
	{
		Pool.BeginFrame();
		BufferPool.BeginFrame();
		foreach (var bg in _pendingBindGroups) { wgpuBindGroupRelease((IntPtr)bg); }
		foreach (var b in _pendingBuffers) { wgpuBufferRelease((IntPtr)b); }
		foreach (var bu in _pendingBundles) { wgpuRenderBundleRelease((IntPtr)bu); }
		_pendingBindGroups.Clear();
		_pendingBuffers.Clear();
		_pendingBundles.Clear();

		// Evict geometry-cache entries not referenced in the previous frame (their recording is gone); then arm
		// the used-flags for this frame.
		_evict.Clear();
		foreach (var kv in GeometryCache) { if (!kv.Value.Used) { _evict.Add(kv.Key); } else { kv.Value.Used = false; } }
		foreach (var k in _evict) { DeferRelease(GeometryCache[k].Owned); GeometryCache.Remove(k); }
	}

	public IntPtr TrackBg(IntPtr bg) { _pendingBindGroups.Add((nint)bg); return bg; }

	// Defers a cached recording's persistent resources for release at the next frame start.
	internal void DeferRelease(OwnedResources owned)
	{
		if (owned is null) { return; }
		_pendingBuffers.AddRange(owned.Buffers);
		_pendingBindGroups.AddRange(owned.BindGroups);
		_pendingBundles.AddRange(owned.Bundles);
	}
	public IntPtr ImgBgl;
	public IntPtr GradBgl;
	// group(1) clip-uniform layouts (one per color-writing pipeline; all describe the same ClipU).
	public IntPtr SolidClipBgl;
	public IntPtr CoverClipBgl;
	public IntPtr ImageClipBgl;
	public IntPtr GradClipBgl;
	public IntPtr Smp;

	// Uniform size (bytes) of the gradient struct: header(16) + geo(16) + colors(16*16) + stops(4*16) + origin(16).
	// origin (radial focal point, device space) is appended last so colour/stop indices stay put.
	public const int GradientUniformBytes = 16 + 16 + 16 * 16 + 4 * 16 + 16;
	public const int MaxGradientStops = 16;

	// Multisample count for anti-aliasing. Every pipeline + the color/depth render targets use this; the pass
	// renders into a multisampled color texture that resolves into the single-sample present/readback texture.
	public const uint MsaaSamples = 4;

	// The color-attachment format the pipelines + offscreen targets use. Rgba8Unorm by default (the
	// offscreen/readback path assumes it); a swapchain renderer passes the surface's supported format.
	public readonly WGPUTextureFormat ColorFormat;
	public const WGPUTextureFormat DefaultColorFormat = WGPUTextureFormat.RGBA8Unorm;
	public const WGPUTextureFormat DepthStencilFormat = WGPUTextureFormat.Depth24PlusStencil8;

	public WebGpuDevice(WGPUTextureFormat colorFormat = DefaultColorFormat)
	{
		ColorFormat = colorFormat;
		Inst = wgpuCreateInstance(null);

		var abox = new IntPtr[1];
		var ah = GCHandle.Alloc(abox);
		var aopts = new WGPURequestAdapterOptions { PowerPreference = WGPUPowerPreference.HighPerformance };
		wgpuInstanceRequestAdapter(Inst, &aopts, new WGPURequestAdapterCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnAdapter,
			Userdata1 = GCHandle.ToIntPtr(ah),
		});
		for (int i = 0; i < 1000 && abox[0] == IntPtr.Zero; i++) { wgpuInstanceProcessEvents(Inst); }
		Adapter = abox[0];
		ah.Free();

		var dbox = new IntPtr[1];
		var dh = GCHandle.Alloc(dbox);
		var ddesc = new WGPUDeviceDescriptor();
		wgpuAdapterRequestDevice(Adapter, &ddesc, new WGPURequestDeviceCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnDevice,
			Userdata1 = GCHandle.ToIntPtr(dh),
		});
		for (int i = 0; i < 1000 && dbox[0] == IntPtr.Zero; i++) { wgpuInstanceProcessEvents(Inst); }
		Dev = dbox[0];
		dh.Free();

		FinishInit();
	}

	// Adopts an already-acquired instance/adapter/device (used by the async browser init, which cannot spin-block).
	internal WebGpuDevice(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr adapter, IntPtr dev)
	{
		ColorFormat = colorFormat;
		Inst = inst;
		Adapter = adapter;
		Dev = dev;
		FinishInit();
	}

	private void FinishInit()
	{
		Q = wgpuDeviceGetQueue(Dev);
		CreatePipelines();
		DummyTex = CreateColorTarget(1, 1);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
	}

	internal static unsafe IntPtr CreateInstancePtr() => wgpuCreateInstance(null);

	internal static unsafe GCHandle BeginAdapterRequest(IntPtr inst)
	{
		var box = new IntPtr[1];
		var h = GCHandle.Alloc(box);
		var aopts = new WGPURequestAdapterOptions { PowerPreference = WGPUPowerPreference.HighPerformance };
		wgpuInstanceRequestAdapter(inst, &aopts, new WGPURequestAdapterCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestAdapterStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnAdapter,
			Userdata1 = GCHandle.ToIntPtr(h),
		});
		return h;
	}

	internal static unsafe GCHandle BeginDeviceRequest(IntPtr adapter)
	{
		var box = new IntPtr[1];
		var h = GCHandle.Alloc(box);
		var ddesc = new WGPUDeviceDescriptor();
		wgpuAdapterRequestDevice(adapter, &ddesc, new WGPURequestDeviceCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnDevice,
			Userdata1 = GCHandle.ToIntPtr(h),
		});
		return h;
	}

	internal static unsafe void ProcessEvents(IntPtr inst) => wgpuInstanceProcessEvents(inst);

	/// <summary>Reads a surface's resolved single-sample texture back to CPU as tightly-packed RGBA8 (top-down). For RTB and tests.</summary>
	public byte[] ReadPixelsRgba(WebGpuRenderSurface s)
	{
		int w = s.Width, h = s.Height;
		uint unpadded = (uint)(w * 4);
		uint padded = (unpadded + 255u) & ~255u;              // wgpu requires 256-byte row alignment for T2B copies
		ulong total = (ulong)padded * (uint)h;
		var bd = new WGPUBufferDescriptor { Size = (nuint)total, Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead };
		var buf = wgpuDeviceCreateBuffer(Dev, &bd);
		var enc = wgpuDeviceCreateCommandEncoder(Dev, null);
		var src = new WGPUTexelCopyTextureInfo { Texture = s.Tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
		var dst = new WGPUTexelCopyBufferInfo { Buffer = buf, Layout = new WGPUTexelCopyBufferLayout { Offset = 0, BytesPerRow = padded, RowsPerImage = (uint)h } };
		var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		wgpuCommandEncoderCopyTextureToBuffer(enc, &src, &dst, &ext);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(Q, 1, (IntPtr)(&cb));
		wgpuDevicePoll(Dev, 1u, null);

		var mapped = new bool[1];
		var mh = GCHandle.Alloc(mapped);
		wgpuBufferMapAsync(buf, WGPUMapMode.Read, 0, (nuint)total, new WGPUBufferMapCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, IntPtr, IntPtr, void>)&OnMap,
			Userdata1 = GCHandle.ToIntPtr(mh),
		});
		while (!mapped[0]) { wgpuDevicePoll(Dev, 1u, null); }
		mh.Free();
		var mp = (byte*)(void*)wgpuBufferGetMappedRange(buf, 0, (nuint)total);
		var outp = new byte[w * h * 4];
		for (int y = 0; y < h; y++)
		{
			for (uint x = 0; x < unpadded; x++) { outp[y * (int)unpadded + (int)x] = mp[(uint)y * padded + x]; }
		}
		wgpuBufferUnmap(buf);
		wgpuBufferDestroy(buf);
		return outp;
	}

	// Shared clip type + coverage fn prepended to every color-writing shader. The uniform is passed as a
	// parameter (each shader declares the binding at its own contiguous group index — colored uses group 0,
	// image/gradient group 1 — avoiding a group hole wgpu's auto-layout rejects). Device-space, axis-aligned
	// rounded-rect mask (radii 0 → plain rect, full coverage); ~1px analytic AA on the corner edge.
	private const string ClipStructFn = @"
struct ClipU { rect: vec4<f32>, radii: vec4<f32>, flags: vec4<f32>, size: vec4<f32> };
fn clipCov(fc: vec2<f32>, clip: ClipU, ctex: texture_2d<f32>, csmp: sampler) -> f32 {
  var cov = 1.0;
  if (clip.flags.x > 0.5) {
    let rl = clip.rect;
    let c = vec2<f32>((rl.x + rl.z) * 0.5, (rl.y + rl.w) * 0.5);
    let h = vec2<f32>((rl.z - rl.x) * 0.5, (rl.w - rl.y) * 0.5);
    let lp = fc - c;
    let rTop = select(clip.radii.x, clip.radii.y, lp.x > 0.0);
    let rBot = select(clip.radii.w, clip.radii.z, lp.x > 0.0);
    let rad = select(rTop, rBot, lp.y > 0.0);
    let q = abs(lp) - h + vec2<f32>(rad, rad);
    let d = min(max(q.x, q.y), 0.0) + length(max(q, vec2<f32>(0.0, 0.0))) - rad;
    let rr = clamp(0.5 - d, 0.0, 1.0);
    // flags.z = Difference (exclude): keep the OUTSIDE of the rounded rect; otherwise keep the inside.
    cov = cov * select(rr, 1.0 - rr, clip.flags.z > 0.5);
  }
  if (clip.flags.y > 0.5) {
    let uv = fc / max(clip.size.xy, vec2<f32>(1.0, 1.0));
    cov = cov * textureSampleLevel(ctex, csmp, uv, 0.0).a;
  }
  return cov;
}
";

	/// <summary>A single-sample Rgba8 render target usable as a shader input (offscreen blur temp/output). The
	/// returned view keeps its texture alive; not pooled/freed yet (fine for offscreen/one-shot).</summary>
	public IntPtr CreateColorTarget(int w, int h)
	{
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = DefaultColorFormat,
			MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		return wgpuTextureCreateView(tex, null);
	}

	private const string ColoredWgsl = @"
@group(0) @binding(0) var<uniform> clip: ClipU;
@group(0) @binding(1) var clipTex: texture_2d<f32>;
@group(0) @binding(2) var clipSmp: sampler;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>) -> VOut {
  var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.p.xy, clip, clipTex, clipSmp)); }";

	private const string PosOnlyWgsl = @"
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return vec4<f32>(pos, 0.0, 1.0); }
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";

	private IntPtr Module(string wgsl)
	{
		var code = SV(wgsl);
		var w = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var d = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&w };
		return wgpuDeviceCreateShaderModule(Dev, &d);
	}

	private static WGPUStencilFaceState Face(WGPUCompareFunction cmp, WGPUStencilOperation pass)
		=> new() { Compare = cmp, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = pass };

	private void CreatePipelines()
	{
		var colored = Module(ClipStructFn + ColoredWgsl);
		var posOnly = Module(PosOnlyWgsl);
		var vs = SV("vs");
		var fs = SV("fs");

		var blend = new WGPUBlendState
		{
			Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.SrcAlpha, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
			Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
		};

		SolidPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), 0x00, 0x00);
		StencilEvenOdd = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), 0xFF, 0xFF);
		StencilNonZero = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.IncrementWrap), Face(WGPUCompareFunction.Always, WGPUStencilOperation.DecrementWrap), 0xFF, 0xFF);
		CoverPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), 0xFF, 0xFF);
		SolidClipBgl = wgpuRenderPipelineGetBindGroupLayout(SolidPipe, 0);
		CoverClipBgl = wgpuRenderPipelineGetBindGroupLayout(CoverPipe, 0);
		CreateImagePipeline();
		CreateGradientPipeline(&blend);
		CreateBlurPipeline();
		CreateCompositePipelines();
	}

	// Composites a full-size layer texture into an MSAA pass. SrcOver for plain/opacity/colorfilter layers,
	// DstIn (out = dst * src.a) for mask layers. Optional color matrix (params.x) applied to the layer content.
	private const string CompositeWgsl = @"
struct CU { params: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var src: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: CU;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  var c = textureSampleLevel(src, smp, i.uv, 0.0);   // premultiplied layer content
  if (u.params.x > 0.5) {
    var s = c;
    if (c.a > 0.0) { s = vec4<f32>(c.rgb / c.a, c.a); }
    let r = vec4<f32>(dot(u.m0, s) + u.off.x, dot(u.m1, s) + u.off.y, dot(u.m2, s) + u.off.z, dot(u.m3, s) + u.off.w);
    let rc = clamp(r, vec4<f32>(0.0), vec4<f32>(1.0));
    c = vec4<f32>(rc.rgb * rc.a, rc.a);
  }
  return c * u.params.y;
}";

	private void CreateCompositePipelines()
	{
		var module = Module(CompositeWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.Always, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var over = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add } };
		var dstIn = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.Zero, DstFactor = WGPUBlendFactor.SrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.Zero, DstFactor = WGPUBlendFactor.SrcAlpha, Operation = WGPUBlendOperation.Add } };
		CompositeSrcOver = MakeComposite(module, vs, fs, &over, &ds);
		CompositeDstIn = MakeComposite(module, vs, fs, &dstIn, &ds);
		CompositeBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeSrcOver, 0);
		CompositeDstInBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeDstIn, 0);
	}

	private IntPtr MakeComposite(IntPtr module, WGPUStringView vs, WGPUStringView fs, WGPUBlendState* blend, WGPUDepthStencilState* ds)
	{
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	// One separable-gaussian pass over a texture. A fullscreen triangle (from vertex_index, no vertex buffer)
	// samples the source along `dir` with per-tap gaussian weights; radius = ceil(3*sigma). Two passes
	// (dir = (1,0) then (0,1)) give a full 2D blur. Single-sample, no blend (overwrite), no depth/stencil.
	private const string BlurWgsl = @"
struct BU { dir: vec2<f32>, texel: vec2<f32>, sigma: f32, pad0: f32, pad1: f32, pad2: f32 };
@group(0) @binding(0) var src: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> b: BU;
struct VO { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
@vertex fn vs(@builtin(vertex_index) vi: u32) -> VO {
  var pts = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  let p = pts[vi];
  var o: VO; o.p = vec4<f32>(p, 0.0, 1.0); o.uv = vec2<f32>((p.x + 1.0) * 0.5, (1.0 - p.y) * 0.5); return o;
}
@fragment fn fs(i: VO) -> @location(0) vec4<f32> {
  let sigma = max(b.sigma, 1e-4);
  let r = i32(ceil(sigma * 3.0));
  var sum = vec4<f32>(0.0);
  var wsum = 0.0;
  for (var k = -r; k <= r; k = k + 1) {
    let w = exp(-0.5 * f32(k * k) / (sigma * sigma));
    sum = sum + textureSampleLevel(src, smp, i.uv + b.dir * b.texel * f32(k), 0.0) * w;
    wsum = wsum + w;
  }
  return sum / max(wsum, 1e-6);
}";

	private void CreateBlurPipeline()
	{
		var module = Module(BlurWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 };
		var target = new WGPUColorTargetState { Format = DefaultColorFormat, Blend = null, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = null,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		BlurPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		BlurBgl = wgpuRenderPipelineGetBindGroupLayout(BlurPipe, 0);
	}

	// Evaluates a linear/radial gradient per pixel. The quad is positioned in NDC; the fragment uses its
	// framebuffer position (device pixels) so the gradient geometry can be baked to device space at record time.
	private const string GradientWgsl = @"
struct Grad { header: vec4<f32>, geo: vec4<f32>, colors: array<vec4<f32>, 16>, stops: array<vec4<f32>, 4>, origin: vec4<f32> };
@group(0) @binding(0) var<uniform> g: Grad;
@group(1) @binding(0) var<uniform> clip: ClipU;
@group(1) @binding(1) var clipTex: texture_2d<f32>;
@group(1) @binding(2) var clipSmp: sampler;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return vec4<f32>(pos, 0.0, 1.0); }
fn stopAt(i: i32) -> f32 { return g.stops[i / 4][i % 4]; }
@fragment fn fs(@builtin(position) fc: vec4<f32>) -> @location(0) vec4<f32> {
  var t: f32 = 0.0;
  if (g.header.x < 0.5) {
    let a = g.geo.xy; let b = g.geo.zw; let ab = b - a; let denom = dot(ab, ab);
    if (denom > 0.0) { t = dot(fc.xy - a, ab) / denom; }
  } else {
    // Radial: normalize to unit-ellipse space, then param along the ray from the focal origin so t=0 at the
    // origin and t=1 at the ellipse edge. origin == center reduces to plain concentric distance.
    let c = g.geo.xy; let rx = g.geo.z; let ry = g.geo.w;
    if (rx > 0.0 && ry > 0.0) {
      let inv = vec2<f32>(1.0 / rx, 1.0 / ry);
      let pn = (fc.xy - c) * inv;
      let on = (g.origin.xy - c) * inv;
      let dir = pn - on; let aa = dot(dir, dir);
      if (aa < 1e-9) { t = 0.0; }
      else {
        let bb = 2.0 * dot(on, dir); let cc = dot(on, on) - 1.0;
        let s = (-bb + sqrt(max(bb * bb - 4.0 * aa * cc, 0.0))) / (2.0 * aa);
        t = 1.0 / max(s, 1e-6);
      }
    }
  }
  let tm = g.header.z;
  if (tm < 0.5) { t = clamp(t, 0.0, 1.0); }
  else if (tm < 1.5) { t = fract(t); }
  else { let f = fract(t * 0.5) * 2.0; if (f > 1.0) { t = 2.0 - f; } else { t = f; } }
  let n = i32(g.header.y);
  var col = g.colors[0];
  if (t <= stopAt(0)) { col = g.colors[0]; }
  else if (t >= stopAt(n - 1)) { col = g.colors[n - 1]; }
  else {
    for (var i = 0; i < n - 1; i = i + 1) {
      let s0 = stopAt(i); let s1 = stopAt(i + 1);
      if (t >= s0 && t <= s1) {
        var u = 0.0;
        if (s1 > s0) { u = (t - s0) / (s1 - s0); }
        col = mix(g.colors[i], g.colors[i + 1], u);
        break;
      }
    }
  }
  return vec4<f32>(col.rgb, col.a * clipCov(fc.xy, clip, clipTex, clipSmp));
}";

	private void CreateGradientPipeline(WGPUBlendState* blend)
	{
		var module = Module(ClipStructFn + GradientWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var attr = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 8, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 1, Attributes = &attr };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.Always, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = IntPtr.Zero };
		GradientPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		GradBgl = wgpuRenderPipelineGetBindGroupLayout(GradientPipe, 0);
		GradClipBgl = wgpuRenderPipelineGetBindGroupLayout(GradientPipe, 1);
	}

	private const string ImageWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
struct U { op: vec4<f32>, tint: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var tex: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: U;
@group(1) @binding(0) var<uniform> clip: ClipU;
@group(1) @binding(1) var clipTex: texture_2d<f32>;
@group(1) @binding(2) var clipSmp: sampler;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut { var o: VOut; o.p = vec4<f32>(pos, 0.0, 1.0); o.uv = uv; return o; }
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> {
  var c = textureSample(tex, smp, i.uv);   // premultiplied
  if (u.op.z > 0.5) {
    // 4x5 colour matrix (effect brush): unpremultiply -> matrix + offset -> clamp -> premultiply.
    var s = c;
    if (c.a > 0.0) { s = vec4<f32>(c.rgb / c.a, c.a); }
    let r = vec4<f32>(dot(u.m0, s) + u.off.x, dot(u.m1, s) + u.off.y, dot(u.m2, s) + u.off.z, dot(u.m3, s) + u.off.w);
    let rc = clamp(r, vec4<f32>(0.0), vec4<f32>(1.0));
    c = vec4<f32>(rc.rgb * rc.a, rc.a);
  } else if (u.op.y > 0.5) {
    // SrcIn blend-mode tint: premultiplied(filterColor) * dst.a.
    let fp = vec4<f32>(u.tint.rgb * u.tint.a, u.tint.a);
    c = fp * c.a;
  }
  return c * u.op.x * clipCov(i.p.xy, clip, clipTex, clipSmp);
}";

	private void CreateImagePipeline()
	{
		var module = Module(ClipStructFn + ImageWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var attrs = stackalloc WGPUVertexAttribute[2];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 };
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 16, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 2, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		// premultiplied image pixels -> One/OneMinusSrcAlpha
		var blend = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add } };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = &blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.Always, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = IntPtr.Zero };
		ImagePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		ImgBgl = wgpuRenderPipelineGetBindGroupLayout(ImagePipe, 0);
		ImageClipBgl = wgpuRenderPipelineGetBindGroupLayout(ImagePipe, 1);
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = wgpuDeviceCreateSampler(Dev, &sd);
	}

	private IntPtr MakePipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, bool colorWrite, bool colorAttrs, WGPUBlendState* blend, WGPUStencilFaceState front, WGPUStencilFaceState back, uint stencilWrite, uint stencilRead)
	{
		var attrs = stackalloc WGPUVertexAttribute[2];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		var stride = 8ul;
		var attrCount = 1u;
		if (colorAttrs)
		{
			attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x4, Offset = 8, ShaderLocation = 1 };
			stride = 24; attrCount = 2;
		}
		var vbl = new WGPUVertexBufferLayout { ArrayStride = stride, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = attrCount, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = colorWrite ? WGPUColorWriteMask.All : 0 };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var ds = new WGPUDepthStencilState
		{
			Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.Always,
			StencilFront = front, StencilBack = back, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	// Persistent UTF-8 for a WGPUStringView (WGSL/entry points; created once at pipeline init, intentionally not freed).
	private static WGPUStringView SV(string s)
		=> new() { Data = Marshal.StringToCoTaskMemUTF8(s), Length = (nuint)System.Text.Encoding.UTF8.GetByteCount(s) };

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAdapter(WGPURequestAdapterStatus status, IntPtr adapter, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = adapter;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDevice(WGPURequestDeviceStatus status, IntPtr device, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = device;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMap(WGPUMapAsyncStatus status, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((bool[])GCHandle.FromIntPtr(u1).Target!)[0] = true;

	// Flushes an offscreen surface so a later, separately-submitted pass can sample its resolved content.
	// A layer's offscreen render + MSAA resolve is one queue submission; the composite that samples it is a
	// later one, and the resolve→sample hazard isn't covered across submissions — without this the composite
	// samples an empty texture. A full texture→buffer copy + map (what ReadPixelsRgba does) forces the deferred
	// MSAA resolve to materialize; a device-idle wait or a partial copy is NOT enough. Perf follow-up: encode the
	// offscreen render and its composite in one command buffer so wgpu barriers it automatically.
	public void FlushForSample(WebGpuRenderSurface s)
	{
		if (s.Tex != IntPtr.Zero) { _ = ReadPixelsRgba(s); }
	}

	public void Dispose() { }
}

// Transient GPU-texture pool for the per-frame offscreens (shadow/backdrop/layer/path-coverage surfaces + blur
// temps). BeginFrame marks all entries free; Rent reuses a free entry matching the key or creates one — so a
// steady-state frame allocates nothing. Every renter clears (LoadOp.Clear) before writing, so reuse is safe.
// (These offscreens stay "in use" until the frame's main pass samples them; reuse happens across frames.)
public sealed unsafe class WebGpuTexturePool
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Tex; public IntPtr View; public int W, H, Samples; public WGPUTextureFormat Fmt; public WGPUTextureUsage Usage; public bool InUse; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// The pool is shared per-device, so an off-loop render (e.g. RenderTargetBitmap) can hit it concurrently
	// with the on-window render loop. Guard mutation/enumeration so a concurrent Add can't invalidate Rent's walk.
	private readonly object _gate = new();

	public WebGpuTexturePool(WebGpuDevice d) => _d = d;

	public void BeginFrame() { lock (_gate) { foreach (var e in _entries) { e.InUse = false; } } }

	public IntPtr Rent(int w, int h, int samples, WGPUTextureUsage usage, WGPUTextureFormat fmt)
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (!e.InUse && e.W == w && e.H == h && e.Samples == samples && e.Fmt == fmt && e.Usage == usage) { e.InUse = true; return e.View; }
			}
			var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = fmt, MipLevelCount = 1, SampleCount = (uint)samples, Dimension = WGPUTextureDimension._2D, Usage = usage };
			var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
			var view = wgpuTextureCreateView(tex, null);
			_entries.Add(new Entry { Tex = tex, View = view, W = w, H = h, Samples = samples, Fmt = fmt, Usage = usage, InUse = true });
			return view;
		}
	}

	/// <summary>The backing texture for a rented view, or Zero if unknown. Used to flush an offscreen's resolve so
	/// a later, separately-submitted pass can sample it.</summary>
	public IntPtr TexForView(IntPtr view)
	{
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { return e.Tex; } } }
		return IntPtr.Zero;
	}
}

// Transient GPU-buffer pool (vertex + uniform buffers). Like the texture pool: BeginFrame frees all; Rent
// reuses a free buffer of the same usage with enough capacity or creates one, so a steady-state frame allocates
// no buffers. Callers QueueWriteBuffer their data before use.
public sealed unsafe class WebGpuBufferPool
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Buf; public int Cap; public WGPUBufferUsage Usage; public bool InUse; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// Shared per-device; guard against concurrent Add invalidating Rent's enumeration (see WebGpuTexturePool).
	private readonly object _gate = new();

	public WebGpuBufferPool(WebGpuDevice d) => _d = d;

	public void BeginFrame() { lock (_gate) { foreach (var e in _entries) { e.InUse = false; } } }

	public IntPtr Rent(int byteSize, WGPUBufferUsage usage)
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (!e.InUse && e.Usage == usage && e.Cap >= byteSize) { e.InUse = true; return e.Buf; }
			}
			int cap = Math.Max(byteSize, 256);
			var bd = new WGPUBufferDescriptor { Size = (nuint)cap, Usage = usage };
			var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
			_entries.Add(new Entry { Buf = buf, Cap = cap, Usage = usage, InUse = true });
			return buf;
		}
	}
}

public sealed unsafe class WebGpuRenderSurface : IRenderTarget
{
	public IntPtr Tex;
	public IntPtr View;              // single-sample resolve target (offscreen readback / swapchain image)
	public IntPtr MsaaColorTex;
	public IntPtr MsaaColorView;     // multisampled color the pass renders into, resolved into View
	public IntPtr DepthTex;
	public IntPtr DepthView;         // multisampled depth/stencil (clip mask + stencil-then-cover)
	public int Width { get; }
	public int Height { get; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;

	// Pooled surfaces rent their views from the WebGpuTexturePool, which owns and reclaims them — Dispose must not
	// touch those. Directly-created surfaces (offscreen readback / swapchain resolve) own their textures and MUST
	// release them, otherwise every window resize leaks a full-window MSAA color + depth texture until VRAM is
	// exhausted (wgpuDeviceCreateTexture: "Not enough memory left").
	private readonly bool _ownsResources = true;

	public void Dispose()
	{
		if (!_ownsResources)
		{
			return;
		}
		if (View != IntPtr.Zero) { wgpuTextureViewRelease(View); View = IntPtr.Zero; }
		if (Tex != IntPtr.Zero) { wgpuTextureDestroy(Tex); Tex = IntPtr.Zero; }
		if (MsaaColorView != IntPtr.Zero) { wgpuTextureViewRelease(MsaaColorView); MsaaColorView = IntPtr.Zero; }
		if (MsaaColorTex != IntPtr.Zero) { wgpuTextureDestroy(MsaaColorTex); MsaaColorTex = IntPtr.Zero; }
		if (DepthView != IntPtr.Zero) { wgpuTextureViewRelease(DepthView); DepthView = IntPtr.Zero; }
		if (DepthTex != IntPtr.Zero) { wgpuTextureDestroy(DepthTex); DepthTex = IntPtr.Zero; }
	}

	public WebGpuRenderSurface(WebGpuDevice device, int width, int height)
	{
		Width = width; Height = height;
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 }, Format = device.ColorFormat,
			MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D,
			// TextureBinding so a resolved surface can be sampled (e.g. shadow coverage feeding the blur pass).
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.CopySrc | WGPUTextureUsage.TextureBinding,
		};
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		CreateMultisampledTargets(device, width, height);
	}

	// External-color variant for a swapchain: the color View/Tex are provided per frame (the acquired
	// swapchain image, used as the resolve target); the multisampled color + depth are owned here.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		Width = width; Height = height;
		CreateMultisampledTargets(device, width, height);
	}

	// Pooled transient offscreen: MSAA color + depth + a single-sample resolve target (sampled later), all rented
	// from the pool so a steady-state frame allocates nothing. Dispose is a no-op (the pool reclaims on BeginFrame).
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, WebGpuTexturePool pool)
	{
		Width = width; Height = height;
		_ownsResources = false;   // the pool owns and reclaims these; Dispose must not release them
		MsaaColorView = pool.Rent(width, height, (int)WebGpuDevice.MsaaSamples, WGPUTextureUsage.RenderAttachment, device.ColorFormat);
		DepthView = pool.Rent(width, height, (int)WebGpuDevice.MsaaSamples, WGPUTextureUsage.RenderAttachment, WebGpuDevice.DepthStencilFormat);
		// CopySrc so the resolved result can be flushed (see WebGpuDevice.FlushForSample) before a later,
		// separately-submitted pass samples it.
		View = pool.Rent(width, height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc, device.ColorFormat);
		Tex = pool.TexForView(View);
	}

	private void CreateMultisampledTargets(WebGpuDevice device, int width, int height)
	{
		var cd = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 }, Format = device.ColorFormat,
			MipLevelCount = 1, SampleCount = WebGpuDevice.MsaaSamples, Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment,
		};
		MsaaColorTex = wgpuDeviceCreateTexture(device.Dev, &cd);
		MsaaColorView = wgpuTextureCreateView(MsaaColorTex, null);

		var dd = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 }, Format = WebGpuDevice.DepthStencilFormat,
			MipLevelCount = 1, SampleCount = WebGpuDevice.MsaaSamples, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.RenderAttachment,
		};
		DepthTex = wgpuDeviceCreateTexture(device.Dev, &dd);
		DepthView = wgpuTextureCreateView(DepthTex, null);
	}
}

// A clip is a device-space scissor AABB (fast reject + plain-rect clip) plus an optional device-space,
// axis-aligned rounded-rect whose corners are masked per-fragment in the shaders. A rotated rounded clip
// degrades to its AABB (the exact fix is clip-local-space eval, as with the radial gradient — follow-up).
internal struct ClipData
{
	public Vector4 Aabb;    // device L,T,R,B scissor
	public bool HasRound;
	public Vector4 Rect;    // device rounded-rect L,T,R,B
	public Vector4 Radii;   // per-corner radius (TL,TR,BR,BL), device px
	public bool HasExclude; // Difference op: keep the area OUTSIDE the rounded Rect (PushClipExclude) rather than inside
	// Arbitrary path clip: the flattened device-space fan is rendered to a full-size coverage texture at present
	// time and sampled per-fragment. Innermost path wins (like the rounded shape); nested paths keep the AABB.
	public float[] PathFan;
	public bool PathEvenOdd;
	public static ClipData None => new() { Aabb = new Vector4(-1e9f, -1e9f, 1e9f, 1e9f) };
}

// Draw commands share one ordered stream so cross-type z-order (rect over path over image) is preserved.
internal abstract class WebGpuCommand
{
	public ClipData Clip;
}

internal sealed class RectCommand : WebGpuCommand
{
	public WColor Color;
	public Vector2 P0, P1, P2, P3;
}

internal sealed class PathFill : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public WColor Color;
	public bool EvenOdd;
}

internal sealed unsafe class ImageCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;
	public IntPtr View;   // the pre-uploaded WebGpuImageTexture view (no per-frame upload)
	public int W, H;
	public float Opacity;
	public float U0, V0, U1 = 1f, V1 = 1f;   // source UV sub-rect (whole texture by default)
	public int TintMode;        // 0 = none, 1 = SrcIn blend-mode tint
	public Vector4 Tint;        // straight-alpha tint color (0..1) for TintMode 1
	public float[] ColorMatrix; // null, or 20-float (4x5) effect colour matrix applied in the image shader
}

internal sealed class GradientCmd : WebGpuCommand
{
	public Vector2 P0, P1, P2, P3;   // device-space quad
	public float[] Uniform;          // packed Grad struct (WebGpuDevice.GradientUniformBytes / 4 floats)
}

// A drop shadow: the silhouette (flattened, device space) is filled into an offscreen coverage texture,
// separably gaussian-blurred (SigmaX/Y), then composited tinted by Color. Same fan/bbox form as PathFill.
internal sealed class ShadowCmd : WebGpuCommand
{
	public float[] FanDevice;
	public Vector2 BbMin, BbMax;
	public bool EvenOdd;
	public WColor Color;
	public float SigmaX, SigmaY;
	public bool Additive;
}

// A SaveLayer group: its Commands are rendered into a full-size offscreen surface, then composited onto the
// parent with CompositeMode (0 = SrcOver, 1 = DstIn mask) and an optional color matrix (SaveLayer(IColorFilter)).
internal sealed class LayerCmd : WebGpuCommand
{
	public List<WebGpuCommand> Commands;
	public int CompositeMode;   // 0 = SrcOver, 1 = DstIn
	public float[] ColorMatrix; // null, or 20-float (4x5) color matrix applied at composite
	public WebGpuEffectFilter ShadowEffect; // SaveLayer(IEffectFilter): a drop shadow derived from the content
}

// DrawEffectBackdrop (acrylic): the content drawn BEFORE this in the frame is captured, gaussian-blurred by
// Effect's sigma, drawn clipped to the effect region, then tinted by Effect.Color. Effect-graph realization is
// simplified to blur + tint (the dominant acrylic visual), not the full IGraphicsEffect DAG.
internal sealed class BackdropCmd : WebGpuCommand
{
	public WebGpuEffectFilter Effect;
	public float Opacity;
}

// A deferred replay of a cacheable child recording under a transform+clip. Captures the child's IMMUTABLE
// command-list reference (not the recording): the frame is presented on the render thread while the main thread
// may Dispose the recording — Dispose only nulls the recording's field, so the captured list stays valid. The
// GPU geometry cache lives on the render-thread device, keyed by this list (the per-visual slab/scroll win).
internal sealed class ReplayRefCmd : WebGpuCommand
{
	public System.Collections.Generic.List<WebGpuCommand> Commands;
	public System.Numerics.Matrix4x4 Transform;
}

// Persistent (non-pooled) GPU resources for a cached recording, released on eviction. Separate from the per-frame
// pool so cached draws survive across frames.
internal sealed class OwnedResources
{
	public System.Collections.Generic.List<nint> Buffers = new();
	public System.Collections.Generic.List<nint> BindGroups = new();
	public System.Collections.Generic.List<nint> Bundles = new();
}

// A recording's cached GPU geometry, owned by the render-thread device and keyed by the immutable command list.
internal sealed unsafe class WebGpuGeometryCache
{
	public List<(int kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)> Ops;
	public OwnedResources Owned;
	public IntPtr Bundle;
	public Matrix4x4 Transform;
	public ClipData Clip;
	public bool Used;
}

// Backend-created gradient shader handle. The WebGPU backend mints its own (rather than delegating to Skia) so
// the recorder can read the gradient parameters back and evaluate them in the WGSL gradient pipeline.
public sealed class WebGpuShader : IShader
{
	public bool Radial;
	public Vector2 P0;          // start (linear) / center (radial), &gradient-local space
	public Vector2 P1;          // end (linear) / gradient origin (radial)
	public float RadiusX, RadiusY;
	public WColor[] Colors;
	public float[] Stops;
	public GradientTileMode TileMode;
	public Matrix3x2 LocalMatrix;
}

// Backend-owned color filter so the WebGPU renderer can read the tint params (an IColorFilter is opaque —
// consumed only by the paired renderer). Currently the SrcIn blend-mode tint (image fade/tint, the only
// DrawImage color-filter case) is honored; other modes / the color matrix carry through but the image path
// applies only SrcIn for now.
public sealed class WebGpuColorFilter : IColorFilter
{
	public bool IsBlendMode;
	public WColor Color;
	public BlendMode Mode;
	public float[] Matrix;
}

// Backend-owned effect filter. Today only the drop shadow (SaveLayer(IEffectFilter) from Visual/ShadowState):
// the layer content is blurred, tinted by Color and offset by (Dx,Dy), drawn behind the content.
public sealed class WebGpuEffectFilter : IEffectFilter
{
	public float Dx, Dy, SigmaX, SigmaY;
	public WColor Color;      // acrylic tint (composited SrcOver on top) / drop-shadow color
	public WColor LumColor;   // acrylic luminosity color (SrcOver over the blurred backdrop == mix(blurred, lum.rgb, lum.a))
	public void Dispose() { }
}

public sealed class WebGpuRenderData : IRenderData
{
	internal List<WebGpuCommand> Commands = new();
	internal WColor? ClearColor;
	internal bool? Cacheable;   // memoized: all commands are simple primitives with no path clip

	// Dispose only nulls the field; the command LIST object stays alive while any in-flight frame's ReplayRef
	// still references it (captured by reference), and the device's geometry cache is keyed on that list.
	public void Dispose() { Commands = null; }
}

public sealed unsafe class WebGpuCommandRecorder : ICommandRecorder, IFlattenedPathSink
{
	// A save frame carries the matrix/clip to restore. Layer frames additionally redirect emitted commands into
	// a sub-list until Restore, which composites that sub-list (as a LayerCmd) back onto the parent.
	private struct SaveEntry { public Matrix4x4 M; public ClipData Clip; public bool IsLayer; public List<WebGpuCommand> ParentTarget; public int CompositeMode; public float[] ColorMatrix; public WebGpuEffectFilter Effect; public float[] PendingColorMatrix; }
	private readonly Stack<SaveEntry> _stack = new();
	private Matrix4x4 _m = Matrix4x4.Identity;
	private ClipData _clip = ClipData.None;
	private float[] _pendingColorMatrix;   // active effect colour matrix, applied per DrawImage in the image shader
	private readonly WebGpuRenderData _data = new();
	private List<WebGpuCommand> _target;   // current emit target (root command list, or a layer's list)

	public WebGpuCommandRecorder() => _target = _data.Commands;

	public Matrix4x4 TotalMatrix => _m;
	public void SetMatrix(in Matrix4x4 matrix) => _m = matrix;
	public void Concat(in Matrix4x4 matrix) => _m = matrix * _m;
	public void Translate(float dx, float dy) => _m = Matrix4x4.CreateTranslation(dx, dy, 0) * _m;
	public void Scale(float sx, float sy) => _m = Matrix4x4.CreateScale(sx, sy, 1) * _m;
	// Returns the PRE-push depth, matching SKCanvas.Save(): RestoreToCount(count) pops entries while Count > count,
	// so it must be handed the depth to restore *to* (before this save). Returning the post-push count made
	// RestoreToCount a no-op, leaking _m/_clip across sibling visuals (identity-local visuals — e.g. opaque
	// container backgrounds — inherited a sibling's transform and painted over content).
	public int Save() { var pre = _stack.Count; _stack.Push(new SaveEntry { M = _m, Clip = _clip, PendingColorMatrix = _pendingColorMatrix }); return pre; }
	public int SaveCount => _stack.Count;
	public void Restore()
	{
		if (_stack.Count == 0) { return; }
		var t = _stack.Pop(); _m = t.M; _clip = t.Clip; _pendingColorMatrix = t.PendingColorMatrix;
		if (t.IsLayer)
		{
			var layerCmds = _target;
			_target = t.ParentTarget;
			_target.Add(new LayerCmd { Commands = layerCmds, CompositeMode = t.CompositeMode, ColorMatrix = t.ColorMatrix, ShadowEffect = t.Effect, Clip = _clip });
		}
	}
	public void RestoreToCount(int count) { while (_stack.Count > count) { Restore(); } }

	private void PushLayer(int compositeMode, float[] colorMatrix, WebGpuEffectFilter effect = null)
	{
		_stack.Push(new SaveEntry { M = _m, Clip = _clip, IsLayer = true, ParentTarget = _target, CompositeMode = compositeMode, ColorMatrix = colorMatrix, Effect = effect, PendingColorMatrix = _pendingColorMatrix });
		_target = new List<WebGpuCommand>();
	}
	public void SaveLayer(bool antialias = false) => PushLayer(0, null);
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false)
	{
		// A 4x5 colour-matrix filter (effect brush): apply it directly in the image shader — matching the original
		// webgpu branch's AddImage(colorMatrix) — instead of an offscreen layer. Scope it to the matching Restore.
		if ((colorFilter as WebGpuColorFilter)?.Matrix is { } matrix)
		{
			_stack.Push(new SaveEntry { M = _m, Clip = _clip, PendingColorMatrix = _pendingColorMatrix });
			_pendingColorMatrix = matrix;
			return;
		}
		PushLayer(0, null);
	}
	public void SaveLayer(BlendMode blendMode, bool antialias = false) => PushLayer(blendMode == BlendMode.DstIn ? 1 : 0, null);
	public void SaveLayer(IEffectFilter filter) => PushLayer(0, null, filter as WebGpuEffectFilter);
	// Device-space AABB of a mapped rect (its 4 corners), for the scissor / fast reject.
	private Vector4 DeviceAabb(in Rect rect)
	{
		var a = Map((float)rect.Left, (float)rect.Top); var b = Map((float)rect.Right, (float)rect.Top);
		var c = Map((float)rect.Right, (float)rect.Bottom); var d = Map((float)rect.Left, (float)rect.Bottom);
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, d.X)); var t = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, d.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, d.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, d.Y));
		return new Vector4(l, t, r, bo);
	}

	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// Tighten the scissor AABB; any active rounded shape is preserved (Intersect only).
		var a = DeviceAabb(rect);
		_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, a.X), MathF.Max(_clip.Aabb.Y, a.Y), MathF.Min(_clip.Aabb.Z, a.Z), MathF.Min(_clip.Aabb.W, a.W));
	}

	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		var aabb = DeviceAabb(roundRect.Rect);
		// Device-space, axis-aligned rounded rect (exact under scale/translate). Per-corner radius uses X, scaled
		// by the matrix's X-axis length; a full rotation would need clip-local eval (falls back to the AABB below).
		var s = new Vector2(_m.M11, _m.M12).Length();
		_clip.HasRound = true;
		_clip.Rect = aabb;
		_clip.Radii = new Vector4(roundRect.TopLeft.X * s, roundRect.TopRight.X * s, roundRect.BottomRight.X * s, roundRect.BottomLeft.X * s);
		// Difference (PushClipExclude): keep the area OUTSIDE the rounded rect — so DON'T tighten the scissor to it
		// (the visible region extends past the rect); the per-fragment clipCov inverts the coverage.
		_clip.HasExclude = operation == ClipOperation.Difference;
		if (!_clip.HasExclude)
		{
			_clip.Aabb = new Vector4(MathF.Max(_clip.Aabb.X, aabb.X), MathF.Max(_clip.Aabb.Y, aabb.Y), MathF.Min(_clip.Aabb.Z, aabb.Z), MathF.Min(_clip.Aabb.W, aabb.W));
		}
	}

	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false)
	{
		// Tighten the scissor to the path bounds, and capture the flattened device-space fan for an exact
		// per-fragment coverage mask (built at present time).
		ClipRect(geometry.Bounds, operation, antialias);
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_clip.PathFan = _fan.ToArray();
			_clip.PathEvenOdd = geometry.FillRule == GeometryFillRule.EvenOdd;
		}
		_fan = null;
	}
	public void Clear(WColor color) => _data.ClearColor = color;

	private Vector2 Map(float x, float y) => new(x * _m.M11 + y * _m.M21 + _m.M41, x * _m.M12 + y * _m.M22 + _m.M42);

	public void DrawRect(in Rect rect, WColor color, bool antialias = false)
		=> _target.Add(new RectCommand
		{
			Color = color, Clip = _clip,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
		});

	private List<float> _fan;
	private Vector2 _pivot, _prev, _bbMin, _bbMax;
	private bool _firstInContour;

	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false)
		=> FillGeometry(geometry, color, geometry.FillRule == GeometryFillRule.EvenOdd);

	private void FillGeometry(IGeometry geometry, WColor color, bool evenOdd)
	{
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		geometry.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_target.Add(new PathFill { FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax, Color = color, EvenOdd = evenOdd, Clip = _clip });
		}
		_fan = null;
	}

	void IFlattenedPathSink.BeginContour(Vector2 start) { _pivot = Map(start.X, start.Y); _prev = _pivot; _firstInContour = true; Include(_pivot); }
	void IFlattenedPathSink.LineTo(Vector2 point)
	{
		var p = Map(point.X, point.Y); Include(p);
		if (_firstInContour) { _firstInContour = false; }
		else { _fan.Add(_pivot.X); _fan.Add(_pivot.Y); _fan.Add(_prev.X); _fan.Add(_prev.Y); _fan.Add(p.X); _fan.Add(p.Y); }
		_prev = p;
	}
	void IFlattenedPathSink.EndContour(bool closed) { }
	private void Include(Vector2 p) { _bbMin = Vector2.Min(_bbMin, p); _bbMax = Vector2.Max(_bbMax, p); }

	public void DrawRect(in Rect rect, IShader shader, bool antialias = false)
	{
		if (shader is not WebGpuShader g)
		{
			return;
		}

		// Compose the gradient's local matrix with the current matrix, so gradient geometry is baked to device
		// space (the WGSL fragment evaluates in device pixels). Linear is exact under affine; radial is elliptical
		// (each radius scaled by its own device-axis length) — exact under scale/translate. Rotation and a focal
		// origin != center remain approximate; the exact fix is the original's gradient-local-space eval (follow-up).
		var lm = new Matrix4x4(
			g.LocalMatrix.M11, g.LocalMatrix.M12, 0, 0,
			g.LocalMatrix.M21, g.LocalMatrix.M22, 0, 0,
			0, 0, 1, 0,
			g.LocalMatrix.M31, g.LocalMatrix.M32, 0, 1);
		var m = lm * _m;
		Vector2 MapM(Vector2 p) => new(p.X * m.M11 + p.Y * m.M21 + m.M41, p.X * m.M12 + p.Y * m.M22 + m.M42);
		var a = MapM(g.P0);
		var b = MapM(g.P1);
		var sx = new Vector2(m.M11, m.M12).Length();
		var sy = new Vector2(m.M21, m.M22).Length();

		var count = Math.Min(g.Colors?.Length ?? 0, WebGpuDevice.MaxGradientStops);
		if (count == 0)
		{
			return;
		}

		var u = new float[WebGpuDevice.GradientUniformBytes / 4];
		u[0] = g.Radial ? 1f : 0f;
		u[1] = count;
		u[2] = g.TileMode switch { GradientTileMode.Repeat => 1f, GradientTileMode.Mirror => 2f, _ => 0f };
		if (g.Radial)
		{
			u[4] = a.X; u[5] = a.Y; u[6] = g.RadiusX * sx; u[7] = g.RadiusY * sy;
			u[88] = b.X; u[89] = b.Y;   // focal origin (device); b = mapped g.P1 (gradientOrigin)
		}
		else
		{
			u[4] = a.X; u[5] = a.Y; u[6] = b.X; u[7] = b.Y;
		}

		for (var i = 0; i < count; i++)
		{
			var c = g.Colors[i];
			u[8 + i * 4] = c.R / 255f;
			u[8 + i * 4 + 1] = c.G / 255f;
			u[8 + i * 4 + 2] = c.B / 255f;
			u[8 + i * 4 + 3] = c.A / 255f;
			u[72 + i] = g.Stops is { Length: > 0 } && i < g.Stops.Length ? g.Stops[i] : (count > 1 ? i / (float)(count - 1) : 0f);
		}

		_target.Add(new GradientCmd
		{
			Clip = _clip, Uniform = u,
			P0 = Map((float)rect.Left, (float)rect.Top), P1 = Map((float)rect.Right, (float)rect.Top),
			P2 = Map((float)rect.Right, (float)rect.Bottom), P3 = Map((float)rect.Left, (float)rect.Bottom),
		});
	}
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false)
	{
		_fan = new List<float>();
		_bbMin = new Vector2(float.MaxValue); _bbMax = new Vector2(float.MinValue);
		silhouette.StreamFlattened(this);
		if (_fan.Count > 0)
		{
			_target.Add(new ShadowCmd
			{
				FanDevice = _fan.ToArray(), BbMin = _bbMin, BbMax = _bbMax,
				EvenOdd = silhouette.FillRule == GeometryFillRule.EvenOdd,
				Color = color, SigmaX = sigmaX, SigmaY = sigmaY, Additive = additive, Clip = _clip,
			});
		}
		_fan = null;
	}
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false)
	{
		using var sg = geometry.GetStrokeFillGeometry(new StrokeStyle { Thickness = strokeWidth, LineJoin = StrokeJoin.Miter, MiterLimit = 10f });
		FillGeometry(sg, color, evenOdd: false);
	}
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false)
	{
		var dir = p1 - p0; var len = dir.Length(); if (len < 1e-4f) { return; } dir /= len;
		var n = new Vector2(-dir.Y, dir.X) * (strokeWidth / 2f);
		_target.Add(new RectCommand
		{
			Color = color, Clip = _clip,
			P0 = Map(p0.X + n.X, p0.Y + n.Y), P1 = Map(p1.X + n.X, p1.Y + n.Y),
			P2 = Map(p1.X - n.X, p1.Y - n.Y), P3 = Map(p0.X - n.X, p0.Y - n.Y),
		});
	}
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false)
	{
		if (texture is not WebGpuImageTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		// No per-frame upload — the texture is already resident; record its view for the present pass.
		_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = opacity, ColorMatrix = _pendingColorMatrix, Clip = _clip });
	}
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false)
	{
		if (texture is not WebGpuImageTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }
		// A 4x5 colour-matrix filter (e.g. MonochromeColor / effect brush): apply it in the image shader.
		// The SrcIn blend-mode tint stays the fast path.
		if (colorFilter is WebGpuColorFilter { Matrix: { } matrix })
		{
			_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = 1f, ColorMatrix = matrix, Clip = _clip });
			return;
		}
		var (mode, tint) = ResolveTint(colorFilter);
		_target.Add(new ImageCmd { P0 = Map(x, y), P1 = Map(x + w, y), P2 = Map(x + w, y + h), P3 = Map(x, y + h), View = t.View, W = w, H = h, Opacity = 1f, TintMode = mode, Tint = tint, ColorMatrix = _pendingColorMatrix, Clip = _clip });
	}

	// A SrcIn blend-mode WebGpuColorFilter → a straight-alpha tint (the only image color-filter case today);
	// anything else (other modes, color matrix, or a foreign filter) → untinted.
	private static (int mode, Vector4 tint) ResolveTint(IColorFilter colorFilter)
		=> colorFilter is WebGpuColorFilter { IsBlendMode: true, Mode: BlendMode.SrcIn } f
			? (1, new Vector4(f.Color.R / 255f, f.Color.G / 255f, f.Color.B / 255f, f.Color.A / 255f))
			: (0, default);

	public void DrawImageNineSlice(IImageTexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false)
	{
		if (texture is not WebGpuImageTexture t) { return; }
		int w = t.PixelWidth, h = t.PixelHeight; if (w <= 0 || h <= 0) { return; }

		// Source (pixel) column/row edges from the center slice, and the matching destination edges: the corner
		// insets keep their source pixel size, the middle band stretches to fill the rest of the destination.
		float sx0 = 0, sx1 = (float)centerSlice.Left, sx2 = (float)centerSlice.Right, sx3 = w;
		float sy0 = 0, sy1 = (float)centerSlice.Top, sy2 = (float)centerSlice.Bottom, sy3 = h;
		float dx0 = (float)destination.Left, dx1 = dx0 + sx1, dx3 = (float)destination.Right, dx2 = dx3 - (sx3 - sx2);
		float dy0 = (float)destination.Top, dy1 = dy0 + sy1, dy3 = (float)destination.Bottom, dy2 = dy3 - (sy3 - sy2);
		float[] sxe = { sx0, sx1, sx2, sx3 }, sye = { sy0, sy1, sy2, sy3 };
		float[] dxe = { dx0, dx1, dx2, dx3 }, dye = { dy0, dy1, dy2, dy3 };

		for (var row = 0; row < 3; row++)
		{
			for (var col = 0; col < 3; col++)
			{
				if (centerHollow && row == 1 && col == 1) { continue; }
				float dl = dxe[col], dr = dxe[col + 1], dt = dye[row], db = dye[row + 1];
				if (dr - dl <= 0 || db - dt <= 0) { continue; }
				_target.Add(new ImageCmd
				{
					View = t.View, W = w, H = h, Opacity = 1f, Clip = _clip,
					P0 = Map(dl, dt), P1 = Map(dr, dt), P2 = Map(dr, db), P3 = Map(dl, db),
					U0 = sxe[col] / w, V0 = sye[row] / h, U1 = sxe[col + 1] / w, V1 = sye[row + 1] / h,
				});
			}
		}
	}
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity)
	{
		if (filter is WebGpuEffectFilter fx) { _target.Add(new BackdropCmd { Effect = fx, Opacity = opacity, Clip = _clip }); }
	}

	public IRenderData Finish() => _data;
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();

	// Whether a recording can be GPU-geometry-cached: only simple primitives (rect/path/image/gradient) with no
	// path clip (its coverage texture is per-frame pooled, so a cached bind group would dangle). Memoized.
	internal static bool IsCacheable(WebGpuRenderData d)
	{
		if (d.Cacheable is { } memo) { return memo; }
		bool ok = d.Commands.Count > 0;
		foreach (var c in d.Commands)
		{
			if (c is not (RectCommand or PathFill or ImageCmd or GradientCmd) || c.Clip.PathFan is not null) { ok = false; break; }
		}
		d.Cacheable = ok;
		return ok;
	}

	// Transforms a recording's (simple) commands to device space under a transform+clip, for building its GPU
	// cache. Uses the inline (always-transform) path so it never emits a nested ReplayRef.
	internal static List<WebGpuCommand> TransformFor(List<WebGpuCommand> commands, Matrix4x4 transform, ClipData clip)
	{
		var rec = new WebGpuCommandRecorder();
		rec._m = transform;
		rec._clip = clip;
		rec.ReplayInline(new WebGpuRenderData { Commands = commands });
		return rec._data.Commands;
	}

	// Retained sub-recordings (SKPicture equivalent) are recorded at identity; replaying one bakes in the target
	// session's current matrix + clip. A cacheable recording is deferred as a ReplayRef capturing its immutable
	// command list (the present caches its GPU geometry); otherwise its commands are transformed inline.
	public void Replay(IRenderData data)
	{
		if (data is WebGpuRenderData cacheable && IsCacheable(cacheable))
		{
			_target.Add(new ReplayRefCmd { Commands = cacheable.Commands, Transform = _m, Clip = _clip });
			return;
		}
		ReplayInline(data);
	}

	private void ReplayInline(IRenderData data)
	{
		if (data is not WebGpuRenderData d) { return; }
		Vector2 T(Vector2 p) => new(p.X * _m.M11 + p.Y * _m.M21 + _m.M41, p.X * _m.M12 + p.Y * _m.M22 + _m.M42);
		foreach (var cmd in d.Commands)
		{
			switch (cmd)
			{
				case RectCommand r:
					_target.Add(new RectCommand { Color = r.Color, Clip = ClipCompose(r.Clip, T), P0 = T(r.P0), P1 = T(r.P1), P2 = T(r.P2), P3 = T(r.P3) });
					break;
				case PathFill p:
					var src = p.FanDevice; var dst = new float[src.Length];
					var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
					for (int i = 0; i < src.Length; i += 2)
					{
						var q = T(new Vector2(src[i], src[i + 1])); dst[i] = q.X; dst[i + 1] = q.Y;
						bbMin = Vector2.Min(bbMin, q); bbMax = Vector2.Max(bbMax, q);
					}
					_target.Add(new PathFill { FanDevice = dst, BbMin = bbMin, BbMax = bbMax, Color = p.Color, EvenOdd = p.EvenOdd, Clip = ClipCompose(p.Clip, T) });
					break;
				case ShadowCmd sh:
					var ssrc = sh.FanDevice; var sdst = new float[ssrc.Length];
					var sbbMin = new Vector2(float.MaxValue); var sbbMax = new Vector2(float.MinValue);
					for (int i = 0; i < ssrc.Length; i += 2)
					{
						var q = T(new Vector2(ssrc[i], ssrc[i + 1])); sdst[i] = q.X; sdst[i + 1] = q.Y;
						sbbMin = Vector2.Min(sbbMin, q); sbbMax = Vector2.Max(sbbMax, q);
					}
					var ss = new Vector2(_m.M11, _m.M12).Length();
					_target.Add(new ShadowCmd { FanDevice = sdst, BbMin = sbbMin, BbMax = sbbMax, EvenOdd = sh.EvenOdd, Color = sh.Color, SigmaX = sh.SigmaX * ss, SigmaY = sh.SigmaY * ss, Additive = sh.Additive, Clip = ClipCompose(sh.Clip, T) });
					break;
				case ImageCmd im:
					_target.Add(new ImageCmd { P0 = T(im.P0), P1 = T(im.P1), P2 = T(im.P2), P3 = T(im.P3), View = im.View, W = im.W, H = im.H, Opacity = im.Opacity, U0 = im.U0, V0 = im.V0, U1 = im.U1, V1 = im.V1, TintMode = im.TintMode, Tint = im.Tint, ColorMatrix = im.ColorMatrix, Clip = ClipCompose(im.Clip, T) });
					break;
				case GradientCmd gc:
					// Transform the device-space geometry baked into the uniform by the replay matrix too, so the
					// gradient stays aligned with its (transformed) quad.
					var uu = (float[])gc.Uniform.Clone();
					var ga = T(new Vector2(uu[4], uu[5])); uu[4] = ga.X; uu[5] = ga.Y;
					if (uu[0] < 0.5f)
					{
						var gb = T(new Vector2(uu[6], uu[7])); uu[6] = gb.X; uu[7] = gb.Y;
					}
					else
					{
						// Elliptical: scale each radius by its own device-axis length (rx by X, ry by Y).
						var rsx = new Vector2(_m.M11, _m.M12).Length();
						var rsy = new Vector2(_m.M21, _m.M22).Length();
						uu[6] *= rsx; uu[7] *= rsy;
						var go = T(new Vector2(uu[88], uu[89])); uu[88] = go.X; uu[89] = go.Y;   // focal origin
					}
					_target.Add(new GradientCmd { P0 = T(gc.P0), P1 = T(gc.P1), P2 = T(gc.P2), P3 = T(gc.P3), Uniform = uu, Clip = ClipCompose(gc.Clip, T) });
					break;
				case LayerCmd lyr:
					var saved = _target;
					var layerList = new List<WebGpuCommand>();
					_target = layerList;
					Replay(new WebGpuRenderData { Commands = lyr.Commands });   // recursively transform sub-commands
					_target = saved;
					_target.Add(new LayerCmd { Commands = layerList, CompositeMode = lyr.CompositeMode, ColorMatrix = lyr.ColorMatrix, ShadowEffect = lyr.ShadowEffect, Clip = ClipCompose(lyr.Clip, T) });
					break;
				case BackdropCmd bk:
					_target.Add(new BackdropCmd { Effect = bk.Effect, Opacity = bk.Opacity, Clip = ClipCompose(bk.Clip, T) });
					break;
				case ReplayRefCmd rr:
					// Compose this replay's transform/clip onto the ref so the present still caches it.
					_target.Add(new ReplayRefCmd { Commands = rr.Commands, Transform = rr.Transform * _m, Clip = ClipCompose(rr.Clip, T) });
					break;
			}
		}
	}

	// AABB of a child rect (its 4 corners) under the replay transform t.
	private static Vector4 TransformedAabb(Vector4 rect, Func<Vector2, Vector2> t)
	{
		var a = t(new Vector2(rect.X, rect.Y)); var b = t(new Vector2(rect.Z, rect.Y)); var e = t(new Vector2(rect.Z, rect.W)); var f = t(new Vector2(rect.X, rect.W));
		var l = MathF.Min(MathF.Min(a.X, b.X), MathF.Min(e.X, f.X)); var top = MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(e.Y, f.Y));
		var r = MathF.Max(MathF.Max(a.X, b.X), MathF.Max(e.X, f.X)); var bo = MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(e.Y, f.Y));
		return new Vector4(l, top, r, bo);
	}

	// Intersect a child (sub-recording) clip into the current session clip, transforming it by the replay matrix.
	private ClipData ClipCompose(ClipData c, Func<Vector2, Vector2> t)
	{
		var result = _clip;
		if (!(c.Aabb.X <= -1e8f && c.Aabb.Y <= -1e8f && c.Aabb.Z >= 1e8f && c.Aabb.W >= 1e8f))
		{
			var a = TransformedAabb(c.Aabb, t);
			result.Aabb = new Vector4(MathF.Max(result.Aabb.X, a.X), MathF.Max(result.Aabb.Y, a.Y), MathF.Min(result.Aabb.Z, a.Z), MathF.Min(result.Aabb.W, a.W));
		}
		// Child rounded shape (innermost) wins; transform its rect and scale radii by the replay matrix.
		if (c.HasRound)
		{
			var s = new Vector2(_m.M11, _m.M12).Length();
			result.HasRound = true;
			result.Rect = TransformedAabb(c.Rect, t);
			result.Radii = c.Radii * s;
			result.HasExclude = c.HasExclude;
		}
		if (c.PathFan != null)
		{
			var pf = new float[c.PathFan.Length];
			for (int i = 0; i < c.PathFan.Length; i += 2) { var p = t(new Vector2(c.PathFan[i], c.PathFan[i + 1])); pf[i] = p.X; pf[i + 1] = p.Y; }
			result.PathFan = pf;
			result.PathEvenOdd = c.PathEvenOdd;
		}
		return result;
	}
}

public sealed unsafe class WebGpuPresentSession : IPresentSession
{
	private readonly WebGpuDevice _d;
	private readonly WebGpuRenderSurface _s;
	private WColor? _presentClear;
	public WebGpuPresentSession(WebGpuDevice d, WebGpuRenderSurface s) { _d = d; _s = s; }

	private bool SetScissor(IntPtr pass, Vector4 clip)
	{
		int x = (int)MathF.Max(0, MathF.Floor(clip.X)); int y = (int)MathF.Max(0, MathF.Floor(clip.Y));
		int r = (int)MathF.Min(_s.Width, MathF.Ceiling(clip.Z)); int b = (int)MathF.Min(_s.Height, MathF.Ceiling(clip.W));
		int w = r - x, h = b - y; if (w <= 0 || h <= 0) { return false; }
		wgpuRenderPassEncoderSetScissorRect(pass, (uint)x, (uint)y, (uint)w, (uint)h); return true;
	}
	private Vector2 Ndc(Vector2 dev) => new(2f * dev.X / _s.Width - 1f, 1f - 2f * dev.Y / _s.Height);

	private IntPtr MakeBuffer(float[] data)
	{
		var size = data.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	private IntPtr MakeUniform(int byteSize)
		=> _d.BufferPool.Rent(byteSize, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);

	// Resource allocation that is pooled (owned == null) for per-frame commands, or persistent (added to `owned`
	// for later release) for a cached recording's geometry that must survive across frames.
	private IntPtr Vbuf(float[] data, OwnedResources owned)
	{
		if (owned is null) { return MakeBuffer(data); }
		int size = data.Length * sizeof(float);
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	private IntPtr Ubuf(int size, OwnedResources owned)
	{
		if (owned is null) { return MakeUniform(size); }
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	private IntPtr Bg(ref WGPUBindGroupDescriptor bgd, OwnedResources owned)
	{
		var bg = wgpuDeviceCreateBindGroup(_d.Dev, (WGPUBindGroupDescriptor*)Unsafe.AsPointer(ref bgd));
		if (owned is null) { _d.TrackBg(bg); } else { owned.BindGroups.Add((nint)bg); }
		return bg;
	}

	// Coverage textures for path clips, cached by the (reference-identical) fan shared across a clip's commands.
	private readonly System.Collections.Generic.Dictionary<float[], nint> _coverageCache = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);

	// The clip bind group for a command: the ClipU uniform (rect, radii, flags, surface size), plus a coverage
	// texture (the path-clip mask, or the device dummy when there's no path clip) and a sampler.
	private IntPtr MakeClipBg(IntPtr bgl, ClipData cd, OwnedResources owned = null)
	{
		var cu = new float[16];
		cu[0] = cd.Rect.X; cu[1] = cd.Rect.Y; cu[2] = cd.Rect.Z; cu[3] = cd.Rect.W;
		cu[4] = cd.Radii.X; cu[5] = cd.Radii.Y; cu[6] = cd.Radii.Z; cu[7] = cd.Radii.W;
		cu[8] = cd.HasRound ? 1f : 0f; cu[9] = cd.PathFan != null ? 1f : 0f; cu[10] = cd.HasExclude ? 1f : 0f;
		cu[12] = _s.Width; cu[13] = _s.Height;
		var buf = Ubuf(64, owned);
		fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, 64); }
		// Cached recordings never have a path clip (excluded by IsCacheable), so covView is always the persistent
		// DummyTex there — no dangling reference to a per-frame pooled coverage texture.
		var covView = cd.PathFan != null ? PathCoverage(cd.PathFan, cd.PathEvenOdd) : _d.DummyTex;
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = buf, Offset = 0, Size = 64 };
		e[1] = new WGPUBindGroupEntry { Binding = 1, TextureView = covView };
		e[2] = new WGPUBindGroupEntry { Binding = 2, Sampler = _d.Smp };
		var bgd = new WGPUBindGroupDescriptor { Layout = bgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, owned);
	}

	private IntPtr PathCoverage(float[] fan, bool evenOdd)
	{
		if (_coverageCache.TryGetValue(fan, out var cached)) { return (IntPtr)cached; }
		var view = RenderPathCoverage(fan, evenOdd);
		_coverageCache[fan] = (nint)view;
		return view;
	}

	// Fills the clip path (stencil-then-cover, white) into a full-size coverage surface; its resolved alpha is
	// the per-fragment clip mask sampled by clipCov.
	private IntPtr RenderPathCoverage(float[] fan, bool evenOdd)
	{
		var cov = new WebGpuRenderSurface(_d, _s.Width, _s.Height, _d.Pool);
		var fanNdc = new float[fan.Length];
		for (int i = 0; i < fan.Length; i += 2) { var n = Ndc(new Vector2(fan[i], fan[i + 1])); fanNdc[i] = n.X; fanNdc[i + 1] = n.Y; }
		var fanBuf = MakeBuffer(fanNdc);
		var cq = new System.Collections.Generic.List<float>();
		void CQ(float x, float y) { cq.Add(x); cq.Add(y); cq.Add(1f); cq.Add(1f); cq.Add(1f); cq.Add(1f); }
		CQ(-1, -1); CQ(1, -1); CQ(1, 1); CQ(-1, -1); CQ(1, 1); CQ(-1, 1);
		var coverBuf = MakeBuffer(cq.ToArray());
		var noClip = MakeClipBg(_d.CoverClipBgl, default);

		var enc = wgpuDeviceCreateCommandEncoder(_d.Dev, null);
		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = cov.MsaaColorView, ResolveTarget = cov.View, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Discard, ClearValue = default };
		var dsa = new WGPURenderPassDepthStencilAttachment { View = cov.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 1f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);
		wgpuRenderPassEncoderSetPipeline(pass, evenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanNdc.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)(fanNdc.Length / 2), 1, 0, 0);
		wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, noClip, 0, (uint*)null);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, coverBuf, 0, (nuint)(cq.Count * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
		return cov.View;
	}

	// Fills the shadow silhouette into an offscreen coverage surface (stencil-then-cover, white), then blurs it
	// separably (H then V). Returns the blurred coverage texture + its device-space placement. NOTE: the per-
	// shadow textures are not pooled/freed yet — fine for offscreen/one-shot; the on-window path needs cleanup.
	private IntPtr RenderShadow(ShadowCmd sh, out Vector2 origin, out Vector2 size)
	{
		float pad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		origin = new Vector2(sh.BbMin.X - pad, sh.BbMin.Y - pad);
		int sw = Math.Clamp((int)MathF.Ceiling(sh.BbMax.X - sh.BbMin.X + 2 * pad), 1, 4096);
		int sh2 = Math.Clamp((int)MathF.Ceiling(sh.BbMax.Y - sh.BbMin.Y + 2 * pad), 1, 4096);
		size = new Vector2(sw, sh2);

		// 1) coverage: fill the fan (stencil-then-cover, white) into an MSAA surface resolved to single-sample.
		var cov = new WebGpuRenderSurface(_d, sw, sh2, _d.Pool);
		var fanNdc = new float[sh.FanDevice.Length];
		for (int i = 0; i < sh.FanDevice.Length; i += 2)
		{
			fanNdc[i] = (sh.FanDevice[i] - origin.X) / sw * 2f - 1f;
			fanNdc[i + 1] = 1f - (sh.FanDevice[i + 1] - origin.Y) / sh2 * 2f;
		}
		var fanBuf = MakeBuffer(fanNdc);
		var cq = new List<float>();
		void CQ(float x, float y) { cq.Add(x); cq.Add(y); cq.Add(1f); cq.Add(1f); cq.Add(1f); cq.Add(1f); }
		CQ(-1, -1); CQ(1, -1); CQ(1, 1); CQ(-1, -1); CQ(1, 1); CQ(-1, 1);
		var coverBuf = MakeBuffer(cq.ToArray());
		var noClip = MakeClipBg(_d.CoverClipBgl, default);

		var enc = wgpuDeviceCreateCommandEncoder(_d.Dev, null);
		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = cov.MsaaColorView, ResolveTarget = cov.View, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Discard, ClearValue = default };
		var dsa = new WGPURenderPassDepthStencilAttachment { View = cov.DepthView, DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Discard, DepthClearValue = 1f, StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Discard, StencilClearValue = 0 };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);
		wgpuRenderPassEncoderSetPipeline(pass, sh.EvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanNdc.Length * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)(fanNdc.Length / 2), 1, 0, 0);
		wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, noClip, 0, (uint*)null);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, coverBuf, 0, (nuint)(cq.Count * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));

		// 2) separable blur: coverage -> temp (H) -> blur (V).
		var tempView = _d.Pool.Rent(sw, sh2, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(cov.View, tempView, new Vector2(1f, 0f), new Vector2(1f / sw, 0f), sh.SigmaX);
		var blurView = _d.Pool.Rent(sw, sh2, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
		BlurPass(tempView, blurView, new Vector2(0f, 1f), new Vector2(0f, 1f / sh2), sh.SigmaY);
		return blurView;
	}

	private void BlurPass(IntPtr src, IntPtr dst, Vector2 dir, Vector2 texel, float sigma)
	{
		var bu = new float[8]; bu[0] = dir.X; bu[1] = dir.Y; bu[2] = texel.X; bu[3] = texel.Y; bu[4] = sigma;
		var ubuf = MakeUniform((int)32);
		fixed (float* p = bu) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, 32); }
		var entries = stackalloc WGPUBindGroupEntry[3];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = src };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 32 };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.BlurBgl, EntryCount = 3, Entries = entries };
		var bg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));

		var enc = wgpuDeviceCreateCommandEncoder(_d.Dev, null);
		var ca = new WGPURenderPassColorAttachment { DepthSlice = uint.MaxValue, View = dst, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Store, ClearValue = default };
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);
		wgpuRenderPassEncoderSetPipeline(pass, _d.BlurPipe);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, bg, 0, (uint*)null);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		wgpuRenderPassEncoderEnd(pass);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
	}

	public void Replay(IRenderData data)
	{
		// During an async backend switch (e.g. the browser's on-canvas WebGPU init) a frame recorded by the
		// previous renderer can reach us; skip it rather than mis-cast — the next frame is recorded by this backend.
		if (data is not WebGpuRenderData rd) { return; }
		lock (_d.RenderGate)
		{
			_d.BeginFrameResources();   // reclaim last frame's pooled textures/buffers + release its bind groups
			RenderInto(rd.Commands, _s, _presentClear ?? rd.ClearColor);
		}
	}

	// Renders WITHOUT the per-frame reset — for a nested offscreen render (RenderOffscreen) that may run inside an
	// enclosing frame; resetting the shared pools mid-frame would free the enclosing frame's in-flight resources.
	// The gate is reentrant, so a nested call inside an enclosing Replay is safe; an independent call is serialized.
	public void ReplayNested(IRenderData data)
	{
		if (data is not WebGpuRenderData rd) { return; }
		lock (_d.RenderGate)
		{
			RenderInto(rd.Commands, _s, _presentClear ?? rd.ClearColor);
		}
	}

	private static bool ClipDataEquals(in ClipData a, in ClipData b)
		=> a.Aabb == b.Aabb && a.HasRound == b.HasRound && a.Rect == b.Rect && a.Radii == b.Radii && ReferenceEquals(a.PathFan, b.PathFan);

	// Pre-encodes a cached recording's draw-ops into a render bundle (replayed with one ExecuteBundles instead of
	// re-issuing every SetPipeline/SetBindGroup/SetVertexBuffer/Draw per frame). Bundles can't set scissor or the
	// stencil reference — clipping is per-fragment (clipCov) and the cover pass uses the default stencil ref (0),
	// so both are safe to omit. The descriptor matches the MSAA main/layer pass formats.
	private IntPtr BuildBundle(List<(int kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)> ops)
	{
		var colorFmt = _d.ColorFormat;
		var desc = new WGPURenderBundleEncoderDescriptor
		{
			ColorFormatCount = 1,
			ColorFormats = &colorFmt,
			DepthStencilFormat = WebGpuDevice.DepthStencilFormat,
			SampleCount = WebGpuDevice.MsaaSamples,
		};
		var enc = wgpuDeviceCreateRenderBundleEncoder(_d.Dev, &desc);
		foreach (var (kind, b0, u0, b1, flag, clip, clipBg) in ops)
		{
			switch (kind)
			{
				case 0:
					wgpuRenderBundleEncoderSetPipeline(enc, _d.SolidPipe);
					wgpuRenderBundleEncoderSetBindGroup(enc, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderBundleEncoderSetVertexBuffer(enc, 0, (IntPtr)b0, 0, (nuint)(u0 * 6 * sizeof(float)));
					wgpuRenderBundleEncoderDraw(enc, u0, 1, 0, 0);
					break;
				case 1:
					wgpuRenderBundleEncoderSetPipeline(enc, flag ? _d.StencilEvenOdd : _d.StencilNonZero);
					wgpuRenderBundleEncoderSetVertexBuffer(enc, 0, (IntPtr)b0, 0, (nuint)(u0 * 2 * sizeof(float)));
					wgpuRenderBundleEncoderDraw(enc, u0, 1, 0, 0);
					wgpuRenderBundleEncoderSetPipeline(enc, _d.CoverPipe);
					wgpuRenderBundleEncoderSetBindGroup(enc, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderBundleEncoderSetVertexBuffer(enc, 0, (IntPtr)b1, 0, (nuint)(36 * sizeof(float)));
					wgpuRenderBundleEncoderDraw(enc, 6, 1, 0, 0);
					break;
				case 2:
					wgpuRenderBundleEncoderSetPipeline(enc, _d.ImagePipe);
					wgpuRenderBundleEncoderSetBindGroup(enc, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderBundleEncoderSetBindGroup(enc, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderBundleEncoderSetVertexBuffer(enc, 0, (IntPtr)b1, 0, (nuint)(24 * sizeof(float)));
					wgpuRenderBundleEncoderDraw(enc, 6, 1, 0, 0);
					break;
				case 3:
					wgpuRenderBundleEncoderSetPipeline(enc, _d.GradientPipe);
					wgpuRenderBundleEncoderSetBindGroup(enc, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderBundleEncoderSetBindGroup(enc, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderBundleEncoderSetVertexBuffer(enc, 0, (IntPtr)b1, 0, (nuint)(12 * sizeof(float)));
					wgpuRenderBundleEncoderDraw(enc, 6, 1, 0, 0);
					break;
			}
		}
		var bdesc = new WGPURenderBundleDescriptor();
		return wgpuRenderBundleEncoderFinish(enc, &bdesc);
	}

	// Builds the draw-op(s) for a simple primitive (rect/path/image/gradient) into `ops`, allocating GPU resources
	// pooled (owned == null, per-frame) or persistent (owned != null, a cached recording's geometry).
	private void BuildSimpleOp(WebGpuCommand cmd, List<(int kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)> ops, OwnedResources owned)
	{
		switch (cmd)
		{
			case RectCommand rc:
			{
				var c = new Vector4(rc.Color.R / 255f, rc.Color.G / 255f, rc.Color.B / 255f, rc.Color.A / 255f);
				var v = new List<float>();
				void V(Vector2 p) { var n = Ndc(p); v.Add(n.X); v.Add(n.Y); v.Add(c.X); v.Add(c.Y); v.Add(c.Z); v.Add(c.W); }
				V(rc.P0); V(rc.P1); V(rc.P2); V(rc.P0); V(rc.P2); V(rc.P3);
				ops.Add((0, (nint)Vbuf(v.ToArray(), owned), 6, 0, false, rc.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc.Clip, owned)));
				break;
			}
			case PathFill pf:
			{
				var fanNdc = new float[pf.FanDevice.Length];
				for (int i = 0; i < pf.FanDevice.Length; i += 2) { var n = Ndc(new Vector2(pf.FanDevice[i], pf.FanDevice[i + 1])); fanNdc[i] = n.X; fanNdc[i + 1] = n.Y; }
				var fanBuf = Vbuf(fanNdc, owned);
				var c = new Vector4(pf.Color.R / 255f, pf.Color.G / 255f, pf.Color.B / 255f, pf.Color.A / 255f);
				var cov = new List<float>();
				void CV(Vector2 p) { var n = Ndc(p); cov.Add(n.X); cov.Add(n.Y); cov.Add(c.X); cov.Add(c.Y); cov.Add(c.Z); cov.Add(c.W); }
				var tl = pf.BbMin; var br = pf.BbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
				CV(tl); CV(tr); CV(br); CV(tl); CV(br); CV(bl);
				ops.Add((1, (nint)fanBuf, (uint)(pf.FanDevice.Length / 2), (nint)Vbuf(cov.ToArray(), owned), pf.EvenOdd, pf.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf.Clip, owned)));
				break;
			}
			case ImageCmd im:
			{
				var view = im.View;
				var ubuf = Ubuf(112, owned);
				var op = stackalloc float[28];
				bool hasMatrix = im.ColorMatrix is { Length: >= 20 };
				op[0] = im.Opacity; op[1] = im.TintMode; op[2] = hasMatrix ? 1f : 0f; op[3] = 0;
				op[4] = im.Tint.X; op[5] = im.Tint.Y; op[6] = im.Tint.Z; op[7] = im.Tint.W;
				if (im.ColorMatrix is { Length: >= 20 } mm)
				{
					op[8] = mm[0]; op[9] = mm[1]; op[10] = mm[2]; op[11] = mm[3];        // m0
					op[12] = mm[5]; op[13] = mm[6]; op[14] = mm[7]; op[15] = mm[8];      // m1
					op[16] = mm[10]; op[17] = mm[11]; op[18] = mm[12]; op[19] = mm[13];  // m2
					op[20] = mm[15]; op[21] = mm[16]; op[22] = mm[17]; op[23] = mm[18];  // m3
					op[24] = mm[4]; op[25] = mm[9]; op[26] = mm[14]; op[27] = mm[19];    // off (5th column)
				}
				wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, 112);
				var entries = stackalloc WGPUBindGroupEntry[3];
				entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
				entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
				entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 112 };
				var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
				var bg = Bg(ref bgd, owned);
				var q = new float[24];
				void QV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); q[idx] = n.X; q[idx + 1] = n.Y; q[idx + 2] = u; q[idx + 3] = vv; }
				QV(0, im.P0, im.U0, im.V0); QV(4, im.P1, im.U1, im.V0); QV(8, im.P2, im.U1, im.V1); QV(12, im.P0, im.U0, im.V0); QV(16, im.P2, im.U1, im.V1); QV(20, im.P3, im.U0, im.V1);
				ops.Add((2, (nint)bg, 0, (nint)Vbuf(q, owned), false, im.Clip, (nint)MakeClipBg(_d.ImageClipBgl, im.Clip, owned)));
				break;
			}
			case GradientCmd gc:
			{
				var bytes = (nuint)WebGpuDevice.GradientUniformBytes;
				var ubuf = Ubuf((int)bytes, owned);
				fixed (float* p = gc.Uniform) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, bytes); }
				var gentry = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
				var gbgd = new WGPUBindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 1, Entries = &gentry };
				var gbg = Bg(ref gbgd, owned);
				var gq = new float[12];
				void GV(int idx, Vector2 pos) { var n = Ndc(pos); gq[idx] = n.X; gq[idx + 1] = n.Y; }
				GV(0, gc.P0); GV(2, gc.P1); GV(4, gc.P2); GV(6, gc.P0); GV(8, gc.P2); GV(10, gc.P3);
				ops.Add((3, (nint)gbg, 0, (nint)Vbuf(gq, owned), false, gc.Clip, (nint)MakeClipBg(_d.GradClipBgl, gc.Clip, owned)));
				break;
			}
		}
	}

	// Renders a command list into a target surface's MSAA pass (resolving to its single-sample view). Layers
	// recurse into their own full-size surface then composite here; shadows/layers pre-render before the pass.
	private void RenderInto(List<WebGpuCommand> cmds, WebGpuRenderSurface target, WColor? clear)
	{

		// Build GPU resources for every command up front (buffers/textures must be created outside the
		// render pass), preserving draw order in a single op list so cross-type z-order is honoured.
		// kind: 0=rect (b0=verts), 1=path (b0=fan, u0=fanCount, b1=cover, flag=evenOdd), 2=image (b0=bindGroup,
		// b1=quad), 3=gradient, 4=composite layer (b0=bindGroup, u0=compositeMode).
		var ops = new List<(int kind, nint b0, uint u0, nint b1, bool flag, ClipData clip, nint clipBg)>();
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			var cmd = cmds[ci];
			switch (cmd)
			{
				case RectCommand rc0:
				{
					// Coalesce a run of consecutive rects sharing the same clip into one vertex buffer + one draw.
					var verts = new List<float>();
					int j = ci;
					while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
					{
						var c = new Vector4(rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
						void V(Vector2 p) { var n = Ndc(p); verts.Add(n.X); verts.Add(n.Y); verts.Add(c.X); verts.Add(c.Y); verts.Add(c.Z); verts.Add(c.W); }
						V(rcj.P0); V(rcj.P1); V(rcj.P2); V(rcj.P0); V(rcj.P2); V(rcj.P3);
						j++;
					}
					ops.Add((0, (nint)MakeBuffer(verts.ToArray()), (uint)((j - ci) * 6), 0, false, rc0.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip)));
					ci = j - 1;   // the for-loop's ci++ advances past the run
					break;
				}
				case PathFill:
				case ImageCmd:
				case GradientCmd:
					BuildSimpleOp(cmd, ops, null);   // pooled (per-frame)
					break;
				case ReplayRefCmd rr:
				{
					// The per-visual GPU-geometry cache (slab/scroll), keyed by the recording's immutable command
					// list. Build once; reuse while it's replayed at the same transform/clip. A stale entry (moved
					// visual) is deferred-released and rebuilt. Entries not referenced any frame are evicted.
					if (!_d.GeometryCache.TryGetValue(rr.Commands, out var entry) || entry.Transform != rr.Transform || !ClipDataEquals(entry.Clip, rr.Clip))
					{
						if (entry is not null) { _d.DeferRelease(entry.Owned); }
						var owned = new OwnedResources();
						var cachedOps = new List<(int, nint, uint, nint, bool, ClipData, nint)>();
						foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, rr.Transform, rr.Clip)) { BuildSimpleOp(tc, cachedOps, owned); }
						var bundle = BuildBundle(cachedOps);
						owned.Bundles.Add((nint)bundle);
						entry = new WebGpuGeometryCache { Ops = cachedOps, Owned = owned, Bundle = bundle, Transform = rr.Transform, Clip = rr.Clip };
						_d.GeometryCache[rr.Commands] = entry;
					}
					entry.Used = true;
					// One ExecuteBundles replays all the cached draws (kind 5). Full-surface clip so the scissor is
					// reset to full before the bundle (its draws clip per-fragment via clipCov).
					ops.Add((5, (nint)entry.Bundle, 0, 0, false, ClipData.None, 0));
					break;
				}
				case ShadowCmd sh:
				{
					// Render the blurred coverage offscreen, then composite it as a SrcIn-tinted image (tint =
					// shadow color) at its device placement — reusing the image draw path (kind 2), incl. clip.
					var blurView = RenderShadow(sh, out var origin, out var size);
					var ubuf = MakeUniform((int)112);
					var op = stackalloc float[8];
					op[0] = 1f; op[1] = 1f; op[2] = 0; op[3] = 0;
					op[4] = sh.Color.R / 255f; op[5] = sh.Color.G / 255f; op[6] = sh.Color.B / 255f; op[7] = sh.Color.A / 255f;
					wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, 32);
					var sentries = stackalloc WGPUBindGroupEntry[3];
					sentries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blurView };
					sentries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
					sentries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = 112 };
					var sbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = sentries };
					var sbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &sbgd));
					var sq = new float[24];
					void SQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); sq[idx] = n.X; sq[idx + 1] = n.Y; sq[idx + 2] = u; sq[idx + 3] = vv; }
					var o0 = origin; var o1 = origin + new Vector2(size.X, 0); var o2 = origin + size; var o3 = origin + new Vector2(0, size.Y);
					SQV(0, o0, 0, 0); SQV(4, o1, 1, 0); SQV(8, o2, 1, 1); SQV(12, o0, 0, 0); SQV(16, o2, 1, 1); SQV(20, o3, 0, 1);
					ops.Add((2, (nint)sbg, 0, (nint)MakeBuffer(sq), false, sh.Clip, (nint)MakeClipBg(_d.ImageClipBgl, sh.Clip)));
					break;
				}
				case LayerCmd lyr:
				{
					// Render the layer's commands into a full-size offscreen surface, then composite (kind 4). The
					// offscreen render is a separate queue submission; wait for it to complete so its MSAA resolve is
					// visible to the composite's sample below (without this, the composite samples an empty scratch —
					// the resolve→sample hazard isn't covered across submissions).
					var layerSurface = new WebGpuRenderSurface(_d, _s.Width, _s.Height, _d.Pool);
					RenderInto(lyr.Commands, layerSurface, null);
					_d.FlushForSample(layerSurface);

					// SaveLayer(IEffectFilter) drop shadow: blur the content, draw it tinted+offset behind, then
					// the content on top. Reuses the image path (SrcIn tint) for the shadow — same as DrawShadow.
					if (lyr.ShadowEffect is { } fx)
					{
						var tmp = _d.Pool.Rent(_s.Width, _s.Height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
						BlurPass(layerSurface.View, tmp, new Vector2(1f, 0f), new Vector2(1f / _s.Width, 0f), fx.SigmaX);
						var blur = _d.Pool.Rent(_s.Width, _s.Height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
						BlurPass(tmp, blur, new Vector2(0f, 1f), new Vector2(0f, 1f / _s.Height), fx.SigmaY);
						var subuf = MakeUniform((int)112);
						var sop = stackalloc float[8];
						sop[0] = 1f; sop[1] = 1f; sop[2] = 0; sop[3] = 0;
						sop[4] = fx.Color.R / 255f; sop[5] = fx.Color.G / 255f; sop[6] = fx.Color.B / 255f; sop[7] = fx.Color.A / 255f;
						wgpuQueueWriteBuffer(_d.Q, subuf, 0, (IntPtr)sop, 32);
						var sfe = stackalloc WGPUBindGroupEntry[3];
						sfe[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = blur };
						sfe[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
						sfe[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = subuf, Offset = 0, Size = 112 };
						var sfbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = sfe };
						var sfbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &sfbgd));
						var fq = new float[24];
						void FQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); fq[idx] = n.X; fq[idx + 1] = n.Y; fq[idx + 2] = u; fq[idx + 3] = vv; }
						var off = new Vector2(fx.Dx, fx.Dy);
						FQV(0, new Vector2(0, 0) + off, 0, 0); FQV(4, new Vector2(_s.Width, 0) + off, 1, 0); FQV(8, new Vector2(_s.Width, _s.Height) + off, 1, 1);
						FQV(12, new Vector2(0, 0) + off, 0, 0); FQV(16, new Vector2(_s.Width, _s.Height) + off, 1, 1); FQV(20, new Vector2(0, _s.Height) + off, 0, 1);
						ops.Add((2, (nint)sfbg, 0, (nint)MakeBuffer(fq), false, lyr.Clip, (nint)MakeClipBg(_d.ImageClipBgl, lyr.Clip)));
					}

					var cu = new float[24];
					cu[0] = lyr.ColorMatrix is { Length: >= 20 } ? 1f : 0f; cu[1] = 1f;
					if (lyr.ColorMatrix is { Length: >= 20 } mm)
					{
						cu[4] = mm[0]; cu[5] = mm[1]; cu[6] = mm[2]; cu[7] = mm[3];        // m0
						cu[8] = mm[5]; cu[9] = mm[6]; cu[10] = mm[7]; cu[11] = mm[8];      // m1
						cu[12] = mm[10]; cu[13] = mm[11]; cu[14] = mm[12]; cu[15] = mm[13]; // m2
						cu[16] = mm[15]; cu[17] = mm[16]; cu[18] = mm[17]; cu[19] = mm[18]; // m3
						cu[20] = mm[4]; cu[21] = mm[9]; cu[22] = mm[14]; cu[23] = mm[19];   // off (5th column)
					}
					var lubuf = MakeUniform((int)96);
					fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, lubuf, 0, (IntPtr)p, 96); }
					var lentries = stackalloc WGPUBindGroupEntry[3];
					lentries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = layerSurface.View };
					lentries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
					lentries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = lubuf, Offset = 0, Size = 96 };
					var lbgd = new WGPUBindGroupDescriptor { Layout = lyr.CompositeMode == 1 ? _d.CompositeDstInBgl : _d.CompositeBgl, EntryCount = 3, Entries = lentries };
					var lbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &lbgd));
					ops.Add((4, (nint)lbg, (uint)lyr.CompositeMode, 0, false, lyr.Clip, 0));
					break;
				}
				case BackdropCmd bk:
				{
					// Capture the content drawn so far (this frame's backdrop), blur it, and draw it clipped to the
					// effect region; then a tint overlay. Simplified acrylic = blurred backdrop + tint.
					var bd = new WebGpuRenderSurface(_d, _s.Width, _s.Height, _d.Pool);
					RenderInto(cmds.GetRange(0, ci), bd, clear);
					var btmp = _d.Pool.Rent(_s.Width, _s.Height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
					BlurPass(bd.View, btmp, new Vector2(1f, 0f), new Vector2(1f / _s.Width, 0f), bk.Effect.SigmaX);
					var bblur = _d.Pool.Rent(_s.Width, _s.Height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding, WebGpuDevice.DefaultColorFormat);
					BlurPass(btmp, bblur, new Vector2(0f, 1f), new Vector2(0f, 1f / _s.Height), bk.Effect.SigmaY);
					var bubuf = MakeUniform((int)112);
					var bop = stackalloc float[8]; bop[0] = bk.Opacity; bop[1] = 0; bop[2] = 0; bop[3] = 0;
					wgpuQueueWriteBuffer(_d.Q, bubuf, 0, (IntPtr)bop, 32);
					var bde = stackalloc WGPUBindGroupEntry[3];
					bde[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = bblur };
					bde[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
					bde[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = bubuf, Offset = 0, Size = 112 };
					var bdbgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = bde };
					var bdbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bdbgd));
					var bq = new float[24];
					void BQV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); bq[idx] = n.X; bq[idx + 1] = n.Y; bq[idx + 2] = u; bq[idx + 3] = vv; }
					BQV(0, new Vector2(0, 0), 0, 0); BQV(4, new Vector2(_s.Width, 0), 1, 0); BQV(8, new Vector2(_s.Width, _s.Height), 1, 1);
					BQV(12, new Vector2(0, 0), 0, 0); BQV(16, new Vector2(_s.Width, _s.Height), 1, 1); BQV(20, new Vector2(0, _s.Height), 0, 1);
					ops.Add((2, (nint)bdbg, 0, (nint)MakeBuffer(bq), false, bk.Clip, (nint)MakeClipBg(_d.ImageClipBgl, bk.Clip)));
					// Acrylic recipe over the blurred backdrop: SrcOver the luminosity colour (== mix(blurred, lum.rgb,
					// lum.a)), then SrcOver the tint. Both fill the effect region (clip AABB). A=0 colours are skipped.
					void Overlay(WColor col)
					{
						if (col.A == 0) { return; }
						var tc = new Vector4(col.R / 255f, col.G / 255f, col.B / 255f, col.A / 255f);
						var tv = new List<float>();
						void TV(float x, float y) { var n = Ndc(new Vector2(x, y)); tv.Add(n.X); tv.Add(n.Y); tv.Add(tc.X); tv.Add(tc.Y); tv.Add(tc.Z); tv.Add(tc.W); }
						var a = bk.Clip.Aabb;
						TV(a.X, a.Y); TV(a.Z, a.Y); TV(a.Z, a.W); TV(a.X, a.Y); TV(a.Z, a.W); TV(a.X, a.W);
						ops.Add((0, (nint)MakeBuffer(tv.ToArray()), 6, 0, false, bk.Clip, (nint)MakeClipBg(_d.SolidClipBgl, bk.Clip)));
					}
					Overlay(bk.Effect.LumColor);
					Overlay(bk.Effect.Color);
					break;
				}
			}
		}

		var enc = wgpuDeviceCreateCommandEncoder(_d.Dev, null);
		var ca = new WGPURenderPassColorAttachment
		{
			// Render into the multisampled color and resolve into the single-sample target texture.
			// A fresh MSAA buffer can't LoadOp.Load, so we always clear (transparent when no clear was given);
			// the neutral loop redraws the whole frame each present, so nothing prior needs preserving here.
			DepthSlice = uint.MaxValue,
			View = target.MsaaColorView, ResolveTarget = target.View, LoadOp = WGPULoadOp.Clear, StoreOp = WGPUStoreOp.Discard,
			ClearValue = clear.HasValue ? new WGPUColor { R = clear.Value.R / 255.0, G = clear.Value.G / 255.0, B = clear.Value.B / 255.0, A = clear.Value.A / 255.0 } : default,
		};
		var dsa = new WGPURenderPassDepthStencilAttachment
		{
			View = target.DepthView,
			DepthLoadOp = WGPULoadOp.Clear, DepthStoreOp = WGPUStoreOp.Store, DepthClearValue = 1f,
			StencilLoadOp = WGPULoadOp.Clear, StencilStoreOp = WGPUStoreOp.Store, StencilClearValue = 0,
		};
		var rp = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &ca, DepthStencilAttachment = &dsa };
		var pass = wgpuCommandEncoderBeginRenderPass(enc, &rp);

		foreach (var (kind, b0, u0, b1, flag, clip, clipBg) in ops)
		{
			if (!SetScissor(pass, clip.Aabb)) { continue; }
			switch (kind)
			{
				case 0:
					wgpuRenderPassEncoderSetPipeline(pass, _d.SolidPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b0, 0, (nuint)(u0 * 6 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, u0, 1, 0, 0);   // u0 = 6 * (coalesced) rect count
					break;
				case 1:
					wgpuRenderPassEncoderSetPipeline(pass, flag ? _d.StencilEvenOdd : _d.StencilNonZero);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b0, 0, (nuint)(u0 * 2 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, u0, 1, 0, 0);
					wgpuRenderPassEncoderSetPipeline(pass, _d.CoverPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetStencilReference(pass, 0);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(36 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 2:
					wgpuRenderPassEncoderSetPipeline(pass, _d.ImagePipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(24 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 3:
					wgpuRenderPassEncoderSetPipeline(pass, _d.GradientPipe);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderSetBindGroup(pass, 1, (IntPtr)clipBg, 0, (uint*)null);
					wgpuRenderPassEncoderSetVertexBuffer(pass, 0, (IntPtr)b1, 0, (nuint)(12 * sizeof(float)));
					wgpuRenderPassEncoderDraw(pass, 6, 1, 0, 0);
					break;
				case 4:
					wgpuRenderPassEncoderSetPipeline(pass, u0 == 1 ? _d.CompositeDstIn : _d.CompositeSrcOver);
					wgpuRenderPassEncoderSetBindGroup(pass, 0, (IntPtr)b0, 0, (uint*)null);
					wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
					break;
				case 5:
				{
					// Replay a cached recording's pre-encoded draws (scissor was reset to full above).
					var bundle = (IntPtr)b0;
					wgpuRenderPassEncoderExecuteBundles(pass, 1, (IntPtr)(&bundle));
					break;
				}
			}
		}

		wgpuRenderPassEncoderEnd(pass);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
		wgpuDevicePoll(_d.Dev, 1u, null);
	}

	public Matrix4x4 TotalMatrix => Matrix4x4.Identity;
	public void SetMatrix(in Matrix4x4 matrix) { }
	public void Concat(in Matrix4x4 matrix) { }
	public void Translate(float dx, float dy) { }
	public void Scale(float sx, float sy) { }
	public int Save() => 0;
	public int SaveCount => 0;
	public void Restore() { }
	public void RestoreToCount(int count) { }
	public void SaveLayer(bool antialias = false) { }
	public void SaveLayer(IColorFilter colorFilter, bool antialias = false) { }
	public void SaveLayer(BlendMode blendMode, bool antialias = false) { }
	public void SaveLayer(IEffectFilter filter) { }
	public void ClipRect(in Rect rect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipRoundRect(in RoundRectangle roundRect, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void ClipPath(IGeometry geometry, ClipOperation operation = ClipOperation.Intersect, bool antialias = false) { }
	public void Clear(WColor color) => _presentClear = color;
	public void DrawRect(in Rect rect, WColor color, bool antialias = false) { }
	public void DrawRect(in Rect rect, IShader shader, bool antialias = false) { }
	public void DrawPath(IGeometry geometry, WColor color, bool antialias = false) { }
	public void DrawShadow(IGeometry silhouette, WColor color, float sigmaX, float sigmaY, bool additive, bool antialias = false) { }
	public void StrokePath(IGeometry geometry, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawLine(Vector2 p0, Vector2 p1, WColor color, float strokeWidth, bool antialias = false) { }
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, float opacity = 1f, bool antialias = false) { }
	public void DrawImage(IImageTexture texture, float x, float y, ImageSampling sampling, IColorFilter colorFilter, bool antialias = false) { }
	public void DrawImageNineSlice(IImageTexture texture, in Rect centerSlice, in Rect destination, bool centerHollow, bool antialias = false) { }
	public void DrawEffectBackdrop(IEffectFilter filter, float opacity) { }
	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder();
	public void Dispose() { }
}

public sealed class WebGpuRenderer : IRenderer
{
	public readonly WebGpuDevice Device;
	public WebGpuRenderer(WebGpuDevice device) => Device = device;
	public ICommandRecorder BeginFrame() => new WebGpuCommandRecorder();
	public IPresentSession BeginPresent(IRenderTarget target) => new WebGpuPresentSession(Device, (WebGpuRenderSurface)target);
}

// --- New-SPI pluggable-backend surface (see doc/uno-drawing-backend-abstraction.md) ---

/// <summary>A <see cref="IGraphicsContext"/> wrapping a <see cref="WebGpuDevice"/>. Created by the graphics-layer context factory for <see cref="GraphicsContextKind.WebGpu"/>.</summary>
public sealed class WebGpuGraphicsContext : IGraphicsContext, IWebGpuDeviceContext
{
	public WebGpuGraphicsContext(WebGpuDevice device) => Device = device;

	public WebGpuDevice Device { get; }

	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public bool IsLost => false;

	// The WebGPU render path is driven by the (legacy, env-gated) X11WebGpu*Renderer today, not GraphicsRegistry
	// .Activate, so this context isn't asked to acquire/present. Stubbed until the WebGPU host adopts the seam.
	public IRenderTarget AcquireRenderTarget(int width, int height)
		=> throw new System.NotSupportedException("WebGpuGraphicsContext does not yet drive AcquireRenderTarget/Present.");

	public void Present()
		=> throw new System.NotSupportedException("WebGpuGraphicsContext does not yet drive AcquireRenderTarget/Present.");

	public void Dispose() { }
}

/// <summary>A host graphics context that owns a <see cref="WebGpuDevice"/> (e.g. an on-window swapchain context).
/// Lets <see cref="WebGpuGraphicsProvider"/> obtain the device without naming the platform context type.</summary>
public interface IWebGpuDeviceContext
{
	WebGpuDevice Device { get; }
}

/// <summary>The registerable WebGPU backend pair. Prefers a WebGPU context; needs an 8-bit stencil for path fills.</summary>
public sealed class WebGpuGraphicsProvider : IGraphicsProvider
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.WebGpu };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Rgba8888 };

	// The device is created by the host context (it owns the window swapchain); the device-bound WebGpu drawing
	// factory wraps whatever factory is current so images become GPU-resident, and the WebGpu renderer draws.
	public Uno.UI.Composition.Drawing.Graphics CreateGraphics(IGraphicsContext context)
	{
		var device = ((IWebGpuDeviceContext)context).Device;
		return new(new WebGpuDrawingFactory(device, DrawingFactory.Current), new WebGpuRenderer(device));
	}
}

// --- Device-bound factory (IImageTexture + eventual shaders) ---

/// <summary>A wgpu texture uploaded once from a neutral <see cref="IImage"/>'s pixels. Owned/disposed by the framework.</summary>
public sealed unsafe class WebGpuImageTexture : IImageTexture
{
	private readonly WebGpuDevice _d;
	private readonly IImage _source; // kept for the neutral CopyPixels cross-backend fallback
	public IntPtr Tex;
	public IntPtr View;

	public int PixelWidth { get; }
	public int PixelHeight { get; }

	public void CopyPixels(Span<byte> destination) => _source.CopyPixels(destination);

	public WebGpuImageTexture(WebGpuDevice device, IImage image)
	{
		_d = device;
		_source = image;
		int w = image.PixelWidth, h = image.PixelHeight;
		PixelWidth = w; PixelHeight = h;
		// A zero-sized source (e.g. an image brush whose surface isn't ready yet) would create an empty wgpu
		// texture whose view is a null/"empty" handle, which fails bind-group validation. Fall back to a 1x1
		// transparent texture so the draw is a no-op instead of a hard wgpu panic.
		if (w <= 0 || h <= 0)
		{
			var td0 = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 }, Format = WGPUTextureFormat.RGBA8Unorm, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst };
			Tex = wgpuDeviceCreateTexture(device.Dev, &td0);
			View = wgpuTextureCreateView(Tex, null);
			var transparent = new byte[4];
			var dst0 = new WGPUTexelCopyTextureInfo { Texture = Tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
			var layout0 = new WGPUTexelCopyBufferLayout { BytesPerRow = 4, RowsPerImage = 1 };
			var ext0 = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 };
			fixed (byte* p0 = transparent) { wgpuQueueWriteTexture(device.Q, &dst0, (IntPtr)p0, 4, &layout0, &ext0); }
			return;
		}
		var bgra = new byte[w * h * 4];
		image.CopyPixels(bgra);
		var rgba = new byte[w * h * 4];
		for (int i = 0; i < bgra.Length; i += 4) { rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1]; rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3]; }
		var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = WGPUTextureFormat.RGBA8Unorm, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst | WGPUTextureUsage.CopySrc };
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		var dst = new WGPUTexelCopyTextureInfo { Texture = Tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
		var layout = new WGPUTexelCopyBufferLayout { BytesPerRow = (uint)(w * 4), RowsPerImage = (uint)h };
		var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		fixed (byte* p = rgba) { wgpuQueueWriteTexture(device.Q, &dst, (IntPtr)p, (nuint)rgba.Length, &layout, &ext); }
	}

	public void Dispose()
	{
		if (View != IntPtr.Zero) { wgpuTextureViewRelease(View); View = IntPtr.Zero; }
		if (Tex != IntPtr.Zero) { wgpuTextureDestroy(Tex); Tex = IntPtr.Zero; }
	}
}

/// <summary>A managed <see cref="IImage"/> over a WebGPU offscreen readback. The readback bytes are in the
/// device's color format (RGBA for the offscreen device, BGRA for a swapchain device); <see cref="CopyPixels"/>
/// yields BGRA per the seam's image convention, swapping R/B only when the source is RGBA. No Skia.</summary>
internal sealed class WebGpuReadbackImage : IImage
{
	private readonly byte[] _bytes;
	private readonly bool _sourceIsBgra;
	public WebGpuReadbackImage(int width, int height, byte[] bytes, bool sourceIsBgra) { PixelWidth = width; PixelHeight = height; _bytes = bytes; _sourceIsBgra = sourceIsBgra; }
	public int PixelWidth { get; }
	public int PixelHeight { get; }
	public void CopyPixels(Span<byte> destination)
	{
		int n = Math.Min(_bytes.Length, destination.Length);
		if (_sourceIsBgra) { _bytes.AsSpan(0, n).CopyTo(destination); return; }
		for (int i = 0; i + 3 < n; i += 4) { destination[i] = _bytes[i + 2]; destination[i + 1] = _bytes[i + 1]; destination[i + 2] = _bytes[i]; destination[i + 3] = _bytes[i + 3]; }
	}
}

/// <summary>
/// The device-bound WebGPU resource factory. Textures, gradient shaders, color filters, the drop-shadow /
/// backdrop-blur effect, and offscreen rasterization are all WebGPU-owned; only neutral geometry delegates to
/// the inner factory. So paired with a managed inner (<see cref="ManagedDrawingFactory"/>) a WebGPU app links
/// zero SkiaSharp for its drawing; paired with Skia it still works (geometry via SKPath, non-blur effects via
/// the inner DAG). Font resolution/shaping and image decode are separate seams (FontProvider / ImageDecoder).
/// </summary>
public sealed class WebGpuDrawingFactory : IDrawingFactory
{
	private readonly WebGpuDevice _device;
	private readonly IDrawingFactory _inner;

	public WebGpuDrawingFactory(WebGpuDevice device, IDrawingFactory inner) { _device = device; _inner = inner; }

	public IImageTexture CreateImageTexture(IImage image) => new WebGpuImageTexture(_device, image);

	// Geometry is neutral — delegate to the inner factory (managed engine → no Skia; Skia → SKPath).
	public IPathBuilder CreatePathBuilder() => _inner.CreatePathBuilder();
	public IPrimitiveGeometryBuilder CreatePrimitiveGeometryBuilder() => _inner.CreatePrimitiveGeometryBuilder();
	public IGeometry CreateRectangleGeometry(Windows.Foundation.Rect rect) => _inner.CreateRectangleGeometry(rect);

	// Offscreen rasterization on the WebGPU device (record → present into an offscreen surface → read back),
	// so a WebGPU app needs no Skia rasterizer. The readback is a managed IImage (BGRA on CopyPixels).
	public IImage RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render)
	{
		var recorder = new WebGpuCommandRecorder();
		render(recorder);
		var surface = new WebGpuRenderSurface(_device, pixelWidth, pixelHeight);
		var present = new WebGpuPresentSession(_device, surface);
		present.ReplayNested(recorder.Finish());   // nested: don't reset the enclosing frame's pools
		var bytes = _device.ReadPixelsRgba(surface);   // bytes are in the device's color format
		return new WebGpuReadbackImage(pixelWidth, pixelHeight, bytes, _device.ColorFormat == WGPUTextureFormat.BGRA8Unorm);
	}
	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = false, P0 = start, P1 = end, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = true, P0 = center, P1 = gradientOrigin, RadiusX = radiusX, RadiusY = radiusY, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IColorFilter CreateBlendModeColorFilter(WColor color, BlendMode mode) => new WebGpuColorFilter { IsBlendMode = true, Color = color, Mode = mode };
	public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => new WebGpuColorFilter { Matrix = matrix };
	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, WColor color) => new WebGpuEffectFilter { Dx = dx, Dy = dy, SigmaX = sigmaX, SigmaY = sigmaY, Color = color };
	public IEffectFilter CreateEffectFilter(Windows.Graphics.Effects.IGraphicsEffect effect, Windows.Foundation.Rect bounds, Func<string, Uno.UI.Composition.Drawing.IEffectSource> sourceResolver, bool useBackdropBlurClamp, bool isSoftwareRenderer, out bool hasBackdropInput)
	{
		// Simplified realization: walk the graph for a GaussianBlur (the acrylic backdrop blur) and honor it as a
		// backdrop blur + tint. Anything else falls back to the inner (Skia) factory. The full IGraphicsEffect DAG
		// (noise, multi-stage blends) is not translated. GaussianBlurEffect GUID per EffectHelpers.
		var blurGuid = new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");
		float sigma = 0f;
		WColor tint = default, lum = default;
		bool sawColorSource = false;
		void Walk(object node)
		{
			if (node is IGraphicsEffectD2D1Interop io)
			{
				if (io.GetEffectId() == blurGuid)
				{
					io.GetNamedPropertyMapping("BlurAmount", out var idx, out _);
					if (io.GetProperty(idx) is float f) { sigma = MathF.Max(sigma, f); }
				}
				// Acrylic bakes its tint/luminosity into named ColorSourceEffects ("TintColor"/"LuminosityColor").
				// The tint is composited SrcOver on top; the luminosity is SrcOver over the blurred backdrop, which
				// equals the original's mix(blurred, lum.rgb, lum.a) luminosity blend.
				if (node is Windows.Graphics.Effects.IGraphicsEffect ge && ge.Name is "TintColor" or "LuminosityColor")
				{
					io.GetNamedPropertyMapping("Color", out var ci, out _);
					if (io.GetProperty(ci) is WColor c)
					{
						sawColorSource = true;
						if (ge.Name == "TintColor") { tint = c; } else { lum = c; }
					}
				}
				for (uint i = 0; i < io.GetSourceCount(); i++) { if (io.GetSource(i) is { } s) { Walk(s); } }
			}
		}
		Walk(effect);
		if (sigma > 0f || sawColorSource)
		{
			hasBackdropInput = true;
			return new WebGpuEffectFilter { SigmaX = sigma, SigmaY = sigma, Color = tint, LumColor = lum };
		}
		// A non-blur, non-acrylic effect (e.g. grayscale/hue/sepia on an image source): the WebGPU backend can't
		// realize it as a backdrop filter. Return null so CompositionEffectBrush.TryPaint falls back to the recipe
		// path — reduce to a source + composed 4×5 colour matrix and paint the source through a colour-matrix layer.
		hasBackdropInput = false;
		return null;
	}
}
