// Minimal ITextRangeProvider implementation. Spans an offset range over a
// snapshot of the owning element's plain text. Enough to satisfy Narrator's
// TextPattern access for read-only content. Mutation (Select / AddToSelection)
// is implemented best-effort for TextBox; for read-only TextBlock it is a no-op.

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

internal sealed class TextRangeAdapter : ITextRangeProvider
{
	private readonly AutomationPeer _ownerPeer;
	private readonly FrameworkElement _owner;
	private int _start;
	private int _end;

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

		// Coarse-grained move for Document/Page: a single Move shifts to begin/end.
		switch (unit)
		{
			case TextUnit.Document:
			case TextUnit.Page:
				if (count > 0)
				{
					_start = text.Length;
					_end = text.Length;
				}
				else
				{
					_start = 0;
					_end = 0;
				}
				return count > 0 ? 1 : -1;

			case TextUnit.Character:
				{
					var actual = Math.Clamp(_start + count, 0, text.Length) - _start;
					_start += actual;
					_end = _start;
					return actual;
				}

			default:
				// Word/Line/Paragraph/Format approximations not implemented — treat as character moves.
				goto case TextUnit.Character;
		}
	}

	public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
	{
		var text = GetOwnerText();
		var current = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		var target = unit switch
		{
			TextUnit.Document or TextUnit.Page => count > 0 ? text.Length : 0,
			_ => Math.Clamp(current + count, 0, text.Length),
		};
		var actual = target - current;
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
		return actual;
	}

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
