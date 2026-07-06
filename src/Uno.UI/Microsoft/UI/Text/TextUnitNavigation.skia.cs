#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	// Text-based (geometry-independent) unit boundary computation for the functional Text Object Model.
	//
	// The Word chunk model is copied VERBATIM from Uno.UI's ParsedText.GetWordAt so programmatic Word
	// navigation (ITextRange.Move/Expand/StartOf/EndOf/GetIndex with TextRangeUnit.Word) matches the
	// interactive Ctrl+Arrow navigation exactly — a "word" is a run of letters/digits (or a run of other
	// non-space, non-break characters) plus its trailing spaces, while \r, \n, \t and \r\n are each their
	// own chunk. Paragraph chunks split on \r, \n and \r\n, and each paragraph includes its trailing break.
	//
	// TODO Uno: Line/Screen/Window units are geometry-based and are handled by the control against the
	// shared DisplayBlock layout, not here. Sentence/Section units are not yet supported.
	internal static class TextUnitNavigation
	{
		internal static List<(int start, int length)> GetWordChunks(string text)
		{
			var chunks = new List<(int start, int length)>();
			var length = text.Length;
			for (var i = 0; i < length;)
			{
				var start = i;
				var c = text[i];
				if (c is '\r' && i < (length - 1) && text[i + 1] == '\n')
				{
					i += 2;
				}
				else if (c is '\r' or '\t' or '\n')
				{
					i++;
				}
				else if (c == ' ')
				{
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}
				else if (char.IsLetterOrDigit(text[i]))
				{
					while (i < length && char.IsLetterOrDigit(text[i]))
					{
						i++;
					}
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}
				else
				{
					while (i < length && !char.IsLetterOrDigit(text[i]) && text[i] != ' ' && text[i] != '\r')
					{
						i++;
					}
					while (i < length && text[i] == ' ')
					{
						i++;
					}
				}

				chunks.Add((start, i - start));
			}

			return chunks;
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
