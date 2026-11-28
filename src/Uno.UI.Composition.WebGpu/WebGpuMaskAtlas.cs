#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.WebGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// The device's one cache of coverage masks (fills, glyphs, clips, blurred shadows). Every mask is looked up by the key
/// of what it rasterizes and baked into a slot on a miss; pages are pooled by format and size and the pools grow as
/// needed. The cache holds one reference to each entry and the idle sweep drops it, while a recording that uses an
/// entry holds its own until released. A mask seen in one frame only is swept within a few frames.
/// </summary>
internal sealed unsafe class WebGpuMaskAtlas
{
	/// <summary>Regular page edge in texels.</summary>
	public const int Size = 1024;

	/// <summary>The largest page: WebGPU guarantees 2D textures at least this wide.</summary>
	public const int MaxPageSize = 8192;

	/// <summary>Per-axis bound for a keyed entry on a regular page.</summary>
	public const int MaxDim = Size - 2;

	/// <summary>
	/// Footprint bound for a keyed entry on a regular page, in AREA: text arrives as one fill per run, very wide and
	/// ~20px tall, so a longest-side cap rejected every run past a few characters. Bigger entries go on large pages.
	/// </summary>
	public const int MaxArea = 256 * 256;

	/// <summary>Frames an empty page is kept before its texture is destroyed.</summary>
	public const int EmptyPageIdleFrames = 60;

	/// <summary>Fills refused for footprint: the counter to watch if content renders aliased.</summary>
	internal static int RejBig;

	/// <summary>
	/// Subpixel phases per axis for fill entries. Text places glyphs at arbitrary fractions, so exact phases give every
	/// occurrence its own entry and a page of text fills the atlas.
	/// </summary>
	public const int FillPhases = 4;

	/// <summary>
	/// W/H are in the key because the scale is quantised; all four 2x2 terms are, because the mask is rasterized in
	/// its final device orientation.
	/// </summary>
	internal readonly record struct Key(long Shape, int M11, int M12, int M21, int M22, int PhaseX, int PhaseY, int W, int H, int Extra = 0);

	/// <summary>A reserved region. Origin is the device-space corner the entry was rasterized against.</summary>
	internal sealed record Slot(int X, int Y, int W, int H, float OriginX, float OriginY)
	{
		/// <summary>The cache key, when <see cref="Keyed"/>.</summary>
		public Key Key { get; init; }

		public bool Keyed { get; init; }

		/// <summary>The page this slot lives on; the draw samples this page's view.</summary>
		public Page? Owner { get; init; }

		/// <summary>The region the allocator handed out: its shelf, and the x range on it.</summary>
		internal ShelfAllocator.Shelf? Shelf { get; init; }
		internal int RegionX { get; init; }
		internal int RegionW { get; init; }

		/// <summary>Holders of this slot. One glyph entry backs every occurrence across many recordings, so only the last may reclaim it.</summary>
		public int RefCount;

		/// <summary>Frame this entry was last used, for the cache's own reference (see <see cref="HoldForCache"/>).</summary>
		public long LastUsed;

		/// <summary>Distinct frames the entry was used in.</summary>
		public int Uses;

		/// <summary>True while the cache holds one of this slot's references.</summary>
		public bool CacheHeld;

		/// <summary>The part of the page a draw of this entry samples; a blurred shadow stored reduced covers less than its slot.</summary>
		public Vector4 Uv = new(0f, 0f, 1f, 1f);
	}

	/// <summary>
	/// One texture. A live slot's UVs are baked into its users' draw ops, so a slot never moves: a full page means
	/// opening another, and a page's texture is only destroyed once it has held no live slot for a while.
	/// </summary>
	internal sealed class Page
	{
		public Texture* Texture;
		public TextureView* View;
		public int W, H;
		public TextureFormat Format;
		public TextureUsage Usage;
		public ShelfAllocator Allocator = null!;
		public int Live;
		// Frame the page last became empty; -1 while it holds slots.
		public long EmptySince = -1;
	}

	/// <summary>Pages of one format, usage and size.</summary>
	private sealed class Pool
	{
		public TextureFormat Format;
		public TextureUsage Usage;
		public int PageSize;
		public readonly List<Page> Pages = new();
	}

	private const TextureUsage MaskPageUsage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopyDst;

	/// <summary>Blurred shadows are copied in from the blur's output, which has its own format.</summary>
	internal const TextureUsage ShadowPageUsage = TextureUsage.TextureBinding | TextureUsage.CopyDst;

	private readonly WebGpuDevice _d;
	private readonly Dictionary<Key, Slot> _slots = new();
	private readonly List<Pool> _pools = new();
	private readonly List<Page> _pages = new();
	private long _frame;

	public WebGpuMaskAtlas(WebGpuDevice d) => _d = d;

	/// <summary>Every page of every pool.</summary>
	public IReadOnlyList<Page> Pages => _pages;

	/// <summary>Pages of the regular mask pool.</summary>
	public int RegularPages => PoolFor(_d.ColorFormat, MaskPageUsage, Size).Pages.Count;

	public bool TryGet(in Key key, out Slot slot) => _slots.TryGetValue(key, out slot!);

	private Pool PoolFor(TextureFormat format, TextureUsage usage, int pageSize)
	{
		foreach (var p in _pools)
		{
			if (p.Format == format && p.Usage == usage && p.PageSize == pageSize) { return p; }
		}
		var pool = new Pool { Format = format, Usage = usage, PageSize = pageSize };
		_pools.Add(pool);
		return pool;
	}

	// The page size a w x h region needs: regular when it fits one, else the smallest large size that holds it.
	private static int PageSizeFor(int w, int h)
	{
		var edge = Math.Max(w, h);
		if (edge <= Size) { return Size; }
		return Math.Min(MaxPageSize, Math.Max(2 * Size, (int)BitOperations.RoundUpToPowerOf2((uint)edge)));
	}

	/// <summary>
	/// The entry for <paramref name="key"/>: the cached one, or a fresh w x h slot the caller bakes into
	/// (<paramref name="fresh"/>). The cache holds a reference either way; a recording that keeps the entry
	/// <see cref="Retain"/>s its own. Null when no page could be created (out of GPU memory, or the device was lost):
	/// the caller skips the mask.
	/// </summary>
	public Slot? Acquire(in Key key, int w, int h, float originX, float originY, out bool fresh)
	{
		fresh = false;
		if (TryGet(key, out var hit))
		{
			Use(hit);
			return hit;
		}
		if (Reserve(PoolFor(_d.ColorFormat, MaskPageUsage, PageSizeFor(w, h)), w, h, w, h) is not { } slot) { return null; }
		fresh = true;
		return Insert(slot with { OriginX = originX, OriginY = originY }, key);
	}

	/// <summary>
	/// Entries for all of <paramref name="keys"/> on ONE page, since a clip's masks bind as one texture: the cached ones
	/// when they already share a page, else fresh slots for all of them (<paramref name="fresh"/>), on the smallest page
	/// size that holds them together. False when no page could hold them (out of GPU memory, or the device was lost).
	/// </summary>
	public bool AcquireGroup(Key[] keys, (int W, int H)[] sizes, Vector2[] origins, Slot[] slots, out bool fresh)
	{
		fresh = false;
		Page? shared = null;
		var hits = true;
		for (var i = 0; i < keys.Length && hits; i++)
		{
			hits = TryGet(keys[i], out slots[i]) && (shared ??= slots[i].Owner) == slots[i].Owner;
		}
		if (hits)
		{
			foreach (var s in slots) { Use(s); }
			return true;
		}

		long area = 0;
		int maxEdge = 0;
		foreach (var (w, h) in sizes)
		{
			area += (long)w * h;
			maxEdge = Math.Max(maxEdge, Math.Max(w, h));
		}
		// From the smallest page that could hold the group (with room for packing losses) upwards.
		for (var size = PageSizeFor(maxEdge, maxEdge); ; size *= 2)
		{
			if (area * 3 / 2 <= (long)size * size && TryReserveGroup(PoolFor(_d.ColorFormat, MaskPageUsage, size), sizes, slots)) { break; }
			if (size >= MaxPageSize) { return false; }
		}
		for (var i = 0; i < keys.Length; i++) { slots[i] = Insert(slots[i] with { OriginX = origins[i].X, OriginY = origins[i].Y }, keys[i]); }
		fresh = true;
		return true;
	}

	/// <summary>
	/// Reserves a region for a keyed blurred shadow, stored reduced: the entry draws w x h but holds regionW x regionH
	/// texels, copied in from the blur's output. The caller sets <see cref="Slot.Uv"/> to the part it samples. Null when
	/// no page could be created.
	/// </summary>
	public Slot? AllocateShadow(in Key key, int w, int h, int regionW, int regionH, float originX, float originY)
	{
		var pool = PoolFor(WebGpuDevice.DefaultColorFormat, ShadowPageUsage, PageSizeFor(regionW, regionH));
		if (Reserve(pool, regionW, regionH, w, h) is not { } slot) { return null; }
		return Insert(slot with { OriginX = originX, OriginY = originY }, key);
	}

	// A fresh slot becomes the key's entry, its one reference held by the cache.
	private Slot Insert(Slot slot, in Key key)
	{
		slot = slot with { Key = key, Keyed = true };
		_slots[key] = slot;
		HoldForCache(slot, _frame);
		slot.Uses = 1;
		return slot;
	}

	// A hit: counted for the sweep, and held by the cache again if the sweep had let it go while a recording kept it.
	private void Use(Slot slot)
	{
		NoteUse(slot, _frame);
		if (!slot.CacheHeld)
		{
			slot.RefCount++;
			HoldForCache(slot, _frame);
		}
	}

	private bool TryReserveGroup(Pool pool, (int W, int H)[] sizes, Slot[] slots)
	{
		foreach (var page in pool.Pages)
		{
			if (TryPlaceAll(page, sizes, slots)) { return true; }
		}
		return AddPage(pool) is { } fresh && TryPlaceAll(fresh, sizes, slots);
	}

	// All of them on this page, or none.
	private bool TryPlaceAll(Page page, (int W, int H)[] sizes, Slot[] slots)
	{
		for (var i = 0; i < sizes.Length; i++)
		{
			if (TryTake(page, sizes[i].W, sizes[i].H, sizes[i].W, sizes[i].H) is { } slot)
			{
				slots[i] = slot;
				continue;
			}
			for (var j = 0; j < i; j++)
			{
				Release(page, slots[j]);
				slots[j] = null!;
			}
			return false;
		}
		return true;
	}

	// A region on the pool's pages, opening another page when they are all full.
	private Slot? Reserve(Pool pool, int regionW, int regionH, int w, int h)
	{
		foreach (var page in pool.Pages)
		{
			if (TryTake(page, regionW, regionH, w, h) is { } slot) { return slot; }
		}
		return AddPage(pool) is { } fresh ? TryTake(fresh, regionW, regionH, w, h) : null;
	}

	private static Slot? TryTake(Page page, int regionW, int regionH, int w, int h)
	{
		if (!page.Allocator.TryAllocate(regionW, regionH, out var shelf, out var x, out var regionX, out var allocatedW)) { return null; }
		page.Live++;
		page.EmptySince = -1;
		return new Slot(x, shelf.Y, w, h, 0, 0) { Owner = page, Shelf = shelf, RegionX = regionX, RegionW = allocatedW, RefCount = 1 };
	}

	// Gives a region back to its page's allocator. A page left empty is kept for a while (see BeginFrame).
	private void Release(Page page, Slot slot)
	{
		page.Allocator.Free(slot.Shelf!, slot.RegionX, slot.RegionW);
		if (--page.Live == 0) { page.EmptySince = _frame; }
	}

	private Page? AddPage(Pool pool)
	{
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = (uint)pool.PageSize, Height = (uint)pool.PageSize, DepthOrArrayLayers = 1 },
			Format = pool.Format,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = pool.Usage,
		};
		var tex = _d.Api.DeviceCreateTexture(_d.Dev, &td);
		if (tex == null) { return null; }
		var page = new Page
		{
			Texture = tex,
			View = _d.Api.TextureCreateView(tex, null),
			W = pool.PageSize,
			H = pool.PageSize,
			Format = pool.Format,
			Usage = pool.Usage,
			Allocator = new ShelfAllocator(pool.PageSize, pool.PageSize),
		};
		pool.Pages.Add(page);
		_pages.Add(page);
		return page;
	}

	/// <summary>Frames an entry may go unused before the cache drops its reference.</summary>
	public const int CacheIdleFrames = 180;

	/// <summary>An entry on a large page holds a lot of texture, so once it goes idle it is let go soon.</summary>
	public const int LargeIdleFrames = 30;

	/// <summary>An entry used in a single frame (moving or animated content) is let go almost at once.</summary>
	public const int OnceIdleFrames = 2;

	private readonly List<Slot> _cacheHeld = new();

	/// <summary>Records that an entry was used this frame, so the idle sweep keeps it.</summary>
	public void NoteUse(Slot slot, long frame)
	{
		if (slot is null || slot.LastUsed == frame) { return; }
		slot.LastUsed = frame;
		slot.Uses++;
	}

	// The cache's reference to an entry; the idle sweep drops it.
	private void HoldForCache(Slot slot, long frame)
	{
		slot.CacheHeld = true;
		slot.LastUsed = frame;
		_cacheHeld.Add(slot);
	}

	/// <summary>Takes an additional reference for a recording that reuses an entry it did not bake.</summary>
	public void Retain(Slot slot)
	{
		if (slot is not null) { slot.RefCount++; }
	}

	/// <summary>
	/// Drops one reference; the last returns the region. Only at a frame boundary: a recording bakes its slot's UVs into
	/// its draw ops, so a region reclaimed while any recording still referenced it would draw another shape's mask.
	/// </summary>
	public void Free(Slot slot)
	{
		if (slot?.Owner is not { } page) { return; }
		if (--slot.RefCount > 0) { return; }
		if (slot.Keyed && _slots.TryGetValue(slot.Key, out var cur) && ReferenceEquals(cur, slot)) { _slots.Remove(slot.Key); }
		Release(page, slot);
	}

	/// <summary>
	/// Frame start: drops the cache's idle references, and destroys the pages that stayed empty, keeping one regular page.
	/// </summary>
	public void BeginFrame(long frame)
	{
		_frame = frame;
		SweepCache(frame);

		foreach (var pool in _pools)
		{
			var keep = pool.Format == _d.ColorFormat && pool.Usage == MaskPageUsage && pool.PageSize == Size ? 1 : 0;
			for (var i = pool.Pages.Count - 1; i >= 0 && pool.Pages.Count > keep; i--)
			{
				var p = pool.Pages[i];
				if (p.EmptySince < 0 || frame - p.EmptySince <= EmptyPageIdleFrames) { continue; }
				pool.Pages.RemoveAt(i);
				_pages.Remove(p);
				Destroy(p);
			}
		}
	}

	private void SweepCache(long frame)
	{
		for (var i = _cacheHeld.Count - 1; i >= 0; i--)
		{
			var slot = _cacheHeld[i];
			if (!slot.CacheHeld) { _cacheHeld.RemoveAt(i); continue; }
			var idle = slot.Uses <= 1 ? OnceIdleFrames : slot.Owner is { W: > Size } ? LargeIdleFrames : CacheIdleFrames;
			if (frame - slot.LastUsed <= idle) { continue; }
			slot.CacheHeld = false;
			_cacheHeld.RemoveAt(i);
			// A recording that also uses the entry holds its own reference, so this reclaims only when the cache was the last holder.
			Free(slot);
		}
	}

	private void Destroy(Page p)
	{
		_d.Groups.NoteDestroyed((nint)p.View);
		if (p.View != null) { _d.Api.TextureViewRelease(p.View); }
		if (p.Texture != null) { _d.Api.TextureRelease(p.Texture); }
	}

	public void Dispose()
	{
		foreach (var p in _pages) { Destroy(p); }
		_pages.Clear();
		_pools.Clear();
	}

	/// <summary>
	/// The key of a mask baked as given: the outline at <paramref name="scale"/>, shifted by <paramref name="shift"/>
	/// pixels, in a w x h slot. For masks <see cref="TryKey"/> refuses; it can never equal one of its keys.
	/// </summary>
	public static Key ExactKey(long shape, Vector2 scale, Vector2 shift, int w, int h, bool evenOdd)
		=> new(shape, int.MinValue, BitConverter.SingleToInt32Bits(scale.X), BitConverter.SingleToInt32Bits(scale.Y), int.MinValue,
			BitConverter.SingleToInt32Bits(shift.X), BitConverter.SingleToInt32Bits(shift.Y), w, h, evenOdd ? 2 : 1);

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
	public static bool TryKey(long shape, in Matrix4x4 matrix, Vector2 bbMin, Vector2 bbMax, Vector2 scale, out Key key, out int w, out int h, out float originX, out float originY, bool allowBig = false, int extra = 0, Vector2 place = default)
		=> TryKey(shape, matrix, bbMin, bbMax, scale, out key, out w, out h, out originX, out originY, out _, allowBig, extra, place, phases: 0);

	/// <param name="phases">Subpixel phases per axis the key snaps to, or 0 for the exact phase.</param>
	/// <param name="snap">The device-pixel shift from the shape's true phase to the one it is keyed at: the bake
	/// rasterizes at the snapped phase, so an entry is the same whichever of its sharers baked it.</param>
	public static bool TryKey(long shape, in Matrix4x4 matrix, Vector2 bbMin, Vector2 bbMax, Vector2 scale, out Key key, out int w, out int h, out float originX, out float originY, out Vector2 snap, bool allowBig = false, int extra = 0, Vector2 place = default, int phases = 0)
	{
		key = default;
		w = h = 0;
		originX = originY = 0;
		snap = default;
		if (shape == 0 || scale.X <= 0 || scale.Y <= 0) { return false; }

		var dw = (bbMax.X - bbMin.X) * scale.X;
		var dh = (bbMax.Y - bbMin.Y) * scale.Y;
		if (dw <= 0 || dh <= 0) { return false; }
		// A big entry goes on a large page, capped at the largest a texture may be; a regular one must fit a shelf.
		if (allowBig ? (dw > 4094 || dh > 4094) : (dw > MaxDim || dh > MaxDim || dw * dh > MaxArea))
		{ RejBig++; return false; }

		// Snap the slot origin to whole DEVICE pixels and let the mask absorb the fractional offset. Placing the
		// quad at a fractional position instead makes the sampler resample a 1:1 mask, which visibly blurs and
		// fattens glyphs — the phase belongs in the baked mask (that is what the phase key is for).
		// The phase must be the one the mask is DRAWN at, so the placement translation belongs here: a recording
		// replayed at a fractional offset would otherwise bake the phase of its own space and land half a pixel out.
		var devMinX = bbMin.X * scale.X + place.X;
		var devMinY = bbMin.Y * scale.Y + place.Y;
		var oxDev = MathF.Floor(devMinX);
		var oyDev = MathF.Floor(devMinY);
		originX = (oxDev - place.X) / scale.X;
		originY = (oyDev - place.Y) / scale.Y;

		int phaseX, phaseY;
		if (phases > 0)
		{
			// Snapped: the entry is baked at the snapped phase itself, so its sharers draw within half a step of true.
			// Negative so a snapped key can never equal an exact one.
			var qx = (int)MathF.Round((devMinX - oxDev) * phases);
			var qy = (int)MathF.Round((devMinY - oyDev) * phases);
			snap = new Vector2((float)qx / phases - (devMinX - oxDev), (float)qy / phases - (devMinY - oyDev));
			phaseX = -1 - qx;
			phaseY = -1 - qy;
		}
		else
		{
			// The subpixel phase on both axes, bit for bit: entries are keyed by outline content, so a shape shares an
			// entry with any other drawn at the same fraction, and only then. Any coarser and two shapes a fraction of a
			// pixel apart share one mask that one of them draws misplaced, depending on which baked first.
			phaseX = BitConverter.SingleToInt32Bits(devMinX - oxDev);
			phaseY = BitConverter.SingleToInt32Bits(devMinY - oyDev);
		}

		// A one-texel skirt keeps bilinear sampling from bleeding a neighbouring slot into the edge. Sized from the
		// snapped outline, which is what the bake rasterizes.
		w = (int)MathF.Ceiling(bbMax.X * scale.X + place.X + snap.X - oxDev) + 2;
		h = (int)MathF.Ceiling(bbMax.Y * scale.Y + place.Y + snap.Y - oyDev) + 2;
		key = new Key(
			shape,
			(int)MathF.Round(matrix.M11 * scale.X * 64f),
			(int)MathF.Round(matrix.M12 * scale.X * 64f),
			(int)MathF.Round(matrix.M21 * scale.Y * 64f),
			(int)MathF.Round(matrix.M22 * scale.Y * 64f),
			phaseX,
			phaseY,
			w,
			h,
			extra);
		return true;
	}
}

/// <summary>
/// Packs rectangles on one page in shelves (rows) whose heights are rounded to a few classes, so masks of similar
/// height share a shelf. Unlike a shelf packer that only appends, it gives space back: a freed range merges with its
/// free neighbours on its shelf, a shelf left empty merges with the empty shelves next to it and can be re-cut for
/// another height, and empty shelves at the bottom return to the page. Masks that come and go every frame at slightly
/// different sizes therefore reuse the same space instead of exhausting the page (after the étagère allocator).
/// </summary>
internal sealed class ShelfAllocator
{
	internal sealed class Shelf
	{
		public int Y, H;
		public int Used;
		// Free x ranges, sorted and never adjacent: adjacent ranges are merged as they are freed.
		public readonly List<(int X, int W)> Free = new();
	}

	// Widths round up to this, so freed ranges line up and slivers too narrow for anything are not left behind.
	private const int WidthAlign = 4;

	private readonly int _w, _h;
	// Sorted by Y; together with the free space below the last one they tile the page.
	private readonly List<Shelf> _shelves = new();
	private int _bottom;

	public ShelfAllocator(int w, int h)
	{
		_w = w;
		_h = h;
	}

	// Height classes: fine for the small masks most entries are, coarser as they grow, so a shelf wastes at most ~12%
	// of its height on any entry it takes.
	private static int HeightClass(int h)
		=> h <= 8 ? 8
			: h <= 64 ? (h + 7) & ~7
			: h <= 256 ? (h + 15) & ~15
			: (h + 31) & ~31;

	public bool TryAllocate(int w, int h, out Shelf shelf, out int x, out int regionX, out int regionW)
	{
		shelf = null!;
		x = regionX = regionW = 0;
		if (w <= 0 || h <= 0 || w > _w || h > _h) { return false; }
		var aw = Math.Min(_w, (w + WidthAlign - 1) & ~(WidthAlign - 1));
		var hc = Math.Min(_h, HeightClass(h));

		// A shelf of the right height with room: the tightest height first, then the narrowest range that fits.
		Shelf? best = null;
		int bestIndex = -1;
		for (var i = 0; i < _shelves.Count; i++)
		{
			var s = _shelves[i];
			if (s.Used == 0 || s.H < hc || s.H > hc + hc / 2) { continue; }
			var r = BestRange(s, aw);
			if (r < 0) { continue; }
			if (best is null || s.H < best.H || (s.H == best.H && s.Free[r].W < best.Free[bestIndex].W))
			{
				best = s;
				bestIndex = r;
			}
		}
		if (best is null)
		{
			// An empty shelf tall enough, cut down to the height needed (the rest stays an empty shelf), or a new one.
			best = TakeEmptyShelf(hc) ?? NewShelf(hc);
			if (best is null) { return false; }
			bestIndex = 0;
		}

		var (fx, fw) = best.Free[bestIndex];
		if (fw == aw) { best.Free.RemoveAt(bestIndex); }
		else { best.Free[bestIndex] = (fx + aw, fw - aw); }
		best.Used++;
		shelf = best;
		x = regionX = fx;
		regionW = aw;
		return true;
	}

	private static int BestRange(Shelf s, int w)
	{
		var best = -1;
		for (var i = 0; i < s.Free.Count; i++)
		{
			if (s.Free[i].W >= w && (best < 0 || s.Free[i].W < s.Free[best].W)) { best = i; }
		}
		return best;
	}

	private Shelf? TakeEmptyShelf(int hc)
	{
		Shelf? best = null;
		foreach (var s in _shelves)
		{
			if (s.Used == 0 && s.H >= hc && (best is null || s.H < best.H)) { best = s; }
		}
		if (best is null) { return null; }
		if (best.H - hc >= 8)
		{
			var rest = new Shelf { Y = best.Y + hc, H = best.H - hc };
			rest.Free.Add((0, _w));
			_shelves.Insert(_shelves.IndexOf(best) + 1, rest);
			best.H = hc;
		}
		return best;
	}

	private Shelf? NewShelf(int hc)
	{
		if (_bottom + hc > _h) { return null; }
		var s = new Shelf { Y = _bottom, H = hc };
		s.Free.Add((0, _w));
		_shelves.Add(s);
		_bottom += hc;
		return s;
	}

	public void Free(Shelf shelf, int x, int w)
	{
		// Insert the range in order and merge it with the free ranges it touches.
		var list = shelf.Free;
		var i = 0;
		while (i < list.Count && list[i].X < x) { i++; }
		list.Insert(i, (x, w));
		if (i + 1 < list.Count && list[i].X + list[i].W == list[i + 1].X)
		{
			list[i] = (list[i].X, list[i].W + list[i + 1].W);
			list.RemoveAt(i + 1);
		}
		if (i > 0 && list[i - 1].X + list[i - 1].W == list[i].X)
		{
			list[i - 1] = (list[i - 1].X, list[i - 1].W + list[i].W);
			list.RemoveAt(i);
		}

		if (--shelf.Used > 0) { return; }

		// An empty shelf merges with the empty shelves around it, and empty shelves at the bottom go back to the page.
		var index = _shelves.IndexOf(shelf);
		while (index + 1 < _shelves.Count && _shelves[index + 1].Used == 0)
		{
			shelf.H += _shelves[index + 1].H;
			_shelves.RemoveAt(index + 1);
		}
		while (index > 0 && _shelves[index - 1].Used == 0)
		{
			_shelves[index - 1].H += shelf.H;
			_shelves.RemoveAt(index);
			index--;
			shelf = _shelves[index];
		}
		shelf.Free.Clear();
		shelf.Free.Add((0, _w));
		if (index == _shelves.Count - 1)
		{
			_bottom = shelf.Y;
			_shelves.RemoveAt(index);
		}
	}
}
