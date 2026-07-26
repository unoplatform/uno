// Minimal ITextRangeProvider implementation. Spans an offset range over a
// snapshot of the owning element's plain text. Enough to satisfy Narrator's
// TextPattern access for read-only content. Mutation (Select / AddToSelection)
// is implemented best-effort for TextBox; for read-only TextBlock it is a no-op.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace DirectUI;

internal sealed class TextRangeAdapter : ITextRangeProvider
{
	private readonly AutomationPeer _ownerPeer;
	private readonly FrameworkElement _owner;
	private int _start;
	private int _end;

	internal FrameworkElement Owner => _owner;

	internal int Start => _start;

	internal int End => _end;

	internal TextRangeAdapter(AutomationPeer ownerPeer, FrameworkElement owner, int start, int end)
	{
		_ownerPeer = ownerPeer;
		_owner = owner;
		var length = GetOwnerText().Length;
		_start = Math.Clamp(start, 0, length);
		_end = Math.Clamp(end, _start, length);
	}

	private string GetOwnerText() => TextAdapter.GetEffectiveText(_owner);

	public ITextRangeProvider Clone() => new TextRangeAdapter(_ownerPeer, _owner, _start, _end);

	public bool Compare(ITextRangeProvider textRangeProvider)
		=> textRangeProvider is TextRangeAdapter other
			&& ReferenceEquals(other._owner, _owner)
			&& other._start == _start
			&& other._end == _end;

	public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint)
	{
		if (textRangeProvider is not TextRangeAdapter other)
		{
			return 0;
		}

		var a = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		var b = targetEndpoint == TextPatternRangeEndpoint.Start ? other._start : other._end;
		return a.CompareTo(b);
	}

	public void ExpandToEnclosingUnit(TextUnit unit)
	{
		var text = GetOwnerText();
		switch (unit)
		{
			case TextUnit.Document:
			case TextUnit.Page:
				_start = 0;
				_end = text.Length;
				break;
			case TextUnit.Paragraph:
			case TextUnit.Line:
				// Treat the whole text as a single line/paragraph — adequate for
				// single-line or wrap-only controls without a layout-aware text store.
				_start = 0;
				_end = text.Length;
				break;
			case TextUnit.Word:
				ExpandToWord(text);
				break;
			case TextUnit.Character:
				if (_start < text.Length)
				{
					_end = Math.Min(_start + 1, text.Length);
				}
				break;
			case TextUnit.Format:
				// No formatting model — treat as document.
				_start = 0;
				_end = text.Length;
				break;
		}
	}

	private void ExpandToWord(string text)
	{
		if (text.Length == 0)
		{
			_start = 0;
			_end = 0;
			return;
		}

		var idx = Math.Clamp(_start, 0, text.Length - 1);
		var startIdx = idx;
		while (startIdx > 0 && !char.IsWhiteSpace(text[startIdx - 1]))
		{
			startIdx--;
		}
		var endIdx = idx;
		while (endIdx < text.Length && !char.IsWhiteSpace(text[endIdx]))
		{
			endIdx++;
		}
		_start = startIdx;
		_end = endIdx;
	}

	public ITextRangeProvider? FindAttribute(int attributeId, object value, bool backward) => null;

	public ITextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
	{
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}

		var body = GetOwnerText();
		if (_start >= body.Length || _end <= _start)
		{
			return null;
		}

		var span = body.Substring(_start, _end - _start);
		var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		var index = backward ? span.LastIndexOf(text, comparison) : span.IndexOf(text, comparison);
		if (index < 0)
		{
			return null;
		}

		var matchStart = _start + index;
		return new TextRangeAdapter(_ownerPeer, _owner, matchStart, matchStart + text.Length);
	}

	public object? GetAttributeValue(int attributeId) => null;

	public void GetBoundingRectangles(out double[] returnValue)
	{
		var rect = _ownerPeer.GetBoundingRectangle();
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			returnValue = Array.Empty<double>();
			return;
		}

		returnValue = new[] { rect.X, rect.Y, rect.Width, rect.Height };
	}

	public IRawElementProviderSimple GetEnclosingElement() => new IRawElementProviderSimple(_ownerPeer);

	public string GetText(int maxLength)
	{
		var text = GetOwnerText();
		if (_start >= text.Length || _end <= _start)
		{
			return string.Empty;
		}

		var slice = text.Substring(_start, _end - _start);
		if (maxLength < 0 || slice.Length <= maxLength)
		{
			return slice;
		}

		return slice.Substring(0, maxLength);
	}

	public int Move(TextUnit unit, int count)
	{
		var text = GetOwnerText();
		if (text.Length == 0 || count == 0)
		{
			return 0;
		}

		switch (unit)
		{
			case TextUnit.Document:
			case TextUnit.Page:
				// WinUI treats moving a range by Page or Document as a no-op.
				return 0;

			case TextUnit.Character:
				{
					var actual = Math.Clamp(_start + count, 0, text.Length) - _start;
					_start += actual;
					_end = _start;
					return actual;
				}

			default:
				goto case TextUnit.Character;
		}
	}

	public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
	{
		var text = GetOwnerText();
		var current = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		int target;
		int unitsMoved;
		if (count == 0)
		{
			return 0;
		}

		if (unit is TextUnit.Document or TextUnit.Page)
		{
			target = count > 0 ? text.Length : 0;
			unitsMoved = target == current ? 0 : Math.Sign(count);
		}
		else
		{
			target = MovePosition(_owner, text, current, unit, count, out unitsMoved);
		}
		if (endpoint == TextPatternRangeEndpoint.Start)
		{
			_start = target;
			if (_end < _start)
			{
				_end = _start;
			}
		}
		else
		{
			_end = target;
			if (_start > _end)
			{
				_start = _end;
			}
		}
		return unitsMoved;
	}

	internal static bool TryGetTextSegment(
		FrameworkElement? owner,
		string text,
		TextUnit unit,
		int position,
		bool forward,
		out int start,
		out int end)
	{
		position = Math.Clamp(position, 0, text.Length);
		start = -1;
		end = -1;
		if (text.Length == 0)
		{
			return false;
		}

		switch (unit)
		{
			case TextUnit.Character:
				return TryGetCharacterSegment(text, position, forward, out start, out end);
			case TextUnit.Word:
				return TryGetWordSegment(owner, text, position, forward, out start, out end);
			case TextUnit.Line:
				return TryGetLineSegment(owner, text, position, forward, out start, out end);
			case TextUnit.Paragraph:
				return TryGetParagraphSegment(text, position, forward, out start, out end);
			case TextUnit.Page:
				return TryGetPageSegment(owner, text, position, forward, out start, out end);
			default:
				return false;
		}
	}

	internal static int GetSupportedTextGranularities(FrameworkElement? owner, string text)
	{
		if (text.Length == 0)
		{
			return 0;
		}

		const int character = 0x1;
		const int word = 0x2;
		const int paragraph = 0x8;

		var granularities = character | word | paragraph;
#if __SKIA__
		const int line = 0x4;
		const int page = 0x10;
		if (GetParsedText(owner) is not null)
		{
			granularities |= line;
			if (owner is { ActualHeight: > 0 })
			{
				granularities |= page;
			}
		}
#endif
		return granularities;
	}

	private static int MovePosition(
		FrameworkElement owner,
		string text,
		int position,
		TextUnit unit,
		int count,
		out int unitsMoved)
	{
		position = Math.Clamp(position, 0, text.Length);
		unitsMoved = 0;
		if (count == 0)
		{
			return position;
		}

		if (unit == TextUnit.Character)
		{
			return MoveByCharacter(text, position, count, out unitsMoved);
		}

		var direction = Math.Sign(count);
		var remaining = Math.Abs(count);
		while (remaining-- > 0 &&
			TryGetTextSegment(owner, text, unit, position, direction > 0, out var start, out var end))
		{
			var next = direction > 0 ? end : start;
			if (next == position)
			{
				break;
			}

			position = next;
			unitsMoved += direction;
		}

		return position;
	}

	private static int MoveByCharacter(
		string text,
		int position,
		int count,
		out int unitsMoved)
	{
		var boundaries = StringInfo.ParseCombiningCharacters(text);
		var boundaryIndex = Array.BinarySearch(boundaries, position);
		boundaryIndex = boundaryIndex >= 0 ? boundaryIndex : ~boundaryIndex;
		var targetIndex = Math.Clamp(boundaryIndex + count, 0, boundaries.Length);
		unitsMoved = targetIndex - boundaryIndex;
		return targetIndex < boundaries.Length ? boundaries[targetIndex] : text.Length;
	}

	private static bool TryGetCharacterSegment(
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
		var boundaries = StringInfo.ParseCombiningCharacters(text);
		if (boundaries.Length == 0)
		{
			start = -1;
			end = -1;
			return false;
		}

		if (forward)
		{
			var index = Array.BinarySearch(boundaries, position);
			if (index < 0)
			{
				index = ~index;
			}

			if (index >= boundaries.Length)
			{
				start = -1;
				end = -1;
				return false;
			}

			start = boundaries[index];
			end = index + 1 < boundaries.Length ? boundaries[index + 1] : text.Length;
			return true;
		}

		var precedingIndex = Array.BinarySearch(boundaries, position);
		precedingIndex = precedingIndex >= 0 ? precedingIndex - 1 : ~precedingIndex - 1;
		if (precedingIndex < 0)
		{
			start = -1;
			end = -1;
			return false;
		}

		start = boundaries[precedingIndex];
		end = precedingIndex + 1 < boundaries.Length ? boundaries[precedingIndex + 1] : text.Length;
		return true;
	}

	private static bool TryGetWordSegment(
		FrameworkElement? owner,
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
#if __SKIA__
		if (GetParsedText(owner) is { } parsedText)
		{
			var current = position;
			while (forward ? current < text.Length : current > 0)
			{
				var segment = parsedText.GetWordAt(current, right: forward);
				var segmentStart = Math.Clamp(segment.start, 0, text.Length);
				var segmentEnd = Math.Clamp(segment.start + segment.length, segmentStart, text.Length);
				if (TryGetLetterOrDigitRun(
					text,
					segmentStart,
					segmentEnd,
					out start,
					out end))
				{
					if (forward && start < position && position < end)
					{
						start = position;
					}
					else if (!forward && start < position && position < end)
					{
						end = position;
					}

					if (forward ? end > position : start < position)
					{
						return true;
					}
				}

				var next = forward ? segmentEnd : segmentStart;
				if (next == current)
				{
					break;
				}

				current = next;
			}
		}
#endif

		var textElementBoundaries = StringInfo.ParseCombiningCharacters(text);
		if (forward)
		{
			var current = Math.Clamp(position, 0, text.Length);
			while (current < text.Length)
			{
				var next = GetNextBoundary(textElementBoundaries, current, text.Length);
				if (ContainsLetterOrDigit(text, current, next - current))
				{
					break;
				}
				current = next;
			}

			start = current;
			while (current < text.Length)
			{
				var next = GetNextBoundary(textElementBoundaries, current, text.Length);
				if (!ContainsLetterOrDigit(text, current, next - current))
				{
					break;
				}
				current = next;
			}
			end = current;
			return end > start;
		}

		var backwardCurrent = Math.Clamp(position, 0, text.Length);
		while (backwardCurrent > 0 &&
			!ContainsLetterOrDigit(text, GetPreviousBoundary(textElementBoundaries, backwardCurrent), backwardCurrent - GetPreviousBoundary(textElementBoundaries, backwardCurrent)))
		{
			backwardCurrent = GetPreviousBoundary(textElementBoundaries, backwardCurrent);
		}

		end = backwardCurrent;
		while (backwardCurrent > 0)
		{
			var previous = GetPreviousBoundary(textElementBoundaries, backwardCurrent);
			if (!ContainsLetterOrDigit(text, previous, backwardCurrent - previous))
			{
				break;
			}
			backwardCurrent = previous;
		}
		start = backwardCurrent;
		return end > start;
	}

	private static bool TryGetLetterOrDigitRun(
		string text,
		int segmentStart,
		int segmentEnd,
		out int start,
		out int end)
	{
		var boundaries = StringInfo.ParseCombiningCharacters(text);
		start = segmentStart;
		while (start < segmentEnd)
		{
			var next = GetNextBoundary(boundaries, start, text.Length);
			if (ContainsLetterOrDigit(text, start, next - start))
			{
				break;
			}
			start = next;
		}

		end = segmentEnd;
		while (end > start)
		{
			var previous = GetPreviousBoundary(boundaries, end);
			if (ContainsLetterOrDigit(text, previous, end - previous))
			{
				break;
			}
			end = previous;
		}

		return end > start;
	}

	private static bool TryGetLineSegment(
		FrameworkElement? owner,
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
		if (text.AsSpan().IndexOfAny('\r', '\n') >= 0)
		{
			return TryGetLogicalLineSegment(text, position, forward, out start, out end);
		}

#if __SKIA__
		if (GetParsedText(owner) is { } parsedText &&
			TryGetParsedLines(parsedText, text, out var lines))
		{
			var currentIndex = FindLineIndex(lines, position);
			if (currentIndex >= 0)
			{
				var current = lines[currentIndex];
				var targetIndex = forward
					? position == current.Start ? currentIndex : currentIndex + 1
					: position == current.End ? currentIndex : currentIndex - 1;
				if (targetIndex >= 0 && targetIndex < lines.Count)
				{
					start = lines[targetIndex].Start;
					end = lines[targetIndex].End;
					return end > start;
				}
			}
		}
#endif

		return TryGetLogicalLineSegment(text, position, forward, out start, out end);
	}

	private static bool TryGetLogicalLineSegment(
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
		var lines = GetLogicalLines(text);
		var currentIndex = FindLineIndex(lines, position);
		if (currentIndex < 0)
		{
			start = -1;
			end = -1;
			return false;
		}

		var current = lines[currentIndex];
		var targetIndex = forward
			? position == current.Start ? currentIndex : currentIndex + 1
			: position == current.End ? currentIndex : currentIndex - 1;
		if (targetIndex < 0 || targetIndex >= lines.Count)
		{
			start = -1;
			end = -1;
			return false;
		}

		start = lines[targetIndex].Start;
		end = lines[targetIndex].End;
		return end > start;
	}

	private static bool TryGetParagraphSegment(
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
		if (forward)
		{
			if (position >= text.Length)
			{
				start = -1;
				end = -1;
				return false;
			}

			start = Math.Max(0, position);
			while (start < text.Length && IsParagraphSeparator(text[start]))
			{
				start++;
			}

			end = start;
			while (end < text.Length && !IsParagraphSeparator(text[end]))
			{
				end++;
			}
			return end > start;
		}

		if (position <= 0)
		{
			start = -1;
			end = -1;
			return false;
		}

		end = Math.Min(text.Length, position);
		while (end > 0 && IsParagraphSeparator(text[end - 1]))
		{
			end--;
		}

		start = end;
		while (start > 0 && !IsParagraphSeparator(text[start - 1]))
		{
			start--;
		}
		return end > start;
	}

	private static bool IsParagraphSeparator(char value) => value is '\r' or '\n';

	private static bool TryGetPageSegment(
		FrameworkElement? owner,
		string text,
		int position,
		bool forward,
		out int start,
		out int end)
	{
#if __SKIA__
		if (owner is { ActualHeight: > 0 } &&
			GetParsedText(owner) is { } parsedText &&
			TryGetParsedLines(parsedText, text, out var lines))
		{
			var currentIndex = FindLineIndex(lines, position);
			if (currentIndex >= 0)
			{
				var pageHeight = owner.ActualHeight;
				if (forward)
				{
					start = Math.Max(0, position);
					var top = parsedText.GetRectForIndex(lines[currentIndex].Start).Y;
					var target = currentIndex;
					while (target + 1 < lines.Count &&
						parsedText.GetRectForIndex(lines[target + 1].Start).Y < top + pageHeight)
					{
						target++;
					}
					end = lines[target].End;
					return end > start;
				}

				end = Math.Min(text.Length, position);
				var currentTop = parsedText.GetRectForIndex(lines[currentIndex].Start).Y;
				var backwardTarget = currentIndex;
				while (backwardTarget > 0 &&
					parsedText.GetRectForIndex(lines[backwardTarget - 1].Start).Y >= currentTop - pageHeight)
				{
					backwardTarget--;
				}
				start = lines[backwardTarget].Start;
				return end > start;
			}
		}
#endif

		start = -1;
		end = -1;
		return false;
	}

	private static List<(int Start, int End)> GetLogicalLines(string text)
	{
		var lines = new List<(int Start, int End)>();
		var start = 0;
		for (var i = 0; i < text.Length;)
		{
			if (IsParagraphSeparator(text[i]))
			{
				var separatorEnd =
					text[i] == '\r' &&
					i + 1 < text.Length &&
					text[i + 1] == '\n'
						? i + 2
						: i + 1;
				lines.Add((start, separatorEnd));
				start = separatorEnd;
				i = separatorEnd;
			}
			else
			{
				i++;
			}
		}

		if (start < text.Length || lines.Count == 0)
		{
			lines.Add((start, text.Length));
		}
		return lines;
	}

	private static int FindLineIndex(IReadOnlyList<(int Start, int End)> lines, int position)
	{
		for (var i = 0; i < lines.Count; i++)
		{
			if (position >= lines[i].Start &&
				(position < lines[i].End || position == lines[i].End && i == lines.Count - 1))
			{
				return i;
			}
		}
		return -1;
	}

#if __SKIA__
	private static bool TryGetParsedLines(
		IParsedText parsedText,
		string text,
		out List<(int Start, int End)> lines)
	{
		lines = new List<(int Start, int End)>();
		var textLength = text.Length;
		var probe = 0;
		var lastLineIndex = -1;
		while (probe <= textLength)
		{
			var line = parsedText.GetLineAt(probe);
			if (line.lineIndex == lastLineIndex)
			{
				break;
			}

			var start = Math.Clamp(line.start, 0, textLength);
			var end = Math.Clamp(line.start + line.length, start, textLength);
			lines.Add((start, end));
			lastLineIndex = line.lineIndex;
			if (line.lastLine || end >= textLength)
			{
				break;
			}

			probe = end > probe ? end : probe + 1;
			while (probe < textLength &&
				IsParagraphSeparator(text[probe]))
			{
				probe++;
			}
		}
		return lines.Count > 0;
	}
#endif

	private static int GetNextBoundary(int[] boundaries, int position, int textLength)
	{
		var index = Array.BinarySearch(boundaries, position);
		index = index >= 0 ? index + 1 : ~index;
		return index < boundaries.Length ? boundaries[index] : textLength;
	}

	private static int GetPreviousBoundary(int[] boundaries, int position)
	{
		var index = Array.BinarySearch(boundaries, position);
		index = index >= 0 ? index - 1 : ~index - 1;
		return index >= 0 ? boundaries[index] : 0;
	}

	private static bool ContainsLetterOrDigit(string text, int start, int length)
	{
		foreach (var rune in text.AsSpan(start, length).EnumerateRunes())
		{
			if (Rune.IsLetterOrDigit(rune))
			{
				return true;
			}
		}

		return false;
	}

#if __SKIA__
	private static IParsedText? GetParsedText(FrameworkElement? owner)
		=> owner switch
		{
			TextBox textBox => textBox.TextBoxView.DisplayBlock.ParsedText,
			TextBlock textBlock => textBlock.ParsedText,
			_ => null,
		};
#endif

	public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider textRangeProvider, TextPatternRangeEndpoint targetEndpoint)
	{
		if (textRangeProvider is not TextRangeAdapter other)
		{
			return;
		}

		var value = targetEndpoint == TextPatternRangeEndpoint.Start ? other._start : other._end;
		if (endpoint == TextPatternRangeEndpoint.Start)
		{
			_start = value;
			if (_end < _start)
			{
				_end = _start;
			}
		}
		else
		{
			_end = value;
			if (_start > _end)
			{
				_start = _end;
			}
		}
	}

	public void Select()
	{
		if (_owner is TextBox textBox)
		{
			textBox.Select(_start, Math.Max(0, _end - _start));
		}
		// No-op for read-only text containers (TextBlock, etc.).
	}

	public void AddToSelection() { /* Multiple selections not supported. */ }

	public void RemoveFromSelection() { /* Multiple selections not supported. */ }

	public void ScrollIntoView(bool alignToTop)
	{
		// Best-effort: defer to the peer's bounding rectangle, which UIA can use.
	}

	public IRawElementProviderSimple[] GetChildren() => Array.Empty<IRawElementProviderSimple>();
}
