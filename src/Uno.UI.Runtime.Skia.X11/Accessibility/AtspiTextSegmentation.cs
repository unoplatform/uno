#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Offset mapping and text segmentation for the AT-SPI Text interface. AT-SPI offsets
/// count Unicode characters (code points) while .NET strings index UTF-16 code units, so
/// every offset crossing the bus goes through <see cref="Utf16Index"/> or
/// <see cref="CharacterCount"/>. Ranges are half-open [start, end) in characters.
/// </summary>
internal static class AtspiTextSegmentation
{
	// AtspiTextGranularity
	private const uint GranularityChar = 0;
	private const uint GranularityWord = 1;

	// AtspiTextBoundaryType
	private const uint BoundaryChar = 0;
	private const uint BoundaryWordStart = 1;
	private const uint BoundaryWordEnd = 2;

	/// <summary>Maps a character offset to a UTF-16 index without splitting a surrogate pair; clamps to the end.</summary>
	public static int Utf16Index(string s, int characterOffset)
	{
		var i = 0;
		for (; characterOffset > 0 && i < s.Length; characterOffset--)
		{
			i += char.IsSurrogatePair(s, i) ? 2 : 1;
		}
		return i;
	}

	/// <summary>Counts the characters before a UTF-16 index (clamped to the string).</summary>
	public static int CharacterCount(string s, int utf16Length)
	{
		var count = 0;
		var limit = Math.Min(utf16Length, s.Length);
		for (var i = 0; i < limit; i += char.IsSurrogatePair(s, i) ? 2 : 1)
		{
			count++;
		}
		return count;
	}

	/// <summary>Text.GetStringAtOffset: char, word (start to next word start) or line for anything coarser.</summary>
	public static (int Start, int End) GetStringAtOffset(string text, int offset, uint granularity)
	{
		var runes = ToRunes(text);
		return granularity switch
		{
			GranularityChar => CharAt(runes, offset),
			GranularityWord => WordStartRange(runes, offset),
			_ => LineAt(runes, offset),
		};
	}

	/// <summary>Text.GetTextAtOffset; sentences are approximated by lines.</summary>
	public static (int Start, int End) GetTextAtOffset(string text, int offset, uint boundary)
		=> At(ToRunes(text), offset, boundary);

	public static (int Start, int End) GetTextBeforeOffset(string text, int offset, uint boundary)
	{
		var runes = ToRunes(text);
		var (start, _) = At(runes, offset, boundary);
		return start > 0 ? At(runes, start - 1, boundary) : (0, 0);
	}

	public static (int Start, int End) GetTextAfterOffset(string text, int offset, uint boundary)
	{
		var runes = ToRunes(text);
		var (_, end) = At(runes, offset, boundary);
		return end < runes.Length ? At(runes, end, boundary) : (runes.Length, runes.Length);
	}

	private static (int Start, int End) At(Rune[] runes, int offset, uint boundary) => boundary switch
	{
		BoundaryChar => CharAt(runes, offset),
		BoundaryWordStart => WordStartRange(runes, offset),
		BoundaryWordEnd => WordEndRange(runes, offset),
		_ => LineAt(runes, offset),
	};

	private static Rune[] ToRunes(string text) => text.EnumerateRunes().ToArray();

	private static int Clamp(Rune[] runes, int offset) => Math.Clamp(offset, 0, runes.Length);

	private static bool IsWordRune(Rune rune)
		=> Rune.IsLetterOrDigit(rune)
			|| rune.Value == '_'
			|| Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;

	private static (int Start, int End) CharAt(Rune[] runes, int offset)
	{
		var o = Clamp(runes, offset);
		return o < runes.Length ? (o, o + 1) : (o, o);
	}

	// From the start of the word at (or before) the offset to the start of the next word.
	private static (int Start, int End) WordStartRange(Rune[] runes, int offset)
	{
		var start = Clamp(runes, offset);
		if (start == runes.Length || !IsWordRune(runes[start]))
		{
			while (start > 0 && !IsWordRune(runes[start - 1]))
			{
				start--;
			}
		}
		while (start > 0 && IsWordRune(runes[start - 1]))
		{
			start--;
		}

		var end = start;
		while (end < runes.Length && IsWordRune(runes[end]))
		{
			end++;
		}
		while (end < runes.Length && !IsWordRune(runes[end]))
		{
			end++;
		}
		return (start, end);
	}

	// From the end of the previous word to the end of the word at (or after) the offset.
	private static (int Start, int End) WordEndRange(Rune[] runes, int offset)
	{
		var end = Clamp(runes, offset);
		while (end < runes.Length && !IsWordRune(runes[end]))
		{
			end++;
		}
		while (end < runes.Length && IsWordRune(runes[end]))
		{
			end++;
		}

		var start = Clamp(runes, offset);
		while (start > 0 && IsWordRune(runes[start - 1]))
		{
			start--;
		}
		while (start > 0 && !IsWordRune(runes[start - 1]))
		{
			start--;
		}
		return (start, end);
	}

	// The line holding the offset, including its terminating newline.
	private static (int Start, int End) LineAt(Rune[] runes, int offset)
	{
		var o = Clamp(runes, offset);
		var start = o;
		while (start > 0 && runes[start - 1].Value != '\n')
		{
			start--;
		}
		var end = o;
		while (end < runes.Length && runes[end].Value != '\n')
		{
			end++;
		}
		if (end < runes.Length)
		{
			end++;
		}
		return (start, end);
	}
}
