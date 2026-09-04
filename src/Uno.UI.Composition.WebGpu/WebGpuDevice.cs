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
	public IntPtr StencilEvenOdd;
	public IntPtr StencilNonZero;
	public IntPtr CoverPipe;
	// Transform-table path-fill pipelines (device verts + per-vertex slot index). XformBgl = group 0 (storage
	// table); CoverTableClipBgl = the cover variant's group 1 (ClipU). Solid/clip-fans/shadows stay on the NDC pipes.
	public IntPtr StencilTableEO;
	public IntPtr StencilTableNZ;
	public IntPtr CoverTablePipe;
	/// <summary>Cover pipeline that does NOT consult the stencil: for geometry that already tiles its own shape,
	/// so there is no stencil pass to mask against (CoverTablePipe would draw nothing, since the stencil is 0).</summary>
	public IntPtr CoverTableDirectPipe;
	// Transform-table SOLID / ROUNDED-RECT variants (device-verts + per-vertex slot). Same table + ClipBgl as the
	// path-fill cover, but drawn unconditionally under the clip depth (no stencil test) so a moved solid/rrect
	// recording repositions via its slot with cross-visual coalescing preserved. See EmitTableFrameSolid.
	public IntPtr SolidTablePipe;
	public IntPtr RrTablePipe;
	public IntPtr XformBgl;
	// Persistent storage buffer + cached bind group for the main pass's per-frame arena transform table (group 0 of
	// the table path-fill pipelines). The table CONTENTS are rewritten every frame, but the buffer identity + bind
	// group only change when the table grows, so the bind group survives across frames — sparing a CreateBuffer +
	// CreateBindGroup on every path-fill frame. Bound at full capacity (the shader only indexes valid slots). Reused
	// across frames safely because wgpuQueueWriteBuffer is queue-ordered after the prior frame's reads; only the main
	// pass uses it (nested/pooled passes keep renting distinct transient buffers, so no in-frame write aliasing).
	private IntPtr _xformBuf; private nuint _xformCap; private IntPtr _xformBg;
	public IntPtr CoverTableClipBgl;
	// In-pass path-clip depth mask: instead of an offscreen coverage texture per clip, stencil the clip fan into
	// the shared depth buffer inside the main pass (depth=0 inside the clip, 1 outside) and let content depth-test
	// against it (GreaterEqual). Fullscreen depth writers (bbox-scissored): SetN clears the region to N; CoverN
	// writes N where the fan stencil is set (and resets the stencil). Depth = clip mask, stencil = fill winding.
	public IntPtr ClipDepthSet0;   // depth := 0 over the scissor region (restore "no clip")
	public IntPtr ClipDepthSet1;   // depth := 1 over the scissor region (prep intersect mask)
	public IntPtr ClipDepthCover0; // depth := 0 where stencil != 0 (inside the fan) + reset stencil — intersect
	public IntPtr ClipDepthCover1; // depth := 1 where stencil != 0 (inside the fan) + reset stencil — exclude
	public IntPtr ImagePipe;
	public IntPtr GradientPipe;
	public IntPtr RrPipe;                // analytic rounded-rect / border-ring fill (per-vertex SDF quad)
	public IntPtr RrClipBgl;             // group 0: ClipU
	public IntPtr BlurPipe;              // separable gaussian (fullscreen), single-sample
	public IntPtr BlurBgl;
	public IntPtr CompositeSrcOver;      // composite a layer texture into an MSAA pass (SrcOver / DstIn)
	public IntPtr CompositeDstIn;
	public IntPtr CompositeBgl;         // SrcOver's group(0) layout
	public IntPtr CompositeDstInBgl;    // DstIn's group(0) layout (auto-layouts aren't interchangeable)
	public IntPtr CompositeBlend;       // two-texture blend: fg(0) over bg(3), full Porter-Duff + separable/non-separable
	public IntPtr CompositeBlendBgl;    // group(0): fg tex, sampler, uniform (params.z = mode id), bg tex
	public IntPtr EffectCombine;        // two-texture linear combine (CrossFade/ArithmeticComposite) + AlphaMask
	public IntPtr EffectCombineBgl;
	public IntPtr ColorFunc;            // single-input per-channel colour function (Contrast / GammaTransfer)
	public IntPtr ColorFuncBgl;
	public IntPtr EffectNoise;          // procedural WhiteNoise generator (no input)
	public IntPtr EffectNoiseBgl;
	public IntPtr DummyTex;                 // 1x1 placeholder for the clip coverage binding when no path clip
	public WebGpuTexturePool Pool;                // transient offscreen pool (reused across frames)
	public WebGpuBufferPool BufferPool;           // transient vertex/uniform buffer pool (reused across frames)
	public WebGpuClipSlab ClipSlab;               // chunked uniform slab backing every owned/restamped ClipU
	public WebGpuUniformSlab GradSlab;            // per-frame gradient uniforms, one queue write per chunk
	// Per-frame ClipU slabs for IMMEDIATE ops, one per bind-group layout (a slot's bind group is created once and
	// reused, so it must always be built with the same layout).
	private readonly System.Collections.Generic.Dictionary<nint, WebGpuUniformSlab> _clipBgSlabs = new();

	public WebGpuUniformSlab ClipBgSlabFor(IntPtr layout, int clipUBytes)
	{
		if (!_clipBgSlabs.TryGetValue(layout, out var slab))
		{
			slab = new WebGpuUniformSlab(this, clipUBytes);
			_clipBgSlabs[layout] = slab;
		}
		return slab;
	}

	/// <summary>Uploads every per-frame uniform slab — call before any submit whose commands read them.</summary>
	public void FlushFrameSlabs()
	{
		GradSlab?.Flush();
		foreach (var kv in _clipBgSlabs) { kv.Value.Flush(); }
	}
	public WebGpuSlab SolidSlab;                  // persistent shared slab: all recordings' solid verts (6 floats/v)
	public WebGpuSlab RrectSlab;                  // persistent shared slab: all recordings' rrect verts (22 floats/v)
												  // Transform-TABLE shared slabs: local (identity-baked) verts + a trailing per-vertex slot index (solid = 7
												  // floats/v, rrect = 23). A moved recording rewrites its transform-table slot instead of re-Putting these verts,
												  // while sibling recordings still coalesce into one draw (each vertex indexes its own slot). See EmitTableFrameSolid.
	public WebGpuSlab SolidTableSlab;
	public WebGpuSlab RrectTableSlab;
	private long _nextSlabId = 1;                 // stable per-recording slab id (assigned on cache miss)
	public long NextSlabId() => _nextSlabId++;
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
	private readonly System.Collections.Concurrent.ConcurrentQueue<(OwnedResources Owned, OwnedResources StampOwned, int XformSlot)> _pendingCompiled = new();
	internal void DeferCompiledRelease(OwnedResources owned, OwnedResources stampOwned, int xformSlot) => _pendingCompiled.Enqueue((owned, stampOwned, xformSlot));

	// Transform-table slot allocator (render-thread only). Each cached path-fill recording owns a STABLE slot — an
	// index into the per-frame _xforms storage buffer. Its device verts bake that index once; the slot's local->NDC
	// affine is rewritten every frame it draws, so a move/resize/DPI change touches only the table, never the verts.
	// A disposed recording's slot is recycled when its compiled state drains below (render thread), so alloc/free are
	// unsynchronized. XformSlotHigh is the high-water count (the per-frame table's resident region size).
	public int XformSlotHigh;
	private readonly System.Collections.Generic.Stack<int> _freeXformSlots = new();
	public int AllocXformSlot() => _freeXformSlots.Count > 0 ? _freeXformSlots.Pop() : XformSlotHigh++;
	public void FreeXformSlot(int slot) { if (slot >= 0) { _freeXformSlots.Push(slot); } }

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
		ResetUniformRing();
		GradSlab?.Reset();
		foreach (var kv in _clipBgSlabs) { kv.Value.Reset(); }
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


		// Free compiled draw-lists whose owning recording was disposed (their slab slices are reclaimed separately by
		// each slab's RetainOnly, since a disposed recording is never replayed → never marked live).
		// The slot free rides the Owned claim: when a rebuild already claimed the bag it also kept (reused) the
		// slot for the replacement entry, so freeing it here would alias two live recordings onto one slot.
		while (_pendingCompiled.TryDequeue(out var c)) { var claimed = DeferRelease(c.Owned); DeferRelease(c.StampOwned); if (claimed && c.XformSlot >= 0) { _freeXformSlots.Push(c.XformSlot); } }
	}

	public IntPtr TrackBg(IntPtr bg) { _pendingBindGroups.Add((nint)bg); return bg; }

	// Per-frame uniform ring. A gradient's uniform holds DEVICE-space geometry, so under a moving transform its
	// content differs every frame and a content-keyed cache can never hit — yet each gradient still needs its own
	// live buffer within the frame. Creating a buffer + bind group per gradient per frame is what made 500
	// gradients cost ~500 CreateBindGroup calls a frame. Allocate pairs ONCE and reuse them by index: the contents
	// are rewritten each frame (queue writes are ordered against submits, so overwriting is safe).
	private readonly List<(IntPtr Buf, IntPtr Bg)> _uniformRing = new();
	private int _uniformRingNext;

	public void ResetUniformRing() => _uniformRingNext = 0;

	public IntPtr RentRingUniform(nuint bytes, IntPtr layout, out IntPtr buf)
	{
		if (_uniformRingNext < _uniformRing.Count)
		{
			var e = _uniformRing[_uniformRingNext++];
			buf = e.Buf;
			return e.Bg;
		}
		var bd = new WGPUBufferDescriptor { Size = bytes, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
		buf = wgpuDeviceCreateBuffer(Dev, &bd);
		var entry = new WGPUBindGroupEntry { Binding = 0, Buffer = buf, Offset = 0, Size = bytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = layout, EntryCount = 1, Entries = &entry };
		var bg = wgpuDeviceCreateBindGroup(Dev, &bgd);
		_uniformRing.Add((buf, bg));
		_uniformRingNext++;
		return bg;
	}

	// Uploads the frame's transform table into the persistent storage buffer (grown 1.5× on demand) and returns a
	// bind group cached by buffer identity — rebuilt only when the buffer reallocates. Only the main on-window pass
	// calls this; nested/pooled passes rent transient buffers so concurrent in-frame writes never alias this one.
	public IntPtr EnsureXformBindGroup(System.Collections.Generic.List<float> xforms)
	{
		int count = xforms.Count;
		if (count == 0) { return IntPtr.Zero; }
		nuint needed = (nuint)(count * sizeof(float));
		if (_xformBuf == IntPtr.Zero || _xformCap < needed)
		{
			// Defer the outgrown buffer + its bind group to the next frame start (like the per-frame bind groups/
			// buffers) instead of releasing immediately: under pipelining the prior frame's submitted commands may
			// still bind them, so an immediate release could reclaim a resource the in-flight GPU work still reads.
			if (_xformBg != IntPtr.Zero) { _pendingBindGroups.Add((nint)_xformBg); _xformBg = IntPtr.Zero; }
			if (_xformBuf != IntPtr.Zero) { _pendingBuffers.Add((nint)_xformBuf); }
			nuint cap = (needed + (needed >> 1) + (nuint)3) & ~(nuint)3;
			var bd = new WGPUBufferDescriptor { Size = cap, Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst };
			_xformBuf = wgpuDeviceCreateBuffer(Dev, &bd);
			_xformCap = cap;
		}
		var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(xforms);
		fixed (float* p = span) { wgpuQueueWriteBuffer(Q, _xformBuf, 0, (IntPtr)p, needed); }
		if (_xformBg == IntPtr.Zero)
		{
			var e = new WGPUBindGroupEntry { Binding = 0, Buffer = _xformBuf, Offset = 0, Size = _xformCap };
			var bgd = new WGPUBindGroupDescriptor { Layout = XformBgl, EntryCount = 1, Entries = &e };
			_xformBg = wgpuDeviceCreateBindGroup(Dev, &bgd);
		}
		return _xformBg;
	}


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
		return true;
	}
	internal void DeferReleaseBuffer(nint buf) { if (buf != IntPtr.Zero) { _pendingBuffers.Add(buf); } }

	public IntPtr ImgBgl;
	public IntPtr GradBgl;
	// group(1) clip-uniform layouts (one per color-writing pipeline; all describe the same ClipU).
	public IntPtr SolidClipBgl;
	public IntPtr CoverClipBgl;
	public IntPtr ImageClipBgl;
	public IntPtr GradClipBgl;
	// Explicit SHARED ClipU layout: solid/cover/stencil use one pipeline layout so a single ClipU bind group binds to
	// all three (auto-derived layouts are pipeline-exclusive — that blocked arena'ing the path stencil+cover pair).
	public IntPtr ClipBgl;
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
	// 4x — the only count besides 1 the WebGPU spec guarantees for every format (lavapipe/CI reject 2x for
	// Bgra8Unorm). (1x/no-MSAA would need a separate no-resolve path — not wired.)
	// The host (WebGpuInitDevice) picks the MSAA sample count and bakes it here via the adopt ctor.
	public uint MsaaSamples { get; private set; } = 4;

	/// <summary>
	/// Masks rasterize at <see cref="MaskSuperSample"/>x linear resolution and are box-filtered down. 4x4
	/// supersampling yields 17 coverage levels where 4x MSAA yields 5, and Skia's mask rasterizer computes exact
	/// area — five levels is visibly chunky on small text. Supersampling also makes the baked coverage
	/// independent of how the frame itself is sampled, which is the point of caching it.
	/// </summary>
	public const int MaskSuperSample = 4;

	public IntPtr MaskStencilEvenOdd, MaskStencilNonZero, MaskCoverPipe, MaskDirectPipe;

	/// <summary>Box-filters the supersampled mask down into an atlas slot. No depth, no sampler (textureLoad).</summary>
	public IntPtr MaskDownsamplePipe, MaskDownsampleBgl;

	/// <summary>A single-sample render target in the DEVICE colour format, usable as a shader input.</summary>
	public IntPtr CreateMaskTarget(int w, int h)
	{
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		return wgpuTextureCreateView(tex, null);
	}

	// The color-attachment format the pipelines + offscreen targets use. Rgba8Unorm by default (the
	// offscreen/readback path assumes it); a swapchain renderer passes the surface's supported format.
	public readonly WGPUTextureFormat ColorFormat;
	public const WGPUTextureFormat DefaultColorFormat = WGPUTextureFormat.RGBA8Unorm;
	public const WGPUTextureFormat DepthStencilFormat = WGPUTextureFormat.Depth24PlusStencil8;

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
		MsaaSamples = ctx.SampleCount == 0 ? 4u : ctx.SampleCount;
		// The analytic AA ring REPLACES multisampling; running both antialiases each edge twice and spreads ink
		// half a pixel too far. It is emitted only when the attachment is single-sampled.
		WebGpuCommandRecorder.AnalyticAa = MsaaSamples == 1;
		FinishInit();
	}

	private void FinishInit()
	{
		CreatePipelines();   // bakes MsaaSamples (already set from the host context) into every pipeline
		DummyTex = CreateColorTarget(1, 1);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
		ClipSlab = new WebGpuClipSlab(this);
		GradSlab = new WebGpuUniformSlab(this, GradientUniformBytes);
		SolidSlab = new WebGpuSlab(this, 6);
		RrectSlab = new WebGpuSlab(this, 22);
		SolidTableSlab = new WebGpuSlab(this, 7);
		RrectTableSlab = new WebGpuSlab(this, 23);
		System.Console.WriteLine($"[webgpu] engine init — msaa={MsaaSamples}x colorFormat={ColorFormat}");
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
		if (PathAtlas.Pages.Count >= WebGpuPathAtlas.MaxPages) { return; }
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


	private void CreateMaskDownsamplePipeline()
	{
		var module = Module(MaskDownsampleWgsl);
		var e = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Fragment,
			Texture = new WGPUTextureBindingLayout { SampleType = WGPUTextureSampleType.Float, ViewDimension = WGPUTextureViewDimension._2D },
		};
		var bgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &e };
		MaskDownsampleBgl = wgpuDeviceCreateBindGroupLayout(Dev, &bgld);
		var bgl = MaskDownsampleBgl;
		var pld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 1, BindGroupLayouts = (IntPtr)(&bgl) };
		var layout = wgpuDeviceCreatePipelineLayout(Dev, &pld);

		var attrs = stackalloc WGPUVertexAttribute[2];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 };
		var vb = new WGPUVertexBufferLayout { ArrayStride = 16, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 2, Attributes = attrs };
		var vs = SV("vs"); var fs = SV("fs");
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vb };
		var ct = new WGPUColorTargetState { Format = ColorFormat, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &ct };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = layout,
		};
		MaskDownsamplePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}





	private IntPtr Module(string wgsl)
	{
		var code = SV(wgsl);
		var w = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var d = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&w };
		return wgpuDeviceCreateShaderModule(Dev, &d);
	}

	private static WGPUStencilFaceState Face(WGPUCompareFunction cmp, WGPUStencilOperation pass)
		=> new() { Compare = cmp, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = pass };

	// Explicit ClipU bind-group layout (one uniform at binding 0, read by vertex xformPos + fragment clipCov) wrapped
	// in a pipeline layout shared by solid/cover/stencil, so one ClipU bind group binds to all three.
	private IntPtr MakeClipPipeLayout()
	{
		var e = new WGPUBindGroupLayoutEntry
		{
			Binding = 0,
			Visibility = WGPUShaderStage.Vertex | WGPUShaderStage.Fragment,
			Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = 288 },
		};
		var bgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &e };
		ClipBgl = wgpuDeviceCreateBindGroupLayout(Dev, &bgld);
		var bgl = ClipBgl;
		var pld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 1, BindGroupLayouts = (IntPtr)(&bgl) };
		return wgpuDeviceCreatePipelineLayout(Dev, &pld);
	}

	private void CreatePipelines()
	{
		var colored = Module(ClipStructFn + ColoredWgsl);
		var posOnly = Module(ClipStructFn + PosOnlyWgsl);
		var vs = SV("vs");
		var fs = SV("fs");
		var clipLayout = MakeClipPipeLayout();

		var blend = new WGPUBlendState
		{
			Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.SrcAlpha, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
			Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.OneMinusSrcAlpha, Operation = WGPUBlendOperation.Add },
		};

		// Content pipelines depth-test GreaterEqual against the clip mask (content z=0: passes where mask depth<=0,
		// i.e. inside the clip or where no clip is active). The fan-stencil pipelines keep DepthCompare=Always
		// (winding is independent of the clip; the clip applies at the colour-writing cover/solid/image/gradient).
		var keep = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		SolidPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, keep, keep, 0x00, 0x00, WGPUCompareFunction.GreaterEqual, layout: clipLayout);
		StencilEvenOdd = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), 0xFF, 0xFF, layout: clipLayout);
		StencilNonZero = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.IncrementWrap), Face(WGPUCompareFunction.Always, WGPUStencilOperation.DecrementWrap), 0xFF, 0xFF, layout: clipLayout);
		CoverPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), 0xFF, 0xFF, WGPUCompareFunction.GreaterEqual, layout: clipLayout);
		// Coverage-mask baking runs at a FIXED sample rate, whatever the frame uses. The mask is rasterized once
		// and sampled forever after, so it must not inherit a single-sampled frame's aliasing -- that is the whole
		// reason the atlas exists. Skia does the same thing with a scanline rasterizer; multisampling is our
		// equivalent, and it is paid once per entry.
		MaskStencilEvenOdd = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), 0xFF, 0xFF, layout: clipLayout);
		MaskStencilNonZero = MakePipe(posOnly, vs, fs, colorWrite: false, colorAttrs: false, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.IncrementWrap), Face(WGPUCompareFunction.Always, WGPUStencilOperation.DecrementWrap), 0xFF, 0xFF, layout: clipLayout);
		MaskCoverPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), 0x00, 0xFF, WGPUCompareFunction.Always, layout: clipLayout);
		// Mask baking needs COVERAGE, not winding, so it does not use the stencil at all: overlapping fan triangles
		// are combined with a MAX blend, which saturates to 1 inside the shape however they overlap. That sidesteps
		// stencil-then-cover entirely -- which cancels to zero for a tiling triangulation whose interior and AA ring
		// wind oppositely, and which never wrote stencil at all in this offscreen configuration.
		var maxBlend = new WGPUBlendState
		{
			Color = new WGPUBlendComponent { Operation = WGPUBlendOperation.Max, SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.One },
			Alpha = new WGPUBlendComponent { Operation = WGPUBlendOperation.Max, SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.One },
		};
		MaskDirectPipe = MakePipe(colored, vs, fs, colorWrite: true, colorAttrs: true, &maxBlend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), 0x00, 0xFF, WGPUCompareFunction.Always, layout: clipLayout);
		// All three share the one explicit ClipU layout, so a bind group made with ClipBgl binds to any of them.
		SolidClipBgl = ClipBgl;
		CoverClipBgl = ClipBgl;
		CreatePathTablePipelines(&blend);
		CreateClipDepthPipelines();
		CreateImagePipeline();
		CreateMaskDownsamplePipeline();
		CreateGradientPipeline(&blend);
		CreateRoundedRectPipeline(&blend);
		CreateBlurPipeline();
		CreateCompositePipelines();
	}


	private void CreateClipDepthPipelines()
	{
		var module = Module(ClipDepthWgsl);
		var vs0 = SV("vs0"); var vs1 = SV("vs1"); var fs = SV("fs");
		var setFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);                 // clear region, don't touch stencil
		var coverFace = Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero);              // write where stencil != 0, reset it
		ClipDepthSet0 = MakeClipDepthPipe(module, vs0, fs, setFace, 0x00, 0x00);
		ClipDepthSet1 = MakeClipDepthPipe(module, vs1, fs, setFace, 0x00, 0x00);
		ClipDepthCover0 = MakeClipDepthPipe(module, vs0, fs, coverFace, 0xFF, 0xFF);
		ClipDepthCover1 = MakeClipDepthPipe(module, vs1, fs, coverFace, 0xFF, 0xFF);
	}

	// A vertex-buffer-less fullscreen pipeline that writes only depth (colour masked) with the given stencil face.
	private IntPtr MakeClipDepthPipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, WGPUStencilFaceState face, uint stencilWrite, uint stencilRead)
	{
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = null, WriteMask = 0 };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var ds = new WGPUDepthStencilState
		{
			Format = DepthStencilFormat,
			DepthWriteEnabled = WGPUOptionalBool.True,
			DepthCompare = WGPUCompareFunction.Always,
			StencilFront = face,
			StencilBack = face,
			StencilReadMask = stencilRead,
			StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 },
			Fragment = &fsState,
			DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}






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

		// Two-texture blend (effect graph): the fragment emits the fully-composited pixel, so the pipeline REPLACES.
		var blendModule = Module(CompositeBlendWgsl);
		var replace = new WGPUBlendState { Color = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.Zero, Operation = WGPUBlendOperation.Add }, Alpha = new WGPUBlendComponent { SrcFactor = WGPUBlendFactor.One, DstFactor = WGPUBlendFactor.Zero, Operation = WGPUBlendOperation.Add } };
		CompositeBlend = MakeComposite(blendModule, vs, fs, &replace, &ds);
		CompositeBlendBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeBlend, 0);

		var combineModule = Module(EffectCombineWgsl);
		EffectCombine = MakeComposite(combineModule, vs, fs, &replace, &ds);
		EffectCombineBgl = wgpuRenderPipelineGetBindGroupLayout(EffectCombine, 0);

		var colorFuncModule = Module(ColorFuncWgsl);
		ColorFunc = MakeComposite(colorFuncModule, vs, fs, &replace, &ds);
		ColorFuncBgl = wgpuRenderPipelineGetBindGroupLayout(ColorFunc, 0);

		var noiseModule = Module(EffectNoiseWgsl);
		EffectNoise = MakeComposite(noiseModule, vs, fs, &replace, &ds);
		EffectNoiseBgl = wgpuRenderPipelineGetBindGroupLayout(EffectNoise, 0);
	}

	private IntPtr MakeComposite(IntPtr module, WGPUStringView vs, WGPUStringView fs, WGPUBlendState* blend, WGPUDepthStencilState* ds)
	{
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
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
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.GreaterEqual, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = IntPtr.Zero };
		RrPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		RrClipBgl = wgpuRenderPipelineGetBindGroupLayout(RrPipe, 0);
	}

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
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.GreaterEqual, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = IntPtr.Zero };
		GradientPipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		GradBgl = wgpuRenderPipelineGetBindGroupLayout(GradientPipe, 0);
		GradClipBgl = wgpuRenderPipelineGetBindGroupLayout(GradientPipe, 1);
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
		var groups = stackalloc IntPtr[2];
		groups[0] = ImgBgl;
		groups[1] = ClipBgl;
		var pld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 2, BindGroupLayouts = (IntPtr)groups };
		return wgpuDeviceCreatePipelineLayout(Dev, &pld);
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
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.GreaterEqual, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = MakeImagePipeLayout() };
		ImagePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		ImageClipBgl = ClipBgl;   // shared, so a stamped clip group binds here too
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Nearest, MaxAnisotropy = 1 };
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

	private IntPtr MakePipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, bool colorWrite, bool colorAttrs, WGPUBlendState* blend, WGPUStencilFaceState front, WGPUStencilFaceState back, uint stencilWrite, uint stencilRead, WGPUCompareFunction depthCompare = WGPUCompareFunction.Always, bool depthWrite = false, IntPtr layout = default, uint samples = 0)
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
			Format = DepthStencilFormat,
			DepthWriteEnabled = depthWrite ? WGPUOptionalBool.True : WGPUOptionalBool.False,
			DepthCompare = depthCompare,
			StencilFront = front,
			StencilBack = back,
			StencilReadMask = stencilRead,
			StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = samples == 0 ? MsaaSamples : samples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = layout,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	// Transform-table path-fill pipelines: device-space verts + a per-vertex Uint32 slot index (last attribute).
	// Auto-layout (Layout=0) so wgpu derives group 0 (storage table) and, for cover, group 1 (ClipU) from the WGSL.
	private void CreatePathTablePipelines(WGPUBlendState* blend)
	{
		// Explicit BGLs so the cover's group 1 IS the shared ClipBgl — existing ClipU bind groups (immediate + the
		// arena re-stamp) bind to the table cover unchanged. Group 0 = the read-only storage transform table.
		var se = new WGPUBindGroupLayoutEntry { Binding = 0, Visibility = WGPUShaderStage.Vertex, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.ReadOnlyStorage } };
		var sbgld = new WGPUBindGroupLayoutDescriptor { EntryCount = 1, Entries = &se };
		XformBgl = wgpuDeviceCreateBindGroupLayout(Dev, &sbgld);
		CoverTableClipBgl = ClipBgl;
		var stencilBgls = stackalloc IntPtr[1] { XformBgl };
		var stencilPld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 1, BindGroupLayouts = (IntPtr)stencilBgls };
		var stencilLayout = wgpuDeviceCreatePipelineLayout(Dev, &stencilPld);
		var coverBgls = stackalloc IntPtr[2] { XformBgl, ClipBgl };
		var coverPld = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = 2, BindGroupLayouts = (IntPtr)coverBgls };
		var coverLayout = wgpuDeviceCreatePipelineLayout(Dev, &coverPld);

		var stencilMod = Module(StencilTableWgsl);
		var coverMod = Module(ClipStructFn + CoverTableWgsl);
		var vs = SV("vs"); var fs = SV("fs");
		StencilTableEO = MakeTablePipe(stencilMod, vs, fs, colorWrite: false, colorAttrs: false, blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Invert), 0xFF, 0xFF, WGPUCompareFunction.Always, stencilLayout);
		StencilTableNZ = MakeTablePipe(stencilMod, vs, fs, colorWrite: false, colorAttrs: false, blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.IncrementWrap), Face(WGPUCompareFunction.Always, WGPUStencilOperation.DecrementWrap), 0xFF, 0xFF, WGPUCompareFunction.Always, stencilLayout);
		CoverTablePipe = MakeTablePipe(coverMod, vs, fs, colorWrite: true, colorAttrs: true, blend, Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), Face(WGPUCompareFunction.NotEqual, WGPUStencilOperation.Zero), 0xFF, 0xFF, WGPUCompareFunction.GreaterEqual, coverLayout);
		// Same shader/vertex layout, but stencil Always + no stencil writes: the tiling-fan path has no stencil
		// pass, so the masked variant would test against 0 and discard every fragment.
		CoverTableDirectPipe = MakeTablePipe(coverMod, vs, fs, colorWrite: true, colorAttrs: true, blend, Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep), 0x00, 0xFF, WGPUCompareFunction.GreaterEqual, coverLayout);
		// Solid transform-table pipe: the cover shader (pos+col+slot) drawn unconditionally under the clip depth (no
		// stencil), so coalesced solids from moving recordings position per-vertex via their own slot.
		var keep = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		SolidTablePipe = MakeTablePipe(coverMod, vs, fs, colorWrite: true, colorAttrs: true, blend, keep, keep, 0x00, 0x00, WGPUCompareFunction.GreaterEqual, coverLayout);
		CreateRrectTablePipeline(blend, coverLayout);
	}

	// Transform-table rounded-rect pipeline: 8 float attrs (cpos/p/hf/radii/col/ihalf/icenter/iradii) + a trailing
	// Uint32 slot. Explicit layout group0 = the storage transform table, group1 = the shared ClipU (ClipBgl).
	private void CreateRrectTablePipeline(WGPUBlendState* blend, IntPtr layout)
	{
		var module = Module(ClipStructFn + RoundedRectTableWgsl);
		var vs = SV("vs"); var fs = SV("fs");
		var attrs = stackalloc WGPUVertexAttribute[9]
		{
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 },   // cpos (LOCAL device)
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 8, ShaderLocation = 1 },   // p (local centred)
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 16, ShaderLocation = 2 },  // hf
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 24, ShaderLocation = 3 },  // radii
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 40, ShaderLocation = 4 },  // col
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 56, ShaderLocation = 5 },  // ihalf
			new() { Format = WGPUVertexFormat.Float32x2, Offset = 64, ShaderLocation = 6 },  // icenter
			new() { Format = WGPUVertexFormat.Float32x4, Offset = 72, ShaderLocation = 7 },  // iradii
			new() { Format = WGPUVertexFormat.Uint32, Offset = 88, ShaderLocation = 8 },     // slot index
		};
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 92, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 9, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var keepFace = Face(WGPUCompareFunction.Always, WGPUStencilOperation.Keep);
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.GreaterEqual, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = layout };
		RrTablePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	private IntPtr MakeTablePipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, bool colorWrite, bool colorAttrs, WGPUBlendState* blend, WGPUStencilFaceState front, WGPUStencilFaceState back, uint stencilWrite, uint stencilRead, WGPUCompareFunction depthCompare, IntPtr layout)
	{
		var attrs = stackalloc WGPUVertexAttribute[3];
		attrs[0] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x2, Offset = 0, ShaderLocation = 0 };
		ulong stride; uint attrCount;
		if (colorAttrs)
		{
			attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x4, Offset = 8, ShaderLocation = 1 };
			attrs[2] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Uint32, Offset = 24, ShaderLocation = 2 };
			stride = 28; attrCount = 3;
		}
		else
		{
			attrs[1] = new WGPUVertexAttribute { Format = WGPUVertexFormat.Uint32, Offset = 8, ShaderLocation = 1 };
			stride = 12; attrCount = 2;
		}
		var vbl = new WGPUVertexBufferLayout { ArrayStride = stride, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = attrCount, Attributes = attrs };
		var vsState = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 1, Buffers = &vbl };
		var target = new WGPUColorTargetState { Format = ColorFormat, Blend = blend, WriteMask = colorWrite ? WGPUColorWriteMask.All : 0 };
		var fsState = new WGPUFragmentState { Module = module, EntryPoint = fs, TargetCount = 1, Targets = &target };
		var ds = new WGPUDepthStencilState
		{
			Format = DepthStencilFormat,
			DepthWriteEnabled = WGPUOptionalBool.False,
			DepthCompare = depthCompare,
			StencilFront = front,
			StencilBack = back,
			StencilReadMask = stencilRead,
			StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState,
			Fragment = &fsState,
			DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
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
		if (_xformBg != IntPtr.Zero) { wgpuBindGroupRelease(_xformBg); _xformBg = IntPtr.Zero; }
		if (_xformBuf != IntPtr.Zero) { wgpuBufferRelease(_xformBuf); _xformBuf = IntPtr.Zero; }
	}
}
