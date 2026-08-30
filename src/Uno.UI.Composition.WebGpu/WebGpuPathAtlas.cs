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

	internal readonly record struct Key(object Geometry, int ScaleX, int ScaleY, int PhaseX, int PhaseY);

	/// <summary>
	/// A reserved region. Origin is the shape's device-space bbox corner the entry was rasterized against, which
	/// the draw needs to place the quad back.
	/// </summary>
	internal sealed record Slot(int X, int Y, int W, int H, float OriginX, float OriginY);

	private readonly Dictionary<Key, Slot> _slots = new();
	private int _shelfY, _shelfH, _cursorX;
	private bool _full;

	public IntPtr Texture { get; private set; }

	public IntPtr View { get; private set; }

	public bool IsFull => _full;

	public void SetTexture(IntPtr texture, IntPtr view)
	{
		Texture = texture;
		View = view;
	}

	public bool TryGet(in Key key, out Slot slot) => _slots.TryGetValue(key, out slot!);

	/// <summary>Reserves a w x h region, or returns null when the page cannot fit it.</summary>
	public Slot? Allocate(in Key key, int w, int h, float originX, float originY)
	{
		if (_full || w <= 0 || h <= 0 || w > Size || h > Size) { return null; }

		// Shelf packing: fill a row left to right, then start a new row below it. Cheap, and glyphs from one
		// font arrive at similar heights, so the waste stays small.
		if (_cursorX + w > Size)
		{
			_shelfY += _shelfH;
			_shelfH = 0;
			_cursorX = 0;
		}
		if (_shelfY + h > Size)
		{
			_full = true;
			return null;
		}

		var slot = new Slot(_cursorX, _shelfY, w, h, originX, originY);
		_cursorX += w;
		if (h > _shelfH) { _shelfH = h; }
		_slots[key] = slot;
		return slot;
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
			Math.Clamp(phaseY, 0, SubPixel - 1));
		return true;
	}
}
