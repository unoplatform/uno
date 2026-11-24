#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Silk.NET.WebGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// The device's one store for coverage masks: glyph and fill entries cached under a key, masks owned by a cached
/// recording, masks drawn for one frame only, and blurred shadows. Entries differ only in who frees their slot: a
/// recording when it is released (<see cref="Free"/>), the next frame start (<see cref="FreeNextFrame"/>), or the
/// idle sweep (<see cref="HoldForCache"/>). Small entries are shelf-packed on shared pages; an entry too big for a
/// shelf gets a standalone page, kept as a spare for a while once freed so the next entry of its size reuses it.
/// </summary>
internal sealed unsafe class WebGpuMaskAtlas
{
	/// <summary>Shared page edge in texels.</summary>
	public const int Size = 1024;

	/// <summary>Per-axis bound: a slot cannot span pages, so nothing wider or taller than a page can ever be placed.</summary>
	public const int MaxDim = Size - 2;

	/// <summary>
	/// Footprint bound for a keyed entry, in AREA: text arrives as one fill per run, very wide and ~20px tall, so a
	/// longest-side cap rejected every run past a few characters.
	/// </summary>
	public const int MaxArea = 256 * 256;

	/// <summary>
	/// Keyed pages beyond which a keyed entry is refused rather than opening another. Keyed entries are the cache: a
	/// transform that animates mints a fresh key every frame, so without a ceiling the cache grows without bound.
	/// </summary>
	public const int MaxPages = 8;

	/// <summary>Keyless pages beyond which a keyless mask takes a standalone page instead; its owner bounds it, not the cache.</summary>
	public const int MaxSharedPages = 32;

	/// <summary>Frames an empty shared page, or a spare standalone one, is kept before its texture is destroyed.</summary>
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

	/// <summary>Too big for a shelf on a shared page: such an entry gets a standalone page.</summary>
	public static bool IsBig(int w, int h) => w > MaxDim || h > MaxDim || w * h > MaxArea;

	/// <summary>A reserved region. Origin is the device-space corner the entry was rasterized against.</summary>
	internal sealed record Slot(int X, int Y, int W, int H, float OriginX, float OriginY)
	{
		/// <summary>The cache key, when <see cref="Keyed"/>.</summary>
		public Key Key { get; init; }

		public bool Keyed { get; init; }

		/// <summary>The page this slot lives on; the draw samples this page's view.</summary>
		public Page? Owner { get; init; }

		/// <summary>Holders of this slot. One glyph entry backs every occurrence across many recordings, so only the last may reclaim it.</summary>
		public int RefCount;

		/// <summary>Frame this entry was last used, for the cache's own reference (see <see cref="HoldForCache"/>).</summary>
		public long LastUsed;

		/// <summary>True while the cache holds one of this slot's references.</summary>
		public bool CacheHeld;

		/// <summary>The part of the page a draw of this entry samples; a blurred shadow stored reduced covers less than its slot.</summary>
		public Vector4 Uv = new(0f, 0f, 1f, 1f);
	}

	/// <summary>
	/// One texture. A live slot's UVs are baked into its users' draw ops, so a slot never moves: a full page means
	/// opening another, and a page is only reset once it holds no live slot.
	/// </summary>
	internal sealed class Page
	{
		public Texture* Texture;
		public TextureView* View;
		public int W = Size, H = Size;
		public TextureFormat Format;
		public TextureUsage Usage;
		// Sized to its entries and never shelf-packed (see StandalonePage).
		public bool Standalone;
		// A shared page holds keyless masks only, or keyed entries only: short-lived masks neither use up the cache's
		// pages nor leave shelf space behind on pages its long-lived entries keep from ever emptying.
		public bool Keyless;
		public int ShelfY, ShelfH, CursorX;
		public int Live;
		// Frame the page last became empty (shared) or spare (standalone); -1 while it holds slots.
		public long EmptySince = -1;
		public readonly Dictionary<(int W, int H), Stack<Slot>> Free = new();

		public void Reset()
		{
			ShelfY = ShelfH = CursorX = 0;
			Free.Clear();
		}
	}

	private const TextureUsage SharedUsage = TextureUsage.RenderAttachment | TextureUsage.TextureBinding | TextureUsage.CopyDst;

	private readonly WebGpuDevice _d;
	private readonly Dictionary<Key, Slot> _slots = new();
	private readonly List<Page> _pages = new();
	private readonly List<Page> _spares = new();
	private readonly List<Slot> _frameSlots = new();
	private long _frame;

	public WebGpuMaskAtlas(WebGpuDevice d) => _d = d;

	public IReadOnlyList<Page> Pages => _pages;

	/// <summary>Shared (shelf-packed) pages.</summary>
	public int RegularPages
	{
		get
		{
			var n = 0;
			for (var i = 0; i < _pages.Count; i++) { if (!_pages[i].Standalone) { n++; } }
			return n;
		}
	}

	private int SharedPages(bool keyless)
	{
		var n = 0;
		for (var i = 0; i < _pages.Count; i++) { if (IsShared(_pages[i], keyless)) { n++; } }
		return n;
	}

	private static bool IsShared(Page page, bool keyless) => !page.Standalone && page.Keyless == keyless;

	public bool TryGet(in Key key, out Slot slot) => _slots.TryGetValue(key, out slot!);

	/// <summary>Reserves a w x h region for a keyed entry, or null when every page is full and the cache is at its ceiling.</summary>
	public Slot? Allocate(in Key key, int w, int h, float originX, float originY)
	{
		if (w <= 0 || h <= 0 || w > Size || h > Size) { return null; }
		if (Reserve(w, h, keyless: false) is not { } slot) { return null; }
		slot = slot with { OriginX = originX, OriginY = originY, Key = key, Keyed = true };
		_slots[key] = slot;
		return slot;
	}

	/// <summary>
	/// Reserves a region of at least w x h on a shared page for a mask nobody looks up, or null when it is bigger than
	/// half a page (take <see cref="StandalonePage"/> instead). Sized up to a power of two, so freed regions recur.
	/// </summary>
	public Slot? AllocateKeyless(int w, int h)
	{
		int cw = SizeClass(w), ch = SizeClass(h);
		if (cw > Size / 2 || ch > Size / 2) { return null; }
		return Reserve(cw, ch, keyless: true);
	}

	/// <summary>
	/// Reserves regions for all of <paramref name="sizes"/> on ONE shared page, since a clip's masks bind as one
	/// texture; false when they do not fit one page together.
	/// </summary>
	public bool AllocateKeylessGroup((int W, int H)[] sizes, Slot[] slots)
	{
		for (var i = 0; i < sizes.Length; i++)
		{
			if (SizeClass(sizes[i].W) > Size / 2 || SizeClass(sizes[i].H) > Size / 2) { return false; }
		}
		for (var pass = 0; pass < 2; pass++)
		{
			for (var p = 0; p < _pages.Count; p++)
			{
				if (IsShared(_pages[p], keyless: true) && TryPlaceAll(_pages[p], sizes, slots)) { return true; }
			}
			if (pass == 0 && (SharedPages(keyless: true) >= MaxSharedPages || !AddSharedPage(keyless: true))) { return false; }
		}
		return false;
	}

	private static int SizeClass(int n) => Math.Max(32, (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, n)));

	private bool TryPlaceAll(Page page, (int W, int H)[] sizes, Slot[] slots)
	{
		int shelfY = page.ShelfY, shelfH = page.ShelfH, cursorX = page.CursorX;
		var placed = 0;
		var reused = new bool[sizes.Length];
		for (; placed < sizes.Length; placed++)
		{
			int w = SizeClass(sizes[placed].W), h = SizeClass(sizes[placed].H);
			if (TryReuse(page, w, h) is { } r) { slots[placed] = r; reused[placed] = true; continue; }
			if (TryShelf(page, w, h) is not { } s) { break; }
			slots[placed] = s;
		}
		if (placed == sizes.Length) { return true; }
		// All or nothing: reused regions go back to their buckets, fresh ones by rewinding the shelf.
		for (var i = 0; i < placed; i++)
		{
			page.Live--;
			if (reused[i]) { PushFree(page, slots[i]); }
			slots[i] = null!;
		}
		page.ShelfY = shelfY; page.ShelfH = shelfH; page.CursorX = cursorX;
		return false;
	}

	private Slot? Reserve(int w, int h, bool keyless)
	{
		for (var pass = 0; pass < 2; pass++)
		{
			// A freed region of the same size first, on any page of this kind, before consuming new space.
			for (var i = 0; i < _pages.Count; i++)
			{
				if (IsShared(_pages[i], keyless) && TryReuse(_pages[i], w, h) is { } reused) { return reused; }
			}
			for (var i = 0; i < _pages.Count; i++)
			{
				if (IsShared(_pages[i], keyless) && TryShelf(_pages[i], w, h) is { } fresh) { return fresh; }
			}
			if (pass == 0 && (SharedPages(keyless) >= (keyless ? MaxSharedPages : MaxPages) || !AddSharedPage(keyless))) { return null; }
		}
		return null;
	}

	private static Slot? TryReuse(Page page, int w, int h)
	{
		if (!page.Free.TryGetValue((w, h), out var bucket) || bucket.Count == 0) { return null; }
		return Take(page, bucket.Pop() with { Key = default, Keyed = false, OriginX = 0, OriginY = 0 });
	}

	// Shelf packing: fill a row left to right, then start a new row below it.
	private static Slot? TryShelf(Page page, int w, int h)
	{
		int shelfY = page.ShelfY, shelfH = page.ShelfH, cursorX = page.CursorX;
		if (cursorX + w > page.W) { shelfY += shelfH; shelfH = 0; cursorX = 0; }
		if (w > page.W || shelfY + h > page.H) { return null; }
		page.ShelfY = shelfY; page.CursorX = cursorX + w; page.ShelfH = Math.Max(shelfH, h);
		return Take(page, new Slot(cursorX, shelfY, w, h, 0, 0) { Owner = page });
	}

	private static Slot Take(Page page, Slot slot)
	{
		slot.RefCount = 1;
		slot.CacheHeld = false;
		slot.LastUsed = 0;
		slot.Uv = new(0f, 0f, 1f, 1f);
		page.Live++;
		page.EmptySince = -1;
		return slot;
	}

	private static void PushFree(Page page, Slot slot)
	{
		if (!page.Free.TryGetValue((slot.W, slot.H), out var bucket)) { page.Free[(slot.W, slot.H)] = bucket = new Stack<Slot>(); }
		bucket.Push(slot);
	}

	private bool AddSharedPage(bool keyless)
	{
		if (NewPage(Size, Size, _d.ColorFormat, SharedUsage) is not { } page) { return false; }
		page.Keyless = keyless;
		_pages.Add(page);
		return true;
	}

	private Page? NewPage(int w, int h, TextureFormat format, TextureUsage usage)
	{
		var td = new TextureDescriptor
		{
			Size = new Extent3D { Width = (uint)w, Height = (uint)h, DepthOrArrayLayers = 1 },
			Format = format,
			MipLevelCount = 1,
			SampleCount = 1,
			Dimension = TextureDimension.Dimension2D,
			Usage = usage,
		};
		var tex = _d.Api.DeviceCreateTexture(_d.Dev, &td);
		if (tex == null) { return null; }
		return new Page { Texture = tex, View = _d.Api.TextureCreateView(tex, null), W = w, H = h, Format = format, Usage = usage };
	}

	/// <summary>
	/// A page of its own, at least w x h, for entries too big to share one: a spare of the same format when one is close
	/// enough in size, else a new texture rounded up so sizes recur. Its entries go on it with <see cref="Place"/>.
	/// Null when no texture could be created (out of GPU memory, or the device was lost): the caller skips the mask.
	/// </summary>
	public Page? StandalonePage(int w, int h, TextureFormat format, TextureUsage usage)
	{
		int rw = Math.Max(w, Math.Min(4096, (w + 63) & ~63)), rh = Math.Max(h, Math.Min(4096, (h + 63) & ~63));
		Page? best = null;
		foreach (var sp in _spares)
		{
			if (sp.Format != format || sp.Usage != usage || sp.W < w || sp.H < h || (long)sp.W * sp.H > 2L * rw * rh) { continue; }
			if (best is null || (long)sp.W * sp.H < (long)best.W * best.H) { best = sp; }
		}
		if (best is not null) { _spares.Remove(best); }
		if ((best ?? NewPage(rw, rh, format, usage)) is not { } page) { return null; }
		page.Standalone = true;
		page.EmptySince = -1;
		page.Reset();
		_pages.Add(page);
		return page;
	}

	/// <summary>A slot at (x, y) on a standalone page, keyed when <paramref name="key"/> is given.</summary>
	public Slot Place(Page page, int x, int y, int w, int h, float originX = 0f, float originY = 0f, Key? key = null)
	{
		var slot = Take(page, new Slot(x, y, w, h, originX, originY) { Owner = page, Key = key ?? default, Keyed = key.HasValue });
		if (key is { } k) { _slots[k] = slot; }
		return slot;
	}

	/// <summary>Frames an entry may go unused before the cache drops its reference.</summary>
	public const int CacheIdleFrames = 180;

	/// <summary>A standalone cache entry is a whole texture, so once it stops recurring it is let go soon.</summary>
	public const int StandaloneIdleFrames = 30;

	private readonly List<Slot> _cacheHeld = new();
	private readonly List<Key> _stale = new();

	private readonly Dictionary<Key, (long Frame, int Run)> _requests = new();

	/// <summary>Consecutive frames a per-frame op's key must recur before the cache admits it.</summary>
	public const int AdmitRun = 8;

	/// <summary>
	/// Whether a per-frame op's key has recurred for <see cref="AdmitRun"/> consecutive frames: static content pays a
	/// few per-frame bakes at first sight, moving content never enters the cache.
	/// </summary>
	public bool Recurring(in Key key, long frame)
	{
		var run = _requests.TryGetValue(key, out var last) && last.Frame == frame - 1 ? last.Run + 1 : 1;
		_requests[key] = (frame, run);
		return run >= AdmitRun;
	}

	/// <summary>Records that an entry was used this frame, so the idle sweep keeps it.</summary>
	public void NoteUse(Slot slot, long frame)
	{
		if (slot is not null) { slot.LastUsed = frame; }
	}

	/// <summary>Gives the cache a reference to an entry baked for a per-frame op; the idle sweep drops it.</summary>
	public void HoldForCache(Slot slot, long frame)
	{
		if (slot is null || slot.CacheHeld) { return; }
		slot.CacheHeld = true;
		slot.LastUsed = frame;
		_cacheHeld.Add(slot);
	}

	/// <summary>Frees a slot a per-frame op used, at the next frame start.</summary>
	public void FreeNextFrame(Slot slot)
	{
		lock (_frameSlots) { _frameSlots.Add(slot); }
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
		if (!page.Standalone) { PushFree(page, slot); }
		if (--page.Live > 0) { return; }

		page.EmptySince = _frame;
		if (page.Standalone)
		{
			_pages.Remove(page);
			_spares.Add(page);
		}
		else
		{
			page.Reset();
		}
	}

	/// <summary>
	/// Frame start: frees last frame's per-frame slots, drops the cache's idle references, and destroys the pages that
	/// stayed empty (keeping one shared page) and the spares nothing reused.
	/// </summary>
	public void BeginFrame(long frame)
	{
		_frame = frame;
		lock (_frameSlots)
		{
			foreach (var s in _frameSlots) { Free(s); }
			_frameSlots.Clear();
		}
		SweepCache(frame);

		for (var i = _pages.Count - 1; i >= 0 && RegularPages > 1; i--)
		{
			var p = _pages[i];
			if (p.Standalone || p.EmptySince < 0 || frame - p.EmptySince <= EmptyPageIdleFrames) { continue; }
			_pages.RemoveAt(i);
			Destroy(p);
		}
		for (var i = _spares.Count - 1; i >= 0; i--)
		{
			if (frame - _spares[i].EmptySince <= EmptyPageIdleFrames) { continue; }
			Destroy(_spares[i]);
			_spares.RemoveAt(i);
		}
	}

	private void SweepCache(long frame)
	{
		if ((frame & 63) == 0)
		{
			foreach (var kv in _requests) { if (kv.Value.Frame < frame - 2) { _stale.Add(kv.Key); } }
			foreach (var k in _stale) { _requests.Remove(k); }
			_stale.Clear();
		}
		for (var i = _cacheHeld.Count - 1; i >= 0; i--)
		{
			var slot = _cacheHeld[i];
			if (!slot.CacheHeld) { _cacheHeld.RemoveAt(i); continue; }
			if (frame - slot.LastUsed <= (slot.Owner is { Standalone: true } ? StandaloneIdleFrames : CacheIdleFrames)) { continue; }
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
		foreach (var p in _spares) { Destroy(p); }
		_pages.Clear();
		_spares.Clear();
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
		// A big entry gets its own texture, capped at the largest a texture may be; a shared-page entry must fit a shelf.
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
