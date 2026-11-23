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

	private readonly object _hbGate = new();
	private HbFont? _hbFont;
	private ulong[]? _asciiCoverage;

	public SkiaFontFace(SKTypeface typeface)
	{
		Typeface = typeface;
		HasColorGlyphs = HasColorTables(typeface);
	}

	public SKTypeface Typeface { get; }

	public bool HasColorGlyphs { get; }

	public HbFont HarfBuzzFont
	{
		get
		{
			if (Volatile.Read(ref _hbFont) is { } existing)
			{
				return existing;
			}

			lock (_hbGate)
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

	private static bool HasColorTables(SKTypeface typeface)
	{
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
