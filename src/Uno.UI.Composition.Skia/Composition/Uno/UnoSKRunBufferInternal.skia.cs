using System;
using SkiaSharp;

namespace SkiaSharp;

internal struct UnoSKRunBufferInternal : IEquatable<UnoSKRunBufferInternal>
{
	public unsafe void* glyphs;

	public unsafe void* pos;

	public unsafe void* utf8text;

	public unsafe void* clusters;

	public unsafe readonly bool Equals(UnoSKRunBufferInternal obj)
	{
		if (glyphs == obj.glyphs && pos == obj.pos && utf8text == obj.utf8text)
		{
			return clusters == obj.clusters;
		}

		return false;
	}

	public override readonly bool Equals(object obj)
	{
		if (obj is UnoSKRunBufferInternal obj2)
		{
			return Equals(obj2);
		}

		return false;
	}

	public static bool operator ==(UnoSKRunBufferInternal left, UnoSKRunBufferInternal right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UnoSKRunBufferInternal left, UnoSKRunBufferInternal right)
	{
		return !left.Equals(right);
	}

	public unsafe override readonly int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(glyphs == null ? 0 : (IntPtr)glyphs);
		hashCode.Add(pos == null ? 0 : (IntPtr)pos);
		hashCode.Add(utf8text == null ? 0 : (IntPtr)utf8text);
		hashCode.Add(clusters == null ? 0 : (IntPtr)clusters);
		return hashCode.ToHashCode();
	}

	public unsafe Span<SKPoint> GetPositionSpan(int size)
	{
		return new Span<SKPoint>(pos, (pos != null) ? size : 0);
	}

	public unsafe Span<ushort> GetGlyphSpan(int size)
	{
		return new Span<ushort>(glyphs, (glyphs != null) ? size : 0);
	}
}
