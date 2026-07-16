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

		// The caret index reached by moving <paramref name="count"/> visual lines up or down from
		// <paramref name="position"/>, preserving the sticky horizontal caret offset. Mirrors the
		// interactive Up/Down arrow logic in TextViewEditor.GetUpDownResult.
		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			target = position;
			unitsMoved = 0;

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
				target = up ? 0 : text.Length;
				unitsMoved = target == position ? 0 : 1;
				return unitsMoved != 0;
			}
			unitsMoved = Math.Abs(newLineIndex - line.lineIndex);

			var x = _caretXOffset;
			if (!TryGetLineStart(displayBlock.ParsedText, text.Length, newLineIndex, out var newLineStart))
			{
				return false;
			}
			var targetLineRect = displayBlock.ParsedText.GetRectForIndex(newLineStart);
			var y = targetLineRect.Y + targetLineRect.Height / 2;
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

		private static bool TryGetLineStart(Documents.IParsedText parsedText, int textLength, int targetLineIndex, out int lineStart)
		{
			lineStart = 0;
			var position = 0;
			while (position <= textLength)
			{
				var line = parsedText.GetLineAt(position);
				if (line.lineIndex == targetLineIndex)
				{
					lineStart = line.start;
					return true;
				}

				var next = line.start + Math.Max(1, line.length);
				if (next <= position || next > textLength)
				{
					break;
				}
				position = next;
			}

			return false;
		}

		internal bool TryGetPageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> TryGetPageTarget(position, up, count, _caretXOffset, out target, out unitsMoved);

		internal bool TryGetRangePageTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				target = position;
				unitsMoved = 0;
				return false;
			}

			var x = displayBlock.ParsedText.GetRectForIndex(Math.Clamp(position, 0, GetPlainTextContent().Length)).X;
			return TryGetPageTarget(position, up, count, x, out target, out unitsMoved);
		}

		private bool TryGetPageTarget(int position, bool up, int count, double x, out int target, out int unitsMoved)
		{
			target = position;
			unitsMoved = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock || _contentElement is not ScrollViewer scrollViewer)
			{
				return false;
			}

			var text = GetPlainTextContent();
			target = Math.Clamp(position, 0, text.Length);
			var viewportHeight = double.IsFinite(scrollViewer.ViewportHeight) && scrollViewer.ViewportHeight > 0
				? scrollViewer.ViewportHeight
				: scrollViewer.ActualHeight;
			for (var i = 0; i < count; i++)
			{
				var rect = displayBlock.ParsedText.GetRectForIndex(target);
				var pageHeight = Math.Max(rect.Height, viewportHeight);
				var y = rect.Y + (up ? -pageHeight : pageHeight);
				var next = Math.Max(0, displayBlock.ParsedText.GetIndexAt(new Point(x, y), true, true));
				if (next == target)
				{
					next = up ? 0 : text.Length;
				}

				if (next == target)
				{
					break;
				}

				target = next;
				unitsMoved++;
			}

			return unitsMoved != 0;
		}

		internal bool TryGetVisibleRange(out int start, out int end)
		{
			start = 0;
			end = 0;
			if (_textBoxView?.DisplayBlock is not { } displayBlock || _contentElement is not ScrollViewer scrollViewer)
			{
				return false;
			}

			var left = scrollViewer.HorizontalOffset;
			var top = scrollViewer.VerticalOffset;
			var viewportWidth = double.IsFinite(scrollViewer.ViewportWidth) && scrollViewer.ViewportWidth > 0
				? scrollViewer.ViewportWidth
				: scrollViewer.ActualWidth;
			var viewportHeight = double.IsFinite(scrollViewer.ViewportHeight) && scrollViewer.ViewportHeight > 0
				? scrollViewer.ViewportHeight
				: scrollViewer.ActualHeight;
			var right = left + Math.Max(0, viewportWidth);
			var bottom = top + Math.Max(0, viewportHeight);
			start = Math.Max(0, displayBlock.ParsedText.GetIndexAt(new Point(left, top), true, false));
			end = Math.Max(start, displayBlock.ParsedText.GetIndexAt(new Point(right, bottom), true, true));
			end = Math.Min(end, GetPlainTextContent().Length);
			return true;
		}
	}
}
