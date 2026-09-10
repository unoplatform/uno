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

public sealed unsafe partial class WebGpuPresentSession
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
	private void Walk(List<WebGpuCommand> cmds, in Matrix3x2 m, in ClipData outer, List<DrawOp> ops)
	{
		bool identity = m.IsIdentity;
		var inv = Matrix3x2.Identity;
		if (!identity && !Matrix3x2.Invert(m, out inv)) { return; }   // a collapsed transform draws nothing
		bool direct = identity && IsNone(outer);
		var composer = new ClipComposer();
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			var cmd = cmds[ci];
			switch (cmd)
			{
				case RectCommand rc0:
					{
						// A run of rects sharing a clip is one draw: their verts are contiguous in the pass buffer.
						var cd = composer.Compose(outer, rc0.Clip, m, inv, direct);
						int j = ci; uint start = (uint)(_solid.Count / VertexStride.Solid);
						while (j < cmds.Count && cmds[j] is RectCommand rcj && (j == ci || ClipDataEquals(rcj.Clip, rc0.Clip)))
						{
							var (p0, p1, p2, p3) = identity ? (rcj.P0, rcj.P1, rcj.P2, rcj.P3) : (Map(rcj.P0, m), Map(rcj.P1, m), Map(rcj.P2, m), Map(rcj.P3, m));
							AppendSolidRect(_solid, p0, p1, p2, p3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
							j++;
						}
						ops.Add(DrawOp.Shared(DrawKind.Solid, start, (uint)((j - ci) * 6), IntPtr.Zero, cd, MakeClipBg(cd)));
						ci = j - 1;
						break;
					}
				case RoundedRectCmd rri:
					{
						var cd = composer.Compose(outer, rri.Clip, m, inv, direct);
						uint st = (uint)(_rrect.Count / VertexStride.RoundedRect);
						if (identity) { AppendRrect(_rrect, rri, rri.P0, rri.P1, rri.P2, rri.P3); }
						else { AppendRrect(_rrect, rri, Map(rri.P0, m), Map(rri.P1, m), Map(rri.P2, m), Map(rri.P3, m)); }
						ops.Add(DrawOp.Shared(DrawKind.RoundedRect, st, 6, IntPtr.Zero, cd, MakeClipBg(cd)));
						break;
					}
				case PathCmd pc:
					{
						var cd = composer.Compose(outer, pc.Clip, m, inv, direct);
						BuildSimpleOp(direct ? pc : Under(pc, m, identity, cd), ops, null, atlasScale: Vector2.One);
						break;
					}
				case ImageCmd im:
					{
						var cd = composer.Compose(outer, im.Clip, m, inv, direct);
						if (identity) { EmitImage(im, im.P0, im.P1, im.P2, im.P3, cd, ops); }
						else { EmitImage(im, Map(im.P0, m), Map(im.P1, m), Map(im.P2, m), Map(im.P3, m), cd, ops); }
						break;
					}
				case GradientCmd gc:
					{
						var cd = composer.Compose(outer, gc.Clip, m, inv, direct);
						EmitGradient(gc, m, identity, cd, ops);
						break;
					}
				case ShadowCmd sh:
					{
						var cd = composer.Compose(outer, sh.Clip, m, inv, direct);
						EmitShadow(direct ? sh : Under(sh, m, identity, cd), ops);
						break;
					}
				case LayerCmd lyr:
					{
						var cd = composer.Compose(outer, lyr.Clip, m, inv, direct);
						EmitLayer(lyr, m, outer, cd, ops);
						break;
					}
				case BackdropCmd bk:
					{
						// A backdrop splits the pass here so it samples the framebuffer resolved so far (see EncodeBackdropSegment).
						var cd = composer.Compose(outer, bk.Clip, m, inv, direct);
						var view = direct ? bk : new BackdropCmd { Effect = bk.Effect, Opacity = bk.Opacity, Clip = cd };
						int bi = _backdrops.Count; _backdrops.Add(view);
						ops.Add(DrawOp.Backdrop(bi, cd));
						break;
					}
				case ReplayRefCmd rr:
					EmitReplay(rr, m, inv, outer, direct, ops);
					break;
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
		return new PathCmd { Geometry = pc.Geometry, M = pm, Stroke = pc.Stroke, Color = pc.Color, EvenOdd = pc.EvenOdd, BbMin = min, BbMax = max, Clip = cd };
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
		var dst = owned is null ? _quadVerts : new List<float>(6 * VertexStride.Quad);
		var first = (uint)(dst.Count / VertexStride.Quad);
		AppendQuad(dst, p0, p1, p2, p3, im.U0, im.V0, im.U1, im.V1);
		ops.Add(QuadOp(DrawKind.Image, dst, first, ImageBg(im, owned), cd, owned));
	}

	private DrawOp QuadOp(DrawKind kind, List<float> verts, uint first, IntPtr group1, in ClipData cd, OwnedResources owned)
		=> owned is null
			? DrawOp.Shared(kind, first, 6, group1, cd, MakeClipBg(cd))
			: DrawOp.Own(kind, Vbuf(verts, owned), 6, group1, cd, MakeClipBg(cd, owned));

	private static void AppendQuad(List<float> dst, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u0, float v0, float u1, float v1)
	{
		void Q(Vector2 pos, float u, float v) { dst.Add(pos.X); dst.Add(pos.Y); dst.Add(u); dst.Add(v); }
		Q(p0, u0, v0); Q(p1, u1, v0); Q(p2, u1, v1); Q(p0, u0, v0); Q(p2, u1, v1); Q(p3, u0, v1);
	}

	// The gradient's geometry is baked in its recording's space; under a replay transform the points move with it and
	// the radial's unit-ellipse map absorbs the inverse, so the gradient stays aligned with its quad.
	private void EmitGradient(GradientCmd gc, in Matrix3x2 m, bool identity, in ClipData cd, List<DrawOp> ops, OwnedResources owned = null)
	{
		var u = identity ? gc.Uniform : TransformedGradient(gc.Uniform, m);
		var gbg = owned is null ? _d.GradSlab.Rent(_d.GradBgl, u) : GradientBg(u, owned);
		var (p0, p1, p2, p3) = identity ? (gc.P0, gc.P1, gc.P2, gc.P3) : (Map(gc.P0, m), Map(gc.P1, m), Map(gc.P2, m), Map(gc.P3, m));
		var dst = owned is null ? _gradVerts : new List<float>(6 * VertexStride.Quad);
		var first = (uint)(dst.Count / VertexStride.Quad);
		AppendQuad(dst, p0, p1, p2, p3, 0f, 0f, 0f, 0f);
		ops.Add(QuadOp(DrawKind.Gradient, dst, first, gbg, cd, owned));
	}

	private static float[] TransformedGradient(float[] src, in Matrix3x2 m)
	{
		var u = (float[])src.Clone();
		var a = Map(new Vector2(u[4], u[5]), m); u[4] = a.X; u[5] = a.Y;
		if (u[0] < 0.5f)
		{
			var b = Map(new Vector2(u[6], u[7]), m); u[6] = b.X; u[7] = b.Y;
			return u;
		}
		// Radial: centre and focal are points; the unit-ellipse map M acts on device deltas, so it becomes M * m^-1.
		int ob = WebGpuDevice.GradOriginBase;
		var o = Map(new Vector2(u[ob], u[ob + 1]), m); u[ob] = o.X; u[ob + 1] = o.Y;
		float dt = m.M11 * m.M22 - m.M21 * m.M12;
		if (MathF.Abs(dt) < 1e-12f) { dt = dt < 0 ? -1e-12f : 1e-12f; }
		float i00 = m.M22 / dt, i01 = -m.M21 / dt, i10 = -m.M12 / dt, i11 = m.M11 / dt;
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
			if (cmds[i] is not (RectCommand or RoundedRectCmd or PathCmd or ImageCmd or GradientCmd)) { return false; }
		}
		return cmds.Count > 0;
	}

	private void EmitReplay(ReplayRefCmd rr, in Matrix3x2 m, in Matrix3x2 inv, in ClipData outer, bool direct, List<DrawOp> ops)
	{
		var rm = m.IsIdentity ? To3x2(rr.Transform) : To3x2(rr.Transform) * m;
		var rc = ComposeClip(outer, rr.Clip, m, inv, direct);
		// A recording entirely clipped out or off-surface costs nothing.
		var bounds = ClampToClip(TransformBounds(rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands), rm), rc);
		if (bounds.X >= bounds.Z || bounds.Y >= bounds.W || bounds.Z <= 0 || bounds.W <= 0 || bounds.X >= _s.Width || bounds.Y >= _s.Height)
		{
			return;
		}
		if (IsPlain(rr)) { EmitArena(rr, rm, rc, ops); }
		else { Walk(rr.Commands, rm, rc, ops); }
	}

	internal static int StatArenaHits;
	private static int _statArenaRebuilds, _statArMiss, _statArMasks, _statStamps;

	/// <summary>
	/// Replays a recording from its arena entry: geometry built once in the recording's own space, positioned on the
	/// GPU by the transform its ops' clip data carries. A move re-stamps that clip data and reuses the vertex buffers;
	/// only an atlas or mask entry that can no longer serve at the new scale forces a rebuild.
	/// </summary>
	private void EmitArena(ReplayRefCmd rr, in Matrix3x2 rm, in ClipData session, List<DrawOp> ops)
	{
		var entry = rr.Data.Compiled;
		bool miss = entry is null;
		if (_emitStats) { StatArenaHits++; }
		if (miss || AtlasNeedsRebuild(entry, rm))
		{
			if (_emitStats) { _statArenaRebuilds++; if (miss) { _statArMiss++; } else { _statArMasks++; } }
			if (entry is not null) { _d.DeferRelease(entry.Owned); _d.DeferRelease(entry.StampOwned); }
			var owned = new OwnedResources();
			var built = new List<DrawOp>();
			bool hasPath = false; foreach (var c in rr.Commands) { if (c is PathCmd) { hasPath = true; break; } }
			int atlasBefore = AtlasHit + AtlasBaked;
			int maskBefore = ClipMasksBaked + FillMasksBaked + FillMaskHits;
			bool atlasSafe = TryAtlasScale(rm, out var scale);
			BuildCoalesced(rr.Commands, built, owned, atlasScale: atlasSafe ? scale : null, maskScale: atlasSafe ? scale : MaskScale(rm));
			bool hasPathClip = false; foreach (var o in built) { if (o.Clip.Paths is not null) { hasPathClip = true; break; } }
			entry = new WebGpuGeometryCache
			{
				Ops = built, Owned = owned, Device = _d,
				HasAtlas = (AtlasHit + AtlasBaked) != atlasBefore,
				HasClipMask = ClipMasksBaked + FillMasksBaked + FillMaskHits != maskBefore,
				HasPathClip = hasPathClip,
				AtlasBlockedByScale = !atlasSafe && hasPath && _pathAtlas,
				AtlasScale = scale, MaskScale = MaskScale(rm),
			};
			StoreCompiled(rr.Data, entry);
		}
		var basis = new Vector2(_basisOx, _basisOy);
		if (!entry.HasStamp || entry.StampXform != rm || entry.StampBasis != basis || !ClipDataEquals(entry.StampClip, session))
		{
			if (_emitStats) { _statStamps++; }
			var finv = Matrix3x2.Invert(rm, out var inv) ? inv : Matrix3x2.Identity;
			// An in-place rewrite keeps the slots and bind groups, so the entry count must match the last stamp's, the
			// last stamp must not be in this frame's submit (its draws still read those uniforms), and no path mask may
			// need baking into a fresh bag.
			var sessionEntries = SessionEntryCount(session, finv);
			var reuse = entry.HasStamp && entry.StampBufs is not null && entry.StampBufs.Count == entry.Ops.Count && entry.StampFrame != _d.FrameSeq
				&& session.Paths is null && !entry.HasPathClip && entry.StampSessionEntries == sessionEntries;
			if (!reuse && entry.StampOwned is not null) { _d.DeferRelease(entry.StampOwned); }
			var stampOwned = reuse ? entry.StampOwned : new OwnedResources();
			var stamped = reuse ? entry.StampedOps : new List<DrawOp>(entry.Ops.Count);
			var bufs = reuse ? entry.StampBufs : new List<nint>(entry.Ops.Count);
			Dictionary<PathClip[], PathClip[]> pathsMemo = null;
			for (int i = 0; i < entry.Ops.Count; i++)
			{
				var op = entry.Ops[i];
				// The scissor: the op's own box under the transform, cut to the session's.
				var scissorClip = op.Clip;
				if (IsFiniteAabb(op.Clip.Aabb)) { scissorClip.Aabb = TransformBounds(op.Clip.Aabb, rm); }
				var sa = session.Aabb;
				scissorClip.Aabb = new Vector4(MathF.Max(scissorClip.Aabb.X, sa.X), MathF.Max(scissorClip.Aabb.Y, sa.Y), MathF.Min(scissorClip.Aabb.Z, sa.Z), MathF.Min(scissorClip.Aabb.W, sa.W));
				scissorClip.ScissorInert = op.Clip.ScissorInert && session.ScissorInert;
				// The ClipU: the op's own clip (recording space) plus the session's, folded back through the transform.
				var uClip = op.Clip;
				FoldSessionEntries(ref uClip, session.Entries, rm);
				FoldSessionPaths(ref uClip, session.Paths, finv, ref pathsMemo);
				if (IsFiniteAabb(session.Aabb)) { FoldSessionAabb(ref uClip, session.Aabb, finv, rm); }
				if (reuse)
				{
					scissorClip.AabbInClipU = RewriteClipU(bufs[i], uClip, rm, finv);
					scissorClip.ScissorLoadBearing = !scissorClip.AabbInClipU;
					stamped[i] = op.WithClip(scissorClip, stamped[i].ClipBg);
				}
				else
				{
					var clipBg = MakeClipBgOwned(uClip, stampOwned, rm, finv, out var buf, out var folded);
					scissorClip.AabbInClipU = folded;
					scissorClip.ScissorLoadBearing = !folded;
					bufs.Add(buf);
					stamped.Add(op.WithClip(scissorClip, clipBg));
				}
			}
			entry.StampOwned = stampOwned; entry.StampedOps = stamped; entry.StampBufs = bufs; entry.StampFrame = _d.FrameSeq;
			entry.StampXform = rm; entry.StampClip = session; entry.StampBasis = basis; entry.StampSessionEntries = sessionEntries; entry.HasStamp = true;
		}
		ops.AddRange(entry.StampedOps);
	}

	// An atlas quad or mask is baked for one replay scale; a different one, or a transform that now allows the
	// atlas where the build could not use it, rebuilds so the content is neither mis-sized nor left aliased.
	private static bool AtlasNeedsRebuild(WebGpuGeometryCache entry, in Matrix3x2 transform)
		=> (entry.HasAtlas && !(TryAtlasScale(transform, out var scale) && SameAtlasScale(scale, entry.AtlasScale)))
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
		if (ext.X >= ext.Z || ext.Y >= ext.W || ext.Z <= 0 || ext.W <= 0 || ext.X >= _s.Width || ext.Y >= _s.Height)
		{
			return;
		}
		var blurView = RenderShadow(sh, out var origin, out var size, out var uv);
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
			if (vis.X >= vis.Z || vis.Y >= vis.W || vis.Z <= 0 || vis.W <= 0 || vis.X >= _s.Width || vis.Y >= _s.Height)
			{
				return;
			}
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
			if (rw >= 1 && rh >= 1 && ((float)rw < BasisW || (float)rh < BasisH))
			{
				sub = true; subOx = ax0; subOy = ay0; subW = rw; subH = rh;
			}
		}

		// Push the target. A sheet slot shares one pass with the frame's other layers at this depth and one blur
		// pyramid per blur radius; slots carrying a shadow sit on that pyramid's top-level texel grid, a texel apart.
		WebGpuRenderSurface surface;
		LayerSheet sheet = null;
		int slotX = 0, slotY = 0;
		float tx = _basisOx, ty = _basisOy, tw = BasisW, th = BasisH;   // the target's device rect
		_layerDepth++;
		if (sub && TryReserveLayerSlot(subW, subH, lyr.ShadowEffect is { } sfe ? 1 << BlurLevels(MathF.Max(sfe.SigmaX, sfe.SigmaY)) : 1, out sheet, out slotX, out slotY))
		{
			surface = sheet.Surface;
			tx = subOx; ty = subOy; tw = subW; th = subH;
			sheet.Builds.Add(BuildPass(lyr.Commands, m, outer, surface, subOx - slotX, subOy - slotY, LayerSheetSize, LayerSheetSize, new Vector4(subOx, subOy, subOx + subW, subOy + subH)));
		}
		else if (sub)
		{
			surface = new WebGpuRenderSurface(_d, subW, subH, _d.Pool);
			tx = subOx; ty = subOy; tw = subW; th = subH;
			RenderInto(lyr.Commands, m, outer, surface, null, false, subOx, subOy, subW, subH);
			_frameLayerSurfaces.Add(surface);
		}
		else
		{
			surface = new WebGpuRenderSurface(_d, (int)tw, (int)th, _d.Pool);
			RenderInto(lyr.Commands, m, outer, surface, null, false, tx, ty, tw, th);
			_frameLayerSurfaces.Add(surface);
		}
		_layerDepth--;

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
					blur = LayerSheetBlur(sheet, MathF.Max(fx.SigmaX, fx.SigmaY));
					float sx0 = rx - subOx + slotX, sy0 = ry - subOy + slotY;
					uv = new Vector4(sx0, sy0, sx0 + rw, sy0 + rh) / LayerSheetSize;
				}
				else
				{
					blur = BlurPyramidRegion(surface.View, (int)tw, (int)th, rx - tx, ry - ty, rw, rh, fx.SigmaX, fx.SigmaY);
				}
				var sbg = TintedImageBg(blur, fx.Color);
				ops.Add(DrawOp.Own(DrawKind.Image, MakeBuffer(TexturedQuad(new Vector2(fx.Dx + rx, fx.Dy + ry), new Vector2(rw, rh), uv)), 6, sbg, cd, MakeClipBg(cd)));
			}
		}

		// Pop the target: the content as one textured quad over the target's rect, sampling its slot 1:1.
		var cuv = sheet is not null
			? new Vector4(slotX, slotY, slotX + tw, slotY + th) / LayerSheetSize
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
