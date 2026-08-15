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


// One entry in a frame-solid recording's ordered emit list: a solid/rrect run (relative vert start into the
// recording's cached SolidVerts/RrectVerts) or a spliced non-solid op (glyph/image/gradient).



/// <summary>The host-facing WebGPU context factories. Each builds the device bring-up (WebGpuInitDevice) + a
/// swapchain/browser context and returns it as a neutral ISwapChain — naming no renderer type. The
/// rasterizationScale argument is retained for the host-facing signature but no longer influences init (MSAA is
/// chosen by capability, and the per-frame DPI scale is applied by the drawing session's matrix).</summary>
internal static class WebGpuContext
{
	/// <summary>Win32 HWND swapchain context.</summary>
	public static ISwapChain CreateWin32(nint hwnd, nint hinstance, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateHwndSurface(inst, hinstance, hwnd));

	/// <summary>X11 window swapchain context.</summary>
	public static ISwapChain CreateX11(nint display, nint window, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateXlibSurface(inst, display, (ulong)window));

	/// <summary>Metal (<c>CAMetalLayer</c>) swapchain context — macOS and iOS/tvOS.</summary>
	public static ISwapChain CreateMetal(nint caMetalLayer, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateMetalSurface(inst, caMetalLayer));

	/// <summary>Android <c>ANativeWindow</c> swapchain context.</summary>
	public static ISwapChain CreateAndroid(nint aNativeWindow, float rasterizationScale)
		=> new WebGpuSwapChainContext(WGPUTextureFormat.BGRA8Unorm, inst => WebGpuSwapChainContext.CreateAndroidSurface(inst, aNativeWindow));

	/// <summary>
	/// Browser (canvas) context. Asynchronous because the device is created in JavaScript (navigator.gpu) and
	/// imported into emdawnwebgpu's C handle table — the in-WASM wgpuInstanceProcessEvents pump hangs when driven
	/// from a managed call stack — and the JS thread must not be blocked. Returns null if the device import fails.
	/// </summary>
	public static async Task<ISwapChain> CreateWasmAsync(string canvasId)
	{
		var inst = WebGpuInitDevice.CreateInstancePtr();
		var devPtr = await WebGpuJsInterop.CreateImportedDeviceAsync((int)inst);
		if (devPtr == 0)
		{
			return null;
		}

		var device = WebGpuInitDevice.FromImported(WGPUTextureFormat.RGBA8Unorm, inst, (IntPtr)devPtr);
		return new WebGpuBrowserGraphicsContext(device, canvasId ?? "");
	}
}

/// <summary>
/// The Uno-provided WebGPU device BRING-UP (the host "GPU-API half"): creates instance/adapter/device/queue, picks
/// the MSAA sample count by capability (2× if the colour format supports it, else 4×, else 1× — no DPI/scale input),
/// and owns the present-blit sampler. It exposes the neutral <see cref="IWebGpuDeviceContext"/> the render backend
/// adopts to build its engine; it names no renderer type. Native path creates the device synchronously; the browser
/// path adopts a device imported from JS.
/// </summary>
internal sealed unsafe class WebGpuInitDevice : IWebGpuDeviceContext
{
	public IntPtr Inst, Adapter, Dev, Q;
	public readonly WGPUTextureFormat ColorFormat;
	public uint MsaaSamples { get; private set; } = 4;
	public IntPtr Smp;                       // present-blit sampler (used by the swapchain/browser contexts)
	public WebGpuProfiler Profiler;
	private bool _hasFormatFeatures;
	private readonly bool _browser;

	public static IntPtr CreateInstancePtr() => wgpuCreateInstance(null);

	public WebGpuInitDevice(WGPUTextureFormat colorFormat)
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
		// wgpu-native's TextureAdapterSpecificFormatFeatures (when present) is what makes 2× MSAA valid for BGRA8/
		// RGBA8; without it 2× fails validation. Request it so PickSampleCount's 2× tier is usable.
		WGPUFeatureName* feats = stackalloc WGPUFeatureName[1];
		var fmtFeat = (WGPUFeatureName)WGPUNativeFeature.TextureAdapterSpecificFormatFeatures;
		_hasFormatFeatures = wgpuAdapterHasFeature(Adapter, fmtFeat) != 0;
		if (_hasFormatFeatures) { feats[0] = fmtFeat; ddesc.RequiredFeatures = feats; ddesc.RequiredFeatureCount = 1; }
		// Non-fatal uncaptured-error handler (wgpu's default panics the process on any validation/OOM error).
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

		Q = wgpuDeviceGetQueue(Dev);
		MsaaSamples = PickSampleCount();
		CreatePresentSampler();
		if (WebGpuProfiler.Enabled) { Profiler = new WebGpuProfiler(); }
		System.Console.WriteLine($"[webgpu] init device — msaa={MsaaSamples}x fmtFeatures={_hasFormatFeatures} colorFormat={ColorFormat}");
	}

	private WebGpuInitDevice(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr dev)
	{
		_browser = true;
		ColorFormat = colorFormat;
		Inst = inst;
		Adapter = IntPtr.Zero;   // JS adapter isn't imported
		Dev = dev;
		Q = wgpuDeviceGetQueue(Dev);
		MsaaSamples = 4;         // browser (Dawn) init is async — can't synchronously probe; take spec-guaranteed 4×
		CreatePresentSampler();
		if (WebGpuProfiler.Enabled) { Profiler = new WebGpuProfiler(); }
	}

	/// <summary>Adopts an instance + a device imported from JS (browser bring-up). No adapter handle.</summary>
	public static WebGpuInitDevice FromImported(WGPUTextureFormat colorFormat, IntPtr inst, IntPtr dev)
		=> new WebGpuInitDevice(colorFormat, inst, dev);

	private void CreatePresentSampler()
	{
		var sd = new WGPUSamplerDescriptor { AddressModeU = WGPUAddressMode.ClampToEdge, AddressModeV = WGPUAddressMode.ClampToEdge, MagFilter = WGPUFilterMode.Linear, MinFilter = WGPUFilterMode.Linear, MipmapFilter = WGPUMipmapFilterMode.Nearest, MaxAnisotropy = 1 };
		Smp = wgpuDeviceCreateSampler(Dev, &sd);
	}

	// MSAA sample count, no DPI/scale input: prefer 2× (needs the format feature), else 4× (spec-guaranteed), else 1×.
	// UNO_WEBGPU_MSAA=1|2|4 forces a count. The browser can't synchronously probe → 4×.
	private uint PickSampleCount()
	{
		var env = Environment.GetEnvironmentVariable("UNO_WEBGPU_MSAA");
		if (env == "4") { return 4; }
		if (env == "2") { return _hasFormatFeatures && SupportsSampleCount(2) ? 2u : 4u; }
		if (env == "1") { return 1; }
		if (_browser || OperatingSystem.IsBrowser()) { return 4; }
		if (_hasFormatFeatures && SupportsSampleCount(2)) { return 2u; }
		if (SupportsSampleCount(4)) { return 4u; }
		return 1u;
	}

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
		var box = new uint[2];
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
		return box[1] == (uint)WGPUErrorType.NoError;
	}

	// --- neutral IWebGpuDeviceContext ---
	nint IWebGpuDeviceContext.Instance => Inst;
	nint IWebGpuDeviceContext.Adapter => Adapter;
	nint IWebGpuDeviceContext.Device => Dev;
	nint IWebGpuDeviceContext.Queue => Q;
	uint IWebGpuDeviceContext.ColorFormat => (uint)ColorFormat;
	uint IWebGpuDeviceContext.SampleCount => MsaaSamples;
	public GraphicsContextKind Kind => GraphicsContextKind.WebGpu;

	public void Dispose()
	{
		if (Smp != IntPtr.Zero) { wgpuSamplerRelease(Smp); Smp = IntPtr.Zero; }
		if (Q != IntPtr.Zero) { wgpuQueueRelease(Q); Q = IntPtr.Zero; }
		if (Dev != IntPtr.Zero) { wgpuDeviceRelease(Dev); Dev = IntPtr.Zero; }
		if (Adapter != IntPtr.Zero) { wgpuAdapterRelease(Adapter); Adapter = IntPtr.Zero; }
		if (Inst != IntPtr.Zero) { wgpuInstanceRelease(Inst); Inst = IntPtr.Zero; }
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnAdapter(WGPURequestAdapterStatus status, IntPtr adapter, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = adapter;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnDevice(WGPURequestDeviceStatus status, IntPtr device, WGPUStringView message, IntPtr u1, IntPtr u2)
		=> ((IntPtr[])GCHandle.FromIntPtr(u1).Target!)[0] = device;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnUncapturedError(IntPtr device, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var msg = message.Data != IntPtr.Zero && message.Length > 0
			? System.Runtime.InteropServices.Marshal.PtrToStringUTF8(message.Data, (int)message.Length)
			: "";
		System.Console.Error.WriteLine($"[webgpu] uncaptured error ({type}): {msg}");
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnPopErrorScope(WGPUPopErrorScopeStatus status, WGPUErrorType type, WGPUStringView message, IntPtr u1, IntPtr u2)
	{
		var box = (uint[])GCHandle.FromIntPtr(u1).Target!;
		box[0] = 1; box[1] = (uint)type;
	}
}
