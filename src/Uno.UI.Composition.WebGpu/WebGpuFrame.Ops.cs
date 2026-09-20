// Turning commands into draw ops: the per-command builders the walk and the arena share, and the clip folding a
// replayed recording's ops need to carry their replay site's clip.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuFrame
{
	// VALUE equality: a recording's clip arrays are copy-on-write and immutable, so across frames they are almost
	// always the same instance — compare by reference first, then by content, which is far cheaper than the rebuild
	// or restamp a false "changed" would cause.
	/// <summary>
	/// Whether two session clips differ only in where they sit: same entries, same shapes, same linear parts, with
	/// the translations free to move. That is what an in-place restamp needs -- every entry's <c>k</c> comes from
	/// the linear parts alone, so it survives, and the translations are recomputed anyway. A scrolling card's clip
	/// moves with it every frame, so requiring full equality here would reject exactly the case worth patching.
	/// </summary>
	internal static bool SessionShapeUnchanged(in ClipData a, in ClipData b)
	{
		if (a.Paths is not null || b.Paths is not null) { return false; }
		if (a.Coverage != b.Coverage || a.CoverageFiltered != b.CoverageFiltered) { return false; }
		int n = a.Entries?.Length ?? 0;
		if (n != (b.Entries?.Length ?? 0)) { return false; }
		for (int i = 0; i < n; i++)
		{
			ref readonly var x = ref a.Entries[i];
			ref readonly var y = ref b.Entries[i];
			if (x.Exclude != y.Exclude || x.Mask != y.Mask) { return false; }
			if (x.Rect != y.Rect || x.Radii != y.Radii || x.RadiiY != y.RadiiY) { return false; }
			if (x.M.M11 != y.M.M11 || x.M.M12 != y.M.M12 || x.M.M21 != y.M.M21 || x.M.M22 != y.M.M22) { return false; }
		}
		return true;
	}

	internal static bool ClipDataEquals(in ClipData a, in ClipData b)
	{
		// Scissor-inert clips emit the full-surface scissor, so their (tight, cull-only) AABBs don't affect drawing.
		if (a.ScissorInert != b.ScissorInert) { return false; }
		if (!a.ScissorInert && a.Aabb != b.Aabb) { return false; }
		if (a.Coverage != b.Coverage || a.CoverageFiltered != b.CoverageFiltered) { return false; }
		if (!ReferenceEquals(a.Entries, b.Entries))
		{
			int an = a.Entries?.Length ?? 0, bn = b.Entries?.Length ?? 0;
			if (an != bn) { return false; }
			for (int i = 0; i < an; i++)
			{
				var x = a.Entries[i]; var y = b.Entries[i];
				if (x.M != y.M || x.Rect != y.Rect || x.Radii != y.Radii || x.RadiiY != y.RadiiY || x.Exclude != y.Exclude) { return false; }
			}
		}
		if (!ReferenceEquals(a.Paths, b.Paths))
		{
			int an = a.Paths?.Length ?? 0, bn = b.Paths?.Length ?? 0;
			if (an != bn) { return false; }
			for (int i = 0; i < an; i++)
			{
				var x = a.Paths[i]; var y = b.Paths[i];
				if (ReferenceEquals(x, y)) { continue; }
				if (x.EvenOdd != y.EvenOdd || x.Exclude != y.Exclude || !ReferenceEquals(x.Geometry, y.Geometry) || x.M != y.M) { return false; }
			}
		}
		return true;
	}

	// A fill clipped to an ellipse still rasterises its whole bounding quad; the corners run the fragment shader
	// only to be multiplied by zero coverage. Discarding them in the shader does not help — the fragments still
	// launch — but not emitting them does. The circumscribed octagon is tangent to the inscribed ellipse, so it
	// covers everything visible while rasterising ~17% less than the quad (16 sides measured no better on a UHD 620).
	private const int OctSides = 8;

	/// <summary>True when the clip is a single inclusive ellipse inscribed in the shape, so the quad's corners
	/// are guaranteed to be clipped away. An affine map preserves "ellipse inscribed in parallelogram", so this
	/// needs no comparison against the device-space quad.</summary>
	private static bool ClipIsInscribedEllipse(in ClipData clip)
	{
		if (clip.Paths is not null || clip.Entries is not { Length: 1 }) { return false; }
		var rc = clip.Entries[0];
		if (rc.Exclude || !rc.M.IsIdentity) { return false; }
		var hw = (rc.Rect.Z - rc.Rect.X) * 0.5f;
		var hh = (rc.Rect.W - rc.Rect.Y) * 0.5f;
		if (hw <= 0 || hh <= 0) { return false; }
		var tx = hw * 0.02f; var ty = hh * 0.02f;
		return MathF.Abs(rc.Radii.X - hw) <= tx && MathF.Abs(rc.Radii.Y - hw) <= tx
			&& MathF.Abs(rc.Radii.Z - hw) <= tx && MathF.Abs(rc.Radii.W - hw) <= tx
			&& MathF.Abs(rc.RadiiY.X - hh) <= ty && MathF.Abs(rc.RadiiY.Y - hh) <= ty
			&& MathF.Abs(rc.RadiiY.Z - hh) <= ty && MathF.Abs(rc.RadiiY.W - hh) <= ty;
	}

	/// <summary>
	/// Writes the circumscribed n-gon as n triangles fanned from the quad's centre. The ellipse inscribed in the
	/// parallelogram p0..p3 is c + u*cos(t) + v*sin(t) with u, v the half-edge vectors; pushing each sample out by
	/// 1/cos(pi/n) puts the polygon's edges tangent to it, so it covers everything the ellipse does.
	/// </summary>
	private static void OctagonTris(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Span<Vector2> tris)
	{
		var c = new Vector2((p0.X + p1.X + p2.X + p3.X) * 0.25f, (p0.Y + p1.Y + p2.Y + p3.Y) * 0.25f);
		var u = new Vector2((p1.X - p0.X) * 0.5f, (p1.Y - p0.Y) * 0.5f);
		var v = new Vector2((p3.X - p0.X) * 0.5f, (p3.Y - p0.Y) * 0.5f);
		var push = 1f / MathF.Cos(MathF.PI / OctSides);
		Span<Vector2> o = stackalloc Vector2[OctSides];
		for (var i = 0; i < OctSides; i++)
		{
			var a = (2f * MathF.PI * i + MathF.PI) / OctSides;
			var cs = MathF.Cos(a) * push; var sn = MathF.Sin(a) * push;
			o[i] = new Vector2(c.X + u.X * cs + v.X * sn, c.Y + u.Y * cs + v.Y * sn);
		}
		for (var i = 0; i < OctSides; i++)
		{
			tris[i * 3] = c;
			tris[i * 3 + 1] = o[i];
			tris[i * 3 + 2] = o[(i + 1) % OctSides];
		}
	}

	/// <summary>
	/// The triangles a gradient covers: the corner-cut octagon when the clip is the inscribed ellipse, else the quad's
	/// two triangles. Returns the number of points written.
	/// </summary>
	private static int GradientCover(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, in ClipData clip, Span<Vector2> pts)
	{
		if (ClipIsInscribedEllipse(clip))
		{
			OctagonTris(p0, p1, p2, p3, pts);
			return OctSides * 3;
		}
		pts[0] = p0; pts[1] = p1; pts[2] = p2;
		pts[3] = p0; pts[4] = p2; pts[5] = p3;
		return 6;
	}

	/// <summary>
	/// The replay scale to bake an arena recording's masks at, or false when the transform cannot be expressed as
	/// one. Rotation and skew are refused HERE and only here: an arena mask is baked from identity-space geometry
	/// and then mapped by the GPU transform, so a rotated replay would resample the coverage ramp.
	/// </summary>
	private static bool TryAtlasScale(in Matrix3x2 t, out Vector2 scale)
	{
		scale = new Vector2(t.M11, t.M22);
		var ok = MathF.Abs(t.M12) < 1e-4f && MathF.Abs(t.M21) < 1e-4f && t.M11 > 0f && t.M22 > 0f;
		if (!ok) { WebGpuCoverage.ScaleBlocked++; }
		return ok;
	}

	/// <summary>
	/// Device pixels per unit of the op's space for a mask bake: the transform's column lengths. Unlike the atlas
	/// scale this never refuses -- a rotated or skewed replay bakes at roughly device density and draws through its
	/// quad, softer than 1:1 but never aliased and never left without a route.
	/// </summary>
	private static Vector2 MaskScale(in Matrix3x2 t)
		=> new(MathF.Max(1e-3f, new Vector2(t.M11, t.M12).Length()), MathF.Max(1e-3f, new Vector2(t.M21, t.M22).Length()));

	private static bool SameAtlasScale(Vector2 a, Vector2 b)
		=> MathF.Abs(a.X - b.X) < 1e-3f && MathF.Abs(a.Y - b.Y) < 1e-3f;

	// Builds a plain recording's ops in its own space, into resources it owns: runs of same-clip rects coalesce into
	// one buffer and one draw, glyph runs into one atlas draw. The arena positions the result on the GPU.
	private void BuildCoalesced(List<WebGpuCommand> cmds, List<DrawOp> ops, OwnedResources owned, Vector2? atlasScale = null, Vector2? maskScale = null, Vector2 place = default)
	{
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			if (cmds[ci].Kind == CmdKind.Rect && cmds[ci] is RectCommand rc0)
			{
				// The replay transform is not known here, so these edges can end up rotated or off the pixel grid when
				// the recording is placed: they take the SDF pipeline's analytic coverage, as an image quad's already do.
				// It still coalesces - the rounded-rect vertex carries its own shape, so a run is one draw either way.
				_scratch.Clear();
				int j = ci;
				while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
				{
					AppendAaRect(_scratch, rcj.Color, rcj.P0, rcj.P1, rcj.P2, rcj.P3);
					j++;
				}
				ops.Add(DrawOp.Own(DrawKind.RoundedRect, Vbuf(_scratch, VertexStride.RoundedRect, owned), (uint)((j - ci) * 6), IntPtr.Zero, rc0.Clip, MakeClipBg(rc0.Clip, owned)));
				ci = j - 1;
			}
			else if (WebGpuCoverage.AtlasEnabled && atlasScale is { } asc && Coverage.TryAtlasBatch(cmds, ref ci, owned, asc, out var aop, place))
			{
				ops.Add(aop);
			}
			else if (cmds[ci].Kind == CmdKind.Path && cmds[ci] is PathCmd pc)
			{
				var density = maskScale ?? atlasScale ?? Vector2.One;
				var shape = Coverage.ShapeOf(pc, density);
				if (shape.Tris is null) { TryBigFill(pc, shape, ops, owned, density, filtered: atlasScale is null, place: place); }
				else { AddFan(pc, shape, ops, owned); }
			}
			else { BuildSimpleOp(cmds[ci], ops, owned, atlasScale, maskScale); }
		}
	}

	// The single-pass fill: the shape's own triangles as solid verts, the ring's per-vertex coverage carrying the
	// antialiasing in the alpha. A per-frame fill joins the pass's solid buffer so it coalesces with its neighbours.
	private void AddFan(PathCmd pc, WebGpuShapeCache.Shape shape, List<DrawOp> ops, OwnedResources owned)
	{
		float r = pc.Color.R / 255f, g = pc.Color.G / 255f, b = pc.Color.B / 255f, a = pc.Color.A / 255f;
		var off = pc.Offset; var tris = shape.Tris; var cov = shape.Cov;
		var clipBg = MakeClipBg(pc.Clip, owned);
		var count = (uint)(tris.Length / 2);
		var dst = owned is null ? _solid : _scratch;
		if (owned is not null) { _scratch.Clear(); }
		uint start = (uint)(dst.Count / VertexStride.Solid);
		var v = Grow(dst, tris.Length / 2 * VertexStride.Solid);
		for (int i = 0, o = 0; i < tris.Length; i += 2, o += VertexStride.Solid)
		{
			float ca = a * (cov is null ? 1f : cov[i >> 1]);
			v[o] = tris[i] + off.X; v[o + 1] = tris[i + 1] + off.Y; v[o + 2] = r; v[o + 3] = g; v[o + 4] = b; v[o + 5] = ca; v[o + 6] = 0f; v[o + 7] = 0f;
		}
		ops.Add(owned is null
			? DrawOp.Shared(DrawKind.Solid, start, count, IntPtr.Zero, pc.Clip, clipBg)
			: DrawOp.Own(DrawKind.Solid, Vbuf(_scratch, VertexStride.Solid, owned), count, IntPtr.Zero, pc.Clip, clipBg));
	}

	// A fill without tiling triangles (self-overlap, too thin for the ring, or simply refused) draws through an exact
	// coverage mask: a cached entry when the shape is keyable, else a per-frame bake. The scale is the device density
	// to bake at, so a rotated replay still gets a mask and draws it through its quad -- filtered, since its texels
	// no longer land on pixels.
	private bool TryBigFill(PathCmd pf, WebGpuShapeCache.Shape shape, List<DrawOp> ops, OwnedResources owned, Vector2 scale, bool filtered, Vector2 place = default)
	{
		if (WebGpuCoverage.AtlasEnabled && Coverage.TryAtlasFill(pf, shape, ops, owned, scale, big: true, filtered: filtered, place: place)) { return true; }
		if (Coverage.TryMaskFill(pf, shape, owned, scale, filtered, out var op)) { ops.Add(op); return true; }
		return false;
	}

	// One command's op in the command's own space, into buffers the recording owns; the walk's per-frame path fills
	// (`owned` null) join the pass buffers instead.
	private void BuildSimpleOp(WebGpuCommand cmd, List<DrawOp> ops, OwnedResources owned, Vector2? atlasScale = null, Vector2? maskScale = null)
	{
		switch (cmd.Kind)
		{
			case CmdKind.Path:
				{
					var pf = (PathCmd)cmd;
					var density = maskScale ?? atlasScale ?? Vector2.One;
					var shape = Coverage.ShapeOf(pf, density);
					// A small axis-aligned shape (a glyph) draws from the coverage atlas: one tinted quad, antialiasing baked in.
					if (atlasScale is { } asc && Coverage.TryAtlasFill(pf, shape, ops, owned, asc)) { break; }
					if (shape.Tris is null) { TryBigFill(pf, shape, ops, owned, density, filtered: atlasScale is null); break; }
					AddFan(pf, shape, ops, owned);
					break;
				}
			case CmdKind.Image:
				{
					var im = (ImageCmd)cmd;
					EmitImage(im, im.P0, im.P1, im.P2, im.P3, im.Clip, ops, owned);
					break;
				}
			case CmdKind.Gradient:
				{
					var gc = (GradientCmd)cmd;
					EmitGradient(gc, Matrix3x2.Identity, true, gc.Clip, ops, owned);
					break;
				}
			case CmdKind.RoundedRect:
				{
					var rrc = (RoundedRectCmd)cmd;
					var tmp = RentRrect();
					AppendRrect(tmp, rrc, rrc.P0, rrc.P1, rrc.P2, rrc.P3);
					var buf = Vbuf(tmp, VertexStride.RoundedRect, owned);
					ReturnRrect(tmp);
					ops.Add(DrawOp.Own(DrawKind.RoundedRect, buf, 6, IntPtr.Zero, rrc.Clip, MakeClipBg(rrc.Clip, owned)));
					break;
				}
		}
	}

	// A recording-owned gradient uniform; per frame the gradient slab hands out a slot instead.
	private IntPtr GradientBg(float[] uniform, OwnedResources owned)
	{
		var bytes = (nuint)WebGpuDevice.GradientUniformBytes;
		var ubuf = Ubuf((int)bytes, owned);
		fixed (float* p = uniform) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, bytes); }
		var e = stackalloc WGPUBindGroupEntry[3];
		e[0] = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
		e[1] = new WGPUBindGroupEntry { Binding = 1, TextureView = _d.RampView };
		e[2] = new WGPUBindGroupEntry { Binding = 3, Sampler = _d.Smp };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 3, Entries = e };
		return Bg(ref bgd, owned);
	}

	private static bool PixelAligned(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		static bool Whole(float v) => MathF.Abs(v - MathF.Round(v)) < 1e-3f;
		return p0.Y == p1.Y && p1.X == p2.X && p2.Y == p3.Y && p3.X == p0.X
			&& Whole(p0.X) && Whole(p0.Y) && Whole(p2.X) && Whole(p2.Y);
	}

	// The image draw's bind group: texture, the sampler for its edge extension, and the uniform carrying opacity,
	// tint, colour matrix, uv rect and the edge-antialiasing flag.
	private IntPtr ImageBg(ImageCmd im, OwnedResources owned)
	{
		var ubuf = Ubuf(WebGpuDevice.ImageUniformBytes, owned);
		var op = stackalloc float[36];
		for (var zi = 0; zi < 36; zi++) { op[zi] = 0f; }
		bool hasMatrix = im.ColorMatrix is { Length: >= 20 };
		op[0] = im.Opacity; op[1] = im.TintMode; op[2] = hasMatrix ? 1f : 0f; op[3] = 0;
		// The quad's uv rect, and the edge-AA flag: the quad's silhouette is its own edges, unless it sits on whole pixels.
		op[28] = im.U0; op[29] = im.V0; op[30] = im.U1; op[31] = im.V1;
		op[32] = owned is null && PixelAligned(im.P0, im.P1, im.P2, im.P3) ? 0f : 1f;
		op[4] = im.Tint.X; op[5] = im.Tint.Y; op[6] = im.Tint.Z; op[7] = im.Tint.W;
		if (im.ColorMatrix is { Length: >= 20 } mm)
		{
			op[8] = mm[0]; op[9] = mm[1]; op[10] = mm[2]; op[11] = mm[3];        // m0
			op[12] = mm[5]; op[13] = mm[6]; op[14] = mm[7]; op[15] = mm[8];      // m1
			op[16] = mm[10]; op[17] = mm[11]; op[18] = mm[12]; op[19] = mm[13];  // m2
			op[20] = mm[15]; op[21] = mm[16]; op[22] = mm[17]; op[23] = mm[18];  // m3
			op[24] = mm[4]; op[25] = mm[9]; op[26] = mm[14]; op[27] = mm[19];    // off (5th column)
		}
		wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, WebGpuDevice.ImageUniformBytes);
		var entries = stackalloc WGPUBindGroupEntry[3];
		entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = im.View };
		entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.TiledSampler(im.ExtendX, im.ExtendY) };
		entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
		return Bg(ref bgd, owned);
	}

	// A widened (full-surface) scissor is sound when the op's rect constraint is enforced analytically: proven
	// non-clipping (ScissorInert), riding the ClipU rect slot (AabbInClipU), or derivable — every unstamped op's ClipU
	// is built from its own ClipData, whose AABB always folds in. Widening lets consecutive ops share one scissor.
	private static bool ScissorWidenable(in ClipData clip)
		=> clip.ScissorInert || clip.AabbInClipU || !clip.ScissorLoadBearing;

	private static bool IsFiniteAabb(Vector4 aabb)
		=> aabb.X > -1e8f || aabb.Y > -1e8f || aabb.Z < 1e8f || aabb.W < 1e8f;

	// Folds the replay site's finite AABB into the op's clip. Axis-aligned, it tightens the local AABB through finv
	// (and rides the ClipU rect slot); otherwise it becomes an entry with square corners in the session's space,
	// exact under any transform. Either way the whole rect constraint is analytic and the scissor can widen.
	private static void FoldSessionAabb(ref ClipData local, Vector4 sessionAabb, in Matrix3x2 finv, in Matrix3x2 t2)
	{
		if (finv.M12 != 0 || finv.M21 != 0)
		{
			ClipData.PushEntry(ref local, new ClipEntry { M = t2, Rect = sessionAabb });
			return;
		}
		var q0 = new Vector2(sessionAabb.X * finv.M11 + sessionAabb.Y * finv.M21 + finv.M31, sessionAabb.X * finv.M12 + sessionAabb.Y * finv.M22 + finv.M32);
		var q1 = new Vector2(sessionAabb.Z * finv.M11 + sessionAabb.W * finv.M21 + finv.M31, sessionAabb.Z * finv.M12 + sessionAabb.W * finv.M22 + finv.M32);
		local.Aabb = new Vector4(
			MathF.Max(local.Aabb.X, MathF.Min(q0.X, q1.X)), MathF.Max(local.Aabb.Y, MathF.Min(q0.Y, q1.Y)),
			MathF.Min(local.Aabb.Z, MathF.Max(q0.X, q1.X)), MathF.Min(local.Aabb.W, MathF.Max(q0.Y, q1.Y)));
	}

	// Folds the replay site's entries into the op's clip: each keeps its shape and reaches its own space from the
	// recording's through the replay transform. Exact under any affine.
	private static void FoldSessionEntries(ref ClipData local, ClipEntry[] sessionEntries, in Matrix3x2 t2)
	{
		if (sessionEntries is not { Length: > 0 }) { return; }
		local.Entries = Folded(local.Entries, sessionEntries, t2);
	}

	/// <summary>
	/// The op's entries followed by the session's, mapped into the op's space. One allocation of the final size,
	/// where pushing one at a time allocated (and copied) once per session entry.
	/// </summary>
	private static ClipEntry[] Folded(ClipEntry[] own, ClipEntry[] sessionEntries, in Matrix3x2 t2)
	{
		int n = own?.Length ?? 0;
		var arr = new ClipEntry[n + sessionEntries.Length];
		if (n > 0) { Array.Copy(own, arr, n); }
		for (int i = 0; i < sessionEntries.Length; i++) { arr[n + i] = sessionEntries[i].Under(t2); }
		return arr;
	}

	/// <summary>
	/// <see cref="FoldSessionEntries"/> memoised over one stamp. The session's entries and the replay transform are
	/// fixed there, so the result depends only on the op's own entries array -- and a run of commands shares one,
	/// because the recorder's clip arrays are copy-on-write. Turns a per-op allocation into one per distinct clip.
	/// </summary>
	private static void FoldSessionEntries(ref ClipData local, ClipEntry[] sessionEntries, in Matrix3x2 t2,
		ref Dictionary<ClipEntry[], ClipEntry[]> memo, ref ClipEntry[] noneFolded)
	{
		if (sessionEntries is not { Length: > 0 }) { return; }
		if (local.Entries is not { } own)
		{
			local.Entries = noneFolded ??= Folded(null, sessionEntries, t2);
			return;
		}
		memo ??= new Dictionary<ClipEntry[], ClipEntry[]>(ReferenceEqualityComparer.Instance);
		if (!memo.TryGetValue(own, out var folded)) { memo[own] = folded = Folded(own, sessionEntries, t2); }
		local.Entries = folded;
	}

	// How many entries a stamp adds beyond the op's own: the session's, plus its finite AABB under a rotation.
	private static int SessionEntryCount(in ClipData session, in Matrix3x2 finv)
		=> (session.Entries?.Length ?? 0) + (IsFiniteAabb(session.Aabb) && (finv.M12 != 0 || finv.M21 != 0) ? 1 : 0);

	// Folds device-space session path clips into a LOCAL-space clip the same way: each path's edges are mapped
	// through finv, so its mask bakes in the recording's space and ClipU's finv lands every fragment on it. One
	// folded list per (local list, stamp): ops sharing a local clip then share one mask, and the folded list is
	// what keys the mask cache.
	private static void FoldSessionPaths(ref ClipData local, PathClip[] sessionPaths, in Matrix3x2 finv, ref Dictionary<PathClip[], PathClip[]> memo)
	{
		if (sessionPaths is not { Length: > 0 }) { return; }
		var key = local.Paths ?? Array.Empty<PathClip>();
		memo ??= new();
		if (!memo.TryGetValue(key, out var folded))
		{
			folded = new PathClip[sessionPaths.Length + key.Length];
			for (int i = 0; i < sessionPaths.Length; i++) { folded[i] = sessionPaths[i].Transformed(finv); }
			Array.Copy(key, 0, folded, sessionPaths.Length, key.Length);
			memo[key] = folded;
		}
		local.Paths = folded;
	}
}
