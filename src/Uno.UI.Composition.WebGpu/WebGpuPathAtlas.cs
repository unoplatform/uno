#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// A coverage atlas for small paths, glyphs above all: each distinct (geometry, scale, subpixel phase) is rasterized
/// once into a shared alpha texture and afterwards drawn as a tinted quad.
/// <para>
/// Geometric coverage (<see cref="PathTessellator"/>) cannot antialias text — a glyph stem is about a pixel wide, so
/// insetting it folds it inside out — and those shapes are refused rather than drawn wrong, which leaves text on
/// MSAA. A baked mask sidesteps that and collapses two draws per glyph into one quad.
/// </para>
/// </summary>
internal sealed unsafe class WebGpuPathAtlas
{
	/// <summary>Atlas edge in texels. One page; when it fills, later shapes fall back to the geometry path.</summary>
	public const int Size = 1024;

	/// <summary>
	/// Per-axis bound: a slot cannot span pages, so nothing wider or taller than a page can ever be placed.
	/// </summary>
	public const int MaxDim = Size - 2;

	/// <summary>
	/// Footprint bound in mask AREA, not longest side: text arrives as one fill per RUN, so a run is very wide and
	/// only ~20px tall, and a longest-side cap of 96 rejected every run past a few characters — leaving it on the
	/// geometry path with no antialiasing. Raising the cap to admit large near-square fills instead measured no
	/// parity gain, cut no draws, and pushed LogView from 320 to 420 draws by displacing glyph runs from their
	/// batches.
	/// </summary>
	public const int MaxArea = 256 * 256;

	/// <summary>
	/// Pages this atlas may hold before it stops taking new entries and shapes fall back to the geometry path.
	/// A page is Size*Size*4 bytes, so this is the cache's memory ceiling — without it, content whose transform
	/// animates mints a fresh key every frame and grows the atlas without bound.
	/// </summary>
	public const int MaxPages = 8;

	/// <summary>Fills refused for footprint — the counter to watch if content renders aliased.</summary>
	internal static int RejBig;

	/// <summary>Subpixel phases per axis. 4 is the usual quality/footprint compromise.</summary>
	public const int SubPixel = 4;

	/// <summary>
	/// W/H are part of the key because the scale is quantised: two nearby scales can share a key while needing
	/// different pixel footprints. All four 2x2 terms are in it too — the mask is rasterized in its final device
	/// orientation, so two angles of one geometry would otherwise collide and share the wrong mask.
	/// </summary>
	internal readonly record struct Key(object Geometry, int M11, int M12, int M21, int M22, int PhaseX, int PhaseY, int W, int H);

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

		/// <summary>
		/// How many recordings hold this entry. Sharing makes this necessary: with per-glyph geometry ONE slot backs
		/// every occurrence of a character across many recordings, so releasing it when the recording that happened
		/// to bake it goes away would hand the region to another glyph while the rest still sample those UVs — which
		/// draws one character in place of another.
		/// </summary>
		public int RefCount;

		/// <summary>Frame this entry was last used, for the cache's own reference (see <see cref="HoldForCache"/>).</summary>
		public long LastUsed;

		/// <summary>True while the CACHE holds one of this slot's references (a per-frame bake, owned by nobody else).</summary>
		public bool CacheHeld;
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
				var reused = bucket.Pop() with { OriginX = originX, OriginY = originY, Key = key, RefCount = 1 };
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

			var slot = new Slot(page.CursorX, page.ShelfY, w, h, originX, originY) { Key = key, Owner = page, RefCount = 1 };
			page.CursorX += w;
			if (h > page.ShelfH) { page.ShelfH = h; }
			page.Live++;
			_slots[key] = slot;
			return slot;
		}

		return null;   // every page is exhausted; the caller opens another and retries
	}

	/// <summary>Frames an entry may go unused before the cache drops its reference.</summary>
	public const int CacheIdleFrames = 180;

	private readonly List<Slot> _cacheHeld = new();

	/// <summary>Records that an entry was used this frame, so the idle sweep keeps it.</summary>
	public void NoteUse(Slot slot, long frame)
	{
		if (slot is not null) { slot.LastUsed = frame; }
	}

	/// <summary>
	/// Gives the CACHE a reference to an entry baked for a per-frame op. Nothing else will free it, so the sweep
	/// below does, once it has gone unused long enough that re-baking is cheaper than holding the region.
	/// </summary>
	public void HoldForCache(Slot slot, long frame)
	{
		if (slot is null || slot.CacheHeld) { return; }
		slot.CacheHeld = true;
		slot.LastUsed = frame;
		_cacheHeld.Add(slot);
	}

	/// <summary>Drops the cache's reference to entries unused for <see cref="CacheIdleFrames"/> frames.</summary>
	public void SweepCache(long frame)
	{
		for (var i = _cacheHeld.Count - 1; i >= 0; i--)
		{
			var slot = _cacheHeld[i];
			if (!slot.CacheHeld) { _cacheHeld.RemoveAt(i); continue; }
			if (frame - slot.LastUsed <= CacheIdleFrames) { continue; }
			slot.CacheHeld = false;
			_cacheHeld.RemoveAt(i);
			// A recording that also uses this entry holds its own reference, so this only reclaims the region
			// when the cache was the last holder.
			Free(slot);
		}
	}

	/// <summary>Takes an additional reference for a recording that reuses an entry it did not bake.</summary>
	public void Retain(Slot slot)
	{
		if (slot is not null) { slot.RefCount++; }
	}

	/// <summary>
	/// Returns a slot to the free pool. Called when the cached recording that owns it is released, which is the
	/// only safe moment: a recording bakes its slot's UVs into its draw ops, so reclaiming a slot while any
	/// recording still referenced it would make that recording sample another shape's mask.
	/// </summary>
	public void Free(Slot slot)
	{
		if (slot?.Owner is not { } page) { return; }
		// Only the LAST holder may reclaim the region — see Slot.RefCount.
		if (--slot.RefCount > 0) { return; }
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
	/// <param name="scale">
	/// Extra scale applied to this op AFTER its own coordinates, i.e. the GPU-side replay scale of an arena
	/// recording (1,1 for geometry already in device space). The mask has to be rasterized at the size the shape
	/// actually covers on screen, so every pixel quantity here — footprint, origin snap, subpixel phase — is
	/// computed in DEVICE space, while the origin is returned in the op's own space for placing the quad.
	/// </param>
	public static bool TryKey(object? geometry, in Matrix4x4 matrix, Vector2 bbMin, Vector2 bbMax, Vector2 scale, out Key key, out int w, out int h, out float originX, out float originY)
	{
		key = default;
		w = h = 0;
		originX = originY = 0;
		if (geometry is null || scale.X <= 0 || scale.Y <= 0) { return false; }

		var dw = (bbMax.X - bbMin.X) * scale.X;
		var dh = (bbMax.Y - bbMin.Y) * scale.Y;
		if (dw <= 0 || dh <= 0) { return false; }
		if (dw > MaxDim || dh > MaxDim || dw * dh > MaxArea)
		{ RejBig++; return false; }

		// Snap the slot origin to whole DEVICE pixels and let the mask absorb the fractional offset. Placing the
		// quad at a fractional position instead makes the sampler resample a 1:1 mask, which visibly blurs and
		// fattens glyphs — the phase belongs in the baked mask (that is what the phase key is for).
		var devMinX = bbMin.X * scale.X;
		var devMinY = bbMin.Y * scale.Y;
		var oxDev = MathF.Floor(devMinX);
		var oyDev = MathF.Floor(devMinY);
		originX = oxDev / scale.X;
		originY = oyDev / scale.Y;

		// A one-texel skirt keeps bilinear sampling from bleeding a neighbouring slot into the edge.
		w = (int)MathF.Ceiling(bbMax.X * scale.X - oxDev) + 2;
		h = (int)MathF.Ceiling(bbMax.Y * scale.Y - oyDev) + 2;

		// Subpixel phase HORIZONTALLY only. Vertical phase would multiply the entry count for no visible gain on
		// horizontal text, and it is what makes a scrolling list miss the cache on every frame: with Y quantised,
		// a list scrolled by whole pixels reuses its glyphs instead of re-rasterising them.
		var phaseX = (int)MathF.Floor((devMinX - oxDev) * SubPixel);
		var phaseY = 0;
		key = new Key(
			geometry,
			(int)MathF.Round(matrix.M11 * scale.X * 64f),
			(int)MathF.Round(matrix.M12 * scale.X * 64f),
			(int)MathF.Round(matrix.M21 * scale.Y * 64f),
			(int)MathF.Round(matrix.M22 * scale.Y * 64f),
			Math.Clamp(phaseX, 0, SubPixel - 1),
			Math.Clamp(phaseY, 0, SubPixel - 1),
			w,
			h);
		return true;
	}
}
