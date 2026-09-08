// Turning a recording into draw ops: what a recording qualifies to be replayed as, then the op build itself
// (coalescing runs of solids, residentizing fans, folding the session clip into each op).
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
			_d.SolidSlab.BeginFrame(); _d.RrectSlab.BeginFrame();   // reset the shared slabs' live sets for this frame
			_d.SolidTableSlab.BeginFrame(); _d.RrectTableSlab.BeginFrame();
			// Apply the root DPI scale to the whole (logical-coord) frame. Nested retained recordings keep their
			// command-list reference (only their Transform gains the scale) so the geometry cache still hits.
			// The actual render is deferred to Dispose so the immediate-mode overlay can be inlined (single pass).
			_pendingCmds = (_presentScale.X == 1f && _presentScale.Y == 1f)
				? rd.Commands
				: WebGpuCommandRecorder.TransformFor(rd.Commands, Matrix4x4.CreateScale(_presentScale.X, _presentScale.Y, 1f), ClipData.None);
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
			RunFrame(rd.Commands, _presentClear ?? rd.ClearColor);
		}
	}

	// VALUE equality: the rounded/path clip arrays are re-allocated every frame (copy-on-write Push / ClipCompose),
	// so a reference compare reports a stable clip as "changed" every frame -> a needless per-frame geometry rebuild
	// for every clipped cached recording. Compare by
	// content instead - far cheaper than the rebuild it prevents (Rounds is <=4 elements; the fan only when both have one).
	private static bool ClipDataEquals(in ClipData a, in ClipData b)
	{
		// Scissor-inert clips emit the full-surface scissor, so their (tight, cull-only) AABBs don't affect
		// drawing; two inert clips compare equal on the remaining components, letting coalescing merge runs
		// across visuals whose only difference is the layout-clip rectangle.
		if (a.ScissorInert != b.ScissorInert) { return false; }
		if (!a.ScissorInert && a.Aabb != b.Aabb) { return false; }
		// Both arrays are copy-on-write and a recording's clip is immutable, so across frames these are almost
		// always the SAME instance — compare by reference before walking them. This runs per replayed recording
		// per frame in every stamp guard, and the fan walk is O(fan length).
		if (!ReferenceEquals(a.Rounds, b.Rounds))
		{
			int an = a.Rounds?.Length ?? 0, bn = b.Rounds?.Length ?? 0;
			if (an != bn) { return false; }
			for (int i = 0; i < an; i++)
			{
				var x = a.Rounds[i]; var y = b.Rounds[i];
				if (x.Rect != y.Rect || x.Radii != y.Radii || x.RadiiY != y.RadiiY || x.Exclude != y.Exclude) { return false; }
			}
		}
		if ((a.PathFan is null) != (b.PathFan is null)) { return false; }
		if (a.PathFan is { } fa && b.PathFan is { } fb)
		{
			if (a.PathEvenOdd != b.PathEvenOdd || a.PathExclude != b.PathExclude) { return false; }
			if (!ReferenceEquals(fa, fb) && !((ReadOnlySpan<float>)fa).SequenceEqual(fb)) { return false; }
		}
		return true;
	}

	// Re-appendable = rect or rounded-rect: cheap to re-emit each frame into a shared per-pass buffer so they
	// coalesce across visuals. Glyphs, images and gradients stay cached and are spliced back in draw order.
	private static bool HasReappendable(ReplayRefCmd rr)
		=> rr.Data is { } d ? d.ReappendableMemo ??= HasReappendable(rr.Commands) : HasReappendable(rr.Commands);

	private static bool IsArenaSafe(ReplayRefCmd rr)
		=> rr.Data is { } d ? d.ArenaSafeMemo ??= IsArenaSafe(rr.Commands) : IsArenaSafe(rr.Commands);

	private static bool HasReappendable(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++) { if (cmds[i] is RectCommand or RoundedRectCmd) { return true; } }
		return false;
	}
	/// <summary>
	private static Vector4 QuadBounds(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		=> new(MathF.Min(MathF.Min(a.X, b.X), MathF.Min(c.X, d.X)), MathF.Min(MathF.Min(a.Y, b.Y), MathF.Min(c.Y, d.Y)),
			MathF.Max(MathF.Max(a.X, b.X), MathF.Max(c.X, d.X)), MathF.Max(MathF.Max(a.Y, b.Y), MathF.Max(c.Y, d.Y)));


	// A fill clipped to an ellipse still rasterises its whole bounding quad; the corners run the fragment shader
	// only to be multiplied by zero coverage. Discarding them in the shader does not help — the fragments still
	// launch — but not emitting them does. The corner-cut octagon (cut at 1 - 1/sqrt(2) along each edge) is
	// tangent to the inscribed ellipse, so it covers everything visible while rasterising ~17% less than the quad.
	// Circumscribed n-gon around the clip's ellipse. Area is n*tan(pi/n)/4 of the bounding box: 0.828 at n=8,
	// 0.796 at n=16, against 0.785 for the ellipse itself. 16 rasterises less in theory but measured no better on
	// a UHD 620, so keep the cheaper 8.
	private const int OctSides = 8;

	/// <summary>True when the clip is a single inclusive ellipse inscribed in the shape, so the quad's corners
	/// are guaranteed to be clipped away. An affine map preserves "ellipse inscribed in parallelogram", so this
	/// needs no comparison against the device-space quad.</summary>
	private static bool ClipIsInscribedEllipse(in ClipData clip)
	{
		if (clip.PathFan is not null || clip.Rounds is not { Length: 1 }) { return false; }
		var rc = clip.Rounds[0];
		if (rc.Exclude) { return false; }
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
	/// The replay scale to bake an arena recording's masks at, or null when the transform cannot be expressed as
	/// one. Rotation and skew are refused HERE and only here: an arena mask is baked from identity-space geometry
	/// and then mapped by the GPU transform, so a rotated replay would resample the coverage ramp. A pure scale
	/// just needs the mask rasterized at the size the shape covers on screen, which is what the scale carries.
	/// </summary>
	private static bool TryAtlasScale(Matrix4x4 t, out Vector2 scale)
	{
		scale = new Vector2(t.M11, t.M22);
		var ok = MathF.Abs(t.M12) < 1e-4f && MathF.Abs(t.M21) < 1e-4f && t.M11 > 0f && t.M22 > 0f;
		if (!ok) { ScaleBlocked++; }
		return ok;
	}

	private static bool SameAtlasScale(Vector2 a, Vector2 b)
		=> MathF.Abs(a.X - b.X) < 1e-3f && MathF.Abs(a.Y - b.Y) < 1e-3f;

	private static bool IsArenaSafe(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			var c = cmds[i];
			// Solid/image/gradient/path all route device fc through finv; the path stencil fan carries the xform via
			// the shared ClipU layout (ClipBgl binds to both stencil + cover). A rect/rounded clip is fine (clipCov
			// maps fc back via finv); a PATH clip uses the depth mask (no finv) so it's still excluded.
			// A per-command path fan does not disqualify: the arena bakes geometry at IDENTITY, so the fan is
			// in identity space too and the re-stamp maps it to device per frame (a handful of points) instead of
			// re-baking the whole recording. Rejecting it sent these to the rebuild-on-move path — 399 recordings
			// per frame on RenderStress_Gradients.
			if (c is not (RectCommand or ImageCmd or GradientCmd or PathFill)) { return false; }
		}
		return cmds.Count > 0;
	}

	// The NDC->NDC affine that maps the recording's own (identity-baked) NDC verts to the replay transform `t`
	// (device->device). Derived so re-stamping this uniform reproduces baking `t` into the verts: with A = the
	// device->NDC map (surface size), the vertex xform is A·T·A⁻¹. Lets a moved cached visual reuse its geometry.
	private Matrix3x2 ArenaXform(Matrix4x4 t)
	{
		float w = _s.Width, h = _s.Height;
		float a = t.M11, b = t.M21, c = t.M12, d = t.M22, e = t.M41, f = t.M42;
		return new Matrix3x2(
			a, -c * w / h,
			-b * h / w, d,
			a + b * h / w + 2f * e / w - 1f,
			-(c * w / h + d) - 2f * f / h + 1f);
	}

	// Builds ops for a command list, COALESCING runs of consecutive same-clip solid rects into one vertex buffer +
	// one draw (a Border's background+edges collapse from 4 draws to 1). Used for cached recordings — the per-command
	// BuildSimpleOp path did not coalesce, so every cached visual emitted a draw per rect (a major draw-count source
	// on Intel, where per-draw overhead dominates — see the RenderDoc capture). Coalesced rects share a clip so they
	// share the arena xform (one clip bind group), staying correct under re-stamp.
	private void BuildCoalesced(List<WebGpuCommand> cmds, List<DrawOp> ops, OwnedResources owned, int pathSlot, Vector2? atlasScale = null)
	{
		float slotBits = System.BitConverter.Int32BitsToSingle(pathSlot);
		for (int ci = 0; ci < cmds.Count; ci++)
		{
			if (cmds[ci] is RectCommand rc0)
			{
				_scratch.Clear();
				int j = ci;
				while (j < cmds.Count && cmds[j] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
				{
					float vr = rcj.Color.R / 255f, vg = rcj.Color.G / 255f, vb = rcj.Color.B / 255f, va = rcj.Color.A / 255f;
					PushVert(rcj.P0, vr, vg, vb, va); PushVert(rcj.P1, vr, vg, vb, va); PushVert(rcj.P2, vr, vg, vb, va);
					PushVert(rcj.P0, vr, vg, vb, va); PushVert(rcj.P2, vr, vg, vb, va); PushVert(rcj.P3, vr, vg, vb, va);
					j++;
				}
				var rvb = Vbuf(_scratch, owned);
				ops.Add(new DrawOp(DrawKind.Solid, (nint)rvb, (uint)((j - ci) * 6), 0, false, rc0.Clip, (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip, owned)));
				ci = j - 1;
			}
			else if (_pathAtlas && atlasScale is { } asc0 && TryAtlasBatch(cmds, ref ci, owned, asc0, out var aop0))
			{
				// Cached recordings are where STATIC text lives: its ops are built once here and replayed forever
				// after, so an atlas hook that only covers the live paths never sees a glyph.
				ops.Add(aop0);
			}
			else if (cmds[ci] is PathFill pf0 && !pf0.EvenOdd)
			{
				// Coalesce a run of consecutive NON-ZERO paths sharing colour + clip (a text run's glyphs) into one
				// stencil (all fans) + one cover over the union bbox — N glyphs collapse from 2N draws to 2. Safe for
				// non-zero winding: the union of same-colour shapes fills identically. Even-odd is excluded (an overlap
				// would XOR to a hole), and per-path clips (PathFan) never enter cached recordings (not arena-safe).
				_scratch.Clear();
				var bbMin = new Vector2(float.MaxValue); var bbMax = new Vector2(float.MinValue);
				// Measure the run BEFORE building anything: a run of one (the common case, since shapes rarely
				// share a colour) reuses the fan already cached on the command instead of re-interleaving it.
				int j = ci;
				while (j < cmds.Count && cmds[j] is PathFill pfj && !pfj.EvenOdd
					&& pfj.Color.R == pf0.Color.R && pfj.Color.G == pf0.Color.G && pfj.Color.B == pf0.Color.B && pfj.Color.A == pf0.Color.A
					&& ClipDataEquals(pfj.Clip, pf0.Clip))
				{
					bbMin = Vector2.Min(bbMin, pfj.BbMin); bbMax = Vector2.Max(bbMax, pfj.BbMax);
					j++;
				}

				var singleFan = j - ci == 1 ? pf0.SlottedFan(slotBits) : null;
				if (singleFan is null)
				{
					for (int k = ci; k < j; k++)
					{
						var pfk = (PathFill)cmds[k];
						for (int i = 0; i < pfk.FanDevice.Length; i += 2) { _scratch.Add(pfk.FanDevice[i]); _scratch.Add(pfk.FanDevice[i + 1]); _scratch.Add(slotBits); }
					}
				}
				// A LONE tiling fill skips stencil-then-cover entirely (see PathFill.FanTiles). A run of >1 stays
				// coalesced: for a glyph run, 2 draws total beats one draw per glyph, and glyph covers are small.
				if (j - ci == 1 && pf0.FanTiles)
				{
					float sr = pf0.Color.R / 255f, sg = pf0.Color.G / 255f, sb = pf0.Color.B / 255f, sa = pf0.Color.A / 255f;
					_scratch.Clear();
					var sCov = pf0.FanCoverage;
					for (int i = 0; i < pf0.FanDevice.Length; i += 2) { PushVertT(new Vector2(pf0.FanDevice[i], pf0.FanDevice[i + 1]), sr, sg, sb, sa * (sCov is null ? 1f : sCov[i >> 1]), slotBits); }
					var sClip = pf0.Clip;
					var sClipBg = MakeClipBg(_d.CoverClipBgl, sClip, owned);
					var sCount = (uint)(pf0.FanDevice.Length / 2);
					ops.Add(owned is null
						? new DrawOp(DrawKind.TilingFan, AppendPathBlock(_scratch), sCount, 0, true, sClip, (nint)sClipBg)
						: new DrawOp(DrawKind.TilingFan, (nint)Vbuf(_scratch, owned), sCount, 0, false, sClip, (nint)sClipBg));
					ci = j - 1;
					continue;
				}
				uint fanCount = (uint)((singleFan?.Length ?? _scratch.Count) / 3);
				var fanShared = owned is null
					? (singleFan is not null ? AppendPathBlock(singleFan) : AppendPathBlock(_scratch))
					: -1;
				var fanBuf = owned is null
					? IntPtr.Zero
					: (singleFan is not null ? Vbuf(singleFan, owned) : Vbuf(_scratch, owned));
				float pr = pf0.Color.R / 255f, pg = pf0.Color.G / 255f, pb = pf0.Color.B / 255f, pa = pf0.Color.A / 255f;
				_scratch.Clear();
				var tl = bbMin; var br = bbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(tr, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits);
				PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits); PushVertT(bl, pr, pg, pb, pa, slotBits);
				// TablePath: b0/b1 are BYTE offsets into the shared per-pass path buffer instead of private buffers.
				ops.Add(owned is null
					? new DrawOp(DrawKind.TablePath, fanShared, fanCount, AppendPathBlock(_scratch), false, pf0.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf0.Clip, owned))
					: new DrawOp(DrawKind.Path, (nint)fanBuf, fanCount, (nint)Vbuf(_scratch, owned), false, pf0.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf0.Clip, owned)));
				ci = j - 1;
			}
			else { BuildSimpleOp(cmds[ci], ops, owned, pathSlot, atlasScale); }
		}
	}

	// Builds the draw-op(s) for a simple primitive (rect/path/image/gradient) into `ops`, allocating GPU resources
	// pooled (owned == null, per-frame) or persistent (owned != null, a cached recording's geometry).
	private DrawOp ResidentizeFan(DrawOp op, OwnedResources owned)
	{
		if (owned is not null && op.clip.PathFan is { } fan && op.clip.FanBuf == 0)
		{
			_scratch.Clear();
			for (int j = 0; j < fan.Length; j += 2) { var n = Ndc(new Vector2(fan[j], fan[j + 1])); _scratch.Add(n.X); _scratch.Add(n.Y); }
			var c = op.clip; c.FanBuf = (nint)Vbuf(_scratch, owned); c.FanW = (int)_s.Width; c.FanH = (int)_s.Height;
			op.clip = c;
		}
		return op;
	}

	/// <summary>
	/// The device-space triangles a gradient covers: the corner-cut octagon when the clip is the inscribed
	/// ellipse, else the quad's two triangles. Decided once here because the two sinks below - the shared per-pass
	/// buffer and a recording's own array - would otherwise each repeat the choice.
	/// </summary>
	/// <returns>The number of points written to <paramref name="pts"/>.</returns>
	private static int GradientCover(GradientCmd gc, in ClipData clip, Span<Vector2> pts)
	{
		if (ClipIsInscribedEllipse(clip))
		{
			OctagonTris(gc.P0, gc.P1, gc.P2, gc.P3, pts);
			return OctSides * 3;
		}

		pts[0] = gc.P0; pts[1] = gc.P1; pts[2] = gc.P2;
		pts[3] = gc.P0; pts[4] = gc.P2; pts[5] = gc.P3;
		return 6;
	}

	private void BuildSimpleOp(WebGpuCommand cmd, List<DrawOp> ops, OwnedResources owned, int pathSlot, Vector2? atlasScale = null)
	{
		switch (cmd)
		{
			case RectCommand rc:
				{
					var c = new Vector4(rc.Color.R / 255f, rc.Color.G / 255f, rc.Color.B / 255f, rc.Color.A / 255f);
					var v = new List<float>();
					void V(Vector2 p) { var n = Ndc(p); v.Add(n.X); v.Add(n.Y); v.Add(c.X); v.Add(c.Y); v.Add(c.Z); v.Add(c.W); }
					V(rc.P0); V(rc.P1); V(rc.P2); V(rc.P0); V(rc.P2); V(rc.P3);
					var rClip = rc.Clip;
					ops.Add(new DrawOp(DrawKind.Solid, (nint)Vbuf(v.ToArray(), owned), 6, 0, false, rClip, (nint)MakeClipBg(_d.SolidClipBgl, rClip, owned)));
					break;
				}
			case PathFill pf:
				{
					// A small axis-aligned shape (a glyph) draws from the coverage atlas: one tinted quad, with
					// antialiasing baked in, instead of stencil-then-cover leaning on the multisampled attachment.
					if (atlasScale is { } asc1 && TryAtlasFill(pf, ops, owned, asc1)) { break; }
					float slotBits = System.BitConverter.Int32BitsToSingle(pathSlot);
					if (pf.FanTiles)
					{
						// The fan tiles the shape, so fill it in ONE pass: no stencil fan writing a multisampled
						// depth-stencil, and no cover quad over the whole bbox. Same pipeline as the cover, fed the
						// fan triangles directly. TilingFan + flag => b0 is a byte offset into the shared path buffer.
						float fr = pf.Color.R / 255f, fg = pf.Color.G / 255f, fb = pf.Color.B / 255f, fa = pf.Color.A / 255f;
						_scratch.Clear();
						var tCov = pf.FanCoverage;
						for (int i = 0; i < pf.FanDevice.Length; i += 2) { PushVertT(new Vector2(pf.FanDevice[i], pf.FanDevice[i + 1]), fr, fg, fb, fa * (tCov is null ? 1f : tCov[i >> 1]), slotBits); }
						var tClip = pf.Clip;
						var tClipBg = MakeClipBg(_d.CoverClipBgl, tClip, owned);
						var tCount = (uint)(pf.FanDevice.Length / 2);
						ops.Add(owned is null
							? new DrawOp(DrawKind.TilingFan, AppendPathBlock(_scratch), tCount, 0, true, tClip, (nint)tClipBg)
							: new DrawOp(DrawKind.TilingFan, (nint)Vbuf(_scratch, owned), tCount, 0, false, tClip, (nint)tClipBg));
						break;
					}
					var slotted = pf.SlottedFan(slotBits);
					var fanShared = owned is null ? AppendPathBlock(slotted) : -1;
					var fanBuf = owned is null ? IntPtr.Zero : Vbuf(slotted, owned);
					float pr = pf.Color.R / 255f, pg = pf.Color.G / 255f, pb = pf.Color.B / 255f, pa = pf.Color.A / 255f;
					_scratch.Clear();
					var tl = pf.BbMin; var br = pf.BbMax; var tr = new Vector2(br.X, tl.Y); var bl = new Vector2(tl.X, br.Y);
					PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(tr, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits);
					PushVertT(tl, pr, pg, pb, pa, slotBits); PushVertT(br, pr, pg, pb, pa, slotBits); PushVertT(bl, pr, pg, pb, pa, slotBits);
					var pClip = pf.Clip;
					var clipBg = MakeClipBg(_d.CoverClipBgl, pClip, owned);
					ops.Add(owned is null
						? new DrawOp(DrawKind.TablePath, fanShared, (uint)(pf.FanDevice.Length / 2), AppendPathBlock(_scratch), pf.EvenOdd, pClip, (nint)clipBg)
						: new DrawOp(DrawKind.Path, (nint)fanBuf, (uint)(pf.FanDevice.Length / 2), (nint)Vbuf(_scratch, owned), pf.EvenOdd, pClip, (nint)clipBg));
					break;
				}
			case ImageCmd im:
				{
					var view = im.View;
					var ubuf = Ubuf(112, owned);
					var op = stackalloc float[28];
					bool hasMatrix = im.ColorMatrix is { Length: >= 20 };
					op[0] = im.Opacity; op[1] = im.TintMode; op[2] = hasMatrix ? 1f : 0f; op[3] = 0;
					op[4] = im.Tint.X; op[5] = im.Tint.Y; op[6] = im.Tint.Z; op[7] = im.Tint.W;
					if (im.ColorMatrix is { Length: >= 20 } mm)
					{
						op[8] = mm[0]; op[9] = mm[1]; op[10] = mm[2]; op[11] = mm[3];        // m0
						op[12] = mm[5]; op[13] = mm[6]; op[14] = mm[7]; op[15] = mm[8];      // m1
						op[16] = mm[10]; op[17] = mm[11]; op[18] = mm[12]; op[19] = mm[13];  // m2
						op[20] = mm[15]; op[21] = mm[16]; op[22] = mm[17]; op[23] = mm[18];  // m3
						op[24] = mm[4]; op[25] = mm[9]; op[26] = mm[14]; op[27] = mm[19];    // off (5th column)
					}
					wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)op, 112);
					var entries = stackalloc WGPUBindGroupEntry[3];
					entries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = view };
					entries[1] = new WGPUBindGroupEntry { Binding = 1, Sampler = _d.TiledSampler(im.ExtendX, im.ExtendY) };
					entries[2] = new WGPUBindGroupEntry { Binding = 2, Buffer = ubuf, Offset = 0, Size = WebGpuDevice.ImageUniformBytes };
					var bgd = new WGPUBindGroupDescriptor { Layout = _d.ImgBgl, EntryCount = 3, Entries = entries };
					var bg = Bg(ref bgd, owned);
					if (owned is null)
					{
						// flag == true: b1 is a BYTE offset into the shared per-pass quad buffer (see gradients).
						var ioff = _quadVerts.Count * sizeof(float);
						void QS(Vector2 pos, float u, float vv) { var n = Ndc(pos); _quadVerts.Add(n.X); _quadVerts.Add(n.Y); _quadVerts.Add(u); _quadVerts.Add(vv); }
						QS(im.P0, im.U0, im.V0); QS(im.P1, im.U1, im.V0); QS(im.P2, im.U1, im.V1); QS(im.P0, im.U0, im.V0); QS(im.P2, im.U1, im.V1); QS(im.P3, im.U0, im.V1);
						ops.Add(new DrawOp(DrawKind.Image, (nint)bg, 0, ioff, true, im.Clip, (nint)MakeClipBg(_d.ImageClipBgl, im.Clip, owned)));
					}
					else
					{
						var q = new float[24];
						void QV(int idx, Vector2 pos, float u, float vv) { var n = Ndc(pos); q[idx] = n.X; q[idx + 1] = n.Y; q[idx + 2] = u; q[idx + 3] = vv; }
						QV(0, im.P0, im.U0, im.V0); QV(4, im.P1, im.U1, im.V0); QV(8, im.P2, im.U1, im.V1); QV(12, im.P0, im.U0, im.V0); QV(16, im.P2, im.U1, im.V1); QV(20, im.P3, im.U0, im.V1);
						ops.Add(new DrawOp(DrawKind.Image, (nint)bg, 0, (nint)Vbuf(q, owned), false, im.Clip, (nint)MakeClipBg(_d.ImageClipBgl, im.Clip, owned)));
					}
					break;
				}
			case GradientCmd gc:
				{
					var bytes = (nuint)WebGpuDevice.GradientUniformBytes;
					IntPtr gbg;
					{
						if (owned is null)
						{
							// One slab slot instead of a buffer + queue write per gradient per frame: the whole
							// frame's gradient uniforms upload in one write per chunk before the submit.
							gbg = _d.GradSlab.Rent(_d.GradBgl, gc.Uniform);
						}
						else
						{
							var ubuf = Ubuf((int)bytes, owned);
							fixed (float* p = gc.Uniform) { wgpuQueueWriteBuffer(_d.Q, ubuf, 0, (IntPtr)p, bytes); }
							var gentry = new WGPUBindGroupEntry { Binding = 0, Buffer = ubuf, Offset = 0, Size = bytes };
							var gbgd = new WGPUBindGroupDescriptor { Layout = _d.GradBgl, EntryCount = 1, Entries = &gentry };
							gbg = Bg(ref gbgd, owned);
						}
					}
					var gClip = gc.Clip;
					Span<Vector2> cover = stackalloc Vector2[OctSides * 3];
					var gCount = (uint)GradientCover(gc, gClip, cover);
					var gClipBg = (nint)MakeClipBg(_d.GradClipBgl, gClip, owned);
					if (owned is null)
					{
						// flag == true: b1 is a BYTE offset into the shared per-pass gradient buffer.
						var goff = _gradVerts.Count * sizeof(float);
						for (var t = 0; t < gCount; t++) { var n = Ndc(cover[t]); _gradVerts.Add(n.X); _gradVerts.Add(n.Y); }
						ops.Add(new DrawOp(DrawKind.Gradient, (nint)gbg, gCount, goff, true, gClip, gClipBg));
					}
					else
					{
						var gq = new float[gCount * 2];
						for (var t = 0; t < gCount; t++) { var n = Ndc(cover[t]); gq[t * 2] = n.X; gq[t * 2 + 1] = n.Y; }
						ops.Add(new DrawOp(DrawKind.Gradient, (nint)gbg, gCount, (nint)Vbuf(gq, owned), false, gClip, gClipBg));
					}
					break;
				}
			case RoundedRectCmd rrc:
				{
					// Legacy per-op fallback (b0=1). The common path routes rrects through the shared per-pass buffer
					// (PassBuffer) for cross-visual coalescing; this stays for any non-frame-solid cached recording.
					var tmp = RentRrect();
					AppendRrect(tmp, rrc);
					var buf = Vbuf(tmp, owned);
					ReturnRrect(tmp);
					ops.Add(new DrawOp(DrawKind.RoundedRect, (nint)buf, 6, 0, false, rrc.Clip, (nint)MakeClipBg(_d.RrClipBgl, rrc.Clip, owned)));
					break;
				}
		}
	}

	// Applies the in-pass path-clip transition to the shared depth buffer (all draws recorded into the open `pass`):
	// restore the previous clip's region to depth=0, then write the new clip's mask (depth=0 kept / else clipped)
	// via stencil-then-cover over its bbox. Content depth-tests GreaterEqual against it. No offscreen, no resolve.
	private void ApplyDepthClip(IntPtr pass, float[] prevFan, Vector4 prevAabb, ClipData next)
	{
		// Restore the previous path clip's region to depth=0 (no clip) so its mask doesn't leak past its bbox.
		if (prevFan is not null && TryScissor(prevAabb, out var px, out var py, out var pw, out var ph))
		{
			wgpuRenderPassEncoderSetScissorRect(pass, (uint)px, (uint)py, (uint)pw, (uint)ph);
			wgpuRenderPassEncoderSetPipeline(pass, _d.ClipDepthSet0);
			wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		}
		if (next.PathFan is not { } fan || !TryScissor(next.Aabb, out var nx, out var ny, out var nw, out var nh))
		{
			return;
		}
		wgpuRenderPassEncoderSetScissorRect(pass, (uint)nx, (uint)ny, (uint)nw, (uint)nh);
		var excl = next.PathExclude;
		// An exclude clip inverts every polarity below: "clipped" is 1 for intersect and 0 for exclude.
		wgpuRenderPassEncoderSetPipeline(pass, excl ? _d.ClipDepthSet0 : _d.ClipDepthSet1);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
		IntPtr fanBuf; int fanVerts;
		if (next.FanBuf != 0 && next.FanW == (int)_s.Width && next.FanH == (int)_s.Height) { fanBuf = (IntPtr)next.FanBuf; fanVerts = fan.Length / 2; }
		else { _scratch.Clear(); for (int i = 0; i < fan.Length; i += 2) { var n = Ndc(new Vector2(fan[i], fan[i + 1])); _scratch.Add(n.X); _scratch.Add(n.Y); } fanBuf = MakeBuffer(_scratch); fanVerts = _scratch.Count / 2; }
		wgpuRenderPassEncoderSetPipeline(pass, next.PathEvenOdd ? _d.StencilEvenOdd : _d.StencilNonZero);
		wgpuRenderPassEncoderSetBindGroup(pass, 0, next.FanXformBg != 0 ? (IntPtr)next.FanXformBg : MakeClipBg(_d.ClipBgl, default), 0, (uint*)null);   // arena xform, else identity (fan already device NDC)
		wgpuRenderPassEncoderSetVertexBuffer(pass, 0, fanBuf, 0, (nuint)(fanVerts * 2 * sizeof(float)));
		wgpuRenderPassEncoderDraw(pass, (uint)fanVerts, 1, 0, 0);
		// Cover also resets the stencil (PassOp=Zero) so the next clip starts clean.
		wgpuRenderPassEncoderSetPipeline(pass, excl ? _d.ClipDepthCover1 : _d.ClipDepthCover0);
		wgpuRenderPassEncoderSetStencilReference(pass, 0);
		wgpuRenderPassEncoderDraw(pass, 3, 1, 0, 0);
	}

	// Gap-4 solid-scroll eligibility: a frame-solid recording whose SESSION clip is fan-free and whose commands
	// are only solids/rrects/path-fills with no path CHILD-clip. Images/gradients (no transform-table pipe) and path
	// child-clips (a depth mask, not restampable) keep the device rebuild path. Rounded session clips are eligible:
	// the stamp folds them into ClipU by mapping them into the recording's local space (see StampTableClip) — list
	// items scrolling inside a rounded container would otherwise full-rebuild every frame.
	private static bool TableFrameEligible(ReplayRefCmd rr)
	{
		// The session clip is per-replay; the command scan is not, so only the latter is memoized.
		if (rr.Clip.PathFan is not null) { return false; }
		if (rr.Data is { } d) { return d.TableEligibleMemo ??= TableEligibleScan(rr.Commands); }
		return TableEligibleScan(rr.Commands);
	}

	private static bool TableEligibleScan(List<WebGpuCommand> cmds)
	{
		for (int i = 0; i < cmds.Count; i++)
		{
			var c = cmds[i];
			if (c is not (RectCommand or RoundedRectCmd or PathFill) || c.Clip.PathFan is not null) { return false; }
		}
		return true;
	}

	// A widened (full-surface) scissor is sound when the op's rect constraint is enforced analytically:
	// proven non-clipping (ScissorInert), riding the ClipU rect slot (AabbInClipU), or derivable — every
	// non-stamp op's ClipU is built from its own ClipData, whose fan-free AABB always folds in. Widening
	// lets consecutive ops dedupe to a single SetScissorRect.
	private static bool ScissorWidenable(in ClipData clip)
		=> clip.ScissorInert || clip.AabbInClipU || (!clip.ScissorLoadBearing && clip.PathFan is null);

	private static bool IsFiniteAabb(Vector4 aabb)
		=> aabb.X > -1e8f || aabb.Y > -1e8f || aabb.Z < 1e8f || aabb.W < 1e8f;

	// Intersects a DEVICE-space session AABB into a LOCAL-space clip AABB through finv, so the folded
	// radius-0 ClipU rect (see AabbInClipU) also carries the session's plain-rect clip and the scissor can
	// widen. Only exact when finv is axis-aligned — callers must not widen (or fold) otherwise.
	private static void FoldSessionAabb(ref ClipData local, Vector4 sessionAabb, in Matrix3x2 finv)
	{
		var q0 = new Vector2(sessionAabb.X * finv.M11 + sessionAabb.Y * finv.M21 + finv.M31, sessionAabb.X * finv.M12 + sessionAabb.Y * finv.M22 + finv.M32);
		var q1 = new Vector2(sessionAabb.Z * finv.M11 + sessionAabb.W * finv.M21 + finv.M31, sessionAabb.Z * finv.M12 + sessionAabb.W * finv.M22 + finv.M32);
		local.Aabb = new Vector4(
			MathF.Max(local.Aabb.X, MathF.Min(q0.X, q1.X)), MathF.Max(local.Aabb.Y, MathF.Min(q0.Y, q1.Y)),
			MathF.Min(local.Aabb.Z, MathF.Max(q0.X, q1.X)), MathF.Min(local.Aabb.W, MathF.Max(q0.Y, q1.Y)));
	}

	// Folds device-space session rounds into a LOCAL-space clip: ClipU carries a single finv (fragment ->
	// recording-local), so the rounds are mapped through that same finv (exact for axis-aligned transforms).
	private static void FoldSessionRounds(ref ClipData local, RoundClip[] sessionRounds, in Matrix3x2 finv)
	{
		if (sessionRounds is not { Length: > 0 })
		{
			return;
		}
		var fsx = new Vector2(finv.M11, finv.M12).Length();
		var fsy = new Vector2(finv.M21, finv.M22).Length();
		foreach (var src in sessionRounds)
		{
			var q0 = new Vector2(src.Rect.X * finv.M11 + src.Rect.Y * finv.M21 + finv.M31, src.Rect.X * finv.M12 + src.Rect.Y * finv.M22 + finv.M32);
			var q1 = new Vector2(src.Rect.Z * finv.M11 + src.Rect.W * finv.M21 + finv.M31, src.Rect.Z * finv.M12 + src.Rect.W * finv.M22 + finv.M32);
			local.Rounds = ClipData.Push(local.Rounds, new RoundClip
			{
				Rect = new Vector4(MathF.Min(q0.X, q1.X), MathF.Min(q0.Y, q1.Y), MathF.Max(q0.X, q1.X), MathF.Max(q0.Y, q1.Y)),
				Radii = src.Radii * fsx,
				RadiiY = src.RadiiY * fsy,
				Exclude = src.Exclude,
			});
		}
	}

	/// <summary>
	/// ClipU layout for the pipeline that draws <paramref name="kind"/>. The image/gradient/rrect pipelines are
	/// created with AUTO layouts, so their group is exclusive to them and cannot take a ClipBgl-based bind group.
	/// </summary>
	private IntPtr ClipBglForKind(DrawKind kind) => kind switch
	{
		// Image now shares ClipBgl (explicit pipeline layout). Gradient and rrect are still AUTO-layout
		// pipelines, so their ClipU group stays exclusive to them.
		DrawKind.Gradient => _d.GradClipBgl,
		_ => _d.ClipBgl,
	};

	/// <summary>
	/// True when an op of this kind is placed by the xform TABLE (its verts carry a slot index), so its clip must
	/// NOT also carry the replay transform. Everything else in a table recording is identity-baked with no slot,
	/// and the clip's xform is the only thing that can move it.
	/// </summary>
	private static bool PlacedByXformTable(DrawKind kind) => kind is DrawKind.Solid or DrawKind.RoundedRect or DrawKind.TablePath;

	private (ClipData Scissor, nint ClipBg, nint Buf) StampTableClip(ClipData local, OwnedResources stampOwned, Matrix3x2 finv, Matrix3x2 t2, Vector4 sessionAabb, bool sessionInert, RoundClip[] sessionRounds, nint reuseBuf, nint reuseBg, IntPtr clipBgl, Matrix3x2 opXform)
	{
		var scissor = local;
		scissor.PathFan = null;
		// The recorded containment proof doesn't cover the replay-site clip being stamped in below.
		scissor.ScissorInert = local.ScissorInert && sessionInert;
		FoldSessionRounds(ref local, sessionRounds, finv);
		var ab = local.Aabb;
		if (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f)
		{
			var p0 = new Vector2(ab.X * t2.M11 + ab.Y * t2.M21 + t2.M31, ab.X * t2.M12 + ab.Y * t2.M22 + t2.M32);
			var p1 = new Vector2(ab.Z * t2.M11 + ab.Y * t2.M21 + t2.M31, ab.Z * t2.M12 + ab.Y * t2.M22 + t2.M32);
			var p2 = new Vector2(ab.Z * t2.M11 + ab.W * t2.M21 + t2.M31, ab.Z * t2.M12 + ab.W * t2.M22 + t2.M32);
			var p3 = new Vector2(ab.X * t2.M11 + ab.W * t2.M21 + t2.M31, ab.X * t2.M12 + ab.W * t2.M22 + t2.M32);
			ab = new Vector4(
				MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
				MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));
		}
		scissor.Aabb = new Vector4(MathF.Max(ab.X, sessionAabb.X), MathF.Max(ab.Y, sessionAabb.Y), MathF.Min(ab.Z, sessionAabb.Z), MathF.Min(ab.W, sessionAabb.W));
		// Widening the scissor is only sound when the whole rect clip rides ClipU: the op's own AABB always
		// does; a finite session AABB folds in exactly only under an axis-aligned transform.
		var sessionFinite = IsFiniteAabb(sessionAabb);
		var axisAligned = finv.M12 == 0 && finv.M21 == 0;
		var canWiden = !sessionFinite || axisAligned;
		if (sessionFinite && axisAligned)
		{
			FoldSessionAabb(ref local, sessionAabb, finv);
		}
		if (reuseBuf != 0)
		{
			scissor.AabbInClipU = RewriteClipU(reuseBuf, local, opXform, finv) && canWiden;
			scissor.ScissorLoadBearing = !scissor.AabbInClipU;
			return (scissor, reuseBg, reuseBuf);
		}
		var bg = (nint)MakeClipBgOwned(clipBgl, local, stampOwned, opXform, finv, out var buf, out var folded);
		scissor.AabbInClipU = folded && canWiden;
		scissor.ScissorLoadBearing = !scissor.AabbInClipU;
		return (scissor, bg, buf);
	}
}
