#nullable enable

using System;
using System.Text;

namespace Uno.UI.Runtime.Wayland;

// zwp_text_input_v3 counts offsets in UTF-8 bytes; TextBox counts UTF-16 code units.
internal static class WaylandTextInputText
{
	// The protocol caps surrounding text at 4000 bytes so the request fits a Wayland message.
	internal const int MaxSurroundingTextBytes = 4000;

	// A window of the text around the selection: Start is its UTF-16 index in the full text,
	// Cursor and Anchor are UTF-8 byte offsets inside Text.
	internal readonly record struct SurroundingText(string Text, int Start, int Cursor, int Anchor, bool IsSupported = true);

	internal static SurroundingText GetSurroundingText(string text, int cursor, int anchor, int maxBytes = MaxSurroundingTextBytes)
	{
		cursor = ClampToCodePoint(text, cursor);
		anchor = ClampToCodePoint(text, anchor);
		if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
		{
			return new(text, 0, CharIndexToByteOffset(text, cursor), CharIndexToByteOffset(text, anchor));
		}

		var start = Math.Min(cursor, anchor);
		var end = Math.Max(cursor, anchor);
		var bytes = Encoding.UTF8.GetByteCount(text.AsSpan(start, end - start));
		if (bytes > maxBytes)
		{
			return new(string.Empty, 0, 0, 0, IsSupported: false);
		}

		// Grow around the selection one code point at a time, alternating sides, while it fits.
		var grown = true;
		while (grown)
		{
			grown = false;
			if (start > 0)
			{
				var previous = start - (start >= 2 && char.IsSurrogatePair(text[start - 2], text[start - 1]) ? 2 : 1);
				var size = Encoding.UTF8.GetByteCount(text.AsSpan(previous, start - previous));
				if (bytes + size <= maxBytes)
				{
					start = previous;
					bytes += size;
					grown = true;
				}
			}
			if (end < text.Length)
			{
				var next = end + (end + 1 < text.Length && char.IsSurrogatePair(text[end], text[end + 1]) ? 2 : 1);
				var size = Encoding.UTF8.GetByteCount(text.AsSpan(end, next - end));
				if (bytes + size <= maxBytes)
				{
					end = next;
					bytes += size;
					grown = true;
				}
			}
		}

		var window = text[start..end];
		return new(window, start, CharIndexToByteOffset(window, cursor - start), CharIndexToByteOffset(window, anchor - start));
	}

	internal static int CharIndexToByteOffset(string text, int index)
		=> Encoding.UTF8.GetByteCount(text.AsSpan(0, ClampToCodePoint(text, index)));

	// Offsets beyond the text clamp to its end; an offset inside a UTF-8 sequence maps to the start of its code point.
	internal static int ByteOffsetToCharIndex(string text, int byteOffset)
	{
		var bytes = 0;
		var index = 0;
		while (index < text.Length)
		{
			var length = index + 1 < text.Length && char.IsSurrogatePair(text[index], text[index + 1]) ? 2 : 1;
			var size = Encoding.UTF8.GetByteCount(text.AsSpan(index, length));
			if (bytes + size > byteOffset)
			{
				break;
			}
			bytes += size;
			index += length;
		}
		return index;
	}

	// Resolves delete_surrounding_text, whose lengths count bytes before and after the selection (which is kept),
	// to UTF-16 ranges of the full text.
	internal static ((int Start, int Length) Before, (int Start, int Length) After) GetDeletionRanges(SurroundingText surrounding, uint beforeLength, uint afterLength)
	{
		if (!surrounding.IsSupported)
		{
			return ((0, 0), (0, 0));
		}
		var text = surrounding.Text;
		var selectionStart = Math.Min(surrounding.Cursor, surrounding.Anchor);
		var selectionEnd = Math.Max(surrounding.Cursor, surrounding.Anchor);
		var beforeStart = ByteOffsetToCharIndex(text, (int)Math.Max(0, selectionStart - (long)beforeLength));
		var afterEnd = ByteOffsetToCharIndex(text, (int)Math.Min(int.MaxValue, selectionEnd + (long)afterLength));
		var selectionStartIndex = ByteOffsetToCharIndex(text, selectionStart);
		var selectionEndIndex = ByteOffsetToCharIndex(text, selectionEnd);
		return (
			(surrounding.Start + beforeStart, selectionStartIndex - beforeStart),
			(surrounding.Start + selectionEndIndex, afterEnd - selectionEndIndex));
	}

	private static int ClampToCodePoint(string text, int index)
	{
		index = Math.Clamp(index, 0, text.Length);
		return index > 0 && index < text.Length && char.IsSurrogatePair(text[index - 1], text[index]) ? index - 1 : index;
	}
}
