// One frame in flight: its command encoder, what it rented, and the passes it builds and encodes. The walk over the
// recorded tree is in WebGpuFrame.Walk.cs, the op builders in WebGpuFrame.Ops.cs, the encoding in
// WebGpuFrame.Encode.cs; the frame's coverage masks and effects live on its WebGpuCoverage and WebGpuEffects.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuFrame
{
	// UNO_WEBGPU_STATS=1: per-pass emit-shape diagnostics (see WriteFrameStats).
	private static readonly bool _emitStats = Environment.GetEnvironmentVariable("UNO_WEBGPU_STATS") is "1" or "true";
	private static int _emitStatsFrame;
	// UNO_WEBGPU_STATS_EVERY = frames between stats lines (default 60).
	private static readonly int _emitStatsEvery = int.TryParse(Environment.GetEnvironmentVariable("UNO_WEBGPU_STATS_EVERY"), out var e) && e > 0 ? e : 60;

	private readonly WebGpuDevice _d;
	internal readonly WebGpuRenderSurface Target;
	internal readonly WebGpuCoverage Coverage;
	internal readonly WebGpuEffects Effects;
	internal IntPtr Encoder;

	internal WebGpuFrame(WebGpuDevice d, WebGpuRenderSurface target)
	{
		_d = d;
		Target = target;
		Coverage = new WebGpuCoverage(this, d);
		Effects = new WebGpuEffects(this, d);
	}

	private static int _frameStatsCounter;
	// Op-build vs pass-encode, accumulated across the frame's passes (UNO_WEBGPU_STATS).
	internal static long OpsBuildTicks, EncodeTicks, RebuildTicks, StampTicks, BakeTicks;
	internal static bool EmitStats => _emitStats;

	// One frame: the main list under its root matrix, the overlay (already in device pixels) on top, one submit.
	internal void Run(List<WebGpuCommand> cmds, in Matrix3x2 m, List<WebGpuCommand> overlay, WColor? clear)
	{
		long t0 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		Begin();
		try
		{
			RenderInto(cmds, m, ClipData.None, Target, clear, overlay: overlay);
		}
		finally
		{
			long t1 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
			End();
			if (_emitStats && (_frameStatsCounter++ % 60) == 0)
			{
				long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
				double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
				System.Console.WriteLine($"[webgpu-frame] cmds={cmds.Count} renderInto={(t1 - t0) * toMs:F1}ms finishSubmit={(t2 - t1) * toMs:F1}ms opsBuild={OpsBuildTicks * toMs:F1}ms (rebuild={RebuildTicks * toMs:F1} stamp={StampTicks * toMs:F1} bake={BakeTicks * toMs:F1}) encode={EncodeTicks * toMs:F1}ms");
				OpsBuildTicks = 0; EncodeTicks = 0; RebuildTicks = 0; StampTicks = 0; BakeTicks = 0;
			}
		}
	}

	/// <summary>Opens the frame's command encoder; every pass, bake and blur of the frame encodes into it.</summary>
	internal void Begin() => Encoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null);

	/// <summary>Submits the frame and hands its layer textures back to the pool.</summary>
	internal void End()
	{
		_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
		_d.FlushFrameSlabs();
		var cb = wgpuCommandEncoderFinish(Encoder, null);
		wgpuQueueSubmit(_d.Q, 1, (IntPtr)(&cb));
		// wgpu holds its own reference until the submission completes, so both handles are dropped here -
		// otherwise every frame leaks an encoder and a command buffer into the handle table.
		wgpuCommandBufferRelease(cb);
		wgpuCommandEncoderRelease(Encoder);
		Encoder = IntPtr.Zero;
		// Pump the device non-blocking so the CPU overlaps the next frame with the GPU: pooled-buffer reuse is
		// queue-ordered and transient textures are refcount-released, and the swapchain's frames-in-flight cap
		// provides the backpressure.
		_ = wgpuDevicePoll(_d.Dev, 0u, null);
		foreach (var ls in LayerSurfaces) { _d.Pool.Return(ls.View); }
		LayerSurfaces.Clear();
	}

	// Stores a freshly built arena entry on its recording, handling the Dispose race: Dispose exchanged the field
	// before this store, so it couldn't see the new entry — hand it over here (the exchange keeps the release
	// single-shot whichever side wins).
	private void StoreCompiled(WebGpuRenderRecord rec, WebGpuGeometryCache fe)
	{
		fe.Device = _d;
		rec.Compiled = fe;
		if (rec.Commands is null && System.Threading.Interlocked.Exchange(ref rec.Compiled, null) is { } orphan)
		{
			_d.DeferCompiledRelease(orphan.Owned, orphan.StampOwned);
		}
	}

	internal readonly List<WebGpuRenderSurface> LayerSurfaces = new();

	// NDC basis for the surface the open pass renders into: device-space (_basisOx,_basisOy) is its top-left and
	// (_basisW,_basisH) its size. The window and a full-size layer get (0,0,W,H) — the target itself; a
	// size-to-content layer gets its device sub-rect, so absolute device coords map into the smaller offscreen.
	// A scissor must also be contained in its attachment, which the same size gives us. Saved/restored per build.
	private float _basisOx, _basisOy, _basisW, _basisH;

	// Size-to-content layer offscreens (on by default). UNO_WEBGPU_NO_SUBLAYER=1 forces the full-window offscreen
	// for every layer — an escape hatch and the A/B baseline for validating the optimization is behaviour-neutral.
	private static readonly bool _subLayerSizing = Environment.GetEnvironmentVariable("UNO_WEBGPU_NO_SUBLAYER") is not ("1" or "true");

	private bool TryScissor(Vector4 clip, out int x, out int y, out int w, out int h)
	{
		// Scissor is in the CURRENT target's pixels; clip AABBs are absolute device coords, so rebase by the basis
		// origin and clamp to its size (identity for the window / a full-size layer, a real shift for a sub-rect).
		var limW = _basisW > 0f ? _basisW : Target.Width;
		var limH = _basisH > 0f ? _basisH : Target.Height;
		x = (int)MathF.Max(0, MathF.Floor(clip.X - _basisOx)); y = (int)MathF.Max(0, MathF.Floor(clip.Y - _basisOy));
		int r = (int)MathF.Min(limW, MathF.Ceiling(clip.Z - _basisOx)); int b = (int)MathF.Min(limH, MathF.Ceiling(clip.W - _basisOy));
		x = (int)MathF.Min(x, limW); y = (int)MathF.Min(y, limH);
		w = r - x; h = b - y; return w > 0 && h > 0;
	}

	// The pass projection bind group (group 0 of every colour draw): the basis the vertex shader projects pixels by.
	private IntPtr MakePassBg()
	{
		var buf = _d.BufferPool.Rent(16, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
		var basis = stackalloc float[4] { _basisOx, _basisOy, BasisW, BasisH };
		wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)basis, 16);
		var e = new WGPUBindGroupEntry { Binding = 0, Buffer = buf, Offset = 0, Size = 16 };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.PassBgl, EntryCount = 1, Entries = &e };
		return _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &bgd));
	}

	private float BasisW => _basisW > 0f ? _basisW : Target.Width;
	private float BasisH => _basisH > 0f ? _basisH : Target.Height;

	private static readonly Vector4 _emptyBounds = new(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

	// How many layers deep the content being built sits: 0 for the window, 1 inside a layer, and so on. A layer's
	// content composites layers one level deeper, so the sheets holding those must render before it does.
	internal int LayerDepth;

	// The bounds of a command list in its own space, each command cut to its own clip.
	private static Vector4 CmdListBounds(List<WebGpuCommand> cmds)
	{
		var b = _emptyBounds;
		foreach (var cmd in cmds)
		{
			var cb = cmd.Kind switch
			{
				CmdKind.Rect when cmd is RectCommand r => QuadBounds(r.P0, r.P1, r.P2, r.P3, r.Clip),
				CmdKind.RoundedRect when cmd is RoundedRectCmd rr => QuadBounds(rr.P0, rr.P1, rr.P2, rr.P3, rr.Clip),
				CmdKind.Image when cmd is ImageCmd im => QuadBounds(im.P0, im.P1, im.P2, im.P3, im.Clip),
				CmdKind.Gradient when cmd is GradientCmd g => QuadBounds(g.P0, g.P1, g.P2, g.P3, g.Clip),
				CmdKind.Path when cmd is PathCmd p => ClampToClip(new Vector4(p.BbMin.X, p.BbMin.Y, p.BbMax.X, p.BbMax.Y), p.Clip),
				CmdKind.Shadow when cmd is ShadowCmd sh => ClampToClip(Inflate(new Vector4(sh.BbMin.X, sh.BbMin.Y, sh.BbMax.X, sh.BbMax.Y), MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f), sh.Clip),
				CmdKind.Layer when cmd is LayerCmd l => ClampToClip(LayerBounds(l), l.Clip),
				CmdKind.ReplayRef when cmd is ReplayRefCmd rr => ClampToClip(TransformBounds(rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands), rr.Transform), rr.Clip),
				// A backdrop samples/draws within its clip; with no finite clip it can cover the whole surface.
				_ => IsFiniteAabb(cmd.Clip.Aabb) && cmd.Kind == CmdKind.Backdrop ? cmd.Clip.Aabb : new Vector4(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue),
			};
			b = new Vector4(MathF.Min(b.X, cb.X), MathF.Min(b.Y, cb.Y), MathF.Max(b.Z, cb.Z), MathF.Max(b.W, cb.W));
		}
		return b;
	}

	private static Vector4 LayerBounds(LayerCmd l)
	{
		var b = CmdListBounds(l.Commands);
		if (l.ShadowEffect is { } fx && b.X <= b.Z)
		{
			var pad = MathF.Ceiling(3f * MathF.Max(fx.SigmaX, fx.SigmaY)) + 2f;
			var sb = Inflate(new Vector4(b.X + fx.Dx, b.Y + fx.Dy, b.Z + fx.Dx, b.W + fx.Dy), pad);
			b = new Vector4(MathF.Min(b.X, sb.X), MathF.Min(b.Y, sb.Y), MathF.Max(b.Z, sb.Z), MathF.Max(b.W, sb.W));
		}
		return b;
	}

	private static Vector4 QuadBounds(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, in ClipData clip)
	{
		var min = Vector2.Min(Vector2.Min(p0, p1), Vector2.Min(p2, p3));
		var max = Vector2.Max(Vector2.Max(p0, p1), Vector2.Max(p2, p3));
		return ClampToClip(new Vector4(min.X, min.Y, max.X, max.Y), clip);
	}

	private static Vector4 ClampToClip(Vector4 b, in ClipData clip)
		=> IsFiniteAabb(clip.Aabb)
			? new Vector4(MathF.Max(b.X, clip.Aabb.X), MathF.Max(b.Y, clip.Aabb.Y), MathF.Min(b.Z, clip.Aabb.Z), MathF.Min(b.W, clip.Aabb.W))
			: b;

	private static Vector4 Inflate(Vector4 b, float pad) => new(b.X - pad, b.Y - pad, b.Z + pad, b.W + pad);

	/// <summary>True when a device-space box contributes no pixels to the target: empty, or wholly outside it.</summary>
	private bool Culled(Vector4 b)
		=> b.X >= b.Z || b.Y >= b.W || b.Z <= 0 || b.W <= 0 || b.X >= Target.Width || b.Y >= Target.Height;

	// The bounds of both; an empty (inverted) side contributes nothing.
	private static Vector4 Union(Vector4 a, Vector4 b)
	{
		if (a.X >= a.Z || a.Y >= a.W) { return b; }
		if (b.X >= b.Z || b.Y >= b.W) { return a; }
		return new Vector4(MathF.Min(a.X, b.X), MathF.Min(a.Y, b.Y), MathF.Max(a.Z, b.Z), MathF.Max(a.W, b.W));
	}

	private static Vector4 TransformBounds(Vector4 b, in Matrix4x4 m) => TransformBounds(b, new Matrix3x2(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42));

	// The box of the mapped corners; an empty or unbounded box stays as it is.
	private static Vector4 TransformBounds(Vector4 b, in Matrix3x2 m)
	{
		if (b.X > b.Z || b.Y > b.W || !IsFiniteAabb(b)) { return b; }
		if (m.IsIdentity) { return b; }
		var q0 = Map(new Vector2(b.X, b.Y), m); var q1 = Map(new Vector2(b.Z, b.Y), m);
		var q2 = Map(new Vector2(b.Z, b.W), m); var q3 = Map(new Vector2(b.X, b.W), m);
		var min = Vector2.Min(Vector2.Min(q0, q1), Vector2.Min(q2, q3));
		var max = Vector2.Max(Vector2.Max(q0, q1), Vector2.Max(q2, q3));
		return new Vector4(min.X, min.Y, max.X, max.Y);
	}

	// Reused so the per-frame op build does not allocate a list and an array per primitive.
	private readonly List<float> _scratch = new();
	private readonly float[] _clipU = new float[ClipUFloats];   // the uniform: header + the first ClipUniformEntries entries
	private float[] _clipMore = new float[4 * ClipEntryFloats];  // the entries past those, for the overflow buffer; grows

	private readonly Stack<List<DrawOp>> _opsPool = new();
	private List<DrawOp> RentOps() => _opsPool.Count > 0 ? _opsPool.Pop() : new(256);
	private void ReturnOps(List<DrawOp> ops)
	{
		ops.Clear();   // drops the captured ClipData refs; keeps the backing array for reuse
		_opsPool.Push(ops);
	}

	// The pass's shared vertex buffers, one per layout: every per-frame draw appends its verts here in op order, so
	// adjacent ops sharing a clip occupy a contiguous range and encode as ONE draw, and the whole pass uploads each
	// buffer once. Fields rather than locals because the builders append to them; saved/restored around a nested build.
	private List<float> _solid, _rrect, _gradVerts, _quadVerts;
	private List<BackdropCmd> _backdrops;
	private readonly Stack<List<float>> _vertsPool = new();
	private List<float> RentVerts() { var l = _vertsPool.Count > 0 ? _vertsPool.Pop() : new List<float>(4096); l.Clear(); return l; }
	private void ReturnVerts(List<float> s) { s.Clear(); _vertsPool.Push(s); }
	private List<float> RentRrect() => RentVerts();
	private void ReturnRrect(List<float> s) => ReturnVerts(s);

	/// <summary>Grows the list by <paramref name="n"/> floats and returns the new tail to write into.</summary>
	internal static Span<float> Grow(List<float> list, int n)
	{
		int c = list.Count;
		System.Runtime.InteropServices.CollectionsMarshal.SetCount(list, c + n);
		return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list).Slice(c, n);
	}

	// Appends one quad (two tris) as solid verts; returns the start vertex index.
	private static int AppendSolidRect(List<float> solid, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float r, float g, float b, float a)
	{
		int start = solid.Count / VertexStride.Solid;
		var v = Grow(solid, 6 * VertexStride.Solid);
		ReadOnlySpan<Vector2> pts = stackalloc Vector2[6] { p0, p1, p2, p0, p2, p3 };
		for (int i = 0, o = 0; i < 6; i++, o += VertexStride.Solid)
		{
			v[o] = pts[i].X; v[o + 1] = pts[i].Y; v[o + 2] = r; v[o + 3] = g; v[o + 4] = b; v[o + 5] = a; v[o + 6] = 0f; v[o + 7] = 0f;
		}
		return start;
	}

	// Appends one rounded rect at the given corners: per-vertex SDF params in its own centred space (transform-invariant).
	private void AppendRrect(List<float> rr, RoundedRectCmd rrc, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		var hf = rrc.Half; var rad = rrc.Radii; var ih = rrc.InnerHalf; var ic = rrc.InnerCenter; var ir = rrc.InnerRadii;
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, color = rrc.Color.A / 255f * rrc.Opacity;
		Span<Vector2> dev = stackalloc Vector2[4] { p0, p1, p3, p2 };
		Span<Vector2> ctr = stackalloc Vector2[4] { new(-hf.X, -hf.Y), new(hf.X, -hf.Y), new(-hf.X, hf.Y), new(hf.X, hf.Y) };
		ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
		var v = Grow(rr, 6 * VertexStride.RoundedRect);
		int o = 0;
		foreach (var idx in tri)
		{
			var d = dev[idx];
			v[o] = d.X; v[o + 1] = d.Y; v[o + 2] = ctr[idx].X; v[o + 3] = ctr[idx].Y; v[o + 4] = hf.X; v[o + 5] = hf.Y;
			v[o + 6] = rad.X; v[o + 7] = rad.Y; v[o + 8] = rad.Z; v[o + 9] = rad.W; v[o + 10] = cr; v[o + 11] = cg; v[o + 12] = cb; v[o + 13] = color;
			v[o + 14] = ih.X; v[o + 15] = ih.Y; v[o + 16] = ic.X; v[o + 17] = ic.Y; v[o + 18] = ir.X; v[o + 19] = ir.Y; v[o + 20] = ir.Z; v[o + 21] = ir.W;
			o += VertexStride.RoundedRect;
		}
	}

	internal IntPtr MakeBuffer(float[] data)
	{
		var size = data.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	// List overload: uploads directly from the list's backing store (no ToArray copy).
	internal IntPtr MakeBuffer(List<float> data)
	{
		var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data);
		var size = span.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = span) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	internal IntPtr Vbuf(List<float> data, OwnedResources owned)
		=> owned is null ? MakeBuffer(data) : Vbuf(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(data).ToArray(), owned);

	internal IntPtr MakeUniform(int byteSize)
		=> _d.BufferPool.Rent(byteSize, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);

	internal IntPtr Vbuf(float[] data, OwnedResources owned)
	{
		if (owned is null) { return MakeBuffer(data); }
		int size = data.Length * sizeof(float);
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	internal IntPtr Ubuf(int size, OwnedResources owned)
	{
		if (owned is null) { return MakeUniform(size); }
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	internal IntPtr Bg(ref WGPUBindGroupDescriptor bgd, OwnedResources owned)
	{
		var bg = wgpuDeviceCreateBindGroup(_d.Dev, (WGPUBindGroupDescriptor*)Unsafe.AsPointer(ref bgd));
		if (owned is null) { _d.TrackBg(bg); } else { owned.BindGroups.Add((nint)bg); }
		return bg;
	}

	/// <summary>
	/// Six vertices (pos.xy in pixels, uv.xy) for an axis-aligned textured quad covering the device rect at
	/// <paramref name="origin"/>, sampling the texture rect <paramref name="uv"/> (x0, y0, x1, y1).
	/// </summary>
	internal float[] TexturedQuad(Vector2 origin, Vector2 size, Vector4 uv)
	{
		var q = new float[24];
		void V(int i, Vector2 pos, float u, float v)
		{
			q[i] = pos.X; q[i + 1] = pos.Y; q[i + 2] = u; q[i + 3] = v;
		}

		var tr = origin + new Vector2(size.X, 0);
		var br = origin + size;
		var bl = origin + new Vector2(0, size.Y);
		V(0, origin, uv.X, uv.Y); V(4, tr, uv.Z, uv.Y); V(8, br, uv.Z, uv.W);
		V(12, origin, uv.X, uv.Y); V(16, br, uv.Z, uv.W); V(20, bl, uv.X, uv.W);
		return q;
	}

	internal float[] TexturedQuad(Vector2 origin, Vector2 size) => TexturedQuad(origin, size, new Vector4(0f, 0f, 1f, 1f));

	/// <summary>
	/// Bind group for a SrcIn-tinted image draw: the texture carries coverage in its alpha and the colour comes
	/// from <paramref name="tint"/> (see ImageWgsl op.y). Pass <paramref name="owned"/> for a cached recording so
	/// the uniform is persistent - a per-frame one would be recycled and replay in another element's colour.
	/// </summary>
	internal IntPtr TintedImageBg(IntPtr view, WColor tint, OwnedResources owned = null)
	{
		var ubuf = Ubuf(WebGpuDevice.ImageUniformBytes, owned);
		var u = stackalloc float[36];
		for (var zi = 0; zi < 36; zi++) { u[zi] = 0f; }
		u[0] = 1f; u[1] = 1f;
		u[4] = tint.R / 255f; u[5] = tint.G / 255f; u[6] = tint.B / 255f; u[7] = tint.A / 255f;
		// Whole uniform, so a recycled buffer cannot leave the edge-AA flag set: the mask carries the coverage.
		wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)u, WebGpuDevice.ImageUniformBytes);
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
		e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, owned);
	}

	// The ClipU header (ctrl, size, xform, xoff, finv, own, inner) followed by one entry per clip; match the WGSL.
	internal const int ClipUHeaderBytes = 112, ClipEntryBytes = 96;
	// The uniform carries the header and the first four entries; a draw with more puts the rest in a storage buffer
	// bound beside it (see ClipBgl). Uniform reads are what make the common one-to-four-clip draw cheap.
	internal const int ClipUniformEntries = 4;
	internal const int ClipUBytes = ClipUHeaderBytes + ClipUniformEntries * ClipEntryBytes;
	private const int ClipUHeaderFloats = ClipUHeaderBytes / sizeof(float), ClipEntryFloats = ClipEntryBytes / sizeof(float), ClipUFloats = ClipUBytes / sizeof(float);

	// Writes the op's ClipU into _clipU (and the entries past the uniform's four into _clipMore): the analytic entries,
	// then the clip's path masks as mask entries. Returns the overflow entry count and whether the clip's AABB rode along.
	private int FillClipU(ClipData cd, Matrix3x2 xform, Matrix3x2 finv, ClipEntry[] masks, out bool foldedAabb)
	{
		if (xform == default) { xform = Matrix3x2.Identity; }   // default(Matrix3x2) is all-zero; treat as identity
		if (finv == default) { finv = Matrix3x2.Identity; }
		var entries = cd.Entries;
		int na = entries?.Length ?? 0, nm = masks?.Length ?? 0, n = na + nm;
		int more = Math.Max(0, n - ClipUniformEntries);
		if (_clipMore.Length < more * ClipEntryFloats) { _clipMore = new float[Math.Max(more * ClipEntryFloats, _clipMore.Length * 2)]; }
		var cu = _clipU;
		Array.Clear(cu);
		// Fold the clip's finite AABB into the dedicated rect slot (ctrl.y flag; min in ctrl.zw, max in
		// size.zw): the shader then owns the rect edge and the emit widens the scissor to cull-only
		// (see AabbInClipU).
		cu[0] = n;   // ctrl.x = entry count
		foldedAabb = false;
		var ab = cd.Aabb;
		if (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f)
		{
			cu[1] = 1f;                      // ctrl.y = rect clip enabled
			cu[2] = ab.X; cu[3] = ab.Y;      // ctrl.zw = rect min
			cu[6] = ab.Z; cu[7] = ab.W;      // size.zw = rect max
			foldedAabb = true;
		}
		// xform = the op's pixel-space transform: px = M11*x + M21*y + M31, py = M12*x + M22*y + M32.
		cu[8] = xform.M11; cu[9] = xform.M21; cu[10] = xform.M12; cu[11] = xform.M22;
		cu[12] = xform.M31; cu[13] = xform.M32;   // xoff.xy
		// finv maps the device fragment position back to the recording's own space. Under a size-to-content layer the
		// fragment position is sub-LOCAL while the clip is ABSOLUTE device, so the basis shift folds into its
		// translation (xoff.zw); a zero basis leaves this unchanged.
		cu[14] = finv.M31 + finv.M11 * _basisOx + finv.M21 * _basisOy;
		cu[15] = finv.M32 + finv.M12 * _basisOx + finv.M22 * _basisOy;
		cu[16] = finv.M11; cu[17] = finv.M12; cu[18] = finv.M21; cu[19] = finv.M22;
		// own.x = the op's own coverage texture is bound (sampled by vertex uv); own.y = drawn scaled or rotated, so filtered.
		if (cd.Coverage != 0) { cu[20] = 1f; cu[21] = cd.CoverageFiltered ? 1f : 0f; }
		// inner: the largest axis-aligned rect every clip covers in full, a pixel inside each edge. An excluded, masked or
		// rotated entry leaves none; a rounded corner insets its sides by the inscribed-square fraction of its radii.
		float ix = -1e30f, iy = -1e30f, iz = 1e30f, iw = 1e30f;
		if (foldedAabb) { ix = ab.X + 1f; iy = ab.Y + 1f; iz = ab.Z - 1f; iw = ab.W - 1f; }
		// A fragment step in the recording's space is a column of finv; through an entry's own matrix it is the entry's
		// step per device pixel, constant under an affine, so it is computed once here rather than per fragment.
		var ddx = new Vector2(finv.M11, finv.M12); var ddy = new Vector2(finv.M21, finv.M22);
		for (int i = 0; i < n; i++)
		{
			var e = i < na ? entries[i] : masks[i - na];
			var dst = i < ClipUniformEntries ? cu : _clipMore;
			int o = i < ClipUniformEntries ? ClipUHeaderFloats + i * ClipEntryFloats : (i - ClipUniformEntries) * ClipEntryFloats;
			dst[o + 0] = e.M.M11; dst[o + 1] = e.M.M12; dst[o + 2] = e.M.M21; dst[o + 3] = e.M.M22;   // m
			dst[o + 4] = e.M.M31; dst[o + 5] = e.M.M32; dst[o + 6] = e.Exclude ? 1f : 0f; dst[o + 7] = e.Mask ? 1f : 0f;   // t
			dst[o + 8] = e.Rect.X; dst[o + 9] = e.Rect.Y; dst[o + 10] = e.Rect.Z; dst[o + 11] = e.Rect.W;
			dst[o + 12] = e.Radii.X; dst[o + 13] = e.Radii.Y; dst[o + 14] = e.Radii.Z; dst[o + 15] = e.Radii.W;
			dst[o + 16] = e.RadiiY.X; dst[o + 17] = e.RadiiY.Y; dst[o + 18] = e.RadiiY.Z; dst[o + 19] = e.RadiiY.W;
			var qx = new Vector2(e.M.M11 * ddx.X + e.M.M21 * ddx.Y, e.M.M11 * ddy.X + e.M.M21 * ddy.Y);
			var qy = new Vector2(e.M.M12 * ddx.X + e.M.M22 * ddx.Y, e.M.M12 * ddy.X + e.M.M22 * ddy.Y);
			dst[o + 20] = MathF.Max(MathF.Max(qx.Length(), qy.Length()), 1e-6f);   // k.x
			dst[o + 21] = e.Radii == e.RadiiY ? 1f : 0f;                            // k.y
			dst[o + 22] = 0f; dst[o + 23] = 0f;
			if (iz <= ix) { continue; }
			if (e.Mask || e.Exclude || MathF.Abs(e.M.M12) > 1e-6f || MathF.Abs(e.M.M21) > 1e-6f || MathF.Abs(e.M.M11) < 1e-9f || MathF.Abs(e.M.M22) < 1e-9f)
			{
				ix = 1f; iz = 0f;
				continue;
			}
			const float inset = 0.2929f;   // 1 - 1/sqrt(2): the corner ellipse's inscribed square
			float k = dst[o + 20];
			float qL = e.Rect.X + MathF.Max(e.Radii.X, e.Radii.W) * inset + k, qR = e.Rect.Z - MathF.Max(e.Radii.Y, e.Radii.Z) * inset - k;
			float qT = e.Rect.Y + MathF.Max(e.RadiiY.X, e.RadiiY.Y) * inset + k, qB = e.Rect.W - MathF.Max(e.RadiiY.Z, e.RadiiY.W) * inset - k;
			// Back from the entry's space: q = s * p + t per axis.
			float pL = (qL - e.M.M31) / e.M.M11, pR = (qR - e.M.M31) / e.M.M11, pT = (qT - e.M.M32) / e.M.M22, pB = (qB - e.M.M32) / e.M.M22;
			ix = MathF.Max(ix, MathF.Min(pL, pR)); iz = MathF.Min(iz, MathF.Max(pL, pR));
			iy = MathF.Max(iy, MathF.Min(pT, pB)); iw = MathF.Min(iw, MathF.Max(pT, pB));
		}
		cu[24] = ix; cu[25] = iy; cu[26] = iz; cu[27] = iw;
		return more;
	}

	// Binding 4: the overflow entries, or the placeholder when the draw has none (the shader never reads it then).
	private WGPUBindGroupEntry ClipMoreEntry(IntPtr buf, int more)
		=> new() { Binding = 4, Buffer = buf != IntPtr.Zero ? buf : _d.DummyClipMore, Offset = 0, Size = (nuint)(Math.Max(more, 1) * ClipEntryBytes) };

	// The overflow entries as a storage buffer: `buf` when it already exists (a restamp rewrites in place), else a new one.
	private IntPtr WriteClipMore(int more, IntPtr buf)
	{
		if (buf == IntPtr.Zero)
		{
			var bd = new WGPUBufferDescriptor { Size = (nuint)(more * ClipEntryBytes), Usage = WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst };
			buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		}
		fixed (float* p = _clipMore) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)(more * ClipEntryBytes)); }
		return buf;
	}

	// In-place restamp of an existing owned ClipU slab slot: the shadow write flushes as part of ONE per-chunk
	// queue write before submit (queue-ordered, so frames already submitted read the old floats); the bind group
	// survives, making a per-frame restamp free of native calls.
	private bool RewriteClipU(nint slot, ClipData cd, Matrix3x2 xform, Matrix3x2 finv)
	{
		var more = FillClipU(cd, xform, finv, null, out var folded);
		_d.ClipSlab.Write(slot, _clipU, ClipUFloats);
		// The caller's reuse guard keeps the entry count unchanged, so the overflow buffer fits.
		if (more > 0) { WriteClipMore(more, _d.ClipSlab.MoreOf(slot)); }
		return folded;
	}

	// Owned variant exposing the ClipU slab slot so a later restamp can RewriteClipU it in place.
	private IntPtr MakeClipBgOwned(ClipData cd, OwnedResources owned, Matrix3x2 xform, Matrix3x2 finv, out nint buf, out bool aabbInClipU)
	{
		var masks = Coverage.ResolveClipMasks(cd, owned);
		var more = FillClipU(cd, xform, finv, masks.Entries, out aabbInClipU);
		var slot = _d.ClipSlab.Alloc();
		_d.ClipSlab.Write(slot, _clipU, ClipUFloats);
		if (more > 0) { _d.ClipSlab.SetMore(slot, WriteClipMore(more, IntPtr.Zero)); }   // freed with the slot
		(owned.ClipSlots ??= new()).Add(slot);
		var e = stackalloc WGPUBindGroupEntry[5];
		e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = _d.ClipSlab.BufferOf(slot), Offset = _d.ClipSlab.OffsetOf(slot), Size = ClipUBytes };
		e[1] = new WGPUBindGroupEntry { Binding = 1, TextureView = masks.View != IntPtr.Zero ? masks.View : _d.DummyTex };
		e[2] = new WGPUBindGroupEntry { Binding = 2, TextureView = cd.Coverage != 0 ? (IntPtr)cd.Coverage : _d.DummyTex };
		e[3] = new WGPUBindGroupEntry { Binding = 3, Sampler = _d.Smp };
		e[4] = ClipMoreEntry(more > 0 ? _d.ClipSlab.MoreOf(slot) : IntPtr.Zero, more);
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ClipBgl, EntryCount = 5, Entries = e };
		buf = slot;
		return Bg(ref bgd, owned);
	}

	internal IntPtr MakeClipBg(ClipData cd, OwnedResources owned = null, Matrix3x2 xform = default, Matrix3x2 finv = default)
	{
		if (owned is not null) { return MakeClipBgOwned(cd, owned, xform, finv, out _, out _); }
		var masks = Coverage.ResolveClipMasks(cd, null);
		var more = FillClipU(cd, xform, finv, masks.Entries, out _);
		var cu = _clipU;
		if (masks.View != IntPtr.Zero || cd.Coverage != 0 || more > 0)
		{
			// A per-op group rather than a slab slot: the slab's persistent groups bind the placeholders, and the mask
			// and coverage textures and the overflow entries are per op. Buffers and group are per-frame; the textures
			// outlive them.
			var ub = _d.BufferPool.Rent(ClipUBytes, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);
			fixed (float* pcu = cu) { wgpuQueueWriteBuffer(_d.Q, ub, 0, (IntPtr)pcu, ClipUBytes); }
			var mb = more > 0 ? WriteClipMore(more, _d.BufferPool.Rent(more * ClipEntryBytes, WGPUBufferUsage.Storage | WGPUBufferUsage.CopyDst)) : IntPtr.Zero;
			var me = stackalloc WGPUBindGroupEntry[5];
			me[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = ub, Offset = 0, Size = ClipUBytes };
			me[1] = new WGPUBindGroupEntry { Binding = 1, TextureView = masks.View != IntPtr.Zero ? masks.View : _d.DummyTex };
			me[2] = new WGPUBindGroupEntry { Binding = 2, TextureView = cd.Coverage != 0 ? (IntPtr)cd.Coverage : _d.DummyTex };
			me[3] = new WGPUBindGroupEntry { Binding = 3, Sampler = _d.Smp };
			me[4] = ClipMoreEntry(mb, more);
			var mbgd = new WGPUBindGroupDescriptor { Layout = _d.ClipBgl, EntryCount = 5, Entries = me };
			return Bg(ref mbgd, null);
		}
		// Immediate ops take a recycled per-frame slab slot: its bind group is created once and reused, and the
		// whole frame's clips upload in one queue write per chunk. Do NOT content-key this: a clip carries
		// DEVICE-space geometry, so under any moving transform every lookup misses and mints a buffer + bind
		// group per draw.
		return _d.ClipBgSlab.Rent(_d.ClipBgl, cu);
	}

	// The ops of one command list, built for one target basis, with the pass buffers they index. A pass encodes one
	// build for a window or a full-size layer, and several for the layer sheet, where every size-to-content layer of
	// the frame draws into its own slot of one texture under its own basis.
	internal sealed class PassBuild
	{
		public List<DrawOp> Ops;
		public List<BackdropCmd> Backdrops;
		public List<float> Solid, Rrect, Grad, Quad;
		public float BasisOx, BasisOy, BasisW, BasisH;
		public Vector4 Bound;   // device rect every scissor stays within: a sheet slot; the whole target otherwise
		public nint SolidBuf, RrectBuf, GradBuf, QuadBuf;
		public nuint SolidBufBytes, RrectBufBytes, GradBufBytes, QuadBufBytes;
		public IntPtr PassBg;
	}

	private static readonly Vector4 _unbounded = new(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

	/// <summary>
	/// Renders a command list into a target surface's pass. Layers render into a surface of their own, or a slot of the
	/// frame's layer sheet, and composite here; shadows and layers pre-render before the pass opens.
	/// </summary>
	// basisW/basisH default (0) to the target's own size at origin (basisOx,basisOy) — the whole-target mapping the
	// window and full-size layers use. A size-to-content layer passes its device sub-rect.
	internal void RenderInto(List<WebGpuCommand> cmds, in Matrix3x2 m, in ClipData outer, WebGpuRenderSurface target, WColor? clear, bool load = false,
		float basisOx = 0f, float basisOy = 0f, float basisW = 0f, float basisH = 0f, List<WebGpuCommand> overlay = null)
	{
		var build = BuildPass(cmds, m, outer, target, basisOx, basisOy, basisW, basisH, _unbounded, overlay);
		_singleBuild[0] = build;
		EncodePass(target, clear, load, _singleBuild);
	}

	private readonly PassBuild[] _singleBuild = new PassBuild[1];

	/// <summary>Timestamp the current build started, for the op-build half of the stats line.</summary>
	private long _renderIntoStart;

	// Builds the ops for one command list under a basis: the whole draw-side work of a pass, none of the encoding.
	internal PassBuild BuildPass(List<WebGpuCommand> cmds, in Matrix3x2 m, in ClipData outer, WebGpuRenderSurface target, float basisOx, float basisOy, float basisW, float basisH, Vector4 bound, List<WebGpuCommand> overlay = null)
	{
		_renderIntoStart = System.Diagnostics.Stopwatch.GetTimestamp();

		var savedBasis = (_basisOx, _basisOy, _basisW, _basisH);
		_basisOx = basisOx;
		_basisOy = basisOy;
		_basisW = basisW > 0f ? basisW : target.Width;
		_basisH = basisH > 0f ? basisH : target.Height;

		var b = new PassBuild { BasisOx = _basisOx, BasisOy = _basisOy, BasisW = _basisW, BasisH = _basisH, Bound = bound };
		var saved = (_solid, _rrect, _gradVerts, _quadVerts, _backdrops);
		b.Ops = RentOps();
		_solid = b.Solid = RentVerts();
		_rrect = b.Rrect = RentVerts();
		_gradVerts = b.Grad = RentVerts();
		_quadVerts = b.Quad = RentVerts();
		_backdrops = b.Backdrops = new List<BackdropCmd>();

		Walk(cmds, m, outer, b.Ops);
		if (overlay is not null) { Walk(overlay, Matrix3x2.Identity, ClipData.None, b.Ops); }

		// Upload the whole pass's shared geometry in ONE buffer per layout; the ops index them.
		b.SolidBuf = _solid.Count > 0 ? (nint)MakeBuffer(_solid) : IntPtr.Zero;
		b.SolidBufBytes = (nuint)(_solid.Count * sizeof(float));
		b.RrectBuf = _rrect.Count > 0 ? (nint)MakeBuffer(_rrect) : IntPtr.Zero;
		b.RrectBufBytes = (nuint)(_rrect.Count * sizeof(float));
		b.GradBuf = _gradVerts.Count > 0 ? (nint)MakeBuffer(_gradVerts) : IntPtr.Zero;
		b.GradBufBytes = (nuint)(_gradVerts.Count * sizeof(float));
		b.QuadBuf = _quadVerts.Count > 0 ? (nint)MakeBuffer(_quadVerts) : IntPtr.Zero;
		b.QuadBufBytes = (nuint)(_quadVerts.Count * sizeof(float));
		b.PassBg = MakePassBg();

		if (_emitStats) { OpsBuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - _renderIntoStart; }

		(_solid, _rrect, _gradVerts, _quadVerts, _backdrops) = saved;
		(_basisOx, _basisOy, _basisW, _basisH) = savedBasis;
		return b;
	}

	// Encodes builds into one pass on the target, each under its own basis and scissor bound. Everything the pass
	// samples -- masks, the frame's layer sheets -- is encoded first.
	internal void EncodePass(WebGpuRenderSurface target, WColor? clear, bool load, IReadOnlyList<PassBuild> builds)
	{
		Coverage.FlushPendingBakes();
		Effects.FlushLayerSheets(LayerDepth);

		var color = new WGPURenderPassColorAttachment
		{
			DepthSlice = uint.MaxValue,
			View = target.View,
			LoadOp = load ? WGPULoadOp.Load : WGPULoadOp.Clear,
			StoreOp = WGPUStoreOp.Store,
			ClearValue = clear.HasValue ? new WGPUColor { R = clear.Value.R / 255.0, G = clear.Value.G / 255.0, B = clear.Value.B / 255.0, A = clear.Value.A / 255.0 } : default,
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color };
		var pass = wgpuCommandEncoderBeginRenderPass(Encoder, &desc);
		var encodeStart = System.Diagnostics.Stopwatch.GetTimestamp();

		var savedBasis = (_basisOx, _basisOy, _basisW, _basisH);
		var savedBound = _bound;
		var enc = new PassEncoder(pass);
		foreach (var b in builds)
		{
			(_basisOx, _basisOy, _basisW, _basisH) = (b.BasisOx, b.BasisOy, b.BasisW, b.BasisH);
			_bound = b.Bound;
			var pst = new PassOps
			{
				Pass = pass, Target = target, Ops = b.Ops, Backdrops = b.Backdrops, PassBg = b.PassBg,
				SolidBuf = b.SolidBuf, SolidBufBytes = b.SolidBufBytes,
				RrectBuf = b.RrectBuf, RrectBufBytes = b.RrectBufBytes,
				GradBuf = b.GradBuf, GradBufBytes = b.GradBufBytes,
				QuadBuf = b.QuadBuf, QuadBufBytes = b.QuadBufBytes,
				Enc = enc,
			};
			EncodeOps(0, b.Ops.Count, ref pst);
			pass = pst.Pass;   // a backdrop segment reopens the pass
			enc = pst.Enc;
			if (_emitStats && b.Ops.Count > 0 && (_emitStatsFrame++ % _emitStatsEvery) == 0)
			{
				WriteFrameStats(b.Ops.Count, ref pst);
			}
		}
		if (_emitStats) { EncodeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - encodeStart; }
		(_basisOx, _basisOy, _basisW, _basisH) = savedBasis;
		_bound = savedBound;

		wgpuRenderPassEncoderEnd(pass);
		foreach (var b in builds) { ReleaseBuild(b); }
	}

	// Everything a build rented goes back once its ops are encoded.
	private void ReleaseBuild(PassBuild b)
	{
		ReturnOps(b.Ops);
		ReturnVerts(b.Solid);
		ReturnVerts(b.Rrect);
		ReturnVerts(b.Grad);
		ReturnVerts(b.Quad);
	}

	// The device rect the current build's scissors stay within (see PassBuild.Bound).
	private Vector4 _bound = new(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

}
