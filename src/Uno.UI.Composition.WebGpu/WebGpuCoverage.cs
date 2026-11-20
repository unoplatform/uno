// A frame's coverage masks: path clips and big fills baked by signed area, glyphs and small shapes placed from the
// coverage atlas (WebGpuCoverage.Atlas.cs), every bake batched per target texture (WebGpuCoverage.Bakes.cs). The
// atlas and the shape cache themselves live on the device and outlive the frame.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.WebGPU;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

internal sealed unsafe partial class WebGpuCoverage
{
	private readonly WebGpuFrame _f;
	private readonly WebGpuDevice _d;
	private WebGPU Api => _d.Api;

	internal WebGpuCoverage(WebGpuFrame f, WebGpuDevice d)
	{
		_f = f;
		_d = d;
	}

	// Coverage atlas: ON by default - it is what makes arbitrary path edges and glyphs crisp without MSAA.
	// UNO_WEBGPU_PATH_ATLAS=0 opts out (falls back to tessellated AA, which aliases on curved outlines).
	internal static readonly bool AtlasEnabled = Environment.GetEnvironmentVariable("UNO_WEBGPU_PATH_ATLAS") is not "0";

	internal static int ClipMasksBaked;

	// A path's rasterisation inputs at the density the GPU draws the op's space at (see WebGpuShapeCache).
	internal WebGpuShapeCache.Shape ShapeOf(PathCmd c, Vector2 scale)
	{
		var density = MathF.Max(scale.X, scale.Y);
		return c.Stroke > 0f ? _d.Shapes.GetStroke(c.Geometry, c.M, c.Stroke, density, c.Join) : _d.Shapes.Get(c.Geometry, c.M, density, c.EvenOdd);
	}

	// A clip's path masks: one mask entry per path (its slot in View), so nesting has no limit and no product bake. All
	// of one clip's masks live on ONE atlas page, the single mask binding a draw has.
	internal struct MaskSet { public TextureView* View; public ClipEntry[] Entries; }

	// Per frame. Keyed on the composed path list AND the owning bag: the list is shared by every command under
	// one clip (ClipCompose memoizes it), so a clip is baked once per frame; the bag is part of the key because a
	// texture that lives in bag A must not be sampled by a group cached in bag B, which A's release would strand.
	// Immediate (unowned) requests share one entry per list and are released at the next frame start.
	private readonly Dictionary<(PathClip[], OwnedResources), MaskSet> _clipMasks = new();

	private static bool UsesMask(in ClipData c) => c.Paths is { Length: > 0 };

	// The slot a path's mask needs: its box grown a pixel each side and, for a per-frame bake, clamped to the surface,
	// since nothing outside it is ever seen. Never empty: an off-surface path bakes a 1x1 mask whose texel reads ~0.
	private void MaskRect(PathClip p, bool clampToSurface, out int ox, out int oy, out int w, out int h)
	{
		float l = p.Bbox.X, t = p.Bbox.Y, r = p.Bbox.Z, b = p.Bbox.W;
		if (clampToSurface) { l = MathF.Max(l, 0f); t = MathF.Max(t, 0f); r = MathF.Min(r, _f.Target.Width); b = MathF.Min(b, _f.Target.Height); }
		ox = (int)MathF.Floor(l) - 1; oy = (int)MathF.Floor(t) - 1;
		w = Math.Clamp((int)MathF.Ceiling(r) + 1 - ox, 1, 4096);
		h = Math.Clamp((int)MathF.Ceiling(b) + 1 - oy, 1, 4096);
	}

	// A clip path's rasterisation inputs in the clip's space (its masks bake at unit density there).
	private WebGpuShapeCache.Shape ShapeOf(PathClip p) => _d.Shapes.Get(p.Geometry, p.M, 1f, p.EvenOdd);

	// A mask entry whose texel (0,0), at slot (x, y), sits on pixel (ox, oy) of the clip's space.
	private static ClipEntry MaskEntry(int ox, int oy, int x, int y, int w, int h, bool exclude)
		=> new() { Mask = true, M = Matrix3x2.CreateTranslation(-ox, -oy), Rect = new Vector4(x, y, w, h), Exclude = exclude };

	/// <summary>
	/// The mask entries for a clip's path list, from the cache or baked on a miss. A clip's masks bind as one texture,
	/// so several paths share a page. Runs during op BUILD.
	/// </summary>
	internal MaskSet ResolveClipMasks(in ClipData cd, OwnedResources owned)
	{
		if (!UsesMask(cd)) { return default; }
		var memo = (cd.Paths, owned);
		if (_clipMasks.TryGetValue(memo, out var cached)) { return cached; }

		var paths = cd.Paths;
		var n = paths.Length;
		var shapes = new WebGpuShapeCache.Shape[n];
		var keys = new WebGpuMaskAtlas.Key[n];
		var sizes = new (int W, int H)[n];
		var origins = new Vector2[n];
		for (var i = 0; i < n; i++)
		{
			shapes[i] = ShapeOf(paths[i]);
			keys[i] = ClipMaskKey(paths[i], shapes[i], owned, out origins[i], out sizes[i]);
		}
		var slots = new WebGpuMaskAtlas.Slot[n];
		bool fresh;
		if (n == 1) { slots[0] = _d.Atlas.Acquire(keys[0], sizes[0].W, sizes[0].H, origins[0].X, origins[0].Y, out fresh); }
		else { _d.Atlas.AcquireGroup(keys, sizes, origins, slots, out fresh); }

		var entries = new ClipEntry[n];
		for (var i = 0; i < n; i++)
		{
			var p = paths[i];
			var slot = slots[i];
			var (w, h) = sizes[i];
			int ox = (int)origins[i].X, oy = (int)origins[i].Y;
			if (owned is not null) { _d.Atlas.Retain(slot); (owned.AtlasSlots ??= new()).Add(slot); }
			if (fresh && !shapes[i].Edges.IsNull)
			{
				AddBake(BatchFor(slot.Owner), slot.X, slot.Y, w, h, shapes[i].Edges, new Vector2(ox + 1, oy + 1) - p.Offset, Vector2.One, p.EvenOdd);
				ClipMasksBaked++;
			}
			entries[i] = MaskEntry(ox, oy, slot.X, slot.Y, w, h, p.Exclude);
		}
		var set = new MaskSet { View = slots[0].Owner.View, Entries = entries };
		_clipMasks[memo] = set;
		return set;
	}

	// A clip path's mask key and the clip-space pixel its texel (0,0) sits on: keyed by outline and transform, or, when
	// too big for that, by exactly what its bake rasterizes (clamped to the surface when per-frame).
	private WebGpuMaskAtlas.Key ClipMaskKey(PathClip p, WebGpuShapeCache.Shape shape, OwnedResources owned, out Vector2 origin, out (int W, int H) size)
	{
		if (!shape.Edges.IsNull && WebGpuMaskAtlas.TryKey(shape.Hash, Matrix4x4.Identity, new Vector2(p.Bbox.X, p.Bbox.Y), new Vector2(p.Bbox.Z, p.Bbox.W), Vector2.One,
			out var key, out var w, out var h, out var ox, out var oy, allowBig: true))
		{
			origin = new Vector2(ox, oy);
			size = (w, h);
			return key;
		}
		MaskRect(p, clampToSurface: owned is null, out var rx, out var ry, out var rw, out var rh);
		origin = new Vector2(rx, ry);
		size = (rw, rh);
		return WebGpuMaskAtlas.ExactKey(shape.Hash, Vector2.One, p.Offset - origin, rw, rh, p.EvenOdd);
	}

	// Masks are baked into and sampled from; in the device's swapchain format, which the resolve pipelines target.
	internal const TextureUsage MaskUsage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding;

	internal static int FillMasksBaked, FillMaskHits;

	/// <summary>
	/// Draws a fill through an exact coverage mask: the route for every fill the atlas refused (too large, no key)
	/// and the tessellator refused (self-overlap, even-odd, or one that simply failed). Cached like any mask, but
	/// keyed by its exact placement, so the atlas and the ringed tiling fan take what they can first. <paramref name="scale"/> is the device
	/// scale the GPU applies to the op's space afterwards, exactly as for the atlas.
	/// </summary>
	internal bool TryMaskFill(PathCmd pf, WebGpuShapeCache.Shape shape, OwnedResources owned, Vector2 scale, bool filtered, out DrawOp op)
	{
		op = default;
		if (shape.Edges is not { Length: >= 12 } || scale.X <= 0 || scale.Y <= 0) { return false; }
		float dx0 = pf.BbMin.X * scale.X, dy0 = pf.BbMin.Y * scale.Y, dx1 = pf.BbMax.X * scale.X, dy1 = pf.BbMax.Y * scale.Y;
		int ox = (int)MathF.Floor(dx0) - 1, oy = (int)MathF.Floor(dy0) - 1;
		int w = (int)MathF.Ceiling(dx1) + 1 - ox, h = (int)MathF.Ceiling(dy1) + 1 - oy;
		if (w <= 0 || h <= 0) { return false; }
		// A mask beyond a page-sized texture bakes at reduced density and upscales through its quad: softer than
		// 1:1, but never refused, because refusal would leave the fill with no route at all.
		var big = MathF.Max(w, h);
		if (big > 4096f)
		{
			var k = 4096f / big;
			scale *= k; dx0 *= k; dy0 *= k; dx1 *= k; dy1 *= k;
			ox = (int)MathF.Floor(dx0) - 1; oy = (int)MathF.Floor(dy0) - 1;
			w = (int)MathF.Ceiling(dx1) + 1 - ox; h = (int)MathF.Ceiling(dy1) + 1 - oy;
		}

		var origin = new Vector2((ox + 1) / scale.X, (oy + 1) / scale.Y) - pf.Offset;
		var key = WebGpuMaskAtlas.ExactKey(shape.Hash, scale, pf.Offset * scale - new Vector2(ox, oy), w, h, pf.EvenOdd);
		var slot = _d.Atlas.Acquire(key, w, h, ox, oy, out var fresh);
		if (owned is not null) { _d.Atlas.Retain(slot); (owned.AtlasSlots ??= new()).Add(slot); }
		var page = slot.Owner;
		if (fresh)
		{
			AddBake(BatchFor(page), slot.X, slot.Y, w, h, shape.Edges, origin, scale, pf.EvenOdd);
			FillMasksBaked++;
		}
		else { FillMaskHits++; }
		var view = page.View;
		var uv = new Vector4(slot.X, slot.Y, slot.X + w, slot.Y + h) / new Vector4(page.W, page.H, page.W, page.H);

		// A solid quad with the mask as its coverage, 1:1 with device pixels once the GPU applies the scale -- the same
		// draw an atlas entry uses, so it coalesces and re-stamps like one.
		var clip = WithCoverage(pf.Clip, view, filtered);
		var q = CoverageQuad(new Vector2(ox / scale.X, oy / scale.Y), new Vector2(w / scale.X, h / scale.Y), pf.Color, uv);
		op = DrawOp.Own(DrawKind.Solid, _f.Vbuf(q, VertexStride.Solid, owned), 6, null, clip, _f.MakeClipBg(clip, owned));
		return true;
	}
}
