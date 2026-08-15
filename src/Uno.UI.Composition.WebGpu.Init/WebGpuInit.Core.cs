// Minimal-but-real WebGPU backend implementing the NEUTRAL drawing seam (public SPI from Uno.UI.Composition).
// Solid rects + even-odd path fill (stencil-then-cover) consuming IGeometry.StreamFlattened (Skia-less).
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
	// Transform-table path-fill pipelines (device verts + per-vertex slot index). XformBgl = group 0 (storage
	// table); CoverTableClipBgl = the cover variant's group 1 (ClipU). Solid/clip-fans/shadows stay on the NDC pipes.
	public IntPtr StencilTableEO;
	public IntPtr StencilTableNZ;
	public IntPtr CoverTablePipe;
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
	public IntPtr DummyTex;                 // 1x1 placeholder for the clip coverage binding when no path clip
	public WebGpuTexturePool Pool;                // transient offscreen pool (reused across frames)
	public WebGpuBufferPool BufferPool;           // transient vertex/uniform buffer pool (reused across frames)
	public WebGpuSlab SolidSlab;                  // persistent shared slab: all recordings' solid verts (6 floats/v)
	public WebGpuSlab RrectSlab;                  // persistent shared slab: all recordings' rrect verts (22 floats/v)
	private long _nextSlabId = 1;                 // stable per-recording slab id (assigned on cache miss)
	public long NextSlabId() => _nextSlabId++;
	// Serializes a whole frame's render (reset → record → submit → poll) on this device. The on-window render
	// loop and off-loop renders (RenderTargetBitmap) share the device's transient pools/caches, so two frames
	// must not overlap or one frame's BeginFrameResources frees the other's in-flight resources (wgpu panics).
	public readonly object RenderGate = new();
	private readonly System.Collections.Generic.List<nint> _pendingBindGroups = new();
	private readonly System.Collections.Generic.List<nint> _pendingBuffers = new();
	// Transient image textures whose owning IRenderData was disposed; drained (GPU-released) at the next frame start.
	// Concurrent because a frame is disposed on the UI thread while BeginFrameResources runs on the render thread.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(nint view, nint tex)> _pendingTextures = new();
	// Per-recording compiled GPU draw-list. It lives ON the recording's WebGpuRenderData (IRenderData is, by its own
	// contract, "backend-defined retained state"), built once and replayed cheaply — no global cache, no per-frame
	// eviction scan. When the owning IRenderData is disposed (UI thread, on a content change), its compiled state is
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
	public void BeginFrameResources()
	{
		Pool.BeginFrame();
		BufferPool.BeginFrame();
		foreach (var bg in _pendingBindGroups) { wgpuBindGroupRelease((IntPtr)bg); }
		foreach (var b in _pendingBuffers) { wgpuBufferRelease((IntPtr)b); }
		// Release (refcount) rather than Destroy (immediate) the transient one-shot textures: wgpu then frees them
		// only once the GPU has finished the frames that used them — safe even when the per-frame drain is skipped.
		while (_pendingTextures.TryDequeue(out var t)) { if (t.view != IntPtr.Zero) { wgpuTextureViewRelease((IntPtr)t.view); } if (t.tex != IntPtr.Zero) { wgpuTextureRelease((IntPtr)t.tex); } }
		_pendingBindGroups.Clear();
		_pendingBuffers.Clear();

		EvictStaleBindGroups();

		// Free compiled draw-lists whose owning recording was disposed (their slab slices are reclaimed separately by
		// each slab's RetainOnly, since a disposed recording is never replayed → never marked live).
		while (_pendingCompiled.TryDequeue(out var c)) { DeferRelease(c.Owned); DeferRelease(c.StampOwned); if (c.XformSlot >= 0) { _freeXformSlots.Push(c.XformSlot); } }
	}

	// Diagnostic (UNO_RENDER_PERF): per-frame render CPU time + bind-group/buffer creates, logged from RunFrame.
	// PerfBgCreates trends to ~0 in steady state once the cross-frame bind-group cache warms. Off by default.
	internal static readonly bool PerfEnabled = Environment.GetEnvironmentVariable("UNO_RENDER_PERF") == "1";

	// EXPERIMENTAL opt-in (UNO_WEBGPU_PIPELINE=1): don't block the CPU on a full GPU drain after each on-window
	// frame — poll non-blocking so the CPU can record the next frame while the GPU renders the current one
	// (~2x steady frame rate). Safe because pooled buffer/texture reuse and the persistent present target are
	// ordered on the queue after the prior frame's reads, and transient textures are refcount-released (not
	// destroyed) so wgpu frees them only once the GPU is done. Off by default (the drain is the conservative path);
	// needs a real-GPU check for present-time tearing before it can become the default.
	// Default ON: pooled-buffer reuse is queue-ordered (wgpuQueueWriteBuffer runs after the prior frame's reads) and
	// transient textures are refcount-released, so non-blocking is safe; the swapchain's max-frames-in-flight
	// provides backpressure. Set UNO_WEBGPU_PIPELINE=0 to force the old blocking drain (debugging / tearing check).
	internal static readonly bool Pipeline = Environment.GetEnvironmentVariable("UNO_WEBGPU_PIPELINE") != "0";
	public int PerfBgCreates, PerfBufCreates, PerfFrame;
	public readonly System.Diagnostics.Stopwatch PerfSw = new();
	public double PerfAccumMs;

	public IntPtr TrackBg(IntPtr bg) { PerfBgCreates++; _pendingBindGroups.Add((nint)bg); return bg; }

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
			PerfBgCreates++;
		}
		return _xformBg;
	}

	// Cross-frame cache for content-identical bind groups whose resources are all persistent (the uniform buffer we
	// own here + device-stable DummyTex/sampler) — i.e. non-path clips and gradients. Static UI chrome rebuilds the
	// same clip/gradient every frame; caching drops a CreateBuffer + CreateBindGroup per such command per frame.
	// Keyed by (layout, uniform floats). Evicted after 240 unused frames. Path-clip bind groups are NOT cached (their
	// coverage texture is per-frame pooled). Touched only under RenderGate (main + nested renders serialize on it).
	private int _bgFrameNo;
	private sealed class CachedBg { public nint Bgl; public float[] Sig; public IntPtr Buf; public IntPtr Bg; public int LastUsed; }
	private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<CachedBg>> _bgCache = new();
	private readonly System.Collections.Generic.List<int> _bgCacheEvict = new();

	private static int SigHash(nint bgl, float[] sig)
	{
		unchecked
		{
			int h = (int)bgl ^ (int)(bgl >> 32);
			foreach (var f in sig) { h = (h * 16777619) ^ BitConverter.SingleToInt32Bits(f); }
			return h;
		}
	}

	internal bool TryGetCachedBg(nint bgl, float[] sig, out IntPtr bg)
	{
		if (_bgCache.TryGetValue(SigHash(bgl, sig), out var bucket))
		{
			foreach (var e in bucket)
			{
				if (e.Bgl == bgl && ((ReadOnlySpan<float>)e.Sig).SequenceEqual(sig)) { e.LastUsed = _bgFrameNo; bg = e.Bg; Profiler?.BgHit(); return true; }
			}
		}
		Profiler?.BgMiss();
		bg = default;
		return false;
	}

	internal void AddCachedBg(nint bgl, float[] sig, IntPtr buf, IntPtr bg)
	{
		PerfBgCreates++;   // this path only runs on a cache miss (a bind group was just created)
		var h = SigHash(bgl, sig);
		if (!_bgCache.TryGetValue(h, out var bucket)) { bucket = new(); _bgCache[h] = bucket; }
		bucket.Add(new CachedBg { Bgl = bgl, Sig = sig, Buf = buf, Bg = bg, LastUsed = _bgFrameNo });
	}

	private void EvictStaleBindGroups()
	{
		_bgFrameNo++;
		_bgCacheEvict.Clear();
		foreach (var kv in _bgCache)
		{
			var bucket = kv.Value;
			for (int i = bucket.Count - 1; i >= 0; i--)
			{
				if (_bgFrameNo - bucket[i].LastUsed > 240)
				{
					wgpuBindGroupRelease(bucket[i].Bg);
					if (bucket[i].Buf != IntPtr.Zero) { wgpuBufferRelease(bucket[i].Buf); }
					bucket.RemoveAt(i);
				}
			}
			if (bucket.Count == 0) { _bgCacheEvict.Add(kv.Key); }
		}
		foreach (var k in _bgCacheEvict) { _bgCache.Remove(k); }
	}

	// Queues a transient image texture's GPU release for the next frame start. A brush that uploads a one-shot
	// texture (e.g. CompositionNineGridBrush) disposes it right after recording its draw, but the WebGPU draw is
	// replayed at present (possibly across several presents of the same recording) — so the texture must live until
	// its owning IRenderData is disposed. WebGpuRenderData.Dispose calls this; the actual free happens at the next
	// BeginFrameResources, after the last present's submit+DevicePoll, like the per-frame bind groups/buffers.
	internal void DeferTextureRelease(IntPtr view, IntPtr tex) => _pendingTextures.Enqueue(((nint)view, (nint)tex));

	// Defers a cached recording's persistent resources for release at the next frame start.
	internal void DeferRelease(OwnedResources owned)
	{
		if (owned is null) { return; }
		_pendingBuffers.AddRange(owned.Buffers);
		_pendingBindGroups.AddRange(owned.BindGroups);
	}
	// Defers a single GPU buffer (e.g. an outgrown slab buffer) for release at the next frame start.
	internal void DeferReleaseBuffer(nint buf) { if (buf != IntPtr.Zero) { _pendingBuffers.Add(buf); } }
	// Detailed frame profiler (UNO_WEBGPU_PROFILE=1). Null when disabled — every hook is `Profiler?.X()` so there
	// is zero overhead and no behaviour change off. Bracketed by the host DrawFrame (FrameStart) + Present (FrameEnd).
	public WebGpuProfiler Profiler;

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
	// Bgra8Unorm). UNO_WEBGPU_MSAA=4 forces 4x. (1x/no-MSAA would need a separate no-resolve path — not wired.)
	public uint MsaaSamples { get; private set; } = 4;

	// DPI/rasterization scale (physical px per logical px), set by the platform host BEFORE the device is created so
	// PickSampleCount can pick a DPI-aware MSAA count matching the reference branch. 1.0 until the host sets it.
	public static float RasterizationScale = 1f;

	// True when the device enabled wgpu-native's TextureAdapterSpecificFormatFeatures — required for 2x MSAA on
	// formats like BGRA8/RGBA8 (else 2x fails validation). Gates whether PickSampleCount may choose 2x.
	public bool HasFormatFeatures;

	// TEMP DIAGNOSTIC (Win32 OOM): log + clamp every texture extent so an absurd size (e.g. a bad DPI/bounds
	// computation) is visible in the console and doesn't hard-abort wgpu with "Not enough memory". Remove once
	// the Win32 texture-allocation crash is root-caused.
	internal static int _texCreateCount;
	internal static void TexLog(string site, uint w, uint h, uint samples)
	{
		var n = System.Threading.Interlocked.Increment(ref _texCreateCount);
		if (n <= 1000 || w > 16384 || h > 16384) { Console.WriteLine($"[webgpu] TEX #{n} {site} {w}x{h} x{samples}"); }
	}

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
		if (Adapter == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: wgpuInstanceRequestAdapter returned no adapter (no compatible GPU/driver, or the callback never fired)."); }

		var dbox = new IntPtr[1];
		var dh = GCHandle.Alloc(dbox);
		var ddesc = new WGPUDeviceDescriptor();
		// Request wgpu-native's TextureAdapterSpecificFormatFeatures when the adapter has it — WITHOUT it, MSAA sample
		// counts that aren't spec-guaranteed for the format (2x for BGRA8/RGBA8) fail validation and the uncaptured-
		// error handler panics; WITH it, 2x works (so the DPI-aware default's 2x tier at 125-200% DPI is usable). The
		// reference (webgpu) branch requests this too. Kept on the stack for the synchronous request below.
		WGPUFeatureName* feats = stackalloc WGPUFeatureName[1];
		var fmtFeat = (WGPUFeatureName)WGPUNativeFeature.TextureAdapterSpecificFormatFeatures;
		HasFormatFeatures = wgpuAdapterHasFeature(Adapter, fmtFeat) != 0;
		if (HasFormatFeatures) { feats[0] = fmtFeat; ddesc.RequiredFeatures = feats; ddesc.RequiredFeatureCount = 1; }
		// Install a non-fatal uncaptured-error handler (matching the reference host): without one, wgpu's default
		// handler PANICS the whole process on any validation/OOM error. Log and continue instead.
		ddesc.UncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo
		{
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnUncapturedError,
		};
		wgpuAdapterRequestDevice(Adapter, &ddesc, new WGPURequestDeviceCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPURequestDeviceStatus, IntPtr, WGPUStringView, IntPtr, IntPtr, void>)&OnDevice,
			Userdata1 = GCHandle.ToIntPtr(dh),
		});
		for (int i = 0; i < 1000 && dbox[0] == IntPtr.Zero; i++) { wgpuInstanceProcessEvents(Inst); }
		Dev = dbox[0];
		dh.Free();
		if (Dev == IntPtr.Zero) { throw new InvalidOperationException("WebGPU: wgpuAdapterRequestDevice returned no device."); }

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

	/// <summary>Adopts an instance + a device imported from JS (SkiaSharp-style browser bring-up). There is no
	/// adapter handle (the JS adapter isn't imported); the surface config falls back to the colour format directly.
	/// FinishInit obtains the queue via wgpuDeviceGetQueue on the imported device.</summary>
	public static WebGpuDevice FromImported(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr dev)
		=> new WebGpuDevice(colorFormat, inst, IntPtr.Zero, dev);

	private void FinishInit()
	{
		Q = wgpuDeviceGetQueue(Dev);
		MsaaSamples = PickSampleCount();   // must precede CreatePipelines (pipelines bake the sample count)
		CreatePipelines();
		DummyTex = CreateColorTarget(1, 1);
		Pool = new WebGpuTexturePool(this);
		BufferPool = new WebGpuBufferPool(this);
		SolidSlab = new WebGpuSlab(this, 6);
		RrectSlab = new WebGpuSlab(this, 22);
		if (WebGpuProfiler.Enabled) { Profiler = new WebGpuProfiler(); }
		// Startup marker (always logged): confirms this build is current + reports the profiler/pipeline/MSAA state,
		// so a missing profiler line can be told apart from a stale build or the flag not being read.
		System.Console.WriteLine($"[webgpu] backend init — UNO_WEBGPU_PROFILE={WebGpuProfiler.Enabled} pipeline={Pipeline} msaa={MsaaSamples}x scale={RasterizationScale} fmtFeatures={HasFormatFeatures} colorFormat={ColorFormat}");
	}

	// MSAA sample count. UNO_WEBGPU_MSAA=1|2|4 forces a count (bypassing the probe): 2 forces 2x even where the
	// auto-probe reported it unsupported (some drivers report conservatively) — if the GPU genuinely rejects it a
	// validation error follows, so fall back to 4. 1 is experimental (needs a no-resolve pass path — not wired yet;
	// currently the pipelines/resolve assume MSAA, so 1 will error) and is clamped to 4 for safety. With no override,
	// prefer 2x where the device supports it for the colour format, else 4x (the only counts the spec guarantees).
	private uint PickSampleCount()
	{
		var env = Environment.GetEnvironmentVariable("UNO_WEBGPU_MSAA");
		if (env == "4") { return 4; }
		if (env == "1") { return 1; }   // no-resolve 1x: passes render straight into the single-sample view (see WebGpuRenderSurface)
		if (env == "2") { return HasFormatFeatures && SupportsSampleCount(2) ? 2u : 4u; }   // 2x needs the format feature (else validation panics)
		// The browser (Dawn) init is async and can't synchronously pump the error-scope callback (no JS event-loop
		// yield), so skip the probe there and take the spec-guaranteed 4x. Desktop probes for 2x.
		if (OperatingSystem.IsBrowser()) { return 4; }
		// DEFAULT matches the reference (webgpu) branch: DPI-AWARE — 1x at >=200% (no MSAA/resolve; the resolve cost
		// scales with physical pixels and blows the budget on high-DPI), 2x at 100-200%, 4x at 100%. The host sets
		// RasterizationScale before the device is created; unset (1.0) => 4x, the reference's 100%-DPI default.
		var scale = RasterizationScale;
		if (scale >= 2f) { return 1u; }
		if (scale > 1f) { return HasFormatFeatures && SupportsSampleCount(2) ? 2u : 4u; }
		return 4u;
	}

	// Probes whether a sample count is valid for the colour format WITHOUT aborting: an unsupported count raises a
	// validation error, which a pushed error scope captures (the default uncaptured handler panics the process).
	private bool SupportsSampleCount(uint samples)
	{
		wgpuDevicePushErrorScope(Dev, WGPUErrorFilter.Validation);
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = 1, Height = 1, DepthOrArrayLayers = 1 },
			Format = ColorFormat, MipLevelCount = 1, SampleCount = samples,
			Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.RenderAttachment,
		};
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		var box = new uint[2];   // [0] = callback fired, [1] = captured WGPUErrorType (0 stays => treat as failure)
		var h = GCHandle.Alloc(box);
		wgpuDevicePopErrorScope(Dev, new WGPUPopErrorScopeCallbackInfo
		{
			Mode = WGPUCallbackMode.AllowProcessEvents,
			Callback = (IntPtr)(delegate* unmanaged[Cdecl]<WGPUPopErrorScopeStatus, WGPUErrorType, WGPUStringView, IntPtr, IntPtr, void>)&OnPopErrorScope,
			Userdata1 = GCHandle.ToIntPtr(h),
		});
		for (int i = 0; i < 1000 && box[0] == 0; i++) { wgpuInstanceProcessEvents(Inst); }
		h.Free();
		if (tex != IntPtr.Zero) { wgpuTextureDestroy(tex); }
		return box[1] == (uint)WGPUErrorType.NoError;   // supported only if the scope popped with no error
	}

	public static unsafe IntPtr CreateInstancePtr() => wgpuCreateInstance(null);

	/// <summary>Reads a surface's resolved single-sample texture back to CPU as tightly-packed RGBA8 (top-down). For RTB and tests.</summary>
	public byte[] ReadPixelsRgba(WebGpuRenderSurface s) => ReadPixelsFromTex(s.Tex, s.Width, s.Height);

	/// <summary>Synchronous GPU→CPU readback of a texture, tightly-packed in the device color format. Uses a
	/// blocking wgpuDevicePoll spin — valid off-browser (a native thread can pump); on WASM the poll is a no-op
	/// and the map never completes, so browser readback goes through <see cref="SnapshotBrowserAsync"/> instead.</summary>
	public byte[] ReadPixelsFromTex(IntPtr tex, int w, int h)
	{
		EncodeCopyTexToReadbackBuffer(tex, w, h, out var buf, out var total, out var padded);
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
		var outp = Unpad(new ReadOnlySpan<byte>(mp, (int)total), w, h, padded);
		wgpuBufferUnmap(buf);
		wgpuBufferDestroy(buf);
		return outp;
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
		total = (int)tot;
		padded = (int)pad;
	}

	public void DestroyBuffer(IntPtr buf) => wgpuBufferDestroy(buf);

	/// <summary>Drops the 256-byte row padding, yielding tightly-packed w×h×4 bytes.</summary>
	public static byte[] Unpad(ReadOnlySpan<byte> paddedRows, int w, int h, int padded)
	{
		int unpadded = w * 4;
		var outp = new byte[w * h * 4];
		for (int y = 0; y < h; y++)
		{
			paddedRows.Slice(y * padded, unpadded).CopyTo(outp.AsSpan(y * unpadded, unpadded));
		}
		return outp;
	}

	/// <summary>Set by the browser head at WebGPU init: maps a readback buffer (by wgpu handle ptr) off the JS
	/// event loop and returns its raw (row-padded) bytes. The only way to complete a GPU→CPU map on WASM, where a
	/// synchronous poll can't yield. Off-browser this stays null and readback uses the blocking poll.</summary>
	public static Func<IntPtr, int, System.Threading.Tasks.Task<byte[]>> BrowserReadbackAsync { get; set; }

	// Shared clip type + coverage fn prepended to every color-writing shader. The uniform is passed as a
	// parameter (each shader declares the binding at its own contiguous group index — colored uses group 0,
	// image/gradient group 1 — avoiding a group hole wgpu's auto-layout rejects). Device-space, axis-aligned
	// rounded-rect mask (radii 0 → plain rect, full coverage); ~1px analytic AA on the corner edge.
	private const string ClipStructFn = @"
// rects[i]/radii[i] are the nested rounded-rect clips (device space), ANDed together; ex[i]>0.5 = Difference
// (keep outside). meta.x = active count. Arbitrary path clips are applied via the shared depth buffer as an in-pass
// mask (see the main-pass clip protocol), not sampled here — so clipCov only carries the analytic rounded-rects.
// radii = per-corner X radius (TL,TR,BR,BL); radiiY = per-corner Y radius (elliptical corners; == radii for circular).
struct ClipU { rects: array<vec4<f32>, 4>, radii: array<vec4<f32>, 4>, ex: vec4<f32>, ctrl: vec4<f32>, size: vec4<f32>, xform: vec4<f32>, xoff: vec4<f32>, finv: vec4<f32>, radiiY: array<vec4<f32>, 4> };
// Arena transform: verts are stored in the recording's own (identity-baked) NDC space; xform (an NDC->NDC affine,
// M = xform.xyzw = [m00 m01 m10 m11], t = xoff.xy) maps them to the replay transform. Identity for immediate draws;
// re-stamped (a single uniform write) when a cached visual moves, so its geometry is reused, not rebuilt.
fn xformPos(clip: ClipU, pos: vec2<f32>) -> vec4<f32> {
  return vec4<f32>(clip.xform.x * pos.x + clip.xform.y * pos.y + clip.xoff.x,
                   clip.xform.z * pos.x + clip.xform.w * pos.y + clip.xoff.y, 0.0, 1.0);
}
// Maps a (moved) device fragment position back to the recording's own space so device-space fragment inputs (clip
// shape, gradient geometry) baked at identity stay correct after an arena transform re-stamp. Identity = no-op.
fn finvMap(clip: ClipU, fcRaw: vec2<f32>) -> vec2<f32> {
  return vec2<f32>(clip.finv.x * fcRaw.x + clip.finv.z * fcRaw.y + clip.xoff.z,
                   clip.finv.y * fcRaw.x + clip.finv.w * fcRaw.y + clip.xoff.w);
}
// Coverage of one rounded-rect clip (rl = L,T,R,B; rad4 = per-corner radii; ex>0.5 = Difference/keep-outside).
fn roundCov(fc: vec2<f32>, rl: vec4<f32>, radX: vec4<f32>, radY: vec4<f32>, ex: f32) -> f32 {
  let c = vec2<f32>((rl.x + rl.z) * 0.5, (rl.y + rl.w) * 0.5);
  let h = vec2<f32>((rl.z - rl.x) * 0.5, (rl.w - rl.y) * 0.5);
  let lp = fc - c;
  let rx = select(select(radX.x, radX.y, lp.x > 0.0), select(radX.w, radX.z, lp.x > 0.0), lp.y > 0.0);
  let ry = select(select(radY.x, radY.y, lp.x > 0.0), select(radY.w, radY.z, lp.x > 0.0), lp.y > 0.0);
  let r = vec2<f32>(rx, ry);
  // Elliptical corner via a first-order (gradient-normalised) implicit-ellipse distance. Degenerates EXACTLY to the
  // circular rounded-box SDF when rx == ry (and to a sharp box when r == 0), so circular clips are unchanged.
  let q = abs(lp) - h + r;
  let outside = max(q, vec2<f32>(0.0, 0.0));
  let rg = max(r, vec2<f32>(1e-6, 1e-6));
  let e = outside / rg;
  let el = length(e);
  let grad = length(outside / (rg * rg)) / max(el, 1e-6);
  let dCorner = (el - 1.0) / max(grad, 1e-6);
  let d = min(max(q.x, q.y), 0.0) + dCorner;
  let rr = clamp(0.5 - d, 0.0, 1.0);
  return select(rr, 1.0 - rr, ex > 0.5);
}
fn clipCov(fcRaw: vec2<f32>, clip: ClipU) -> f32 {
  // Fast path: no clip => full coverage, and NO finvMap (unclipped fragments must cost what they did pre-arena).
  let n = i32(clip.ctrl.x);
  if (n == 0) { return 1.0; }
  // Unrolled with STATIC array indices (n is 1..4). A dynamic uniform-array index (clip.rects[i]) is a GPU perf
  // cliff on some drivers; the common single-clip case (n==1) must cost what the old single-rect clipCov did.
  let fc = finvMap(clip, fcRaw);
  var cov = roundCov(fc, clip.rects[0], clip.radii[0], clip.radiiY[0], clip.ex.x);
  if (n > 1) { cov = cov * roundCov(fc, clip.rects[1], clip.radii[1], clip.radiiY[1], clip.ex.y); }
  if (n > 2) { cov = cov * roundCov(fc, clip.rects[2], clip.radii[2], clip.radiiY[2], clip.ex.z); }
  if (n > 3) { cov = cov * roundCov(fc, clip.rects[3], clip.radii[3], clip.radiiY[3], clip.ex.w); }
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
		TexLog("CreateColorTarget", (uint)w, (uint)h, 1);
		var tex = wgpuDeviceCreateTexture(Dev, &td);
		return wgpuTextureCreateView(tex, null);
	}

	private const string ColoredWgsl = @"
@group(0) @binding(0) var<uniform> clip: ClipU;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>) -> VOut {
  var o: VOut; o.p = xformPos(clip, pos); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.p.xy, clip)); }";

	// Stencil pass (winding only, colour masked). Binds the SHARED ClipU at group 0 for the arena vertex xform so a
	// moved path's fan follows the re-stamped transform; identity for immediate/non-arena draws (fan already NDC).
	private const string PosOnlyWgsl = @"
@group(0) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return xformPos(clip, pos); }
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";

	// TRANSFORM-TABLE variants (path fills only). Vertices are recorded-DEVICE space + a per-vertex slot index into
	// a read-only storage buffer of local->NDC affines (a=ax,ay,az,aw  b=bx,by,_,_) that fold the replay transform
	// AND the device->NDC projection. Recomputing a (tiny) entry per frame repositions a moved/resized visual without
	// re-baking or re-tessellating its fan — so a scroll or a window resize touches only the table, not the verts.
	private const string StencilTableWgsl = @"
struct Xf { a: vec4<f32>, b: vec4<f32> };
@group(0) @binding(0) var<storage, read> xf: array<Xf>;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) ti: u32) -> @builtin(position) vec4<f32> {
  let t = xf[ti];
  return vec4<f32>(pos.x * t.a.x + pos.y * t.a.y + t.a.z, pos.x * t.a.w + pos.y * t.b.x + t.b.y, 0.0, 1.0);
}
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";

	private const string CoverTableWgsl = @"
struct Xf { a: vec4<f32>, b: vec4<f32> };
@group(0) @binding(0) var<storage, read> xf: array<Xf>;
@group(1) @binding(0) var<uniform> clip: ClipU;
struct VOut { @builtin(position) p: vec4<f32>, @location(0) c: vec4<f32> };
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) col: vec4<f32>, @location(2) ti: u32) -> VOut {
  let t = xf[ti];
  var o: VOut; o.p = vec4<f32>(pos.x * t.a.x + pos.y * t.a.y + t.a.z, pos.x * t.a.w + pos.y * t.b.x + t.b.y, 0.0, 1.0); o.c = col; return o;
}
@fragment fn fs(i: VOut) -> @location(0) vec4<f32> { return vec4<f32>(i.c.rgb, i.c.a * clipCov(i.p.xy, clip)); }";

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
		// All three now share the one explicit ClipU layout — a ClipU bind group made with ClipBgl binds to any of them.
		SolidClipBgl = ClipBgl;
		CoverClipBgl = ClipBgl;
		CreatePathTablePipelines(&blend);
		CreateClipDepthPipelines();
		CreateImagePipeline();
		CreateGradientPipeline(&blend);
		CreateRoundedRectPipeline(&blend);
		CreateBlurPipeline();
		CreateCompositePipelines();
	}

	// Fullscreen-triangle depth writers for the in-pass path-clip mask. vs0/vs1 emit the tri at z=0/z=1; the
	// fragment writes nothing (colour masked off) — only depth (and, for the cover variants, the stencil reset).
	private const string ClipDepthWgsl = @"
@vertex fn vs0(@builtin(vertex_index) vi: u32) -> @builtin(position) vec4<f32> {
  var p = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  return vec4<f32>(p[vi], 0.0, 1.0);
}
@vertex fn vs1(@builtin(vertex_index) vi: u32) -> @builtin(position) vec4<f32> {
  var p = array<vec2<f32>, 3>(vec2<f32>(-1.0, -1.0), vec2<f32>(3.0, -1.0), vec2<f32>(-1.0, 3.0));
  return vec4<f32>(p[vi], 1.0, 1.0);
}
@fragment fn fs() -> @location(0) vec4<f32> { return vec4<f32>(0.0, 0.0, 0.0, 0.0); }";

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
			Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.True, DepthCompare = WGPUCompareFunction.Always,
			StencilFront = face, StencilBack = face, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = new WGPUVertexState { Module = module, EntryPoint = vs, BufferCount = 0 },
			Fragment = &fsState, DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
			Layout = IntPtr.Zero,
		};
		return wgpuDeviceCreateRenderPipeline(Dev, &pd);
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
// ctrl.x > 0.5 => downsample (single linear tap = box-average the 2x2 source block, one pyramid level). Otherwise a
// separable FIXED 9-tap gaussian (radius 4, sigma~2) — the requested blur radius is achieved by the pyramid DEPTH
// (sigma-scaled downsample levels), not by a sigma-scaled tap count, so cost is constant instead of O(sigma). The
// FIRST (extract) pass remaps into a sub-rect of the source via srcOrigin/srcScale so only the region behind the
// acrylic element is ever processed; gaussian passes run at identity (srcOrigin=0, srcScale=1) on region textures.
struct BU { dir: vec2<f32>, texel: vec2<f32>, ctrl: vec2<f32>, srcOrigin: vec2<f32>, srcScale: vec2<f32> };
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
  let suv = b.srcOrigin + i.uv * b.srcScale;
  if (b.ctrl.x > 0.5) { return textureSampleLevel(src, smp, suv, 0.0); }
  let o1 = b.dir * b.texel; let o2 = o1 * 2.0; let o3 = o1 * 3.0; let o4 = o1 * 4.0;
  var sum = textureSampleLevel(src, smp, suv, 0.0) * 0.204164;
  sum = sum + (textureSampleLevel(src, smp, suv + o1, 0.0) + textureSampleLevel(src, smp, suv - o1, 0.0)) * 0.180174;
  sum = sum + (textureSampleLevel(src, smp, suv + o2, 0.0) + textureSampleLevel(src, smp, suv - o2, 0.0)) * 0.123832;
  sum = sum + (textureSampleLevel(src, smp, suv + o3, 0.0) + textureSampleLevel(src, smp, suv - o3, 0.0)) * 0.066282;
  sum = sum + (textureSampleLevel(src, smp, suv + o4, 0.0) + textureSampleLevel(src, smp, suv - o4, 0.0)) * 0.027631;
  return sum;
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
struct Grad { header: vec4<f32>, geo: vec4<f32>, colors: array<vec4<f32>, 64>, stops: array<vec4<f32>, 16>, origin: vec4<f32> };
@group(0) @binding(0) var<uniform> g: Grad;
@group(1) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>) -> @builtin(position) vec4<f32> { return xformPos(clip, pos); }
fn stopAt(i: i32) -> f32 { return g.stops[i / 4][i % 4]; }
@fragment fn fs(@builtin(position) fc: vec4<f32>) -> @location(0) vec4<f32> {
  // Arena: map the device fragment back to the recording's own space so the gradient geometry (baked at identity)
  // is correct after a transform re-stamp. Identity finv => gfc == fc.xy for immediate/non-arena draws.
  let gfc = finvMap(clip, fc.xy);
  var t: f32 = 0.0;
  if (g.header.x < 0.5) {
    let a = g.geo.xy; let b = g.geo.zw; let ab = b - a; let denom = dot(ab, ab);
    if (denom > 0.0) { t = dot(gfc - a, ab) / denom; }
  } else {
    // Radial: map the device delta from the (device-space) center into unit-ellipse space via M — the inverse of
    // the gradient's local->device linear map, per-axis normalized by the local radii. M carries rotation, so a
    // rotated elliptical gradient (and an off-centre focal under rotation) is exact, not axis-aligned-approximate.
    // Then param along the ray from the focal origin so t=0 at the origin and t=1 at the ellipse edge.
    let c = g.geo.xy;
    let m = mat2x2<f32>(g.geo.z, g.geo.w, g.origin.z, g.origin.w);
    let pn = m * (gfc - c);
    let on = m * (g.origin.xy - c);
    let dir = pn - on; let aa = dot(dir, dir);
    if (aa < 1e-9) { t = 0.0; }
    else {
      let bb = 2.0 * dot(on, dir); let cc = dot(on, on) - 1.0;
      let s = (-bb + sqrt(max(bb * bb - 4.0 * aa * cc, 0.0))) / (2.0 * aa);
      t = 1.0 / max(s, 1e-6);
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
  return vec4<f32>(col.rgb, col.a * clipCov(fc.xy, clip));
}";

	// Analytic rounded-rect / border-ring fill (ported from ramez's RoundedWgsl). The SDF is evaluated in LOCAL
	// centred space (`p`/`hf`/`radii` interpolated per-vertex) so it's exact under any affine transform; the four
	// device corners only position the quad. `ihalf.x >= 0` = BORDER RING (subtract an inner rounded rect). clipCov
	// applies neutral's analytic rounded/rect clips using the device-pixel builtin position.
	private const string RoundedRectWgsl = @"
struct VSOut { @builtin(position) pos: vec4<f32>, @location(0) p: vec2<f32>, @location(1) hf: vec2<f32>, @location(2) radii: vec4<f32>, @location(3) col: vec4<f32>, @location(4) ihalf: vec2<f32>, @location(5) icenter: vec2<f32>, @location(6) iradii: vec4<f32> };
@group(0) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) cpos: vec2<f32>, @location(1) p: vec2<f32>, @location(2) hf: vec2<f32>, @location(3) radii: vec4<f32>, @location(4) col: vec4<f32>, @location(5) ihalf: vec2<f32>, @location(6) icenter: vec2<f32>, @location(7) iradii: vec4<f32>) -> VSOut {
  var o: VSOut; o.pos = vec4<f32>(cpos, 0.0, 1.0); o.p = p; o.hf = hf; o.radii = radii; o.col = col; o.ihalf = ihalf; o.icenter = icenter; o.iradii = iradii; return o;
}
fn sdRR(p: vec2<f32>, hf: vec2<f32>, radii: vec4<f32>) -> f32 {
  let rTop = select(radii.x, radii.y, p.x > 0.0); let rBot = select(radii.w, radii.z, p.x > 0.0);
  let rad = select(rTop, rBot, p.y > 0.0); let q = abs(p) - hf + vec2<f32>(rad, rad);
  return min(max(q.x, q.y), 0.0) + length(max(q, vec2<f32>(0.0, 0.0))) - rad;
}
@fragment fn fs(i: VSOut) -> @location(0) vec4<f32> {
  let d = sdRR(i.p, i.hf, i.radii); let aa = max(fwidth(d), 1e-4);
  var cov = 1.0 - smoothstep(-aa, aa, d);
  // Compute the inner-rrect SDF + its screen-space derivative in UNIFORM control flow (outside the `if`): WGSL
  // forbids fwidth/derivatives inside non-uniform control flow, and Dawn (browser WebGPU) enforces this strictly
  // even though wgpu-native (desktop) tolerated it. The result is only APPLIED when an inner rect is present.
  let di = sdRR(i.p - i.icenter, i.ihalf, i.iradii); let aai = max(fwidth(di), 1e-4);
  if (i.ihalf.x >= 0.0) { cov = cov * smoothstep(-aai, aai, di); }
  cov = cov * clipCov(i.pos.xy, clip);
  return vec4<f32>(i.col.rgb, i.col.a * cov);
}";

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

	private const string ImageWgsl = @"
struct VOut { @builtin(position) p: vec4<f32>, @location(0) uv: vec2<f32> };
struct U { op: vec4<f32>, tint: vec4<f32>, m0: vec4<f32>, m1: vec4<f32>, m2: vec4<f32>, m3: vec4<f32>, off: vec4<f32> };
@group(0) @binding(0) var tex: texture_2d<f32>;
@group(0) @binding(1) var smp: sampler;
@group(0) @binding(2) var<uniform> u: U;
@group(1) @binding(0) var<uniform> clip: ClipU;
@vertex fn vs(@location(0) pos: vec2<f32>, @location(1) uv: vec2<f32>) -> VOut { var o: VOut; o.p = xformPos(clip, pos); o.uv = uv; return o; }
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
  } else if (u.op.w > 0.5) {
    // Acrylic backdrop composite: blurred backdrop -> luminosity blend (tint = lum rgb/a) -> procedural grain
    // (off.x = noise opacity), opaque within the region. One draw replaces the blurred-image + luminosity overlay.
    var rgb = mix(c.rgb, u.tint.rgb, u.tint.a);
    let nz = (fract(sin(dot(floor(i.p.xy), vec2<f32>(12.9898, 78.233))) * 43758.5453) - 0.5) * 2.0 * u.off.x;
    rgb = clamp(rgb + vec3<f32>(nz), vec3<f32>(0.0), vec3<f32>(1.0));
    c = vec4<f32>(rgb, 1.0);
  }
  return c * u.op.x * clipCov(i.p.xy, clip);
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
		var ds = new WGPUDepthStencilState { Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = WGPUCompareFunction.GreaterEqual, StencilFront = keepFace, StencilBack = keepFace, StencilReadMask = 0, StencilWriteMask = 0 };
		var pd = new WGPURenderPipelineDescriptor { Vertex = vsState, Fragment = &fsState, DepthStencil = &ds, Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None }, Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 }, Layout = IntPtr.Zero };
		ImagePipe = wgpuDeviceCreateRenderPipeline(Dev, &pd);
		ImgBgl = wgpuRenderPipelineGetBindGroupLayout(ImagePipe, 0);
		ImageClipBgl = wgpuRenderPipelineGetBindGroupLayout(ImagePipe, 1);
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = wgpuDeviceCreateSampler(Dev, &sd);
	}

	private IntPtr MakePipe(IntPtr module, WGPUStringView vs, WGPUStringView fs, bool colorWrite, bool colorAttrs, WGPUBlendState* blend, WGPUStencilFaceState front, WGPUStencilFaceState back, uint stencilWrite, uint stencilRead, WGPUCompareFunction depthCompare = WGPUCompareFunction.Always, bool depthWrite = false, IntPtr layout = default)
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
			Format = DepthStencilFormat, DepthWriteEnabled = depthWrite ? WGPUOptionalBool.True : WGPUOptionalBool.False, DepthCompare = depthCompare,
			StencilFront = front, StencilBack = back, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = &ds,
			Primitive = new WGPUPrimitiveState { Topology = WGPUPrimitiveTopology.TriangleList, StripIndexFormat = WGPUIndexFormat.Undefined, FrontFace = WGPUFrontFace.CCW, CullMode = WGPUCullMode.None },
			Multisample = new WGPUMultisampleState { Count = MsaaSamples, Mask = uint.MaxValue, AlphaToCoverageEnabled = 0 },
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
			Format = DepthStencilFormat, DepthWriteEnabled = WGPUOptionalBool.False, DepthCompare = depthCompare,
			StencilFront = front, StencilBack = back, StencilReadMask = stencilRead, StencilWriteMask = stencilWrite,
		};
		var pd = new WGPURenderPipelineDescriptor
		{
			Vertex = vsState, Fragment = &fsState, DepthStencil = &ds,
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
	private static void OnAdapter(WGPURequestAdapterStatus status, IntPtr adapter, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = adapter;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDevice(WGPURequestDeviceStatus status, IntPtr device, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = device;

	// Non-fatal device error handler: wgpu's DEFAULT uncaptured-error handler panics the process, which turned any
	// stray validation error (e.g. an unsupported MSAA count) into a hard crash. Log and keep running like the
	// reference host. Static + [UnmanagedCallersOnly] so the pointer stays valid for the device's lifetime.
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnUncapturedError(IntPtr device, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var msg = message.Data != IntPtr.Zero && message.Length > 0
			? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(message.Data, (int)message.Length)
			: "";
		System.Console.Error.WriteLine($"[webgpu] uncaptured error ({type}): {msg}");
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMap(WGPUMapAsyncStatus status, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((bool[])GCHandle.FromIntPtr(u1).Target!)[0] = true;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnPopErrorScope(WGPUPopErrorScopeStatus status, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var box = (uint[])GCHandle.FromIntPtr(u1).Target!;
		box[0] = 1; box[1] = (uint)type;
	}

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

// Detailed per-frame profiler (UNO_WEBGPU_PROFILE=1). Accumulates phase timings + operation counts over a window
// of frames and logs one line per window, so a single run pinpoints the bottleneck — CPU record vs CPU encode vs
// the GPU poll-drain vs present vs offscreen count vs allocation — without another round-trip. All timings are ms.
// Ordered, human-readable trace of exactly what each frame submits to the GPU (passes, pipelines, draws, uploads),
// gated by UNO_WEBGPU_TRACE. Pure logging — no behaviour change. Used to prove per-primitive GPU-submission parity
// against the original ramez/webgpu-experiment backend. Reset() at frame start, Dump() to read the accumulated trace.
public static class WebGpuTrace
{
	public static readonly bool Enabled = Environment.GetEnvironmentVariable("UNO_WEBGPU_TRACE") is "1" or "true";
	private static readonly System.Text.StringBuilder _sb = new();
	private static int _depth;

	public static void Reset() { if (!Enabled) { return; } _sb.Clear(); _depth = 0; }

	public static void Pass(string kind, int w, int h, uint msaa, bool clear)
	{
		if (!Enabled) { return; }
		_sb.Append(' ', _depth * 2).Append("PASS ").Append(kind).Append(' ').Append(w).Append('x').Append(h)
			.Append(" msaa=").Append(msaa).Append(clear ? " clear" : " load").Append('\n');
		_depth++;
	}

	public static void PassEnd()
	{
		if (!Enabled) { return; }
		_depth = Math.Max(0, _depth - 1);
		_sb.Append(' ', _depth * 2).Append("PASS end\n");
	}

	public static void Draw(string pipe, uint vtx)
	{
		if (!Enabled) { return; }
		_sb.Append(' ', _depth * 2).Append("DRAW ").Append(pipe).Append(" v=").Append(vtx).Append('\n');
	}

	public static void Upload(string what, int bytes)
	{
		if (!Enabled) { return; }
		_sb.Append(' ', _depth * 2).Append("UPLOAD ").Append(what).Append(' ').Append(bytes).Append("B\n");
	}

	public static string Dump() => Enabled ? _sb.ToString() : "";
}

public sealed class WebGpuProfiler
{
	public static readonly bool Enabled = Environment.GetEnvironmentVariable("UNO_WEBGPU_PROFILE") is "1" or "true";
	private static readonly double Ms = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
	private const int Window = 60;
	private int _n;

	// Window sums (ticks) + single-frame peaks.
	private long _frameReq, _replay, _present, _beginFrame, _render, _submit, _poll, _acquire, _blit, _surface;
	private long _pkFrame, _pkPoll, _pkRender, _pkPresent;
	// Window count sums.
	private long _cmds, _ops, _draws, _osLayer, _osBackdrop, _osCov, _osShadow, _backReCmds, _texCreate, _rent, _bgHit, _bgMiss, _bufNew, _upBytes;

	// Per-frame temporaries (reset in FrameEnd after aggregation; FrameStart also resets for cleanliness — so a
	// missing/unpaired FrameStart can't stall logging). No gate: adders always accumulate.
	private long _tFrameReq, _tReplay, _tPresent, _tBeginFrame, _tRender, _tSubmit, _tPoll, _tAcquire, _tBlit, _tSurface;
	private long _cCmds, _cOps, _cDraws, _cOsLayer, _cOsBackdrop, _cOsCov, _cOsShadow, _cBackReCmds, _cTexCreate, _cRent, _cBgHit, _cBgMiss, _cBufNew, _cUpBytes;
	// Per-kind draw counts (which draws dominate a real scene: Solid/Path/Image/Gradient/Composite/Clip) — the lever
	// for deciding where cross-visual coalescing pays off. `_cDCoal` counts solid ops folded away by coalescing.
	private long _cDSolid, _cDPath, _cDImage, _cDGrad, _cDComp, _cDClip, _cDCoal, _cDRr;
	private long _dSolid, _dPath, _dImage, _dGrad, _dComp, _dClip, _dCoal, _dRr;
	// Window-level GC + wall clock marks (measured across the window, not per frame, so no FrameStart dependency).
	private long _allocMark; private int _g0Mark, _g1Mark, _g2Mark; private long _flushMark; private bool _started;

	public static long T() => System.Diagnostics.Stopwatch.GetTimestamp();

	private void ResetFrameTemps()
	{
		_tFrameReq = _tReplay = _tPresent = _tBeginFrame = _tRender = _tSubmit = _tPoll = _tAcquire = _tBlit = _tSurface = 0;
		_cCmds = _cOps = _cDraws = _cOsLayer = _cOsBackdrop = _cOsCov = _cOsShadow = _cBackReCmds = _cTexCreate = _cRent = _cBgHit = _cBgMiss = _cBufNew = _cUpBytes = 0;
		_cDSolid = _cDPath = _cDImage = _cDGrad = _cDComp = _cDClip = _cDCoal = _cDRr = 0;
	}

	public void FrameStart() => ResetFrameTemps();

	// Timing adders — pass a start timestamp captured with T().
	public void FrameRequested(long t0) => _tFrameReq += T() - t0;
	public void Replayed(long t0) => _tReplay += T() - t0;
	public void Presented(long t0) => _tPresent += T() - t0;
	public void BeginFrameT(long t0) => _tBeginFrame += T() - t0;
	public void Render(long t0) => _tRender += T() - t0;
	public void Submit(long t0) => _tSubmit += T() - t0;
	public void Poll(long t0) => _tPoll += T() - t0;
	public void Acquire(long t0) => _tAcquire += T() - t0;
	public void Blit(long t0) => _tBlit += T() - t0;
	public void Surface(long t0) => _tSurface += T() - t0;

	// Counters.
	public void Cmds(int n) => _cCmds += n;
	public void Ops(int n) => _cOps += n;
	public void Draw() => _cDraws++;
	// kind: 0=solid 1=path 2=image 3=gradient 4=composite 5=clip. `coalesced` = solid ops merged into one draw.
	public void DrawKind(int kind)
	{
		switch (kind)
		{
			case 0: _cDSolid++; break;
			case 1: _cDPath++; break;
			case 2: _cDImage++; break;
			case 3: _cDGrad++; break;
			case 4: _cDComp++; break;
			case 5: _cDClip++; break;
			case 6: _cDRr++; break;
		}
	}
	public void Coalesced(int n) => _cDCoal += n;
	public void OsLayer() => _cOsLayer++;
	public void OsBackdrop(int reCmds) { _cOsBackdrop++; _cBackReCmds += reCmds; }
	public void OsCov() => _cOsCov++;
	public void OsShadow() => _cOsShadow++;
	public void TexCreate() => _cTexCreate++;
	public void Rent() => _cRent++;
	public void BgHit() => _cBgHit++;
	public void BgMiss() => _cBgMiss++;
	public void BufNew() => _cBufNew++;
	public void Upload(long bytes) => _cUpBytes += bytes;

	// Called once per on-window frame (from the swapchain Present — the last frame step). Aggregates the frame's
	// temporaries and logs one line every 30 frames OR every ~1s (whichever first — so even at very low fps a line
	// appears within a second).
	public void FrameEnd()
	{
		if (!_started) { _started = true; _allocMark = GC.GetAllocatedBytesForCurrentThread(); _g0Mark = GC.CollectionCount(0); _g1Mark = GC.CollectionCount(1); _g2Mark = GC.CollectionCount(2); _flushMark = T(); }
		_frameReq += _tFrameReq; _replay += _tReplay; _present += _tPresent; _beginFrame += _tBeginFrame; _render += _tRender; _submit += _tSubmit; _poll += _tPoll; _acquire += _tAcquire; _blit += _tBlit; _surface += _tSurface;
		var frame = _tFrameReq + _tPresent; if (frame > _pkFrame) { _pkFrame = frame; } if (_tPoll > _pkPoll) { _pkPoll = _tPoll; } if (_tRender > _pkRender) { _pkRender = _tRender; } if (_tPresent > _pkPresent) { _pkPresent = _tPresent; }
		_cmds += _cCmds; _ops += _cOps; _draws += _cDraws; _osLayer += _cOsLayer; _osBackdrop += _cOsBackdrop; _osCov += _cOsCov; _osShadow += _cOsShadow; _backReCmds += _cBackReCmds; _texCreate += _cTexCreate; _rent += _cRent; _bgHit += _cBgHit; _bgMiss += _cBgMiss; _bufNew += _cBufNew; _upBytes += _cUpBytes;
		_dSolid += _cDSolid; _dPath += _cDPath; _dImage += _cDImage; _dGrad += _cDGrad; _dComp += _cDComp; _dClip += _cDClip; _dCoal += _cDCoal; _dRr += _cDRr;
		ResetFrameTemps();
		_n++;
		if (_n >= 30 || (T() - _flushMark) * Ms >= 1000.0) { Flush(); }
	}

	private void Flush()
	{
		if (_n == 0) { return; }
		double A(long ticks) => ticks * Ms / _n;   // avg ms/frame
		double Pk(long ticks) => ticks * Ms;        // single-frame peak ms
		long C(long c) => c / _n;                   // avg count/frame
		long alloc = GC.GetAllocatedBytesForCurrentThread() - _allocMark;
		int g0 = GC.CollectionCount(0) - _g0Mark, g1 = GC.CollectionCount(1) - _g1Mark, g2 = GC.CollectionCount(2) - _g2Mark;
		// frameReq (host DrawFrame) wraps record+replay; if it wasn't measured (e.g. a non-Win32 host or the smoke),
		// fall back to replay+present so FRAME/fps/record stay sane instead of 0/Infinity/negative.
		var totalTicks = _frameReq >= _replay ? _frameReq + _present : _replay + _present;
		var record = _frameReq > _replay ? _frameReq - _replay : 0;
		var frameMs = A(totalTicks);
		var fps = frameMs > 0.001 ? 1000.0 / frameMs : 0;
		System.Console.WriteLine(
			$"[webgpu-profile] n={_n} FRAME={frameMs:F2}ms (~{fps:F0}fps: record={A(record):F2} replay={A(_replay):F2} present={A(_present):F2}) | " +
			$"replay[beginFrame={A(_beginFrame):F2} render={A(_render):F2} submit={A(_submit):F2} poll={A(_poll):F2}] " +
			$"present[acquire={A(_acquire):F2} blit={A(_blit):F2} surface={A(_surface):F2}] | " +
			$"cnt/f: cmds={C(_cmds)} ops={C(_ops)} draws={C(_draws)}[S{C(_dSolid)} RR{C(_dRr)} P{C(_dPath)} I{C(_dImage)} G{C(_dGrad)} C{C(_dComp)} Clip{C(_dClip)} coal-{C(_dCoal)}] offscr={C(_osLayer + _osBackdrop + _osCov + _osShadow)}(L{C(_osLayer)} B{C(_osBackdrop)} Cov{C(_osCov)} Sh{C(_osShadow)}) backdropReCmds={C(_backReCmds)} texCreate={C(_texCreate)} rent={C(_rent)} bg(hit={C(_bgHit)} miss={C(_bgMiss)}) bufNew={C(_bufNew)} upload={C(_upBytes) / 1024}KB | " +
			$"gc: alloc={alloc / _n / 1024}KB/f gen0={g0} gen1={g1} gen2={g2} | peak: FRAME={Pk(_pkFrame):F2} render={Pk(_pkRender):F2} poll={Pk(_pkPoll):F2} present={Pk(_pkPresent):F2}");
		System.Console.Out.Flush();
		_n = 0;
		_frameReq = _replay = _present = _beginFrame = _render = _submit = _poll = _acquire = _blit = _surface = 0;
		_pkFrame = _pkPoll = _pkRender = _pkPresent = 0;
		_cmds = _ops = _draws = _osLayer = _osBackdrop = _osCov = _osShadow = _backReCmds = _texCreate = _rent = _bgHit = _bgMiss = _bufNew = _upBytes = 0;
		_dSolid = _dPath = _dImage = _dGrad = _dComp = _dClip = _dCoal = _dRr = 0;
		_allocMark = GC.GetAllocatedBytesForCurrentThread(); _g0Mark = GC.CollectionCount(0); _g1Mark = GC.CollectionCount(1); _g2Mark = GC.CollectionCount(2); _flushMark = T();
	}
}

// Transient GPU-texture pool for the per-frame offscreens (shadow/backdrop/layer/path-coverage surfaces + blur
// temps). BeginFrame marks all entries free; Rent reuses a free entry matching the key or creates one — so a
// steady-state frame allocates nothing. Every renter clears (LoadOp.Clear) before writing, so reuse is safe.
// (These offscreens stay "in use" until the frame's main pass samples them; reuse happens across frames.)
public sealed unsafe class WebGpuTexturePool : IDisposable
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Tex; public IntPtr View; public int W, H, Samples; public WGPUTextureFormat Fmt; public WGPUTextureUsage Usage; public bool InUse; public int LastUsed; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// The pool is shared per-device, so an off-loop render (e.g. RenderTargetBitmap) can hit it concurrently
	// with the on-window render loop. Guard mutation/enumeration so a concurrent Add can't invalidate Rent's walk.
	private readonly object _gate = new();
	private int _frameNo;
	// Release entries not rented for this many frames. Without eviction, every window resize strands a whole
	// generation of full-window MSAA colour + depth textures (they no longer match a Rent key) until process exit.
	private const int EvictAfterFrames = 16;

	public WebGpuTexturePool(WebGpuDevice d) => _d = d;

	public void BeginFrame()
	{
		lock (_gate)
		{
			for (int i = _entries.Count - 1; i >= 0; i--)
			{
				var e = _entries[i];
				if (!e.InUse && _frameNo - e.LastUsed > EvictAfterFrames)
				{
					if (e.View != IntPtr.Zero) { wgpuTextureViewRelease(e.View); }
					if (e.Tex != IntPtr.Zero) { wgpuTextureDestroy(e.Tex); }
					_entries.RemoveAt(i);
				}
				else { e.InUse = false; }
			}
			_frameNo++;
		}
	}

	public IntPtr Rent(int w, int h, int samples, WGPUTextureUsage usage, WGPUTextureFormat fmt)
	{
		lock (_gate)
		{
			_d.Profiler?.Rent();
			foreach (var e in _entries)
			{
				if (!e.InUse && e.W == w && e.H == h && e.Samples == samples && e.Fmt == fmt && e.Usage == usage) { e.InUse = true; e.LastUsed = _frameNo; return e.View; }
			}
			_d.Profiler?.TexCreate();
			var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = fmt, MipLevelCount = 1, SampleCount = (uint)samples, Dimension = WGPUTextureDimension._2D, Usage = usage };
			WebGpuDevice.TexLog("Pool.Rent", (uint)w, (uint)h, (uint)samples);
			var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
			var view = wgpuTextureCreateView(tex, null);
			_entries.Add(new Entry { Tex = tex, View = view, W = w, H = h, Samples = samples, Fmt = fmt, Usage = usage, InUse = true, LastUsed = _frameNo });
			return view;
		}
	}

	/// <summary>Marks a rented view free again so it can be re-rented within the SAME frame. Used for the depth/
	/// stencil target, which is written only inside its own (already-ended) render pass and never sampled after —
	/// so one depth texture per size is reused across all of a frame's offscreen passes + the main pass.</summary>
	public void Return(IntPtr view)
	{
		if (view == IntPtr.Zero) { return; }
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { e.InUse = false; return; } } }
	}

	/// <summary>The backing texture for a rented view, or Zero if unknown. Used to flush an offscreen's resolve so
	/// a later, separately-submitted pass can sample it.</summary>
	public IntPtr TexForView(IntPtr view)
	{
		lock (_gate) { foreach (var e in _entries) { if (e.View == view) { return e.Tex; } } }
		return IntPtr.Zero;
	}

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _entries)
			{
				if (e.View != IntPtr.Zero) { wgpuTextureViewRelease(e.View); }
				if (e.Tex != IntPtr.Zero) { wgpuTextureDestroy(e.Tex); }
			}
			_entries.Clear();
		}
	}
}

// Transient GPU-buffer pool (vertex + uniform buffers). Like the texture pool: BeginFrame frees all; Rent
// reuses a free buffer of the same usage with enough capacity or creates one, so a steady-state frame allocates
// no buffers. Callers QueueWriteBuffer their data before use.
public sealed unsafe class WebGpuBufferPool : IDisposable
{
	private readonly WebGpuDevice _d;
	private sealed class Entry { public IntPtr Buf; public int Cap; public WGPUBufferUsage Usage; public bool InUse; }
	private readonly System.Collections.Generic.List<Entry> _entries = new();
	// Shared per-device; guard against concurrent Add invalidating Rent's enumeration (see WebGpuTexturePool).
	private readonly object _gate = new();

	public WebGpuBufferPool(WebGpuDevice d) => _d = d;

	public void BeginFrame() { lock (_gate) { foreach (var e in _entries) { e.InUse = false; } } }

	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _entries) { if (e.Buf != IntPtr.Zero) { wgpuBufferRelease(e.Buf); } }
			_entries.Clear();
		}
	}

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
			_d.PerfBufCreates++;
			_d.Profiler?.BufNew();
			_entries.Add(new Entry { Buf = buf, Cap = cap, Usage = usage, InUse = true });
			return buf;
		}
	}
}

public sealed unsafe class WebGpuRenderSurface : IWebGpuRenderTarget
{
	public IntPtr Tex;
	public IntPtr View;              // single-sample resolve target (offscreen readback / swapchain image)
	public IntPtr MsaaColorTex;
	public IntPtr MsaaColorView;     // multisampled color the pass renders into, resolved into View
	public IntPtr DepthTex;
	public IntPtr DepthView;         // multisampled depth/stencil (clip mask + stencil-then-cover)
	public int Width { get; }
	public int Height { get; }
	// True when MSAA colour + depth were rented from the transient pool. Both are write-only within this surface's
	// own render pass (the MSAA colour resolves into View, depth is discarded) and never sampled afterwards, so
	// once the pass ends they can be returned to the pool and reused by the next same-size offscreen/main pass —
	// only the single-sample resolve View must stay live (it's sampled later as a layer/coverage/backdrop texture).
	public bool Pooled { get; private set; }
	public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;

	// Pooled surfaces rent their views from the WebGpuTexturePool, which owns and reclaims them — Dispose must not
	// touch those. Directly-created surfaces (offscreen readback / swapchain resolve) own their textures and MUST
	// release them, otherwise every window resize leaks a full-window MSAA color + depth texture until VRAM is
	// exhausted (wgpuDeviceCreateTexture: "Not enough memory left").
	private readonly bool _ownsResources = true;
	// For a swapchain surface the colour View/Tex are the per-frame acquired swapchain image, borrowed from the
	// context (WebGpuSwapChainContext) which releases them in Present. Only the MSAA+depth are owned here, so
	// Dispose (on resize) must NOT release the borrowed colour — doing so double-frees the swapchain view.
	private readonly bool _ownsColor = true;

	public void Dispose()
	{
		if (!_ownsResources)
		{
			return;
		}
		if (_ownsColor)
		{
			if (View != IntPtr.Zero) { wgpuTextureViewRelease(View); View = IntPtr.Zero; }
			if (Tex != IntPtr.Zero) { wgpuTextureDestroy(Tex); Tex = IntPtr.Zero; }
		}
		// At 1x MsaaColorView aliases the (already-released) View and there is no MSAA texture — only release it when
		// it's a distinct multisampled texture (MsaaColorTex set).
		if (MsaaColorTex != IntPtr.Zero) { wgpuTextureViewRelease(MsaaColorView); MsaaColorView = IntPtr.Zero; wgpuTextureDestroy(MsaaColorTex); MsaaColorTex = IntPtr.Zero; }
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
		WebGpuDevice.TexLog("Surface.color", (uint)width, (uint)height, 1);
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		CreateMultisampledTargets(device, width, height);
	}

	// External-color variant for a swapchain: the color View/Tex are provided per frame (the acquired
	// swapchain image, used as the resolve target); the multisampled color + depth are owned here.
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, bool externalColor)
	{
		Width = width; Height = height;
		_ownsColor = false;   // View/Tex are the borrowed swapchain image (set per frame); only MSAA+depth are owned
		CreateMultisampledTargets(device, width, height);
	}

	// Pooled transient offscreen: MSAA color + depth + a single-sample resolve target (sampled later), all rented
	// from the pool so a steady-state frame allocates nothing. Dispose is a no-op (the pool reclaims on BeginFrame).
	public WebGpuRenderSurface(WebGpuDevice device, int width, int height, WebGpuTexturePool pool)
	{
		Width = width; Height = height;
		_ownsResources = false;   // the pool owns and reclaims these; Dispose must not release them
		Pooled = true;
		DepthView = pool.Rent(width, height, (int)device.MsaaSamples, WGPUTextureUsage.RenderAttachment, WebGpuDevice.DepthStencilFormat);
		// CopySrc so the resolved result can be read back (ReadPixelsRgba) for RenderTargetBitmap / offscreen.
		View = pool.Rent(width, height, 1, WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopySrc, device.ColorFormat);
		Tex = pool.TexForView(View);
		// 1x: no separate MSAA colour — the pass renders straight into the single-sample View (no resolve). Otherwise
		// the pass renders into a multisampled colour that resolves into View. MsaaColorView aliases View at 1x, so the
		// pool-return/Dispose paths must NOT free it as if it were a distinct texture (guarded on MsaaSamples>1).
		MsaaColorView = device.MsaaSamples > 1
			? pool.Rent(width, height, (int)device.MsaaSamples, WGPUTextureUsage.RenderAttachment, device.ColorFormat)
			: View;
	}

	// Hands the resolved single-sample color texture/view to a longer-lived owner (RenderOffscreen → IImageTexture)
	// and nulls them here so Dispose releases only the (now-finished) MSAA + depth targets. Only valid on a
	// resource-owning surface (the dedicated ctor), after the render has been submitted+resolved.
	internal (IntPtr tex, IntPtr view) DetachColor()
	{
		var t = Tex; var v = View;
		Tex = IntPtr.Zero; View = IntPtr.Zero;
		return (t, v);
	}

	private void CreateMultisampledTargets(WebGpuDevice device, int width, int height)
	{
		// 1x: no multisampled colour — the pass renders straight into the single-sample View (no resolve). For the
		// swapchain external-colour surface View is set per frame, so MsaaColorView is aliased to it there.
		if (device.MsaaSamples > 1)
		{
			var cd = new WGPUTextureDescriptor
			{
				Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 }, Format = device.ColorFormat,
				MipLevelCount = 1, SampleCount = device.MsaaSamples, Dimension = WGPUTextureDimension._2D,
				Usage = WGPUTextureUsage.RenderAttachment,
			};
			WebGpuDevice.TexLog("Surface.msaa", (uint)width, (uint)height, device.MsaaSamples);
			MsaaColorTex = wgpuDeviceCreateTexture(device.Dev, &cd);
			MsaaColorView = wgpuTextureCreateView(MsaaColorTex, null);
		}
		else
		{
			MsaaColorView = View;   // Zero for the swapchain ctor (View set per frame) — aliased in the context
		}

		var dd = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)width, Height = (uint)height, DepthOrArrayLayers = 1 }, Format = WebGpuDevice.DepthStencilFormat,
			MipLevelCount = 1, SampleCount = device.MsaaSamples, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.RenderAttachment,
		};
		WebGpuDevice.TexLog("Surface.depth", (uint)width, (uint)height, device.MsaaSamples);
		DepthTex = wgpuDeviceCreateTexture(device.Dev, &dd);
		DepthView = wgpuTextureCreateView(DepthTex, null);
	}
}

// A clip is a device-space scissor AABB (fast reject + plain-rect clip) plus an optional device-space,
// axis-aligned rounded-rect whose corners are masked per-fragment in the shaders. A rotated rounded clip
// degrades to its AABB (the exact fix is clip-local-space eval, as with the radial gradient — follow-up).
// A single analytic rounded-rect clip (device space). Nested clips stack in ClipData.Rounds and are ANDed in-shader.

internal sealed class OwnedResources
{
	public System.Collections.Generic.List<nint> Buffers = new();
	public System.Collections.Generic.List<nint> BindGroups = new();
}

// One draw op in a pass's ordered list. Was a 7-tuple; promoted to a struct so glyph coalescing can carry the extra
// fields (a shared glyph-fan-buffer start + the fill colour) without threading a wider tuple through ~30 sites. The
// lowercase field names + Deconstruct keep the existing `var (kind, b0, ...) = op` destructuring and `.kind`/`.b0`
// access working unchanged. kind: 0=rect 1=path 2=image 3=gradient 5=rrect. For a coalesced-glyph path op (kind 1),
// GlyphFanStart>=0 marks the fan as living in the pass's shared glyph buffer at that start vertex (b0 unused),
// and Color is the run colour (coalescing merges same-Color+same-clip stencils).

public sealed unsafe class WebGpuSlab
{
	private readonly WebGpuDevice _d;
	private readonly int _stride;                 // floats per vertex
	private readonly WebGpuVertexSlab _alloc = new();
	private readonly List<float> _shadow = new();
	private readonly HashSet<long> _live = new();
	public IntPtr Buf;                             // persistent GPU buffer (Vertex | CopyDst)
	private int _bufVerts;                         // GPU buffer capacity in vertices

	public WebGpuSlab(WebGpuDevice d, int strideFloats) { _d = d; _stride = strideFloats; }

	public void BeginFrame() => _live.Clear();
	public void MarkLive(long id) => _live.Add(id);
	public void EndFrame() => _alloc.RetainOnly(_live);
	public int ByteOffset(long id) => (_alloc.TryGet(id, out var s) ? s.Off : 0) * _stride * sizeof(float);

	// Reserve/reuse `id`'s stable slice, and upload ONLY what changed: the whole shadow if the buffer had to grow,
	// otherwise a dirty diff against the CPU shadow — skip the write entirely when byte-identical (static UI), else
	// write only the changed [lo..hi] sub-range. Returns the slice's BYTE offset.
	public int Put(long id, System.Collections.Generic.List<float> verts)
	{
		_live.Add(id);
		int vcount = verts.Count / _stride;
		int voff = _alloc.Ensure(id, vcount, out _);
		int capVerts = _alloc.Capacity;
		int needFloats = capVerts * _stride;
		if (_shadow.Count < needFloats) { System.Runtime.InteropServices.CollectionsMarshal.SetCount(_shadow, needFloats); }
		var dst = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_shadow);
		var src = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(verts);
		int byteOff = voff * _stride * sizeof(float);
		int n = verts.Count;
		var slot = dst.Slice(voff * _stride, n);
		if (Buf == IntPtr.Zero || _bufVerts < capVerts)
		{
			// (Re)allocate the persistent buffer (1.5x headroom already in the allocator) and upload the whole shadow.
			src.CopyTo(slot);
			if (Buf != IntPtr.Zero) { _d.DeferReleaseBuffer(Buf); }
			_bufVerts = capVerts;
			var bd = new WGPUBufferDescriptor { Size = (nuint)(_bufVerts * _stride * sizeof(float)), Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
			Buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
			fixed (float* p = dst) { wgpuQueueWriteBuffer(_d.Q, Buf, 0, (IntPtr)p, (nuint)(needFloats * sizeof(float))); }
			_d.Profiler?.Upload(needFloats * sizeof(float));
			return byteOff;
		}
		// Dirty diff vs the shadow: first/last changed float. Identical → nothing to upload (the common static case).
		int lo = 0; while (lo < n && slot[lo] == src[lo]) { lo++; }
		if (lo == n) { return byteOff; }
		int hi = n - 1; while (hi > lo && slot[hi] == src[hi]) { hi--; }
		int len = hi - lo + 1;
		src.Slice(lo, len).CopyTo(slot.Slice(lo, len));
		fixed (float* p = &dst[voff * _stride + lo]) { wgpuQueueWriteBuffer(_d.Q, Buf, (nuint)(byteOff + lo * sizeof(float)), (IntPtr)p, (nuint)(len * sizeof(float))); }
		_d.Profiler?.Upload(len * sizeof(float));
		return byteOff;
	}
}

// One entry in a frame-solid recording's ordered emit list: a solid/rrect run (relative vert start into the
// recording's cached SolidVerts/RrectVerts) or a spliced non-solid op (glyph/image/gradient).

public interface IWebGpuDeviceContext
{
	WebGpuDevice Device { get; }
}

/// <summary>The registerable WebGPU backend pair. Prefers a WebGPU context; needs an 8-bit stencil for path fills.</summary>

public static class WebGpuContext
{
	private static float NormalizeScale(float scale) => scale <= 0 ? 1f : scale;

	/// <summary>Win32 HWND swapchain context.</summary>
	public static IGraphicsContext CreateWin32(nint hwnd, nint hinstance, float rasterizationScale)
	{
		// Set before the swapchain context creates the device (it reads this for its DPI-scaled targets).
		WebGpuDevice.RasterizationScale = NormalizeScale(rasterizationScale);
		return new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateHwndSurface(inst, hinstance, hwnd));
	}

	/// <summary>X11 window swapchain context.</summary>
	public static IGraphicsContext CreateX11(nint display, nint window, float rasterizationScale)
	{
		WebGpuDevice.RasterizationScale = NormalizeScale(rasterizationScale);
		return new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateXlibSurface(inst, display, (ulong)window));
	}

	/// <summary>Metal (<c>CAMetalLayer</c>) swapchain context — macOS and iOS/tvOS.</summary>
	public static IGraphicsContext CreateMetal(nint caMetalLayer, float rasterizationScale)
	{
		WebGpuDevice.RasterizationScale = NormalizeScale(rasterizationScale);
		return new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateMetalSurface(inst, caMetalLayer));
	}

	/// <summary>Android <c>ANativeWindow</c> swapchain context.</summary>
	public static IGraphicsContext CreateAndroid(nint aNativeWindow, float rasterizationScale)
	{
		WebGpuDevice.RasterizationScale = NormalizeScale(rasterizationScale);
		return new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateAndroidSurface(inst, aNativeWindow));
	}

	/// <summary>
	/// Browser (canvas) context. Asynchronous because the device is created in JavaScript (navigator.gpu) and
	/// imported into emdawnwebgpu's C handle table — the in-WASM wgpuInstanceProcessEvents pump hangs when driven
	/// from a managed call stack — and the JS thread must not be blocked. Returns null if the device import fails.
	/// </summary>
	public static async Task<IGraphicsContext> CreateWasmAsync(string canvasId)
	{
		var inst = WebGpuDevice.CreateInstancePtr();
		var devPtr = await WebGpuJsInterop.CreateImportedDeviceAsync((int)inst);
		if (devPtr == 0)
		{
			return null;
		}

		var device = WebGpuDevice.FromImported(WGPUTextureFormat.RGBA8Unorm, inst, (IntPtr)devPtr);
		// Install the JS readback hook (GPU->CPU can't block the browser thread).
		WebGpuDevice.BrowserReadbackAsync = async (buf, len) =>
			Convert.FromBase64String(await WebGpuJsInterop.MapReadBase64Async((int)buf, len));
		return new WebGpuBrowserGraphicsContext(device, canvasId ?? "");
	}
}

// --- Device-bound factory (IImageTexture + eventual shaders) ---

/// <summary>A wgpu texture uploaded once from a neutral <see cref="IImage"/>'s pixels. Owned/disposed by the framework.</summary>

// Per-visual STABLE slice allocator over a persistent per-kind vertex buffer (ported from ramez's
// WebGpuVertexSlab). Each visual (keyed by its recording's command-list identity) gets a fixed offset+capacity in
// a shared GPU buffer, so a content change rewrites its slice IN PLACE (stable offset → dirty only that byte
// range) and geometry is RESIDENT across frames (no re-upload for a static visual). Holds CPU metadata only.
internal sealed class WebGpuVertexSlab
{
	internal struct Slice { public int Off; public int Cap; public int Len; }   // in vertices (caller's stride)
	private readonly Dictionary<long, Slice> _map = new();
	private readonly List<(int off, int cap)> _free = new();
	private int _cap;                       // high-water = the buffer length (in vertices) to size to
	private readonly List<long> _toFree = new();

	internal int Capacity => _cap;
	internal void Reset() { _map.Clear(); _free.Clear(); _cap = 0; }
	internal bool TryGet(long id, out Slice s) => _map.TryGetValue(id, out s);

	// Reserve `verts` vertices for `id`, reusing its slot when it still fits (stable offset), else best-fit/grow.
	// Returns the VERTEX offset. `grew` is set when the high-water advanced (the GPU buffer must be (re)allocated).
	internal int Ensure(long id, int verts, out bool grew)
	{
		grew = false;
		if (_map.TryGetValue(id, out var s))
		{
			if (s.Cap >= verts) { s.Len = verts; _map[id] = s; return s.Off; }
			_free.Add((s.Off, s.Cap));   // outgrew → reclaim, reallocate below
		}
		int want = verts + (verts >> 1);   // 1.5x slack so small growth doesn't realloc next frame
		int bestI = -1, bestCap = int.MaxValue;
		for (int i = 0; i < _free.Count; i++) { if (_free[i].cap >= verts && _free[i].cap < bestCap) { bestI = i; bestCap = _free[i].cap; } }
		int off, capAlloc;
		if (bestI >= 0) { off = _free[bestI].off; capAlloc = _free[bestI].cap; _free.RemoveAt(bestI); }
		else { off = _cap; capAlloc = want; _cap += want; grew = true; }
		_map[id] = new Slice { Off = off, Cap = capAlloc, Len = verts };
		return off;
	}

	internal void Free(long id) { if (_map.TryGetValue(id, out var s)) { _free.Add((s.Off, s.Cap)); _map.Remove(id); } }

	// Free slices of visuals not present this frame (returns their capacity to the free list). `live` = this frame's ids.
	internal void RetainOnly(HashSet<long> live)
	{
		_toFree.Clear();
		foreach (var id in _map.Keys) { if (!live.Contains(id)) { _toFree.Add(id); } }
		foreach (var id in _toFree) { Free(id); }
	}
}
