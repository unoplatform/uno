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
using Silk.NET.WebGPU;
using Buffer = Silk.NET.WebGPU.Buffer;
using Silk.NET.WebGPU.Extensions.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuDevice : IDisposable
{
	public WebGPU Api;
	public Wgpu Wgpu;
	public Instance* Inst;
	public Adapter* Adapter;
	public Device* Dev;
	public Queue* Q;
	public RenderPipeline* SolidPipe;
	public RenderPipeline* ImagePipe;
	public RenderPipeline* ImageDstInPipe;        // the image draw blended DstIn: a mask layer's composite
	public RenderPipeline* GradientPipe;
	public RenderPipeline* RrPipe;                // analytic rounded-rect / border-ring fill (per-vertex SDF quad)
	public RenderPipeline* BlurPipe;              // separable gaussian (fullscreen), single-sample
	public BindGroupLayout* BlurBgl;
	public RenderPipeline* CompositeSrcOver;      // fullscreen SrcOver of a texture: the effect evaluator's final draw
	public BindGroupLayout* CompositeBgl;         // its group(0) layout
	public RenderPipeline* CompositeBlend;       // two-texture blend: fg(0) over bg(3), full Porter-Duff + separable/non-separable
	public BindGroupLayout* CompositeBlendBgl;    // group(0): fg tex, sampler, uniform (params.z = mode id), bg tex
	public RenderPipeline* EffectCombine;        // two-texture linear combine (CrossFade/ArithmeticComposite) + AlphaMask
	public BindGroupLayout* EffectCombineBgl;
	public RenderPipeline* ColorFunc;            // single-input per-channel colour function (Contrast / GammaTransfer)
	public BindGroupLayout* ColorFuncBgl;
	public RenderPipeline* EffectNoise;          // procedural WhiteNoise generator (no input)
	public BindGroupLayout* EffectNoiseBgl;
	public RenderPipeline* ClearRectPipe;        // solid fill under a scissor: the damaged rect's clear on a partial repaint
	public BindGroupLayout* ClearRectBgl;
	public TextureView* DummyTex;                 // 1x1 placeholder for the clip coverage binding when no path clip
	public Texture* RampTex;
	public TextureView* RampView;       // one row per gradient: its colour ramp sampled by t (see RampRow)
	public Buffer* DummyClipMore;            // one-entry placeholder for the clip overflow binding when a draw has four clips or fewer
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
	private BindGroup* _identitySiteBg;

	/// <summary>The site group for ops whose geometry is already in device space: an identity placement.</summary>
	public BindGroup* IdentitySiteBg
	{
		get
		{
			if (_identitySiteBg == null)
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
				var e = new BindGroupEntry { Binding = 0, Buffer = SiteSlab.BufferOf(slot), Offset = SiteSlab.OffsetOf(slot), Size = WebGpuFrame.SiteUBytes };
				var d = new BindGroupDescriptor { Layout = SiteBgl, EntryCount = 1, Entries = &e };
				_identitySiteBg = Api.DeviceCreateBindGroup(Dev, &d);
			}
			return _identitySiteBg;
		}
	}
	public WebGpuUniformSlab GradSlab;            // per-frame gradient uniforms, one queue write per chunk

	// Per-frame ClipU slabs for IMMEDIATE ops, one per (bind-group layout, byte size): a slot's bind group is created
	// once and reused, so it must always be built with the same layout and bind the same size.
	private WebGpuUniformSlab _clipBgSlab;

	public WebGpuUniformSlab ClipBgSlab => _clipBgSlab ??= new WebGpuUniformSlab(this, WebGpuFrame.ClipUBytes, DummyTex, BufferUsage.Uniform | BufferUsage.CopyDst, DummyTex, Smp, DummyClipMore);

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
	internal readonly WebGpuOffscreenPool OffscreenPool;
	internal void DeferTextureRecycle(TextureView* view, Texture* tex, int w, int h) => _pendingRecycle.Enqueue(((nint)view, (nint)tex, w, h));
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
	internal void DeferSiteRelease(BindGroup* siteBg, nint siteSlot)
	{
		if (siteBg != null || siteSlot != 0) { _pendingSites.Enqueue(((nint)siteBg, siteSlot)); }
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

	// Submissions the GPU may not have finished yet, oldest first.
	private readonly Queue<ulong> _inFlight = new();

	/// <summary>How many frames the CPU may submit ahead of the GPU. A swapchain caps this for presented frames,
	/// but nothing does for frames that are never presented (offscreen renders, a window whose present is skipped):
	/// on a slow device those pile up with every resource they hold, faster than the GPU retires them.</summary>
	internal const int MaxFramesInFlight = 3;

	/// <summary>
	/// Submits a frame's commands. Natively the CPU runs ahead of the GPU by a few frames, no more: pooled-buffer
	/// reuse is queue-ordered and transient textures are refcount-released, so a frame's resources live until the GPU
	/// has finished it. The browser has neither submission indices nor a blocking poll, and paces frames itself.
	/// </summary>
	internal void SubmitFrame(CommandBuffer* commandBuffer)
	{
		if (OperatingSystem.IsBrowser())
		{
			Api.QueueSubmit(Q, 1, &commandBuffer);
			return;
		}

		ThrottleSubmission(Wgpu.QueueSubmitForIndex(Q, 1, &commandBuffer));
	}

	/// <summary>Records a frame's submission and blocks on the oldest one once more than
	/// <see cref="MaxFramesInFlight"/> are outstanding, then pumps the device without waiting.</summary>
	private void ThrottleSubmission(ulong submission)
	{
		_inFlight.Enqueue(submission);
		while (_inFlight.Count > MaxFramesInFlight)
		{
			var oldest = new WrappedSubmissionIndex { Queue = Q, SubmissionIndex = _inFlight.Dequeue() };
			_ = Wgpu.DevicePoll(Dev, true, &oldest);
		}

		_ = Wgpu.DevicePoll(Dev, false, null);
	}

	public void BeginFrameResources()
	{
		// Read LAST frame's timestamps here: the resolve/copy are recorded into the frame encoder, so mapping
		// before submit targets a buffer still pending in an unsubmitted command buffer and never completes.
		GradSlab?.Reset();
		_clipBgSlab?.Reset();
		FrameSeq++;
		Pool.BeginFrame();
		BufferPool.BeginFrame();
		foreach (var bg in _pendingBindGroups) { Api.BindGroupRelease((BindGroup*)bg); }
		foreach (var b in _pendingBuffers) { Api.BufferRelease((Buffer*)b); }
		// Release (refcount) rather than Destroy (immediate) the transient one-shot textures: wgpu then frees them
		// only once the GPU has finished the frames that used them — safe even when the per-frame drain is skipped.
		while (_pendingTextures.TryDequeue(out var t)) { if (t.view != 0) { Api.TextureViewRelease((TextureView*)t.view); } if (t.tex != 0) { Api.TextureRelease((Texture*)t.tex); } }
		while (_pendingRecycle.TryDequeue(out var rc)) { OffscreenPool.Return((Texture*)rc.tex, (TextureView*)rc.view, rc.w, rc.h); }
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
			if (s.Bg != 0) { Api.BindGroupRelease((BindGroup*)s.Bg); }
			if (s.SiteSlot != 0) { SiteSlab.Free(s.SiteSlot); }
		}
	}

	public BindGroup* TrackBg(BindGroup* bg) { _pendingBindGroups.Add((nint)bg); return bg; }

	// Queues a transient image texture's GPU release for the next frame start. A brush that uploads a one-shot
	// texture (e.g. CompositionNineGridBrush) disposes it right after recording its draw, but the WebGPU draw is
	// replayed at present (possibly across several presents of the same recording) — so the texture must live until
	// its owning IRenderRecord is disposed. WebGpuRenderRecord.Dispose calls this; the actual free happens at the next
	// BeginFrameResources, after the last present's submit+DevicePoll, like the per-frame bind groups/buffers.
	internal void DeferTextureRelease(TextureView* view, Texture* tex) => _pendingTextures.Enqueue(((nint)view, (nint)tex));

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
	internal void DeferReleaseBuffer(Buffer* buf) { if (buf != null) { _pendingBuffers.Add((nint)buf); } }

	public BindGroupLayout* ImgBgl;
	public BindGroupLayout* GradBgl;
	// Explicit SHARED ClipU layout: every colour pipeline binds it at its last group, so a single ClipU bind group
	// binds to any of them (auto-derived layouts are pipeline-exclusive).
	public BindGroupLayout* ClipBgl;
	public BindGroupLayout* SiteBgl;
	private BindGroupLayout* _emptyBgl;
	// Group 0 of every colour pipeline: the pass projection (16 bytes), one bind group per pass.
	public BindGroupLayout* PassBgl;
	public Sampler* Smp;
	private readonly Sampler*[] _tiledSmp = new Sampler*[16];

	/// <summary>The sampler for a tiled image draw with these per-axis edge modes.</summary>
	public Sampler* TiledSampler(EdgeExtend x, EdgeExtend y) => _tiledSmp[((int)x * 4) + (int)y];

	private static AddressMode Address(EdgeExtend extend) => extend switch
	{
		EdgeExtend.Wrap => AddressMode.Repeat,
		EdgeExtend.Mirror => AddressMode.MirrorRepeat,
		_ => AddressMode.ClampToEdge,
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
	public readonly TextureFormat ColorFormat;
	public const TextureFormat DefaultColorFormat = TextureFormat.Rgba8Unorm;

	// Adopts the device the host created (Uno.UI.Composition.WebGpu.Init); no device bring-up happens here.
	public WebGpuDevice(IWebGpuDeviceContext ctx)
	{
		Api = ctx.Api;
		Wgpu = new Wgpu(Api.Context);
		OffscreenPool = new WebGpuOffscreenPool(Api);
		ColorFormat = ctx.ColorFormat == TextureFormat.Undefined ? DefaultColorFormat : ctx.ColorFormat;
		Inst = ctx.Instance;
		Adapter = ctx.Adapter;
		Dev = ctx.Device;
		Q = ctx.Queue;
		FinishInit();
	}

	private void FinishInit()
	{
		CreatePipelines();
		DummyTex = CreateColorTarget(1, 1);
		var moreDesc = new BufferDescriptor { Size = WebGpuFrame.ClipEntryBytes, Usage = BufferUsage.Storage };
		DummyClipMore = Api.DeviceCreateBuffer(Dev, &moreDesc);
		var rampDesc = new TextureDescriptor
		{
			Size = new Extent3D { Width = RampWidth, Height = RampRows, DepthOrArrayLayers = 1 },
			Format = TextureFormat.Rgba8Unorm,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.TextureBinding | TextureUsage.CopyDst,
		};
		RampTex = Api.DeviceCreateTexture(Dev, &rampDesc);
		RampView = Api.TextureCreateView(RampTex, null);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
		ClipSlab = new WebGpuClipSlab(this);
		SiteSlab = new WebGpuSiteSlab(this);
		GradSlab = new WebGpuUniformSlab(this, GradientUniformBytes, RampView, BufferUsage.Uniform | BufferUsage.CopyDst, default, Smp);
		System.Console.WriteLine($"[webgpu] engine init — colorFormat={ColorFormat}");
	}

	/// <summary>Synchronous GPU→CPU readback of a texture, tightly-packed in the device color format, via a blocking
	/// wgpuDevicePoll spin. Off-browser only (a native thread can pump the map); on the browser the drawing factory's
	/// SnapshotAsync maps off the JS event loop instead, so this is never reached there.</summary>
	public WebGpuReadbackImage ReadPixelsToImage(Texture* tex, int w, int h, bool sourceIsBgra)
	{
		EncodeCopyTexToReadbackBuffer(tex, w, h, out var buf, out var total, out var padded);
		_ = Wgpu.DevicePoll(Dev, true, null);

		var mapped = new bool[1];
		var mh = GCHandle.Alloc(mapped);
		Api.BufferMapAsync(buf, MapMode.Read, 0, (nuint)total, new PfnBufferMapCallback(&OnMap), (void*)GCHandle.ToIntPtr(mh));
		while (!mapped[0]) { _ = Wgpu.DevicePoll(Dev, true, null); }
		mh.Free();
		var mp = (byte*)Api.BufferGetMappedRange(buf, 0, (nuint)total);
		// Unpadded straight out of the mapped range into the image's own buffer — no intermediate array.
		var image = new WebGpuReadbackImage(w, h, new ReadOnlySpan<byte>(mp, (int)total), padded, sourceIsBgra);
		Api.BufferUnmap(buf);
		Api.BufferDestroy(buf);
		Api.BufferRelease(buf);
		return image;
	}

	/// <summary>Creates a MAP_READ buffer, copies <paramref name="tex"/> into it (256-byte-aligned rows) and submits.
	/// The caller maps <paramref name="buf"/> (blocking off-browser, async in JS on the browser) then destroys it.</summary>
	public void EncodeCopyTexToReadbackBuffer(Texture* tex, int w, int h, out Buffer* buf, out int total, out int padded)
	{
		uint pad = ((uint)(w * 4) + 255u) & ~255u;              // wgpu requires 256-byte row alignment for T2B copies
		ulong tot = (ulong)pad * (uint)h;
		var bd = new BufferDescriptor { Size = (nuint)tot, Usage = BufferUsage.CopyDst | BufferUsage.MapRead };
		buf = Api.DeviceCreateBuffer(Dev, &bd);
		var enc = Api.DeviceCreateCommandEncoder(Dev, null);
		var src = new ImageCopyTexture { Texture = tex, Aspect = TextureAspect.All, MipLevel = 0, Origin = default };
		var dst = new ImageCopyBuffer { Buffer = buf, Layout = new TextureDataLayout { Offset = 0, BytesPerRow = pad, RowsPerImage = (uint)h } };
		var ext = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
		Api.CommandEncoderCopyTextureToBuffer(enc, &src, &dst, &ext);
		var cb = Api.CommandEncoderFinish(enc, null);
		Api.QueueSubmit(Q, 1, &cb);
		Api.CommandBufferRelease(cb);
		Api.CommandEncoderRelease(enc);
		total = (int)tot;
		padded = (int)pad;
	}

	// Destroy frees the allocation now; without the Release the handle (and wgpu's accounting of it) stays for the
	// life of the device.
	public void DestroyBuffer(Buffer* buf) { Api.BufferDestroy(buf); Api.BufferRelease(buf); }

	/// <summary>Set by the browser head at WebGPU init: maps a readback buffer (by wgpu handle ptr) off the JS
	/// event loop and returns its raw (row-padded) bytes. The only way to complete a GPU→CPU map on WASM, where a
	/// synchronous poll can't yield. Off-browser this stays null and readback uses the blocking poll.</summary>

	/// <summary>A single-sample Rgba8 render target usable as a shader input (offscreen blur temp/output). The
	/// returned view keeps its texture alive; not pooled/freed yet (fine for offscreen/one-shot).</summary>
	public TextureView* CreateColorTarget(int w, int h)
	{
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = DefaultColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding,
		};
		var tex = Api.DeviceCreateTexture(Dev, &td);
		return Api.TextureCreateView(tex, null);
	}

	/// <summary>
	/// Shared coverage atlas for small paths (glyphs). Created on first use; CopyDst because entries are
	/// rasterized into a scratch surface and copied in, TextureBinding because the draw samples it.
	/// </summary>
	public WebGpuPathAtlas PathAtlas { get; } = new();

	/// <summary>An entry was refused for want of room with every page open; the next frame evicts idle holders.</summary>
	internal bool AtlasStarved;

	/// <summary>Evictions run so far, so an entry built while starved can tell whether room has been made since.</summary>
	internal long AtlasEvictions;

	/// <summary>Opens another atlas page. Pages are added when the existing ones are exhausted.</summary>
	public void AddPathAtlasPage()
	{
		if (PathAtlas.RegularPages >= WebGpuPathAtlas.MaxPages) { return; }
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = WebGpuPathAtlas.Size, Height = WebGpuPathAtlas.Size, DepthOrArrayLayers = 1 },
			// The device's own format: entries are copied in from a scratch surface, and a texture copy requires
			// matching formats (Bgra8 vs Rgba8 is not copy-compatible).
			Format = ColorFormat,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopyDst,
		};
		var tex = Api.DeviceCreateTexture(Dev, &td);
		PathAtlas.AddPage(tex, Api.TextureCreateView(tex, null));
	}

	/// <summary>Destroys the textures of pages whose last slot was freed. Called at the frame boundary.</summary>
	public void ReleaseRetiredAtlasPages()
	{
		if (PathAtlas.Retired.Count == 0) { return; }
		foreach (var page in PathAtlas.Retired)
		{
			if (page.View != null) { Api.TextureViewRelease(page.View); }
			if (page.Texture != null) { Api.TextureRelease(page.Texture); }
		}
		PathAtlas.Retired.Clear();
	}

	private ShaderModule* Module(string wgsl)
	{
		var code = SV(wgsl);
		var w = new ShaderModuleWGSLDescriptor { Chain = new ChainedStruct { SType = SType.ShaderModuleWgslDescriptor }, Code = code };
		var d = new ShaderModuleDescriptor { NextInChain = (ChainedStruct*)&w };
		return Api.DeviceCreateShaderModule(Dev, &d);
	}

	// The ClipU group every colour pipeline shares: the uniform at 0, the path-clip mask at 1, the coverage mask at 2,
	// their sampler at 3 and the clip entries past the uniform's four at 4.
	private PipelineLayout* MakeClipPipeLayout()
	{
		const ShaderStage vf = ShaderStage.Vertex | ShaderStage.Fragment;
		ClipBgl = Bgl(UniformEntry(0, vf, WebGpuFrame.ClipUBytes), TextureEntry(1, TextureSampleType.Float), TextureEntry(2, TextureSampleType.Float), SamplerEntry(3), StorageEntry(4, vf, WebGpuFrame.ClipEntryBytes));
		PassBgl = Bgl(UniformEntry(0, ShaderStage.Vertex, 16));
		SiteBgl = Bgl(UniformEntry(0, vf, WebGpuFrame.SiteUBytes));   // the vertex shader places, the fragment clips
		_emptyBgl = Bgl();
		return ColourLayout(ClipBgl, _emptyBgl, SiteBgl);
	}

	private void CreatePipelines()
	{
		var clipLayout = MakeClipPipeLayout();
		var straight = Blend(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha, BlendFactor.One, BlendFactor.OneMinusSrcAlpha);   // straight-alpha vertex colours
		var over = Blend(BlendFactor.One, BlendFactor.OneMinusSrcAlpha);   // premultiplied textures
		var replace = Blend(BlendFactor.One, BlendFactor.Zero);
		var dstIn = Blend(BlendFactor.Zero, BlendFactor.SrcAlpha);        // the destination keeps only where the texture has alpha
		var add = Blend(BlendFactor.One, BlendFactor.One);
		const VertexFormat F2 = VertexFormat.Float32x2, F4 = VertexFormat.Float32x4;

		// Each colour pipeline gets a twin carrying depth state: a pipeline may only run in a pass whose attachments
		// match it, and only the window pass has a depth buffer. The modules and layouts are shared.
		var solidMod = Module(ClipStructFn + ColoredWgsl);
		SolidPipe = Pipeline(solidMod, clipLayout, &straight, ColorFormat, F2, F4, F2);   // pos, colour, coverage uv
		SolidPipeD = DepthPipeline(solidMod, clipLayout, &straight, ColorFormat, F2, F4, F2);
		var rrMod = Module(ClipStructFn + RoundedRectWgsl);
		RrPipe = Pipeline(rrMod, clipLayout, &straight, ColorFormat, F2, F2, F2, F4, F4, F2, F2, F4);   // corner, local p, half size, radii, colour, inner half, inner centre, inner radii
		RrPipeD = DepthPipeline(rrMod, clipLayout, &straight, ColorFormat, F2, F2, F2, F4, F4, F2, F2, F4);
		GradBgl = Bgl(UniformEntry(0, ShaderStage.Fragment, GradientUniformBytes), TextureEntry(1, TextureSampleType.Float), SamplerEntry(3));
		var gradMod = Module(ClipStructFn + GradientWgsl);
		var gradLayout = ColourLayout(GradBgl, ClipBgl, SiteBgl);
		GradientPipe = Pipeline(gradMod, gradLayout, &straight, ColorFormat, F2, F2);
		GradientPipeD = DepthPipeline(gradMod, gradLayout, &straight, ColorFormat, F2, F2);
		ImgBgl = Bgl(TextureEntry(0, TextureSampleType.Float), SamplerEntry(1), UniformEntry(2, ShaderStage.Fragment, ImageUniformBytes));
		var image = Module(ClipStructFn + ImageWgsl);
		var imageLayout = ColourLayout(ImgBgl, ClipBgl, SiteBgl);
		ImagePipe = Pipeline(image, imageLayout, &over, ColorFormat, F2, F2);
		ImagePipeD = DepthPipeline(image, imageLayout, &over, ColorFormat, F2, F2);
		ImageDstInPipe = Pipeline(image, imageLayout, &dstIn, ColorFormat, F2, F2);
		ImageDstInPipeD = DepthPipeline(image, imageLayout, &dstIn, ColorFormat, F2, F2);
		DepthPrepassPipe = DepthOnlyPipeline(Module(DepthPrepassWgsl), PassOnlyLayout(), ColorFormat);

		// Coverage bakes: signed area accumulates additively into a float sheet, which then resolves to alpha.
		CoverageSheetBgl = Bgl(StorageEntry(0, ShaderStage.Vertex), StorageEntry(1, ShaderStage.Vertex), UniformEntry(2, ShaderStage.Vertex, 16));
		CoverageSheetPipe = Pipeline(Module(CoverageSheetWgsl), Layout(CoverageSheetBgl), &add, CoverageFormat);
		CoverageResolveSheetBgl = Bgl(TextureEntry(0, TextureSampleType.UnfilterableFloat));
		CoverageResolveSheetPipe = Pipeline(Module(CoverageResolveSheetWgsl), Layout(CoverageResolveSheetBgl), null, ColorFormat, F2, F2, F2);

		// Fullscreen effect passes take their bind group layout from the shader. The effect-graph stages emit the
		// composited pixel, so they replace.
		BlurPipe = Pipeline(Module(BlurWgsl), null, null, DefaultColorFormat);
		BlurBgl = Api.RenderPipelineGetBindGroupLayout(BlurPipe, 0);
		CompositeSrcOver = Pipeline(Module(CompositeWgsl), null, &over, ColorFormat);
		CompositeBgl = Api.RenderPipelineGetBindGroupLayout(CompositeSrcOver, 0);
		CompositeBlend = Pipeline(Module(CompositeBlendWgsl), null, &replace, ColorFormat);
		CompositeBlendBgl = Api.RenderPipelineGetBindGroupLayout(CompositeBlend, 0);
		EffectCombine = Pipeline(Module(EffectCombineWgsl), null, &replace, ColorFormat);
		EffectCombineBgl = Api.RenderPipelineGetBindGroupLayout(EffectCombine, 0);
		ColorFunc = Pipeline(Module(ColorFuncWgsl), null, &replace, ColorFormat);
		ColorFuncBgl = Api.RenderPipelineGetBindGroupLayout(ColorFunc, 0);
		EffectNoise = Pipeline(Module(EffectNoiseWgsl), null, &replace, ColorFormat);
		EffectNoiseBgl = Api.RenderPipelineGetBindGroupLayout(EffectNoise, 0);
		// Replace, so the fill writes the colour and its alpha exactly as a load-op clear would.
		ClearRectPipe = Pipeline(Module(ClearRectWgsl), null, &replace, ColorFormat);
		ClearRectBgl = Api.RenderPipelineGetBindGroupLayout(ClearRectPipe, 0);

		// The LOD clamps are WebGPU's defaults, spelled out: a zeroed descriptor clamps to level 0 and ignores the mips.
		var sd = new SamplerDescriptor { AddressModeU = AddressMode.ClampToEdge, AddressModeV = AddressMode.ClampToEdge, MagFilter = FilterMode.Linear, MinFilter = FilterMode.Linear, MipmapFilter = MipmapFilterMode.Linear, LodMinClamp = 0f, LodMaxClamp = 32f, MaxAnisotropy = 1 };
		Smp = Api.DeviceCreateSampler(Dev, &sd);
		// Address-mode variants for tiled image draws. EdgeExtend.None shares the clamp sampler: a non-filling
		// draw is bounded by its quad instead, so nothing ever samples past the texture.
		for (var x = 0; x < 4; x++)
		{
			for (var y = 0; y < 4; y++)
			{
				var td = sd;
				td.AddressModeU = Address((EdgeExtend)x);
				td.AddressModeV = Address((EdgeExtend)y);
				_tiledSmp[(x * 4) + y] = Api.DeviceCreateSampler(Dev, &td);
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
		var dst = new ImageCopyTexture { Texture = RampTex, Aspect = TextureAspect.All, MipLevel = 0, Origin = new Origin3D { X = 0, Y = (uint)row, Z = 0 } };
		var layout = new TextureDataLayout { Offset = 0, BytesPerRow = RampWidth * 4, RowsPerImage = 1 };
		var ext = new Extent3D { Width = RampWidth, Height = 1, DepthOrArrayLayers = 1 };
		fixed (byte* p = px) { Api.QueueWriteTexture(Q, &dst, p, (nuint)px.Length, &layout, &ext); }
		_rampRows[key] = row;
		return (row + 0.5f) / RampRows;
	}

	/// <summary>Single-channel float so accumulation can exceed 1 and go negative; blendable, unlike r32float.</summary>
	public const TextureFormat CoverageFormat = TextureFormat.R16float;
	public RenderPipeline* CoverageSheetPipe, CoverageResolveSheetPipe;
	public BindGroupLayout* CoverageSheetBgl, CoverageResolveSheetBgl;
	/// <summary>The colour pipelines again, with depth state, for the one pass that carries a depth buffer.</summary>
	public RenderPipeline* SolidPipeD, RrPipeD, GradientPipeD, ImagePipeD, ImageDstInPipeD;
	/// <summary>Writes the occlusion depth buffer and nothing else.</summary>
	public RenderPipeline* DepthPrepassPipe;

	private static readonly byte* VsEntry = SV("vs"), FsEntry = SV("fs");

	// One render pipeline: a fullscreen triangle when it has no vertex attributes, else one vertex buffer of the
	// attributes packed in order.
	/// <summary>The occlusion depth buffer's format. Depth only -- nothing here uses stencil.</summary>
	public const TextureFormat DepthFormat = TextureFormat.Depth24Plus;

	// A colour pipeline that reads the occlusion depth buffer: it never writes depth (the prepass does that), and
	// keeps a fragment only where its site is at or in front of whatever opaque site last covered that pixel.
	private RenderPipeline* DepthPipeline(ShaderModule* module, PipelineLayout* layout, BlendState* blend, TextureFormat format, params ReadOnlySpan<VertexFormat> attrs)
	{
		var ds = new DepthStencilState
		{
			Format = DepthFormat,
			DepthWriteEnabled = false,
			DepthCompare = CompareFunction.GreaterEqual,
			StencilFront = new StencilFaceState { Compare = CompareFunction.Always, FailOp = StencilOperation.Keep, DepthFailOp = StencilOperation.Keep, PassOp = StencilOperation.Keep },
			StencilBack = new StencilFaceState { Compare = CompareFunction.Always, FailOp = StencilOperation.Keep, DepthFailOp = StencilOperation.Keep, PassOp = StencilOperation.Keep },
			StencilReadMask = 0xFFFFFFFF,
			StencilWriteMask = 0xFFFFFFFF,
		};
		return Pipeline(module, layout, blend, format, &ds, attrs);
	}

	private RenderPipeline* Pipeline(ShaderModule* module, PipelineLayout* layout, BlendState* blend, TextureFormat format, params ReadOnlySpan<VertexFormat> attrs)
		=> Pipeline(module, layout, blend, format, null, attrs);

	private RenderPipeline* Pipeline(ShaderModule* module, PipelineLayout* layout, BlendState* blend, TextureFormat format, DepthStencilState* depth, ReadOnlySpan<VertexFormat> attrs)
	{
		var va = stackalloc VertexAttribute[Math.Max(1, attrs.Length)];
		ulong stride = 0;
		for (var i = 0; i < attrs.Length; i++)
		{
			va[i] = new VertexAttribute { Format = attrs[i], Offset = stride, ShaderLocation = (uint)i };
			stride += attrs[i] == VertexFormat.Float32x4 ? 16u : 8u;
		}
		var vbl = new VertexBufferLayout { ArrayStride = stride, StepMode = VertexStepMode.Vertex, AttributeCount = (nuint)attrs.Length, Attributes = va };
		var target = new ColorTargetState { Format = format, Blend = blend, WriteMask = ColorWriteMask.All };
		var fs = new FragmentState { Module = module, EntryPoint = FsEntry, TargetCount = 1, Targets = &target };
		var pd = new RenderPipelineDescriptor
		{
			Vertex = new VertexState { Module = module, EntryPoint = VsEntry, BufferCount = attrs.Length == 0 ? 0u : 1u, Buffers = attrs.Length == 0 ? (VertexBufferLayout*)null : &vbl },
			Fragment = &fs,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = depth,
			Layout = layout,
		};
		return Api.DeviceCreateRenderPipeline(Dev, &pd);
	}

	private static BlendState Blend(BlendFactor src, BlendFactor dst) => Blend(src, dst, src, dst);

	private static BlendState Blend(BlendFactor src, BlendFactor dst, BlendFactor alphaSrc, BlendFactor alphaDst)
		=> new()
		{
			Color = new BlendComponent { SrcFactor = src, DstFactor = dst, Operation = BlendOperation.Add },
			Alpha = new BlendComponent { SrcFactor = alphaSrc, DstFactor = alphaDst, Operation = BlendOperation.Add },
		};

	private static BindGroupLayoutEntry UniformEntry(uint binding, ShaderStage visibility, int minSize)
		=> new() { Binding = binding, Visibility = visibility, Buffer = new BufferBindingLayout { Type = BufferBindingType.Uniform, MinBindingSize = (ulong)minSize } };

	private static BindGroupLayoutEntry StorageEntry(uint binding, ShaderStage visibility, int minSize = 0)
		=> new() { Binding = binding, Visibility = visibility, Buffer = new BufferBindingLayout { Type = BufferBindingType.ReadOnlyStorage, MinBindingSize = (ulong)minSize } };

	private static BindGroupLayoutEntry TextureEntry(uint binding, TextureSampleType sampleType)
		=> new() { Binding = binding, Visibility = ShaderStage.Fragment, Texture = new TextureBindingLayout { SampleType = sampleType, ViewDimension = TextureViewDimension.Dimension2D } };

	private static BindGroupLayoutEntry SamplerEntry(uint binding)
		=> new() { Binding = binding, Visibility = ShaderStage.Fragment, Sampler = new SamplerBindingLayout { Type = SamplerBindingType.Filtering } };

	private BindGroupLayout* Bgl(params ReadOnlySpan<BindGroupLayoutEntry> entries)
	{
		fixed (BindGroupLayoutEntry* p = entries)
		{
			var d = new BindGroupLayoutDescriptor { EntryCount = (nuint)entries.Length, Entries = p };
			return Api.DeviceCreateBindGroupLayout(Dev, &d);
		}
	}

	private PipelineLayout* Layout(params BindGroupLayout*[] groups)
	{
		fixed (BindGroupLayout** p = groups)
		{
			var d = new PipelineLayoutDescriptor { BindGroupLayoutCount = (nuint)groups.Length, BindGroupLayouts = p };
			return Api.DeviceCreatePipelineLayout(Dev, &d);
		}
	}

	// A colour pipeline's layout: the pass projection at group 0, then its own groups.
	private PipelineLayout* ColourLayout(params BindGroupLayout*[] groups)
	{
		var all = new BindGroupLayout*[groups.Length + 1];
		all[0] = PassBgl;
		for (var i = 0; i < groups.Length; i++)
		{
			all[i + 1] = groups[i];
		}
		return Layout(all);
	}

	// The prepass needs nothing but the pass projection.
	private PipelineLayout* PassOnlyLayout() => Layout(PassBgl);

	// Depth-only: the fragment stage writes no colour, so the draw is a vertex per corner plus fixed-function
	// depth. GREATER + write leaves each pixel holding the depth of the LAST opaque site that covered it.
	private RenderPipeline* DepthOnlyPipeline(ShaderModule* module, PipelineLayout* layout, TextureFormat format)
	{
		var attr = new VertexAttribute { Format = VertexFormat.Float32x3, Offset = 0, ShaderLocation = 0 };
		var vbl = new VertexBufferLayout { ArrayStride = 12, StepMode = VertexStepMode.Vertex, AttributeCount = 1, Attributes = &attr };
		var ds = new DepthStencilState
		{
			Format = DepthFormat,
			DepthWriteEnabled = true,
			DepthCompare = CompareFunction.Greater,
			StencilFront = new StencilFaceState { Compare = CompareFunction.Always, FailOp = StencilOperation.Keep, DepthFailOp = StencilOperation.Keep, PassOp = StencilOperation.Keep },
			StencilBack = new StencilFaceState { Compare = CompareFunction.Always, FailOp = StencilOperation.Keep, DepthFailOp = StencilOperation.Keep, PassOp = StencilOperation.Keep },
			StencilReadMask = 0xFFFFFFFF,
			StencilWriteMask = 0xFFFFFFFF,
		};
		var target = new ColorTargetState { Format = format, Blend = null, WriteMask = ColorWriteMask.None };
		var fs = new FragmentState { Module = module, EntryPoint = FsEntry, TargetCount = 1, Targets = &target };
		var pd = new RenderPipelineDescriptor
		{
			Vertex = new VertexState { Module = module, EntryPoint = VsEntry, BufferCount = 1, Buffers = &vbl },
			Fragment = &fs,
			Primitive = new PrimitiveState { Topology = PrimitiveTopology.TriangleList, FrontFace = FrontFace.Ccw, CullMode = CullMode.None },
			Multisample = new MultisampleState { Count = 1, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			DepthStencil = &ds,
			Layout = layout,
		};
		return Api.DeviceCreateRenderPipeline(Dev, &pd);
	}

	// Persistent UTF-8 for a WGPUStringView (WGSL/entry points; created once at pipeline init, intentionally not freed).
	private static byte* SV(string s)
		=> (byte*)Marshal.StringToCoTaskMemUTF8(s);

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMap(BufferMapAsyncStatus status, void* u1)
		=> ((bool[])GCHandle.FromIntPtr((nint)u1).Target!)[0] = true;

	// Releases the transient pools (the multi-GB VRAM the offscreens + resize generations accumulate). The wgpu
	// device/queue/adapter/instance are owned by the host context and released there; here we only reclaim what
	// this device allocated into its own pools so a closed window doesn't leak its full-window textures.
	public void Dispose()
	{
		Pool?.Dispose();
		BufferPool?.Dispose();
	}
}
