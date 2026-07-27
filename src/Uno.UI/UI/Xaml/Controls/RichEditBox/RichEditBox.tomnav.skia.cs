#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
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
		private readonly List<VisualLineInfo> _visualLineIndex = new();
		private TextBlock? _visualLineIndexBlock;
		private IParsedText? _visualLineParsedText;
		private TextRangeUnitBoundarySet? _visualLineUnitBoundaries;
		private long _visualLineTextVersion = -1;
		private long _visualLineWidthBits;
		private int _visualLineIndexRebuildCount;

		private readonly record struct VisualLineInfo(
			int Start,
			int End,
			int LineIndex,
			bool IsLast,
			Rect Bounds,
			double Baseline);

		internal int VisualLineIndexRebuildCount => _visualLineIndexRebuildCount;

		private bool TryGetInteractiveUpDownResult(
			int selectionStart,
			int selectionLength,
			bool shift,
			bool ctrl,
			bool up,
			out int target)
		{
			var activePosition = GetInteractiveVerticalPosition(selectionStart, selectionLength, shift, up);
			if (ctrl)
			{
				var paragraphs = Document.GetUnitBoundaries(global::Microsoft.UI.Text.TextRangeUnit.Paragraph);
				if (paragraphs is null)
				{
					target = activePosition;
					return false;
				}

				target = paragraphs.Move(activePosition, up ? -1 : 1, out _);
				return true;
			}

			return TryGetVerticalTarget(activePosition, up, 1, out target, out _);
		}

		private static int GetInteractiveVerticalPosition(
			int selectionStart,
			int selectionLength,
			bool shift,
			bool up)
		{
			if (shift || up && selectionLength < 0 || !up && selectionLength > 0)
			{
				return selectionStart + selectionLength;
			}

			return selectionStart;
		}

		internal bool TryGetLineLayoutStamp(out long layoutVersion, out double width)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock || displayBlock.TextLayoutVersion == 0)
			{
				layoutVersion = -1;
				width = double.NaN;
				return false;
			}

			layoutVersion = displayBlock.TextLayoutVersion;
			width = displayBlock.TextLayoutWidth;
			return true;
		}

		internal TextRangeUnitBoundarySet? GetVisualLineUnitBoundaries()
			=> EnsureVisualLineIndex() ? _visualLineUnitBoundaries : null;

		// The [lineStart, lineEnd) of the visual line containing <paramref name="position"/>, where
		// lineEnd stops before a trailing carriage return (matching the interactive End key), plus the
		// line's index and whether it is the last line.
		internal bool TryGetLineBounds(int position, out int lineStart, out int lineEnd, out int lineIndex, out bool isLast)
			=> TryGetLineBounds(position, atEndOfLine: false, out lineStart, out lineEnd, out lineIndex, out isLast);

		internal bool TryGetLineBounds(
			int position,
			bool atEndOfLine,
			out int lineStart,
			out int lineEnd,
			out int lineIndex,
			out bool isLast)
		{
			lineStart = position;
			lineEnd = position;
			lineIndex = 0;
			isLast = true;

			if (!TryGetVisualLine(position, atEndOfLine, out var line))
			{
				return false;
			}

			lineStart = line.Start;
			lineEnd = line.End;
			lineIndex = line.LineIndex;
			isLast = line.IsLast;
			return true;
		}

		internal bool IsVisualLineEnd(int position)
		{
			if (position == 0 || !TryGetVisualLine(position, atEndOfLine: true, out var line))
			{
				return false;
			}

			return Math.Clamp(position, 0, GetPlainTextLength()) == line.End;
		}

		// The caret index reached by moving <paramref name="count"/> visual lines up or down from
		// <paramref name="position"/>, preserving the sticky horizontal caret offset. Mirrors the
		// interactive Up/Down arrow logic in TextViewEditor.GetUpDownResult.
		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			double? desiredX = _caretXOffset;
			return TryGetVerticalTarget(
				position,
				up,
				count,
				atEndOfLine: false,
				ref desiredX,
				out target,
				out unitsMoved,
				out _);
		}

		internal bool TryGetVerticalTarget(
			int position,
			bool up,
			int count,
			bool atEndOfLine,
			ref double? desiredX,
			out int target,
			out int unitsMoved,
			out bool targetAtEndOfLine)
		{
			target = position;
			unitsMoved = 0;
			targetAtEndOfLine = atEndOfLine;

			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			position = Math.Clamp(position, 0, textLength);
			if (!TryGetVisualLine(position, atEndOfLine, out var line))
			{
				return false;
			}
			var lineCount = _visualLineIndex.Count;

			var newLineIndex = up ? line.LineIndex - count : line.LineIndex + count;
			newLineIndex = Math.Clamp(newLineIndex, 0, lineCount - 1);
			if (newLineIndex == line.LineIndex)
			{
				if (atEndOfLine && up)
				{
					return false;
				}

				target = up ? 0 : textLength;
				unitsMoved = target == position ? 0 : 1;
				targetAtEndOfLine = !up && target == textLength;
				return unitsMoved != 0;
			}
			unitsMoved = Math.Abs(newLineIndex - line.LineIndex);

			desiredX ??= GetCaretX(displayBlock, position, atEndOfLine);
			var x = desiredX.Value;
			var targetLine = _visualLineIndex[newLineIndex];
			var y = GetLineHitTestY(targetLine);
			var index = Math.Max(0, displayBlock.ParsedText.GetIndexAt(new Point(x, y), true, true));
			var newLine = displayBlock.ParsedText.GetLineAt(index);
			if (textLength > index - 1
				&& newLine.length > 1
				&& index - 1 >= 0
				&& index == newLine.start + newLine.length
				&& (Document.GetCharacterAt(index - 1) == '\r' || Document.GetCharacterAt(index - 1) == ' '))
			{
				// If we landed just past a \r or trailing space, we are really at the next line's start.
				index--;
			}

			target = index;
			targetAtEndOfLine = IsVisualLineEnd(index);
			return true;
		}

		private static double GetLineHitTestY(VisualLineInfo line)
		{
			if (line.Bounds.Height <= 0)
			{
				return line.Bounds.Y;
			}

			const double edgeInset = 0.01;
			return Math.Clamp(
				line.Bounds.Top + line.Bounds.Height / 2,
				line.Bounds.Top + edgeInset,
				Math.Max(line.Bounds.Top + edgeInset, line.Bounds.Bottom - edgeInset));
		}

		private static double GetCaretX(TextBlock displayBlock, int position, bool atEndOfLine)
		{
			position = Math.Max(0, position);
			var index = atEndOfLine && position > 0 ? position - 1 : position;
			var rect = displayBlock.ParsedText.GetRectForIndex(index);
			var y = rect.Y + rect.Height / 2;
			var left = rect.X;
			var right = rect.X + rect.Width;
			if (displayBlock.ParsedText.GetIndexAt(new Point(left, y), true, true) == position)
			{
				return left;
			}

			if (displayBlock.ParsedText.GetIndexAt(new Point(right, y), true, true) == position)
			{
				return right;
			}

			return atEndOfLine ? right : left;
		}

		private bool TryGetVisualLine(int position, bool atEndOfLine, out VisualLineInfo line)
		{
			line = default;
			if (!EnsureVisualLineIndex())
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			var probe = Math.Clamp(atEndOfLine && position > 0 ? position - 1 : position, 0, textLength);
			var low = 0;
			var high = _visualLineIndex.Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_visualLineIndex[middle].Start <= probe)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			line = _visualLineIndex[Math.Max(0, low - 1)];
			return true;
		}

		private bool EnsureVisualLineIndex()
		{
			var textVersion = _document?.TextVersion ?? 0;
			if (_visualLineTextVersion != textVersion)
			{
				UpdateLayout();
			}

			if (_textBoxView?.DisplayBlock is not { } displayBlock || displayBlock.TextLayoutVersion == 0)
			{
				return false;
			}

			var textLength = GetPlainTextLength();
			var widthBits = BitConverter.DoubleToInt64Bits(displayBlock.TextLayoutWidth);
			if (ReferenceEquals(_visualLineIndexBlock, displayBlock)
				&& ReferenceEquals(_visualLineParsedText, displayBlock.ParsedText)
				&& _visualLineTextVersion == textVersion
				&& _visualLineWidthBits == widthBits)
			{
				return _visualLineIndex.Count > 0;
			}

			_visualLineIndex.Clear();
			_visualLineUnitBoundaries = null;
			_visualLineIndexBlock = displayBlock;
			_visualLineParsedText = displayBlock.ParsedText;
			_visualLineTextVersion = textVersion;
			_visualLineWidthBits = widthBits;
			_visualLineIndexRebuildCount++;

			var parsedText = displayBlock.ParsedText;
			for (var lineIndex = 0; lineIndex < parsedText.VisualLineCount; lineIndex++)
			{
				var parsedLine = parsedText.GetVisualLine(lineIndex);
				var rawEnd = parsedLine.Start + parsedLine.Length;
				var lineEnd = rawEnd - Document.GetHardLineBreakLengthEndingAt(rawEnd);
				_visualLineIndex.Add(new VisualLineInfo(
					parsedLine.Start,
					lineEnd,
					parsedLine.LineIndex,
					parsedLine.IsLast,
					parsedLine.Bounds,
					parsedLine.Baseline));
			}

			if (_visualLineIndex.Count == 0)
			{
				return false;
			}

			var spans = new TextRangeUnitSpan[_visualLineIndex.Count];
			for (var i = 0; i < spans.Length; i++)
			{
				var containmentEnd = i + 1 < spans.Length
					? _visualLineIndex[i + 1].Start
					: textLength + 1;
				spans[i] = new TextRangeUnitSpan(
					_visualLineIndex[i].Start,
					containmentEnd,
					containmentEnd,
					_visualLineIndex[i].End);
			}
			_visualLineUnitBoundaries = new TextRangeUnitBoundarySet(spans);
			return true;
		}

		internal bool TryGetPageTarget(int position, bool up, int count, out int target, out int unitsMoved)
			=> TryGetPageTarget(position, up, count, _caretXOffset, out target, out unitsMoved);

		internal bool TryGetPageTarget(
			int position,
			bool up,
			int count,
			bool atEndOfLine,
			ref double? desiredX,
			out int target,
			out int unitsMoved,
			out bool targetAtEndOfLine)
		{
			targetAtEndOfLine = atEndOfLine;
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				target = position;
				unitsMoved = 0;
				return false;
			}

			desiredX ??= GetCaretX(displayBlock, Math.Clamp(position, 0, GetPlainTextLength()), atEndOfLine);
			var moved = TryGetPageTarget(position, up, count, desiredX.Value, out target, out unitsMoved);
			if (moved)
			{
				targetAtEndOfLine = IsVisualLineEnd(target);
			}
			return moved;
		}

		internal bool TryGetRangePageTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			if (_textBoxView?.DisplayBlock is not { } displayBlock)
			{
				target = position;
				unitsMoved = 0;
				return false;
			}

			var x = displayBlock.ParsedText.GetRectForIndex(Math.Clamp(position, 0, GetPlainTextLength())).X;
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

			var textLength = GetPlainTextLength();
			target = Math.Clamp(position, 0, textLength);
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
					next = up ? 0 : textLength;
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
			if (!double.IsFinite(viewportWidth)
				|| viewportWidth <= 0
				|| !double.IsFinite(viewportHeight)
				|| viewportHeight <= 0)
			{
				return false;
			}
			var right = left + Math.Max(0, viewportWidth);
			var bottom = top + Math.Max(0, viewportHeight);
			start = Math.Max(0, displayBlock.ParsedText.GetIndexAt(new Point(left, top), true, false));
			end = Math.Max(start, displayBlock.ParsedText.GetIndexAt(new Point(right, bottom), true, true));
			end = Math.Min(end, GetPlainTextLength());
			return true;
		}
	}
}
