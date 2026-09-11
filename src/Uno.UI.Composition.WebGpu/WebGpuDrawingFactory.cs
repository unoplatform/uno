// The backend's entry point: the provider that registers it, the factory that creates its resources, and those
// resources (textures, shaders, filters, and the record a recording produces).
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Logging;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

// Backend-created gradient shader handle. The WebGPU backend mints its own (rather than delegating to Skia) so
// the recorder can read the gradient parameters back and evaluate them in the WGSL gradient pipeline.
public sealed class WebGpuShader : DrawingResource, IShader
{
	public bool Radial;
	public Vector2 P0;          // start (linear) / center (radial), &gradient-local space
	public Vector2 P1;          // end (linear) / gradient origin (radial)
	public float RadiusX, RadiusY;
	public WColor[] Colors;
	public float[] Stops;
	public GradientTileMode TileMode;
	public Matrix3x2 LocalMatrix;

	// Gradient parameters only; nothing native behind it.
	protected override void Free() { }
}

// Backend-owned color filter so the WebGPU renderer can read the tint params (an IColorFilter is opaque —
// consumed only by the paired renderer). Currently the SrcIn blend-mode tint (image fade/tint, the only
// DrawImage color-filter case) is honored; other modes / the color matrix carry through but the image path
// applies only SrcIn for now.
public sealed class WebGpuColorFilter : DrawingResource, IColorFilter
{
	/// <summary>A SrcIn tint (<see cref="Color"/>); otherwise this is a colour-matrix filter (<see cref="Matrix"/>).</summary>
	public bool IsTint;
	public WColor Color;
	public float[] Matrix;

	// Tint/matrix parameters only; nothing native behind it.
	protected override void Free() { }
}

// Backend-owned effect filter. Today only the drop shadow (SaveLayer(IEffectFilter) from Visual/ShadowState):
// the layer content is blurred, tinted by Color and offset by (Dx,Dy), drawn behind the content.
public sealed class WebGpuEffectFilter : DrawingResource, IEffectFilter
{
	public float Dx, Dy, SigmaX, SigmaY;
	public WColor Color;      // acrylic tint (composited SrcOver on top) / drop-shadow color
	public WColor LumColor;   // acrylic luminosity color (SrcOver over the blurred backdrop == mix(blurred, lum.rgb, lum.a))
	public float Noise;       // acrylic procedural-grain opacity (0 = none); baked into the backdrop composite
	// General non-backdrop effect-graph evaluator result: the whole tree rendered to a texture (drawn as-is on
	// Restore). When set, this filter is NOT the acrylic backdrop shape — DrawEffectBackdrop just draws it.
	public ITexture EvaluatedTexture;
	public Rect EvaluatedBounds;

	protected override void Free() => EvaluatedTexture?.Release();

	// The evaluator's texture is only released here, so a missed Release on the filter would strand it.
	~WebGpuEffectFilter() => Free();
}

public sealed class WebGpuRenderRecord : IRenderRecord
{
	internal List<WebGpuCommand> Commands = new();
	internal WColor? ClearColor;
	// Memoised scans of the immutable command list: every command is a plain draw (the recording lives in the arena);
	// some command, directly or through a nested recording, is a backdrop; the union of the commands' bounds.
	internal bool? PlainMemo, HasBackdropMemo;
	// Memoised content key of the immutable command list: 0 once computed and found unpoolable.
	internal long? ContentKeyMemo;
	internal Vector4? IdentityBounds;
	// The arena entry for this recording (the persistent retained state IRenderRecord is contracted to hold): built
	// once on the render thread at first replay, reused every frame, freed (deferred to the render thread) when this
	// recording is disposed. Written by the render thread, taken by the UI thread's Dispose — via Interlocked.
	internal WebGpuGeometryCache Compiled;
	// Transient image textures recorded into this frame that the caller disposed while recording (e.g. the one-shot
	// texture CompositionNineGridBrush uploads). We keep them alive for every present of this recording, then release
	// their GPU resources here at Dispose — resident textures (surface-owned) keep the composition's own reference.
	internal List<WebGpuTexture> Textures;
	internal List<IGeometry> Geometries;   // recorded path geometries, held like textures until this recording is disposed
	// Guards Dispose against a second call: the texture Release()s below are refcount decrements, so a double Dispose
	// would over-release and free a view an in-flight ReplayRef still holds. Interlocked because Dispose (UI thread)
	// can race the render thread's Compiled rebuild.
	private int _disposed;

	// Backend-bound: dispatches to the WebGpu session that must consume it (guaranteed same-backend by the single
	// registered backend). A recorder nests it as a ReplayRef; a present session encodes and submits it as the frame.
	public void Replay(IDrawingSession into)
	{
		switch (into)
		{
			case WebGpuCommandRecorder recorder:
				recorder.Replay(this);
				break;
			case WebGpuPresentSession present:
				present.Replay(this);
				break;
		}
	}

	// Dispose only nulls the field; the command LIST object stays alive while any in-flight frame's ReplayRef
	// still references it (captured by reference), and the device's geometry cache is keyed on that list.
	public void Dispose()
	{
		if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		// Drop this recording's references to the textures it recorded/nested. The GPU view is freed only once the
		// composition has disposed the texture AND every recording that captured its handle has released it — an outer
		// frame's ReplayRef may still hold this command list (with the raw view handle) and be compiled after us.
		if (Textures is { } textures) { foreach (var t in textures) { t.Release(); } }
		if (Geometries is { } geometries) { foreach (var g in geometries) { g.Release(); } }
		// Hand the arena entry's GPU resources to the render thread for a deferred free (an in-flight frame may
		// still reference them). Interlocked so a concurrent render-thread rebuild can't leak or double-free it.
		var c = System.Threading.Interlocked.Exchange(ref Compiled, null);
		// A shared entry outlives this recording: only the last holder hands the GPU resources over, and even then
		// the entry stays in the pool (claimable by an identical recording) until the idle sweep frees it.
		if (c is { Device: { } dev } && System.Threading.Interlocked.Decrement(ref c.Refs) <= 0)
		{
			c.IdleSince = dev.FrameSeq;
			if (c.ContentKey == 0)
			{
				foreach (var st in c.Stamps) { dev.DeferCompiledRelease(null, st.Owned); }
				dev.DeferCompiledRelease(c.Owned, null);
			}
		}
		Commands = null;
		GC.SuppressFinalize(this);
	}

	// The arena entry's buffers are reachable only through this object. The release queues are concurrent, so the
	// finalizer can hand them over.
	~WebGpuRenderRecord() => Dispose();
}

/// <summary>A host graphics context that owns a <see cref="WebGpuDevice"/> (e.g. an on-window swapchain context).
/// Lets <see cref="WebGpuGraphicsProvider"/> obtain the device without naming the platform context type.</summary>
public sealed class WebGpuGraphicsProvider : IGraphicsProvider<IWebGpuDeviceContext>
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.WebGpu };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	// Builds the WebGPU render engine from the neutral device context the host created (raw wgpu handles + the
	// host's colour format + MSAA count) — the exact seam a third-party WebGPU backend consumes. No privileged
	// path into the host's internals. Geometry is a separate seam (GeometryFactory): WebGPU flattens everything, so
	// a SkiaSharp-free app registers a ManagedGeometryFactory there rather than injecting it here.
	private static WebGpuDrawingFactory _shared;
	private static nint _sharedDevice;

	/// <summary>
	/// One factory per device, reused across windows. Negotiation runs per window, but the factory it produces is
	/// installed as the process-wide <c>DrawingFactory.Current</c> and every window's visuals record through it, so
	/// there can only be one: a second engine on the same device builds its own pipelines, and a bind group made
	/// against the first engine's layouts is rejected when a draw from a cached recording meets the second engine's
	/// pipeline ("Exclusive pipelines don't match"). The surface is rebound per present, so one engine serves any
	/// number of windows.
	/// </summary>
	public IDrawingFactory CreateGraphics(IWebGpuDeviceContext context)
	{
		DrawingCapabilities.NativeStroking = true;
		if (_shared is not null && _sharedDevice == context.Device)
		{
			return _shared;
		}

		_sharedDevice = context.Device;
		return _shared = new WebGpuDrawingFactory(new WebGpuDevice(context));
	}
}

/// <summary>
/// The WebGPU "GPU-API half" (renderer-agnostic): builds an on-window WebGPU swapchain context (surface + device)
/// from a host's <em>raw</em> native handles, so a host can create a WebGpu context for the <see cref="GraphicsContextKind.WebGpu"/>
/// kind by calling one of these entry points — without referencing the WebGPU <em>renderer</em>. The returned
/// context exposes its device via <see cref="IWebGpuDeviceContext"/>, consumed by <see cref="WebGpuGraphicsProvider"/>
/// (or a user's own WebGPU-rendering <see cref="IGraphicsProvider"/>).
/// </summary>
public sealed unsafe class WebGpuTexture : DrawingResource, ITexture
{
	private readonly WebGpuDevice _d;
	public IntPtr Tex;
	public IntPtr View;

	public int PixelWidth { get; }
	public int PixelHeight { get; }

	// Adopts an already-rendered offscreen texture (from RenderOffscreen) as a sampleable, disposable handle —
	// no upload, no readback. Deferred release is shared with the upload path (refcount + DisposeRequested).
	internal WebGpuTexture(WebGpuDevice device, IntPtr tex, IntPtr view, int width, int height)
	{
		_d = device;
		Tex = tex;
		View = view;
		PixelWidth = width;
		PixelHeight = height;
	}

	internal WebGpuTexture(WebGpuDevice device, IImage image)
	{
		_d = device;
		int w = image.PixelWidth, h = image.PixelHeight;
		PixelWidth = w; PixelHeight = h;
		byte[] bgra = (w > 0 && h > 0) ? new byte[w * h * 4] : System.Array.Empty<byte>();
		if (bgra.Length > 0) { image.CopyPixels(bgra); }
		UploadBgra(device, w, h, bgra);
	}

	// Raw pixels-in-hand path (e.g. an add-in that rasterized to its own surface): no IImage detour.
	internal WebGpuTexture(WebGpuDevice device, int width, int height, ReadOnlySpan<byte> bgraPremul)
	{
		_d = device;
		PixelWidth = width; PixelHeight = height;
		UploadBgra(device, width, height, bgraPremul);
	}

	private void UploadBgra(WebGpuDevice device, int w, int h, ReadOnlySpan<byte> bgra)
	{
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
		var rgba = new byte[w * h * 4];
		for (int i = 0; i < rgba.Length; i += 4) { rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1]; rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3]; }
		// A full mip chain: a draw that minifies the image then samples the level near its on-screen size instead of
		// striding across the full-resolution one, which is what makes large images bandwidth-bound.
		var td = new WGPUTextureDescriptor { Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 }, Format = WGPUTextureFormat.RGBA8Unorm, MipLevelCount = (uint)MipLevels(w, h), SampleCount = 1, Dimension = WGPUTextureDimension._2D, Usage = WGPUTextureUsage.TextureBinding | WGPUTextureUsage.CopyDst | WGPUTextureUsage.CopySrc };
		Tex = wgpuDeviceCreateTexture(device.Dev, &td);
		View = wgpuTextureCreateView(Tex, null);
		for (uint level = 0; ; level++)
		{
			var dst = new WGPUTexelCopyTextureInfo { Texture = Tex, Aspect = WGPUTextureAspect.All, MipLevel = level, Origin = default };
			var layout = new WGPUTexelCopyBufferLayout { BytesPerRow = (uint)(w * 4), RowsPerImage = (uint)h };
			var ext = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 };
			fixed (byte* p = rgba) { wgpuQueueWriteTexture(device.Q, &dst, (IntPtr)p, (nuint)rgba.Length, &layout, &ext); }
			if (w == 1 && h == 1) { break; }
			rgba = Halve(rgba, ref w, ref h);
		}
	}

	private static int MipLevels(int w, int h)
	{
		int n = 1;
		while (w > 1 || h > 1) { w = Math.Max(1, w >> 1); h = Math.Max(1, h >> 1); n++; }
		return n;
	}

	// The next mip level: each texel the mean of a 2x2 block above it (premultiplied, so a plain mean is right).
	private static byte[] Halve(byte[] src, ref int w, ref int h)
	{
		int dw = Math.Max(1, w >> 1), dh = Math.Max(1, h >> 1);
		var dst = new byte[dw * dh * 4];
		for (int y = 0; y < dh; y++)
		{
			int y0 = Math.Min(y * 2, h - 1), y1 = Math.Min(y * 2 + 1, h - 1);
			for (int x = 0; x < dw; x++)
			{
				int x0 = Math.Min(x * 2, w - 1), x1 = Math.Min(x * 2 + 1, w - 1);
				int a = (y0 * w + x0) * 4, b = (y0 * w + x1) * 4, c = (y1 * w + x0) * 4, d = (y1 * w + x1) * 4, o = (y * dw + x) * 4;
				for (int k = 0; k < 4; k++) { dst[o + k] = (byte)((src[a + k] + src[b + k] + src[c + k] + src[d + k] + 2) >> 2); }
			}
		}
		w = dw; h = dh;
		return dst;
	}

	// A transient image texture (e.g. CompositionNineGridBrush, or any per-frame-changing image) is disposed by the
	// composition right after recording its draw, but the recorded ImageCmd captures the raw view HANDLE and the WebGPU
	// draw is compiled/replayed later at present — possibly from an OUTER frame recording whose ReplayRef still holds the
	// (disposed) content recording's command list. So the view must outlive EVERY recording that references it, not just
	// the innermost one. The DrawingResource refcount is what covers that: every recording that records or nests this
	// texture holds a reference alongside the composition's own. Mirrors SkiaSharp's SKPicture refcounting the SKImage
	// it captured across nested pictures. Resident surface-owned textures are never disposed, so they never reach zero.
	//
	// The release itself is deferred: it lands at the next BeginFrameResources (drained under RenderGate, after the
	// last present's submit), like the per-frame bind groups and buffers.
	protected override void Free()
	{
		if (View != IntPtr.Zero || Tex != IntPtr.Zero) { _d.DeferTextureRelease(View, Tex); View = IntPtr.Zero; Tex = IntPtr.Zero; }
	}

	// Nothing can reach View/Tex once this object is collected, so a missed Release would strand the allocation for
	// the life of the device. The release queue is concurrent, so the finalizer can hand them over — late, not lost.
	~WebGpuTexture()
	{
		if (View == IntPtr.Zero && Tex == IntPtr.Zero) { return; }
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"A {PixelWidth}x{PixelHeight} WebGPU texture was finalized with live handles: a reference was never released. Freeing it late.");
		}

		Free();
	}
}

/// <summary>A managed <see cref="IImage"/> over a WebGPU offscreen readback. The readback bytes are in the
/// device's color format (RGBA for the offscreen device, BGRA for a swapchain device); <see cref="CopyPixels"/>
/// yields BGRA per the seam's image convention, swapping R/B only when the source is RGBA. No Skia.</summary>
internal sealed unsafe class WebGpuReadbackImage : DrawingResource, IImage
{
	private IntPtr _bytes;
	private readonly int _length;
	private readonly bool _sourceIsBgra;

	/// <summary>Drops the readback's 256-byte row padding into a tightly-packed buffer this image owns.</summary>
	// Unmanaged because a window-sized snapshot runs to several megabytes: as an array it would land on the
	// large-object heap, and on wasm in a heap with little room to spare.
	public WebGpuReadbackImage(int width, int height, ReadOnlySpan<byte> paddedRows, int paddedStride, bool sourceIsBgra)
	{
		PixelWidth = width;
		PixelHeight = height;
		_sourceIsBgra = sourceIsBgra;
		var stride = width * 4;
		_length = stride * height;
		_bytes = (IntPtr)NativeMemory.Alloc((nuint)_length);
		var destination = new Span<byte>((void*)_bytes, _length);
		for (var y = 0; y < height; y++)
		{
			paddedRows.Slice(y * paddedStride, stride).CopyTo(destination.Slice(y * stride, stride));
		}
	}

	public int PixelWidth { get; }
	public int PixelHeight { get; }

	public void CopyPixels(Span<byte> destination)
	{
		var source = new ReadOnlySpan<byte>((void*)_bytes, _length);
		int n = Math.Min(_length, destination.Length);
		if (_sourceIsBgra) { source.Slice(0, n).CopyTo(destination); return; }
		for (int i = 0; i + 3 < n; i += 4) { destination[i] = source[i + 2]; destination[i + 1] = source[i + 1]; destination[i + 2] = source[i]; destination[i + 3] = source[i + 3]; }
	}

	protected override void Free()
	{
		var buffer = System.Threading.Interlocked.Exchange(ref _bytes, IntPtr.Zero);
		if (buffer != IntPtr.Zero) { NativeMemory.Free((void*)buffer); }
	}

	// Unmanaged, so a missed Release would lose the buffer rather than leave it to the GC.
	~WebGpuReadbackImage() => Free();
}

/// <summary>
/// The device-bound WebGPU resource factory: textures, gradient shaders, color filters, the drop-shadow /
/// backdrop-blur effect, and offscreen rasterization are all WebGPU-owned. Geometry, font resolution/shaping and
/// image decode are separate backend-independent seams (<see cref="GeometryFactory"/> / <see cref="FontProvider"/>
/// / <see cref="ImageEncoderDecoder"/>); WebGPU consumes the neutral <see cref="IGeometry"/> it's registered by flattening
/// it, so a SkiaSharp-free app registers a <see cref="ManagedGeometryFactory"/> and links zero SkiaSharp for drawing.
/// </summary>
public sealed partial class WebGpuDrawingFactory : IDrawingFactory<IWebGpuRenderTarget>
{
	private readonly WebGpuDevice _device;

	/// <summary>The main-pass surface for one render target: the host's colour (the neutral <see cref="IWebGpuRenderTarget"/>) wrapped as a surface.</summary>
	private sealed class MainSurface
	{
		public WebGpuRenderSurface Surface;
		public int Width, Height;
		public IntPtr ColorView;        // the resolve view the backend renders into (imported from JsColorView on WASM)
	}

	/// <summary>
	/// One main surface PER TARGET. A single factory serves every window - it is installed as the process-wide
	/// <c>DrawingFactory.Current</c> - so a single surface would be disposed and rebuilt on every present as two
	/// windows took turns, and two windows of equal size would render into whichever presented first. Keyed on the
	/// target instance, whose lifetime is that window's swapchain context (a resize replaces it).
	/// </summary>
	private readonly System.Collections.Generic.Dictionary<IWebGpuRenderTarget, MainSurface> _mainSurfaces = new();

	/// <summary>Targets in least-recently-presented order, so surfaces for windows that are gone cannot pile up.</summary>
	private readonly System.Collections.Generic.List<IWebGpuRenderTarget> _mainSurfaceLru = new();

	/// <summary>Distinct targets whose surfaces are kept. A handful of windows plus their in-flight resizes.</summary>
	private const int MaxMainSurfaces = 8;

	internal WebGpuDrawingFactory(WebGpuDevice device) { _device = device; }

	public ICommandRecorder CreateRecording() => new WebGpuCommandRecorder(this);

	/// <summary>
	/// Drops the least-recently-presented main surfaces once more than <see cref="MaxMainSurfaces"/> targets are
	/// tracked. A closed window's target is simply never presented again, so nothing else would ever release it.
	/// </summary>
	private void EvictMainSurfaces()
	{
		while (_mainSurfaceLru.Count >= MaxMainSurfaces)
		{
			var oldest = _mainSurfaceLru[0];
			_mainSurfaceLru.RemoveAt(0);
			if (_mainSurfaces.Remove(oldest, out var stale))
			{
				stale.Surface?.Dispose();
			}
		}
	}

	public IPresentSession BeginPresent(IWebGpuRenderTarget target)
	{
		// A minimized window can report an empty client area; the surface's textures must still be at least 1x1.
		var width = Math.Max(1, target.Width);
		var height = Math.Max(1, target.Height);

		if (!_mainSurfaces.TryGetValue(target, out var main))
		{
			EvictMainSurfaces();
			_mainSurfaces[target] = main = new MainSurface();
		}

		_mainSurfaceLru.Remove(target);
		_mainSurfaceLru.Add(target);

		if (main.Surface is null || main.Width != width || main.Height != height)
		{
			main.Surface?.Dispose();
			main.Surface = new WebGpuRenderSurface(_device, width, height, externalColor: true);
			main.Width = width;
			main.Height = height;
			// Browser: the host hands the resolve target as a live JS GPUTextureView; convert it to a wgpu view HERE
			// (the backend's own emdawn import) — symmetric with the device import — rather than consuming a raw
			// pointer from the contract. Imported once per size. Native targets have JsColorView == null → use the
			// pointer directly. The imported handle wraps the same JS view the host presents from (shared underlying).
			if (OperatingSystem.IsBrowser() && target.JsColorView is { } jsView)
			{
				main.ColorView = (IntPtr)WebGpuJsInterop.ImportTextureView(jsView, 0);
				System.Console.WriteLine($"[webgpu] backend imported JS color view ptr={main.ColorView}");
			}
			else
			{
				main.ColorView = target.ColorView;
			}
		}

		// Point the backend surface at the host's colour view (host owns its lifetime; the render pass only needs the view).
		main.Surface.View = main.ColorView;
		return new WebGpuPresentSession(_device, main.Surface, this);
	}

	public ITexture CreateTexture(IImage image) => new WebGpuTexture(_device, image);

	public ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
		=> new WebGpuTexture(_device, pixelWidth, pixelHeight, bgraPremul);

	// Offscreen rasterization on the WebGPU device (record → present into a dedicated offscreen surface) and hand
	// back the resolved color texture as a sampleable ITexture — no CPU read-back, so a nine-slice/glyph/SVG
	// consumer draws it straight. CPU pixels (RenderTargetBitmap) come from SnapshotAsync instead.
	public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render)
	{
		var recorder = new WebGpuCommandRecorder(this);
		render(recorder);
		var surface = new WebGpuRenderSurface(_device, pixelWidth, pixelHeight);
		var present = new WebGpuPresentSession(_device, surface, this);
		var record = recorder.Finish();
		present.ReplayNested(record);   // encodes + submits the nested render into the surface's color texture
		// The recording referenced every texture it drew; the pass is submitted, so hand those references back.
		record.Dispose();
		// Take ownership of the resolved color texture; disposing the surface releases only the (finished) MSAA + depth targets.
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, pixelWidth, pixelHeight);
	}

	// GPU→CPU read of a texture produced by this factory. Off-browser a native thread drives the map (blocking);
	// on the browser the map must run off the JS event loop, so the copy is encoded here and mapped in JS.
	public async System.Threading.Tasks.Task<IImage> SnapshotAsync(ITexture texture)
	{
		if (texture is not WebGpuTexture t)
		{
			throw new ArgumentException("Texture was not produced by WebGpuDrawingFactory.", nameof(texture));
		}

		int w = t.PixelWidth, h = t.PixelHeight;
		bool srcBgra = _device.ColorFormat == WGPUTextureFormat.BGRA8Unorm;
		if (!OperatingSystem.IsBrowser())
		{
			return _device.ReadPixelsToImage(t.Tex, w, h, srcBgra);
		}

		_device.EncodeCopyTexToReadbackBuffer(t.Tex, w, h, out var buf, out var total, out var padded);
		// Browser GPU→CPU map must run off the JS event loop; the JS bridge lives in the host init assembly.
		var paddedBytes = Convert.FromBase64String(await WebGpuJsInterop.MapReadBase64Async((int)buf, total));
		_device.DestroyBuffer(buf);
		return new WebGpuReadbackImage(w, h, paddedBytes, padded, srcBgra);
	}
	public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = false, P0 = start, P1 = end, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, WColor[] colors, float[] colorPositions, GradientTileMode tileMode, System.Numerics.Matrix3x2 localMatrix)
		=> new WebGpuShader { Radial = true, P0 = center, P1 = gradientOrigin, RadiusX = radiusX, RadiusY = radiusY, Colors = colors, Stops = colorPositions, TileMode = tileMode, LocalMatrix = localMatrix };
	public IColorFilter CreateTintColorFilter(WColor color) => new WebGpuColorFilter { IsTint = true, Color = color };
	public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => new WebGpuColorFilter { Matrix = matrix };
	public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, WColor color) => new WebGpuEffectFilter { Dx = dx, Dy = dy, SigmaX = sigmaX, SigmaY = sigmaY, Color = color };

}
