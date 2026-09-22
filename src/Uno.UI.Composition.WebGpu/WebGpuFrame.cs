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
	// WalkTicks spans the whole traversal (so the traversal's own share is WalkTicks minus rebuild/stamp/bake);
	// UploadTicks is the per-pass shared vertex buffers and the pass bind group.
	internal static long WalkTicks, UploadTicks;
	// Layers build nested passes from inside the walk; only the outermost one is timed, else the nested
	// time is counted once per level and the total exceeds opsBuild.
	private static int _passDepth;
	internal static bool EmitStats => _emitStats;

	// One frame: the main list under its root matrix, the overlay (already in device pixels) on top, one submit.
	internal void Run(List<WebGpuCommand> cmds, in Matrix3x2 m, List<WebGpuCommand> overlay, WColor? clear)
	{
		long t0 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		Begin();
		try
		{
			RenderInto(cmds, m, ClipData.None, Target, clear, overlay: overlay, depth: !WebGpuDevice.NoDepthOcclusion);
		}
		finally
		{
			long t1 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
			End();
			// Same cadence as the stats line, so UNO_WEBGPU_STATS_EVERY=1 gives a per-FRAME phase breakdown rather
			// than a 60-frame average - the only way to see a distribution instead of a mean.
			if (_emitStats && (_frameStatsCounter++ % _emitStatsEvery) == 0)
			{
				long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
				double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
				System.Console.WriteLine($"[webgpu-frame] cmds={cmds.Count} renderInto={(t1 - t0) * toMs:F1}ms finishSubmit={(t2 - t1) * toMs:F1}ms opsBuild={OpsBuildTicks * toMs:F1}ms (walk={WalkTicks * toMs:F1} rebuild={RebuildTicks * toMs:F1} stamp={StampTicks * toMs:F1} bake={BakeTicks * toMs:F1} upload={UploadTicks * toMs:F1}) encode={EncodeTicks * toMs:F1}ms");
				OpsBuildTicks = 0; EncodeTicks = 0; RebuildTicks = 0; StampTicks = 0; BakeTicks = 0;
				WalkTicks = 0; UploadTicks = 0;
			}
		}
	}

	/// <summary>Opens the frame's command encoder; every pass, bake and blur of the frame encodes into it.</summary>
	internal void Begin()
	{
		SweepEntryPool();
		Encoder = wgpuDeviceCreateCommandEncoder(_d.Dev, null);
	}

	/// <summary>Submits the frame and hands its layer textures back to the pool.</summary>
	internal void End()
	{
		_d.ClipSlab.Flush();   // one queue write per dirty chunk, before the submit that reads the clips
		_d.SiteSlab.Flush();
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
			ReleaseEntry(orphan);
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
		var limW = (_basisW > 0f ? _basisW : Target.Width) / _basisScale;
		var limH = (_basisH > 0f ? _basisH : Target.Height) / _basisScale;
		x = (int)MathF.Max(0, MathF.Floor((clip.X - _basisOx) / _basisScale)); y = (int)MathF.Max(0, MathF.Floor((clip.Y - _basisOy) / _basisScale));
		int r = (int)MathF.Min(limW, MathF.Ceiling((clip.Z - _basisOx) / _basisScale)); int b = (int)MathF.Min(limH, MathF.Ceiling((clip.W - _basisOy) / _basisScale));
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
	private float BasisScale => _basisScale;
	private float BasisH => _basisH > 0f ? _basisH : Target.Height;

	private static readonly Vector4 _emptyBounds = new(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

	// Device pixels per target pixel. 1 everywhere except a shadow layer, which is only ever read blurred and so is
	// rendered smaller than the area it covers. The basis stays in DEVICE units (that is what `project` wants);
	// this is what turns a device rect into target pixels for the scissor.
	private float _basisScale = 1f;

	/// <summary>This pass carries the occlusion depth buffer, so its draws use the depth pipeline variants.</summary>
	internal bool UseDepth;
	private IntPtr _depthView;

	// Replay sites in draw order within a pass, and the depth step between them. 4096 sites is far past what any
	// frame builds, and keeps neighbouring depths apart in a 24-bit buffer.
	private int _siteSeq;
	private const float SiteDepthStep = 1f / 4096f;

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

	// The rect an axis-aligned quad covers, before the antialiasing pad -- empty when the quad is rotated or skewed,
	// which the occlusion cull does not handle. Corners are TL, TR, BR, BL.
	private static Vector4 AaRect(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
		=> p0.Y == p1.Y && p2.Y == p3.Y && p0.X == p3.X && p1.X == p2.X && p1.X > p0.X && p3.Y > p0.Y
			? new Vector4(p0.X, p0.Y, p2.X, p2.Y)
			: default;

	// A rounded rect paints at full alpha everywhere except its corners: no ring cut out of it, and opaque colour.
	private static bool OpaqueRrect(RoundedRectCmd rr)
		=> rr.Color.A == 255 && rr.Opacity >= 1f && rr.InnerHalf.X < 0f;

	// The biggest axis-aligned rect inside a rounded one: keep the full width and lose the corner rows, or keep
	// the full height and lose the corner columns, whichever leaves more. A 240x168 card at radius 12 keeps 88%.
	private static Vector4 Inscribed(in Vector4 r, float radX, float radY)
	{
		if (radX <= 0f && radY <= 0f) { return r; }
		float w = r.Z - r.X, h = r.W - r.Y;
		float wide = w * MathF.Max(0f, h - 2f * radY), tall = MathF.Max(0f, w - 2f * radX) * h;
		return wide >= tall
			? new Vector4(r.X, r.Y + radY, r.Z, r.W - radY)
			: new Vector4(r.X + radX, r.Y, r.Z - radX, r.W);
	}

	// The rect a rounded-rect command fills, in device pixels: its box less the corners, whose LOCAL radii reach
	// device space through the quad's own device size.
	private static Vector4 RrectCover(RoundedRectCmd rr, in Vector4 box)
	{
		if (box == default) { return default; }
		float rad = MathF.Max(MathF.Max(rr.Radii.X, rr.Radii.Y), MathF.Max(rr.Radii.Z, rr.Radii.W));
		if (rad <= 0f) { return box; }
		float sx = rr.Half.X > 0f ? (box.Z - box.X) / (2f * rr.Half.X) : 1f;
		float sy = rr.Half.Y > 0f ? (box.W - box.Y) / (2f * rr.Half.Y) : 1f;
		return Inscribed(box, rad * sx, rad * sy);
	}

	// The largest rect every analytic clip entry covers in FULL, in the clip's own space -- the CPU twin of the
	// inner box WriteClipU hands the shader. An excluded, masked or rotated entry leaves none, and a path clip
	// leaves none. Without this a rounded clip disqualifies its op from ever hiding anything, which is most of a
	// real UI: a card's corner radius alone is enough.
	private static Vector4 ClipInner(in ClipData cd)
	{
		if (cd.Paths is not null || cd.Coverage != 0) { return default; }
		float ix = -1e30f, iy = -1e30f, iz = 1e30f, iw = 1e30f;
		if (!cd.ScissorInert && IsFiniteAabb(cd.Aabb)) { ix = cd.Aabb.X + 1f; iy = cd.Aabb.Y + 1f; iz = cd.Aabb.Z - 1f; iw = cd.Aabb.W - 1f; }
		var ents = cd.Entries;
		for (int i = 0; ents is not null && i < ents.Length; i++)
		{
			ref readonly var e = ref ents[i];
			if (e.Exclude || e.Mask || MathF.Abs(e.M.M12) > 1e-6f || MathF.Abs(e.M.M21) > 1e-6f
				|| MathF.Abs(e.M.M11) < 1e-9f || MathF.Abs(e.M.M22) < 1e-9f) { return default; }
			// The inscribed-square fraction of each corner radius, plus a device pixel for the analytic ramp.
			const float inset = 0.2929f;
			float k = MathF.Max(MathF.Max(new Vector2(e.M.M11, e.M.M12).Length(), new Vector2(e.M.M21, e.M.M22).Length()), 1e-6f);
			float qL = e.Rect.X + MathF.Max(e.Radii.X, e.Radii.W) * inset + k, qR = e.Rect.Z - MathF.Max(e.Radii.Y, e.Radii.Z) * inset - k;
			float qT = e.Rect.Y + MathF.Max(e.RadiiY.X, e.RadiiY.Y) * inset + k, qB = e.Rect.W - MathF.Max(e.RadiiY.Z, e.RadiiY.W) * inset - k;
			float pL = (qL - e.M.M31) / e.M.M11, pR = (qR - e.M.M31) / e.M.M11;
			float pT = (qT - e.M.M32) / e.M.M22, pB = (qB - e.M.M32) / e.M.M22;
			ix = MathF.Max(ix, MathF.Min(pL, pR)); iz = MathF.Min(iz, MathF.Max(pL, pR));
			iy = MathF.Max(iy, MathF.Min(pT, pB)); iw = MathF.Min(iw, MathF.Max(pT, pB));
		}
		return iz > ix && iw > iy ? new Vector4(ix, iy, iz, iw) : default;
	}

	// A clip that cannot cut the op's own shape anywhere inside its AABB: anything else and the op paints less
	// than its rect, so it may not be trusted to hide what is under it.
	private static bool ClipIsPlain(in ClipData c)
		=> c.Paths is null && (c.Entries is null || c.Entries.Length == 0) && c.Coverage == 0;

	// Coverage decides in PIXELS, not in the rect's own coordinates: a rect flush with the surface edge still fills
	// its edge pixels, which a rect-versus-rect comparison with any margin for the antialiasing ramp would deny.
	// Both of these are inclusive pixel indices.

	// Where the analytic ramp reads exactly 1: a pixel centre at least half a pixel inside the edge.
	private static Vector4 PixelsFilled(in Vector4 r)
		=> new(MathF.Ceiling(r.X), MathF.Ceiling(r.Y), MathF.Floor(r.Z - 1f), MathF.Floor(r.W - 1f));

	// Where it reads anything at all: a pixel centre less than half a pixel outside.
	private static Vector4 PixelsTouched(in Vector4 r)
		=> new(MathF.Floor(r.X - 1f) + 1f, MathF.Floor(r.Y - 1f) + 1f, MathF.Ceiling(r.Z) - 1f, MathF.Ceiling(r.W) - 1f);

	private static Vector4 Meet(in Vector4 a, in Vector4 b)
		=> new(MathF.Max(a.X, b.X), MathF.Max(a.Y, b.Y), MathF.Min(a.Z, b.Z), MathF.Min(a.W, b.W));

	private static bool Contains(in Vector4 outer, in Vector4 inner)
		=> inner.X >= outer.X && inner.Y >= outer.Y && inner.Z <= outer.Z && inner.W <= outer.W;

	// What of `box` a cover rect leaves uncovered, as up to four bands (above, below, left, right of the overlap).
	// Rect minus rect is not a rect in general -- a page background under an opaque content panel is covered across
	// its middle and survives only as a header and a footer strip.
	private static int Remainder(in Vector4 cover, in Vector4 box, Span<Vector4> outBands)
	{
		float ix0 = MathF.Max(box.X, cover.X), ix1 = MathF.Min(box.Z, cover.Z);
		float iy0 = MathF.Max(box.Y, cover.Y), iy1 = MathF.Min(box.W, cover.W);
		if (ix0 > ix1 || iy0 > iy1) { return 0; }   // no overlap at all: nothing to trim
		int n = 0;
		if (iy0 > box.Y) { outBands[n++] = new Vector4(box.X, box.Y, box.Z, iy0 - 1f); }
		if (iy1 < box.W) { outBands[n++] = new Vector4(box.X, iy1 + 1f, box.Z, box.W); }
		if (ix0 > box.X) { outBands[n++] = new Vector4(box.X, iy0, ix0 - 1f, iy1); }
		if (ix1 < box.Z) { outBands[n++] = new Vector4(ix1 + 1f, iy0, box.Z, iy1); }
		return n;
	}

	private static float PixelArea(in Vector4 r) => MathF.Max(0f, r.Z - r.X + 1f) * MathF.Max(0f, r.W - r.Y + 1f);

	// Splitting multiplies the draw, so it has to buy a lot: a big op that keeps little of itself.
	private const float SplitMinArea = 100_000f;
	private const float SplitMaxKept = 0.6f;

	/// <summary>
	/// Drops ops that a later opaque rect paints over completely. Stacked full-surface backgrounds -- the app's, the
	/// page's and the control's, each an opaque fill of the whole window -- are the ordinary case, and each one of
	/// them costs a whole surface of fill rate.
	/// </summary>
	private static void CullOccluded(List<DrawOp> ops, in Vector4 bound)
	{
		var cover = default(Vector4);
		float coverArea = 0f;
		for (int i = ops.Count - 1; i >= 0; i--)
		{
			var op = ops[i];
			// A backdrop samples the target, so everything before it stays visible to it whatever is drawn later.
			if (op.Kind == DrawKind.BackdropSegment) { cover = default; coverArea = 0f; continue; }
			if (op.Bounds == default) { continue; }
			var touched = PixelsTouched(op.Bounds);
			if (coverArea > 0f && Contains(cover, touched))
			{
				ops.RemoveAt(i);
				StatCulled++;
				continue;
			}
			if (op.Cover == default)
			{
				// Not covered outright, but maybe covered across its middle: redraw only the bands left over.
				if (coverArea > 0f && op.CullScissor.Z <= op.CullScissor.X && PixelArea(touched) > SplitMinArea)
				{
					Span<Vector4> bands = stackalloc Vector4[4];
					int n = Remainder(cover, touched, bands);
					float kept = 0f;
					for (int k = 0; k < n; k++) { kept += PixelArea(bands[k]); }
					if (n > 0 && kept < PixelArea(touched) * SplitMaxKept)
					{
						// Inclusive pixel indices back to a device rect: the far edge is one past the last.
						for (int k = 0; k < n; k++)
						{
							var o = op;
							o.CullScissor = new Vector4(bands[k].X, bands[k].Y, bands[k].Z + 1f, bands[k].W + 1f);
							if (k == 0) { ops[i] = o; } else { ops.Insert(i + k, o); }
						}
						StatSplit++;
					}
				}
				continue;
			}
			// What it actually fills: its own covered pixels, cut to every scissor the encode will apply.
			var r = PixelsFilled(op.Cover);
			if (!op.Clip.ScissorInert) { r = Meet(r, PixelsFilled(op.Clip.Aabb)); }
			if (bound.X > float.MinValue) { r = Meet(r, PixelsFilled(bound)); }
			var area = MathF.Max(0f, r.Z - r.X + 1f) * MathF.Max(0f, r.W - r.Y + 1f);
			if (area > coverArea) { cover = r; coverArea = area; }
		}
	}

	// One quad per op that paints something opaque, at its site's depth. The prepass draws these into the depth
	// buffer so the colour pass can reject whatever an opaque site later covered -- per fragment, and by the
	// fixed-function hardware, rather than per rect pair on the CPU.
	private static void BuildPrepass(PassBuild b)
	{
		int n = 0;
		foreach (var op in b.Ops) { if (PrepassCover(op, b) != default) { n++; } }
		if (n == 0) { return; }
		var v = new float[n * 6 * 3];
		int o = 0;
		foreach (var op in b.Ops)
		{
			var c = PrepassCover(op, b);
			if (c == default) { continue; }
			var z = op.Depth;
			ReadOnlySpan<float> xs = stackalloc float[6] { c.X, c.Z, c.Z, c.X, c.Z, c.X };
			ReadOnlySpan<float> ys = stackalloc float[6] { c.Y, c.Y, c.W, c.Y, c.W, c.W };
			for (int i = 0; i < 6; i++) { v[o++] = xs[i]; v[o++] = ys[i]; v[o++] = z; }
		}
		b.Prepass = v;
		b.PrepassVerts = n * 6;
		StatPrepass += n;
	}

	// What an op may claim in the depth buffer: only where it certainly paints at full alpha. The scissor the
	// encode will apply cuts it (the CPU cull does the same), the build's bound cuts it, and it is inset by a
	// pixel because analytic antialiasing leaves every edge pixel partly transparent. Over-claiming here does not
	// look like a cull miss -- it silently deletes whatever was underneath.
	private static Vector4 PrepassCover(in DrawOp op, PassBuild b)
	{
		if (op.Cover == default || op.Depth <= 0f) { return default; }
		var c = op.Cover;
		// Only for an op the walk placed itself. A replayed op's clip is in its RECORDING's space, so meeting a
		// device-space cover with it would claim ground the clip never covered; AppendSite has already cut such a
		// cover to its session clip, in device space, which is the equivalent restriction.
		if (op.SiteSlot == 0 && !op.Clip.ScissorInert) { c = Meet(c, op.Clip.Aabb); }
		if (b.Bound.X > float.MinValue) { c = Meet(c, b.Bound); }
		c = new Vector4(c.X + 1f, c.Y + 1f, c.Z - 1f, c.W - 1f);
		return c.Z - c.X >= 1f && c.W - c.Y >= 1f ? c : default;
	}

	/// <summary>Ops the occlusion cull dropped this frame, and ops it cut down to their uncovered bands.</summary>
	internal static int StatCulled;
	internal static int StatSplit;
	/// <summary>Border rings cut into bands. CUMULATIVE: op building happens once per cached recording, so a
	/// counter reset every stats interval reads zero in steady state while the rings still draw every frame.</summary>
	internal static int StatRingBands;
	internal static int StatPrepass;
	internal static int StatLayerShadow, StatLayerMatrix, StatLayerMask, StatLayerPlain;

	// Off for bisecting a visual regression against the cull.
	private static readonly bool _noOcclusionCull = Environment.GetEnvironmentVariable("UNO_WEBGPU_NO_OCCLUSION_CULL") == "1";

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
		// No rotation or skew: the box maps to a box, so two opposite corners settle it. A scrolling list is in
		// this case and walks here once per record per frame.
		if (m.M12 == 0f && m.M21 == 0f)
		{
			float ax = b.X * m.M11 + m.M31, bx = b.Z * m.M11 + m.M31;
			float ay = b.Y * m.M22 + m.M32, by = b.W * m.M22 + m.M32;
			return new Vector4(MathF.Min(ax, bx), MathF.Min(ay, by), MathF.Max(ax, bx), MathF.Max(ay, by));
		}
		var q0 = Map(new Vector2(b.X, b.Y), m); var q1 = Map(new Vector2(b.Z, b.Y), m);
		var q2 = Map(new Vector2(b.Z, b.W), m); var q3 = Map(new Vector2(b.X, b.W), m);
		var min = Vector2.Min(Vector2.Min(q0, q1), Vector2.Min(q2, q3));
		var max = Vector2.Max(Vector2.Max(q0, q1), Vector2.Max(q2, q3));
		return new Vector4(min.X, min.Y, max.X, max.Y);
	}

	// Reused so the per-frame op build does not allocate a list and an array per primitive.
	private readonly VertBuf _scratch = new();
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
	private VertBuf _solid, _rrect, _gradVerts, _quadVerts;
	private List<BackdropCmd> _backdrops;
	private readonly Stack<VertBuf> _vertsPool = new();
	private VertBuf RentVerts() { var l = _vertsPool.Count > 0 ? _vertsPool.Pop() : new VertBuf(); l.Clear(); return l; }
	private void ReturnVerts(VertBuf s) { s.Clear(); _vertsPool.Push(s); }
	private VertBuf RentRrect() => RentVerts();
	private void ReturnRrect(VertBuf s) => ReturnVerts(s);

	/// <summary>Grows the buffer by <paramref name="n"/> floats and returns the new tail to write into.</summary>
	internal static Span<float> Grow(VertBuf buf, int n) => buf.Grow(n);

	// Appends one quad (two tris) as solid verts; returns the start vertex index.
	private static int AppendSolidRect(VertBuf solid, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float r, float g, float b, float a)
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

	// A plain rect through the rounded-rect pipeline, with no corners: its analytic coverage is the only edge
	// antialiasing a solid quad can have, and the SDF's local space comes from the device corners, so it is exact
	// under any affine.
	private int AppendAaRect(VertBuf rr, in WColor color, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
		=> AppendRrect(rr, new RoundedRectCmd { Half = new Vector2((p1 - p0).Length() * 0.5f, (p3 - p0).Length() * 0.5f), Color = color }, p0, p1, p2, p3);

	// Appends one rounded rect at the given corners: per-vertex SDF params in its own centred space (transform-invariant).
	// The quad is grown a pixel past the shape on every side: coverage below 1 lies OUTSIDE the edge, and a quad that
	// stops at the edge never rasterises it - which left a rotated rect hard and an offset one half a pixel thin.
	// Returns how many vertices it appended.
	private int AppendRrect(VertBuf rr, RoundedRectCmd rrc, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		var hf = rrc.Half; var rad = rrc.Radii; var ih = rrc.InnerHalf; var ic = rrc.InnerCenter; var ir = rrc.InnerRadii;
		float cr = rrc.Color.R / 255f, cg = rrc.Color.G / 255f, cb = rrc.Color.B / 255f, color = rrc.Color.A / 255f * rrc.Opacity;
		const float Pad = 1f;
		var ax = p1 - p0; var ay = p3 - p0;
		float dw = ax.Length(), dh = ay.Length();
		var ext = hf;
		var local = Vector2.One;   // local units per device pixel, per axis
		if (dw > 1e-4f && dh > 1e-4f)
		{
			// The pad is a DEVICE pixel, so it reaches the SDF's own space through that axis' device length.
			local = new Vector2(hf.X * 2f / dw, hf.Y * 2f / dh);
			ext += local * Pad;
			var ex = ax / dw * Pad; var ey = ay / dh * Pad;
			p0 -= ex + ey; p1 += ex - ey; p2 += ex + ey; p3 += ey - ex;
		}

		// Local (centred, +/-ext) to device, so any sub-rect of the quad can be placed under the same affine.
		var centre = (p0 + p2) * 0.5f; var hx = (p1 - p0) * 0.5f; var hy = (p3 - p0) * 0.5f;
		int written = 0;
		void Band(float lx0, float ly0, float lx1, float ly1)
		{
			ReadOnlySpan<int> tri = stackalloc int[6] { 0, 1, 2, 2, 1, 3 };
			Span<Vector2> ctr = stackalloc Vector2[4] { new(lx0, ly0), new(lx1, ly0), new(lx0, ly1), new(lx1, ly1) };
			var v = Grow(rr, 6 * VertexStride.RoundedRect);
			int o = 0;
			foreach (var idx in tri)
			{
				var c = ctr[idx];
				var d = centre + hx * (c.X / ext.X) + hy * (c.Y / ext.Y);
				v[o] = d.X; v[o + 1] = d.Y; v[o + 2] = c.X; v[o + 3] = c.Y; v[o + 4] = hf.X; v[o + 5] = hf.Y;
				v[o + 6] = rad.X; v[o + 7] = rad.Y; v[o + 8] = rad.Z; v[o + 9] = rad.W; v[o + 10] = cr; v[o + 11] = cg; v[o + 12] = cb; v[o + 13] = color;
				v[o + 14] = ih.X; v[o + 15] = ih.Y; v[o + 16] = ic.X; v[o + 17] = ic.Y; v[o + 18] = ir.X; v[o + 19] = ir.Y; v[o + 20] = ir.Z; v[o + 21] = ir.W;
				o += VertexStride.RoundedRect;
			}
			written += 6;
		}

		// A border ring paints its frame and nothing else, yet one quad shades the whole box for it: a 3px border on
		// a 240x168 card shades forty times the pixels it can possibly change. Cut the middle out as four bands over
		// a hole small enough that every fragment in it would have come out at zero coverage anyway.
		if (ih.X >= 0f && ext.X > 0f && ext.Y > 0f)
		{
			// Inside the inner shape far enough that its coverage is exactly zero: the box, less the widest corner
			// radius, less the ramp. The shader divides the distance by the LARGER of the two axes' pixel rates, so
			// both insets use that one; twice it, because the derivative it reads is a 2x2 quad's estimate.
			var back = MathF.Max(MathF.Max(ir.X, ir.Y), MathF.Max(ir.Z, ir.W)) + 2f * MathF.Max(local.X, local.Y);
			float hx0 = MathF.Max(ic.X - ih.X + back, -ext.X), hx1 = MathF.Min(ic.X + ih.X - back, ext.X);
			float hy0 = MathF.Max(ic.Y - ih.Y + back, -ext.Y), hy1 = MathF.Min(ic.Y + ih.Y - back, ext.Y);
			// Four quads cost three extra draws' worth of vertices, so only when the hole is most of the box.
			if ((hx1 - hx0) * (hy1 - hy0) > ext.X * ext.Y * 2f)
			{
				StatRingBands++;
				Band(-ext.X, -ext.Y, ext.X, hy0);
				Band(-ext.X, hy1, ext.X, ext.Y);
				Band(-ext.X, hy0, hx0, hy1);
				Band(hx1, hy0, ext.X, hy1);
				return written;
			}
		}
		Band(-ext.X, -ext.Y, ext.X, ext.Y);
		return written;
	}

	internal IntPtr MakeBuffer(float[] data)
	{
		var size = data.Length * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	// Uploads directly from the buffer's backing array (no copy).
	internal IntPtr MakeBuffer(VertBuf data)
	{
		var size = data.Count * sizeof(float);
		var buf = _d.BufferPool.Rent(size, WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst);
		fixed (float* p = data.A) { wgpuQueueWriteBuffer(_d.Q, buf, 0, (IntPtr)p, (nuint)size); }
		return buf;
	}

	internal IntPtr Vbuf(VertBuf data, int stride, OwnedResources owned)
		=> owned is null ? MakeBuffer(data) : PackVerts(owned, data.Span, stride);

	internal IntPtr MakeUniform(int byteSize)
		=> _d.BufferPool.Rent(byteSize, WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst);

	internal IntPtr Vbuf(float[] data, int stride, OwnedResources owned)
		=> owned is null ? MakeBuffer(data) : PackVerts(owned, data, stride);

	/// <summary>A bag's arena is split once it reaches this, so one recording's geometry can never ask for a
	/// buffer larger than the device allows (wgpu's default maximum is 256 MB).</summary>
	private const int MaxArenaBytes = 64 << 20;

	/// <summary>Appends an op's vertices to its bag's current arena chunk, aligned so the range starts on a vertex
	/// boundary of its own stride, and returns the chunk and first-vertex index tagged negative:
	/// <see cref="RealizeOwnedVertices"/> swaps it for that chunk's buffer.</summary>
	private static IntPtr PackVerts(OwnedResources owned, ReadOnlySpan<float> data, int stride)
	{
		var arenas = owned.VertexArenas ??= new List<VertBuf>();
		if (arenas.Count == 0) { arenas.Add(new VertBuf()); }
		var arena = arenas[arenas.Count - 1];
		if (arena.Count > 0 && ((long)arena.Count + stride + data.Length) * sizeof(float) > MaxArenaBytes)
		{
			arena = new VertBuf();
			arenas.Add(arena);
		}

		var misaligned = arena.Count % stride;
		if (misaligned != 0) { arena.Grow(stride - misaligned).Clear(); }
		var first = arena.Count / stride;
		data.CopyTo(arena.Grow(data.Length));
		return (IntPtr)(-(((long)(arenas.Count - 1) << 40) | (uint)first) - 1);
	}

	/// <summary>Uploads a finished bag's arena chunks and points every op at the chunk it landed in.</summary>
	internal void RealizeOwnedVertices(List<DrawOp> ops, OwnedResources owned)
	{
		if (owned?.VertexArenas is not { Count: > 0 } arenas) { return; }
		var buffers = new IntPtr[arenas.Count];
		for (var c = 0; c < arenas.Count; c++)
		{
			var arena = arenas[c];
			if (arena.Count == 0) { continue; }
			var size = arena.Count * sizeof(float);
			var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Vertex | WGPUBufferUsage.CopyDst };
			buffers[c] = wgpuDeviceCreateBuffer(_d.Dev, &bd);
			fixed (float* p = arena.A) { wgpuQueueWriteBuffer(_d.Q, buffers[c], 0, (IntPtr)p, (nuint)size); }
			owned.Buffers.Add((nint)buffers[c]);
		}

		owned.VertexArenas = null;
		for (var i = 0; i < ops.Count; i++)
		{
			var tag = (nint)ops[i].Verts;
			if (tag >= 0) { continue; }
			var packed = -tag - 1;
			var op = ops[i];
			op.Verts = buffers[(int)(packed >> 40)];
			op.FirstVertex = (uint)(packed & 0xFFFFFFFF);
			ops[i] = op;
		}
	}

	internal IntPtr Ubuf(int size, OwnedResources owned)
	{
		if (owned is null) { return MakeUniform(size); }
		var bd = new WGPUBufferDescriptor { Size = (nuint)size, Usage = WGPUBufferUsage.Uniform | WGPUBufferUsage.CopyDst };
		var buf = wgpuDeviceCreateBuffer(_d.Dev, &bd);
		owned.Buffers.Add((nint)buf);
		return buf;
	}

	internal void SetSiteScissor(nint slot, Vector4 box) => _d.SetSiteScissor(slot, box);

	private Vector4 SiteScissor(nint slot) => _d.SiteScissor(slot);

	/// <summary>The site's clip entries the uniform can hold before one has to be folded per op instead.</summary>
	internal const int SiteUniformEntries = 4;

	/// <summary>
	/// Whether a replay site's clip can ride the site uniform rather than being folded into every op's. Mask
	/// entries cannot: they sample the clip mask bound with the OP, which the site block has no access to.
	/// </summary>
	internal static bool SiteCanCarry(in ClipData session)
	{
		if (session.Paths is not null) { return false; }
		var e = session.Entries;
		if (e is null) { return true; }
		if (e.Length > SiteUniformEntries) { return false; }
		for (int i = 0; i < e.Length; i++) { if (e[i].Mask) { return false; } }
		return true;
	}

	/// <summary>
	/// Fills a site slot: where the recording sits, and the site's own clip in DEVICE space. Writing this is the
	/// whole cost of moving a cached recording, in place of a rewrite of every one of its ops' clip uniforms.
	/// </summary>
	private void WriteSite(nint slot, in Matrix3x2 rm, in ClipData session, bool carry, float depth = 0f)
	{
		var u = _d.SiteSlab.SlotSpan(slot);
		u[0] = rm.M11; u[1] = rm.M21; u[2] = rm.M12; u[3] = rm.M22;
		// xoff.zw: the session clips written below are absolute device, while the fragment position the shader
		// tests them at is relative to the target - a layer sheet slot does not share the window's origin.
		u[4] = rm.M31; u[5] = rm.M32; u[6] = _basisOx; u[7] = _basisOy;
		int n = carry ? (session.Entries?.Length ?? 0) : 0;
		u[8] = n;
		var ab = carry ? session.Aabb : new Vector4(-1e9f, -1e9f, 1e9f, 1e9f);
		bool rect = ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f;
		u[9] = rect ? 1f : 0f;
		u[10] = ab.X; u[11] = ab.Y;
		// rect.x is the site's draw-order depth (see project); rect.y stays spare.
		u[12] = depth; u[13] = 0f; u[14] = ab.Z; u[15] = ab.W;

		float ix = -1e30f, iy = -1e30f, iz = 1e30f, iw = 1e30f;
		if (rect) { ix = ab.X + 1f; iy = ab.Y + 1f; iz = ab.Z - 1f; iw = ab.W - 1f; }
		for (int i = 0; i < n; i++)
		{
			ref readonly var e = ref session.Entries[i];
			int o = SiteUHeaderFloats + i * ClipEntryFloats;
			u[o + 0] = e.M.M11; u[o + 1] = e.M.M12; u[o + 2] = e.M.M21; u[o + 3] = e.M.M22;
			u[o + 4] = e.M.M31; u[o + 5] = e.M.M32; u[o + 6] = e.Exclude ? 1f : 0f; u[o + 7] = 0f;
			u[o + 8] = e.Rect.X; u[o + 9] = e.Rect.Y; u[o + 10] = e.Rect.Z; u[o + 11] = e.Rect.W;
			u[o + 12] = e.Radii.X; u[o + 13] = e.Radii.Y; u[o + 14] = e.Radii.Z; u[o + 15] = e.Radii.W;
			u[o + 16] = e.RadiiY.X; u[o + 17] = e.RadiiY.Y; u[o + 18] = e.RadiiY.Z; u[o + 19] = e.RadiiY.W;
			// A device pixel in the entry's own units: the longer image of the two unit steps.
			var kx = new Vector2(e.M.M11, e.M.M12).Length();
			var ky = new Vector2(e.M.M21, e.M.M22).Length();
			u[o + 20] = MathF.Max(MathF.Max(kx, ky), 1e-6f);
			u[o + 21] = e.Radii == e.RadiiY ? 1f : 0f;
			u[o + 22] = 0f; u[o + 23] = 0f;

			if (iz <= ix) { continue; }
			if (e.Exclude || MathF.Abs(e.M.M12) > 1e-6f || MathF.Abs(e.M.M21) > 1e-6f
				|| MathF.Abs(e.M.M11) < 1e-9f || MathF.Abs(e.M.M22) < 1e-9f)
			{
				ix = 1f; iz = 0f;
				continue;
			}
			const float inset = 0.2929f;
			float k = u[o + 20];
			float qL = e.Rect.X + MathF.Max(e.Radii.X, e.Radii.W) * inset + k, qR = e.Rect.Z - MathF.Max(e.Radii.Y, e.Radii.Z) * inset - k;
			float qT = e.Rect.Y + MathF.Max(e.RadiiY.X, e.RadiiY.Y) * inset + k, qB = e.Rect.W - MathF.Max(e.RadiiY.Z, e.RadiiY.W) * inset - k;
			float pL = (qL - e.M.M31) / e.M.M11, pR = (qR - e.M.M31) / e.M.M11;
			float pT = (qT - e.M.M32) / e.M.M22, pB = (qB - e.M.M32) / e.M.M22;
			ix = MathF.Max(ix, MathF.Min(pL, pR)); iz = MathF.Min(iz, MathF.Max(pL, pR));
			iy = MathF.Max(iy, MathF.Min(pT, pB)); iw = MathF.Min(iw, MathF.Max(pT, pB));
		}
		u[16] = ix; u[17] = iy; u[18] = iz; u[19] = iw;
	}

	/// <summary>The bind group for one site slot. Lives as long as the stamp, so a move rebinds nothing.</summary>
	private IntPtr MakeSiteBg(nint slot)
	{
		var e = new WGPUBindGroupEntry { Binding = 0, Buffer = _d.SiteSlab.BufferOf(slot), Offset = _d.SiteSlab.OffsetOf(slot), Size = SiteUBytes };
		var d = new WGPUBindGroupDescriptor { Layout = _d.SiteBgl, EntryCount = 1, Entries = &e };
		return wgpuDeviceCreateBindGroup(_d.Dev, &d);
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
	/// <summary>Placement, clip header and the site's first four clip entries.</summary>
	internal const int SiteUBytes = 464;
	private const int SiteUHeaderFloats = 20;
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
		// Only the header needs zeroing: its fields are written conditionally, while every entry slot the
		// shader will read is fully overwritten below, and slots past ctrl.x are never read.
		Array.Clear(cu, 0, ClipUHeaderFloats);
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
	/// <summary>How much of the ClipU a draw actually reads: the header plus its live entries. The rest of the
	/// slot holds stale floats the shader never samples, so it is neither cleared nor copied.</summary>
	private static int LiveClipUFloats(in ClipData cd, ClipEntry[] masks)
	{
		var n = (cd.Entries?.Length ?? 0) + (masks?.Length ?? 0);
		return ClipUHeaderFloats + Math.Min(n, ClipUniformEntries) * ClipEntryFloats;
	}

	/// <summary>
	/// A restamp where the replay transform moved but did not rotate or scale, and the session clip is the one the
	/// slot already holds. Then the op's own entries, every entry's <c>k</c> (a function of the linear parts alone)
	/// and all the shape fields are already right: only the placement, the session entries' translation, the rect
	/// and the inner box move. Patching those in place skips folding an array per op, rewriting twenty-four floats
	/// per entry and copying the slot -- the work a scrolling wall was doing for every op of every card, every frame.
	/// </summary>
	private bool PatchClipU(nint slot, in ClipData own, in ClipData session, in Matrix3x2 xform, in Matrix3x2 finv)
	{
		var cu = _d.ClipSlab.SlotSpan(slot);
		int na = own.Entries?.Length ?? 0;

		// The rect slot: the op's own box, cut by the session's mapped back through finv (FoldSessionAabb).
		var ab = own.Aabb;
		bool rotated = finv.M12 != 0 || finv.M21 != 0;
		if (IsFiniteAabb(session.Aabb) && !rotated)
		{
			var sa = session.Aabb;
			var q0 = new Vector2(sa.X * finv.M11 + sa.Y * finv.M21 + finv.M31, sa.X * finv.M12 + sa.Y * finv.M22 + finv.M32);
			var q1 = new Vector2(sa.Z * finv.M11 + sa.W * finv.M21 + finv.M31, sa.Z * finv.M12 + sa.W * finv.M22 + finv.M32);
			ab = new Vector4(
				MathF.Max(ab.X, MathF.Min(q0.X, q1.X)), MathF.Max(ab.Y, MathF.Min(q0.Y, q1.Y)),
				MathF.Min(ab.Z, MathF.Max(q0.X, q1.X)), MathF.Min(ab.W, MathF.Max(q0.Y, q1.Y)));
		}
		bool foldedAabb = ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f;
		cu[1] = foldedAabb ? 1f : 0f;
		if (foldedAabb) { cu[2] = ab.X; cu[3] = ab.Y; cu[6] = ab.Z; cu[7] = ab.W; }

		cu[12] = xform.M31; cu[13] = xform.M32;
		cu[14] = finv.M31 + finv.M11 * _basisOx + finv.M21 * _basisOy;
		cu[15] = finv.M32 + finv.M12 * _basisOx + finv.M22 * _basisOy;

		float ix = -1e30f, iy = -1e30f, iz = 1e30f, iw = 1e30f;
		if (foldedAabb) { ix = ab.X + 1f; iy = ab.Y + 1f; iz = ab.Z - 1f; iw = ab.W - 1f; }
		int n = na + (session.Entries?.Length ?? 0);
		for (int i = 0; i < n; i++)
		{
			// Only a session entry moves: its matrix is the session's under the replay transform.
			float m11, m12, m21, m22, m31, m32, rx, ry, rz, rw, radX, radY, radZ, radW, ryX, ryY, ryZ, ryW, k;
			bool excluded, masked;
			if (i < na)
			{
				ref readonly var e = ref own.Entries[i];
				m11 = e.M.M11; m12 = e.M.M12; m21 = e.M.M21; m22 = e.M.M22; m31 = e.M.M31; m32 = e.M.M32;
				rx = e.Rect.X; ry = e.Rect.Y; rz = e.Rect.Z; rw = e.Rect.W;
				radX = e.Radii.X; radY = e.Radii.Y; radZ = e.Radii.Z; radW = e.Radii.W;
				ryX = e.RadiiY.X; ryY = e.RadiiY.Y; ryZ = e.RadiiY.Z; ryW = e.RadiiY.W;
				excluded = e.Exclude; masked = e.Mask;
			}
			else
			{
				ref readonly var e = ref session.Entries[i - na];
				var sm = e.M;
				m11 = xform.M11 * sm.M11 + xform.M12 * sm.M21; m12 = xform.M11 * sm.M12 + xform.M12 * sm.M22;
				m21 = xform.M21 * sm.M11 + xform.M22 * sm.M21; m22 = xform.M21 * sm.M12 + xform.M22 * sm.M22;
				m31 = xform.M31 * sm.M11 + xform.M32 * sm.M21 + sm.M31;
				m32 = xform.M31 * sm.M12 + xform.M32 * sm.M22 + sm.M32;
				rx = e.Rect.X; ry = e.Rect.Y; rz = e.Rect.Z; rw = e.Rect.W;
				radX = e.Radii.X; radY = e.Radii.Y; radZ = e.Radii.Z; radW = e.Radii.W;
				ryX = e.RadiiY.X; ryY = e.RadiiY.Y; ryZ = e.RadiiY.Z; ryW = e.RadiiY.W;
				excluded = e.Exclude; masked = e.Mask;
				if (i < ClipUniformEntries)
				{
					int o2 = ClipUHeaderFloats + i * ClipEntryFloats;
					cu[o2 + 4] = m31; cu[o2 + 5] = m32;
				}
				else
				{
					// An overflow entry lives in the storage buffer, which this path does not hold; the caller's
					// guard keeps those on the full rewrite.
					return foldedAabb;
				}
			}

			if (iz <= ix) { continue; }
			k = i < ClipUniformEntries ? cu[ClipUHeaderFloats + i * ClipEntryFloats + 20] : 0f;
			if (masked || excluded || MathF.Abs(m12) > 1e-6f || MathF.Abs(m21) > 1e-6f || MathF.Abs(m11) < 1e-9f || MathF.Abs(m22) < 1e-9f)
			{
				ix = 1f; iz = 0f;
				continue;
			}
			const float inset = 0.2929f;
			float qL = rx + MathF.Max(radX, radW) * inset + k, qR = rz - MathF.Max(radY, radZ) * inset - k;
			float qT = ry + MathF.Max(ryX, ryY) * inset + k, qB = rw - MathF.Max(ryZ, ryW) * inset - k;
			float pL = (qL - m31) / m11, pR = (qR - m31) / m11, pT = (qT - m32) / m22, pB = (qB - m32) / m22;
			ix = MathF.Max(ix, MathF.Min(pL, pR)); iz = MathF.Min(iz, MathF.Max(pL, pR));
			iy = MathF.Max(iy, MathF.Min(pT, pB)); iw = MathF.Min(iw, MathF.Max(pT, pB));
		}
		cu[24] = ix; cu[25] = iy; cu[26] = iz; cu[27] = iw;
		_d.ClipSlab.MarkDirty(slot);
		return foldedAabb;
	}

	private bool RewriteClipU(nint slot, ClipData cd, Matrix3x2 xform, Matrix3x2 finv)
	{
		var more = FillClipU(cd, xform, finv, null, out var folded);
		_d.ClipSlab.Write(slot, _clipU, LiveClipUFloats(cd, null));
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
		_d.ClipSlab.Write(slot, _clipU, LiveClipUFloats(cd, masks.Entries));
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
		public VertBuf Solid, Rrect, Grad, Quad;
		public float BasisOx, BasisOy, BasisW, BasisH, BasisScale;
		/// <summary>The occlusion prepass' quads: 6 verts of (x, y, depth) per opaque cover.</summary>
		public float[] Prepass;
		public int PrepassVerts;
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
		float basisOx = 0f, float basisOy = 0f, float basisW = 0f, float basisH = 0f, List<WebGpuCommand> overlay = null, bool depth = false)
	{
		var build = BuildPass(cmds, m, outer, target, basisOx, basisOy, basisW, basisH, _unbounded, overlay);
		_singleBuild[0] = build;
		EncodePass(target, clear, load, _singleBuild, depth);
	}

	private readonly PassBuild[] _singleBuild = new PassBuild[1];

	/// <summary>Timestamp the current build started, for the op-build half of the stats line.</summary>

	// Builds the ops for one command list under a basis: the whole draw-side work of a pass, none of the encoding.
	internal PassBuild BuildPass(List<WebGpuCommand> cmds, in Matrix3x2 m, in ClipData outer, WebGpuRenderSurface target, float basisOx, float basisOy, float basisW, float basisH, Vector4 bound, List<WebGpuCommand> overlay = null, float basisScale = 1f)
	{
		long passStart = _emitStats && _passDepth == 0 ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

		var savedBasis = (_basisOx, _basisOy, _basisW, _basisH, _basisScale);
		_basisScale = basisScale;
		_basisOx = basisOx;
		_basisOy = basisOy;
		_basisW = basisW > 0f ? basisW : target.Width;
		_basisH = basisH > 0f ? basisH : target.Height;

		var b = new PassBuild { BasisOx = _basisOx, BasisOy = _basisOy, BasisW = _basisW, BasisH = _basisH, BasisScale = _basisScale, Bound = bound };
		var saved = (_solid, _rrect, _gradVerts, _quadVerts, _backdrops);
		b.Ops = RentOps();
		_solid = b.Solid = RentVerts();
		_rrect = b.Rrect = RentVerts();
		_gradVerts = b.Grad = RentVerts();
		_quadVerts = b.Quad = RentVerts();
		_backdrops = b.Backdrops = new List<BackdropCmd>();

		// Restarted once per frame, not per pass: a nested layer pass runs INSIDE the outer walk, so resetting there
		// would hand the rest of the outer pass depths below the ones it already issued.
		if (_passDepth == 0) { _siteSeq = 0; }
		long walkStart = _emitStats && _passDepth == 0 ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		_passDepth++;
		Walk(cmds, m, outer, b.Ops);
		if (overlay is not null) { Walk(overlay, Matrix3x2.Identity, ClipData.None, b.Ops); }
		_passDepth--;
		if (_emitStats && _passDepth == 0) { WalkTicks += System.Diagnostics.Stopwatch.GetTimestamp() - walkStart; }
		if (!_noOcclusionCull) { CullOccluded(b.Ops, bound); }
		BuildPrepass(b);
		long uploadStart = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;

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

		if (_emitStats)
		{
			var now = System.Diagnostics.Stopwatch.GetTimestamp();
			UploadTicks += now - uploadStart;
			if (_passDepth == 0) { OpsBuildTicks += now - passStart; }
		}

		(_solid, _rrect, _gradVerts, _quadVerts, _backdrops) = saved;
		(_basisOx, _basisOy, _basisW, _basisH, _basisScale) = savedBasis;
		return b;
	}

	// Encodes builds into one pass on the target, each under its own basis and scissor bound. Everything the pass
	// samples -- masks, the frame's layer sheets -- is encoded first.
	internal void EncodePass(WebGpuRenderSurface target, WColor? clear, bool load, IReadOnlyList<PassBuild> builds, bool depth = false)
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
		var depthView = depth ? _d.Pool.Rent(target.Width, target.Height, 1, WGPUTextureUsage.RenderAttachment, WebGpuDevice.DepthFormat) : IntPtr.Zero;
		var da = new WGPURenderPassDepthStencilAttachment
		{
			View = depthView,
			DepthLoadOp = WGPULoadOp.Clear,
			DepthStoreOp = WGPUStoreOp.Store,
			DepthClearValue = 0f,   // nothing covers anything until the prepass says so
			StencilLoadOp = WGPULoadOp.Undefined,
			StencilStoreOp = WGPUStoreOp.Undefined,
		};
		var desc = new WGPURenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &color, DepthStencilAttachment = depthView != IntPtr.Zero ? &da : null };
		var pass = wgpuCommandEncoderBeginRenderPass(Encoder, &desc);
		UseDepth = depthView != IntPtr.Zero;
		_depthView = depthView;
		var encodeStart = System.Diagnostics.Stopwatch.GetTimestamp();

		var savedBasis = (_basisOx, _basisOy, _basisW, _basisH, _basisScale);
		var savedBound = _bound;
		var enc = new PassEncoder(pass);
		foreach (var b in builds)
		{
			(_basisOx, _basisOy, _basisW, _basisH, _basisScale) = (b.BasisOx, b.BasisOy, b.BasisW, b.BasisH, b.BasisScale <= 0f ? 1f : b.BasisScale);
			_bound = b.Bound;
			var pst = new PassOps
			{
				Pass = pass,
				Target = target,
				Ops = b.Ops,
				Backdrops = b.Backdrops,
				PassBg = b.PassBg,
				SolidBuf = b.SolidBuf,
				SolidBufBytes = b.SolidBufBytes,
				RrectBuf = b.RrectBuf,
				RrectBufBytes = b.RrectBufBytes,
				GradBuf = b.GradBuf,
				GradBufBytes = b.GradBufBytes,
				QuadBuf = b.QuadBuf,
				QuadBufBytes = b.QuadBufBytes,
				Enc = enc,
			};
			if (UseDepth && b.PrepassVerts > 0)
			{
				float ps = b.BasisScale <= 0f ? 1f : b.BasisScale;
				var pv = MakeBuffer(b.Prepass);
				pst.Enc.Pipe(_d.DepthPrepassPipe);
				pst.Enc.Bg(0, b.PassBg);
				pst.Enc.Scissor(0, 0, (int)(b.BasisW / ps), (int)(b.BasisH / ps));
				pst.Enc.Vb(pv, 0, (nuint)(b.Prepass.Length * sizeof(float)));
				pst.Enc.Draw((uint)b.PrepassVerts, 0);
			}
			EncodeOps(0, b.Ops.Count, ref pst);
			pass = pst.Pass;   // a backdrop segment reopens the pass
			enc = pst.Enc;
			if (_emitStats && b.Ops.Count > 0 && (_emitStatsFrame++ % _emitStatsEvery) == 0)
			{
				WriteFrameStats(b.Ops.Count, ref pst);
			}
		}
		if (_emitStats) { EncodeTicks += System.Diagnostics.Stopwatch.GetTimestamp() - encodeStart; }
		(_basisOx, _basisOy, _basisW, _basisH, _basisScale) = savedBasis;
		_bound = savedBound;

		wgpuRenderPassEncoderEnd(pass);
		wgpuRenderPassEncoderRelease(pass);
		UseDepth = false;
		_depthView = IntPtr.Zero;
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
