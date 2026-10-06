#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

/// <summary>
/// Every distinct character rastered once into a single image, so a figure of thousands of letters
/// is a handful of textured draws instead of thousands of text calls. The page and the figure both
/// sample this atlas, which is what lets a letter leave the column and arrive unchanged.
/// </summary>
internal sealed class GlyphAtlas : IDisposable
{
	private readonly Dictionary<char, int> _slots = new();

	private GlyphAtlas(SKImage image, SKRect[] bounds, Dictionary<char, int> slots, float cellSize)
	{
		Image = image;
		Bounds = bounds;
		_slots = slots;
		CellSize = cellSize;
	}

	internal SKImage Image { get; }

	/// <summary>Source rect in atlas pixels, indexed by slot.</summary>
	internal SKRect[] Bounds { get; }

	internal float CellSize { get; }

	internal int SlotFor(char c) => _slots.TryGetValue(c, out var slot) ? slot : -1;

	internal static GlyphAtlas Create(string alphabet, SKTypeface typeface, float cellSize)
	{
		var distinct = new List<char>();
		var slots = new Dictionary<char, int>();
		foreach (var c in alphabet)
		{
			if (!slots.ContainsKey(c))
			{
				slots[c] = distinct.Count;
				distinct.Add(c);
			}
		}

		var columns = (int)Math.Ceiling(Math.Sqrt(distinct.Count));
		var rows = (int)Math.Ceiling(distinct.Count / (double)columns);
		var width = (int)(columns * cellSize);
		var height = (int)(rows * cellSize);

		var info = new SKImageInfo(Math.Max(1, width), Math.Max(1, height), SKColorType.Rgba8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		var canvas = surface.Canvas;
		canvas.Clear(SKColors.Transparent);

		using var font = new SKFont(typeface, cellSize * 0.95f) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
		using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true };

		var bounds = new SKRect[distinct.Count];
		for (var i = 0; i < distinct.Count; i++)
		{
			var col = i % columns;
			var row = i / columns;
			var originX = col * cellSize;
			var originY = row * cellSize;

			// Centred in its cell, baseline placed from the font metrics so every glyph shares one
			// vertical reference - the figure tilts letters, and a per-glyph baseline would wobble.
			var metrics = font.Metrics;
			var baseline = originY + (cellSize - (metrics.Descent - metrics.Ascent)) * 0.5f - metrics.Ascent;
			var text = distinct[i].ToString();
			canvas.DrawText(text, originX + cellSize * 0.5f, baseline, SKTextAlign.Center, font, paint);

			bounds[i] = new SKRect(originX, originY, originX + cellSize, originY + cellSize);
		}

		return new GlyphAtlas(surface.Snapshot(), bounds, slots, cellSize);
	}

	public void Dispose() => Image.Dispose();
}
