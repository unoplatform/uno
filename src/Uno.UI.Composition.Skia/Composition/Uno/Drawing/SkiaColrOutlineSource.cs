#nullable enable

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The plain glyph outlines of a font whose colour glyphs come from COLR alone, for Skia's CoreText scaler: it treats the font-level
/// colour flag as per glyph, so it gives no outline for any glyph of such a font. A copy of the font without COLR gives them.
/// </summary>
internal sealed class SkiaColrOutlineSource
{
	private static readonly uint _colrTag = SkiaFontFace.Tag("COLR");
	private static readonly uint _cpalTag = SkiaFontFace.Tag("CPAL");
	private static readonly uint _dsigTag = SkiaFontFace.Tag("DSIG");
	private static readonly uint _headTag = SkiaFontFace.Tag("head");
	private static readonly uint _cffTag = SkiaFontFace.Tag("CFF ");
	private static readonly uint _cff2Tag = SkiaFontFace.Tag("CFF2");
	private static readonly uint _ottoTag = SkiaFontFace.Tag("OTTO");
	private static readonly uint _opszTag = SkiaFontFace.Tag("opsz");

	private readonly ulong[] _colorGlyphs;

	private SkiaColrOutlineSource(SKTypeface typeface, ulong[] colorGlyphs)
	{
		Typeface = typeface;
		_colorGlyphs = colorGlyphs;
	}

	/// <summary>The font without its COLR and CPAL tables, for the outlines of its plain glyphs.</summary>
	public SKTypeface Typeface { get; }

	/// <summary>Whether COLR colours <paramref name="glyph"/>, so it draws as a colour glyph rather than from its outline.</summary>
	public bool IsColorGlyph(ushort glyph) => (_colorGlyphs[glyph >> 6] & (1UL << (glyph & 63))) != 0;

	/// <summary>Creates the outline source for <paramref name="typeface"/>.</summary>
	/// <remarks>Returns <c>null</c> when the COLR table is malformed or of an unknown version, or the copy doesn't load as a font of its own
	/// without COLR, so the font draws as it would without this source.</remarks>
	public static SkiaColrOutlineSource? Create(SKTypeface typeface)
	{
		try
		{
			return ReadColorGlyphs(typeface) is { } colorGlyphs && CreateCopyWithoutColr(typeface) is { } copy
				? new SkiaColrOutlineSource(copy, colorGlyphs)
				: null;
		}
		catch
		{
			return null;
		}
	}

	// The glyphs COLR gives colour layers (v0 BaseGlyphRecords) or a paint (v1 BaseGlyphList), as a bit per glyph id.
	private static ulong[]? ReadColorGlyphs(SKTypeface typeface)
	{
		long size = typeface.GetTableSize(_colrTag);
		Span<byte> header = stackalloc byte[18];
		if (size < 14 || !ReadColr(typeface, 0, header[..14]))
		{
			return null;
		}

		var version = BinaryPrimitives.ReadUInt16BigEndian(header);
		if (version > 1 || (version == 1 && (size < 18 || !ReadColr(typeface, 14, header[14..]))))
		{
			return null;
		}

		var glyphs = new ulong[(ushort.MaxValue + 1) / 64];

		// v0 BaseGlyphRecord: glyph id, first layer index, layer count.
		if (!MarkGlyphs(typeface, size, BinaryPrimitives.ReadUInt32BigEndian(header[4..]), BinaryPrimitives.ReadUInt16BigEndian(header[2..]), glyphs))
		{
			return null;
		}

		// v1 BaseGlyphList: a 32-bit count, then a BaseGlyphPaintRecord (glyph id, paint offset) per glyph.
		if (version == 1 && BinaryPrimitives.ReadUInt32BigEndian(header[14..]) is var listOffset and not 0)
		{
			Span<byte> count = stackalloc byte[4];
			if (listOffset + 4L > size || !ReadColr(typeface, (int)listOffset, count)
				|| !MarkGlyphs(typeface, size, listOffset + 4L, BinaryPrimitives.ReadUInt32BigEndian(count), glyphs))
			{
				return null;
			}
		}

		return glyphs;
	}

	// Marks the glyph ids of `count` 6-byte records that start with one, at `offset` in COLR.
	private static bool MarkGlyphs(SKTypeface typeface, long tableSize, long offset, long count, ulong[] glyphs)
	{
		// No records to read (TryGetTableData fails for a length of 0).
		if (count == 0)
		{
			return true;
		}

		var length = count * 6;
		if (offset + length > tableSize)
		{
			return false;
		}

		var records = ArrayPool<byte>.Shared.Rent((int)length);
		try
		{
			var span = records.AsSpan(0, (int)length);
			if (!ReadColr(typeface, (int)offset, span))
			{
				return false;
			}

			for (var i = 0; i < span.Length; i += 6)
			{
				var glyph = BinaryPrimitives.ReadUInt16BigEndian(span[i..]);
				glyphs[glyph >> 6] |= 1UL << (glyph & 63);
			}

			return true;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(records);
		}
	}

	private static unsafe bool ReadColr(SKTypeface typeface, int offset, Span<byte> destination)
	{
		fixed (byte* p = destination)
		{
			return typeface.TryGetTableData(_colrTag, offset, destination.Length, (IntPtr)p);
		}
	}

	// The font's tables but COLR, CPAL and DSIG (whose signature no longer matches) in an sfnt of their own, loaded as a typeface at the
	// original's variation position. Null when CoreText resolves it back to a font with COLR, e.g. by its PostScript name.
	private static SKTypeface? CreateCopyWithoutColr(SKTypeface typeface)
	{
		var allTags = typeface.GetTableTags();
		var tags = new uint[allTags.Length];
		var sizes = new int[allTags.Length];
		var count = 0;
		var isCff = false;
		long fileSize = 0;
		foreach (var tag in allTags)
		{
			if (tag != _colrTag && tag != _cpalTag && tag != _dsigTag && typeface.GetTableSize(tag) is var size and > 0)
			{
				tags[count] = tag;
				sizes[count++] = size;
				fileSize += Align4(size);
				isCff |= tag == _cffTag || tag == _cff2Tag;
			}
		}

		// The table directory is searched by tag, so it is sorted by tag.
		Array.Sort(tags, sizes, 0, count);
		var directorySize = 12 + 16 * count;
		fileSize += directorySize;

		using var data = SKData.Create(checked((int)fileSize));
		unsafe
		{
			var sfnt = new Span<byte>((void*)data.Data, (int)fileSize);
			var entrySelector = BitOperations.Log2((uint)count);
			var searchRange = (1 << entrySelector) * 16;
			BinaryPrimitives.WriteUInt32BigEndian(sfnt, isCff ? _ottoTag : 0x00010000);
			BinaryPrimitives.WriteUInt16BigEndian(sfnt[4..], (ushort)count);
			BinaryPrimitives.WriteUInt16BigEndian(sfnt[6..], (ushort)searchRange);
			BinaryPrimitives.WriteUInt16BigEndian(sfnt[8..], (ushort)entrySelector);
			BinaryPrimitives.WriteUInt16BigEndian(sfnt[10..], (ushort)(count * 16 - searchRange));

			// head's checksum counts its checkSumAdjustment as 0. The adjustment itself stays the original's: CoreText doesn't validate it.
			var offset = directorySize;
			for (var i = 0; i < count; i++)
			{
				var table = sfnt.Slice(offset, Align4(sizes[i]));
				table[sizes[i]..].Clear();
				if (!typeface.TryGetTableData(tags[i], 0, sizes[i], (IntPtr)((byte*)data.Data + offset)))
				{
					return null;
				}

				var checksum = Checksum(table);
				if (tags[i] == _headTag && sizes[i] >= 12)
				{
					checksum -= BinaryPrimitives.ReadUInt32BigEndian(table[8..]);
				}

				var record = sfnt[(12 + 16 * i)..];
				BinaryPrimitives.WriteUInt32BigEndian(record, tags[i]);
				BinaryPrimitives.WriteUInt32BigEndian(record[4..], checksum);
				BinaryPrimitives.WriteUInt32BigEndian(record[8..], (uint)offset);
				BinaryPrimitives.WriteUInt32BigEndian(record[12..], (uint)sizes[i]);
				offset += table.Length;
			}
		}

		if (SKTypeface.FromData(data, 0) is not { } copy)
		{
			return null;
		}

		// CoreText sets the optical size from the text size unless opsz is set explicitly. Font creation never sets it on the original
		// (it leaves opsz at its default), so to keep that, the copy takes the original's variation position except opsz.
		if (typeface.VariationDesignPosition is { Length: > 0 } position
			&& Array.FindAll(position, c => c.Axis != _opszTag) is { Length: > 0 } axes)
		{
			var positioned = copy.Clone(axes);
			if (!ReferenceEquals(positioned, copy))
			{
				copy.Dispose();
			}

			if (positioned is null)
			{
				return null;
			}

			copy = positioned;
		}

		if (copy.GetTableSize(_colrTag) != 0)
		{
			copy.Dispose();
			return null;
		}

		return copy;
	}

	private static int Align4(int size) => (size + 3) & ~3;

	// The sfnt table checksum: the sum of the table's big-endian 32-bit words, zero padding included.
	private static uint Checksum(ReadOnlySpan<byte> table)
	{
		uint sum = 0;
		foreach (var word in MemoryMarshal.Cast<byte, uint>(table))
		{
			sum += BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(word) : word;
		}

		return sum;
	}
}
