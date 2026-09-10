// Signed-area coverage: rasterizes a path's exact per-pixel coverage analytically. The one producer behind every
// atlas mask, path-clip mask and standalone fill mask.
#nullable disable
using System;
using System.Collections.Generic;
using System.Numerics;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed unsafe partial class WebGpuPresentSession
{
	internal static int ClipMasksBaked;

	// A clip's path masks: one mask entry per path (its slot in View), so nesting has no limit and no product bake. All
	// of one clip's masks live in ONE texture, the single mask binding a draw has.
	internal struct MaskSet { public IntPtr View; public ClipEntry[] Entries; }

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
		if (clampToSurface) { l = MathF.Max(l, 0f); t = MathF.Max(t, 0f); r = MathF.Min(r, _s.Width); b = MathF.Min(b, _s.Height); }
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
	/// The mask entries for a clip's path list, baking on first use this frame. One path: a cached entry when its key
	/// is known or recurs, else a sheet slot. Several: slots reserved together on one texture. Runs during op BUILD.
	/// </summary>
	private MaskSet ResolveClipMasks(in ClipData cd, OwnedResources owned)
	{
		if (!UsesMask(cd)) { return default; }
		var key = (cd.Paths, owned);
		if (_clipMasks.TryGetValue(key, out var cached)) { return cached; }

		var set = cd.Paths.Length == 1 && TryCachedClipMask(cd.Paths[0], owned, out var one) ? one : BakeFrameClipMasks(cd.Paths, owned);
		_clipMasks[key] = set;
		return set;
	}

	// A single path clip is a shape like any fill: its mask lives in the atlas (a texture of its own), keyed by outline
	// and transform, so a static clip bakes once and a scrolled one is a hit. Difference is applied where the entry
	// is sampled, so the mask is the same either way.
	private bool TryCachedClipMask(PathClip p, OwnedResources owned, out MaskSet set)
	{
		set = default;
		if (!_pathAtlas) { return false; }
		var shape = ShapeOf(p);
		if (shape.Edges is null) { return false; }
		if (!WebGpuPathAtlas.TryKey(shape.Hash, Matrix4x4.Identity, new Vector2(p.Bbox.X, p.Bbox.Y), new Vector2(p.Bbox.Z, p.Bbox.W), Vector2.One,
			out var key, out var w, out var h, out var ox, out var oy, allowBig: true)) { return false; }
		if (_d.PathAtlas.TryGet(key, out var slot))
		{
			_d.PathAtlas.NoteUse(slot, _d.FrameSeq);
			if (owned is not null) { _d.PathAtlas.Retain(slot); (owned.AtlasSlots ??= new()).Add(slot); }
		}
		else
		{
			if (owned is null && !_d.PathAtlas.Recurring(key, _d.FrameSeq)) { return false; }
			slot = AddStandaloneSlot(key, w, h, ox, oy, _d.ColorFormat);
			if (owned is not null) { (owned.AtlasSlots ??= new()).Add(slot); }
			else { _d.PathAtlas.HoldForCache(slot, _d.FrameSeq); }
			AddBake(BatchFor(slot.Owner.View, slot.Owner.W, slot.Owner.H, load: true), slot.X, slot.Y, w, h, shape.Edges, new Vector2(ox + 1, oy + 1) - p.Offset, Vector2.One, p.EvenOdd);
			ClipMasksBaked++;
		}
		set = new MaskSet { View = slot.Owner.View, Entries = new[] { MaskEntry((int)slot.OriginX, (int)slot.OriginY, slot.X, slot.Y, slot.W, slot.H, p.Exclude) } };
		return true;
	}

	// Per-frame masks, one slot per path, all on one texture: the frame's sheet when they fit on it together, else a
	// texture of their own packed the same way (a cached recording's masks always take their own, since they outlive
	// the frame).
	private MaskSet BakeFrameClipMasks(PathClip[] paths, OwnedResources owned)
	{
		var n = paths.Length;
		var rects = new (int Ox, int Oy, int W, int H)[n];
		var pos = new (int X, int Y)[n];
		for (var i = 0; i < n; i++) { MaskRect(paths[i], clampToSurface: owned is null, out var ox, out var oy, out var w, out var h); rects[i] = (ox, oy, w, h); }
		if (!(owned is null && TryReserveSheetSlots(rects, pos, out var batch)))
		{
			PackOwn(rects, pos, out var tw, out var th);
			var (view, tex) = NewMaskTexture(tw, th);
			if (owned is not null) { (owned.Textures ??= new()).Add(((nint)view, (nint)tex)); }
			else { _d.DeferTextureRelease(view, tex); }
			batch = BatchFor(view, tw, th, load: false);
		}
		var entries = new ClipEntry[n];
		for (var i = 0; i < n; i++)
		{
			var (ox, oy, w, h) = rects[i]; var (x, y) = pos[i];
			var shape = ShapeOf(paths[i]);
			if (shape.Edges is not null) { AddBake(batch, x, y, w, h, shape.Edges, new Vector2(ox + 1, oy + 1) - paths[i].Offset, Vector2.One, paths[i].EvenOdd); }
			entries[i] = MaskEntry(ox, oy, x, y, w, h, paths[i].Exclude);
		}
		ClipMasksBaked += n;
		return new MaskSet { View = batch.Target, Entries = entries };
	}

	// A w x h mask in the device's swapchain format, which is what the resolve pipelines target -- not DefaultColorFormat.
	private (IntPtr view, IntPtr tex) NewMaskTexture(int w, int h)
	{
		var td = new WGPUTextureDescriptor
		{
			Size = new WGPUExtent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = _d.ColorFormat, MipLevelCount = 1, SampleCount = 1, Dimension = WGPUTextureDimension._2D,
			Usage = WGPUTextureUsage.RenderAttachment | WGPUTextureUsage.TextureBinding,
		};
		var tex = wgpuDeviceCreateTexture(_d.Dev, &td);
		return (wgpuTextureCreateView(tex, null), tex);
	}

	internal static int FillMasksBaked, FillMaskHits;

	/// <summary>
	/// Draws a fill through an exact coverage mask: the route for every fill the atlas refused (too large, no key)
	/// and the tessellator refused (self-overlap, even-odd, or one that simply failed). A bake per fill, so the
	/// atlas and the ringed tiling fan take what they can first. <paramref name="scale"/> is the device
	/// scale the GPU applies to the op's space afterwards, exactly as for the atlas.
	/// </summary>
	private bool TryMaskFill(PathCmd pf, WebGpuShapeCache.Shape shape, OwnedResources owned, Vector2 scale, bool filtered, out DrawOp op)
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

		// A per-frame fill goes on the frame's sheet and samples its slot; a cached recording's gets a texture of its own.
		IntPtr view; var uv = new Vector4(0f, 0f, 1f, 1f);
		var origin = new Vector2((ox + 1) / scale.X, (oy + 1) / scale.Y) - pf.Offset;
		if (owned is null && TryReserveSheetSlot(w, h, out var sheet, out var sx, out var sy))
		{
			AddBake(sheet, sx, sy, w, h, shape.Edges, origin, scale, pf.EvenOdd);
			view = sheet.Target;
			uv = new Vector4(sx, sy, sx + w, sy + h) / SheetSize;
		}
		else
		{
			(view, var tex) = NewMaskTexture(w, h);
			AddBake(BatchFor(view, w, h, load: false), 0, 0, w, h, shape.Edges, origin, scale, pf.EvenOdd);
			if (owned is not null) { (owned.Textures ??= new()).Add(((nint)view, (nint)tex)); }
			else { _d.DeferTextureRelease(view, tex); }
		}
		FillMasksBaked++;

		// A solid quad with the mask as its coverage, 1:1 with device pixels once the GPU applies the scale -- the same
		// draw an atlas entry uses, so it coalesces and re-stamps like one.
		var clip = WithCoverage(pf.Clip, view, filtered);
		var q = CoverageQuad(new Vector2(ox / scale.X, oy / scale.Y), new Vector2(w / scale.X, h / scale.Y), pf.Color, uv);
		op = DrawOp.Own(DrawKind.Solid, Vbuf(q, owned), 6, IntPtr.Zero, clip, MakeClipBg(clip, owned));
		return true;
	}
}
