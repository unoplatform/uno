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

public sealed unsafe partial class WebGpuPresentSession
{
	public void Replay(IRenderRecord data)
	{
		// During an async backend switch (e.g. the browser's on-canvas WebGPU init) a frame recorded by the
		// previous renderer can reach us; skip it rather than mis-cast — the next frame is recorded by this backend.
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			_d.BeginFrameResources();   // reclaim last frame's pooled textures/buffers + release its bind groups
			// The frame is recorded in logical coordinates; the root DPI scale is the walk's root matrix, applied at
			// present and never folded into a recording. The render itself waits for Dispose so the immediate-mode
			// overlay joins the same pass.
			_pendingCmds = rd.Commands;
			_pendingScale = _presentScale;
			_pendingClear = _presentClear ?? rd.ClearColor;
		}
	}

	// Renders WITHOUT the per-frame reset — for a nested offscreen render (RenderOffscreen) that may run inside an
	// enclosing frame; resetting the shared pools mid-frame would free the enclosing frame's in-flight resources.
	// The gate is reentrant, so a nested call inside an enclosing Replay is safe; an independent call is serialized.
	public void ReplayNested(IRenderRecord data)
	{
		if (data is not WebGpuRenderRecord rd) { return; }
		lock (_d.RenderGate)
		{
			RunFrame(rd.Commands, Matrix3x2.Identity, null, _presentClear ?? rd.ClearColor);
		}
	}

	// VALUE equality: a recording's clip arrays are copy-on-write and immutable, so across frames they are almost
	// always the same instance — compare by reference first, then by content, which is far cheaper than the rebuild
	// or restamp a false "changed" would cause.
	private static bool ClipDataEquals(in ClipData a, in ClipData b)
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

	/// <summary>
	/// The replay scale to bake an arena recording's masks at, or false when the transform cannot be expressed as
	/// one. Rotation and skew are refused HERE and only here: an arena mask is baked from identity-space geometry
	/// and then mapped by the GPU transform, so a rotated replay would resample the coverage ramp.
	/// </summary>
	private static bool TryAtlasScale(in Matrix3x2 t, out Vector2 scale)
	{
		scale = new Vector2(t.M11, t.M22);
		var ok = MathF.Abs(t.M12) < 1e-4f && MathF.Abs(t.M21) < 1e-4f && t.M11 > 0f && t.M22 > 0f;
		if (!ok) { ScaleBlocked++; }
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
	private void BuildCoalesced(List<WebGpuCommand> cmds, List<DrawOp> ops, OwnedResources owned, Vector2? atlasScale = null, Vector2? maskScale = null)
	{
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			if (cmds[ci] is RectCommand rc0)
			{
				_scratch.Clear();
				int j = ci;
				while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
				{
					AppendSolidRect(_scratch, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
					j++;
				}
				ops.Add(DrawOp.Own(DrawKind.Solid, Vbuf(_scratch, owned), (uint)((j - ci) * 6), IntPtr.Zero, rc0.Clip, MakeClipBg(rc0.Clip, owned)));
				ci = j - 1;
			}
			else if (_pathAtlas && atlasScale is { } asc && TryAtlasBatch(cmds, ref ci, owned, asc, out var aop))
			{
				ops.Add(aop);
			}
			else if (cmds[ci] is PathCmd pc)
			{
				var density = maskScale ?? atlasScale ?? Vector2.One;
				var shape = ShapeOf(pc, density);
				if (shape.Tris is null) { TryBigFill(pc, shape, ops, owned, density, filtered: atlasScale is null); }
				else { AddFan(pc, shape, ops, owned); }
			}
			else { BuildSimpleOp(cmds[ci], ops, owned, atlasScale, maskScale); }
		}
	}

	// A path's rasterisation inputs at the density the GPU draws the op's space at (see WebGpuShapeCache).
	private WebGpuShapeCache.Shape ShapeOf(PathCmd c, Vector2 scale)
	{
		var density = MathF.Max(scale.X, scale.Y);
		return c.Stroke > 0f ? _d.Shapes.GetStroke(c.Geometry, c.M, c.Stroke, density) : _d.Shapes.Get(c.Geometry, c.M, density, c.EvenOdd);
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
		for (int i = 0; i < tris.Length; i += 2)
		{
			float ca = a * (cov is null ? 1f : cov[i >> 1]);
			dst.Add(tris[i] + off.X); dst.Add(tris[i + 1] + off.Y); dst.Add(r); dst.Add(g); dst.Add(b); dst.Add(ca); dst.Add(0f); dst.Add(0f);
		}
		ops.Add(owned is null
			? DrawOp.Shared(DrawKind.Solid, start, count, IntPtr.Zero, pc.Clip, clipBg)
			: DrawOp.Own(DrawKind.Solid, Vbuf(_scratch, owned), count, IntPtr.Zero, pc.Clip, clipBg));
	}

	// A fill without tiling triangles (self-overlap, too thin for the ring, or simply refused) draws through an exact
	// coverage mask: a cached entry when the shape is keyable, else a per-frame bake. The scale is the device density
	// to bake at, so a rotated replay still gets a mask and draws it through its quad -- filtered, since its texels
	// no longer land on pixels.
	private bool TryBigFill(PathCmd pf, WebGpuShapeCache.Shape shape, List<DrawOp> ops, OwnedResources owned, Vector2 scale, bool filtered)
	{
		if (_pathAtlas && TryAtlasFill(pf, shape, ops, owned, scale, big: true, filtered: filtered)) { return true; }
		if (TryMaskFill(pf, shape, owned, scale, filtered, out var op)) { ops.Add(op); return true; }
		return false;
	}

	// One command's op in the command's own space, into buffers the recording owns; the walk's per-frame path fills
	// (`owned` null) join the pass buffers instead.
	private void BuildSimpleOp(WebGpuCommand cmd, List<DrawOp> ops, OwnedResources owned, Vector2? atlasScale = null, Vector2? maskScale = null)
	{
		switch (cmd)
		{
			case PathCmd pf:
				{
					var density = maskScale ?? atlasScale ?? Vector2.One;
					var shape = ShapeOf(pf, density);
					// A small axis-aligned shape (a glyph) draws from the coverage atlas: one tinted quad, antialiasing baked in.
					if (atlasScale is { } asc && TryAtlasFill(pf, shape, ops, owned, asc)) { break; }
					if (shape.Tris is null) { TryBigFill(pf, shape, ops, owned, density, filtered: atlasScale is null); break; }
					AddFan(pf, shape, ops, owned);
					break;
				}
			case ImageCmd im:
				EmitImage(im, im.P0, im.P1, im.P2, im.P3, im.Clip, ops, owned);
				break;
			case GradientCmd gc:
				EmitGradient(gc, Matrix3x2.Identity, true, gc.Clip, ops, owned);
				break;
			case RoundedRectCmd rrc:
				{
					var tmp = RentRrect();
					AppendRrect(tmp, rrc, rrc.P0, rrc.P1, rrc.P2, rrc.P3);
					var buf = Vbuf(tmp, owned);
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
		var entry = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
		var bgd = new WGPUBindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 1, Entries = &entry };
		return Bg(ref bgd, owned);
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
		// The quad's uv rect, and the edge-AA flag: every image draw is a plain quad whose silhouette is its own edges.
		op[28] = im.U0; op[29] = im.V0; op[30] = im.U1; op[31] = im.V1;
		op[32] = 1f;
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
		foreach (var src in sessionEntries) { ClipData.PushEntry(ref local, src.Under(t2)); }
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
