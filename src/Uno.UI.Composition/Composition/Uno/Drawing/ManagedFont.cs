#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.UI;
using HbFont = HarfBuzzSharp.Font;
using HbFace = HarfBuzzSharp.Face;
using HbBuffer = HarfBuzzSharp.Buffer;
using HbBlob = HarfBuzzSharp.Blob;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// An alternative, SkiaSharp-free <see cref="IFont"/> implementation: glyph outlines are read straight from the
/// font's sfnt tables (TrueType <c>glyf</c> — simple and composite — and CFF/Type2 <c>CFF </c>) and emitted through
/// the neutral <see cref="IPathBuilder"/>; color glyphs (<c>COLR</c>/<c>CPAL</c>) are composited from their colored
/// layers into an <see cref="IImage"/> via <see cref="IDrawingFactory.RenderOffscreen"/>. No Skia is used for any of
/// the outline/color work — this proves the font seam is genuinely backend-neutral, and lets the whole app render
/// text through a non-Skia backend when toggled on (see <c>FontDetails.FontHandle</c>).
/// </summary>
/// <remarks>
/// Shaping (text -> glyph ids/positions) still happens upstream; this handle only turns a shaped run into drawable
/// output. Bitmap color formats (CBDT/sbix) and COLRv1/OT-SVG are not handled here yet — those need the image-decode
/// half of the backend (a later phase); a font using only those color formats still renders its outlines.
/// </remarks>
internal sealed class ManagedFont : IFont
{
	// HarfBuzz shapes in font-design units at this fixed scale; Shape() converts the output back to pixels.
	private const int ShapeScale = 512;

	private readonly byte[] _data;
	private readonly float _pixelSize;
	private readonly int _unitsPerEm;
	private HbFont? _hbFont;
	private readonly int _numGlyphs;

	// TrueType 'glyf' outlines.
	private readonly int _glyf;
	private readonly int _loca;
	private readonly bool _longLoca;

	// CFF/Type2 outlines (mutually exclusive with glyf in practice).
	private readonly CffTable? _cff;

	// COLR/CPAL color layers.
	private readonly ColrTable? _colr;
	private readonly Color[]? _palette;

	// Metrics: cmap (char→glyph), hmtx (advances), and vertical line metrics (font units).
	private readonly CmapTable? _cmap;
	private readonly int _hmtx;
	private readonly int _numHMetrics;
	private readonly int _ascent;
	private readonly int _descent;
	private readonly int _lineGap;
	private readonly int? _underlinePosition;
	private readonly int? _underlineThickness;
	private readonly int? _strikeoutPosition;
	private readonly int? _strikeoutThickness;

	// Offset of the sfnt table directory within _data (past the ttc header for a collection), so table bytes
	// can be served on demand (GetFontTable) for the shaper face.
	private readonly int _sfntOffset;

	// 'name' table offset (0 = absent); family name parsed lazily.
	private readonly int _name;
	private string? _familyName;

	private ManagedFont(byte[] data, float pixelSize, int unitsPerEm, int numGlyphs, int glyf, int loca, bool longLoca, CffTable? cff, ColrTable? colr, Color[]? palette,
		CmapTable? cmap, int hmtx, int numHMetrics, int ascent, int descent, int lineGap, int? underlinePosition, int? underlineThickness,
		int? strikeoutPosition, int? strikeoutThickness, int sfntOffset, int name)
	{
		_name = name;
		_data = data;
		_pixelSize = pixelSize;
		_unitsPerEm = unitsPerEm;
		_numGlyphs = numGlyphs;
		_glyf = glyf;
		_loca = loca;
		_longLoca = longLoca;
		_cff = cff;
		_colr = colr;
		_palette = palette;
		_cmap = cmap;
		_hmtx = hmtx;
		_numHMetrics = numHMetrics;
		_ascent = ascent;
		_descent = descent;
		_lineGap = lineGap;
		_underlinePosition = underlinePosition;
		_underlineThickness = underlineThickness;
		_strikeoutPosition = strikeoutPosition;
		_strikeoutThickness = strikeoutThickness;
		_sfntOffset = sfntOffset;
	}

	private float Scale => _pixelSize / _unitsPerEm;

	/// <summary>Maps a Unicode codepoint to a glyph index via the font's cmap (0 = .notdef / missing).</summary>
	public ushort GetGlyphIndex(int codepoint) => _cmap?.Map(_data, codepoint) ?? 0;

	/// <summary>Whether this font has a glyph for <paramref name="codepoint"/>.</summary>
	public bool ContainsGlyph(int codepoint) => GetGlyphIndex(codepoint) != 0;

	public float GetGlyphAdvance(ushort glyph) => GetAdvance(glyph);

	/// <summary>The glyph's horizontal advance in font units (hmtx; glyphs past numHMetrics reuse the last advance).</summary>
	public int GetAdvanceWidth(ushort glyph)
	{
		if (_hmtx == 0 || _numHMetrics == 0) { return 0; }
		var i = glyph < _numHMetrics ? glyph : _numHMetrics - 1;
		return U16(_data, _hmtx + i * 4);
	}

	/// <summary>The glyph's advance in pixels (at this font's size).</summary>
	public float GetAdvance(ushort glyph) => GetAdvanceWidth(glyph) * Scale;

	// sfnt stores ascent up-positive / descent down-negative; the IFont contract follows SkiaSharp (ascent
	// negative above the baseline, descent positive below), so both are negated here.

	/// <summary>Distance from the baseline to the top of the text, negative (above the baseline).</summary>
	public float Ascent => -_ascent * Scale;

	/// <summary>Distance from the baseline to the bottom of the text, positive (below the baseline).</summary>
	public float Descent => -_descent * Scale;

	/// <summary>Recommended extra line spacing in pixels.</summary>
	public float LineGap => _lineGap * Scale;

	// post stores the underline offset up-negative (below baseline); Skia's convention is positive-below, so negate.

	/// <summary>Underline stroke offset from the baseline (positive below), or null if the font doesn't specify one.</summary>
	public float? UnderlinePosition => _underlinePosition is { } p ? -p * Scale : null;

	/// <summary>Underline stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	public float? UnderlineThickness => _underlineThickness is { } t ? t * Scale : null;

	// OS/2 stores the strikeout offset up-positive (above baseline); Skia's convention is negative-above, so negate.

	/// <summary>Strikeout stroke offset from the baseline (negative above), or null if the font doesn't specify one.</summary>
	public float? StrikeoutPosition => _strikeoutPosition is { } p ? -p * Scale : null;

	/// <summary>Strikeout stroke thickness in pixels, or null if the font doesn't specify one.</summary>
	public float? StrikeoutThickness => _strikeoutThickness is { } t ? t * Scale : null;

	/// <summary>The font's family name from the <c>name</c> table (empty if unavailable).</summary>
	public string FamilyName => _familyName ??= ParseFamilyName(_data, _name);

	public GlyphRun Shape(ReadOnlySpan<char> text, TextDirection direction, bool enableLigatures = true)
	{
		using var buffer = new HbBuffer();
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
		var scale = _pixelSize / ShapeScale;
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
			face.UnitsPerEm = _unitsPerEm;
			var font = new HbFont(face);
			font.SetScale(ShapeScale, ShapeScale);
			font.SetFunctionsOpenType();
			_hbFont = font;
		}

		return _hbFont;
	}

	private HbBlob? CreateTableBlob(uint tag)
	{
		var bytes = ReadTable(tag);
		if (bytes is not { Length: > 0 })
		{
			return null;
		}

		var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		return new HbBlob(handle.AddrOfPinnedObject(), bytes.Length, HarfBuzzSharp.MemoryMode.ReadOnly, handle.Free);
	}

	// Reads the family name from the 'name' table. Prefers the typographic family (nameID 16) over the legacy
	// family (nameID 1); prefers a Windows UTF-16BE English record, else the first usable record.
	internal static string ParseFamilyName(byte[] d, int name)
	{
		if (name == 0 || name + 6 > d.Length)
		{
			return string.Empty;
		}

		try
		{
			var count = U16(d, name + 2);
			var storage = name + U16(d, name + 4);
			string? family1 = null, family16 = null;
			var recordsStart = name + 6;
			for (var i = 0; i < count; i++)
			{
				var rec = recordsStart + i * 12;
				if (rec + 12 > d.Length)
				{
					break;
				}

				var nameId = U16(d, rec + 6);
				if (nameId != 1 && nameId != 16)
				{
					continue;
				}

				var platformId = U16(d, rec + 0);
				var length = U16(d, rec + 8);
				var offset = storage + U16(d, rec + 10);
				if (offset + length > d.Length)
				{
					continue;
				}

				// Platform 3 (Windows) and 0 (Unicode) store UTF-16BE; platform 1 (Mac) stores single-byte.
				var value = platformId is 3 or 0
					? DecodeUtf16Be(d, offset, length)
					: DecodeLatin1(d, offset, length);

				if (value.Length == 0)
				{
					continue;
				}

				if (nameId == 16)
				{
					family16 ??= value;
				}
				else
				{
					family1 ??= value;
				}
			}

			return family16 ?? family1 ?? string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string DecodeUtf16Be(byte[] d, int offset, int length)
	{
		var chars = new char[length / 2];
		for (var i = 0; i < chars.Length; i++)
		{
			chars[i] = (char)((d[offset + i * 2] << 8) | d[offset + i * 2 + 1]);
		}
		return new string(chars);
	}

	private static string DecodeLatin1(byte[] d, int offset, int length)
	{
		var chars = new char[length];
		for (var i = 0; i < length; i++)
		{
			chars[i] = (char)d[offset + i];
		}
		return new string(chars);
	}

	/// <summary>Returns the bytes of the sfnt table with the given tag, or null if absent (feeds the HarfBuzz face).</summary>
	private byte[]? ReadTable(uint tag)
	{
		var numTables = U16(_data, _sfntOffset + 4);
		var dir = _sfntOffset + 12;
		for (var i = 0; i < numTables; i++, dir += 16)
		{
			if (U32(_data, dir) == tag)
			{
				var offset = (int)U32(_data, dir + 8);
				var length = (int)U32(_data, dir + 12);
				if (offset < 0 || length < 0 || offset + length > _data.Length)
				{
					return null;
				}

				var bytes = new byte[length];
				Array.Copy(_data, offset, bytes, 0, length);
				return bytes;
			}
		}

		return null;
	}

	/// <summary>
	/// Parses <paramref name="data"/> (a full sfnt or TrueType collection) into a managed font. Returns false when the
	/// font carries neither a supported outline format (<c>glyf</c> or <c>CFF </c>) nor the tables needed to read them,
	/// so callers can fall back to another backend.
	/// </summary>
	public static bool TryCreate(byte[] data, int ttcIndex, float pixelSize, out ManagedFont font)
	{
		font = null!;
		try
		{
			var baseOffset = 0;
			if (data.Length >= 16 && U32(data, 0) == 0x74746366) // 'ttcf'
			{
				var count = (int)U32(data, 8);
				baseOffset = (int)U32(data, 12 + (ttcIndex >= 0 && ttcIndex < count ? ttcIndex : 0) * 4);
			}

			var numTables = U16(data, baseOffset + 4);
			int glyf = 0, loca = 0, head = 0, maxp = 0, cff = 0, colr = 0, cpal = 0, cmap = 0, hmtx = 0, hhea = 0, os2 = 0, post = 0, name = 0;
			var dir = baseOffset + 12;
			for (var i = 0; i < numTables; i++, dir += 16)
			{
				var offset = (int)U32(data, dir + 8);
				switch (U32(data, dir))
				{
					case 0x676C7966: glyf = offset; break; // 'glyf'
					case 0x6C6F6361: loca = offset; break; // 'loca'
					case 0x68656164: head = offset; break; // 'head'
					case 0x6D617870: maxp = offset; break; // 'maxp'
					case 0x43464620: cff = offset; break;  // 'CFF '
					case 0x434F4C52: colr = offset; break; // 'COLR'
					case 0x4350414C: cpal = offset; break; // 'CPAL'
					case 0x636D6170: cmap = offset; break; // 'cmap'
					case 0x686D7478: hmtx = offset; break; // 'hmtx'
					case 0x68686561: hhea = offset; break; // 'hhea'
					case 0x4F532F32: os2 = offset; break;  // 'OS/2'
					case 0x706F7374: post = offset; break; // 'post'
					case 0x6E616D65: name = offset; break; // 'name'
				}
			}

			if (head == 0 || maxp == 0)
			{
				return false;
			}

			var unitsPerEm = U16(data, head + 18);
			var longLoca = U16(data, head + 50) == 1;
			var numGlyphs = U16(data, maxp + 4);

			// Horizontal metrics + vertical line metrics (font units). Prefer OS/2 typo metrics when
			// the font asks for them (fsSelection bit 7 = USE_TYPO_METRICS); else fall back to hhea.
			var numHMetrics = hhea != 0 ? U16(data, hhea + 34) : 0;
			int ascent = 0, descent = 0, lineGap = 0;
			if (hhea != 0) { ascent = S16(data, hhea + 4); descent = S16(data, hhea + 6); lineGap = S16(data, hhea + 8); }
			if (os2 != 0 && os2 + 78 <= data.Length)
			{
				var useTypo = (U16(data, os2 + 62) & 0x80) != 0; // fsSelection USE_TYPO_METRICS
				if (useTypo || hhea == 0)
				{
					ascent = S16(data, os2 + 68); descent = S16(data, os2 + 70); lineGap = S16(data, os2 + 72);
				}
			}
			// post table (v1/v2/v3 all share the header): underlinePosition + underlineThickness in font units.
			int? underlinePosition = null, underlineThickness = null;
			if (post != 0 && post + 12 <= data.Length)
			{
				underlinePosition = S16(data, post + 8);
				underlineThickness = S16(data, post + 10);
			}

			// OS/2: yStrikeoutSize @26, yStrikeoutPosition @28 (font units).
			int? strikeoutThickness = null, strikeoutPosition = null;
			if (os2 != 0 && os2 + 30 <= data.Length)
			{
				strikeoutThickness = S16(data, os2 + 26);
				strikeoutPosition = S16(data, os2 + 28);
			}

			var cmapTable = cmap != 0 ? CmapTable.Parse(data, cmap) : null;

			var cffTable = cff != 0 ? CffTable.Parse(data, cff) : null;
			var hasOutlines = (glyf != 0 && loca != 0) || cffTable is not null;
			if (!hasOutlines || unitsPerEm == 0)
			{
				return false;
			}

			ColrTable? colrTable = null;
			Color[]? palette = null;
			if (colr != 0 && cpal != 0)
			{
				colrTable = ColrTable.Parse(data, colr);
				palette = ParseCpalPalette0(data, cpal);
				if (colrTable is null || palette is null)
				{
					colrTable = null;
					palette = null;
				}
			}

			font = new ManagedFont(data, pixelSize, unitsPerEm, numGlyphs, glyf, loca, longLoca, cffTable, colrTable, palette,
				cmapTable, hmtx, numHMetrics, ascent, descent, lineGap, underlinePosition, underlineThickness,
				strikeoutPosition, strikeoutThickness, baseOffset, name);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool HasColorGlyphs => _colr is not null && _palette is not null;

	/// <summary>Whether this font's outlines come from a CFF/Type2 table (rather than TrueType <c>glyf</c>). For diagnostics/tests.</summary>
	internal bool IsCffFont() => _cff is not null;

	/// <summary>Whether <paramref name="glyph"/> is a TrueType composite (multi-component) glyph. For diagnostics/tests.</summary>
	internal bool IsCompositeGlyph(ushort glyph)
	{
		if (_cff is not null || _glyf == 0 || glyph >= _numGlyphs)
		{
			return false;
		}

		int start, end;
		if (_longLoca)
		{
			start = (int)U32(_data, _loca + glyph * 4);
			end = (int)U32(_data, _loca + (glyph + 1) * 4);
		}
		else
		{
			start = U16(_data, _loca + glyph * 2) * 2;
			end = U16(_data, _loca + (glyph + 1) * 2) * 2;
		}

		return end > start && S16(_data, _glyf + start) < 0;
	}

	public IGeometry BuildGlyphRunOutline(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY)
	{
		var scale = _pixelSize / _unitsPerEm;
		var builder = GeometryFactory.Current.CreatePathBuilder();
		for (var i = 0; i < glyphs.Length; i++)
		{
			if (HasColorGlyphs && _colr!.HasBaseGlyph(glyphs[i]))
			{
				continue; // drawn as a color image by AppendColorGlyphImages
			}

			// Font units are Y-up with the origin at the baseline; screen space is Y-down.
			EmitOutline(builder, glyphs[i], positions[i].X, positions[i].Y + baselineY, scale);
		}

		return builder.Build();
	}

	public void AppendColorGlyphImages(ReadOnlySpan<ushort> glyphs, ReadOnlySpan<Vector2> positions, float baselineY, IList<PositionedGlyphImage> output)
	{
		if (!HasColorGlyphs)
		{
			return;
		}

		var scale = _pixelSize / _unitsPerEm;
		for (var i = 0; i < glyphs.Length; i++)
		{
			if (TryRasterizeColorGlyph(glyphs[i], positions[i].X, positions[i].Y + baselineY, scale, out var image))
			{
				output.Add(image);
			}
		}
	}

	private bool TryRasterizeColorGlyph(ushort glyph, float originX, float originY, float scale, out PositionedGlyphImage result)
	{
		result = default;
		if (!_colr!.TryGetLayers(glyph, out var layers))
		{
			return false;
		}

		// Build each layer's outline at the baseline origin, accumulate the ink bounds.
		var built = new List<(IGeometry Geometry, Color Color)>(layers.Count);
		var left = float.MaxValue;
		var top = float.MaxValue;
		var right = float.MinValue;
		var bottom = float.MinValue;
		foreach (var layer in layers)
		{
			var builder = GeometryFactory.Current.CreatePathBuilder();
			EmitOutline(builder, layer.GlyphId, 0f, 0f, scale);
			var geometry = builder.Build();
			var b = geometry.Bounds;
			if (b.Width > 0 && b.Height > 0)
			{
				left = Math.Min(left, (float)b.Left);
				top = Math.Min(top, (float)b.Top);
				right = Math.Max(right, (float)b.Right);
				bottom = Math.Max(bottom, (float)b.Bottom);
			}

			built.Add((geometry, PaletteColor(layer.PaletteIndex)));
		}

		if (right <= left || bottom <= top)
		{
			foreach (var (g, _) in built)
			{
				g.Dispose();
			}

			return false;
		}

		var width = (int)Math.Ceiling(right - left);
		var height = (int)Math.Ceiling(bottom - top);
		var shiftX = -left;
		var shiftY = -top;

		var image = DrawingFactory.Current.RenderOffscreen(width, height, session =>
		{
			session.Translate(shiftX, shiftY);
			foreach (var (geometry, color) in built)
			{
				session.DrawPath(geometry, color, antialias: true);
			}
		});

		foreach (var (g, _) in built)
		{
			g.Dispose();
		}

		result = new PositionedGlyphImage(image, originX + left, originY + top, width, height);
		return true;
	}

	private Color PaletteColor(int paletteIndex)
	{
		// 0xFFFF means "use the current text foreground color"; approximate with opaque black here.
		if (paletteIndex == 0xFFFF || _palette is null || paletteIndex < 0 || paletteIndex >= _palette.Length)
		{
			return Color.FromArgb(0xFF, 0, 0, 0);
		}

		return _palette[paletteIndex];
	}

	private void EmitOutline(IPathBuilder builder, ushort glyph, float originX, float originY, float scale)
	{
		if (_cff is not null)
		{
			_cff.EmitGlyph(builder, glyph, originX, originY, scale);
		}
		else
		{
			EmitGlyf(builder, glyph, originX, originY, scale, Matrix3x2.Identity, depth: 0);
		}
	}

	private void EmitGlyf(IPathBuilder builder, ushort glyph, float originX, float originY, float scale, Matrix3x2 componentTransform, int depth)
	{
		if (glyph >= _numGlyphs || depth > 8)
		{
			return;
		}

		int start, end;
		if (_longLoca)
		{
			start = (int)U32(_data, _loca + glyph * 4);
			end = (int)U32(_data, _loca + (glyph + 1) * 4);
		}
		else
		{
			start = U16(_data, _loca + glyph * 2) * 2;
			end = U16(_data, _loca + (glyph + 1) * 2) * 2;
		}

		if (end <= start)
		{
			return; // empty glyph (e.g. space)
		}

		var p = _glyf + start;
		var numContours = S16(_data, p);
		p += 2 + 8; // numberOfContours + xMin/yMin/xMax/yMax

		if (numContours < 0)
		{
			EmitCompositeGlyf(builder, p, originX, originY, scale, componentTransform, depth);
			return;
		}

		var endPts = new int[numContours];
		for (var c = 0; c < numContours; c++, p += 2)
		{
			endPts[c] = U16(_data, p);
		}

		var numPoints = numContours == 0 ? 0 : endPts[numContours - 1] + 1;
		if (numPoints == 0)
		{
			return;
		}

		var instructionLength = U16(_data, p);
		p += 2 + instructionLength;

		var flags = new byte[numPoints];
		for (var i = 0; i < numPoints;)
		{
			var flag = _data[p++];
			flags[i++] = flag;
			if ((flag & 0x08) != 0) // REPEAT_FLAG
			{
				var repeat = _data[p++];
				while (repeat-- > 0 && i < numPoints)
				{
					flags[i++] = flag;
				}
			}
		}

		var xs = new int[numPoints];
		var x = 0;
		for (var i = 0; i < numPoints; i++)
		{
			var flag = flags[i];
			if ((flag & 0x02) != 0) // X_SHORT_VECTOR
			{
				var dx = _data[p++];
				x += (flag & 0x10) != 0 ? dx : -dx;
			}
			else if ((flag & 0x10) == 0) // not X_IS_SAME
			{
				x += S16(_data, p);
				p += 2;
			}

			xs[i] = x;
		}

		var ys = new int[numPoints];
		var y = 0;
		for (var i = 0; i < numPoints; i++)
		{
			var flag = flags[i];
			if ((flag & 0x04) != 0) // Y_SHORT_VECTOR
			{
				var dy = _data[p++];
				y += (flag & 0x20) != 0 ? dy : -dy;
			}
			else if ((flag & 0x20) == 0) // not Y_IS_SAME
			{
				y += S16(_data, p);
				p += 2;
			}

			ys[i] = y;
		}

		Vector2 Screen(int index)
		{
			var v = Vector2.Transform(new Vector2(xs[index], ys[index]), componentTransform);
			return new Vector2(originX + v.X * scale, originY - v.Y * scale);
		}

		var contourStart = 0;
		for (var c = 0; c < numContours; c++)
		{
			EmitGlyfContour(builder, flags, contourStart, endPts[c], Screen);
			contourStart = endPts[c] + 1;
		}
	}

	private void EmitCompositeGlyf(IPathBuilder builder, int p, float originX, float originY, float scale, Matrix3x2 parentTransform, int depth)
	{
		bool more;
		do
		{
			var flags = (ushort)U16(_data, p);
			p += 2;
			var componentGlyph = (ushort)U16(_data, p);
			p += 2;
			more = (flags & 0x0020) != 0; // MORE_COMPONENTS

			float dx, dy;
			if ((flags & 0x0001) != 0) // ARG_1_AND_2_ARE_WORDS
			{
				dx = S16(_data, p);
				dy = S16(_data, p + 2);
				p += 4;
			}
			else
			{
				dx = (sbyte)_data[p];
				dy = (sbyte)_data[p + 1];
				p += 2;
			}

			// Point-matching args (bit1 clear) are rare; treat both as an offset (a small positional approximation).
			float a = 1f, b = 0f, c = 0f, d = 1f;
			if ((flags & 0x0008) != 0) // WE_HAVE_A_SCALE
			{
				a = d = F2Dot14(p);
				p += 2;
			}
			else if ((flags & 0x0040) != 0) // WE_HAVE_AN_X_AND_Y_SCALE
			{
				a = F2Dot14(p);
				d = F2Dot14(p + 2);
				p += 4;
			}
			else if ((flags & 0x0080) != 0) // WE_HAVE_A_TWO_BY_TWO
			{
				a = F2Dot14(p);
				b = F2Dot14(p + 2);
				c = F2Dot14(p + 4);
				d = F2Dot14(p + 6);
				p += 8;
			}

			var componentTransform = new Matrix3x2(a, b, c, d, dx, dy) * parentTransform;
			EmitGlyf(builder, componentGlyph, originX, originY, scale, componentTransform, depth + 1);
		}
		while (more);
	}

	private static void EmitGlyfContour(IPathBuilder builder, byte[] flags, int first, int last, Func<int, Vector2> screen)
	{
		var n = last - first + 1;
		if (n <= 0)
		{
			return;
		}

		Vector2 Point(int k) => screen(first + ((k % n) + n) % n);
		bool OnCurve(int k) => (flags[first + ((k % n) + n) % n] & 0x01) != 0;

		var startK = -1;
		for (var k = 0; k < n; k++)
		{
			if (OnCurve(k))
			{
				startK = k;
				break;
			}
		}

		if (startK == -1)
		{
			// All off-curve: the start is the implied midpoint of the last and first control points.
			var implied = Vector2.Lerp(Point(n - 1), Point(0), 0.5f);
			builder.MoveTo(implied);
			var previousOff = Point(0);
			for (var k = 1; k <= n; k++)
			{
				var current = Point(k);
				builder.QuadraticTo(previousOff, Vector2.Lerp(previousOff, current, 0.5f));
				previousOff = current;
			}

			builder.Close();
			return;
		}

		builder.MoveTo(Point(startK));
		var haveOff = false;
		var off = default(Vector2);
		for (var k = 1; k <= n; k++)
		{
			var index = startK + k;
			var current = Point(index);
			if (OnCurve(index))
			{
				if (haveOff)
				{
					builder.QuadraticTo(off, current);
					haveOff = false;
				}
				else
				{
					builder.LineTo(current);
				}
			}
			else
			{
				if (haveOff)
				{
					builder.QuadraticTo(off, Vector2.Lerp(off, current, 0.5f)); // implied on-curve midpoint
				}

				off = current;
				haveOff = true;
			}
		}

		builder.Close();
	}

	private static Color[]? ParseCpalPalette0(byte[] d, int cpal)
	{
		var numPaletteEntries = U16(d, cpal + 2);
		var numColorRecords = U16(d, cpal + 6);
		var colorRecordsOffset = (int)U32(d, cpal + 8);
		var firstIndex = U16(d, cpal + 12); // colorRecordIndices[0]
		if (numPaletteEntries == 0 || firstIndex + numPaletteEntries > numColorRecords)
		{
			return null;
		}

		var palette = new Color[numPaletteEntries];
		var p = colorRecordsOffset + firstIndex * 4;
		for (var i = 0; i < numPaletteEntries; i++, p += 4)
		{
			// CPAL color records are BGRA.
			palette[i] = Color.FromArgb(d[p + 3], d[p + 2], d[p + 1], d[p]);
		}

		return palette;
	}

	private float F2Dot14(int offset) => S16(_data, offset) / 16384f;

	internal static int U16(byte[] d, int o) => (d[o] << 8) | d[o + 1];
	internal static short S16(byte[] d, int o) => (short)U16(d, o);
	internal static uint U32(byte[] d, int o) => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];

	/// <summary>Unicode <c>cmap</c> subtable (format 4 BMP or format 12 full) mapping codepoints to glyph indices.</summary>
	private sealed class CmapTable
	{
		private readonly int _offset; // absolute offset of the chosen subtable
		private readonly int _format;

		private CmapTable(int offset, int format) { _offset = offset; _format = format; }

		public static CmapTable? Parse(byte[] d, int cmap)
		{
			int numTables = U16(d, cmap + 2);
			int best = -1, bestScore = -1;
			for (var i = 0; i < numTables; i++)
			{
				var rec = cmap + 4 + i * 8;
				int plat = U16(d, rec), enc = U16(d, rec + 2);
				var off = (int)U32(d, rec + 4);
				// Prefer full-Unicode (3/10, 0/{4,6}) over BMP (3/1, 0/3) over any Unicode platform-0.
				var score = (plat, enc) switch
				{
					(3, 10) => 5,
					(0, 6) => 4,
					(0, 4) => 4,
					(3, 1) => 3,
					(0, 3) => 3,
					(0, _) => 2,
					_ => -1,
				};
				if (score > bestScore) { bestScore = score; best = cmap + off; }
			}
			if (best < 0) { return null; }
			var format = U16(d, best);
			return format is 4 or 12 ? new CmapTable(best, format) : null;
		}

		public ushort Map(byte[] d, int codepoint) => _format == 12 ? Map12(d, codepoint) : Map4(d, codepoint);

		private ushort Map4(byte[] d, int cp)
		{
			if (cp > 0xFFFF) { return 0; }
			var o = _offset;
			var segX2 = U16(d, o + 6);
			var segCount = segX2 / 2;
			var endO = o + 14;
			var startO = endO + segX2 + 2;
			var deltaO = startO + segX2;
			var rangeO = deltaO + segX2;
			for (var i = 0; i < segCount; i++)
			{
				var end = U16(d, endO + i * 2);
				if (cp > end) { continue; }
				var start = U16(d, startO + i * 2);
				if (cp < start) { return 0; }
				int idDelta = S16(d, deltaO + i * 2);
				var idRange = U16(d, rangeO + i * 2);
				if (idRange == 0) { return (ushort)((cp + idDelta) & 0xFFFF); }
				var gi = U16(d, rangeO + i * 2 + idRange + (cp - start) * 2);
				return gi == 0 ? (ushort)0 : (ushort)((gi + idDelta) & 0xFFFF);
			}
			return 0;
		}

		private ushort Map12(byte[] d, int cp)
		{
			var o = _offset;
			var nGroups = (int)U32(d, o + 12);
			var g = o + 16;
			for (var i = 0; i < nGroups; i++, g += 12)
			{
				uint startC = U32(d, g), endC = U32(d, g + 4), startG = U32(d, g + 8);
				if (cp >= startC && cp <= endC) { return (ushort)(startG + (cp - startC)); }
			}
			return 0;
		}
	}

	/// <summary>COLRv0 base-glyph -> colored layer records (glyph id + CPAL palette index).</summary>
	private sealed class ColrTable
	{
		private readonly byte[] _data;
		private readonly int _baseGlyphRecords;
		private readonly int _numBaseGlyphRecords;
		private readonly int _layerRecords;

		private ColrTable(byte[] data, int baseGlyphRecords, int numBaseGlyphRecords, int layerRecords)
		{
			_data = data;
			_baseGlyphRecords = baseGlyphRecords;
			_numBaseGlyphRecords = numBaseGlyphRecords;
			_layerRecords = layerRecords;
		}

		public static ColrTable? Parse(byte[] d, int colr)
		{
			var version = U16(d, colr);
			if (version != 0)
			{
				// COLRv1 (gradients/transforms) is not handled by this reader yet.
				return null;
			}

			var numBaseGlyphRecords = U16(d, colr + 2);
			var baseGlyphRecordsOffset = (int)U32(d, colr + 4);
			var layerRecordsOffset = (int)U32(d, colr + 8);
			return new ColrTable(d, colr + baseGlyphRecordsOffset, numBaseGlyphRecords, colr + layerRecordsOffset);
		}

		public bool HasBaseGlyph(ushort glyph) => TryFindBaseGlyph(glyph, out _, out _);

		public bool TryGetLayers(ushort glyph, out List<(ushort GlyphId, int PaletteIndex)> layers)
		{
			layers = null!;
			if (!TryFindBaseGlyph(glyph, out var firstLayer, out var numLayers))
			{
				return false;
			}

			layers = new List<(ushort, int)>(numLayers);
			for (var i = 0; i < numLayers; i++)
			{
				var p = _layerRecords + (firstLayer + i) * 4;
				layers.Add(((ushort)U16(_data, p), U16(_data, p + 2)));
			}

			return true;
		}

		private bool TryFindBaseGlyph(ushort glyph, out int firstLayerIndex, out int numLayers)
		{
			firstLayerIndex = 0;
			numLayers = 0;

			// Base-glyph records are sorted by glyph id — binary search.
			int lo = 0, hi = _numBaseGlyphRecords - 1;
			while (lo <= hi)
			{
				var mid = (lo + hi) >> 1;
				var p = _baseGlyphRecords + mid * 6;
				var gid = U16(_data, p);
				if (gid == glyph)
				{
					firstLayerIndex = U16(_data, p + 2);
					numLayers = U16(_data, p + 4);
					return true;
				}

				if (gid < glyph)
				{
					lo = mid + 1;
				}
				else
				{
					hi = mid - 1;
				}
			}

			return false;
		}
	}
}
