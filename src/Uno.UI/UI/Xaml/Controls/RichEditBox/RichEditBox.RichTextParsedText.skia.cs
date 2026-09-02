#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	private sealed class RichParagraphLayoutCacheEntry
	{
		internal RichParagraphLayoutCacheEntry(
			int start,
			int length,
			List<RenderFragmentSpec> specs,
			ParagraphListMarkerState listStateBefore)
		{
			Start = start;
			Length = length;
			Specs = specs;
			ListStateBefore = listStateBefore;
		}

		internal int Start;
		internal int Length;
		internal int End => Start + Length;
		internal List<RenderFragmentSpec> Specs { get; }
		internal ParagraphListMarkerState ListStateBefore { get; }
		internal IParsedText? ParsedText { get; private set; }
		internal Size Size { get; private set; }
		private RichParagraphCachedLayout? _firstLayout;
		private RichParagraphCachedLayout? _secondLayout;
		private RichParagraphLayoutKey? _activeKey;

		internal bool TrySelectLayout(RichParagraphLayoutKey key, out bool selectionChanged)
		{
			if (TrySelect(_firstLayout, key, out selectionChanged)
				|| TrySelect(_secondLayout, key, out selectionChanged))
			{
				return true;
			}

			selectionChanged = false;
			return false;
		}

		internal void StoreLayout(
			RichParagraphLayoutKey key,
			IParsedText parsedText,
			Size size)
		{
			var layout = new RichParagraphCachedLayout(key, parsedText, size);
			if (_firstLayout is null)
			{
				_firstLayout = layout;
			}
			else if (_secondLayout is null)
			{
				_secondLayout = layout;
			}
			else
			{
				_firstLayout = _secondLayout;
				_secondLayout = layout;
			}

			_activeKey = key;
			ParsedText = parsedText;
			Size = size;
		}

		internal void InvalidateLayouts()
		{
			_firstLayout = null;
			_secondLayout = null;
			_activeKey = null;
			ParsedText = null;
			Size = default;
		}

		private bool TrySelect(
			RichParagraphCachedLayout? layout,
			RichParagraphLayoutKey key,
			out bool selectionChanged)
		{
			if (layout is not { } value || !value.Key.Equals(key))
			{
				selectionChanged = false;
				return false;
			}

			selectionChanged = !_activeKey.Equals(key);
			_activeKey = key;
			ParsedText = value.ParsedText;
			Size = value.Size;
			return true;
		}
	}

	private readonly record struct RichParagraphCachedLayout(
		RichParagraphLayoutKey Key,
		IParsedText ParsedText,
		Size Size);

	private readonly record struct RichParagraphLayoutKey(
		double AvailableWidth,
		string DefaultFontFamily,
		double DefaultFontSize,
		global::Windows.UI.Text.FontWeight DefaultFontWeight,
		global::Windows.UI.Text.FontStyle DefaultFontStyle,
		global::Windows.UI.Text.FontStretch DefaultFontStretch,
		int DefaultCharacterSpacing,
		bool IsTextScaleFactorEnabled,
		float DefaultSkFontSize,
		float DefaultSkFontScaleX,
		double LineHeight,
		LineStackingStrategy LineStackingStrategy,
		FlowDirection FlowDirection,
		TextAlignment? TextAlignment,
		TextWrapping TextWrapping,
		TextTrimming TextTrimming,
		bool IsSpellCheckEnabled,
		float DefaultTabStop,
		ParagraphLayoutInfo? EndingParagraphLayout,
		TextAlignment? EndingParagraphAlignment,
		Brush? DefaultForeground,
		bool AlignmentIncludesTrailingWhitespace,
		bool IgnoreTrailingCharacterSpacing);

	private sealed class RichTextParsedText : IParsedText
	{
		private static readonly IReadOnlyList<TextHighlighter> _noHighlighters = Array.Empty<TextHighlighter>();
		private readonly Paragraph[] _paragraphs;
		private readonly int _textLength;
		private readonly int _visualLineCount;

		internal RichTextParsedText(IReadOnlyList<RichParagraphLayoutCacheEntry> paragraphs)
		{
			_paragraphs = new Paragraph[paragraphs.Count];
			double top = 0;
			double width = 0;
			var lineIndex = 0;
			for (var i = 0; i < paragraphs.Count; i++)
			{
				var source = paragraphs[i];
				var parsedText = source.ParsedText!;
				_paragraphs[i] = new Paragraph(
					source.Start,
					source.Length,
					top,
					source.Size,
					lineIndex,
					parsedText);
				top += source.Size.Height;
				width = Math.Max(width, source.Size.Width);
				lineIndex += parsedText.VisualLineCount;
			}

			_textLength = paragraphs.Count == 0 ? 0 : paragraphs[^1].End;
			_visualLineCount = Math.Max(1, lineIndex);
			Size = new Size(width, top);
		}

		internal Size Size { get; }

		public bool IsBaseDirectionRightToLeft
			=> _paragraphs.Length > 0 && _paragraphs[0].ParsedText.IsBaseDirectionRightToLeft;

		public int VisualLineCount => _visualLineCount;

		public void Draw(
			in Visual.PaintingSession session,
			(int index, CompositionBrush brush, float thickness)? caret,
			IEnumerable<TextHighlighter> highlighters,
			(int startIndex, int length)? compositionRange)
		{
			var highlighterList = highlighters as IReadOnlyList<TextHighlighter>
				?? new List<TextHighlighter>(highlighters);
			var caretParagraph = caret is null ? -1 : FindParagraphForIndex(caret.Value.index);
			var clip = session.Canvas.LocalClipBounds;
			var firstVisibleParagraph = FindFirstParagraphEndingAfter(clip.Top);
			for (var i = firstVisibleParagraph; i < _paragraphs.Length; i++)
			{
				var paragraph = _paragraphs[i];
				if (paragraph.Top >= clip.Bottom)
				{
					break;
				}
				(int index, CompositionBrush brush, float thickness)? localCaret = null;
				if (i == caretParagraph && caret is { } caretValue)
				{
					localCaret = (
						caretValue.index - paragraph.Start,
						caretValue.brush,
						caretValue.thickness);
				}
				var localComposition = GetLocalRange(
					compositionRange,
					paragraph.Start,
					paragraph.End);
				var localHighlighters = GetLocalHighlighters(
					highlighterList,
					paragraph.Start,
					paragraph.End);

				session.Canvas.Save();
				session.Canvas.Translate(0, (float)paragraph.Top);
				paragraph.ParsedText.Draw(
					session,
					localCaret,
					localHighlighters,
					localComposition);
				session.Canvas.Restore();
			}
		}

		private int FindFirstParagraphEndingAfter(double y)
		{
			var low = 0;
			var high = _paragraphs.Length;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				var paragraph = _paragraphs[middle];
				if (paragraph.Top + paragraph.Size.Height <= y)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return low;
		}

		public Rect GetRectForIndex(int adjustedIndex)
		{
			var paragraph = _paragraphs[FindParagraphForIndex(adjustedIndex)];
			return Offset(
				paragraph.ParsedText.GetRectForIndex(
					Math.Clamp(adjustedIndex - paragraph.Start, 0, paragraph.Length)),
				paragraph.Top);
		}

		public TextGeometryPositionInfo GetGeometryPosition(int adjustedIndex)
		{
			var paragraph = _paragraphs[FindParagraphForIndex(adjustedIndex)];
			var geometry = paragraph.ParsedText.GetGeometryPosition(
				Math.Clamp(adjustedIndex - paragraph.Start, 0, paragraph.Length));
			return geometry with
			{
				CharacterRect = Offset(geometry.CharacterRect, paragraph.Top),
				CaretRect = Offset(geometry.CaretRect, paragraph.Top),
			};
		}

		public double GetBaselineForIndex(int adjustedIndex)
		{
			var paragraph = _paragraphs[FindParagraphForIndex(adjustedIndex)];
			return paragraph.Top + paragraph.ParsedText.GetBaselineForIndex(
				Math.Clamp(adjustedIndex - paragraph.Start, 0, paragraph.Length));
		}

		public TextVisualLineInfo GetVisualLine(int lineIndex)
		{
			if ((uint)lineIndex >= (uint)VisualLineCount)
			{
				throw new ArgumentOutOfRangeException(nameof(lineIndex));
			}

			var paragraphIndex = FindParagraphForLine(lineIndex);
			var paragraph = _paragraphs[paragraphIndex];
			var local = paragraph.ParsedText.GetVisualLine(lineIndex - paragraph.FirstLineIndex);
			return local with
			{
				Start = paragraph.Start + local.Start,
				LineIndex = lineIndex,
				Bounds = Offset(local.Bounds, paragraph.Top),
				Baseline = paragraph.Top + local.Baseline,
				IsFirst = lineIndex == 0,
				IsLast = lineIndex == VisualLineCount - 1,
			};
		}

		public int GetIndexAt(Point point, bool ignoreEndingNewLine, bool extendedSelection)
		{
			var paragraph = _paragraphs[FindParagraphForY(point.Y)];
			var local = paragraph.ParsedText.GetIndexAt(
				new Point(point.X, point.Y - paragraph.Top),
				ignoreEndingNewLine,
				extendedSelection);
			return local < 0 ? -1 : paragraph.Start + local;
		}

		public Hyperlink? GetHyperlinkAt(Point point)
		{
			var paragraph = _paragraphs[FindParagraphForY(point.Y)];
			return paragraph.ParsedText.GetHyperlinkAt(
				new Point(point.X, point.Y - paragraph.Top));
		}

		public (int start, int length) GetWordAt(int index, bool right)
		{
			var paragraphIndex = FindParagraphForIndex(index);
			if (!right
				&& paragraphIndex > 0
				&& _paragraphs[paragraphIndex].Start == index)
			{
				paragraphIndex--;
			}
			var paragraph = _paragraphs[paragraphIndex];
			var local = paragraph.ParsedText.GetWordAt(
				Math.Clamp(index - paragraph.Start, 0, paragraph.Length),
				right);
			return (paragraph.Start + local.start, local.length);
		}

		public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
		{
			var paragraph = _paragraphs[FindParagraphForIndex(index)];
			var local = paragraph.ParsedText.GetLineAt(
				Math.Clamp(index - paragraph.Start, 0, paragraph.Length));
			var lineIndex = paragraph.FirstLineIndex + local.lineIndex;
			return (
				paragraph.Start + local.start,
				local.length,
				lineIndex == 0,
				lineIndex == VisualLineCount - 1,
				lineIndex);
		}

		private int FindParagraphForIndex(int index)
		{
			index = Math.Clamp(index, 0, _textLength);
			var low = 0;
			var high = _paragraphs.Length;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_paragraphs[middle].Start <= index)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return Math.Clamp(low - 1, 0, _paragraphs.Length - 1);
		}

		private int FindParagraphForLine(int lineIndex)
		{
			var low = 0;
			var high = _paragraphs.Length;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_paragraphs[middle].FirstLineIndex <= lineIndex)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return Math.Clamp(low - 1, 0, _paragraphs.Length - 1);
		}

		private int FindParagraphForY(double y)
		{
			var low = 0;
			var high = _paragraphs.Length;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_paragraphs[middle].Top + _paragraphs[middle].Size.Height <= y)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}
			return Math.Clamp(low, 0, _paragraphs.Length - 1);
		}

		private static IReadOnlyList<TextHighlighter> GetLocalHighlighters(
			IReadOnlyList<TextHighlighter> highlighters,
			int paragraphStart,
			int paragraphEnd)
		{
			List<TextHighlighter>? local = null;
			foreach (var highlighter in highlighters)
			{
				TextHighlighter? localHighlighter = null;
				foreach (var range in highlighter.Ranges)
				{
					var start = Math.Max(paragraphStart, range.StartIndex);
					var end = Math.Min(paragraphEnd, range.StartIndex + range.Length);
					if (end <= start)
					{
						continue;
					}

					localHighlighter ??= new TextHighlighter
					{
						Background = highlighter.Background,
						Foreground = highlighter.Foreground,
					};
					localHighlighter.Ranges.Add(new TextRange
					{
						StartIndex = start - paragraphStart,
						Length = end - start,
					});
				}

				if (localHighlighter is not null)
				{
					(local ??= new()).Add(localHighlighter);
				}
			}
			return local ?? _noHighlighters;
		}

		private static (int startIndex, int length)? GetLocalRange(
			(int startIndex, int length)? range,
			int paragraphStart,
			int paragraphEnd)
		{
			if (range is not { } value)
			{
				return null;
			}

			var start = Math.Max(paragraphStart, value.startIndex);
			var end = Math.Min(paragraphEnd, value.startIndex + value.length);
			return end <= start ? null : (start - paragraphStart, end - start);
		}

		private static Rect Offset(Rect rect, double y)
			=> new(rect.X, rect.Y + y, rect.Width, rect.Height);

		private readonly record struct Paragraph(
			int Start,
			int Length,
			double Top,
			Size Size,
			int FirstLineIndex,
			IParsedText ParsedText)
		{
			internal int End => Start + Length;
		}
	}
}
