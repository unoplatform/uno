#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// A coverage atlas for small paths — glyphs above all. Each distinct (geometry, scale, subpixel phase) is
/// rasterized ONCE into a shared alpha texture; afterwards the shape draws as a textured quad tinted by its
/// colour.
/// </summary>
/// <remarks>
/// Why this exists: geometric coverage (<see cref="PathTessellator"/>) cannot antialias text. A glyph stem at
/// normal sizes is about one pixel wide, so insetting it by half a pixel folds it inside out, and text fills
/// NON-ZERO while hole detection by nesting parity misreads overlapping kerned glyphs. Both cases are refused
/// rather than drawn wrong, which left text depending on MSAA. Baking coverage into a texture sidesteps the
/// problem entirely: the rasterizer resolves it once, at 4x, and every later frame just samples it.
///
/// It also removes the per-glyph draw explosion. Stencil-then-cover costs two pipeline switches and two draws
/// per glyph; an atlased glyph is one quad, and consecutive quads sharing colour and texture merge.
///
/// The cache key includes a subpixel phase so glyph positions stay sub-pixel accurate (rounding them to whole
/// pixels visibly damages spacing). It deliberately does NOT cover rotated or skewed transforms: the entry is
/// rasterized axis-aligned, so anything else falls back to the geometry path.
/// </remarks>
internal sealed unsafe class WebGpuPathAtlas
{
	/// <summary>Atlas edge in texels. One page; when it fills, later shapes fall back to the geometry path.</summary>
	public const int Size = 1024;

	/// <summary>Largest shape (device px) worth atlasing — beyond this the texture cost outweighs the redraw.</summary>
	public const int MaxShape = 96;

	/// <summary>Subpixel phases per axis. 4 is the usual quality/footprint compromise.</summary>
	public const int SubPixel = 4;

	/// <summary>
	/// W/H are part of the key, not just the scale: the scale is quantised, so two nearby scales can share a key
	/// while needing different pixel footprints — the cached mask would then be the wrong SIZE for the draw.
	/// </summary>
	internal readonly record struct Key(object Geometry, int ScaleX, int ScaleY, int PhaseX, int PhaseY, int W, int H);

	/// <summary>
	/// A reserved region. Origin is the shape's device-space bbox corner the entry was rasterized against, which
	/// the draw needs to place the quad back.
	/// </summary>
	internal sealed record Slot(int X, int Y, int W, int H, float OriginX, float OriginY)
	{
		/// <summary>The entry this slot backs, so freeing it can drop the cache entry too.</summary>
		public Key Key { get; init; }

		/// <summary>The page this slot lives on; the draw samples this page's view.</summary>
		public Page? Owner { get; init; }
	}

	/// <summary>
	/// One atlas page. Pages exist because a cached recording bakes its slot's UVs into its draw ops: a live slot
	/// can never be moved or reassigned, so when a page runs out the only safe response is to open another. A page
	/// is destroyed once its last slot is freed.
	/// </summary>
	internal sealed class Page
	{
		public IntPtr Texture, View;
		public int ShelfY, ShelfH, CursorX;
		public int Live;
		public readonly Dictionary<(int W, int H), Stack<Slot>> Free = new();
	}

	private readonly Dictionary<Key, Slot> _slots = new();
	private readonly List<Page> _pages = new();

	/// <summary>Pages whose texture should be destroyed (their last slot was freed).</summary>
	public readonly List<Page> Retired = new();

	public IReadOnlyList<Page> Pages => _pages;

	public Page AddPage(IntPtr texture, IntPtr view)
	{
		var page = new Page { Texture = texture, View = view };
		_pages.Add(page);
		return page;
	}

	/// <summary>True when every page is exhausted and a new one is required.</summary>
	public bool NeedsPage
	{
		get
		{
			for (var i = 0; i < _pages.Count; i++) { if (_pages[i].ShelfY + _pages[i].ShelfH < Size) { return false; } }
			return true;
		}
	}

	public bool TryGet(in Key key, out Slot slot) => _slots.TryGetValue(key, out slot!);

	/// <summary>Reserves a w x h region, or returns null when the page cannot fit it.</summary>
	public Slot? Allocate(in Key key, int w, int h, float originX, float originY)
	{
		if (w <= 0 || h <= 0 || w > Size || h > Size) { return null; }

		// Reuse a freed region of the same size before consuming new space. This is what lets a long scroll run
		// indefinitely: rows that scroll away release their slots, and rows scrolling in take them.
		for (var i = 0; i < _pages.Count; i++)
		{
			if (_pages[i].Free.TryGetValue((w, h), out var bucket) && bucket.Count > 0)
			{
				var reused = bucket.Pop() with { OriginX = originX, OriginY = originY, Key = key };
				_pages[i].Live++;
				_slots[key] = reused;
				return reused;
			}
		}

		// Shelf packing: fill a row left to right, then start a new row below it. Cheap, and glyphs from one
		// font arrive at similar heights, so the waste stays small.
		for (var i = 0; i < _pages.Count; i++)
		{
			var page = _pages[i];
			if (page.CursorX + w > Size)
			{
				page.ShelfY += page.ShelfH;
				page.ShelfH = 0;
				page.CursorX = 0;
			}
			if (page.ShelfY + h > Size) { continue; }

			var slot = new Slot(page.CursorX, page.ShelfY, w, h, originX, originY) { Key = key, Owner = page };
			page.CursorX += w;
			if (h > page.ShelfH) { page.ShelfH = h; }
			page.Live++;
			_slots[key] = slot;
			return slot;
		}

		return null;   // every page is exhausted; the caller opens another and retries
	}

	/// <summary>
	/// Returns a slot to the free pool. Called when the cached recording that owns it is released, which is the
	/// only safe moment: a recording bakes its slot's UVs into its draw ops, so reclaiming a slot while any
	/// recording still referenced it would make that recording sample another shape's mask.
	/// </summary>
	public void Free(Slot slot)
	{
		if (slot?.Owner is not { } page) { return; }
		if (_slots.TryGetValue(slot.Key, out var cur) && ReferenceEquals(cur, slot)) { _slots.Remove(slot.Key); }
		if (!page.Free.TryGetValue((slot.W, slot.H), out var bucket)) { page.Free[(slot.W, slot.H)] = bucket = new Stack<Slot>(); }
		bucket.Push(slot);

		// A page with no live slots is pure memory: retire it (unless it is the only one, which would just be
		// reallocated immediately).
		if (--page.Live <= 0 && _pages.Count > 1)
		{
			_pages.Remove(page);
			Retired.Add(page);
		}
	}

	/// <summary>
	/// Builds the cache key for a fill, or returns false when it is not atlasable: rotated/skewed transforms
	/// (the entry is rasterized axis-aligned), and shapes too large to be worth a texture.
	/// </summary>
	public static bool TryKey(object? geometry, in Matrix4x4 matrix, Vector2 bbMin, Vector2 bbMax, out Key key, out int w, out int h, out float originX, out float originY)
	{
		key = default;
		w = h = 0;
		originX = originY = 0;
		if (geometry is null) { return false; }
		if (MathF.Abs(matrix.M12) > 1e-4f || MathF.Abs(matrix.M21) > 1e-4f) { return false; }

		var dw = bbMax.X - bbMin.X;
		var dh = bbMax.Y - bbMin.Y;
		if (dw <= 0 || dh <= 0 || dw > MaxShape || dh > MaxShape) { return false; }

		// Snap the slot origin to whole PIXELS and let the mask absorb the fractional offset. Placing the quad at
		// a fractional position instead makes the sampler resample a 1:1 mask, which visibly blurs and fattens
		// glyphs — the phase belongs in the baked mask (that is what the phase key is for), not in the placement.
		originX = MathF.Floor(bbMin.X);
		originY = MathF.Floor(bbMin.Y);

		// A one-texel skirt keeps bilinear sampling from bleeding a neighbouring slot into the edge.
		w = (int)MathF.Ceiling(bbMax.X - originX) + 2;
		h = (int)MathF.Ceiling(bbMax.Y - originY) + 2;

		// Subpixel phase HORIZONTALLY only. Vertical phase would multiply the entry count for no visible gain on
		// horizontal text, and it is what makes a scrolling list miss the cache on every frame: with Y quantised,
		// a list scrolled by whole pixels reuses its glyphs instead of re-rasterising them.
		var phaseX = (int)MathF.Floor((bbMin.X - MathF.Floor(bbMin.X)) * SubPixel);
		var phaseY = 0;
		key = new Key(
			geometry,
			(int)MathF.Round(matrix.M11 * 64f),
			(int)MathF.Round(matrix.M22 * 64f),
			Math.Clamp(phaseX, 0, SubPixel - 1),
			Math.Clamp(phaseY, 0, SubPixel - 1),
			w,
			h);
		return true;
	}
}
