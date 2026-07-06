#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	// Geometry-backed helpers for the functional Text Object Model's Line-unit and vertical navigation
	// (ITextRange.StartOf/EndOf/Expand/GetIndex with TextRangeUnit.Line and ITextSelection.MoveUp/
	// MoveDown/HomeKey/EndKey). These delegate to the shared DisplayBlock layout so programmatic line
	// navigation matches the interactive keyboard behaviour (Home/End/Up/Down) exactly. All helpers
	// no-op (return false / 0) when the view is not yet laid out.
	partial class RichEditBox
	{
		// The [lineStart, lineEnd) of the visual line containing <paramref name="position"/>, where
		// lineEnd stops before a trailing carriage return (matching the interactive End key), plus the
		// line's index and whether it is the last line.
		internal bool TryGetLineBounds(int position, out int lineStart, out int lineEnd, out int lineIndex, out bool isLast)
		{
			lineStart = position;
			lineEnd = position;
			lineIndex = 0;
			isLast = true;

			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			position = Math.Clamp(position, 0, text.Length);
			var line = displayBlock.ParsedText.GetLineAt(position);
			lineStart = line.start;
			lineEnd = line.start + line.length;

			// A newline belongs to the line before it, but the line's logical end stops before the \r.
			if (line.length > 0 && lineEnd > 0 && lineEnd <= text.Length && text[lineEnd - 1] == '\r')
			{
				lineEnd--;
			}

			lineIndex = line.lineIndex;
			isLast = line.lastLine;
			return true;
		}

		// The number of visual lines in the document, or 0 when the view is not laid out.
		internal int GetLineCountForTom()
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return 0;
			}

			var text = GetPlainTextContent();
			return displayBlock.ParsedText.GetLineAt(text.Length).lineIndex + 1;
		}

		// The caret index reached by moving <paramref name="count"/> visual lines up or down from
		// <paramref name="position"/>, preserving the sticky horizontal caret offset. Mirrors the
		// interactive Up/Down arrow logic in TextViewEditor.GetUpDownResult.
		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target)
		{
			target = position;

			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var text = GetPlainTextContent();
			position = Math.Clamp(position, 0, text.Length);
			var line = displayBlock.ParsedText.GetLineAt(position);
			var lineCount = displayBlock.ParsedText.GetLineAt(text.Length).lineIndex + 1;

			var newLineIndex = up ? line.lineIndex - count : line.lineIndex + count;
			newLineIndex = Math.Clamp(newLineIndex, 0, lineCount - 1);
			if (newLineIndex == line.lineIndex)
			{
				// Already at the first/last line: WinUI moves the caret to the story start/end.
				target = up ? 0 : text.Length;
				return target != position;
			}

			var rect = displayBlock.ParsedText.GetRectForIndex(position);
			var x = _caretXOffset;
			var y = (newLineIndex + 0.5) * rect.Height;
			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(new Point(x, y), true, true));
			var newLine = displayBlock.ParsedText.GetLineAt(index);
			if (text.Length > index - 1
				&& newLine.length > 1
				&& index - 1 >= 0
				&& index == newLine.start + newLine.length
				&& (text[index - 1] == '\r' || text[index - 1] == ' '))
			{
				// If we landed just past a \r or trailing space, we are really at the next line's start.
				index--;
			}

			target = index;
			return true;
		}
	}
}
