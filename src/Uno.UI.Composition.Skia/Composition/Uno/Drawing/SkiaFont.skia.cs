#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using SkiaSharp;
using HbFont = HarfBuzzSharp.Font;
using HbFace = HarfBuzzSharp.Face;
using HbBuffer = HarfBuzzSharp.Buffer;
using HbBlob = HarfBuzzSharp.Blob;

namespace Uno.UI.Composition.Drawing;

/// <summary>SkiaSharp-backed <see cref="IFont"/> wrapping an <see cref="SKFont"/>. Shaping is done with HarfBuzz
/// (an implementation detail of this handle), fed from the typeface's own sfnt tables.</summary>
internal sealed class SkiaFont : IFont
{
	private static readonly uint _colrTag = Tag("COLR");
	private static readonly uint _cbdtTag = Tag("CBDT");
	private static readonly uint _sbixTag = Tag("sbix");
	private static readonly uint _svgTag = Tag("SVG ");

	// HarfBuzz shapes in font-design units at this fixed scale; Shape() converts the output back to pixels.
	private const int ShapeScale = 512;

	private readonly SKFont _font;
	private readonly SKFontMetrics _metrics;
	private HbFont? _hbFont;

	public SkiaFont(SKFont font)
	{
		_font = font;
		_metrics = font.Metrics;
		HasColorGlyphs = HasColorTables(font.Typeface);
	}

	public bool HasColorGlyphs { get; }

	public float Ascent => _metrics.Ascent;

	public float Descent => _metrics.Descent;

	public float LineGap => _metrics.Leading;

	public float? UnderlinePosition => _metrics.UnderlinePosition;

	public float? UnderlineThickness => _metrics.UnderlineThickness;

	public float? StrikeoutPosition => _metrics.StrikeoutPosition;

	public float? StrikeoutThickness => _metrics.StrikeoutThickness;

	public ushort GetGlyphIndex(int codepoint) => _font.GetGlyph(codepoint);

	public bool ContainsGlyph(int codepoint) => _font.ContainsGlyph(codepoint);

	public float GetGlyphAdvance(ushort glyph)
	{
		Span<ushort> glyphs = [glyph];
		return _font.MeasureText(glyphs, null);
	}

	public string FamilyName => _font.Typeface?.FamilyName ?? string.Empty;

	public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
		=> ShapeCore(text, direction, enableLigatures, out _);

	public GlyphRun Shape(ReadOnlySpan<char> text, out TextDirection resolvedDirection, bool enableLigatures = true)
		=> ShapeCore(text, null, enableLigatures, out resolvedDirection);

	private GlyphRun ShapeCore(ReadOnlySpan<char> text, TextDirection? direction, bool enableLigatures, out TextDirection resolvedDirection)
	{
		using var buffer = new HbBuffer();
		buffer.AddUtf16(text);
		buffer.GuessSegmentProperties();
		if (direction is { } requested)
		{
			buffer.Direction = requested == TextDirection.RightToLeft ? HarfBuzzSharp.Direction.RightToLeft : HarfBuzzSharp.Direction.LeftToRight;
		}
		resolvedDirection = buffer.Direction == HarfBuzzSharp.Direction.RightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;

		if (enableLigatures)
		{
			GetHarfBuzzFont().Shape(buffer);
		}
		else
		{
			// Disable the OpenType 'liga' feature (a run may span multiple chars that must stay separately addressable).
			GetHarfBuzzFont().Shape(buffer, new HarfBuzzSharp.Feature(new HarfBuzzSharp.Tag('l', 'i', 'g', 'a'), 0));
		}

		var infos = buffer.GetGlyphInfoSpan();
		var pos = buffer.GetGlyphPositionSpan();
		var count = buffer.Length;
		var glyphs = new ushort[count];
		var offsets = new Vector2[count];
		var advances = new float[count];
		var clusters = new int[count];
		var scale = _font.Size / (float)ShapeScale;
		for (var i = 0; i < count; i++)
		{
			glyphs[i] = (ushort)infos[i].Codepoint;
			clusters[i] = (int)infos[i].Cluster;
			offsets[i] = new Vector2(pos[i].XOffset * scale, pos[i].YOffset * scale);
			advances[i] = pos[i].XAdvance * scale;
		}

		return new GlyphRun(glyphs, offsets, advances, clusters);
	}

	private HbFont GetHarfBuzzFont()
	{
		if (_hbFont is null)
		{
			var face = new HbFace((_, tag) => CreateTableBlob((uint)tag));
			face.UnitsPerEm = _font.Typeface?.UnitsPerEm ?? 0;
			var font = new HbFont(face);
			font.SetScale(ShapeScale, ShapeScale);
			font.SetFunctionsOpenType();
			_hbFont = font;
		}

		return _hbFont;
	}

	private HbBlob? CreateTableBlob(uint tag)
	{
		var typeface = _font.Typeface;
		if (typeface is null)
		{
			return null;
		}

		// GetTableData throws when the table is absent (HarfBuzz probes optional tags), so size-check first.
		var fourByteTag = new SKFourByteTag(tag);
		var size = typeface.GetTableSize(fourByteTag);
		if (size == 0)
		{
			return null;
		}

		var bytes = new byte[size];
		unsafe
		{
			fixed (byte* p = bytes)
			{
				if (!typeface.TryGetTableData(fourByteTag, 0, size, (IntPtr)p))
				{
					return null;
				}
			}
		}

		var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		return new HbBlob(handle.AddrOfPinnedObject(), bytes.Length, HarfBuzzSharp.MemoryMode.ReadOnly, handle.Free);
	}

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
