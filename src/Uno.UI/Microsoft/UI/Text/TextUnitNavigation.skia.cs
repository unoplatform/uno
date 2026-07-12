#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.UI.Text
{
	// Text-based (geometry-independent) unit boundary computation for the functional Text Object Model.
	//
	// The Word chunk model follows Uno.UI's ParsedText.GetWordAt while treating Unicode text elements as
	// indivisible. A "word" is a run of letters/digits (or a run of other non-space, non-break text
	// elements) plus its trailing spaces, while \r, \n, \t and \r\n are each their own chunk. Paragraph
	// chunks split on \r, \n and \r\n, and each paragraph includes its trailing break.
	//
	// TODO Uno: Line/Screen/Window units are geometry-based and are handled by the control against the
	// shared DisplayBlock layout, not here. Sentence/Section units are not yet supported.
	internal static class TextUnitNavigation
	{
		internal static int GetTextElementStart(string text, int position)
		{
			if (position <= 0 || position >= text.Length)
			{
				return position;
			}

			var starts = StringInfo.ParseCombiningCharacters(text);
			var index = Array.BinarySearch(starts, position);
			return index >= 0 ? position : starts[Math.Max(0, ~index - 1)];
		}

		internal static int GetTextElementEnd(string text, int position)
		{
			if (position <= 0 || position >= text.Length)
			{
				return position;
			}

			var starts = StringInfo.ParseCombiningCharacters(text);
			var index = Array.BinarySearch(starts, position);
			return index >= 0 ? position : (~index < starts.Length ? starts[~index] : text.Length);
		}

		internal static int[] GetTextElementBoundaries(string text)
		{
			var starts = StringInfo.ParseCombiningCharacters(text);
			var boundaries = new int[starts.Length + 1];
			starts.CopyTo(boundaries, 0);
			boundaries[boundaries.Length - 1] = text.Length;

			return boundaries;
		}

		internal static string TruncateToUtf16Boundary(string text, int maxLength)
		{
			if (maxLength <= 0)
			{
				return string.Empty;
			}

			if (text.Length <= maxLength)
			{
				return text;
			}

			if (char.IsHighSurrogate(text[maxLength - 1]) && char.IsLowSurrogate(text[maxLength]))
			{
				maxLength--;
			}

			return text.Substring(0, maxLength);
		}

		internal static List<(int start, int length)> GetWordChunks(string text)
		{
			var chunks = new List<(int start, int length)>();
			var boundaries = GetTextElementBoundaries(text);
			var elementCount = boundaries.Length - 1;
			var elementIndex = 0;
			while (elementIndex < elementCount)
			{
				var start = boundaries[elementIndex];
				if (text[start] == '\r' && start + 1 < text.Length && text[start + 1] == '\n')
				{
					elementIndex++;
					while (elementIndex < elementCount && boundaries[elementIndex] < start + 2)
					{
						elementIndex++;
					}
				}
				else
				{
					var kind = GetWordTextElementKind(text, start);
					elementIndex++;
					if (kind is WordTextElementKind.Word or WordTextElementKind.Other)
					{
						while (elementIndex < elementCount
							&& GetWordTextElementKind(text, boundaries[elementIndex]) == kind)
						{
							elementIndex++;
						}
					}

					if (kind is WordTextElementKind.Word or WordTextElementKind.Other or WordTextElementKind.Space)
					{
						while (elementIndex < elementCount
							&& GetWordTextElementKind(text, boundaries[elementIndex]) == WordTextElementKind.Space)
						{
							elementIndex++;
						}
					}
				}

				chunks.Add((start, boundaries[elementIndex] - start));
			}

			return chunks;
		}

		private static WordTextElementKind GetWordTextElementKind(string text, int start)
		{
			var first = text[start];
			if (first is '\r' or '\n' or '\t')
			{
				return WordTextElementKind.Break;
			}

			if (first == ' ')
			{
				return WordTextElementKind.Space;
			}

			if (!Rune.TryGetRuneAt(text, start, out var value))
			{
				return WordTextElementKind.Other;
			}

			if (Rune.IsLetterOrDigit(value))
			{
				return WordTextElementKind.Word;
			}

			return Rune.GetUnicodeCategory(value) is UnicodeCategory.NonSpacingMark
				or UnicodeCategory.SpacingCombiningMark
				or UnicodeCategory.EnclosingMark
				or UnicodeCategory.ConnectorPunctuation
				? WordTextElementKind.Word
				: WordTextElementKind.Other;
		}

		private enum WordTextElementKind
		{
			Word,
			Space,
			Break,
			Other,
		}

		internal static List<(int start, int length)> GetParagraphChunks(string text)
		{
			var chunks = new List<(int start, int length)>();
			var length = text.Length;
			var i = 0;
			while (i < length)
			{
				var start = i;
				while (i < length)
				{
					if (text[i] == '\r' && i < length - 1 && text[i + 1] == '\n')
					{
						i += 2;
						break;
					}

					if (text[i] is '\r' or '\n')
					{
						i++;
						break;
					}

					i++;
				}

				chunks.Add((start, i - start));
			}

			return chunks;
		}

		// Returns the text chunks for a unit, or null when the unit is not text-navigable here
		// (Character/Story are handled directly by the caller; geometry units by the control).
		internal static List<(int start, int length)>? GetChunks(string text, global::Microsoft.UI.Text.TextRangeUnit unit)
			=> unit switch
			{
				global::Microsoft.UI.Text.TextRangeUnit.Word => GetWordChunks(text),
				global::Microsoft.UI.Text.TextRangeUnit.Paragraph => GetParagraphChunks(text),
				_ => null,
			};

		// Index of the chunk that contains <paramref name="position"/>. Chunks are contiguous starting at
		// 0, so this is the first chunk whose end is past the position; a position at (or past) the end maps
		// to the last chunk.
		internal static int FindChunkIndex(List<(int start, int length)> chunks, int position)
		{
			for (var i = 0; i < chunks.Count; i++)
			{
				var chunk = chunks[i];
				if (position < chunk.start + chunk.length)
				{
					return i;
				}
			}

			return chunks.Count - 1;
		}
	}
}
