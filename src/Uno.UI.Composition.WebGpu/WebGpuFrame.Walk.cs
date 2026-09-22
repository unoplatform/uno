// The walk: one pass over a frame's command tree. Matrices and clips compose top-down, every draw lands in the pass's
// shared buffers as a device-space op, a replayed recording of plain draws comes from its arena entry, and a layer
// pushes a target, walks its content there, and pops as one textured draw.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuFrame
{
	// The clip in force for a command: the replay site's (device space) narrowed by the command's own, carried from
	// the recording's space through m. Consecutive commands share their clip arrays, so the last result is kept.
	private struct ClipComposer
	{
		private Vector4 _aabb; private ClipEntry[] _entries; private PathClip[] _paths; private bool _inert, _has;
		private ClipData _out;

		public ClipData Compose(in ClipData outer, in ClipData c, in Matrix3x2 m, in Matrix3x2 inv, bool direct)
		{
			if (_has && c.Aabb == _aabb && ReferenceEquals(c.Entries, _entries) && ReferenceEquals(c.Paths, _paths) && c.ScissorInert == _inert) { return _out; }
			_aabb = c.Aabb; _entries = c.Entries; _paths = c.Paths; _inert = c.ScissorInert; _has = true;
			_out = ComposeClip(outer, c, m, inv, direct);
			return _out;
		}
	}

	// True when the clip constrains nothing: the root of a frame, or a replay site inside such a recording.
	private static bool IsNone(in ClipData c) => c.Entries is null && c.Paths is null && !IsFiniteAabb(c.Aabb);

	// `direct` = the matrix is the identity and the outer clip is none, so the command's own clip is the answer.
	private static ClipData ComposeClip(in ClipData outer, in ClipData c, in Matrix3x2 m, in Matrix3x2 inv, bool direct)
	{
		if (direct) { return c; }
		bool identity = m.IsIdentity;
		var r = outer;
		// The command's containment proof covers its own recorded clip; the replay site's can still cut it.
		r.ScissorInert = c.ScissorInert && outer.ScissorInert;
		r.Coverage = 0; r.CoverageFiltered = false; r.AabbInClipU = false; r.ScissorLoadBearing = false;
		if (IsFiniteAabb(c.Aabb))
		{
			var a = identity ? c.Aabb : TransformBounds(c.Aabb, m);
			r.Aabb = new Vector4(MathF.Max(r.Aabb.X, a.X), MathF.Max(r.Aabb.Y, a.Y), MathF.Min(r.Aabb.Z, a.Z), MathF.Min(r.Aabb.W, a.W));
		}
		// The command's entries keep their shape and gain the way back from device space into the recording's.
		if (c.Entries is { Length: > 0 } es)
		{
			int n = outer.Entries?.Length ?? 0;
			var arr = new ClipEntry[n + es.Length];
			if (n > 0) { Array.Copy(outer.Entries, arr, n); }
			for (int i = 0; i < es.Length; i++) { arr[n + i] = identity ? es[i] : es[i].Under(inv); }
			r.Entries = arr;
		}
		if (c.Paths is { Length: > 0 } ps)
		{
			if (identity && outer.Paths is null) { r.Paths = ps; }   // the same array, so its masks stay keyed on it
			else
			{
				int n = outer.Paths?.Length ?? 0;
				var arr = new PathClip[n + ps.Length];
				if (n > 0) { Array.Copy(outer.Paths, arr, n); }
				for (int i = 0; i < ps.Length; i++) { arr[n + i] = identity ? ps[i] : ps[i].Transformed(m); }
				r.Paths = arr;
			}
		}
		return r;
	}

	private static Matrix3x2 To3x2(in Matrix4x4 t) => new(t.M11, t.M12, t.M21, t.M22, t.M41, t.M42);

	private static Vector2 Map(Vector2 p, in Matrix3x2 m) => new(p.X * m.M11 + p.Y * m.M21 + m.M31, p.X * m.M12 + p.Y * m.M22 + m.M32);

	/// <summary>
	/// Emits the ops of a command list into the current build. <paramref name="m"/> maps the list's space to device
	/// pixels; <paramref name="outer"/> is the clip in force where the list is replayed, in device space.
	/// </summary>
	// Bounds of each run of CullChunk replayed records, in the list's own space. The visual tree culls leaves but
	// never recordings, so a list hands the walk every record it has -- thousands, of which a screenful survives.
	// Rejecting them a chunk at a time makes that cost follow what is on screen rather than what exists. Keyed on
	// the command list, which never changes once recorded.
	private const int CullChunk = 32;
	private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<List<WebGpuCommand>, Vector4[]> s_chunkBounds = new();

	/// <summary>
	/// Chunk boxes for a list, or null when it is too short to be worth it. A chunk holding anything but a
	/// replayed record gets an unbounded box, so it is never rejected as a group.
	/// </summary>
	private static Vector4[] ChunkBounds(List<WebGpuCommand> cmds)
	{
		if (cmds.Count < CullChunk * 2) { return null; }
		if (s_chunkBounds.TryGetValue(cmds, out var cached)) { return cached; }
		var chunks = new Vector4[(cmds.Count + CullChunk - 1) / CullChunk];
		for (int c = 0; c < chunks.Length; c++)
		{
			int lo = c * CullChunk, hi = Math.Min(lo + CullChunk, cmds.Count);
			var box = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
			for (int i = lo; i < hi; i++)
			{
				if (cmds[i] is not ReplayRefCmd rr) { box = ClipData.None.Aabb; break; }
				var b = TransformBounds(rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands), rr.Transform2);
				if (!IsFiniteAabb(b)) { box = ClipData.None.Aabb; break; }
				box = new Vector4(MathF.Min(box.X, b.X), MathF.Min(box.Y, b.Y), MathF.Max(box.Z, b.Z), MathF.Max(box.W, b.W));
			}
			chunks[c] = box;
		}
		s_chunkBounds.Add(cmds, chunks);
		return chunks;
	}

	private void Walk(List<WebGpuCommand> cmds, in Matrix3x2 m, in ClipData outer, List<DrawOp> ops)
	{
		bool identity = m.IsIdentity;
		var inv = Matrix3x2.Identity;
		if (!identity && !Matrix3x2.Invert(m, out inv)) { return; }   // a collapsed transform draws nothing
		bool direct = identity && IsNone(outer);
		var composer = new ClipComposer();
		var chunks = ChunkBounds(cmds);
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			if (chunks is not null && (ci % CullChunk) == 0)
			{
				var cb = chunks[ci / CullChunk];
				if (IsFiniteAabb(cb) && Culled(ClampToClip(TransformBounds(cb, m), outer)))
				{
					ci += CullChunk - 1;
					continue;
				}
			}
			var cmd = cmds[ci];
			switch (cmd.Kind)
			{
				// A replayed recording first: a long list is nothing else, and every kind tested before it is
				// a test every one of its thousands of records would pay.
				case CmdKind.ReplayRef:
					EmitReplay((ReplayRefCmd)cmd, m, inv, outer, direct, ops);
					break;
				case CmdKind.Rect:
					{
						var rc0 = (RectCommand)cmd;
						// A run of rects sharing a clip is one draw: their verts are contiguous in the pass buffer. One whose
						// edges miss the pixel grid leaves the run - the solid pipeline writes binary coverage, so only the
						// rounded-rect pipeline's SDF can antialias it.
						var cd = composer.Compose(outer, rc0.Clip, m, inv, direct);
						int j = ci; uint start = (uint)(_solid.Count / VertexStride.Solid);
						while (j < cmds.Count && cmds[j] is RectCommand rcj && (j == ci || ClipDataEquals(rcj.Clip, rc0.Clip)))
						{
							var (p0, p1, p2, p3) = identity ? (rcj.P0, rcj.P1, rcj.P2, rcj.P3) : (Map(rcj.P0, m), Map(rcj.P1, m), Map(rcj.P2, m), Map(rcj.P3, m));
							if (!PixelAligned(p0, p1, p2, p3)) { break; }
							AppendSolidRect(_solid, p0, p1, p2, p3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
							j++;
						}
						if (j == ci)
						{
							var (a0, a1, a2, a3) = identity ? (rc0.P0, rc0.P1, rc0.P2, rc0.P3) : (Map(rc0.P0, m), Map(rc0.P1, m), Map(rc0.P2, m), Map(rc0.P3, m));
							var ast = (uint)(_rrect.Count / VertexStride.RoundedRect);
							AppendAaRect(_rrect, rc0.Color, a0, a1, a2, a3);
							var aop = DrawOp.Shared(DrawKind.RoundedRect, ast, 6, IntPtr.Zero, cd, MakeClipBg(cd));
							aop.Bounds = AaRect(a0, a1, a2, a3);
							aop.Opaque = rc0.Color.A == 255 && ClipIsPlain(cd);
							ops.Add(aop);
							break;
						}
						var sop = DrawOp.Shared(DrawKind.Solid, start, (uint)((j - ci) * 6), IntPtr.Zero, cd, MakeClipBg(cd));
						// Only a run of one: a longer run's rects need not tile the box they share, so its box is not painted.
						if (j == ci + 1)
						{
							var (s0, s1, s2, s3) = identity ? (rc0.P0, rc0.P1, rc0.P2, rc0.P3) : (Map(rc0.P0, m), Map(rc0.P1, m), Map(rc0.P2, m), Map(rc0.P3, m));
							sop.Bounds = AaRect(s0, s1, s2, s3);
							sop.Opaque = rc0.Color.A == 255 && ClipIsPlain(cd);
						}
						ops.Add(sop);
						ci = j - 1;
						break;
					}
				case CmdKind.RoundedRect:
					{
						var rri = (RoundedRectCmd)cmd;
						var cd = composer.Compose(outer, rri.Clip, m, inv, direct);
						uint st = (uint)(_rrect.Count / VertexStride.RoundedRect);
						var (r0, r1, r2, r3) = identity ? (rri.P0, rri.P1, rri.P2, rri.P3) : (Map(rri.P0, m), Map(rri.P1, m), Map(rri.P2, m), Map(rri.P3, m));
						var rn = (uint)AppendRrect(_rrect, rri, r0, r1, r2, r3);
						var rop = DrawOp.Shared(DrawKind.RoundedRect, st, rn, IntPtr.Zero, cd, MakeClipBg(cd));
						rop.Bounds = AaRect(r0, r1, r2, r3);
						rop.Opaque = OpaqueRrect(rri) && ClipIsPlain(cd);
						ops.Add(rop);
						break;
					}
				case CmdKind.Path:
					{
						var pc = (PathCmd)cmd;
						if (_emitStats) { StatWalkPaths++; }
						var cd = composer.Compose(outer, pc.Clip, m, inv, direct);
						BuildSimpleOp(direct ? pc : Under(pc, m, identity, cd), ops, null, atlasScale: Vector2.One);
						break;
					}
				case CmdKind.Image:
					{
						var im = (ImageCmd)cmd;
						var cd = composer.Compose(outer, im.Clip, m, inv, direct);
						if (identity) { EmitImage(im, im.P0, im.P1, im.P2, im.P3, cd, ops); }
						else { EmitImage(im, Map(im.P0, m), Map(im.P1, m), Map(im.P2, m), Map(im.P3, m), cd, ops); }
						break;
					}
				case CmdKind.Gradient:
					{
						var gc = (GradientCmd)cmd;
						var cd = composer.Compose(outer, gc.Clip, m, inv, direct);
						EmitGradient(gc, m, identity, cd, ops);
						break;
					}
				case CmdKind.Shadow:
					{
						var sh = (ShadowCmd)cmd;
						var cd = composer.Compose(outer, sh.Clip, m, inv, direct);
						EmitShadow(direct ? sh : Under(sh, m, identity, cd), ops);
						break;
					}
				case CmdKind.Layer:
					{
						var lyr = (LayerCmd)cmd;
						var cd = composer.Compose(outer, lyr.Clip, m, inv, direct);
						EmitLayer(lyr, m, outer, cd, ops);
						break;
					}
				case CmdKind.Backdrop:
					{
						var bk = (BackdropCmd)cmd;
						// A backdrop splits the pass here so it samples the framebuffer resolved so far (see EncodeBackdropSegment).
						var cd = composer.Compose(outer, bk.Clip, m, inv, direct);
						var view = direct ? bk : new BackdropCmd { Effect = bk.Effect, Opacity = bk.Opacity, Clip = cd };
						int bi = _backdrops.Count; _backdrops.Add(view);
						ops.Add(DrawOp.Backdrop(bi, cd));
						break;
					}
			}
		}
	}

	// The path as it stands under m: its matrix composed, its bounds refreshed, the clip in force in place of its own.
	private static PathCmd Under(PathCmd pc, in Matrix3x2 m, bool identity, in ClipData cd)
	{
		var pm = identity ? pc.M : pc.M * m;
		Geo.Bounds(pc.Geometry, pm, out var min, out var max);
		if (pc.Stroke > 0f)
		{
			var half = new Vector2(pc.Stroke * 0.5f * MathF.Max(MathF.Abs(pm.M11), MathF.Abs(pm.M12)), pc.Stroke * 0.5f * MathF.Max(MathF.Abs(pm.M21), MathF.Abs(pm.M22)));
			min -= half; max += half;
		}
		return new PathCmd { Geometry = pc.Geometry, M = pm, Stroke = pc.Stroke, Join = pc.Join, Color = pc.Color, EvenOdd = pc.EvenOdd, BbMin = min, BbMax = max, Clip = cd };
	}

	private static ShadowCmd Under(ShadowCmd sh, in Matrix3x2 m, bool identity, in ClipData cd)
	{
		var sm = identity ? sh.M : sh.M * m;
		Geo.Bounds(sh.Geometry, sm, out var min, out var max);
		var ss = identity ? 1f : new Vector2(m.M11, m.M12).Length();
		return new ShadowCmd { Geometry = sh.Geometry, M = sm, BbMin = min, BbMax = max, EvenOdd = sh.EvenOdd, Color = sh.Color, SigmaX = sh.SigmaX * ss, SigmaY = sh.SigmaY * ss, Additive = sh.Additive, Clip = cd };
	}

	// One textured quad: into the pass's shared quad buffer per frame, else into a buffer the recording owns.
	private void EmitImage(ImageCmd im, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, in ClipData cd, List<DrawOp> ops, OwnedResources owned = null)
	{
		var dst = owned is null ? _quadVerts : new VertBuf();
		var first = (uint)(dst.Count / VertexStride.Quad);
		AppendQuad(dst, p0, p1, p2, p3, im.U0, im.V0, im.U1, im.V1);
		ops.Add(QuadOp(DrawKind.Image, dst, first, ImageBg(im, owned), cd, owned));
	}

	private DrawOp QuadOp(DrawKind kind, VertBuf verts, uint first, IntPtr group1, in ClipData cd, OwnedResources owned)
		=> owned is null
			? DrawOp.Shared(kind, first, 6, group1, cd, MakeClipBg(cd))
			: DrawOp.Own(kind, Vbuf(verts, VertexStride.Quad, owned), 6, group1, cd, MakeClipBg(cd, owned));

	private static void AppendQuad(VertBuf dst, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u0, float v0, float u1, float v1)
	{
		var q = Grow(dst, 6 * VertexStride.Quad);
		ReadOnlySpan<Vector2> pts = stackalloc Vector2[6] { p0, p1, p2, p0, p2, p3 };
		ReadOnlySpan<float> us = stackalloc float[6] { u0, u1, u1, u0, u1, u0 };
		ReadOnlySpan<float> vs = stackalloc float[6] { v0, v0, v1, v0, v1, v1 };
		for (int i = 0, o = 0; i < 6; i++, o += VertexStride.Quad) { q[o] = pts[i].X; q[o + 1] = pts[i].Y; q[o + 2] = us[i]; q[o + 3] = vs[i]; }
	}

	// The gradient's geometry is baked in its recording's space; under a replay transform the points move with it and
	// the radial's unit-ellipse map absorbs the inverse, so the gradient stays aligned with its quad.
	private void EmitGradient(GradientCmd gc, in Matrix3x2 m, bool identity, in ClipData cd, List<DrawOp> ops, OwnedResources owned = null)
	{
		var u = identity ? gc.Uniform : TransformedGradient(gc.Uniform, m);
		u[3] = _d.RampRow(u, (int)u[1]);
		var gbg = owned is null ? _d.GradSlab.Rent(_d.GradBgl, u) : GradientBg(u, owned);
		var (p0, p1, p2, p3) = identity ? (gc.P0, gc.P1, gc.P2, gc.P3) : (Map(gc.P0, m), Map(gc.P1, m), Map(gc.P2, m), Map(gc.P3, m));
		Span<Vector2> cover = stackalloc Vector2[OctSides * 3];
		var count = (uint)GradientCover(p0, p1, p2, p3, cd, cover);
		var dst = owned is null ? _gradVerts : new VertBuf();
		var first = (uint)(dst.Count / VertexStride.Quad);
		for (var t = 0; t < count; t++) { dst.Add(cover[t].X); dst.Add(cover[t].Y); dst.Add(0f); dst.Add(0f); }
		ops.Add(owned is null
			? DrawOp.Shared(DrawKind.Gradient, first, count, gbg, cd, MakeClipBg(cd))
			: DrawOp.Own(DrawKind.Gradient, Vbuf(dst, VertexStride.Quad, owned), count, gbg, cd, MakeClipBg(cd, owned)));
	}

	private static float[] TransformedGradient(float[] src, in Matrix3x2 m)
	{
		var u = (float[])src.Clone();
		var a = Map(new Vector2(u[4], u[5]), m); u[4] = a.X; u[5] = a.Y;
		float dt = m.M11 * m.M22 - m.M21 * m.M12;
		if (MathF.Abs(dt) < 1e-12f) { dt = dt < 0 ? -1e-12f : 1e-12f; }
		float i00 = m.M22 / dt, i01 = -m.M21 / dt, i10 = -m.M12 / dt, i11 = m.M11 / dt;
		if (u[0] < 0.5f)
		{
			// Linear: the direction is a covector, so it moves by the inverse transpose.
			float dx = u[6], dy = u[7];
			u[6] = i00 * dx + i10 * dy; u[7] = i01 * dx + i11 * dy;
			return u;
		}
		// Radial: centre and focal are points; the unit-ellipse map M acts on device deltas, so it becomes M * m^-1.
		int ob = WebGpuDevice.GradOriginBase;
		var o = Map(new Vector2(u[ob], u[ob + 1]), m); u[ob] = o.X; u[ob + 1] = o.Y;
		float m00 = u[6], m10 = u[7], m01 = u[ob + 2], m11 = u[ob + 3];
		u[6] = m00 * i00 + m01 * i10; u[ob + 2] = m00 * i01 + m01 * i11;
		u[7] = m10 * i00 + m11 * i10; u[ob + 3] = m10 * i01 + m11 * i11;
		return u;
	}

	// ------------------------------------------------------------------------------------------- replayed recordings

	/// <summary>Whether every command is a plain draw, so the recording can live in the arena. Memoised on the record.</summary>
	private static bool IsPlain(ReplayRefCmd rr)
		=> rr.Data is { } d ? d.PlainMemo ??= IsPlain(rr.Commands) : IsPlain(rr.Commands);

	private static bool IsPlain(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			if (cmds[i].Kind > CmdKind.Gradient) { return false; }
		}
		return cmds.Count > 0;
	}

	private void EmitReplay(ReplayRefCmd rr, in Matrix3x2 m, in Matrix3x2 inv, in ClipData outer, bool direct, List<DrawOp> ops)
	{
		// Cull against the replay site's clip BEFORE composing this record's own: the composed clip is the
		// intersection of the two, so whatever the site rejects the composition rejects as well. A list of
		// thousands of moving rows hands the walk every one of its records each frame (the visual tree culls
		// leaves, never recordings), so the scrolled-out ones are the hot path here. Composed and tested in
		// scalars: wasm copies the Matrix3x2/Vector4 forms rather than intrinsifying them, and at four thousand
		// records a frame that difference is milliseconds.
		var t = rr.Transform2;
		float a11 = t.M11, a12 = t.M12, a21 = t.M21, a22 = t.M22, a31 = t.M31, a32 = t.M32;
		if (!m.IsIdentity)
		{
			float b11 = m.M11, b12 = m.M12, b21 = m.M21, b22 = m.M22;
			float c11 = a11 * b11 + a12 * b21, c12 = a11 * b12 + a12 * b22;
			float c21 = a21 * b11 + a22 * b21, c22 = a21 * b12 + a22 * b22;
			float c31 = a31 * b11 + a32 * b21 + m.M31, c32 = a31 * b12 + a32 * b22 + m.M32;
			a11 = c11; a12 = c12; a21 = c21; a22 = c22; a31 = c31; a32 = c32;
		}
		var rm = new Matrix3x2(a11, a12, a21, a22, a31, a32);
		var ib = rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands);
		Vector4 db;
		if (a12 == 0f && a21 == 0f && ib.X <= ib.Z && ib.Y <= ib.W && IsFiniteAabb(ib))
		{
			float x0 = ib.X * a11 + a31, x1 = ib.Z * a11 + a31;
			float y0 = ib.Y * a22 + a32, y1 = ib.W * a22 + a32;
			float lo = x0 < x1 ? x0 : x1, hi = x0 < x1 ? x1 : x0;
			float top = y0 < y1 ? y0 : y1, bot = y0 < y1 ? y1 : y0;
			// The site's clip folds into the same compare, so a rejected record never touches ClipData again.
			float cx0 = lo, cy0 = top, cx1 = hi, cy1 = bot;
			var oa = outer.Aabb;
			if (IsFiniteAabb(oa))
			{
				if (cx0 < oa.X) { cx0 = oa.X; }
				if (cy0 < oa.Y) { cy0 = oa.Y; }
				if (cx1 > oa.Z) { cx1 = oa.Z; }
				if (cy1 > oa.W) { cy1 = oa.W; }
			}
			if (cx0 >= cx1 || cy0 >= cy1 || cx1 <= 0f || cy1 <= 0f || cx0 >= Target.Width || cy0 >= Target.Height) { return; }
			db = new Vector4(lo, top, hi, bot);
		}
		else
		{
			db = TransformBounds(ib, rm);
			if (Culled(ClampToClip(db, outer))) { return; }
		}
		var rc = ComposeClip(outer, rr.Clip, m, inv, direct);
		if (Culled(ClampToClip(db, rc))) { return; }
		if (IsPlain(rr)) { EmitArena(rr, rm, rc, ops); }
		else { if (_emitStats) { StatWalkedRecords++; } Walk(rr.Commands, rm, rc, ops); }
	}

	// How many stamps one recording keeps: enough for a template replayed at a handful of sites in a frame
	// without holding slab slots for sites that are long gone.
	private const int MaxStampsPerEntry = 4;
	// Ceiling on the adaptive cap: past this the slab slots and bind groups cost more than the re-stamping saves.
	private const int MaxStampsPerEntryHard = 64;

	// Arena entries offered for sharing, keyed by the content of the recording that built them. A templated list
	// records the same commands once per item, so without this every item builds and stores its own copy of one
	// geometry. Render-thread only. The pool's reference is uncounted: an entry whose recordings have all gone
	// stays here, claimable, until SweepEntryPool frees it.
	private static readonly Dictionary<long, WebGpuGeometryCache> s_entryPool = new();
	// How long an unreferenced entry stays claimable. A re-recorded visual reappears within a frame or two.
	private const long EntryPoolIdleFrames = 120;
	internal static int StatPoolHits, StatPoolAdds;

	/// <summary>Drops one hold on an entry, freeing its GPU resources once nothing holds it and the pool has let
	/// it go. A pooled entry survives at zero holders -- that is what makes it claimable again.</summary>
	private void ReleaseEntry(WebGpuGeometryCache e)
	{
		if (System.Threading.Interlocked.Decrement(ref e.Refs) > 0) { return; }
		e.IdleSince = _d.FrameSeq;
		if (e.ContentKey != 0) { return; }
		foreach (var st in e.Stamps) { _d.DeferCompiledRelease(null, st.Owned); _d.DeferSiteRelease(st.SiteBg, st.SiteSlot); }
		_d.DeferCompiledRelease(e.Owned, null);
	}

	/// <summary>
	/// Frees pooled entries no recording has held for a while. Called once per frame, before the walk.
	/// </summary>
	internal void SweepEntryPool()
	{
		if (s_entryPool.Count == 0) { return; }
		List<long> drop = null;
		foreach (var kv in s_entryPool)
		{
			var e = kv.Value;
			if (e.Refs > 0 || !ReferenceEquals(e.Device, _d) || _d.FrameSeq - e.IdleSince < EntryPoolIdleFrames) { continue; }
			(drop ??= new()).Add(kv.Key);
			foreach (var st in e.Stamps) { _d.DeferCompiledRelease(null, st.Owned); _d.DeferSiteRelease(st.SiteBg, st.SiteSlot); }
			_d.DeferCompiledRelease(e.Owned, null);
		}
		if (drop is not null) { foreach (var k in drop) { s_entryPool.Remove(k); } }
	}

	/// <summary>
	/// The content key of a recording, or 0 when it must not be shared. Only commands that reference no GPU
	/// resource of their own qualify: a pooled entry outlives the recording that built it, and an image view or a
	/// path geometry would be released out from under it. Memoised, since the command list is immutable.
	/// </summary>
	private static long ContentKey(WebGpuRenderRecord data, List<WebGpuCommand> cmds)
	{
		if (data.ContentKeyMemo is { } memo) { return memo; }
		long h = 1469598103934665603L;
		void Mix(long v) => h = unchecked((h ^ v) * 1099511628211L);
		void MixF(float f) => Mix(BitConverter.SingleToInt32Bits(f));
		void MixV2(Vector2 v) { MixF(v.X); MixF(v.Y); }
		void MixV4(Vector4 v) { MixF(v.X); MixF(v.Y); MixF(v.Z); MixF(v.W); }

		Mix(cmds.Count);
		foreach (var c in cmds)
		{
			if (c.Kind is not (CmdKind.Rect or CmdKind.RoundedRect or CmdKind.Gradient) || c.Clip.Paths is not null)
			{
				data.ContentKeyMemo = 0;
				return 0;
			}
			Mix((long)c.Kind);
			// By VALUE, never by array identity: each recording allocates its own clip array, so identity would give
			// two identical recordings different keys and they would never reach the equality check.
			Mix(c.Clip.ScissorInert ? 1 : 0);
			if (!c.Clip.ScissorInert) { MixV4(c.Clip.Aabb); }
			Mix(c.Clip.Coverage);
			Mix(c.Clip.CoverageFiltered ? 1 : 0);
			Mix(c.Clip.Entries?.Length ?? 0);
			if (c.Clip.Entries is { } ents)
			{
				foreach (var e in ents)
				{
					MixF(e.M.M11); MixF(e.M.M12); MixF(e.M.M21); MixF(e.M.M22); MixF(e.M.M31); MixF(e.M.M32);
					MixV4(e.Rect); MixV4(e.Radii); MixV4(e.RadiiY); Mix(e.Exclude ? 1 : 0); Mix(e.Mask ? 1 : 0);
				}
			}
			switch (c)
			{
				case RectCommand r:
					Mix(Argb(r.Color)); MixV2(r.P0); MixV2(r.P1); MixV2(r.P2); MixV2(r.P3);
					break;
				case RoundedRectCmd rr:
					Mix(Argb(rr.Color)); MixF(rr.Opacity); MixV2(rr.P0); MixV2(rr.P1); MixV2(rr.P2); MixV2(rr.P3);
					MixV2(rr.Half); MixV4(rr.Radii); MixV2(rr.InnerHalf); MixV2(rr.InnerCenter); MixV4(rr.InnerRadii);
					break;
				case GradientCmd g:
					MixV2(g.P0); MixV2(g.P1); MixV2(g.P2); MixV2(g.P3);
					// Skips slot 3 for the same reason as SameContent: it is assigned during op building.
					if (g.Uniform is { } u) { for (int j = 0; j < u.Length; j++) { if (j != 3) { MixF(u[j]); } } }
					break;
			}
		}
		// 0 is the "not poolable" marker, so never hand it back as a real key.
		if (h == 0) { h = 1; }
		data.ContentKeyMemo = h;
		return h;
	}

	private static int Argb(WColor c) => (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;

	// Bit equality, not value equality: the key hashes float BITS, so the check that guards it has to agree. They
	// differ on exactly one value -- NaN, which is never == to itself and would reject every candidate.
	private static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
	private static bool Same(Vector2 a, Vector2 b) => Same(a.X, b.X) && Same(a.Y, b.Y);
	private static bool Same(Vector4 a, Vector4 b) => Same(a.X, b.X) && Same(a.Y, b.Y) && Same(a.Z, b.Z) && Same(a.W, b.W);
	private static bool Same(Matrix3x2 a, Matrix3x2 b)
		=> Same(a.M11, b.M11) && Same(a.M12, b.M12) && Same(a.M21, b.M21) && Same(a.M22, b.M22) && Same(a.M31, b.M31) && Same(a.M32, b.M32);

	private static bool SameClip(in ClipData a, in ClipData b)
	{
		if (a.ScissorInert != b.ScissorInert) { return false; }
		if (!a.ScissorInert && !Same(a.Aabb, b.Aabb)) { return false; }
		if (a.Coverage != b.Coverage || a.CoverageFiltered != b.CoverageFiltered) { return false; }
		if (a.Paths is not null || b.Paths is not null) { return false; }
		int an = a.Entries?.Length ?? 0, bn = b.Entries?.Length ?? 0;
		if (an != bn) { return false; }
		for (int i = 0; i < an; i++)
		{
			var x = a.Entries[i]; var y = b.Entries[i];
			if (!Same(x.M, y.M) || !Same(x.Rect, y.Rect) || !Same(x.Radii, y.Radii) || !Same(x.RadiiY, y.RadiiY)
				|| x.Exclude != y.Exclude || x.Mask != y.Mask) { return false; }
		}
		return true;
	}

	/// <summary>Whether two command lists would build the same geometry -- the guard against a key collision.</summary>
	private static bool SameContent(List<WebGpuCommand> a, List<WebGpuCommand> b)
	{
		if (ReferenceEquals(a, b)) { return true; }
		if (a is null || b is null || a.Count != b.Count) { return false; }
		for (int i = 0; i < a.Count; i++)
		{
			WebGpuCommand x = a[i], y = b[i];
			if (x.Kind != y.Kind || !SameClip(x.Clip, y.Clip)) { return false; }
			switch (x)
			{
				case RectCommand r when y is RectCommand r2:
					if (Argb(r.Color) != Argb(r2.Color) || !Same(r.P0, r2.P0) || !Same(r.P1, r2.P1)
						|| !Same(r.P2, r2.P2) || !Same(r.P3, r2.P3)) { return false; }
					break;
				case RoundedRectCmd u when y is RoundedRectCmd u2:
					if (Argb(u.Color) != Argb(u2.Color) || !Same(u.Opacity, u2.Opacity)
						|| !Same(u.P0, u2.P0) || !Same(u.P1, u2.P1) || !Same(u.P2, u2.P2) || !Same(u.P3, u2.P3)
						|| !Same(u.Half, u2.Half) || !Same(u.Radii, u2.Radii) || !Same(u.InnerHalf, u2.InnerHalf)
						|| !Same(u.InnerCenter, u2.InnerCenter) || !Same(u.InnerRadii, u2.InnerRadii)) { return false; }
					break;
				case GradientCmd g when y is GradientCmd g2:
					if (!Same(g.P0, g2.P0) || !Same(g.P1, g2.P1) || !Same(g.P2, g2.P2) || !Same(g.P3, g2.P3)) { return false; }
					if ((g.Uniform is null) != (g2.Uniform is null)) { return false; }
					if (g.Uniform is { } ga && g2.Uniform is { } gb)
					{
						if (ga.Length != gb.Length) { return false; }
						// Slot 3 (header.w) is the ramp-texture row, which op building assigns and writes BACK into
						// the recorded command. It is derived from the stops that follow it, so two commands equal
						// everywhere else resolve to the same row; comparing it would only ever reject an entry for
						// having been built already.
						for (int j = 0; j < ga.Length; j++) { if (j != 3 && !Same(ga[j], gb[j])) { return false; } }
					}
					break;
				default:
					return false;
			}
		}
		return true;
	}

	internal static int StatArenaHits, StatWalkPaths, StatWalkedRecords;
	private static int _statArenaRebuilds, _statArMiss, _statArMasks, _statStamps;

	/// <summary>
	/// Replays a recording from its arena entry: geometry built once in the recording's own space, positioned on the
	/// GPU by the transform its ops' clip data carries. A move re-stamps that clip data and reuses the vertex buffers;
	/// only an atlas or mask entry that can no longer serve at the new scale forces a rebuild.
	/// </summary>
	private void EmitArena(ReplayRefCmd rr, in Matrix3x2 rm, in ClipData session, List<DrawOp> ops)
	{
		var entry = rr.Data.Compiled;
		if (_emitStats) { StatArenaHits++; }
		long t0 = _emitStats ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
		var key = entry is null ? ContentKey(rr.Data, rr.Commands) : 0;
		if (entry is null && key != 0
			&& s_entryPool.TryGetValue(key, out var shared) && ReferenceEquals(shared.Device, _d)
			&& SameContent(shared.Src, rr.Commands))
		{
			// Another recording already built this exact geometry: take a reference rather than build it again.
			System.Threading.Interlocked.Increment(ref shared.Refs);
			entry = shared;
			StoreCompiled(rr.Data, entry);
			if (_emitStats) { StatPoolHits++; }
		}
		bool miss = entry is null;
		if (miss || AtlasNeedsRebuild(entry, rm))
		{
			if (_emitStats) { _statArenaRebuilds++; if (miss) { _statArMiss++; } else { _statArMasks++; } }
			// Shared entries are released by their last holder (or the pool sweep), never by whoever rebuilds first.
			if (entry is not null) { ReleaseEntry(entry); }
			var owned = new OwnedResources();
			var built = new List<DrawOp>();
			bool hasPath = false; foreach (var c in rr.Commands) { if (c is PathCmd) { hasPath = true; break; } }
			int atlasBefore = WebGpuCoverage.AtlasHit + WebGpuCoverage.AtlasBaked;
			int maskBefore = WebGpuCoverage.ClipMasksBaked + WebGpuCoverage.FillMasksBaked + WebGpuCoverage.FillMaskHits;
			bool atlasSafe = TryAtlasScale(rm, out var scale);
			BuildCoalesced(rr.Commands, built, owned, atlasScale: atlasSafe ? scale : null, maskScale: atlasSafe ? scale : MaskScale(rm), place: new Vector2(rm.M31, rm.M32));
			RealizeOwnedVertices(built, owned);
			bool hasPathClip = false; foreach (var o in built) { if (o.Clip.Paths is not null) { hasPathClip = true; break; } }
			entry = new WebGpuGeometryCache
			{
				Ops = built,
				Owned = owned,
				Device = _d,
				HasAtlas = (WebGpuCoverage.AtlasHit + WebGpuCoverage.AtlasBaked) != atlasBefore,
				HasClipMask = WebGpuCoverage.ClipMasksBaked + WebGpuCoverage.FillMasksBaked + WebGpuCoverage.FillMaskHits != maskBefore,
				HasPathClip = hasPathClip,
				AtlasBlockedByScale = !atlasSafe && hasPath && WebGpuCoverage.AtlasEnabled,
				AtlasScale = scale,
				AtlasPhase = AtlasPhase(rm),
				MaskScale = MaskScale(rm),
			};
			entry.Refs = 1;
			entry.ContentKey = key;
			if (key != 0)
			{
				if (s_entryPool.TryGetValue(key, out var displaced) && !ReferenceEquals(displaced, entry))
				{
					// Unpooled from here on, so whoever still holds it frees it on release.
					displaced.ContentKey = 0;
					if (displaced.Refs <= 0) { ReleaseEntry(displaced); }
				}
				entry.Src = rr.Commands;
				s_entryPool[key] = entry;
				if (_emitStats) { StatPoolAdds++; }
			}
			StoreCompiled(rr.Data, entry);
			if (_emitStats) { RebuildTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0; t0 = System.Diagnostics.Stopwatch.GetTimestamp(); }
		}
		if (entry.SitesFrame != _d.FrameSeq)
		{
			entry.SitesLastFrame = entry.SitesThisFrame;
			entry.SitesThisFrame = 0;
			entry.SitesFrame = _d.FrameSeq;
		}
		entry.SitesThisFrame++;
		var basis = new Vector4(_basisOx, _basisOy, BasisW, BasisH);
		// The stamp for this replay site, if it already holds the right transform and clip: its ops go out untouched.
		StampSlot slot = null;
		foreach (var st in entry.Stamps)
		{
			if (st.Xform == rm && st.Basis == basis && ClipDataEquals(st.Clip, session)) { slot = st; break; }
		}
		if (slot is null)
		{
			if (_emitStats) { _statStamps++; }
			var finv = Matrix3x2.Invert(rm, out var inv) ? inv : Matrix3x2.Identity;
			var sessionEntries = SessionEntryCount(session, finv);
			// An in-place rewrite keeps the slots and bind groups, so take the stamp this frame has not used yet (its
			// draws would still be reading those uniforms), whose op count matches, and with no path mask to bake into
			// a fresh bag. Failing that, add a stamp of its own.
			StampSlot reuse = null;
			if (session.Paths is null && !entry.HasPathClip)
			{
				foreach (var st in entry.Stamps)
				{
					if (st.Frame != _d.FrameSeq && st.Bufs is not null && st.Bufs.Count == entry.Ops.Count && st.SessionEntries == sessionEntries) { reuse = st; break; }
				}
			}
			// Hold a stamp for every site the entry served last frame, so a wall of cards stops evicting itself.
			var cap = Math.Clamp(Math.Max(entry.SitesLastFrame, entry.Refs), MaxStampsPerEntry, MaxStampsPerEntryHard);
			if (reuse is null && entry.Stamps.Count >= cap)
			{
				// At the cap: take the least recently used one, dropping what it held. Only a stamp this frame has
				// not replayed can be taken -- its site uniform is still what the ops already queued for it read,
				// so rewriting it would draw that earlier site at THIS one's placement. An entry with more live
				// sites than the cap therefore grows past it: a site being drawn has to have a site slot.
				foreach (var st in entry.Stamps) { if (st.Frame != _d.FrameSeq && (reuse is null || st.Frame < reuse.Frame)) { reuse = st; } }
				if (reuse is not null)
				{
					if (reuse.Owned is not null) { _d.DeferRelease(reuse.Owned); }
					reuse.Owned = null; reuse.Bufs = null; reuse.Ops = null;
				}
			}
			var fresh = reuse is null || reuse.Bufs is null;
			slot = reuse ?? new StampSlot();
			if (reuse is null) { entry.Stamps.Add(slot); }
			if (slot.SiteSlot == 0)
			{
				slot.SiteSlot = _d.SiteSlab.Alloc();
				slot.SiteBg = MakeSiteBg(slot.SiteSlot);
			}
			// When the site's clip can ride the site uniform, no op's clip depends on where the recording sits any
			// more -- so a move writes THIS block and nothing else.
			var siteCarries = SiteCanCarry(session);
			WriteSite(slot.SiteSlot, rm, session, siteCarries);
			// The ops' scissor, once for the whole site.
			SetSiteScissor(slot.SiteSlot, IsFiniteAabb(session.Aabb) ? session.Aabb : ClipData.None.Aabb);
			// Every op of this site is then independent of where the site is, so a move reuses the list as it
			// stands -- no per-op work at all, which is the point of the site block.
			if (siteCarries && !fresh && slot.SiteOps)
			{
				slot.Frame = _d.FrameSeq;
				AppendSite(ops, slot.Ops, rm, session);
				return;
			}
			var stampOwned = fresh ? new OwnedResources() : slot.Owned;
			var stamped = fresh ? new List<DrawOp>(entry.Ops.Count) : slot.Ops;
			var bufs = fresh ? new List<nint>(entry.Ops.Count) : slot.Bufs;
			// A move, not a rotation or a scale, onto the clip the slot already holds: then only placement changes
			// and each op's ClipU can be patched where it lies. This is the scrolling case, which is the one that
			// restamps every op of every record every frame.
			// The site MOVED but did not rotate or scale, onto a clip of the same shape: then every op's ClipU is
			// right except for where it sits, and can be patched where it lies instead of folded, rebuilt and
			// copied. This is the scrolling case -- the one that restamps every op of every record every frame.
			// The basis is free to move with it: it reaches the ClipU only through two finv translation floats,
			// which the patch recomputes.
			var moved = !fresh
				&& session.Paths is null && !entry.HasPathClip
				&& slot.Xform.M11 == rm.M11 && slot.Xform.M12 == rm.M12
				&& slot.Xform.M21 == rm.M21 && slot.Xform.M22 == rm.M22
				&& !(IsFiniteAabb(session.Aabb) && (finv.M12 != 0 || finv.M21 != 0))
				&& SessionShapeUnchanged(slot.Clip, session);
			Dictionary<PathClip[], PathClip[]> pathsMemo = null;
			Dictionary<ClipEntry[], ClipEntry[]> entsMemo = null;
			ClipEntry[] entsNoneFolded = null;
			for (int i = 0; i < entry.Ops.Count; i++)
			{
				var op = entry.Ops[i];
				// The scissor: the op's own box under the transform, cut to the session's.
				var scissorClip = op.Clip;
				if (IsFiniteAabb(op.Clip.Aabb)) { scissorClip.Aabb = TransformBounds(op.Clip.Aabb, rm); }
				var sa = session.Aabb;
				scissorClip.Aabb = new Vector4(MathF.Max(scissorClip.Aabb.X, sa.X), MathF.Max(scissorClip.Aabb.Y, sa.Y), MathF.Min(scissorClip.Aabb.Z, sa.Z), MathF.Min(scissorClip.Aabb.W, sa.W));
				scissorClip.ScissorInert = op.Clip.ScissorInert && session.ScissorInert;
				// Overflow entries live in a storage buffer the patch does not hold, so those stay on the rebuild.
				if (moved && op.Clip.Paths is null
					&& (op.Clip.Entries?.Length ?? 0) + (session.Entries?.Length ?? 0) <= ClipUniformEntries)
				{
					scissorClip.AabbInClipU = PatchClipU(bufs[i], op.Clip, session, rm, finv);
					scissorClip.ScissorLoadBearing = !scissorClip.AabbInClipU;
					stamped[i] = op.WithClipSite(scissorClip, stamped[i].ClipBg, slot.SiteBg, siteCarries ? slot.SiteSlot : 0);
					continue;
				}
				// The ClipU: the op's own clip (recording space) plus the session's, folded back through the transform.
				var uClip = op.Clip;
				if (!siteCarries)
				{
					FoldSessionEntries(ref uClip, session.Entries, rm, ref entsMemo, ref entsNoneFolded);
					FoldSessionPaths(ref uClip, session.Paths, finv, ref pathsMemo);
					if (IsFiniteAabb(session.Aabb)) { FoldSessionAabb(ref uClip, session.Aabb, finv, rm); }
				}
				if (!fresh)
				{
					scissorClip.AabbInClipU = RewriteClipU(bufs[i], uClip, rm, finv);
					scissorClip.ScissorLoadBearing = !scissorClip.AabbInClipU;
					stamped[i] = op.WithClipSite(scissorClip, stamped[i].ClipBg, slot.SiteBg, siteCarries ? slot.SiteSlot : 0);
				}
				else
				{
					var clipBg = MakeClipBgOwned(uClip, stampOwned, rm, finv, out var buf, out var folded);
					scissorClip.AabbInClipU = folded;
					scissorClip.ScissorLoadBearing = !folded;
					bufs.Add(buf);
					stamped.Add(op.WithClipSite(scissorClip, clipBg, slot.SiteBg, siteCarries ? slot.SiteSlot : 0));
				}
			}
			slot.Owned = stampOwned; slot.Ops = stamped; slot.Bufs = bufs; slot.SiteOps = siteCarries;
			slot.Xform = rm; slot.Clip = session; slot.Basis = basis; slot.SessionEntries = sessionEntries;
			if (_emitStats) { StampTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0; }
		}
		else
		{
			// A matched stamp keeps its ops, but the site block they read is per-slot GPU state that another
			// render may have moved on from - rewrite it so the ops are placed and scissored for THIS replay.
			WriteSite(slot.SiteSlot, rm, session, slot.SiteOps);
			SetSiteScissor(slot.SiteSlot, IsFiniteAabb(session.Aabb) ? session.Aabb : ClipData.None.Aabb);
		}

		slot.Frame = _d.FrameSeq;
		AppendSite(ops, slot.Ops, rm, session);
	}

	// A stamp's ops into the pass list, each op's box lifted from its recording's space into device pixels -- that is
	// the space the occlusion cull compares in, and a stamp's ops are shared across the sites replaying it.
	private static void AppendSite(List<DrawOp> ops, List<DrawOp> stamped, in Matrix3x2 rm, in ClipData session)
	{
		// A rotated or skewed placement does not map a box to the box it paints, and a session clip beyond a plain
		// rect cuts the op somewhere its own clip does not record.
		bool boxes = rm.M12 == 0f && rm.M21 == 0f;
		bool plain = boxes && ClipIsPlain(session);
		foreach (var op in stamped)
		{
			var o = op;
			o.Bounds = boxes && op.Bounds != default ? TransformBounds(op.Bounds, rm) : default;
			o.Opaque = op.Opaque && plain;
			ops.Add(o);
		}
	}

	// An atlas quad or mask is baked for one replay scale; a different one, or a transform that now allows the
	// atlas where the build could not use it, rebuilds so the content is neither mis-sized nor left aliased.
	// The subpixel phase the atlas masks were baked for: only the FRACTION of the placement matters, so a cached
	// recording that moves by whole pixels still reuses its masks and only a fractional move rebakes them.
	private static Vector2 AtlasPhase(in Matrix3x2 t)
		=> new(t.M31 - MathF.Floor(t.M31), t.M32 - MathF.Floor(t.M32));

	private static bool AtlasNeedsRebuild(WebGpuGeometryCache entry, in Matrix3x2 transform)
		=> (entry.HasAtlas && !(TryAtlasScale(transform, out var scale) && SameAtlasScale(scale, entry.AtlasScale)))
			|| (entry.HasAtlas && AtlasPhase(transform) != entry.AtlasPhase)
			|| (entry.HasClipMask && !SameAtlasScale(MaskScale(transform), entry.MaskScale))
			|| (entry.AtlasBlockedByScale && TryAtlasScale(transform, out _));

	// ---------------------------------------------------------------------------------------------------- shadows

	/// <summary>
	/// Renders a drop shadow: blurred coverage of the silhouette offscreen, composited as a SrcIn-tinted quad at its
	/// device placement. Culled first when the blurred extent is entirely clipped or offscreen.
	/// </summary>
	private void EmitShadow(ShadowCmd sh, List<DrawOp> ops)
	{
		var pad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		var ext = ClampToClip(Inflate(new Vector4(sh.BbMin.X, sh.BbMin.Y, sh.BbMax.X, sh.BbMax.Y), pad), sh.Clip);
		if (Culled(ext))
		{
			return;
		}
		var blurView = Effects.RenderShadow(sh, out var origin, out var size, out var uv);
		var bg = TintedImageBg(blurView, sh.Color);
		ops.Add(DrawOp.Own(DrawKind.Image, MakeBuffer(TexturedQuad(origin, size, uv)), 6, bg, sh.Clip, MakeClipBg(sh.Clip)));
	}

	// ----------------------------------------------------------------------------------------------------- layers

	// True if the list, or a recording it replays, contains a backdrop: such content must sample the real framebuffer,
	// which a size-to-content surface lacks. Memoised on the records.
	private static bool HasBackdrop(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			switch (cmds[i])
			{
				case BackdropCmd: return true;
				case LayerCmd l when HasBackdrop(l.Commands): return true;
				case ReplayRefCmd rr when (rr.Data is { } d ? d.HasBackdropMemo ??= HasBackdrop(rr.Commands) : HasBackdrop(rr.Commands)): return true;
			}
		}
		return false;
	}

	/// <summary>
	/// A layer: its content renders into a target of its own (a slot of the frame's layer sheet when it is a plain
	/// group sized to its content, else a surface), then one textured draw brings it onto the parent. The draw's blend
	/// state is the group's composite mode; a colour matrix rides the image shader; a drop shadow composites the
	/// content's blurred alpha, tinted, first.
	/// </summary>
	private void EmitLayer(LayerCmd lyr, in Matrix3x2 m, in ClipData outer, in ClipData cd, List<DrawOp> ops)
	{
		var content = ClampToClip(TransformBounds(CmdListBounds(lyr.Commands), m), cd);
		bool plain = lyr.CompositeMode == 0 && lyr.ColorMatrix is null;
		if (plain)
		{
			// Content or shadow may each be empty (clipped out); an empty layer is culled, never rendered. A mask must
			// still erase outside its content and a colour matrix can turn transparent pixels opaque, so those stay.
			var vis = content;
			if (lyr.ShadowEffect is { } sfx)
			{
				var spad = MathF.Ceiling(3f * MathF.Max(sfx.SigmaX, sfx.SigmaY)) + 2f;
				vis = Union(vis, ClampToClip(Inflate(new Vector4(content.X + sfx.Dx, content.Y + sfx.Dy, content.Z + sfx.Dx, content.W + sfx.Dy), spad), cd));
			}
			if (Culled(vis)) { return; }
		}

		// Size to content: a plain group with no backdrop inside needs a target only as big as what it draws (plus,
		// for a shadow, the region the blur reads). Everything else keeps a target the size of the current one.
		bool sub = false;
		float subOx = 0f, subOy = 0f;
		int subW = 0, subH = 0;
		if (plain && _subLayerSizing && !HasBackdrop(lyr.Commands))
		{
			var lb = content;
			if (lyr.ShadowEffect is { } se)
			{
				lb = Inflate(content, MathF.Ceiling(3f * MathF.Max(se.SigmaX, se.SigmaY)) + 2f);
			}
			float ax0 = MathF.Max(_basisOx, MathF.Floor(lb.X)), ay0 = MathF.Max(_basisOy, MathF.Floor(lb.Y));
			float ax1 = MathF.Min(_basisOx + BasisW, MathF.Ceiling(lb.Z)), ay1 = MathF.Min(_basisOy + BasisH, MathF.Ceiling(lb.W));
			int rw = (int)(ax1 - ax0), rh = (int)(ay1 - ay0);
			// Round the target up to a coarse grid. Content bounds move by a pixel between frames, and every
			// distinct size is a texture the pool cannot reuse - which costs committed GPU memory for good, since
			// the allocator never hands a freed block back. The extra margin is transparent and scissored out.
			const int SizeQuantum = 64;
			rw = Math.Min((rw + SizeQuantum - 1) / SizeQuantum * SizeQuantum, (int)(_basisOx + BasisW - ax0));
			rh = Math.Min((rh + SizeQuantum - 1) / SizeQuantum * SizeQuantum, (int)(_basisOy + BasisH - ay0));
			if (rw >= 1 && rh >= 1 && ((float)rw < BasisW || (float)rh < BasisH))
			{
				sub = true; subOx = ax0; subOy = ay0; subW = rw; subH = rh;
			}
		}

		// Push the target. A sheet slot shares one pass with the frame's other layers at this depth and one blur
		// pyramid per blur radius; slots carrying a shadow sit on that pyramid's top-level texel grid, a texel apart.
		WebGpuRenderSurface surface;
		WebGpuEffects.LayerSheet sheet = null;
		int slotX = 0, slotY = 0;
		float tx = _basisOx, ty = _basisOy, tw = BasisW, th = BasisH;   // the target's device rect
		LayerDepth++;
		if (sub && Effects.TryReserveLayerSlot(subW, subH, lyr.ShadowEffect is { } sfe ? 1 << WebGpuEffects.BlurLevels(MathF.Max(sfe.SigmaX, sfe.SigmaY)) : 1, out sheet, out slotX, out slotY))
		{
			surface = sheet.Surface;
			tx = subOx; ty = subOy; tw = subW; th = subH;
			sheet.Builds.Add(BuildPass(lyr.Commands, m, outer, surface, subOx - slotX, subOy - slotY, WebGpuEffects.LayerSheetSize, WebGpuEffects.LayerSheetSize, new Vector4(subOx, subOy, subOx + subW, subOy + subH)));
		}
		else if (sub)
		{
			surface = new WebGpuRenderSurface(_d, subW, subH, _d.Pool);
			tx = subOx; ty = subOy; tw = subW; th = subH;
			RenderInto(lyr.Commands, m, outer, surface, null, false, subOx, subOy, subW, subH);
			LayerSurfaces.Add(surface);
		}
		else
		{
			surface = new WebGpuRenderSurface(_d, (int)tw, (int)th, _d.Pool);
			RenderInto(lyr.Commands, m, outer, surface, null, false, tx, ty, tw, th);
			LayerSurfaces.Add(surface);
		}
		LayerDepth--;

		// The shadow: the content's alpha blurred over its region padded by the blur reach, drawn tinted and offset.
		if (lyr.ShadowEffect is { } fx)
		{
			var pad = MathF.Ceiling(3f * MathF.Max(fx.SigmaX, fx.SigmaY)) + 2f;
			var rg = Inflate(content, pad);
			float rx = MathF.Max(tx, MathF.Floor(rg.X)), ry = MathF.Max(ty, MathF.Floor(rg.Y));
			float rw = MathF.Min(tx + tw, MathF.Ceiling(rg.Z)) - rx, rh = MathF.Min(ty + th, MathF.Ceiling(rg.W)) - ry;
			if (rw >= 1f && rh >= 1f)
			{
				IntPtr blur; var uv = new Vector4(0f, 0f, 1f, 1f);
				if (sheet is not null)
				{
					blur = Effects.LayerSheetBlur(sheet, MathF.Max(fx.SigmaX, fx.SigmaY));
					float sx0 = rx - subOx + slotX, sy0 = ry - subOy + slotY;
					uv = new Vector4(sx0, sy0, sx0 + rw, sy0 + rh) / WebGpuEffects.LayerSheetSize;
				}
				else
				{
					blur = Effects.BlurPyramidRegion(surface.View, (int)tw, (int)th, rx - tx, ry - ty, rw, rh, fx.SigmaX, fx.SigmaY);
				}
				var sbg = TintedImageBg(blur, fx.Color);
				ops.Add(DrawOp.Own(DrawKind.Image, MakeBuffer(TexturedQuad(new Vector2(fx.Dx + rx, fx.Dy + ry), new Vector2(rw, rh), uv)), 6, sbg, cd, MakeClipBg(cd)));
			}
		}

		// Pop the target: the content as one textured quad over the target's rect, sampling its slot 1:1.
		var cuv = sheet is not null
			? new Vector4(slotX, slotY, slotX + tw, slotY + th) / WebGpuEffects.LayerSheetSize
			: new Vector4(0f, 0f, 1f, 1f);
		var compClip = cd;
		if (plain && IsFiniteAabb(content))
		{
			// Only what the content covers needs compositing; the scissor carries that, not the clip's rect slot.
			compClip.Aabb = content;
			compClip.ScissorInert = false;
			compClip.AabbInClipU = false;
			compClip.ScissorLoadBearing = true;
		}
		var bg = LayerBg(surface.View, lyr.ColorMatrix);
		var first = (uint)(_quadVerts.Count / VertexStride.Quad);
		AppendQuad(_quadVerts, new Vector2(tx, ty), new Vector2(tx + tw, ty), new Vector2(tx + tw, ty + th), new Vector2(tx, ty + th), cuv.X, cuv.Y, cuv.Z, cuv.W);
		ops.Add(DrawOp.Shared(lyr.CompositeMode == 1 ? DrawKind.Mask : DrawKind.Image, first, 6, bg, compClip, MakeClipBg(compClip)));
	}

	// The image bind group for a layer composite: no tint, no edge antialiasing (the layer's own alpha is the shape),
	// the colour matrix when the group carries one.
	private IntPtr LayerBg(IntPtr view, float[] colorMatrix)
	{
		var ubuf = MakeUniform(WebGpuDevice.ImageUniformBytes);
		var u = stackalloc float[36];
		for (var i = 0; i < 36; i++) { u[i] = 0f; }
		u[0] = 1f;
		if (colorMatrix is { Length: >= 20 } mm)
		{
			u[2] = 1f;
			u[8] = mm[0]; u[9] = mm[1]; u[10] = mm[2]; u[11] = mm[3];
			u[12] = mm[5]; u[13] = mm[6]; u[14] = mm[7]; u[15] = mm[8];
			u[16] = mm[10]; u[17] = mm[11]; u[18] = mm[12]; u[19] = mm[13];
			u[20] = mm[15]; u[21] = mm[16]; u[22] = mm[17]; u[23] = mm[18];
			u[24] = mm[4]; u[25] = mm[9]; u[26] = mm[14]; u[27] = mm[19];
		}
		wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)u, WebGpuDevice.ImageUniformBytes);
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
		e[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.Smp };
		e[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, null);
	}
}
