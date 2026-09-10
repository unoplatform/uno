// The WebGPU device: the wgpu handles, every pipeline and bind-group layout built on them, and the per-frame
// deferred-release queues. The WGSL these pipelines compile is in WebGpuShaders.cs; the shared buffers they draw
// from are in WebGpuSlabs.cs.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuDevice : IDisposable
{
	public IntPtr Inst;
	public IntPtr Adapter;
	public IntPtr Dev;
	public IntPtr Q;
	public IntPtr SolidPipe;
	public IntPtr ImagePipe;
	public IntPtr ImageDstInPipe;        // the image draw blended DstIn: a mask layer's composite
	public IntPtr GradientPipe;
	public IntPtr RrPipe;                // analytic rounded-rect / border-ring fill (per-vertex SDF quad)
	public IntPtr BlurPipe;              // separable gaussian (fullscreen), single-sample
	public IntPtr BlurBgl;
	public IntPtr CompositeSrcOver;      // fullscreen SrcOver of a texture: the effect evaluator's final draw
	public IntPtr CompositeBgl;         // its group(0) layout
	public IntPtr CompositeBlend;       // two-texture blend: fg(0) over bg(3), full Porter-Duff + separable/non-separable
	public IntPtr CompositeBlendBgl;    // group(0): fg tex, sampler, uniform (params.z = mode id), bg tex
	public IntPtr EffectCombine;        // two-texture linear combine (CrossFade/ArithmeticComposite) + AlphaMask
	public IntPtr EffectCombineBgl;
	public IntPtr ColorFunc;            // single-input per-channel colour function (Contrast / GammaTransfer)
	public IntPtr ColorFuncBgl;
	public IntPtr EffectNoise;          // procedural WhiteNoise generator (no input)
	public IntPtr EffectNoiseBgl;
	public IntPtr DummyTex;                 // 1x1 placeholder for the clip coverage binding when no path clip
	public IntPtr DummyClipMore;            // one-entry placeholder for the clip overflow binding when a draw has four clips or fewer
	public WebGpuTexturePool Pool;                // transient offscreen pool (reused across frames)
	public WebGpuBufferPool BufferPool;           // transient vertex/uniform buffer pool (reused across frames)
	public WebGpuClipSlab ClipSlab;               // size-classed storage slab backing every owned/restamped ClipU
	public WebGpuUniformSlab GradSlab;            // per-frame gradient uniforms, one queue write per chunk
	// Per-frame ClipU slabs for IMMEDIATE ops, one per (bind-group layout, byte size): a slot's bind group is created
	// once and reused, so it must always be built with the same layout and bind the same size.
	private WebGpuUniformSlab _clipBgSlab;

	public WebGpuUniformSlab ClipBgSlab => _clipBgSlab ??= new WebGpuUniformSlab(this, WebGpuPresentSession.ClipUBytes, DummyTex, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst, DummyTex, Smp, DummyClipMore);

	/// <summary>Uploads every per-frame uniform slab — call before any submit whose commands read them.</summary>
	public void FlushFrameSlabs()
	{
		GradSlab?.Flush();
		_clipBgSlab?.Flush();
	}
	// Serializes a whole frame's render (reset → record → submit → poll) on this device. The on-window render
	// loop and off-loop renders (RenderTargetBitmap) share the device's transient pools/caches, so two frames
	// must not overlap or one frame's BeginFrameResources frees the other's in-flight resources (wgpu panics).
	public readonly object RenderGate = new();
	private readonly System.Collections.Generic.List<nint> _pendingBindGroups = new();
	private readonly System.Collections.Generic.List<nint> _pendingBuffers = new();
	private readonly System.Collections.Generic.List<nint> _pendingClipSlots = new();
	private readonly System.Collections.Generic.List<WebGpuPathAtlas.Slot> _pendingAtlasSlots = new();
	// Transient image textures whose owning IRenderRecord was disposed; drained (GPU-released) at the next frame start.
	// Concurrent because a frame is disposed on the UI thread while BeginFrameResources runs on the render thread.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(nint view, nint tex)> _pendingTextures = new();
	// Per-recording compiled GPU draw-list. It lives ON the recording's WebGpuRenderRecord (IRenderRecord is, by its own
	// contract, "backend-defined retained state"), built once and replayed cheaply — no global cache, no per-frame
	// eviction scan. When the owning IRenderRecord is disposed (UI thread, on a content change), its compiled state is
	// enqueued here and freed on the render thread at the next BeginFrameResources (concurrent, like _pendingTextures).
	// Decoupled from the renderer's WebGpuGeometryCache: the device only needs the GPU resources to free (two
	// OwnedResources bags) + the transform-table slot to reclaim, so the queue carries those primitives — keeping
	// WebGpuDevice (init tier) free of any renderer type.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(OwnedResources Owned, OwnedResources StampOwned)> _pendingCompiled = new();
	internal void DeferCompiledRelease(OwnedResources owned, OwnedResources stampOwned) => _pendingCompiled.Enqueue((owned, stampOwned));

	// Per-frame bind groups reference the frame's pooled buffers, so they're released at the next frame start once
	// the previous frame's GPU work has completed (present DevicePolls). A cached recording's persistent resources
	// released mid-frame (cache miss) are deferred the same way, since ops already emitted this frame still use
	// them. Pooled buffers/textures are reused (not released). Call once per frame before rebuilding.
	// Monotonic per-session-frame counter: lets stamp memos detect "already stamped under the current submit",
	// where an in-place uniform rewrite would clobber data this frame's earlier draws still reference.
	public long FrameSeq;


	public void BeginFrameResources()
	{
		// Read LAST frame's timestamps here: the resolve/copy are recorded into the frame encoder, so mapping
		// before submit targets a buffer still pending in an unsubmitted command buffer and never completes.
		GradSlab?.Reset();
		_clipBgSlab?.Reset();
		FrameSeq++;
		Pool.BeginFrame();
		BufferPool.BeginFrame();
		foreach (var bg in _pendingBindGroups) { wgpuBindGroupRelease((IntPtr)bg); }
		foreach (var b in _pendingBuffers) { wgpuBufferRelease((IntPtr)b); }
		// Release (refcount) rather than Destroy (immediate) the transient one-shot textures: wgpu then frees them
		// only once the GPU has finished the frames that used them — safe even when the per-frame drain is skipped.
		while (_pendingTextures.TryDequeue(out var t)) { if (t.view != IntPtr.Zero) { wgpuTextureViewRelease((IntPtr)t.view); } if (t.tex != IntPtr.Zero) { wgpuTextureRelease((IntPtr)t.tex); } }
		_pendingBindGroups.Clear();
		_pendingBuffers.Clear();
		// Clip-slab slots ride the same deferred pipeline as the buffers/bind groups that referenced them.
		foreach (var s in _pendingClipSlots) { ClipSlab.Free(s); }
		_pendingClipSlots.Clear();
		foreach (var a in _pendingAtlasSlots) { PathAtlas.Free(a); }
		_pendingAtlasSlots.Clear();
		PathAtlas.SweepCache(FrameSeq);
		ReleaseRetiredAtlasPages();


		// Free the arena entries whose owning recording was disposed.
		while (_pendingCompiled.TryDequeue(out var c)) { DeferRelease(c.Owned); DeferRelease(c.StampOwned); }
	}

	public IntPtr TrackBg(IntPtr bg) { _pendingBindGroups.Add((nint)bg); return bg; }

	// Queues a transient image texture's GPU release for the next frame start. A brush that uploads a one-shot
	// texture (e.g. CompositionNineGridBrush) disposes it right after recording its draw, but the WebGPU draw is
	// replayed at present (possibly across several presents of the same recording) — so the texture must live until
	// its owning IRenderRecord is disposed. WebGpuRenderRecord.Dispose calls this; the actual free happens at the next
	// BeginFrameResources, after the last present's submit+DevicePoll, like the per-frame bind groups/buffers.
	internal void DeferTextureRelease(IntPtr view, IntPtr tex) => _pendingTextures.Enqueue(((nint)view, (nint)tex));

	// Defers a cached recording's persistent resources for release at the next frame start. Idempotent per bag
	// (see OwnedResources.Released) — concurrent rebuild/Dispose hand-offs must not double-release. Returns
	// whether THIS call claimed the bag (callers gate coupled frees, e.g. the transform slot, on the claim).
	internal bool DeferRelease(OwnedResources owned)
	{
		if (owned is null || System.Threading.Interlocked.Exchange(ref owned.Released, 1) != 0) { return false; }
		_pendingBuffers.AddRange(owned.Buffers);
		_pendingBindGroups.AddRange(owned.BindGroups);
		if (owned.ClipSlots is { } slots) { _pendingClipSlots.AddRange(slots); }
		if (owned.AtlasSlots is { } aslots) { _pendingAtlasSlots.AddRange(aslots); }
		if (owned.Textures is { } texs) { foreach (var tx in texs) { _pendingTextures.Enqueue(tx); } }
		return true;
	}
	internal void DeferReleaseBuffer(nint buf) { if (buf != IntPtr.Zero) { _pendingBuffers.Add(buf); } }

	public IntPtr ImgBgl;
	public IntPtr GradBgl;
	// Explicit SHARED ClipU layout: every colour pipeline binds it at its last group, so a single ClipU bind group
	// binds to any of them (auto-derived layouts are pipeline-exclusive).
	public IntPtr ClipBgl;
	// Group 0 of every colour pipeline: the pass projection (16 bytes), one bind group per pass.
	public IntPtr PassBgl;
	public IntPtr Smp;
	private readonly IntPtr[] _tiledSmp = new IntPtr[16];

	/// <summary>The sampler for a tiled image draw with these per-axis edge modes.</summary>
	public IntPtr TiledSampler(EdgeExtend x, EdgeExtend y) => _tiledSmp[((int)x * 4) + (int)y];

	private static WGPUAddressMode Address(EdgeExtend extend) => extend switch
	{
		EdgeExtend.Wrap => WGPUAddressMode.Repeat,
		EdgeExtend.Mirror => WGPUAddressMode.MirrorRepeat,
		_ => WGPUAddressMode.ClampToEdge,
	};

	// Gradient stops are evaluated analytically in-shader (exact, unlike a quantised LUT). The cap sizes the colour
	// + stop arrays in the uniform; raised well past any realistic UI gradient so >16-stop gradients render all their
	// stops instead of silently clamping (the original branch used an unbounded 256-entry LUT — analytic ≤ cap is
	// crisper). Float offsets within the uniform are derived so the layout stays consistent if the cap changes.
	public const int MaxGradientStops = 64;
	public const int GradColorsBase = 8;                                    // floats: after header(4) + geo(4)
	public const int GradStopsBase = GradColorsBase + MaxGradientStops * 4; // colours are vec4 each
	public const int GradOriginBase = GradStopsBase + MaxGradientStops;     // stops are one float each (packed as vec4[])
	public const int GradientUniformBytes = (GradOriginBase + 4) * 4;       // + origin(vec4)

	// Multisample count for anti-aliasing. Every pipeline + the color/depth render targets use this; the pass
	// renders into a multisampled color texture that resolves into the single-sample present/readback texture.
	// Multisample count, probed per device at init (PickSampleCount): 2x when the device supports it for our colour
	// format (half the MSAA colour/depth bandwidth + resolve cost of 4x for near-identical AA at typical DPI), else
	// Single-sampled unless the host asks otherwise: the pass renders straight into the attachment with no
	// resolve, and the recorder emits its analytic AA ring instead. The host (WebGpuInitDevice) picks the count
	// and bakes it here via the adopt ctor.
	/// <summary>Path rasterisation inputs, resolved at draw time at the density they are drawn at.</summary>
	internal readonly WebGpuShapeCache Shapes = new();
	// Same resolve with colour = src * dst, so successive path clips AND into one mask (its first pass clears to 1).

	/// <summary>A single-sample render target in the DEVICE colour format, usable as a shader input.</summary>
	// The color-attachment format the pipelines + offscreen targets use. Rgba8Unorm by default (the
	// offscreen/readback path assumes it); a swapchain renderer passes the surface's supported format.
	public readonly WGPUTextureFormat ColorFormat;
	public const WGPUTextureFormat DefaultColorFormat = WGPUTextureFormat.RGBA8Unorm;

	// Adopts the device the HOST already created (Uno.UI.Composition.WebGpu.Init) via the neutral
	// IWebGpuDeviceContext — the renderer stands on the raw wgpu handles + the host's chosen sample count, exactly
	// as a third-party WebGPU backend would. No device bring-up here (that is the host's WebGpuInitDevice).
	public WebGpuDevice(IWebGpuDeviceContext ctx)
	{
		ColorFormat = ctx.ColorFormat == 0 ? DefaultColorFormat : (WGPUTextureFormat)ctx.ColorFormat;
		Inst = ctx.Instance;
		Adapter = ctx.Adapter;
		Dev = ctx.Device;
		var ownImport = false;
		// Browser: the neutral seam hands us the live JS GPUDevice. Convert it to a wgpu pointer HERE — the backend's
		// own emdawn import — rather than relying on a pre-imported native pointer in the contract (a direct-JS backend
		// would use the JS object as-is). This is a SECOND import of the same JS device; the host's swapchain holds the
		// first for present. Both wgpu handles wrap the same underlying JS GPUDevice.
		if (OperatingSystem.IsBrowser() && ctx.JsDevice is { } jsDev)
		{
			var p = WebGpuJsInterop.ImportDevice(jsDev, (int)ctx.Instance);
			if (p != 0) { Dev = (IntPtr)p; ownImport = true; System.Console.WriteLine($"[webgpu] backend imported JS device ptr={p}"); }
		}
		Q = (!ownImport && ctx.Queue != IntPtr.Zero) ? ctx.Queue : wgpuDeviceGetQueue(Dev);
		FinishInit();
	}

	private void FinishInit()
	{
		CreatePipelines();
		DummyTex = CreateColorTarget(1, 1);
		var moreDesc = new WGPUBufferDescriptor { Size = WebGpuPresentSession.ClipEntryBytes, Usage = WGPUBufferUsage.Storage };
		DummyClipMore = wgpuDeviceCreateBuffer(Dev, &moreDesc);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
		ClipSlab = new WebGpuClipSlab(this);
		GradSlab = new WebGpuUniformSlab(this, GradientUniformBytes);
		System.Console.WriteLine($"[webgpu] engine init — colorFormat={ColorFormat}");
	}

	/// <summary>Synchronous GPU→CPU readback of a texture, tightly-packed in the device color format, via a blocking
	/// wgpuDevicePoll spin. Off-browser only (a native thread can pump the map); on the browser the drawing factory's
	/// SnapshotAsync maps off the JS event loop instead, so this is never reached there.</summary>
	public WebGpuReadbackImage ReadPixelsToImage(IntPtr tex, int w, int h, bool sourceIsBgra)
	{
		EncodeCopyTexToReadbackBuffer(tex, w, h, out var buf, out var total, out var padded);
		_ = wgpuDevicePoll(Dev, 1u, null);

		var mapped = new bool[1];
		var mh = GCHandle.Alloc(mapped);
		wgpuBufferMapAsync(buf, WGPUMapMode.Read, 0, (nuint)total, new WGPUBufferMapCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUMapAsyncStatus, WGPUStringView, IntPtr, IntPtr, void>)&OnMap,
			Userdata1 = GCHandle.ToIntPtr(mh),
		});
		while (!mapped[0]) { _ = wgpuDevicePoll(Dev, 1u, null); }
		mh.Free();
		var mp = (byte*)(void*)wgpuBufferGetMappedRange(buf, 0, (nuint)total);
		// Unpadded straight out of the mapped range into the image's own buffer — no intermediate array.
		var image = new WebGpuReadbackImage(w, h, new ReadOnlySpan<byte>(mp, (int)total), padded, sourceIsBgra);
		wgpuBufferUnmap(buf);
		wgpuBufferDestroy(buf);
		return image;
	}

	/// <summary>Creates a MAP_READ buffer, copies <paramref name="tex"/> into it (256-byte-aligned rows) and submits.
	/// The caller maps <paramref name="buf"/> (blocking off-browser, async in JS on the browser) then destroys it.</summary>
	public void EncodeCopyTexToReadbackBuffer(IntPtr tex, int w, int h, out IntPtr buf, out int total, out int padded)
	{
		uint pad = ((uint)(w * 4) + 255u) & ~255u;              // wgpu requires 256-byte row alignment for T2B copies
		ulong tot = (ulong)pad * (uint)h;
		var bd = new WGPUBufferDescriptor { Size = (nuint)tot, Usage = WGPUBufferUsage.CopyDst | WGPUBufferUsage.MapRead };
		buf = wgpuDeviceCreateBuffer(Dev, &bd);
		var enc = wgpuDeviceCreateCommandEncoder(Dev, null);
		var src = new WGPUTexelCopyTextureInfo { Texture = tex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = default };
		var dst = new WGPUTexelCopyBufferInfo { Buffer = buf, Layout = new WGPUTexelCopyBufferLayout { Offset = 0, BytesPerRow = pad, RowsPerImage = (uint)h } };
		var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		wgpuCommandEncoderCopyTextureToBuffer(enc, &src, &dst, &ext);
		var cb = wgpuCommandEncoderFinish(enc, null);
		wgpuQueueSubmit(Q, 1, (IntPtr)(&cb));
		wgpuCommandBufferRelease(cb);
		wgpuCommandEncoderRelease(enc);
		total = (int)tot;
		padded = (int)pad;
	}

	public void DestroyBuffer(IntPtr buf) => wgpuBufferDestroy(buf);

	/// <summary>Set by the browser head at WebGPU init: maps a readback buffer (by wgpu handle ptr) off the JS
	/// event loop and returns its raw (row-padded) bytes. The only way to complete a GPU→CPU map on WASM, where a
	/// synchronous poll can't yield. Off-browser this stays null and readback uses the blocking poll.</summary>


	/// <summary>A single-sample Rgba8 render target usable as a shader input (offscreen blur temp/output). The
	/// returned view keeps its texture alive; not pooled/freed yet (fine for offscreen/one-shot).</summary>
	public IntPtr CreateColorTarget(int w, int h)
	{
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = DefaultColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		return wgpuTextureCreateView(tex, null);
	}

	/// <summary>
	/// Shared coverage atlas for small paths (glyphs). Created on first use; CopyDst because entries are
	/// rasterized into a scratch surface and copied in, TextureBinding because the draw samples it.
	/// </summary>
	public WebGpuPathAtlas PathAtlas { get; } = new();

	/// <summary>Opens another atlas page. Pages are added when the existing ones are exhausted.</summary>
	public void AddPathAtlasPage()
	{
		if (PathAtlas.RegularPages >= WebGpuPathAtlas.MaxPages) { return; }
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = WebGpuPathAtlas.Size, Height = WebGpuPathAtlas.Size, DepthOrArrayLayers = 1 },
			// The device's own format: entries are copied in from a scratch surface, and a texture copy requires
			// matching formats (Bgra8 vs Rgba8 is not copy-compatible).
			Format = ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		PathAtlas.AddPage(tex, wgpuTextureCreateView(tex, null));
	}

	/// <summary>Destroys the textures of pages whose last slot was freed. Called at the frame boundary.</summary>
	public void ReleaseRetiredAtlasPages()
	{
		if (PathAtlas.Retired.Count == 0) { return; }
		foreach (var page in PathAtlas.Retired)
		{
			if (page.View != IntPtr.Zero) { wgpuTextureViewRelease(page.View); }
			if (page.Texture != IntPtr.Zero) { wgpuTextureRelease(page.Texture); }
		}
		PathAtlas.Retired.Clear();
	}

	private void CreateCoverageSheetPipelines()
	{
		var module = Module(CoverageSheetWgsl);
		var e = stackalloc WGPUBindGroupLayoutEntry[3];
		e[0] = new WGPUBindGroupLayoutEntry { Binding = 0, Visibility = WGPUShaderStage.Vertex, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.ReadOnlyStorage } };
		e[1] = new WGPUBindGroupLayoutEntry { Binding = 1, Visibility = WGPUShaderStage.Vertex, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.ReadOnlyStorage } };
		e[2] = new WGPUBindGroupLayoutEntry { Binding = 2, Visibility = WGPUShaderStage.Vertex, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = 16 } };
		var bgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 3, Entries = e };
		CoverageSheetBgl = wgpuDeviceCreateBindGroupLayout(Dev, &bgld);
		var bgl = CoverageSheetBgl;
		var pld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 1, BindGroupLayouts = (IntPtr)(&bgl) };
		var blend = new WGPUBlendState
		{
			Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.One, Operation = WGPUBlendOperation.Add },
			Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.One, Operation = WGPUBlendOperation.Add },
		};
		var target = new WGPUColorTargetState { Format = CoverageFormat, Blend = &blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = SV("fs"), TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = module, EntryPoint = SV("vs"), BufferCount = 0 },
			Fragment = &fsState,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = wgpuDeviceCreatePipelineLayout(Dev, &pld),
		};
		CoverageSheetPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);

		var rmodule = Module(CoverageResolveSheetWgsl);
		var re = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Fragment,
			Texture = new WGPUTextureBindingLayout { SampleType = WGPUTextureSampleType.UnfilterableFloat, ViewDimension = WGPUTextureViewDimension._2D },
		};
		var rbgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &re };
		CoverageResolveSheetBgl = wgpuDeviceCreateBindGroupLayout(Dev, &rbgld);
		var rbgl = CoverageResolveSheetBgl;
		var rpld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 1, BindGroupLayouts = (IntPtr)(&rbgl) };
		var attrs = stackalloc WGPUVertexAttribute[3];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 };
		attrs[2] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 16, ShaderLocation = 2 };
		var vb = new WGPUVertexBufferLayout { ArrayStride = 24, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 3, Attributes = attrs };
		var rtarget = new WGPUColorTargetState { Format = ColorFormat, Blend = null, WriteMask = WGPUColorWriteMask.All };
		var rfs = new WGPUFragmentState { Module = rmodule, EntryPoint = SV("fs"), TargetCount = 1, Targets = &rtarget };
		var rpd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = rmodule, EntryPoint = SV("vs"), BufferCount = 1, Buffers = &vb },
			Fragment = &rfs,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = wgpuDeviceCreatePipelineLayout(Dev, &rpld),
		};
		CoverageResolveSheetPipe = wgpuDeviceCreateRenderPipeline(Dev, &rpd);
	}

	private IntPtr Module(string wgsl)
	{
		var code = SV(wgsl);
		var w = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var d = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&w };
		return wgpuDeviceCreateShaderModule(Dev, &d);
	}

	// Explicit ClipU bind-group layout (the uniform at binding 0, read by vertex place + fragment clipCov, and the
	// path-clip mask at binding 1) wrapped in a pipeline layout the colour pipelines share.
	private IntPtr MakeClipPipeLayout()
	{
		var e = stackalloc WGPUBindGroupLayoutEntry[5];
		// The ClipU uniform: header plus the first four entries, a fixed size. Entries past those ride the storage
		// buffer at binding 4, so nesting stays uncapped while the common draw reads only the uniform.
		e[0] = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Vertex | WGPUShaderStage.Fragment,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = WebGpuPresentSession.ClipUBytes },
		};
		e[4] = new WGPUBindGroupLayoutEntry
		{
			Binding = 4,
			Visibility = WGPUShaderStage.Vertex | WGPUShaderStage.Fragment,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.ReadOnlyStorage, MinBindingSize = WebGpuPresentSession.ClipEntryBytes },
		};
		// The path-clip coverage mask rides the same group, so one clip bind group still binds to every pipeline.
		// Clips without a path bind DummyTex and never read it: ClipU.mask.z gates the sample.
		e[1] = new WGPUBindGroupLayoutEntry
		{
			Binding = 1,
			Visibility = WGPUShaderStage.Fragment,
			Texture = new WGPUTextureBindingLayout { SampleType = WGPUTextureSampleType.Float, ViewDimension = WGPUTextureViewDimension._2D },
		};
		// The op's own coverage texture (see ClipData.Coverage) and the sampler its per-vertex uv reads it with.
		e[2] = new WGPUBindGroupLayoutEntry
		{
			Binding = 2,
			Visibility = WGPUShaderStage.Fragment,
			Texture = new WGPUTextureBindingLayout { SampleType = WGPUTextureSampleType.Float, ViewDimension = WGPUTextureViewDimension._2D },
		};
		e[3] = new WGPUBindGroupLayoutEntry
		{
			Binding = 3,
			Visibility = WGPUShaderStage.Fragment,
			Sampler = new WGPUSamplerBindingLayout { Type = WGPUSamplerBindingType.Filtering },
		};
		var bgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 5, Entries = e };
		ClipBgl = wgpuDeviceCreateBindGroupLayout(Dev, &bgld);
		var pe = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Vertex,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = 16 },
		};
		var pbgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &pe };
		PassBgl = wgpuDeviceCreateBindGroupLayout(Dev, &pbgld);
		return ColourLayout(ClipBgl);
	}

	// [pass, group1, ..., ClipBgl]: the pass projection first, the op's clip last, the pipeline's own groups between.
	private IntPtr ColourLayout(params IntPtr[] groups)
	{
		var bgls = stackalloc IntPtr[groups.Length + 1];
		bgls[0] = PassBgl;
		for (int i = 0; i < groups.Length; i++) { bgls[i + 1] = groups[i]; }
		var pld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = (nuint)(groups.Length + 1), BindGroupLayouts = (IntPtr)bgls };
		return wgpuDeviceCreatePipelineLayout(Dev, &pld);
	}

	private void CreatePipelines()
	{
		var colored = Module(ClipStructFn + ColoredWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var clipLayout = MakeClipPipeLayout();

		var blend = new WGPUBlendState
		{
			Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.SrcAlpha, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
			Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
		};

		SolidPipe = MakePipe(colored, vs, fs, &blend, clipLayout);
		CreateCoverageSheetPipelines();
		CreateImagePipeline();
		CreateGradientPipeline(&blend);
		CreateRoundedRectPipeline(&blend);
		CreateBlurPipeline();
		CreateCompositePipelines();
	}


	/// <summary>Single-channel float so accumulation can exceed 1 and go negative; blendable, unlike r32float.</summary>
	public const WGPUTextureFormat CoverageFormat = WGPUTextureFormat.R16Float;
	public IntPtr CoverageSheetPipe, CoverageSheetBgl, CoverageResolveSheetPipe, CoverageResolveSheetBgl;   // the per-frame mask sheet's passes

	// Accumulates signed area into a CoverageFormat target with additive blending. Single-sampled whatever the
	// frame is: the mask IS the coverage, so supersampling it would only quantise an answer that is already exact.
	private void CreateCompositePipelines()
	{
		var module = Module(CompositeWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var over = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add } };
		CompositeSrcOver = MakeComposite(module, vs, fs, &over);
		CompositeBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeSrcOver, 0);

		// Two-texture blend (effect graph): the fragment emits the fully-composited pixel, so the pipeline REPLACES.
		var blendModule = Module(CompositeBlendWgsl);
		var replace = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.Zero, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.Zero, Operation = WGPUBlendOperation.Add } };
		CompositeBlend = MakeComposite(blendModule, vs, fs, &replace);
		CompositeBlendBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeBlend, 0);

		var combineModule = Module(EffectCombineWgsl);
		EffectCombine = MakeComposite(combineModule, vs, fs, &replace);
		EffectCombineBgl = wgpuRenderPipelineGetBindGroupLayout(EffectCombine, 0);

		var colorFuncModule = Module(ColorFuncWgsl);
		ColorFunc = MakeComposite(colorFuncModule, vs, fs, &replace);
		ColorFuncBgl = wgpuRenderPipelineGetBindGroupLayout(ColorFunc, 0);

		var noiseModule = Module(EffectNoiseWgsl);
		EffectNoise = MakeComposite(noiseModule, vs, fs, &replace);
		EffectNoiseBgl = wgpuRenderPipelineGetBindGroupLayout(EffectNoise, 0);
	}

	private IntPtr MakeComposite(IntPtr module, WGPUStringView vs, WGPUStringView fs, WGPUBlendState* blend)
	{
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = null,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}


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
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = null,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		BlurPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		BlurBgl = wgpuRenderPipelineGetBindGroupLayout(BlurPipe, 0);
	}




	private void CreateRoundedRectPipeline(WGPUBlendState* blend)
	{
		var module = Module(ClipStructFn + RoundedRectWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var attrs = stackalloc WGPUVertexAttribute[8]
		{
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 },   // cpos (NDC)
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 },   // p (local centred)
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 16, ShaderLocation = 2 },  // hf
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 24, ShaderLocation = 3 },  // radii
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 40, ShaderLocation = 4 },  // col
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 56, ShaderLocation = 5 },  // ihalf
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 64, ShaderLocation = 6 },  // icenter
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 72, ShaderLocation = 7 },  // iradii
		};
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 88, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 8, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = null, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = ColourLayout(ClipBgl) };
		RrPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	private void CreateGradientPipeline(WGPUBlendState* blend)
	{
		var module = Module(ClipStructFn + GradientWgsl);
		var ge = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Fragment,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = (ulong)GradientUniformBytes },
		};
		var gbgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &ge };
		GradBgl = wgpuDeviceCreateBindGroupLayout(Dev, &gbgld);
		var vs = SV("vs");
		var fs = SV("fs");
		var gattrs = stackalloc WGPUVertexAttribute[2];
		gattrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		gattrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 };
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 16, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 2, Attributes = gattrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = null, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = ColourLayout(GradBgl, ClipBgl) };
		GradientPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}


	// group 0 = image resources (texture + sampler + op uniform), group 1 = the SHARED ClipU layout.
	private IntPtr MakeImagePipeLayout()
	{
		var e = stackalloc WGPUBindGroupLayoutEntry[3];
		e[0] = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Fragment,
			Texture = new WGPUTextureBindingLayout { SampleType = WGPUTextureSampleType.Float, ViewDimension = WGPUTextureViewDimension._2D },
		};
		e[1] = new WGPUBindGroupLayoutEntry
		{
			Binding = 1,
			Visibility = WGPUShaderStage.Fragment,
			Sampler = new WGPUSamplerBindingLayout { Type = WGPUSamplerBindingType.Filtering },
		};
		e[2] = new WGPUBindGroupLayoutEntry
		{
			Binding = 2,
			Visibility = WGPUShaderStage.Fragment,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = ImageUniformBytes },
		};
		var bgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 3, Entries = e };
		ImgBgl = wgpuDeviceCreateBindGroupLayout(Dev, &bgld);
		return ColourLayout(ImgBgl, ClipBgl);
	}

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
			var blend = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add } };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = &blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = null, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = MakeImagePipeLayout() };
		ImagePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		// DstIn: the destination keeps only where the texture has alpha (out = dst * src.a).
		var dstIn = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.Zero, DstFactor = WGPUBlendFactor.SrcAlpha, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.Zero, DstFactor = WGPUBlendFactor.SrcAlpha, Operation = WGPUBlendOperation.Add } };
		var maskTarget = new WGPUColorTargetState { Format = ColorFormat, Blend = &dstIn, WriteMask = WGPUColorWriteMask.All };
		var maskFs = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &maskTarget };
		pd.Fragment = &maskFs;
		ImageDstInPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Linear, MaxAnisotropy = 1 };
		Smp = wgpuDeviceCreateSampler(Dev, &sd);
		// Address-mode variants for tiled image draws. EdgeExtend.None shares the clamp sampler: a non-filling
		// draw is bounded by its quad instead, so nothing ever samples past the texture.
		for (var x = 0; x < 4; x++)
		{
			for (var y = 0; y < 4; y++)
			{
				var td = sd;
				td.AddressModeU = Address((EdgeExtend)x);
				td.AddressModeV = Address((EdgeExtend)y);
				_tiledSmp[(x * 4) + y] = wgpuDeviceCreateSampler(Dev, &td);
			}
		}
	}

	// pos.xy + col.rgba + coverage uv vertices, no depth/stencil: every clip is analytic or a sampled mask.
	private IntPtr MakePipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, WGPUBlendState* blend, IntPtr layout)
	{
		var attrs = stackalloc WGPUVertexAttribute[3];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x4, Offset = 8, ShaderLocation = 1 };
		attrs[2] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 24, ShaderLocation = 2 };
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 32, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 3, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = null,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = layout,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	// Persistent UTF-8 for a WGPUStringView (WGSL/entry points; created once at pipeline init, intentionally not freed).
	private static WGPUStringView SV(string s)
		=> new() { Data = Marshal.StringToCoTaskMemUTF8(s), Length = (nuint)System.Text.Encoding.UTF8.GetByteCount(s) };

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMap(WGPUMapAsyncStatus status, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((bool[])GCHandle.FromIntPtr(u1).Target!)[0] = true;

	// Releases the transient pools (the multi-GB VRAM the offscreens + resize generations accumulate). The wgpu
	// device/queue/adapter/instance are owned by the host context and released there; here we only reclaim what
	// this device allocated into its own pools so a closed window doesn't leak its full-window textures.
	public void Dispose()
	{
		Pool?.Dispose();
		BufferPool?.Dispose();
	}
}
