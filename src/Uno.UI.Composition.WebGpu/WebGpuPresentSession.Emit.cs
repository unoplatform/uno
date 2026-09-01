// The replay strategies. Each one emits the ops for a cached recording; which is chosen depends on what the
// recording contains and on what a move would cost, from re-appendable solids to identity-baked arena geometry.
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
	// Transform-table frame-solid emit (gap-4 solid-scroll redo). Builds the recording's solids/rrects/path-fills ONCE
	// in identity (local) device space + one shared per-vertex slot, resident in the shared TABLE slabs; each frame a
	// MOVE rewrites only that slot (WriteXform) and re-stamps the per-op clips — no re-tessellation, no vertex re-Put.
	// The absolute slab offset is re-derived every frame (TryByteOffset, else re-Put) so a recording that scrolled out
	// (its slice culled + reclaimed) then back in never draws from a stale offset — the reverted attempt's crash.
	private void EmitTableFrameSolid(ReplayRefCmd rr, WebGpuGeometryCache feCur, List<DrawOp> ops)
	{
		var fe = feCur;
		bool hit = fe is { TableFrame: true, FrameOrder: not null }
			// An atlas quad carries build-time NDC and no table slot, so unlike the rest of this entry it is not
			// re-projected by the per-frame slot rewrite: it has to be rebuilt when the surface resizes, and its
			// mask is only the right size while the replay transform still neither scales nor rotates.
			&& !(fe.HasAtlas && (fe.BuiltW != (int)_s.Width || fe.BuiltH != (int)_s.Height
				|| !(TryAtlasScale(rr.Transform, out var feScale) && SameAtlasScale(feScale, fe.AtlasScale))))
			// ...and rebuild once the transform settles, so content first built mid-animation stops being aliased.
			&& !(fe.AtlasBlockedByScale && TryAtlasScale(rr.Transform, out _));
		if (!hit)
		{
			if (_emitStats) { _statTableRebuilds++; }
			if (fe is not null) { _d.DeferRelease(fe.Owned); _d.DeferRelease(fe.StampOwned); }
			var fOwned = new OwnedResources();
			var sv = new List<float>(); var rv = new List<float>(); var order = new List<FrameOp>();
			var tmp = new List<DrawOp>();
			var tcmds = WebGpuCommandRecorder.TransformFor(rr.Commands, Matrix4x4.Identity, ClipData.None);
			int tableAtlasBefore = AtlasHit + AtlasBaked;
			bool tableAtlasSafe = TryAtlasScale(rr.Transform, out var tableScale);
			bool tableHasPath = false; for (int _i = 0; _i < tcmds.Count; _i++) { if (tcmds[_i] is PathFill) { tableHasPath = true; break; } }
			// One stable slot shared by ALL of this recording's geometry (solids/rrects/path-fills): its local->NDC
			// affine folds the replay transform + projection, rewritten per frame, so a move repositions everything.
			int slot = (fe is not null && fe.XformSlot >= 0) ? fe.XformSlot : _d.AllocXformSlot();
			float slotBits = System.BitConverter.Int32BitsToSingle(slot);
			for (int ti = 0; ti < tcmds.Count; ti++)
			{
				var tc = tcmds[ti];
				if (tc is RectCommand rc0)
				{
					// Coalesce a run of consecutive same-clip rects into one contiguous LOCAL-vert range + one draw.
					int rel = sv.Count / 7; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
					{
						AppendSolidRectLocalT(sv, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f, slotBits);
						tj++;
					}
					order.Add(new FrameOp { Kind = DrawKind.Solid, ByteOff = rel * 7 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rc0.Clip });
					ti = tj - 1;
				}
				else if (tc is RoundedRectCmd rr0)
				{
					int rel = rv.Count / 23; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RoundedRectCmd rrj && ClipDataEquals(rrj.Clip, rr0.Clip))
					{
						AppendRrectLocalT(rv, rrj, slotBits);
						tj++;
					}
					order.Add(new FrameOp { Kind = DrawKind.RoundedRect, ByteOff = rel * 23 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rr0.Clip });
					ti = tj - 1;
				}
				else
				{
					// Path fills (glyphs/icons): local device fan/cover + the shared slot, residentized so the fan/cover
					// buffers upload once. The move repositions them via the slot; clipCov uses the per-frame finv stamp.
					//
					// The atlas first: coalesced stencil-then-cover has NO antialiasing at a single sample, so a glyph
					// that reaches the collapse below instead of a mask renders aliased.
					// A RUN of atlas quads collapses to one draw. Without this each glyph emitted its own FrameOp,
					// so per-glyph geometry cost a draw per character (ops 420 -> 4530 on a 1800-row log view).
					if (_pathAtlas && tableAtlasSafe && TryAtlasBatch(tcmds, ref ti, fOwned, tableScale, out var aop))
					{
						order.Add(new FrameOp { Kind = null, NonSolid = aop });
						continue;
					}
					// A run of consecutive NON-ZERO paths sharing colour + clip (a text run's glyphs) collapses to ONE
					// stencil + ONE cover, the same coalescing BuildCoalesced does for arena recordings. Without it every
					// recording that also contains rects — i.e. every real list row, grid cell or card, since they all
					// have a background — paid 2 draws and 2 pipeline switches per GLYPH.
					if (tc is PathFill pf0 && !pf0.EvenOdd && !pf0.FanTiles)
					{
						_scratch.Clear();
						var gMin = new Vector2(float.MaxValue); var gMax = new Vector2(float.MinValue);
						int gj = ti;
						while (gj < tcmds.Count && tcmds[gj] is PathFill pfj && !pfj.EvenOdd
							&& pfj.Color.R == pf0.Color.R && pfj.Color.G == pf0.Color.G && pfj.Color.B == pf0.Color.B && pfj.Color.A == pf0.Color.A
							&& ClipDataEquals(pfj.Clip, pf0.Clip))
						{
							for (int gi = 0; gi < pfj.FanDevice.Length; gi += 2) { _scratch.Add(pfj.FanDevice[gi]); _scratch.Add(pfj.FanDevice[gi + 1]); _scratch.Add(slotBits); }
							gMin = Vector2.Min(gMin, pfj.BbMin); gMax = Vector2.Max(gMax, pfj.BbMax);
							gj++;
						}
						var gFan = Vbuf(_scratch, fOwned);
						uint gCount = (uint)(_scratch.Count / 3);
						float gr = pf0.Color.R / 255f, gg = pf0.Color.G / 255f, gb = pf0.Color.B / 255f, ga = pf0.Color.A / 255f;
						_scratch.Clear();
						var gTl = gMin; var gBr = gMax; var gTr = new Vector2(gBr.X, gTl.Y); var gBl = new Vector2(gTl.X, gBr.Y);
						PushVertT(gTl, gr, gg, gb, ga, slotBits); PushVertT(gTr, gr, gg, gb, ga, slotBits); PushVertT(gBr, gr, gg, gb, ga, slotBits);
						PushVertT(gTl, gr, gg, gb, ga, slotBits); PushVertT(gBr, gr, gg, gb, ga, slotBits); PushVertT(gBl, gr, gg, gb, ga, slotBits);
						var gCov = Vbuf(_scratch, fOwned);
						var gOp = new DrawOp(DrawKind.Path, (nint)gFan, gCount, (nint)gCov, false, pf0.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf0.Clip, fOwned));
						order.Add(new FrameOp { Kind = null, NonSolid = ResidentizeFan(gOp, fOwned) });
						ti = gj - 1;
						continue;
					}
					tmp.Clear();
					BuildSimpleOp(tc, tmp, fOwned, slot);
					foreach (var o in tmp) { order.Add(new FrameOp { Kind = null, NonSolid = ResidentizeFan(o, fOwned) }); }
				}
			}
			long id = (fe is not null && fe.SlabId != 0) ? fe.SlabId : _d.NextSlabId();
			bool tableHasAtlas = (AtlasHit + AtlasBaked) != tableAtlasBefore;
			bool tableBlocked = !tableAtlasSafe && tableHasPath && _pathAtlas;
			fe = new WebGpuGeometryCache { TableFrame = true, FrameSolid = true, SlabId = id, FrameOrder = order, TableSolids = sv, TableRrects = rv, Owned = fOwned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = slot, HasAtlas = tableHasAtlas, AtlasBlockedByScale = tableBlocked, AtlasScale = tableScale };
			StoreCompiled(rr.Data, fe);
		}
		// Re-derive the CURRENT slab byte offset of this recording's slices every frame: reuse the resident slice when it
		// survived last frame (no upload), else re-Put its UNCHANGED local verts (the slice was culled + its offset
		// reclaimed). NEVER a cached absolute offset: a stale offset into a reclaimed slice reads another visual's verts.
		int sBase = 0, rBase = 0;
		if (fe.TableSolids.Count > 0 && !_d.SolidTableSlab.TryByteOffset(fe.SlabId, out sBase)) { sBase = _d.SolidTableSlab.Put(fe.SlabId, fe.TableSolids); }
		if (fe.TableRrects.Count > 0 && !_d.RrectTableSlab.TryByteOffset(fe.SlabId, out rBase)) { rBase = _d.RrectTableSlab.Put(fe.SlabId, fe.TableRrects); }
		// The single slot's affine = replay transform folded with the current projection: a move/resize is this one write.
		WriteXform(fe.XformSlot, rr.Transform);
		// Re-stamp per-op device scissors + clip bind groups only when transform / session clip / surface size changed
		// (memoized like the arena stamp); a STATIC table recording reuses them verbatim. The slab base is applied below.
		if (!fe.HasStamp || fe.StampXform != rr.Transform || !ClipDataEquals(fe.StampClip, rr.Clip) || fe.StampW != (int)_s.Width || fe.StampH != (int)_s.Height)
		{
			if (_emitStats) { _statStamps++; }
			// In-place restamp: rewrite the previous stamp's ClipU buffers and keep its bind groups. Unsafe only when
			// this entry was already stamped under the current submit (same device frame) - the rewrite would clobber
			// uniforms this frame's earlier draws still read - so that case (and the first stamp) allocates fresh.
			var reuse = fe.HasStamp && fe.StampBufs is not null && fe.StampBufs.Count == fe.FrameOrder.Count && fe.StampFrame != _d.FrameSeq;
			if (!reuse && fe.StampOwned is not null) { _d.DeferRelease(fe.StampOwned); }
			var stampOwned = reuse ? fe.StampOwned : new OwnedResources();
			var t2 = new Matrix3x2(rr.Transform.M11, rr.Transform.M12, rr.Transform.M21, rr.Transform.M22, rr.Transform.M41, rr.Transform.M42);
			Matrix3x2 finv = Matrix3x2.Invert(t2, out var inv) ? inv : Matrix3x2.Identity;
			var sessionAabb = rr.Clip.Aabb;
			var stamps = reuse ? fe.StampClips : new List<(ClipData Scissor, nint ClipBg)>(fe.FrameOrder.Count);
			var bufs = reuse ? fe.StampBufs : new List<nint>(fe.FrameOrder.Count);
			for (int i = 0; i < fe.FrameOrder.Count; i++)
			{
				var fo = fe.FrameOrder[i];
				var local = fo.Kind is null ? fo.NonSolid.clip : fo.Clip;
				// An atlas quad in a stamped recording is a kind-2 IMAGE draw: it needs the image pipeline's own
				// ClipU layout, not the shared one, or the draw is rejected and the process aborts.
				var opKind = fo.Kind ?? fo.NonSolid.kind;
				var stampBgl = ClipBglForKind(opKind);
				// An op with no xform-table slot (an atlas quad, an image, a gradient) is still identity-baked, so
				// its clip has to carry the replay transform or it draws at the recording's local origin.
				var stampXform = PlacedByXformTable(opKind) ? Matrix3x2.Identity : ArenaXform(rr.Transform);
				var st = StampTableClip(local, stampOwned, finv, t2, sessionAabb, rr.Clip.ScissorInert, rr.Clip.Rounds, reuse ? bufs[i] : 0, reuse ? stamps[i].ClipBg : 0, stampBgl, stampXform);
				if (reuse) { stamps[i] = (st.Scissor, st.ClipBg); } else { stamps.Add((st.Scissor, st.ClipBg)); bufs.Add(st.Buf); }
			}
			fe.StampOwned = stampOwned; fe.StampClips = stamps; fe.StampBufs = bufs; fe.StampFrame = _d.FrameSeq; fe.StampXform = rr.Transform; fe.StampClip = rr.Clip; fe.StampW = (int)_s.Width; fe.StampH = (int)_s.Height; fe.HasStamp = true;
		}
		// Emit from the resident slabs (b0=2 => table slab) with the per-frame base + the memoized stamped clip.
		for (int i = 0; i < fe.FrameOrder.Count; i++)
		{
			var fo = fe.FrameOrder[i]; var (sc, bg) = fe.StampClips[i];
			if (fo.Kind == DrawKind.Solid) { ops.Add(new DrawOp(DrawKind.Solid, VertexSource.TableSlab, fo.Count, (nint)(sBase + fo.ByteOff), false, sc, bg)); }
			else if (fo.Kind == DrawKind.RoundedRect) { ops.Add(new DrawOp(DrawKind.RoundedRect, VertexSource.TableSlab, fo.Count, (nint)(rBase + fo.ByteOff), false, sc, bg)); }
			else { var op = fo.NonSolid; ops.Add(new DrawOp(op.kind, op.b0, op.u0, op.b1, op.flag, sc, bg)); }
		}
	}

	// Renders a command list into a target surface's MSAA pass (resolving to its single-sample view). Layers
	// recurse into their own full-size surface then composite here; shadows/layers pre-render before the pass.
	private long _renderIntoStart;

	/// <summary>
	/// Emits one nested recording's draw ops. Three strategies, picked by what the recording holds and whether its
	/// transform moved: a TABLE FRAME (solids re-emitted into the shared per-pass buffers, every vertex placed by
	/// the xform table), an ARENA entry (geometry baked once in identity space and re-stamped on a move), or a
	/// plain CACHED entry (rebuilt whenever its transform changes).
	/// </summary>
	/// <summary>
	/// Replays a recording whose solids can be re-appended to the shared per-pass buffers, so a Border's
	/// background and edges coalesce with its siblings' instead of costing a draw each. Falls through to the
	/// device path when the same list is emitted twice in one frame, since one resident slice cannot serve both.
	/// </summary>
	private void EmitReappendableReplay(ReplayRefCmd rr, List<DrawOp> ops, HashSet<List<WebGpuCommand>> frameEmitted)
	{
		// A recording replayed MORE THAN ONCE in a single frame (same command list, different
		// transforms) can't reuse one resident slab slice — the second build's Put would overwrite
		// the first. Repeat emissions get a fresh transient slice (freed next frame); the first
		// emission keeps the recording's stable, resident slice.
		bool repeat = !frameEmitted.Add(rr.Commands);
		// Gap-4 solid-scroll: an eligible recording (plain-AABB/None session clip, only solids/rrects/
		// path-fills, no path child-clip, no images/gradients) restamps via the transform table on a MOVE
		// — identity verts resident in the shared table slabs + a per-vertex slot, so a scroll rewrites
		// the slot, not the verts, while siblings still coalesce. A repeat emission (same list twice this
		// frame) can't share one resident slice, so it takes the device path below.
		if (!repeat && TableFrameEligible(rr))
		{
			EmitTableFrameSolid(rr, rr.Data.Compiled, ops);
			return;
		}
		WebGpuGeometryCache fe = null;
		// Re-derived every frame (build => Put, reuse => TryByteOffset-else-Put); FrameOrder is relative.
		int sBase = 0, rBase = 0;
		bool fMiss, fStale;
		if (repeat) { fMiss = true; fStale = false; }
		else
		{
			fe = rr.Data.Compiled;
			fMiss = fe is null;
			fStale = !fMiss && (!fe.FrameSolid || fe.TableFrame || fe.FrameOrder is null || fe.Transform != rr.Transform || fe.BuiltW != (int)_s.Width || fe.BuiltH != (int)_s.Height || !ClipDataEquals(fe.Clip, rr.Clip));
		}
		if (fMiss || fStale)
		{
			// Build once: extract device-space solid/rrect verts + an ordered emit list; owned (persistent) clip
			// bind groups so nothing is re-created per frame.
			if (fe is not null) { _d.DeferRelease(fe.Owned); }
			var fOwned = new OwnedResources();
			var sv = new List<float>(); var rv = new List<float>(); var order = new List<FrameOp>();
			var tmp = new List<DrawOp>();
			var tcmds = new List<WebGpuCommand>();
			foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, rr.Transform, rr.Clip)) { tcmds.Add(tc); }
			// One stable transform-table slot for this recording's device-space path fills (reused on
			// rebuild; transient for a repeat emission). Verts are final-device here, so the slot's entry
			// is the pure device->NDC projection, rewritten per frame at emit.
			bool fHasPath = false; foreach (var c in tcmds) { if (c is PathFill) { fHasPath = true; break; } }
			int fSlot = fHasPath ? ((!repeat && fe is not null && fe.XformSlot >= 0) ? fe.XformSlot : _d.AllocXformSlot()) : -1;
			if (fHasPath && repeat) { _xformTransient.Add(fSlot); }
			for (int ti = 0; ti < tcmds.Count; ti++)
			{
				var tc = tcmds[ti];
				if (tc is RectCommand rc0)
				{
					// Coalesce a run of consecutive same-clip rects into one contiguous range + one draw.
					int rel = sv.Count / 6; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RectCommand rcj && ClipDataEquals(rcj.Clip, rc0.Clip))
					{
						AppendSolidRect(sv, rcj.P0, rcj.P1, rcj.P2, rcj.P3, rcj.Color.R / 255f, rcj.Color.G / 255f, rcj.Color.B / 255f, rcj.Color.A / 255f);
						tj++;
					}
					order.Add(new FrameOp { Kind = DrawKind.Solid, ByteOff = rel * 6 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rc0.Clip, ClipBg = (nint)MakeClipBg(_d.SolidClipBgl, rc0.Clip, fOwned) });
					ti = tj - 1;
				}
				else if (tc is RoundedRectCmd rr0)
				{
					int rel = rv.Count / 22; int tj = ti;
					while (tj < tcmds.Count && tcmds[tj] is RoundedRectCmd rrj && ClipDataEquals(rrj.Clip, rr0.Clip))
					{
						AppendRrect(rv, rrj);
						tj++;
					}
					order.Add(new FrameOp { Kind = DrawKind.RoundedRect, ByteOff = rel * 22 * sizeof(float), Count = (uint)((tj - ti) * 6), Clip = rr0.Clip, ClipBg = (nint)MakeClipBg(_d.RrClipBgl, rr0.Clip, fOwned) });
					ti = tj - 1;
				}
				else
				{
					// Atlas first, for the same reason as the table path: the collapse below antialiases
					// nothing at one sample. Safe unconditionally here — these commands are already in
					// final device space, and fStale rebuilds this entry on any transform or resize.
					if (_pathAtlas && TryAtlasBatch(tcmds, ref ti, fOwned, Vector2.One, out var aop2))
					{
						order.Add(new FrameOp { Kind = null, NonSolid = aop2 });
						continue;
					}
					// Same glyph-run collapse as the table path: one stencil + one cover for a run of
					// consecutive non-zero paths sharing colour + clip, instead of 2 draws per glyph.
					if (tc is PathFill pf0 && !pf0.EvenOdd && !pf0.FanTiles)
					{
						float fSlotBits = System.BitConverter.Int32BitsToSingle(fSlot);
						_scratch.Clear();
						var gMin = new Vector2(float.MaxValue); var gMax = new Vector2(float.MinValue);
						int gj = ti;
						while (gj < tcmds.Count && tcmds[gj] is PathFill pfj && !pfj.EvenOdd
							&& pfj.Color.R == pf0.Color.R && pfj.Color.G == pf0.Color.G && pfj.Color.B == pf0.Color.B && pfj.Color.A == pf0.Color.A
							&& ClipDataEquals(pfj.Clip, pf0.Clip))
						{
							for (int gi = 0; gi < pfj.FanDevice.Length; gi += 2) { _scratch.Add(pfj.FanDevice[gi]); _scratch.Add(pfj.FanDevice[gi + 1]); _scratch.Add(fSlotBits); }
							gMin = Vector2.Min(gMin, pfj.BbMin); gMax = Vector2.Max(gMax, pfj.BbMax);
							gj++;
						}
						var gFan = Vbuf(_scratch, fOwned);
						uint gCount = (uint)(_scratch.Count / 3);
						float gr = pf0.Color.R / 255f, gg = pf0.Color.G / 255f, gb = pf0.Color.B / 255f, ga = pf0.Color.A / 255f;
						_scratch.Clear();
						var gTl = gMin; var gBr = gMax; var gTr = new Vector2(gBr.X, gTl.Y); var gBl = new Vector2(gTl.X, gBr.Y);
						PushVertT(gTl, gr, gg, gb, ga, fSlotBits); PushVertT(gTr, gr, gg, gb, ga, fSlotBits); PushVertT(gBr, gr, gg, gb, ga, fSlotBits);
						PushVertT(gTl, gr, gg, gb, ga, fSlotBits); PushVertT(gBr, gr, gg, gb, ga, fSlotBits); PushVertT(gBl, gr, gg, gb, ga, fSlotBits);
						var gCov = Vbuf(_scratch, fOwned);
						var gOp = new DrawOp(DrawKind.Path, (nint)gFan, gCount, (nint)gCov, false, pf0.Clip, (nint)MakeClipBg(_d.CoverClipBgl, pf0.Clip, fOwned));
						order.Add(new FrameOp { Kind = null, NonSolid = ResidentizeFan(gOp, fOwned) });
						ti = gj - 1;
						continue;
					}
					tmp.Clear();
					BuildSimpleOp(tc, tmp, fOwned, fSlot);
					foreach (var o in tmp) { order.Add(new FrameOp { Kind = null, NonSolid = ResidentizeFan(o, fOwned) }); }
				}
			}
			// `id` is stable across frames so a static recording's slice stays resident (no re-upload) and
			// coalesces with neighbours across recordings; a repeat emission gets a fresh transient id.
			long id = repeat ? _d.NextSlabId()
				: ((fMiss || fe is null || fe.SlabId == 0) ? _d.NextSlabId() : fe.SlabId);
			// Upload the (transform-baked) verts under this recording's stable id and keep them resident on
			// the cache. FrameOrder offsets stay RELATIVE — the absolute base (returned here, re-derived on a
			// later pure reuse) is applied at emit — never a cached absolute into a possibly-reclaimed slice.
			sBase = sv.Count > 0 ? _d.SolidSlab.Put(id, sv) : 0;
			rBase = rv.Count > 0 ? _d.RrectSlab.Put(id, rv) : 0;
			fe = new WebGpuGeometryCache { FrameSolid = true, SlabId = id, FrameOrder = order, FrameSolidVerts = sv, FrameRrectVerts = rv, Owned = fOwned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = fSlot };
			// A repeat emission is not cached (its slice is transient); free its bind groups next frame.
			if (repeat) { _d.DeferRelease(fOwned); }
			else { StoreCompiled(rr.Data, fe); }
		}
		else
		{
			// Pure reuse (no rebuild): re-derive the CURRENT slab base. TryByteOffset marks the slice live
			// on a hit; if it was culled last frame its offset was reclaimed, so re-Put the resident verts.
			if (fe.FrameSolidVerts is { Count: > 0 } && !_d.SolidSlab.TryByteOffset(fe.SlabId, out sBase)) { sBase = _d.SolidSlab.Put(fe.SlabId, fe.FrameSolidVerts); }
			if (fe.FrameRrectVerts is { Count: > 0 } && !_d.RrectSlab.TryByteOffset(fe.SlabId, out rBase)) { rBase = _d.RrectSlab.Put(fe.SlabId, fe.FrameRrectVerts); }
		}
		// Rewrite this recording's path-fill transform entry every frame (device verts => pure current
		// projection), so a window resize repositions its glyphs via the table with no re-tessellation.
		if (fe.XformSlot >= 0) { WriteXform(fe.XformSlot, Matrix4x4.Identity); }
		// Per frame: re-emit ops drawing from the RESIDENT shared slabs (b0=1 => solid slab / rrect slab;
		// b1 = absolute slab byte offset). No append, no upload, no re-tessellation on a cache hit.
		foreach (var fo in fe.FrameOrder)
		{
			if (fo.Kind == DrawKind.Solid) { ops.Add(new DrawOp(DrawKind.Solid, VertexSource.Slab, fo.Count, (nint)(sBase + fo.ByteOff), false, fo.Clip, fo.ClipBg)); }
			else if (fo.Kind == DrawKind.RoundedRect) { ops.Add(new DrawOp(DrawKind.RoundedRect, VertexSource.Slab, fo.Count, (nint)(rBase + fo.ByteOff), false, fo.Clip, fo.ClipBg)); }
			else { ops.Add(fo.NonSolid); }
		}
		return;
	}

	/// <summary>
	/// Replays an ARENA recording: geometry baked once in its own identity space, with the replay transform
	/// applied on the GPU. A move re-stamps the per-op clip bind groups and reuses the vertex buffers; only a
	/// resize, a scale/rotation change, or an atlas entry that can no longer be reused forces a rebuild.
	/// </summary>
	private void EmitArenaReplay(ReplayRefCmd rr, List<DrawOp> ops, WebGpuGeometryCache entry, bool miss)
	{
		// Stable path-fill transform slot: arena verts are in the recording's OWN (identity) space, so
		// the slot's entry folds the replay transform + projection — written per frame below, so a
		// move OR resize repositions the fan/cover via the table with no re-stamp and no re-bake.
		int aSlot = (miss || entry is null) ? -1 : entry.XformSlot;
		// A pure-path arena entry is surface-size-independent (device verts + table), so a resize is
		// handled by the per-frame table write below with NO rebuild; a mixed entry's NDC-baked solids
		// still force a size rebuild.
		bool aSizeChanged = entry is not null && (entry.BuiltW != (int)_s.Width || entry.BuiltH != (int)_s.Height);
		// An atlas quad is an image op: its NDC is baked at build time and it carries no xform-table
		// slot, so unlike the rest of a pure-path entry it is NOT re-projected when the surface
		// size changes. Replaying one on a differently-sized target drew it scaled (the offscreen
		// RenderTargetBitmap path, which is how the shape parity tests capture).
		if (miss || !entry.Arena || (aSizeChanged && (!entry.PurePath || entry.HasAtlas))
			|| (entry.HasAtlas && !(TryAtlasScale(rr.Transform, out var curScale) && SameAtlasScale(curScale, entry.AtlasScale)))
			|| (entry.AtlasBlockedByScale && TryAtlasScale(rr.Transform, out _)))
		{
			if (_emitStats) { _statArenaRebuilds++; }
			if (entry is not null) { _d.DeferRelease(entry.Owned); _d.DeferRelease(entry.StampOwned); }
			var aOwned = new OwnedResources();
			var aOps = new List<DrawOp>();
			var aList = new List<WebGpuCommand>();
			foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, Matrix4x4.Identity, ClipData.None)) { aList.Add(tc); }
			bool aHasPath = false, aPure = aList.Count > 0; foreach (var c in aList) { if (c is PathFill) { aHasPath = true; } else { aPure = false; } }
			if (aHasPath && aSlot < 0) { aSlot = _d.AllocXformSlot(); }
			int atlasBefore = AtlasHit + AtlasBaked;
			bool aAtlasSafe = TryAtlasScale(rr.Transform, out var aScale);
			BuildCoalesced(aList, aOps, aOwned, aSlot, atlasScale: aAtlasSafe ? aScale : null);
			bool aHasAtlas = (AtlasHit + AtlasBaked) != atlasBefore;
			bool aBlocked = !aAtlasSafe && aHasPath && _pathAtlas;
			for (int _ri = 0; _ri < aOps.Count; _ri++) { aOps[_ri] = ResidentizeFan(aOps[_ri], aOwned); }
			entry = new WebGpuGeometryCache { Ops = aOps, Owned = aOwned, Transform = rr.Transform, Clip = rr.Clip, Arena = true, HasAtlas = aHasAtlas, AtlasBlockedByScale = aBlocked, AtlasScale = aScale, PurePath = aPure, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = aSlot };
			StoreCompiled(rr.Data, entry);
		}
		// Per frame (even on a cache/stamp hit): the identity-space verts map to the current replay
		// transform + surface projection via this one table entry — the whole arena move/resize path.
		if (entry.XformSlot >= 0) { WriteXform(entry.XformSlot, rr.Transform); }
		if ((!entry.HasStamp || entry.StampXform != rr.Transform || !ClipDataEquals(entry.StampClip, rr.Clip)))
		{
			if (_emitStats) { _statStamps++; }
			// In-place restamp (same guard as the table stamp): rewrite ClipU buffers, keep bind groups.
			var reuse = entry.HasStamp && entry.StampBufs is not null && entry.StampBufs.Count == entry.Ops.Count && entry.StampFrame != _d.FrameSeq;
			if (!reuse && entry.StampOwned is not null) { _d.DeferRelease(entry.StampOwned); }
			var stampOwned = reuse ? entry.StampOwned : new OwnedResources();
			var stamped = reuse ? entry.StampedOps : new List<DrawOp>(entry.Ops.Count);
			var bufs = reuse ? entry.StampBufs : new List<nint>(entry.Ops.Count);
			var xf = ArenaXform(rr.Transform);
			// finv = inverse device affine, so clipCov maps the moved fragment back to the recording's
			// own space where the (identity-baked) clip lives.
			var t2 = new Matrix3x2(rr.Transform.M11, rr.Transform.M12, rr.Transform.M21, rr.Transform.M22, rr.Transform.M41, rr.Transform.M42);
			Matrix3x2 finv = Matrix3x2.Invert(t2, out var inv) ? inv : Matrix3x2.Identity;
			Vector2 MoveP(float x, float y) => new(x * t2.M11 + y * t2.M21 + t2.M31, x * t2.M12 + y * t2.M22 + t2.M32);
			// One ClipU for every fan in this recording: same arena transform, so it is built once —
			// but LAZILY. xf/finv change every frame under a moving transform, so the content-keyed
			// cache misses each time and this mints a buffer + bind group per recording per frame
			// (370/frame on RenderStress_Gradients). Most recordings carry no fan at all, and once
			// rounded rects are recognised analytically none of them do.
			nint arenaFanBg = 0;
			for (int i = 0; i < entry.Ops.Count; i++)
			{
				var op = entry.Ops[i];
				var abgl = op.kind switch { DrawKind.Gradient => _d.GradClipBgl, DrawKind.Image => _d.ImageClipBgl, _ => _d.SolidClipBgl };
				// clipCov reads the LOCAL rounded shape (finv maps fc back to it); the SCISSOR is device-space
				// so its Aabb must follow the move — transform the (finite) clip Aabb by the replay transform.
				var scissorClip = op.clip;
				// The op's own fan is identity-space (arena bakes at identity): map it to device for
				// this replay. FanBuf is cleared because the residentized NDC buffer was baked for
				// identity and is stale once moved.
				if (op.clip.PathFan is { } localFan)
				{
					// stampOwned, never the per-frame slab: this bind group is memoized into StampedOps and
				// reused on later frames while the transform holds.
				if (arenaFanBg == 0) { arenaFanBg = (nint)MakeClipBg(_d.ClipBgl, default, stampOwned, xf, finv); }
					// Keep the identity-space fan and its resident NDC buffer; hand the stencil draw
					// the arena transform instead. Transforming on the CPU here would mean a fresh
					// fan upload per op per frame (392/frame on RenderStress_Gradients).
					scissorClip.FanXformBg = arenaFanBg;
					var fa = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
					for (int fi = 0; fi < localFan.Length; fi += 2)
					{
						var mp = MoveP(localFan[fi], localFan[fi + 1]);
						fa = new Vector4(MathF.Min(fa.X, mp.X), MathF.Min(fa.Y, mp.Y), MathF.Max(fa.Z, mp.X), MathF.Max(fa.W, mp.Y));
					}
					scissorClip.Aabb = new Vector4(MathF.Max(scissorClip.Aabb.X, fa.X), MathF.Max(scissorClip.Aabb.Y, fa.Y), MathF.Min(scissorClip.Aabb.Z, fa.Z), MathF.Min(scissorClip.Aabb.W, fa.W));
				}
				var ab = op.clip.Aabb;
				if (ab.X > -1e8f || ab.Y > -1e8f || ab.Z < 1e8f || ab.W < 1e8f)
				{
					var p0 = MoveP(ab.X, ab.Y); var p1 = MoveP(ab.Z, ab.Y); var p2 = MoveP(ab.Z, ab.W); var p3 = MoveP(ab.X, ab.W);
					scissorClip.Aabb = new Vector4(
						MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)),
						MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X)), MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y)));
				}
				// Session clip: tighten the device scissor by its Aabb; fold its rounds into ClipU (local space).
				var sa = rr.Clip.Aabb;
				scissorClip.Aabb = new Vector4(MathF.Max(scissorClip.Aabb.X, sa.X), MathF.Max(scissorClip.Aabb.Y, sa.Y), MathF.Min(scissorClip.Aabb.Z, sa.Z), MathF.Min(scissorClip.Aabb.W, sa.W));
				scissorClip.ScissorInert = op.clip.ScissorInert && rr.Clip.ScissorInert;
				// Carry the session fan onto the stamped op so the depth mask still clips it — unless it
				// provably covers this op, in which case attaching it would cost an ApplyDepthClip
				// setup (4 pipeline switches + 3 draws + a bind group + a vertex buffer) that cannot
				// change a pixel. The arena builds at identity with ClipData.None, so the build-time
				// strip never sees the session fan; this is the only place it can be caught.
				if (rr.Clip.PathFan is { } sessionFan
					&& !(!rr.Clip.PathExclude && FanCoversAabb(sessionFan, scissorClip.Aabb)))
				{
					scissorClip.PathFan = sessionFan;
					scissorClip.PathEvenOdd = rr.Clip.PathEvenOdd;
					scissorClip.PathExclude = rr.Clip.PathExclude;
					scissorClip.FanBuf = rr.Clip.FanBuf;
					scissorClip.FanW = rr.Clip.FanW;
					scissorClip.FanH = rr.Clip.FanH;
				}

				var uClip = op.clip;
				FoldSessionRounds(ref uClip, rr.Clip.Rounds, finv);
				var uSessionFinite = IsFiniteAabb(rr.Clip.Aabb);
				var uAxisAligned = finv.M12 == 0 && finv.M21 == 0;
				var uCanWiden = !uSessionFinite || uAxisAligned;
				if (uSessionFinite && uAxisAligned)
				{
					FoldSessionAabb(ref uClip, rr.Clip.Aabb, finv);
				}
				if (reuse)
				{
					scissorClip.AabbInClipU = RewriteClipU(bufs[i], uClip, xf, finv) && uCanWiden;
					scissorClip.ScissorLoadBearing = !scissorClip.AabbInClipU;
					stamped[i] = new DrawOp(op.kind, op.b0, op.u0, op.b1, op.flag, scissorClip, stamped[i].clipBg);
				}
				else
				{
					var aClipBg = MakeClipBgOwned(abgl, uClip, stampOwned, xf, finv, out var buf, out var aFolded);
					scissorClip.AabbInClipU = aFolded && uCanWiden;
					scissorClip.ScissorLoadBearing = !scissorClip.AabbInClipU;
					bufs.Add(buf);
					stamped.Add(new DrawOp(op.kind, op.b0, op.u0, op.b1, op.flag, scissorClip, (nint)aClipBg));
				}
			}
			entry.StampOwned = stampOwned; entry.StampedOps = stamped; entry.StampBufs = bufs; entry.StampFrame = _d.FrameSeq; entry.StampXform = rr.Transform; entry.StampClip = rr.Clip; entry.HasStamp = true;
		}
		ops.AddRange(entry.StampedOps);
		return;
	}

	private void EmitReplayRef(ReplayRefCmd rr, List<DrawOp> ops, HashSet<List<WebGpuCommand>> frameEmitted)
	{
		// Cull a recording whose (transformed) content is entirely clipped out or off-surface: the
		// widened cull-only scissor no longer rejects it, so a scrolled-out row/card would otherwise
		// still pay its per-frame stamps/rebuilds and its draws every frame. A culled recording's
		// slab slices are reclaimed by RetainOnly and re-Put when it scrolls back in (TryByteOffset
		// handles the reclaimed-slice case).
		var rrBounds = ClampToClip(TransformBounds(rr.Data.IdentityBounds ??= CmdListBounds(rr.Commands), rr.Transform), rr.Clip);
		if (rrBounds.X >= rrBounds.Z || rrBounds.Y >= rrBounds.W
			|| rrBounds.Z <= 0 || rrBounds.W <= 0 || rrBounds.X >= _s.Width || rrBounds.Y >= _s.Height)
		{
			return;
		}
		// FRAME-SOLID path: any recording that contains rects — a Border background,
		// a Button (background + border + glyphs) — re-emits its SOLIDS into the SHARED per-pass buffer
		// every frame so sibling visuals sharing a clip collapse to ONE draw (the cross-visual draw-count
		// win the profiler showed). NON-solids (glyphs/images/gradients) stay cached (device space,
		// rebuilt only on a transform/clip change) and are consumed in draw order as the recording is
		// re-walked. Pure non-solid recordings fall through to the arena path below (moving-visual reuse).
		if (HasReappendable(rr))
		{
			EmitReappendableReplay(rr, ops, frameEmitted);
			return;
		}

		// The per-visual GPU-geometry cache (slab/scroll), keyed by the recording's immutable command
		// list. Build once; reuse while it's replayed at the same transform/clip. A stale entry (moved
		// visual) is deferred-released and rebuilt. Entries not referenced any frame are evicted.
		var entry = rr.Data.Compiled;
		var miss = entry is null;
		// ARENA: a transform-safe recording (solid/image, no clip) bakes its geometry ONCE in its own
		// identity NDC space. A moved replay re-stamps the vertex xform on the per-op clip bind groups
		// and reuses the vertex buffers rather than rebuilding them.
		// A session PATH clip does not force the device-bake path: the fan is applied separately by the
		// in-pass depth mask (ApplyDepthClip reads clip.PathFan in device space), so only a session
		// clip's rounds/AABB need folding through finv.
		if (IsArenaSafe(rr))
		{
			EmitArenaReplay(rr, ops, entry, miss);
			return;
		}

		var transformChanged = !miss && entry.Transform != rr.Transform;
		int cSlot = (miss || entry is null) ? -1 : entry.XformSlot;
		// Bisect level 4: reuse the previous bake on a pure move (visually stale, but it prices the
		if (miss || transformChanged || entry.Arena || entry.BuiltW != (int)_s.Width || entry.BuiltH != (int)_s.Height || !ClipDataEquals(entry.Clip, rr.Clip))
		{
			// Why did this rebuild? The cached path is the only replay path that re-bakes geometry on a
			// MOVE (table and arena both re-stamp), so a scrolling recording that lands here pays a full
			// re-tessellate + re-upload every frame.
			if (_emitStats)
			{
				_statCachedRebuilds++;
				if (miss) { _statCrMiss++; }
				else if (transformChanged) { _statCrMove++; }
				else if (entry.Arena) { _statCrPathFlip++; }
				else if (entry.BuiltW != (int)_s.Width || entry.BuiltH != (int)_s.Height) { _statCrSize++; }
				else { _statCrClip++; }
			}
			if (entry is not null) { _d.DeferRelease(entry.Owned); }
			var owned = new OwnedResources();
			var cachedOps = new List<DrawOp>();
			var cList = new List<WebGpuCommand>();
			foreach (var tc in WebGpuCommandRecorder.TransformFor(rr.Commands, rr.Transform, rr.Clip)) { cList.Add(tc); }
			bool cHasPath = false; foreach (var c in cList) { if (c is PathFill) { cHasPath = true; break; } }
			if (cHasPath && cSlot < 0) { cSlot = _d.AllocXformSlot(); }
			BuildCoalesced(cList, cachedOps, owned, cSlot, atlasScale: Vector2.One);
			for (int _ri = 0; _ri < cachedOps.Count; _ri++) { cachedOps[_ri] = ResidentizeFan(cachedOps[_ri], owned); }
			entry = new WebGpuGeometryCache { Ops = cachedOps, Owned = owned, Transform = rr.Transform, Clip = rr.Clip, Device = _d, BuiltW = (int)_s.Width, BuiltH = (int)_s.Height, XformSlot = cSlot };
			StoreCompiled(rr.Data, entry);
		}
		// Device-space verts => the slot's entry is the pure current projection (rewritten per frame so a
		// resize repositions the path fills via the table without re-baking).
		if (entry.XformSlot >= 0) { WriteXform(entry.XformSlot, Matrix4x4.Identity); }
		// Splice the cached draw-ops straight into this frame's op list — replayed by direct encoding in
		// the main pass, NOT a render bundle (ExecuteBundles measured ~6x slower on wgpu-native, and forces
		// a scissor reset; direct replay keeps each op's scissor). Buffers/bind groups persist in `owned`.
		ops.AddRange(entry.Ops);
	}

	/// <summary>
	/// Renders a drop shadow: blurred coverage of the silhouette offscreen, composited as a SrcIn-tinted
	/// image at its device placement. Culled first when the blurred extent is entirely clipped or offscreen,
	/// since it otherwise pays a coverage render plus blur every frame.
	/// </summary>
	private void EmitShadow(ShadowCmd sh, List<DrawOp> ops)
	{
		// Cull a shadow whose blurred extent is entirely clipped out or off-surface (e.g. a
		// scrolled-out card) — otherwise it still pays a coverage render + blur every frame.
		var shPad = MathF.Ceiling(3f * MathF.Max(sh.SigmaX, sh.SigmaY)) + 2f;
		var shExt = ClampToClip(Inflate(new Vector4(sh.BbMin.X, sh.BbMin.Y, sh.BbMax.X, sh.BbMax.Y), shPad), sh.Clip);
		if (shExt.X >= shExt.Z || shExt.Y >= shExt.W || shExt.Z <= 0 || shExt.W <= 0 || shExt.X >= _s.Width || shExt.Y >= _s.Height)
		{
			return;
		}
		// Render the blurred coverage offscreen, then composite it as a SrcIn-tinted image (tint =
		// shadow color) at its device placement — reusing the image draw path (kind 2), incl. clip.
		var blurView = RenderShadow(sh, out var origin, out var size);
		var sbg = TintedImageBg(blurView, sh.Color);
		var sq = TexturedQuad(origin, size);
		ops.Add(new DrawOp(DrawKind.Image, (nint)sbg, 0, (nint)MakeBuffer(sq), false, sh.Clip, (nint)MakeClipBg(_d.ImageClipBgl, sh.Clip)));
	}

	/// <summary>
	/// Renders a layer: its content goes to an offscreen surface, then composites back onto the parent (and,
	/// for a layer carrying a drop-shadow effect, a blurred tinted copy goes down first). Both the offscreen
	/// render and the composite record into the frame's single encoder, so wgpu orders them for us.
	/// </summary>
	private void EmitLayer(LayerCmd lyr, List<DrawOp> ops)
	{
		// Cull a plain (SrcOver, no colour-matrix) layer whose content — including its shadow's offset+blur
		// reach — is entirely clipped out or off-surface: a scrolled-out card with a ThemeShadow otherwise
		// still pays a full offscreen render + blur every frame. Mask (DstIn) and colour-matrix layers keep
		// full-surface semantics (an empty mask must still erase; a matrix offset can produce coverage).
		var contentBounds = ClampToClip(CmdListBounds(lyr.Commands), lyr.Clip);
		if (lyr.CompositeMode == 0 && lyr.ColorMatrix is null)
		{
			var vis = contentBounds;
			if (lyr.ShadowEffect is { } sfx && contentBounds.X <= contentBounds.Z)
			{
				var spad = MathF.Ceiling(3f * MathF.Max(sfx.SigmaX, sfx.SigmaY)) + 2f;
				var sb = ClampToClip(Inflate(new Vector4(contentBounds.X + sfx.Dx, contentBounds.Y + sfx.Dy, contentBounds.Z + sfx.Dx, contentBounds.W + sfx.Dy), spad), lyr.Clip);
				vis = new Vector4(MathF.Min(vis.X, sb.X), MathF.Min(vis.Y, sb.Y), MathF.Max(vis.Z, sb.Z), MathF.Max(vis.W, sb.W));
			}
			if (vis.X >= vis.Z || vis.Y >= vis.W || vis.Z <= 0 || vis.W <= 0 || vis.X >= _s.Width || vis.Y >= _s.Height)
			{
				return;
			}
		}

		// Render the layer's commands into a full-size offscreen surface, then composite (kind 4). Both the
		// offscreen render and this composite record into the frame's single encoder, so wgpu barriers the
		// offscreen resolve before the composite samples it — no explicit flush needed.
		var layerSurface = new WebGpuRenderSurface(_d, _s.Width, _s.Height, _d.Pool);
		var _savedPw = _passW; var _savedPh = _passH;
		_passW = layerSurface.Width; _passH = layerSurface.Height;
		RenderInto(lyr.Commands, layerSurface, null);
		_passW = _savedPw; _passH = _savedPh;

		// The layer's depth/stencil is write-only inside its own (now ended) pass: cleared on entry,
		// discarded on exit, never sampled. Hand it straight back so every layer in the frame reuses
		// ONE depth texture instead of renting its own — a stack of N layers otherwise keeps N
		// full-window depth targets resident for the whole frame. The colour view can NOT be returned
		// here: the composite op below samples it, and that is encoded later in the parent's pass.
		_d.Pool.Return(layerSurface.DepthView);
		// Reclaimed after submit (see _frameLayerSurfaces): the composite below still samples it.
		if (layerSurface.Pooled) { _frameLayerSurfaces.Add(layerSurface); }

		// SaveLayer(IEffectFilter) drop shadow: blur the content, draw it tinted+offset behind, then
		// the content on top. Reuses the image path (SrcIn tint) for the shadow — same as DrawShadow.
		// The pyramid runs only over the content's region (padded by the blur reach), not the full
		// window — a card-sized caster costs card-sized blur passes.
		if (lyr.ShadowEffect is { } fx)
		{
			var pad = MathF.Ceiling(3f * MathF.Max(fx.SigmaX, fx.SigmaY)) + 2f;
			var rg = Inflate(contentBounds, pad);
			float rx = MathF.Max(0f, MathF.Floor(rg.X)), ry = MathF.Max(0f, MathF.Floor(rg.Y));
			float rw = MathF.Min(_s.Width, MathF.Ceiling(rg.Z)) - rx, rh = MathF.Min(_s.Height, MathF.Ceiling(rg.W)) - ry;
			if (rw >= 1f && rh >= 1f)
			{
				var blur = BlurPyramidRegion(layerSurface.View, _s.Width, _s.Height, rx, ry, rw, rh, fx.SigmaX, fx.SigmaY);
				var sfbg = TintedImageBg(blur, fx.Color);
				var fq = TexturedQuad(new Vector2(fx.Dx + rx, fx.Dy + ry), new Vector2(rw, rh));
				ops.Add(new DrawOp(DrawKind.Image, (nint)sfbg, 0, (nint)MakeBuffer(fq), false, lyr.Clip, (nint)MakeClipBg(_d.ImageClipBgl, lyr.Clip)));
			}
		}

		var cu = new float[24];
		cu[0] = lyr.ColorMatrix is { Length: >= 20 } ? 1f : 0f; cu[1] = 1f;
		if (lyr.ColorMatrix is { Length: >= 20 } mm)
		{
			cu[4] = mm[0]; cu[5] = mm[1]; cu[6] = mm[2]; cu[7] = mm[3];        // m0
			cu[8] = mm[5]; cu[9] = mm[6]; cu[10] = mm[7]; cu[11] = mm[8];      // m1
			cu[12] = mm[10]; cu[13] = mm[11]; cu[14] = mm[12]; cu[15] = mm[13]; // m2
			cu[16] = mm[15]; cu[17] = mm[16]; cu[18] = mm[17]; cu[19] = mm[18]; // m3
			cu[20] = mm[4]; cu[21] = mm[9]; cu[22] = mm[14]; cu[23] = mm[19];   // off (5th column)
		}
		var lubuf = MakeUniform((int)96);
		fixed (float* p = cu) { wgpuQueueWriteBuffer(_d.Q, lubuf, 0, (IntPtr)p, 96); }
		// Two entries, not three: the composite shader uses textureLoad, so its layout has no sampler.
		var lentries = stackalloc WGPUBindGroupEntry[2];
		lentries[0] = new WGPUBindGroupEntry { Binding = 0, TextureView = layerSurface.View };
		lentries[1] = new WGPUBindGroupEntry { Binding = 2, Buffer = lubuf, Offset = 0, Size = 96 };
		var lbgd = new WGPUBindGroupDescriptor { Layout = lyr.CompositeMode == 1 ? _d.CompositeDstInBgl : _d.CompositeBgl, EntryCount = 2, Entries = lentries };
		var lbg = _d.TrackBg(wgpuDeviceCreateBindGroup(_d.Dev, &lbgd));
		// Scissor the composite to the layer's content. The composite shader draws a FULLSCREEN triangle,
		// so without this every layer blends the whole window no matter how small it is — a tooltip's
		// opacity group costs the same as a full-page scrim. Only plain SrcOver layers can be tightened:
		// a DstIn mask must still erase outside its content, and a colour matrix with an offset can turn
		// transparent pixels opaque, so both keep full-surface semantics (same condition as the cull above).
		var compClip = lyr.Clip;
		if (lyr.CompositeMode == 0 && lyr.ColorMatrix is null && IsFiniteAabb(contentBounds))
		{
			compClip.Aabb = contentBounds;
			compClip.ScissorInert = false;
			compClip.AabbInClipU = false;
			compClip.ScissorLoadBearing = true;
		}
		ops.Add(new DrawOp(DrawKind.CompositeLayer, (nint)lbg, (uint)lyr.CompositeMode, 0, false, compClip, 0));
	}
}
