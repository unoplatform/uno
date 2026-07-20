#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IFont"/> wrapping an <see cref="SKFont"/>.</summary>
internal sealed class SkiaFont : IFont
{
	private static readonly uint _colrTag = Tag("COLR");
	private static readonly uint _cbdtTag = Tag("CBDT");
	private static readonly uint _sbixTag = Tag("sbix");
	private static readonly uint _svgTag = Tag("SVG ");

	private readonly SKFont _font;

	public SkiaFont(SKFont font)
	{
		_font = font;
		HasColorGlyphs = HasColorTables(font.Typeface);
	}

	public bool HasColorGlyphs { get; }

	public IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY)
	{
		var builder = new SKPathBuilder();

		for (var i = 0; i < glyphs.Length; i++)
		{
			using var glyphPath = _font.GetGlyphPath(glyphs[i]);
			if (glyphPath is { IsEmpty: false })
			{
				glyphPath.Transform(SKMatrix.CreateTranslation(positions[i].X, positions[i].Y + baselineY));
				builder.AddPath(glyphPath, SKPathAddMode.Append);
			}
		}

		return new SkiaGeometrySource2D(builder.Detach());
	}

	public void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output)
	{
		if (!HasColorGlyphs)
		{
			return;
		}

		for (var i = 0; i < glyphs.Length; i++)
		{
			using (var glyphPath = _font.GetGlyphPath(glyphs[i]))
			{
				if (glyphPath is { IsEmpty: false })
				{
					// Outline glyph — already covered by BuildGlyphRunOutline.
					continue;
				}
			}

			if (TryRasterizeColorGlyph(glyphs[i], positions[i].X, positions[i].Y + baselineY, out var image))
			{
				output.Add(image);
			}
		}
	}

	private bool TryRasterizeColorGlyph(ushort glyph, float originX, float originY, out PositionedGlyphImage result)
	{
		result = default;

		using var builder = new SKTextBlobBuilder();
		builder.AddPositionedRun(new[] { glyph }, _font, new[] { new SKPoint(0, 0) });
		using var blob = builder.Build();
		if (blob is null)
		{
			return false;
		}

		var ink = blob.Bounds; // ink bounds of the glyph placed at the (0,0) baseline
		if (ink.Width <= 0 || ink.Height <= 0)
		{
			// No ink (e.g. whitespace) — nothing to draw.
			return false;
		}

		var width = (int)Math.Ceiling(ink.Width);
		var height = (int)Math.Ceiling(ink.Height);

		var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.Transparent);
		using var paint = new SKPaint { IsAntialias = true };
		// Shift the glyph's ink from its baseline-relative bounds into [0,0]..[width,height].
		surface.Canvas.DrawText(blob, -ink.Left, -ink.Top, paint);

		result = new PositionedGlyphImage(new SkiaImage(surface.Snapshot()), originX + ink.Left, originY + ink.Top, width, height);
		return true;
	}

	private static bool HasColorTables(SKTypeface? typeface)
	{
		if (typeface is null)
		{
			return false;
		}

		foreach (var tag in typeface.GetTableTags())
		{
			if (tag == _colrTag || tag == _cbdtTag || tag == _sbixTag || tag == _svgTag)
			{
				return true;
			}
		}

		return false;
	}

	private static uint Tag(string tag) => (uint)((tag[0] << 24) | (tag[1] << 16) | (tag[2] << 8) | tag[3]);
}
