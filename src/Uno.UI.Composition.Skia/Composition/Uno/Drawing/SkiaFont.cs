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
	// Per-glyph caches (this instance is kept stable by SkiaFontProvider, so they survive across paints): repaints
	// reuse the neutral outline / rasterized pixels instead of re-extracting them. Coords are glyph-local; the pen
	// offset is applied at replay.
	private readonly Dictionary<ushort, PenOp[]> _glyphOutlines = new();
	private readonly Dictionary<ushort, RasterGlyph?> _colorGlyphs = new();

	public SkiaFont(SKFont font)
	{
		_font = font;
		_metrics = font.Metrics;
		HasColorGlyphs = HasColorTables(font.Typeface);
	}

	public bool HasColorGlyphs { get; }

	public float Ascent => _metrics.Ascent;

	public float Descent => _metrics.Descent;


	public float? UnderlinePosition => _metrics.UnderlinePosition;

	public float? UnderlineThickness => _metrics.UnderlineThickness;

	public float? StrikeoutPosition => _metrics.StrikeoutPosition;

	public float? StrikeoutThickness => _metrics.StrikeoutThickness;

	public ushort GetGlyphIndex(int codepoint) => _font.GetGlyph(codepoint);

	// ASCII coverage bitmap: text layout probes ContainsGlyph per character (a SkiaSharp P/Invoke), which
	// dominates short-label re-layout; this instance is provider-cached so the one-time probe amortizes.
	private ulong _asciiCoverageLo, _asciiCoverageHi;
	private bool _asciiCoverageInitialized;

	public bool ContainsGlyph(int codepoint)
	{
		if ((uint)codepoint < 128)
		{
			if (!_asciiCoverageInitialized)
			{
				for (var c = 0; c < 128; c++)
				{
					if (_font.ContainsGlyph(c))
					{
						if (c < 64) { _asciiCoverageLo |= 1UL << c; }
						else { _asciiCoverageHi |= 1UL << (c - 64); }
					}
				}
				_asciiCoverageInitialized = true;
			}

			return codepoint < 64
				? (_asciiCoverageLo & (1UL << codepoint)) != 0
				: (_asciiCoverageHi & (1UL << (codepoint - 64))) != 0;
		}

		return _font.ContainsGlyph(codepoint);
	}

	public float GetGlyphAdvance(ushort glyph)
	{
		Span<ushort> glyphs = [glyph];
		return _font.MeasureText(glyphs, null);
	}

	public string FamilyName => _font.Typeface?.FamilyName ?? string.Empty;

	// One reusable shaping buffer per thread: creating/destroying a native buffer per Shape call is a real
	// cost when short labels re-shape every frame.
	[ThreadStatic]
	private static HbBuffer? _shapeBuffer;

	private readonly GlyphRunCache _shapeCache = new();

	public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
	{
		if (_shapeCache.TryGet(text, direction, enableLigatures, out var cached))
		{
			return cached;
		}

		var buffer = _shapeBuffer ??= new HbBuffer();
		buffer.ClearContents();
		buffer.AddUtf16(text);
		buffer.GuessSegmentProperties(); // sets the run's script/language for the shaper; direction is set explicitly below
		buffer.Direction = direction == TextDirection.RightToLeft ? HarfBuzzSharp.Direction.RightToLeft : HarfBuzzSharp.Direction.LeftToRight;

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

		var run = new GlyphRun(glyphs, offsets, advances, clusters);
		_shapeCache.Add(text, direction, enableLigatures, run);
		return run;
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

	public void BuildGlyphRun(IGeometryFactory geometry, ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<GlyphRunElement> elements)
	{
		// Build the outline through the registered geometry factory — the font emits neutral pen verbs and never
		// names a concrete IGeometry type, so it works with whatever geometry backend is registered.
		var builder = geometry.CreatePathBuilder();

		for (var i = 0; i < glyphs.Length; i++)
		{
			var ops = GetGlyphOutline(glyphs[i]);
			if (ops.Length > 0)
			{
				ReplayTranslated(builder, ops, positions[i].X, positions[i].Y + baselineY);
			}
			else if (HasColorGlyphs && GetColorGlyph(glyphs[i]) is { } raster)
			{
				// Empty outline == a colour glyph (or blank). SkiaSharp only rasterizes it, so hand the neutral pixels
				// across the seam; the caller uploads them to a texture (the font stays off the backend).
				elements.Add(new GlyphImage(raster.Pixels, raster.Width, raster.Height,
					positions[i].X + raster.InkLeft, positions[i].Y + baselineY + raster.InkTop));
			}
		}

		elements.Add(new GlyphOutline(builder.Build()));
	}

	// Cached neutral pen ops for a glyph in glyph-local coords (empty = no outline / colour glyph).
	private PenOp[] GetGlyphOutline(ushort glyph)
	{
		if (_glyphOutlines.TryGetValue(glyph, out var cached))
		{
			return cached;
		}

		using var glyphPath = _font.GetGlyphPath(glyph);
		var ops = glyphPath is { IsEmpty: false } ? ExtractPenOps(glyphPath) : Array.Empty<PenOp>();
		_glyphOutlines[glyph] = ops;
		return ops;
	}

	// Replays cached glyph-local pen ops into a neutral IPathBuilder, translated to the pen position.
	private static void ReplayTranslated(IPathBuilder builder, PenOp[] ops, float dx, float dy)
	{
		var d = new Vector2(dx, dy);
		foreach (var op in ops)
		{
			switch (op.Verb)
			{
				case PenVerb.Move: builder.MoveTo(op.P0 + d); break;
				case PenVerb.Line: builder.LineTo(op.P0 + d); break;
				case PenVerb.Quad: builder.QuadraticTo(op.P0 + d, op.P1 + d); break;
				case PenVerb.Cubic: builder.CubicTo(op.P0 + d, op.P1 + d, op.P2 + d); break;
				case PenVerb.Close: builder.Close(); break;
			}
		}
	}

	// Extracts an SKPath's contours to neutral pen ops (glyph-local; no SkiaSharp type crosses the seam or is cached).
	private static PenOp[] ExtractPenOps(SKPath path)
	{
		var ops = new List<PenOp>();
		using var it = path.CreateIterator(false);
		var pts = new SKPoint[4];
		SKPathVerb verb;
		while ((verb = it.Next(pts)) != SKPathVerb.Done)
		{
			switch (verb)
			{
				case SKPathVerb.Move:
					ops.Add(new PenOp(PenVerb.Move, new Vector2(pts[0].X, pts[0].Y)));
					break;
				case SKPathVerb.Line:
					ops.Add(new PenOp(PenVerb.Line, new Vector2(pts[1].X, pts[1].Y)));
					break;
				case SKPathVerb.Quad:
					ops.Add(new PenOp(PenVerb.Quad, new Vector2(pts[1].X, pts[1].Y), new Vector2(pts[2].X, pts[2].Y)));
					break;
				case SKPathVerb.Cubic:
					ops.Add(new PenOp(PenVerb.Cubic, new Vector2(pts[1].X, pts[1].Y), new Vector2(pts[2].X, pts[2].Y), new Vector2(pts[3].X, pts[3].Y)));
					break;
				case SKPathVerb.Conic:
					// No neutral conic verb — subdivide into quads (2 for pow2=1: array is [p0,c0,m,c1,p2]).
					var quads = SKPath.ConvertConicToQuads(pts[0], pts[1], pts[2], it.ConicWeight(), 1);
					for (var q = 1; q + 1 < quads.Length; q += 2)
					{
						ops.Add(new PenOp(PenVerb.Quad, new Vector2(quads[q].X, quads[q].Y), new Vector2(quads[q + 1].X, quads[q + 1].Y)));
					}
					break;
				case SKPathVerb.Close:
					ops.Add(new PenOp(PenVerb.Close));
					break;
			}
		}

		return ops.ToArray();
	}

	// Cached rasterized colour-glyph pixels (glyph-local ink; the pen origin is applied per paint), or null when the
	// glyph has no ink (e.g. whitespace).
	private RasterGlyph? GetColorGlyph(ushort glyph)
	{
		if (_colorGlyphs.TryGetValue(glyph, out var cached))
		{
			return cached;
		}

		var raster = RasterizeColorGlyph(glyph);
		_colorGlyphs[glyph] = raster;
		return raster;
	}

	private RasterGlyph? RasterizeColorGlyph(ushort glyph)
	{
		using var builder = new SKTextBlobBuilder();
		builder.AddPositionedRun(new[] { glyph }, _font, new[] { new SKPoint(0, 0) });
		using var blob = builder.Build();
		if (blob is null)
		{
			return null;
		}

		var ink = blob.Bounds; // ink bounds of the glyph placed at the (0,0) baseline
		if (ink.Width <= 0 || ink.Height <= 0)
		{
			// No ink (e.g. whitespace) — nothing to draw.
			return null;
		}

		var width = (int)Math.Ceiling(ink.Width);
		var height = (int)Math.Ceiling(ink.Height);

		var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
		using var surface = SKSurface.Create(info);
		surface.Canvas.Clear(SKColors.Transparent);
		using var paint = new SKPaint { IsAntialias = true };
		// Shift the glyph's ink from its baseline-relative bounds into [0,0]..[width,height].
		surface.Canvas.DrawText(blob, -ink.Left, -ink.Top, paint);

		// Read the rasterized glyph back to neutral BGRA8888-premultiplied pixels — the seam currency, not a texture.
		var pixels = new byte[width * height * 4];
		unsafe
		{
			fixed (byte* dst = pixels)
			{
				if (!surface.ReadPixels(info, (nint)dst, info.RowBytes, 0, 0))
				{
					return null;
				}
			}
		}

		return new RasterGlyph(pixels, width, height, ink.Left, ink.Top);
	}

	private enum PenVerb : byte { Move, Line, Quad, Cubic, Close }

	private readonly struct PenOp
	{
		public readonly PenVerb Verb;
		public readonly Vector2 P0, P1, P2;

		public PenOp(PenVerb verb, Vector2 p0 = default, Vector2 p1 = default, Vector2 p2 = default)
		{
			Verb = verb;
			P0 = p0;
			P1 = p1;
			P2 = p2;
		}
	}

	private readonly struct RasterGlyph
	{
		public readonly byte[] Pixels;
		public readonly int Width, Height;
		public readonly float InkLeft, InkTop;

		public RasterGlyph(byte[] pixels, int width, int height, float inkLeft, float inkTop)
		{
			Pixels = pixels;
			Width = width;
			Height = height;
			InkLeft = inkLeft;
			InkTop = inkTop;
		}
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
