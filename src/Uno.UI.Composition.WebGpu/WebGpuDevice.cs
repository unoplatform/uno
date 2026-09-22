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
	public IntPtr RampTex, RampView;        // one row per gradient: its colour ramp sampled by t (see RampRow)
	public IntPtr DummyClipMore;            // one-entry placeholder for the clip overflow binding when a draw has four clips or fewer
	public WebGpuTexturePool Pool;                // transient offscreen pool (reused across frames)
	public WebGpuBufferPool BufferPool;           // transient vertex/uniform buffer pool (reused across frames)
	public WebGpuClipSlab ClipSlab;               // size-classed storage slab backing every owned/restamped ClipU
	public WebGpuSiteSlab SiteSlab;
	// One device scissor box per site slot. A site's ops all share it, so a move writes it once instead of
	// rewriting a box into every op -- which is what lets a moved recording reuse its op list untouched. It lives
	// here, not on the frame: a site slot outlives any one frame, and a stamp that HITS never rewrites its box.
	private Vector4[] _siteScissor = new Vector4[256];

	public void SetSiteScissor(nint slot, Vector4 box)
	{
		if (slot >= _siteScissor.Length) { Array.Resize(ref _siteScissor, Math.Max((int)slot + 1, _siteScissor.Length * 2)); }
		_siteScissor[slot] = box;
	}

	public Vector4 SiteScissor(nint slot) => slot < _siteScissor.Length ? _siteScissor[slot] : ClipData.None.Aabb;
	private IntPtr _identitySiteBg;

	/// <summary>The site group for ops whose geometry is already in device space: an identity placement.</summary>
	public IntPtr IdentitySiteBg
	{
		get
		{
			if (_identitySiteBg == IntPtr.Zero)
			{
				var slot = SiteSlab.Alloc();
				// The WHOLE block, not just the placement: slots are recycled, so anything left unwritten here is
				// the clip of whatever site held this slot before - which would clip away every device-space op.
				var u = SiteSlab.SlotSpan(slot);
				u.Clear();
				u[0] = 1f; u[3] = 1f;                                             // identity placement
				u[10] = -1e9f; u[11] = -1e9f; u[14] = 1e9f; u[15] = 1e9f;         // no site aabb...
				u[16] = -1e30f; u[17] = -1e30f; u[18] = 1e30f; u[19] = 1e30f;     // ...so everything is inside it
				// Depth 1 = nearest, so a device-space op is never rejected by the occlusion prepass. It never
				// occludes either: its DrawOp.Depth stays 0, which keeps it out of the prepass entirely.
				u[12] = 1f;
				var e = new WGPUBindGroupEntry { Binding = 0, Buffer = SiteSlab.BufferOf(slot), Offset = SiteSlab.OffsetOf(slot), Size = WebGpuFrame.SiteUBytes };
				var d = new WGPUBindGroupDescriptor { Layout = SiteBgl, EntryCount = 1, Entries = &e };
				_identitySiteBg = wgpuDeviceCreateBindGroup(Dev, &d);
			}
			return _identitySiteBg;
		}
	}
	public WebGpuUniformSlab GradSlab;            // per-frame gradient uniforms, one queue write per chunk

	// Per-frame ClipU slabs for IMMEDIATE ops, one per (bind-group layout, byte size): a slot's bind group is created
	// once and reused, so it must always be built with the same layout and bind the same size.
	private WebGpuUniformSlab _clipBgSlab;

	public WebGpuUniformSlab ClipBgSlab => _clipBgSlab ??= new WebGpuUniformSlab(this, WebGpuFrame.ClipUBytes, DummyTex, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst, DummyTex, Smp, DummyClipMore);

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
	// Offscreen colours on their way back to the recycling pool. Deferred like a release, for the same reason: a
	// recording built this frame may still reference the view.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(nint view, nint tex, int w, int h)> _pendingRecycle = new();
	internal readonly WebGpuOffscreenPool OffscreenPool = new();
	internal void DeferTextureRecycle(IntPtr view, IntPtr tex, int w, int h) => _pendingRecycle.Enqueue(((nint)view, (nint)tex, w, h));
	// Per-recording compiled GPU draw-list. It lives ON the recording's WebGpuRenderRecord (IRenderRecord is, by its own
	// contract, "backend-defined retained state"), built once and replayed cheaply — no global cache, no per-frame
	// eviction scan. When the owning IRenderRecord is disposed (UI thread, on a content change), its compiled state is
	// enqueued here and freed on the render thread at the next BeginFrameResources (concurrent, like _pendingTextures).
	// Decoupled from the renderer's WebGpuGeometryCache: the device only needs the GPU resources to free (two
	// OwnedResources bags) + the transform-table slot to reclaim, so the queue carries those primitives — keeping
	// WebGpuDevice (init tier) free of any renderer type.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(OwnedResources Owned, OwnedResources StampOwned)> _pendingCompiled = new();
	internal void DeferCompiledRelease(OwnedResources owned, OwnedResources stampOwned) => _pendingCompiled.Enqueue((owned, stampOwned));

	// A stamp's site uniform slot and the bind group over it live as long as the stamp, not the frame, so they sit
	// outside the per-frame tracking — which means the stamp's death is the only place they can be reclaimed.
	// Primitives rather than the renderer's stamp type, to keep this tier free of renderer types.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(nint Bg, nint SiteSlot)> _pendingSites = new();
	internal void DeferSiteRelease(nint siteBg, nint siteSlot)
	{
		if (siteBg != 0 || siteSlot != 0) { _pendingSites.Enqueue((siteBg, siteSlot)); }
	}

	// Per-frame bind groups reference the frame's pooled buffers, so they're released at the next frame start once
	// the previous frame's GPU work has completed (present DevicePolls). A cached recording's persistent resources
	// released mid-frame (cache miss) are deferred the same way, since ops already emitted this frame still use
	// them. Pooled buffers/textures are reused (not released). Call once per frame before rebuilding.
	// Monotonic per-session-frame counter: lets stamp memos detect "already stamped under the current submit",
	// where an in-place uniform rewrite would clobber data this frame's earlier draws still reference.
	public long FrameSeq;

	// Blurred shadows for rounded-rect silhouettes, keyed by shape so a wall of identical cards shares one. Lives
	// on the device because a WebGpuFrame -- and its WebGpuEffects -- is constructed per frame.
	internal readonly Dictionary<WebGpuEffects.ShapeKey, WebGpuEffects.ShapeShadow> ShapeShadows = new();
	internal readonly List<WebGpuEffects.ShapeKey> ShapeShadowQueue = new();
	internal readonly List<WebGpuEffects.ShapeKey> ShapeShadowStale = new();

	// Sends a rounded-rect layer shadow back through the sheet: replayed and blurred per card per frame.
	internal static readonly bool NoShapeShadow = Environment.GetEnvironmentVariable("UNO_WEBGPU_NO_SHAPE_SHADOW") == "1";

	// Escape hatch for the occlusion prepass: it changes what reaches the rasteriser, so a driver that gets the
	// depth comparison wrong can be told apart from a bug in the geometry without a rebuild.
	internal static readonly bool NoDepthOcclusion = Environment.GetEnvironmentVariable("UNO_WEBGPU_NO_DEPTH_OCCLUSION") == "1";

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
		while (_pendingRecycle.TryDequeue(out var rc)) { OffscreenPool.Return((IntPtr)rc.tex, (IntPtr)rc.view, rc.w, rc.h); }
		OffscreenPool.BeginFrame();
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

		// ... and the site slots/bind groups of the stamps that went with them.
		while (_pendingSites.TryDequeue(out var s))
		{
			if (s.Bg != 0) { wgpuBindGroupRelease(s.Bg); }
			if (s.SiteSlot != 0) { SiteSlab.Free(s.SiteSlot); }
		}
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
	public IntPtr SiteBgl;
	private IntPtr _emptyBgl;
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
	public const int GradRampBase = GradOriginBase + 4;                     // scale, bias (vec4 each) per interval of a <= 4-stop gradient
	public const int GradientUniformBytes = (GradRampBase + 8 * 4) * 4;

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
		var moreDesc = new WGPUBufferDescriptor { Size = WebGpuFrame.ClipEntryBytes, Usage = WGPUBufferUsage.Storage };
		DummyClipMore = wgpuDeviceCreateBuffer(Dev, &moreDesc);
		var rampDesc = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = RampWidth, Height = RampRows, DepthOrArrayLayers = 1 },
			Format = WGPUTextureFormat.RGBA8Unorm,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst,
		};
		RampTex = wgpuDeviceCreateTexture(Dev, &rampDesc);
		RampView = wgpuTextureCreateView(RampTex, null);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
		ClipSlab = new WebGpuClipSlab(this);
		SiteSlab = new WebGpuSiteSlab(this);
		GradSlab = new WebGpuUniformSlab(this, GradientUniformBytes, RampView, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst, default, Smp);
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
		wgpuBufferRelease(buf);
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

	// Destroy frees the allocation now; without the Release the handle (and wgpu's accounting of it) stays for the
	// life of the device.
	public void DestroyBuffer(IntPtr buf) { wgpuBufferDestroy(buf); wgpuBufferRelease(buf); }

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

	private IntPtr Module(string wgsl)
	{
		var code = SV(wgsl);
		var w = new WGPUShaderSourceWGSL { Chain = new WGPUChainedStruct { SType = WGPUSType.ShaderSourceWGSL }, Code = code };
		var d = new WGPUShaderModuleDescriptor { NextInChain = (WGPUChainedStruct*)&w };
		return wgpuDeviceCreateShaderModule(Dev, &d);
	}

	// The ClipU group every colour pipeline shares: the uniform at 0, the path-clip mask at 1, the coverage mask at 2,
	// their sampler at 3 and the clip entries past the uniform's four at 4.
	private IntPtr MakeClipPipeLayout()
	{
		const WGPUShaderStage vf = WGPUShaderStage.Vertex | WGPUShaderStage.Fragment;
		ClipBgl = Bgl(UniformEntry(0, vf, WebGpuFrame.ClipUBytes), TextureEntry(1, WGPUTextureSampleType.Float), TextureEntry(2, WGPUTextureSampleType.Float), SamplerEntry(3), StorageEntry(4, vf, WebGpuFrame.ClipEntryBytes));
		PassBgl = Bgl(UniformEntry(0, WGPUShaderStage.Vertex, 16));
		SiteBgl = Bgl(UniformEntry(0, vf, WebGpuFrame.SiteUBytes));   // the vertex shader places, the fragment clips
		_emptyBgl = Bgl();
		return ColourLayout(ClipBgl, _emptyBgl, SiteBgl);
	}

	private void CreatePipelines()
	{
		var clipLayout = MakeClipPipeLayout();
		var straight = Blend(WGPUBlendFactor.SrcAlpha, WGPUBlendFactor.OneMinusSrcAlpha, WGPUBlendFactor.One, WGPUBlendFactor.OneMinusSrcAlpha);   // straight-alpha vertex colours
		var over = Blend(WGPUBlendFactor.One, WGPUBlendFactor.OneMinusSrcAlpha);   // premultiplied textures
		var replace = Blend(WGPUBlendFactor.One, WGPUBlendFactor.Zero);
		var dstIn = Blend(WGPUBlendFactor.Zero, WGPUBlendFactor.SrcAlpha);        // the destination keeps only where the texture has alpha
		var add = Blend(WGPUBlendFactor.One, WGPUBlendFactor.One);
		const WGPUVertexFormat F2 = WGPUVertexFormat.Float32x2, F4 = WGPUVertexFormat.Float32x4;

		// Each colour pipeline gets a twin carrying depth state: a pipeline may only run in a pass whose attachments
		// match it, and only the window pass has a depth buffer. The modules and layouts are shared.
		var solidMod = Module(ClipStructFn + ColoredWgsl);
		SolidPipe = Pipeline(solidMod, clipLayout, &straight, ColorFormat, F2, F4, F2);   // pos, colour, coverage uv
		SolidPipeD = DepthPipeline(solidMod, clipLayout, &straight, ColorFormat, F2, F4, F2);
		var rrMod = Module(ClipStructFn + RoundedRectWgsl);
		RrPipe = Pipeline(rrMod, clipLayout, &straight, ColorFormat, F2, F2, F2, F4, F4, F2, F2, F4);   // corner, local p, half size, radii, colour, inner half, inner centre, inner radii
		RrPipeD = DepthPipeline(rrMod, clipLayout, &straight, ColorFormat, F2, F2, F2, F4, F4, F2, F2, F4);
		GradBgl = Bgl(UniformEntry(0, WGPUShaderStage.Fragment, GradientUniformBytes), TextureEntry(1, WGPUTextureSampleType.Float), SamplerEntry(3));
		var gradMod = Module(ClipStructFn + GradientWgsl);
		var gradLayout = ColourLayout(GradBgl, ClipBgl, SiteBgl);
		GradientPipe = Pipeline(gradMod, gradLayout, &straight, ColorFormat, F2, F2);
		GradientPipeD = DepthPipeline(gradMod, gradLayout, &straight, ColorFormat, F2, F2);
		ImgBgl = Bgl(TextureEntry(0, WGPUTextureSampleType.Float), SamplerEntry(1), UniformEntry(2, WGPUShaderStage.Fragment, ImageUniformBytes));
		var image = Module(ClipStructFn + ImageWgsl);
		var imageLayout = ColourLayout(ImgBgl, ClipBgl, SiteBgl);
		ImagePipe = Pipeline(image, imageLayout, &over, ColorFormat, F2, F2);
		ImagePipeD = DepthPipeline(image, imageLayout, &over, ColorFormat, F2, F2);
		ImageDstInPipe = Pipeline(image, imageLayout, &dstIn, ColorFormat, F2, F2);
		ImageDstInPipeD = DepthPipeline(image, imageLayout, &dstIn, ColorFormat, F2, F2);
		DepthPrepassPipe = DepthOnlyPipeline(Module(DepthPrepassWgsl), PassOnlyLayout(), ColorFormat);

		// Coverage bakes: signed area accumulates additively into a float sheet, which then resolves to alpha.
		CoverageSheetBgl = Bgl(StorageEntry(0, WGPUShaderStage.Vertex), StorageEntry(1, WGPUShaderStage.Vertex), UniformEntry(2, WGPUShaderStage.Vertex, 16));
		CoverageSheetPipe = Pipeline(Module(CoverageSheetWgsl), Layout(CoverageSheetBgl), &add, CoverageFormat);
		CoverageResolveSheetBgl = Bgl(TextureEntry(0, WGPUTextureSampleType.UnfilterableFloat));
		CoverageResolveSheetPipe = Pipeline(Module(CoverageResolveSheetWgsl), Layout(CoverageResolveSheetBgl), null, ColorFormat, F2, F2, F2);

		// Fullscreen effect passes take their bind group layout from the shader. The effect-graph stages emit the
		// composited pixel, so they replace.
		BlurPipe = Pipeline(Module(BlurWgsl), IntPtr.Zero, null, DefaultColorFormat);
		BlurBgl = wgpuRenderPipelineGetBindGroupLayout(BlurPipe, 0);
		CompositeSrcOver = Pipeline(Module(CompositeWgsl), IntPtr.Zero, &over, ColorFormat);
		CompositeBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeSrcOver, 0);
		CompositeBlend = Pipeline(Module(CompositeBlendWgsl), IntPtr.Zero, &replace, ColorFormat);
		CompositeBlendBgl = wgpuRenderPipelineGetBindGroupLayout(CompositeBlend, 0);
		EffectCombine = Pipeline(Module(EffectCombineWgsl), IntPtr.Zero, &replace, ColorFormat);
		EffectCombineBgl = wgpuRenderPipelineGetBindGroupLayout(EffectCombine, 0);
		ColorFunc = Pipeline(Module(ColorFuncWgsl), IntPtr.Zero, &replace, ColorFormat);
		ColorFuncBgl = wgpuRenderPipelineGetBindGroupLayout(ColorFunc, 0);
		EffectNoise = Pipeline(Module(EffectNoiseWgsl), IntPtr.Zero, &replace, ColorFormat);
		EffectNoiseBgl = wgpuRenderPipelineGetBindGroupLayout(EffectNoise, 0);

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

	// Gradient colour ramps: one 256-texel row per distinct gradient, so the fragment shader reads its colour with
	// one filtered fetch instead of walking the stops. Measured on a UHD 620: stripping the stop walk entirely took
	// OverlayStack 52.8 -> 34.4 ms and RadialGlow 32.0 -> 17.0, i.e. that walk was a third to a half of the frame.
	private const uint RampWidth = 256, RampRows = 256;
	private readonly Dictionary<long, int> _rampRows = new();
	private int _rampNext;

	/// <summary>
	/// The ramp row for a gradient uniform, as the v coordinate to sample at, or -1 to keep the per-stop path.
	/// Refused when two stops sit closer than a texel, because the ramp would smear a hard colour switch (a focused
	/// TextBox puts two stops at the same offset to get an accent underline), and once the table is full.
	/// </summary>
	public float RampRow(float[] u, int count)
	{
		if (count < 2 || count > MaxGradientStops) { return -1f; }
		long key = count;
		for (var i = 0; i < count; i++)
		{
			key = key * 31 + BitConverter.SingleToInt32Bits(u[GradStopsBase + i]);
			for (var ch = 0; ch < 4; ch++) { key = key * 31 + BitConverter.SingleToInt32Bits(u[GradColorsBase + i * 4 + ch]); }
		}
		if (_rampRows.TryGetValue(key, out var row)) { return (row + 0.5f) / RampRows; }
		for (var i = 1; i < count; i++)
		{
			if (u[GradStopsBase + i] - u[GradStopsBase + i - 1] < 1f / RampWidth) { return -1f; }
		}
		if (_rampNext >= RampRows) { return -1f; }
		row = _rampNext++;
		var px = new byte[RampWidth * 4];
		int seg = 0;
		for (var x = 0; x < RampWidth; x++)
		{
			float t = (x + 0.5f) / RampWidth;
			while (seg < count - 2 && t > u[GradStopsBase + seg + 1]) { seg++; }
			float s0 = u[GradStopsBase + seg], s1 = u[GradStopsBase + seg + 1];
			float f = t <= s0 ? 0f : t >= s1 ? 1f : (t - s0) / (s1 - s0);
			for (var ch = 0; ch < 4; ch++)
			{
				float c = u[GradColorsBase + seg * 4 + ch] + (u[GradColorsBase + (seg + 1) * 4 + ch] - u[GradColorsBase + seg * 4 + ch]) * f;
				px[x * 4 + ch] = (byte)Math.Clamp((int)MathF.Round(c * 255f), 0, 255);
			}
		}
		var dst = new WGPUTexelCopyTextureInfo { Texture = RampTex, Aspect = WGPUTextureAspect.All, MipLevel = 0, Origin = new WGPUOrigin3D { X = 0, Y = (uint)row, Z = 0 } };
		var layout = new WGPUTexelCopyBufferLayout { Offset = 0, BytesPerRow = RampWidth * 4, RowsPerImage = 1 };
		var ext = new WGPUExtent3D { Width = RampWidth, Height = 1, DepthOrArrayLayers = 1 };
		fixed (byte* p = px) { wgpuQueueWriteTexture(Q, &dst, (IntPtr)p, (nuint)px.Length, &layout, &ext); }
		_rampRows[key] = row;
		return (row + 0.5f) / RampRows;
	}

	/// <summary>Single-channel float so accumulation can exceed 1 and go negative; blendable, unlike r32float.</summary>
	public const WGPUTextureFormat CoverageFormat = WGPUTextureFormat.R16Float;
	public IntPtr CoverageSheetPipe, CoverageSheetBgl, CoverageResolveSheetPipe, CoverageResolveSheetBgl;
	/// <summary>The colour pipelines again, with depth state, for the one pass that carries a depth buffer.</summary>
	public IntPtr SolidPipeD, RrPipeD, GradientPipeD, ImagePipeD, ImageDstInPipeD;
	/// <summary>Writes the occlusion depth buffer and nothing else.</summary>
	public IntPtr DepthPrepassPipe;

	private static readonly WGPUStringView VsEntry = SV("vs"), FsEntry = SV("fs");

	// One render pipeline: a fullscreen triangle when it has no vertex attributes, else one vertex buffer of the
	// attributes packed in order.
	/// <summary>The occlusion depth buffer's format. Depth only -- nothing here uses stencil.</summary>
	public const WGPUTextureFormat DepthFormat = WGPUTextureFormat.Depth24Plus;

	// A colour pipeline that reads the occlusion depth buffer: it never writes depth (the prepass does that), and
	// keeps a fragment only where its site is at or in front of whatever opaque site last covered that pixel.
	private IntPtr DepthPipeline(IntPtr module, IntPtr layout, WGPUBlendState* blend, WGPUTextureFormat format, params ReadOnlySpan<WGPUVertexFormat> attrs)
	{
		var ds = new WGPUDepthStencilState
		{
			Format = DepthFormat,
			DepthWriteEnabled = WGPUOptionalBool.False,
			DepthCompare = WGPUCompareFunction.GreaterEqual,
			StencilFront = new WGPUStencilFaceState { Compare = WGPUCompareFunction.Always, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = WGPUStencilOperation.Keep },
			StencilBack = new WGPUStencilFaceState { Compare = WGPUCompareFunction.Always, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = WGPUStencilOperation.Keep },
			StencilReadMask = 0xFFFFFFFF,
			StencilWriteMask = 0xFFFFFFFF,
		};
		return Pipeline(module, layout, blend, format, &ds, attrs);
	}

	private IntPtr Pipeline(IntPtr module, IntPtr layout, WGPUBlendState* blend, WGPUTextureFormat format, params ReadOnlySpan<WGPUVertexFormat> attrs)
		=> Pipeline(module, layout, blend, format, null, attrs);

	private IntPtr Pipeline(IntPtr module, IntPtr layout, WGPUBlendState* blend, WGPUTextureFormat format, WGPUDepthStencilState* depth, ReadOnlySpan<WGPUVertexFormat> attrs)
	{
		var va = stackalloc WGPUVertexAttribute[Math.Max(1, attrs.Length)];
		ulong stride = 0;
		for (var i = 0; i < attrs.Length; i++)
		{
			va[i] = new WGPUVertexAttribute { Format = attrs[i], Offset = stride, ShaderLocation = (uint)i };
			stride += attrs[i] == WGPUVertexFormat.Float32x4 ? 16u : 8u;
		}
		var vbl = new WGPUVertexBufferLayout { ArrayStride = stride, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = (nuint)attrs.Length, Attributes = va };
		var target = new WGPUColorTargetState { Format = format, Blend = blend, WriteMask = WGPUColorWriteMask.All };
		var fs = new WGPUFragmentState { Module = module, EntryPoint = FsEntry, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = module, EntryPoint = VsEntry, BufferCount = attrs.Length == 0 ? 0u : 1u, Buffers = attrs.Length == 0 ? (WGPUVertexBufferLayout*)null : &vbl },
			Fragment = &fs,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = depth,
			Layout = layout,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
	}

	private static WGPUBlendState Blend(WGPUBlendFactor src, WGPUBlendFactor dst) => Blend(src, dst, src, dst);

	private static WGPUBlendState Blend(WGPUBlendFactor src, WGPUBlendFactor dst, WGPUBlendFactor alphaSrc, WGPUBlendFactor alphaDst)
		=> new()
		{
			Color = new WGPUBlendComponent { SrcFactor = src, DstFactor = dst, Operation = WGPUBlendOperation.Add },
			Alpha = new WGPUBlendComponent { SrcFactor = alphaSrc, DstFactor = alphaDst, Operation = WGPUBlendOperation.Add },
		};

	private static WGPUBindGroupLayoutEntry UniformEntry(uint binding, WGPUShaderStage visibility, int minSize)
		=> new() { Binding = binding, Visibility = visibility, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.Uniform, MinBindingSize = (ulong)minSize } };

	private static WGPUBindGroupLayoutEntry StorageEntry(uint binding, WGPUShaderStage visibility, int minSize = 0)
		=> new() { Binding = binding, Visibility = visibility, Buffer = new WGPUBufferBindingLayout { Type = WGPUBufferBindingType.ReadOnlyStorage, MinBindingSize = (ulong)minSize } };

	private static WGPUBindGroupLayoutEntry TextureEntry(uint binding, WGPUTextureSampleType sampleType)
		=> new() { Binding = binding, Visibility = WGPUShaderStage.Fragment, Texture = new WGPUTextureBindingLayout { SampleType = sampleType, ViewDimension = WGPUTextureViewDimension._2D } };

	private static WGPUBindGroupLayoutEntry SamplerEntry(uint binding)
		=> new() { Binding = binding, Visibility = WGPUShaderStage.Fragment, Sampler = new WGPUSamplerBindingLayout { Type = WGPUSamplerBindingType.Filtering } };

	private IntPtr Bgl(params ReadOnlySpan<WGPUBindGroupLayoutEntry> entries)
	{
		fixed (WGPUBindGroupLayoutEntry* p = entries)
		{
			var d = new WGPUBindGroupLayoutDescriptor { EntryCount = (nuint)entries.Length, Entries = p };
			return wgpuDeviceCreateBindGroupLayout(Dev, &d);
		}
	}

	private IntPtr Layout(params ReadOnlySpan<IntPtr> groups)
	{
		fixed (IntPtr* p = groups)
		{
			var d = new WGPUPipelineLayoutDescriptor { BindGroupLayoutCount = (nuint)groups.Length, BindGroupLayouts = (IntPtr)p };
			return wgpuDeviceCreatePipelineLayout(Dev, &d);
		}
	}

	// A colour pipeline's layout: the pass projection at group 0, then its own groups.
	private IntPtr ColourLayout(params ReadOnlySpan<IntPtr> groups) => Layout([PassBgl, .. groups]);

	// The prepass needs nothing but the pass projection.
	private IntPtr PassOnlyLayout() => Layout(PassBgl);

	// Depth-only: the fragment stage writes no colour, so the draw is a vertex per corner plus fixed-function
	// depth. GREATER + write leaves each pixel holding the depth of the LAST opaque site that covered it.
	private IntPtr DepthOnlyPipeline(IntPtr module, IntPtr layout, WGPUTextureFormat format)
	{
		var attr = new WGPUVertexAttribute { Format = WGPUVertexFormat.Float32x3, Offset = 0, ShaderLocation = 0 };
		var vbl = new WGPUVertexBufferLayout { ArrayStride = 12, StepMode = WGPUVertexStepMode.Vertex, AttributeCount = 1, Attributes = &attr };
		var ds = new WGPUDepthStencilState
		{
			Format = DepthFormat,
			DepthWriteEnabled = WGPUOptionalBool.True,
			DepthCompare = WGPUCompareFunction.Greater,
			StencilFront = new WGPUStencilFaceState { Compare = WGPUCompareFunction.Always, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = WGPUStencilOperation.Keep },
			StencilBack = new WGPUStencilFaceState { Compare = WGPUCompareFunction.Always, FailOp = WGPUStencilOperation.Keep, DepthFailOp = WGPUStencilOperation.Keep, PassOp = WGPUStencilOperation.Keep },
			StencilReadMask = 0xFFFFFFFF,
			StencilWriteMask = 0xFFFFFFFF,
		};
		var target = new WGPUColorTargetState { Format = format, Blend = null, WriteMask = WGPUColorWriteMask.None };
		var fs = new WGPUFragmentState { Module = module, EntryPoint = FsEntry, TargetCount = 1, Targets = &target };
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = module, EntryPoint = VsEntry, BufferCount = 1, Buffers = &vbl },
			Fragment = &fs,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = &ds,
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
