// The coverage atlas: rasterizes a shape's coverage mask once, then places it as a textured quad 1:1 with device
// pixels for as long as nothing about its device-space footprint changes.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Silk.NET.WebGPU;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Logging;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuCoverage
{
	/// <summary>
	/// Emits an atlased fill as a tinted quad, or returns false to leave it on the geometry path.
	/// </summary>
	internal bool TryAtlasFill(PathCmd pf, WebGpuShapeCache.Shape shape, List<DrawOp> ops, OwnedResources owned, Vector2 scale, bool big = false, bool filtered = false, Vector2 place = default)
	{
		if (!TryAtlasOp(pf, shape, owned, scale, out var op, big, filtered, place)) { return false; }
		ops.Add(op);
		return true;
	}

	internal static int AtlasTried, AtlasNoKey, AtlasHit, AtlasBaked, AtlasNoRoom, AtlasNoEdges, ScaleBlocked;

	/// <summary>
	/// Keys and places on the op's own coordinate space, with <paramref name="scale"/> giving the extra scale the
	/// GPU applies afterwards — Vector2.One for geometry already in device space, the replay scale for an arena
	/// recording (identity-baked geometry mapped by the xform table). Getting that scale wrong bakes the mask at
	/// the wrong size, which is what broke When_ShapeVisual_ViewBox_Shape_Combinations.
	/// </summary>
	private bool TryAtlasOp(PathCmd pf, WebGpuShapeCache.Shape shape, OwnedResources owned, Vector2 scale, out DrawOp result, bool big = false, bool filtered = false, Vector2 place = default)
	{
		result = default;
		if (!TryAtlasSlot(pf, shape, owned, scale, out var slot, out var ox, out var oy, big, place)) { return false; }
		_atlasQuads.Clear();
		var extent = AppendAtlasQuad(_atlasQuads, slot, ox, oy, scale, pf.Color);
		result = MakeAtlasOp(pf, slot.Owner, _atlasQuads, owned, extent, filtered);
		return true;
	}

	private readonly VertBuf _atlasQuads = new();

	// A batch ends after this many glyphs, so one long run (a 100K-glyph line) becomes ops compact enough for the
	// replay to drop the ones its clip leaves out; a single op would rasterize every glyph against the scissor.
	private const int AtlasBatchMaxQuads = 256;

	/// <summary>
	/// A position among consecutive path commands: a plain fill, or one instance of a run.
	/// </summary>
	internal struct FillCursor
	{
		public int Cmd, Item;

		public FillCursor(int cmd) { Cmd = cmd; }

		public readonly bool Valid(List<WebGpuCommand> cmds) => Cmd < cmds.Count && cmds[Cmd].Kind is CmdKind.Path or CmdKind.PathRun;

		/// <summary>The fill here, a run's written into <paramref name="scratch"/>; null for an instance without area.</summary>
		public readonly PathCmd Current(List<WebGpuCommand> cmds, PathCmd scratch)
			=> cmds[Cmd] is PathRunCmd run ? (run.Fill(Item, scratch) ? scratch : null) : (PathCmd)cmds[Cmd];

		public void Advance(List<WebGpuCommand> cmds)
		{
			if (cmds[Cmd] is PathRunCmd run && Item + 1 < run.Instances.Length) { Item++; }
			else { Cmd++; Item = 0; }
		}
	}

	private PathCmd _runFill;

	/// <summary>A run's instance as a fill, rewritten for each one. Nothing keeps the fill it is handed.</summary>
	internal PathCmd RunFill => _runFill ??= new();

	// Pages one batch may draw from, one op each. Made on the first batch a frame builds, so a frame that builds
	// nothing pays nothing; the first page's quads go in _atlasQuads, the others' in buffers rented from the device.
	private const int AtlasBatchMaxPages = 4;
	private VertBuf[] _pageQuads;
	private WebGpuMaskAtlas.Page[] _pages;
	private Vector4[] _pageExtents;

	/// <summary>Hands the batch buffers rented for this frame back to the device.</summary>
	internal void ReturnPooled()
	{
		if (_pageQuads is not { } quads) { return; }
		for (var p = 1; p < quads.Length; p++)
		{
			if (quads[p] is { } q) { _d.ReturnVerts(q); quads[p] = null; }
		}
	}

	/// <summary>
	/// Emits a RUN of consecutive fills sharing a colour and a clip as one draw per atlas page they sample, advancing
	/// <paramref name="at"/> past them. Per-glyph geometry turns a string into N fills, and a draw apiece is far
	/// worse than the merged run it replaces. Grouping by page reorders fills of one colour under one clip, which
	/// source-over blends to the same result in any order up to rounding: where two glyphs' coverage overlaps, an
	/// 8-bit target can differ by one level. Splitting the run at every page change instead broke text into a draw
	/// every few glyphs once its glyphs had spread over pages.
	/// </summary>
	internal bool TryAtlasBatch(List<WebGpuCommand> cmds, ref FillCursor at, OwnedResources owned, Vector2 scale, List<DrawOp> ops, Vector2 place = default)
	{
		var first = at.Current(cmds, RunFill);
		if (first is null) { return false; }
		if (!TryAtlasSlot(first, ShapeOf(first, scale), owned, scale, out var slot0, out var ox0, out var oy0, place: place)) { return false; }

		var color = first.Color;
		var clip = first.Clip;
		var pages = 1;
		_pages ??= new WebGpuMaskAtlas.Page[AtlasBatchMaxPages];
		_pageQuads ??= new VertBuf[AtlasBatchMaxPages];
		_pageExtents ??= new Vector4[AtlasBatchMaxPages];
		_pages[0] = slot0.Owner;
		var q0 = _pageQuads[0] ??= _atlasQuads;
		q0.Clear();
		_pageExtents[0] = AppendAtlasQuad(q0, slot0, ox0, oy0, scale, color);
		var quads = 1;
		at.Advance(cmds);
		while (quads < AtlasBatchMaxQuads && at.Valid(cmds))
		{
			var nx = at.Current(cmds, RunFill);
			if (nx is null) { at.Advance(cmds); continue; }
			if (nx.Color.A != color.A || nx.Color.R != color.R || nx.Color.G != color.G || nx.Color.B != color.B
				|| !WebGpuFrame.ClipDataEquals(nx.Clip, clip)) { break; }
			// A fill that does not take the atlas, or lands on a page past the batch's last, stays where it is; the
			// caller picks it up next, by then a cache hit.
			if (!TryAtlasSlot(nx, ShapeOf(nx, scale), owned, scale, out var slotN, out var oxN, out var oyN, place: place)) { break; }
			var p = 0;
			while (p < pages && !ReferenceEquals(_pages[p], slotN.Owner)) { p++; }
			if (p == pages)
			{
				if (pages == AtlasBatchMaxPages) { break; }
				_pages[p] = slotN.Owner;
				(_pageQuads[p] ??= _d.RentVerts()).Clear();
				pages++;
				_pageExtents[p] = AppendAtlasQuad(_pageQuads[p], slotN, oxN, oyN, scale, color);
			}
			else
			{
				_pageExtents[p] = WebGpuFrame.Union(_pageExtents[p], AppendAtlasQuad(_pageQuads[p], slotN, oxN, oyN, scale, color));
			}
			quads++;
			at.Advance(cmds);
		}

		for (var p = 0; p < pages; p++)
		{
			ops.Add(MakeAtlasOp(clip, _pages[p], _pageQuads[p], owned, _pageExtents[p]));
			_pages[p] = null;
		}
		return true;
	}

	/// <summary>
	/// Resolves (baking on a miss) the atlas entry for one fill. <paramref name="big"/> admits fills too large for
	/// a shared page as entries with a texture of their own -- the cached form of what used to be a per-frame mask.
	/// </summary>
	private bool TryAtlasSlot(PathCmd pf, WebGpuShapeCache.Shape shape, OwnedResources owned, Vector2 scale, out WebGpuMaskAtlas.Slot slot, out float ox, out float oy, bool big = false, Vector2 place = default)
	{
		slot = null; ox = oy = 0;
		if (!AtlasEnabled) { return false; }
		AtlasTried++;
		// A cached recording OWNS the entries it bakes and frees them when released. A per-frame op has no such
		// owner, so the ATLAS holds the reference and drops it after the entry goes idle (HoldForCache/SweepCache).
		// Per-frame ops must be able to BAKE, not only hit: restricting them to hits looks safer (nothing owns
		// the entry) but renders identical content crisp through the retained path and tessellated through the
		// command-list fallback.
		bool hitOnly = owned is null;
		if (shape.Edges is null) { AtlasNoEdges++; return false; }
		// The mask's footprint must come from the geometry the bake will rasterize, not from the command's bounds:
		// those are the geometry's declared bounds mapped by the matrix, and a geometry whose outline reaches past
		// them leaves the slot too small. The accumulate pass clamps every edge to the slot's right bound, so the
		// overflow is not merely cropped - the winding it carries is lost and the fill comes out empty.
		// A big fill is one mask of its own, so it keeps its exact phase; small fills (glyphs above all) share entries
		// across the fractional positions text puts them at.
		if (!WebGpuMaskAtlas.TryKey(shape.Hash, Matrix4x4.Identity, shape.BbMin + pf.Offset, shape.BbMax + pf.Offset, scale, out var key, out var w, out var h, out ox, out oy, out var snap, allowBig: big, place: place, phases: big ? 0 : WebGpuMaskAtlas.FillPhases)) { AtlasNoKey++; return false; }

		if (_d.Atlas.TryGet(key, out slot))
		{
			// An entry baked through the big-fill route is a cached mask, not an atlas quad, whatever its size: it was
			// baked at the transform's axis lengths and its quad rotates it, so the replay guards must compare its
			// scale, not its axis alignment.
			if (big) { FillMaskHits++; } else { AtlasHit++; }
			_d.Atlas.NoteUse(slot, _d.FrameSeq);
			// A REUSED entry needs its own reference for this recording. One slot backs a glyph everywhere it
			// appears, so without this the recording that baked it releases the region out from under every other
			// holder and they start sampling whatever took its place.
			// Once per bag, not per occurrence: a glyph repeated through a long text is one reference, not thousands.
			if (!hitOnly && slot.LastBag != owned.Id)
			{
				slot.LastBag = owned.Id;
				_d.Atlas.Retain(slot);
				(owned.AtlasSlots ??= new()).Add(slot);
			}
		}
		else
		{
			// A per-frame op enters the cache only once its key has held for two frames (see Recurring); until then
			// it takes the per-frame bake, so moving content never accumulates entries.
			if (owned is null && big && !_d.Atlas.Recurring(key, _d.FrameSeq)) { return false; }
			if (WebGpuMaskAtlas.IsBig(w, h))
			{
				slot = AddStandaloneSlot(key, w, h, ox, oy, _d.ColorFormat);
			}
			else
			{
				// Opens another page when every one is exhausted, up to the cache's ceiling.
				slot = _d.Atlas.Allocate(key, w, h, ox, oy);
			}
			if (slot is null) { AtlasNoRoom++; _d.AtlasStarved = true; return false; }
			if (owned is not null) { slot.LastBag = owned.Id; (owned.AtlasSlots ??= new()).Add(slot); }
			else { _d.Atlas.HoldForCache(slot, _d.FrameSeq); }
			QueueEntryBake(pf, shape, slot, scale, snap);
			if (big) { FillMasksBaked++; } else { AtlasBaked++; }
		}

		return true;
	}

	// A keyed entry on a standalone page, so it shares the key, reference count and idle sweep of a shelf slot. Null
	// when no page could be created.
	internal WebGpuMaskAtlas.Slot AddStandaloneSlot(in WebGpuMaskAtlas.Key key, int w, int h, float ox, float oy, TextureFormat format)
		=> AddStandaloneSlot(key, w, h, ox, oy, format, w, h, MaskUsage);

	// The entry draws w x h from a page of at least texW x texH: a blurred shadow is stored at its pyramid's top level.
	internal WebGpuMaskAtlas.Slot AddStandaloneSlot(in WebGpuMaskAtlas.Key key, int w, int h, float ox, float oy, TextureFormat format, int texW, int texH, TextureUsage usage)
		=> _d.Atlas.StandalonePage(texW, texH, format, usage) is { } page ? _d.Atlas.Place(page, 0, 0, w, h, ox, oy, key) : null;

	/// <summary>
	/// Appends one entry as 6 solid vertices (pos, colour, coverage uv), placed at the fill's OWN origin, and returns
	/// the quad's rect in that space.
	/// </summary>
	private Vector4 AppendAtlasQuad(VertBuf dst, WebGpuMaskAtlas.Slot slot, float ox, float oy, Vector2 scale, WColor color)
	{
		float cr = color.R / 255f, cg = color.G / 255f, cb = color.B / 255f, ca = color.A / 255f;
		// The quad lives in the op's own space; one device pixel is 1/scale there, so a slot.W-wide mask needs a
		// slot.W/scale-wide quad to land 1:1 after the replay scale is applied on the GPU. Placed at this fill's
		// origin rather than the slot's: on a cache hit the same shape elsewhere draws at its own position, and
		// the subpixel phase is part of the key, so the mask already suits it.
		float x0 = ox - 1f / scale.X, y0 = oy - 1f / scale.Y;
		float x1 = x0 + slot.W / scale.X, y1 = y0 + slot.H / scale.Y;
		float pw = slot.Owner.W, ph = slot.Owner.H;
		float u0 = slot.X / pw, v0 = slot.Y / ph;
		float u1 = (slot.X + slot.W) / pw, v1 = (slot.Y + slot.H) / ph;
		void QV(float x, float y, float uu, float vv) { dst.Add(x); dst.Add(y); dst.Add(cr); dst.Add(cg); dst.Add(cb); dst.Add(ca); dst.Add(uu); dst.Add(vv); }
		QV(x0, y0, u0, v0); QV(x1, y0, u1, v0); QV(x1, y1, u1, v1);
		QV(x0, y0, u0, v0); QV(x1, y1, u1, v1); QV(x0, y1, u0, v1);
		return new Vector4(x0, y0, x1, y1);
	}

	/// <summary>Six solid vertices (pos, colour, coverage uv over <paramref name="uv"/> = u0,v0,u1,v1) covering the rect at <paramref name="origin"/>.</summary>
	private static float[] CoverageQuad(Vector2 origin, Vector2 size, WColor color, Vector4 uv)
	{
		float cr = color.R / 255f, cg = color.G / 255f, cb = color.B / 255f, ca = color.A / 255f;
		var q = new float[6 * VertexStride.Solid];
		int i = 0;
		void V(float x, float y, float u, float v) { q[i++] = x; q[i++] = y; q[i++] = cr; q[i++] = cg; q[i++] = cb; q[i++] = ca; q[i++] = u; q[i++] = v; }
		float x0 = origin.X, y0 = origin.Y, x1 = origin.X + size.X, y1 = origin.Y + size.Y;
		V(x0, y0, uv.X, uv.Y); V(x1, y0, uv.Z, uv.Y); V(x1, y1, uv.Z, uv.W);
		V(x0, y0, uv.X, uv.Y); V(x1, y1, uv.Z, uv.W); V(x0, y1, uv.X, uv.W);
		return q;
	}

	/// <summary>The op's clip with its own coverage texture as the innermost entry.</summary>
	private static ClipData WithCoverage(ClipData clip, TextureView* view, bool filtered = false)
	{
		clip.Coverage = view;
		clip.CoverageFiltered = filtered;
		return clip;
	}

	/// <summary>One solid draw for a batch of quads sharing a page, a colour and a clip; the page is their coverage.</summary>
	private DrawOp MakeAtlasOp(PathCmd pf, WebGpuMaskAtlas.Page page, VertBuf quads, OwnedResources owned, Vector4 extent, bool filtered = false)
		=> MakeAtlasOp(pf.Clip, page, quads, owned, extent, filtered);

	private DrawOp MakeAtlasOp(in ClipData fillClip, WebGpuMaskAtlas.Page page, VertBuf quads, OwnedResources owned, Vector4 extent, bool filtered = false)
	{
		var clip = WithCoverage(fillClip, page.View, filtered);
		var op = DrawOp.Own(DrawKind.Solid, _f.Vbuf(quads, VertexStride.Solid, owned), (uint)(quads.Count / VertexStride.Solid), null, clip, _f.MakeClipBg(clip, owned));
		op.Extent = extent;
		return op;
	}
}
