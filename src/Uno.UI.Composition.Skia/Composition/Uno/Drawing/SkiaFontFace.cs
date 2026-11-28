#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using HbBlob = HarfBuzzSharp.Blob;
using HbFace = HarfBuzzSharp.Face;
using HbFont = HarfBuzzSharp.Font;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The size-independent part of a font: one per typeface, shared by every <see cref="SkiaFont"/> of that typeface.
/// A new size only adds an <see cref="SKFont"/>, so animating FontSize doesn't re-read the sfnt tables per size.
/// </summary>
internal sealed class SkiaFontFace
{
	private static readonly uint _colrTag = Tag("COLR");
	private static readonly uint _cbdtTag = Tag("CBDT");
	private static readonly uint _sbixTag = Tag("sbix");
	private static readonly uint _svgTag = Tag("SVG ");

	// HarfBuzz shapes in font-design units at this fixed scale; SkiaFont converts the output to pixels for its size.
	internal const int ShapeScale = 512;

	// Guards the lazily built HarfBuzz font and COLR outline source.
	private readonly object _gate = new();
	private HbFont? _hbFont;
	private ulong[]? _asciiCoverage;
	private readonly bool _needsColrOutlineSource;
	private bool _colrOutlineSourceCreated;
	private SkiaColrOutlineSource? _colrOutlineSource;

	public SkiaFontFace(SKTypeface typeface)
	{
		Typeface = typeface;
		HasColorGlyphs = HasColorTables(typeface, out var colrOnly);

		// Skia's CoreText scaler treats the font-level colour flag as per glyph, so it gives no outline for any glyph of a COLR font.
		_needsColrOutlineSource = colrOnly && SkiaFontProvider.UsesCoreText;
	}

	public SKTypeface Typeface { get; }

	public bool HasColorGlyphs { get; }

	/// <summary>The outlines of the plain glyphs of a font whose colour glyphs come from COLR alone, where <see cref="Typeface"/> gives none.</summary>
	/// <remarks><c>null</c> where <see cref="Typeface"/> gives them itself (any scaler but CoreText's), or where the source can't be built.
	/// Built on first use.</remarks>
	public SkiaColrOutlineSource? ColrOutlineSource
	{
		get
		{
			if (!_needsColrOutlineSource)
			{
				return null;
			}

			if (Volatile.Read(ref _colrOutlineSourceCreated))
			{
				return _colrOutlineSource;
			}

			lock (_gate)
			{
				if (!_colrOutlineSourceCreated)
				{
					_colrOutlineSource = SkiaColrOutlineSource.Create(Typeface);
					Volatile.Write(ref _colrOutlineSourceCreated, true);
				}

				return _colrOutlineSource;
			}
		}
	}

	public HbFont HarfBuzzFont
	{
		get
		{
			if (Volatile.Read(ref _hbFont) is { } existing)
			{
				return existing;
			}

			lock (_gate)
			{
				if (_hbFont is null)
				{
					var face = new HbFace((_, tag) => CreateTableBlob((uint)tag));
					face.UnitsPerEm = Typeface.UnitsPerEm;
					var font = new HbFont(face);
					font.SetScale(ShapeScale, ShapeScale);
					font.SetFunctionsOpenType();
					Volatile.Write(ref _hbFont, font);
				}

				return _hbFont;
			}
		}
	}

	// Text layout probes coverage per character (a SkiaSharp P/Invoke), which dominates short-label re-layout.
	public bool ContainsAsciiGlyph(int codepoint, SKFont font)
	{
		var coverage = Volatile.Read(ref _asciiCoverage);
		if (coverage is null)
		{
			coverage = new ulong[2];
			for (var c = 0; c < 128; c++)
			{
				if (font.ContainsGlyph(c))
				{
					coverage[c >> 6] |= 1UL << (c & 63);
				}
			}

			Volatile.Write(ref _asciiCoverage, coverage);
		}

		return (coverage[codepoint >> 6] & (1UL << (codepoint & 63))) != 0;
	}

	public byte[]? ReadTable(uint tag)
	{
		// GetTableData throws when the table is absent (HarfBuzz probes optional tags), so size-check first.
		var fourByteTag = new SKFourByteTag(tag);
		var size = Typeface.GetTableSize(fourByteTag);
		if (size == 0)
		{
			return null;
		}

		var bytes = new byte[size];
		unsafe
		{
			fixed (byte* p = bytes)
			{
				if (!Typeface.TryGetTableData(fourByteTag, 0, size, (IntPtr)p))
				{
					return null;
				}
			}
		}

		return bytes;
	}

	private HbBlob? CreateTableBlob(uint tag)
	{
		if (ReadTable(tag) is not { } bytes)
		{
			return null;
		}

		var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		return new HbBlob(handle.AddrOfPinnedObject(), bytes.Length, HarfBuzzSharp.MemoryMode.ReadOnly, handle.Free);
	}

	private static bool HasColorTables(SKTypeface typeface, out bool colrOnly)
	{
		bool colr = false, other = false;
		foreach (var tag in typeface.GetTableTags())
		{
			colr |= tag == _colrTag;
			other |= tag == _cbdtTag || tag == _sbixTag || tag == _svgTag;
		}

		colrOnly = colr && !other;
		return colr || other;
	}

	internal static uint Tag(string tag) => (uint)((tag[0] << 24) | (tag[1] << 16) | (tag[2] << 8) | tag[3]);
}
