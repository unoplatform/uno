// Minimal ITextRangeProvider implementation. TextBox and TextBlock ranges retain
// their existing offset behavior. RichEditBox ranges retain a live TOM range so
// UIA clients can keep providers across document edits.

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

internal sealed class TextRangeAdapter : ITextRangeProvider, ITextRangeProvider2
{
	private readonly AutomationPeer _ownerPeer;
	private readonly FrameworkElement _owner;
	private readonly bool _useObjectText;
	private readonly Microsoft.UI.Text.ITextRange? _liveRange;
	private int _startValue;
	private int _endValue;

	internal FrameworkElement Owner => _owner;

	internal int Start => _start;

	internal int End => _end;

	private int _start
	{
		get => _liveRange?.StartPosition ?? _startValue;
		set
		{
			if (_liveRange is { } liveRange)
			{
				liveRange.StartPosition = value;
			}
			else
			{
				_startValue = value;
			}
		}
	}

	private int _end
	{
		get => _liveRange?.EndPosition ?? _endValue;
		set
		{
			if (_liveRange is { } liveRange)
			{
				liveRange.EndPosition = value;
			}
			else
			{
				_endValue = value;
			}
		}
	}

	internal TextRangeAdapter(
		AutomationPeer ownerPeer,
		FrameworkElement owner,
		int start,
		int end,
		bool useObjectText = false)
	{
		_ownerPeer = ownerPeer;
		_owner = owner;
		_useObjectText = useObjectText;
		var length = GetOwnerTextLength();
		_startValue = Math.Clamp(start, 0, length);
		_endValue = Math.Clamp(end, _startValue, length);
		if (owner is RichEditBox richEditBox)
		{
			_liveRange = richEditBox.Document.GetRange(_startValue, _endValue);
		}
	}

	private TextRangeAdapter(
		AutomationPeer ownerPeer,
		FrameworkElement owner,
		Microsoft.UI.Text.ITextRange liveRange,
		bool useObjectText)
	{
		_ownerPeer = ownerPeer;
		_owner = owner;
		_useObjectText = useObjectText;
		_liveRange = liveRange;
		_startValue = liveRange.StartPosition;
		_endValue = liveRange.EndPosition;
	}

	private string GetOwnerText() => TextAdapter.GetEffectiveText(_owner);

	private int GetOwnerTextLength() => TextAdapter.GetEffectiveTextLength(_owner);

	private bool TryGetRichEditRange(out Microsoft.UI.Text.ITextRange range)
	{
		if (_liveRange is { } liveRange)
		{
			range = liveRange;
			return true;
		}

		range = null!;
		return false;
	}

	// TOM ranges can reach past the story's final paragraph mark, which the UIA text excludes.
	private void ClampToOwnerText(Microsoft.UI.Text.ITextRange range)
	{
		var length = GetOwnerTextLength();
		if (range.EndPosition > length)
		{
			range.EndPosition = length;
		}
	}

	private int ClampMovedToOwnerText(Microsoft.UI.Text.ITextRange range, TextPatternRangeEndpoint endpoint, int endpointBefore, TextUnit unit, int moved)
	{
		var unclamped = GetEndpoint(range, endpoint);
		ClampToOwnerText(range);
		var clamped = GetEndpoint(range, endpoint);
		if (clamped == unclamped)
		{
			return moved;
		}

		if (clamped == endpointBefore)
		{
			return 0;
		}

		return unit == TextUnit.Character ? moved - (unclamped - clamped) : moved;
	}

	private static int GetEndpoint(Microsoft.UI.Text.ITextRange range, TextPatternRangeEndpoint endpoint)
		=> endpoint == TextPatternRangeEndpoint.Start ? range.StartPosition : range.EndPosition;

	private static bool TryMapTextUnit(TextUnit unit, out Microsoft.UI.Text.TextRangeUnit rangeUnit)
	{
		switch (unit)
		{
			case TextUnit.Character:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.Character;
				return true;
			case TextUnit.Word:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.Word;
				return true;
			case TextUnit.Line:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.Line;
				return true;
			case TextUnit.Paragraph:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.Paragraph;
				return true;
			case TextUnit.Format:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.CharacterFormat;
				return true;
			case TextUnit.Document:
				rangeUnit = Microsoft.UI.Text.TextRangeUnit.Story;
				return true;
			default:
				rangeUnit = default;
				return false;
		}
	}

	private bool TryGetFormatBoundaries(out int[] boundaries)
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox)
		{
			boundaries = richEditBox.Document.GetCharacterFormatBoundaries();
			return true;
		}
#endif

		boundaries = Array.Empty<int>();
		return false;
	}

	private static int FindFormatUnit(int[] boundaries, int position)
	{
		if (boundaries.Length <= 1)
		{
			return 0;
		}

		position = Math.Clamp(position, boundaries[0], boundaries[^1]);
		var index = Array.BinarySearch(boundaries, position);
		if (index >= 0)
		{
			return Math.Min(index, boundaries.Length - 2);
		}

		return Math.Clamp(~index - 1, 0, boundaries.Length - 2);
	}

	private void ExpandToFormat()
	{
		if (!TryGetFormatBoundaries(out var boundaries) || boundaries.Length <= 1)
		{
			return;
		}

		var unit = FindFormatUnit(boundaries, _start);
		_start = boundaries[unit];
		_end = boundaries[unit + 1];
	}

	private int MoveByFormat(int count)
	{
		if (count == 0 || !TryGetFormatBoundaries(out var boundaries) || boundaries.Length <= 1)
		{
			return 0;
		}

		var current = FindFormatUnit(boundaries, _start);
		var target = Math.Clamp(current + count, 0, boundaries.Length - 2);
		_start = boundaries[target];
		_end = boundaries[target + 1];
		return target - current;
	}

	private static int MoveBoundary(int[] boundaries, int position, int count, out int unitsMoved)
	{
		unitsMoved = 0;
		if (count == 0 || boundaries.Length == 0)
		{
			return position;
		}

		var index = Array.BinarySearch(boundaries, position);
		int targetIndex;
		if (index >= 0)
		{
			targetIndex = Math.Clamp(index + count, 0, boundaries.Length - 1);
			unitsMoved = targetIndex - index;
		}
		else
		{
			var insertion = ~index;
			if (count > 0)
			{
				targetIndex = Math.Clamp(insertion + count - 1, 0, boundaries.Length - 1);
				unitsMoved = targetIndex - insertion + 1;
			}
			else
			{
				targetIndex = Math.Clamp(insertion + count, 0, boundaries.Length - 1);
				unitsMoved = targetIndex - insertion;
			}
		}

		return boundaries[targetIndex];
	}

	private int MoveFormatEndpoint(TextPatternRangeEndpoint endpoint, int count)
	{
		if (!TryGetFormatBoundaries(out var boundaries))
		{
			return 0;
		}

		var current = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		var target = MoveBoundary(boundaries, current, count, out var unitsMoved);
		SetEndpoint(endpoint, target);
		return unitsMoved;
	}

#if __SKIA__
	private bool TryGetRichEditDocument(out Microsoft.UI.Text.RichEditTextDocument document)
	{
		if (_owner is RichEditBox richEditBox)
		{
			document = richEditBox.Document;
			return true;
		}

		document = null!;
		return false;
	}

	private bool ExpandToPage()
	{
		if (!TryGetRichEditDocument(out var document)
			|| !document.TryGetVisibleRange(out var start, out var end))
		{
			return false;
		}

		_start = start;
		_end = end;
		return true;
	}

	private int MoveByPage(int count)
	{
		if (count == 0 || !TryGetRichEditDocument(out var document))
		{
			return 0;
		}

		var direction = Math.Sign(count);
		var pageCount = count == int.MinValue ? int.MaxValue : Math.Abs(count);
		var anchor = _start;
		var textLength = GetOwnerTextLength();
		if (!document.TryGetRangePageTarget(anchor, direction < 0, pageCount, out var targetStart, out var moved)
			|| targetStart == anchor
			|| direction > 0 && targetStart >= textLength)
		{
			return 0;
		}

		var targetEnd = textLength;
		document.TryGetRangePageTarget(targetStart, up: false, count: 1, out targetEnd, out _);
		_start = targetStart;
		_end = Math.Max(targetStart, targetEnd);
		return direction * moved;
	}

	private int MovePageEndpoint(TextPatternRangeEndpoint endpoint, int count)
	{
		if (count == 0 || !TryGetRichEditDocument(out var document))
		{
			return 0;
		}

		var current = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		var pageCount = count == int.MinValue ? int.MaxValue : Math.Abs(count);
		if (!document.TryGetRangePageTarget(current, count < 0, pageCount, out var target, out var moved))
		{
			return 0;
		}

		SetEndpoint(endpoint, target);
		return Math.Sign(count) * moved;
	}
#endif

	private void SetEndpoint(TextPatternRangeEndpoint endpoint, int value)
	{
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

	private void ExpandToDocument()
	{
		_start = 0;
		_end = GetOwnerTextLength();
	}

	private int MoveDocumentEndpoint(TextPatternRangeEndpoint endpoint, int count)
	{
		if (count == 0)
		{
			return 0;
		}

		var current = endpoint == TextPatternRangeEndpoint.Start ? _start : _end;
		var target = count > 0 ? GetOwnerTextLength() : 0;
		if (target == current)
		{
			return 0;
		}

		SetEndpoint(endpoint, target);
		return count > 0 ? 1 : -1;
	}

	private void UpdateFromRichEditRange(Microsoft.UI.Text.ITextRange range)
	{
		if (_liveRange is null)
		{
			_startValue = range.StartPosition;
			_endValue = range.EndPosition;
		}
	}

	public ITextRangeProvider Clone()
		=> _liveRange is { } liveRange
			? new TextRangeAdapter(_ownerPeer, _owner, liveRange.GetClone(), _useObjectText)
			: new TextRangeAdapter(_ownerPeer, _owner, _start, _end, _useObjectText);

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
		if (unit == TextUnit.Document)
		{
			ExpandToDocument();
			return;
		}

		if (unit == TextUnit.Format && TryGetFormatBoundaries(out _))
		{
			ExpandToFormat();
			return;
		}

#if __SKIA__
		if (unit == TextUnit.Page && ExpandToPage())
		{
			return;
		}
#endif

		if (TryGetRichEditRange(out var range) && TryMapTextUnit(unit, out var rangeUnit))
		{
			range.Expand(rangeUnit);
			ClampToOwnerText(range);
			UpdateFromRichEditRange(range);
			return;
		}

		var text = GetOwnerText();
		switch (unit)
		{
			case TextUnit.Document:
				_start = 0;
				_end = text.Length;
				break;
			case TextUnit.Page:
				if (TryGetEnclosingTextSegment(
					_owner,
					text,
					unit,
					_start,
					_end,
					out var pageStart,
					out var pageEnd))
				{
					_start = pageStart;
					_end = pageEnd;
				}
				else
				{
					_start = 0;
					_end = text.Length;
				}
				break;
			case TextUnit.Paragraph when _owner is TextBlock:
				_start = 0;
				_end = text.Length;
				break;
			case TextUnit.Paragraph:
			case TextUnit.Line:
			case TextUnit.Word:
				if (TryGetEnclosingTextSegment(
					_owner,
					text,
					unit,
					_start,
					_end,
					out var segmentStart,
					out var segmentEnd))
				{
					_start = segmentStart;
					_end = segmentEnd;
				}
				else
				{
					_start = 0;
					_end = text.Length;
				}
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

	private static bool TryGetEnclosingTextSegment(
		FrameworkElement owner,
		string text,
		TextUnit unit,
		int rangeStart,
		int rangeEnd,
		out int start,
		out int end)
	{
		start = -1;
		end = -1;
		if (text.Length == 0)
		{
			return false;
		}

		var firstPosition = Math.Clamp(rangeStart, 0, text.Length);
		var lastPosition = rangeEnd > rangeStart
			? Math.Clamp(rangeEnd - 1, 0, text.Length - 1)
			: firstPosition;
		var cursor = 0;
		var lastSegmentStart = -1;
		var lastSegmentEnd = -1;

		while (cursor <= text.Length &&
			TryGetTextSegment(
				owner,
				text,
				unit,
				cursor,
				forward: true,
				out var segmentStart,
				out var segmentEnd))
		{
			if (segmentEnd <= cursor)
			{
				break;
			}

			lastSegmentStart = segmentStart;
			lastSegmentEnd = segmentEnd;
			if (start < 0 && firstPosition < segmentEnd)
			{
				start = segmentStart;
			}

			if (start >= 0 && lastPosition < segmentEnd)
			{
				end = segmentEnd;
				return true;
			}

			cursor = segmentEnd;
		}

		if (start < 0)
		{
			start = lastSegmentStart;
		}
		end = lastSegmentEnd;
		return start >= 0 && end > start;
	}

	public ITextRangeProvider? FindAttribute(int attributeId, object value, bool backward)
	{
#if __SKIA__
		if (_owner is RichEditBox)
		{
			if (_start == _end)
			{
				var current = GetAttributeValueForSpan((AutomationTextAttributesEnum)attributeId, _start, _end);
				return AttributeValuesEqual(current, value) ? Clone() : null;
			}

			TextRangeAdapter? match = null;
			var position = _start;
			while (position < _end)
			{
				var current = GetAttributeValueForSpan((AutomationTextAttributesEnum)attributeId, position, position + 1);
				if (!AttributeValuesEqual(current, value))
				{
					position++;
					continue;
				}

				var matchStart = position++;
				while (position < _end
					&& AttributeValuesEqual(
						GetAttributeValueForSpan((AutomationTextAttributesEnum)attributeId, position, position + 1),
						value))
				{
					position++;
				}

				match = new TextRangeAdapter(_ownerPeer, _owner, matchStart, position);
				if (!backward)
				{
					return match;
				}
			}

			return match;
		}
#endif

		return null;
	}

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

	public object GetAttributeValue(int attributeId)
	{
#if __SKIA__
		if (_owner is RichEditBox)
		{
			return GetAttributeValueForSpan((AutomationTextAttributesEnum)attributeId, _start, _end);
		}
#endif

		return TextAttributeValueSentinel.NotSupported;
	}

#if __SKIA__
	private object GetAttributeValueForSpan(AutomationTextAttributesEnum attribute, int start, int end)
	{
		var owner = (RichEditBox)_owner;
		var character = owner.Document.GetFormatOverRange(start, end, resolveForeground: true);
		var paragraph = owner.Document.GetParagraphFormatOverRange(start, end);

		return attribute switch
		{
			AutomationTextAttributesEnum.BackgroundColorAttribute => GetBackgroundColor(character, owner),
			AutomationTextAttributesEnum.BulletStyleAttribute => GetBulletStyle(paragraph),
			AutomationTextAttributesEnum.CapStyleAttribute => GetCapStyle(character),
			AutomationTextAttributesEnum.CultureAttribute => GetCulture(character, owner),
			AutomationTextAttributesEnum.FontNameAttribute => GetFontName(character, owner),
			AutomationTextAttributesEnum.FontSizeAttribute => GetFontSize(character, owner),
			AutomationTextAttributesEnum.FontWeightAttribute => GetFontWeight(character),
			AutomationTextAttributesEnum.ForegroundColorAttribute
				or AutomationTextAttributesEnum.UnderlineColorAttribute
				or AutomationTextAttributesEnum.StrikethroughColorAttribute => GetForegroundColor(character, owner),
			AutomationTextAttributesEnum.HorizontalTextAlignmentAttribute => GetHorizontalAlignment(paragraph, owner),
			AutomationTextAttributesEnum.IndentationFirstLineAttribute => GetParagraphMetric(paragraph.FirstLineIndent),
			AutomationTextAttributesEnum.IndentationLeadingAttribute => GetParagraphMetric(paragraph.LeftIndent),
			AutomationTextAttributesEnum.IndentationTrailingAttribute => GetParagraphMetric(paragraph.RightIndent),
			AutomationTextAttributesEnum.IsHiddenAttribute => GetEffect(character.Hidden),
			AutomationTextAttributesEnum.IsItalicAttribute => GetEffect(character.Italic),
			AutomationTextAttributesEnum.IsReadOnlyAttribute => owner.IsReadOnly,
			AutomationTextAttributesEnum.IsSubscriptAttribute => GetEffect(character.Subscript),
			AutomationTextAttributesEnum.IsSuperscriptAttribute => GetEffect(character.Superscript),
			AutomationTextAttributesEnum.MarginBottomAttribute => owner.Margin.Bottom + GetParagraphMetricValue(paragraph.SpaceAfter),
			AutomationTextAttributesEnum.MarginLeadingAttribute => GetLeadingMargin(owner) + GetParagraphMetricValue(paragraph.LeftIndent),
			AutomationTextAttributesEnum.MarginTopAttribute => owner.Margin.Top + GetParagraphMetricValue(paragraph.SpaceBefore),
			AutomationTextAttributesEnum.MarginTrailingAttribute => GetTrailingMargin(owner) + GetParagraphMetricValue(paragraph.RightIndent),
			AutomationTextAttributesEnum.StrikethroughStyleAttribute => GetDecorationStyle(character.Strikethrough),
			AutomationTextAttributesEnum.TabsAttribute => GetTabs(paragraph),
			AutomationTextAttributesEnum.TextFlowDirectionsAttribute => GetTextFlowDirections(paragraph),
			AutomationTextAttributesEnum.UnderlineStyleAttribute => GetUnderlineStyle(character.Underline),
			AutomationTextAttributesEnum.AnnotationTypesAttribute => GetAnnotationTypes(start, end),
			AutomationTextAttributesEnum.AnnotationObjectsAttribute => GetAnnotationObjects(start, end),
			AutomationTextAttributesEnum.LinkAttribute => GetLinkAttribute(owner, start, end),
			AutomationTextAttributesEnum.SelectionActiveEndAttribute => GetSelectionActiveEnd(owner, start, end),
			_ => TextAttributeValueSentinel.NotSupported,
		};
	}

	private object GetAnnotationTypes(int start, int end)
		=> GetAnnotationObjects(start, end).Length == 0
			? Array.Empty<int>()
			: new[] { (int)AnnotationType.SpellingError };

	private IRawElementProviderSimple[] GetAnnotationObjects(int start, int end)
		=> _ownerPeer is RichEditBoxAutomationPeer peer
			? peer.GetSpellingErrorAnnotations(start, end)
			: Array.Empty<IRawElementProviderSimple>();

	private static object GetLinkAttribute(RichEditBox owner, int start, int end)
		=> owner.Document.GetAutomationLinkState(start, end) switch
		{
			true => true,
			false => false,
			null => TextAttributeValueSentinel.Mixed,
		};

	private static object GetCapStyle(Microsoft.UI.Text.UnoTextCharacterFormat format)
	{
		if (format.AllCaps == Microsoft.UI.Text.FormatEffect.Undefined
			|| format.SmallCaps == Microsoft.UI.Text.FormatEffect.Undefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		if (format.SmallCaps == Microsoft.UI.Text.FormatEffect.On)
		{
			return FontCapitals.SmallCaps;
		}

		return format.AllCaps == Microsoft.UI.Text.FormatEffect.On
			? FontCapitals.AllSmallCaps
			: FontCapitals.Normal;
	}

	private static object GetBackgroundColor(
		Microsoft.UI.Text.UnoTextCharacterFormat format,
		RichEditBox owner)
	{
		if (!format.BackgroundDefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		var color = format.BackgroundColor;
		if (color == Microsoft.UI.Text.TextConstants.AutoColor)
		{
			return owner.Background is Microsoft.UI.Xaml.Media.SolidColorBrush brush
				? ToColorRef(brush.Color)
				: TextAttributeValueSentinel.NotSupported;
		}

		return ToColorRef(color);
	}

	private static object GetBulletStyle(Microsoft.UI.Text.UnoTextParagraphFormat format)
	{
		if (!format.ListTypeDefined || !format.ListStyleDefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		return format.ListType switch
		{
			Microsoft.UI.Text.MarkerType.Undefined or Microsoft.UI.Text.MarkerType.None
				=> AutomationBulletStyle.None,
			Microsoft.UI.Text.MarkerType.WhiteCircleWingding => AutomationBulletStyle.HollowRoundBullet,
			Microsoft.UI.Text.MarkerType.BlackCircleWingding => AutomationBulletStyle.FilledRoundBullet,
			Microsoft.UI.Text.MarkerType.Bullet when format.ListStyle == Microsoft.UI.Text.MarkerStyle.Minus
				=> AutomationBulletStyle.DashBullet,
			Microsoft.UI.Text.MarkerType.Bullet => AutomationBulletStyle.FilledRoundBullet,
			_ => AutomationBulletStyle.None,
		};
	}

	private static object GetCulture(Microsoft.UI.Text.UnoTextCharacterFormat format, RichEditBox owner)
	{
		if (!format.LanguageTagDefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		var language = string.IsNullOrWhiteSpace(format.LanguageTag) ? owner.Language : format.LanguageTag;
		if (string.IsNullOrWhiteSpace(language))
		{
			return CultureInfo.CurrentCulture.LCID;
		}

		try
		{
			return CultureInfo.GetCultureInfo(language).LCID;
		}
		catch (CultureNotFoundException)
		{
			return CultureInfo.CurrentCulture.LCID;
		}
	}

	private static object GetFontName(Microsoft.UI.Text.UnoTextCharacterFormat format, RichEditBox owner)
		=> !format.NameDefined
			? TextAttributeValueSentinel.Mixed
			: string.IsNullOrWhiteSpace(format.Name) ? owner.FontFamily.Source : format.Name;

	private static object GetFontSize(Microsoft.UI.Text.UnoTextCharacterFormat format, RichEditBox owner)
		=> !format.SizeDefined
			? TextAttributeValueSentinel.Mixed
			: format.Size > 0 ? format.Size : owner.FontSize * 72d / 96d;

	private static object GetFontWeight(Microsoft.UI.Text.UnoTextCharacterFormat format)
		=> format.Weight == Microsoft.UI.Text.TextConstants.UndefinedInt32Value
			? TextAttributeValueSentinel.Mixed
			: format.Weight;

	private static object GetForegroundColor(Microsoft.UI.Text.UnoTextCharacterFormat format, RichEditBox owner)
	{
		if (!format.ForegroundDefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		var color = format.ForegroundColor;
		if (color == Microsoft.UI.Text.TextConstants.AutoColor)
		{
			return owner.Foreground is Microsoft.UI.Xaml.Media.SolidColorBrush brush
				? ToColorRef(brush.Color)
				: TextAttributeValueSentinel.NotSupported;
		}

		return ToColorRef(color);
	}

	private static object GetHorizontalAlignment(Microsoft.UI.Text.UnoTextParagraphFormat format, RichEditBox owner)
	{
		var alignment = format.Alignment == Microsoft.UI.Text.ParagraphAlignment.Undefined
			? owner.TextAlignment switch
			{
				TextAlignment.Center => Microsoft.UI.Text.ParagraphAlignment.Center,
				TextAlignment.Right => Microsoft.UI.Text.ParagraphAlignment.Right,
				TextAlignment.Justify => Microsoft.UI.Text.ParagraphAlignment.Justify,
				_ => Microsoft.UI.Text.ParagraphAlignment.Left,
			}
			: format.Alignment;

		return alignment switch
		{
			Microsoft.UI.Text.ParagraphAlignment.Left => 0,
			Microsoft.UI.Text.ParagraphAlignment.Center => 1,
			Microsoft.UI.Text.ParagraphAlignment.Right => 2,
			Microsoft.UI.Text.ParagraphAlignment.Justify => 3,
			_ => TextAttributeValueSentinel.Mixed,
		};
	}

	private static object GetParagraphMetric(float value)
		=> value == Microsoft.UI.Text.TextConstants.UndefinedFloatValue
			? TextAttributeValueSentinel.Mixed
			: value;

	private static object GetTabs(Microsoft.UI.Text.UnoTextParagraphFormat format)
	{
		if (!format.TabsDefined)
		{
			return TextAttributeValueSentinel.Mixed;
		}

		var tabs = new double[format.TabsValue.Count];
		for (var i = 0; i < tabs.Length; i++)
		{
			tabs[i] = format.TabsValue[i].Position;
		}
		return tabs;
	}

	private static object GetTextFlowDirections(Microsoft.UI.Text.UnoTextParagraphFormat format)
		=> format.RightToLeft switch
		{
			Microsoft.UI.Text.FormatEffect.On => AutomationFlowDirections.RightToLeft,
			Microsoft.UI.Text.FormatEffect.Off => AutomationFlowDirections.Default,
			_ => TextAttributeValueSentinel.Mixed,
		};

	private static object GetSelectionActiveEnd(RichEditBox owner, int start, int end)
	{
		var selection = owner.Document.Selection;
		if (start != selection.StartPosition || end != selection.EndPosition || start == end)
		{
			return AutomationActiveEnd.None;
		}

		return selection.Options.HasFlag(Microsoft.UI.Text.SelectionOptions.StartActive)
			? AutomationActiveEnd.Start
			: AutomationActiveEnd.End;
	}

	private static double GetParagraphMetricValue(float value)
		=> value == Microsoft.UI.Text.TextConstants.UndefinedFloatValue ? 0 : value;

	private static object GetEffect(Microsoft.UI.Text.FormatEffect effect)
		=> effect switch
		{
			Microsoft.UI.Text.FormatEffect.On => true,
			Microsoft.UI.Text.FormatEffect.Off => false,
			_ => TextAttributeValueSentinel.Mixed,
		};

	private static object GetDecorationStyle(Microsoft.UI.Text.FormatEffect effect)
		=> effect switch
		{
			Microsoft.UI.Text.FormatEffect.On => AutomationTextDecorationLineStyle.Single,
			Microsoft.UI.Text.FormatEffect.Off => AutomationTextDecorationLineStyle.None,
			_ => TextAttributeValueSentinel.Mixed,
		};

	private static object GetUnderlineStyle(Microsoft.UI.Text.UnderlineType underline)
		=> underline switch
		{
			Microsoft.UI.Text.UnderlineType.None => AutomationTextDecorationLineStyle.None,
			Microsoft.UI.Text.UnderlineType.Single => AutomationTextDecorationLineStyle.Single,
			Microsoft.UI.Text.UnderlineType.Words => AutomationTextDecorationLineStyle.WordsOnly,
			Microsoft.UI.Text.UnderlineType.Double => AutomationTextDecorationLineStyle.Double,
			Microsoft.UI.Text.UnderlineType.Dotted => AutomationTextDecorationLineStyle.Dot,
			Microsoft.UI.Text.UnderlineType.Dash => AutomationTextDecorationLineStyle.Dash,
			Microsoft.UI.Text.UnderlineType.DashDot => AutomationTextDecorationLineStyle.DashDot,
			Microsoft.UI.Text.UnderlineType.DashDotDot => AutomationTextDecorationLineStyle.DashDotDot,
			Microsoft.UI.Text.UnderlineType.Wave => AutomationTextDecorationLineStyle.Wavy,
			Microsoft.UI.Text.UnderlineType.Undefined => TextAttributeValueSentinel.Mixed,
			_ => AutomationTextDecorationLineStyle.Other,
		};

	private static int ToColorRef(Windows.UI.Color color)
		=> color.R | color.G << 8 | color.B << 16;

	private static double GetLeadingMargin(RichEditBox owner)
		=> owner.FlowDirection == FlowDirection.RightToLeft ? owner.Margin.Right : owner.Margin.Left;

	private static double GetTrailingMargin(RichEditBox owner)
		=> owner.FlowDirection == FlowDirection.RightToLeft ? owner.Margin.Left : owner.Margin.Right;
#endif

#if __SKIA__
	private static bool AttributeValuesEqual(object left, object right)
	{
		if (left is TextAttributeValueSentinel || right is TextAttributeValueSentinel)
		{
			return Equals(left, right);
		}

		if (left.GetType().IsEnum && right is IConvertible)
		{
			return Convert.ToInt32(left, CultureInfo.InvariantCulture)
				== Convert.ToInt32(right, CultureInfo.InvariantCulture);
		}

		if (right.GetType().IsEnum && left is IConvertible)
		{
			return Convert.ToInt32(left, CultureInfo.InvariantCulture)
				== Convert.ToInt32(right, CultureInfo.InvariantCulture);
		}

		return Equals(left, right);
	}
#endif

	public void GetBoundingRectangles(out double[] returnValue)
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox)
		{
			if (richEditBox.Document.TryGetRangeRectangles(
				_start,
				_end,
				Microsoft.UI.Text.PointOptions.None,
				out var rectangles))
			{
				returnValue = new double[rectangles.Length * 4];
				for (var i = 0; i < rectangles.Length; i++)
				{
					var offset = i * 4;
					returnValue[offset] = rectangles[i].X;
					returnValue[offset + 1] = rectangles[i].Y;
					returnValue[offset + 2] = rectangles[i].Width;
					returnValue[offset + 3] = rectangles[i].Height;
				}
				return;
			}
		}
#endif

		var rect = _ownerPeer.GetBoundingRectangle();
		if (rect.Width <= 0 || rect.Height <= 0)
		{
			returnValue = Array.Empty<double>();
			return;
		}

		returnValue = new[] { rect.X, rect.Y, rect.Width, rect.Height };
	}

	public IRawElementProviderSimple GetEnclosingElement()
	{
#if __SKIA__
		if (_ownerPeer is RichEditBoxAutomationPeer richEditPeer
			&& richEditPeer.TryGetEnclosingTextObject(_start, _end, out var textObject))
		{
			return new IRawElementProviderSimple(textObject);
		}
#endif

		return new IRawElementProviderSimple(_ownerPeer);
	}

	public string GetText(int maxLength)
	{
		string slice;
#if __SKIA__
		if (_useObjectText && _owner is RichEditBox richEditBox)
		{
			slice = richEditBox.Document.GetTextInRange(
				_start,
				_end,
				Microsoft.UI.Text.TextGetOptions.UseObjectText);
		}
		else
#endif
		{
			var text = GetOwnerText();
			if (_start >= text.Length || _end <= _start)
			{
				return string.Empty;
			}

			slice = text.Substring(_start, _end - _start);
		}

		if (slice.Length == 0)
		{
			return string.Empty;
		}

		if (maxLength < 0 || slice.Length <= maxLength)
		{
			return slice;
		}

		return slice.Substring(0, maxLength);
	}

	public int Move(TextUnit unit, int count)
	{
		if (unit == TextUnit.Document)
		{
			return 0;
		}

		if (unit == TextUnit.Format && TryGetFormatBoundaries(out _))
		{
			return MoveByFormat(count);
		}

#if __SKIA__
		if (unit == TextUnit.Page && TryGetRichEditDocument(out _))
		{
			return MoveByPage(count);
		}
#endif

		if (TryGetRichEditRange(out var range) && TryMapTextUnit(unit, out var rangeUnit))
		{
			var startBefore = range.StartPosition;
			var moved = ClampMovedToOwnerText(range, TextPatternRangeEndpoint.Start, startBefore, unit, range.Move(rangeUnit, count));
			UpdateFromRichEditRange(range);
			return moved;
		}

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
				{
					// Word/Line/Paragraph/Format move by real units, matching the segmentation
					// used by MoveEndpointByUnit instead of approximating with characters.
					var target = MovePosition(_owner, text, _start, unit, count, out var unitsMoved);
					_start = target;
					_end = _start;
					return unitsMoved;
				}
		}
	}

	public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
	{
		if (unit == TextUnit.Document)
		{
			return MoveDocumentEndpoint(endpoint, count);
		}

		if (unit == TextUnit.Format && TryGetFormatBoundaries(out _))
		{
			return MoveFormatEndpoint(endpoint, count);
		}

#if __SKIA__
		if (unit == TextUnit.Page && TryGetRichEditDocument(out _))
		{
			return MovePageEndpoint(endpoint, count);
		}
#endif

		if (TryGetRichEditRange(out var range) && TryMapTextUnit(unit, out var rangeUnit))
		{
			var endpointBefore = GetEndpoint(range, endpoint);
			var moved = endpoint == TextPatternRangeEndpoint.Start
				? range.MoveStart(rangeUnit, count)
				: range.MoveEnd(rangeUnit, count);
			moved = ClampMovedToOwnerText(range, endpoint, endpointBefore, unit, moved);
			UpdateFromRichEditRange(range);
			return moved;
		}

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

		SetEndpoint(endpoint, target);
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
		var remaining = Math.Min(Math.Abs((long)count), (long)text.Length + 1);
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

#if __SKIA__
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
#endif

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
			ITextBoxHost { Core: { IsPassword: false } core } => core.TextBoxView?.DisplayBlock?.ParsedText,
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
		if (_owner is ITextBoxHost { Core: { } core })
		{
			core.Select(_start, Math.Max(0, _end - _start));
		}
		else if (_owner is RichEditBox richEditBox)
		{
			richEditBox.Document.Selection.SetRange(_start, _end);
		}
		// No-op for read-only text containers (TextBlock, etc.).
	}

	public void AddToSelection() { /* Multiple selections not supported. */ }

	public void RemoveFromSelection() { /* Multiple selections not supported. */ }

	public void ScrollIntoView(bool alignToTop)
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox)
		{
			richEditBox.TryScrollRangeIntoView(_start, _end, alignToTop);
		}
#else
		if (TryGetRichEditRange(out var range))
		{
			range.ScrollIntoView(alignToTop ? Microsoft.UI.Text.PointOptions.Start : Microsoft.UI.Text.PointOptions.None);
		}
#endif
	}

	public void ShowContextMenu()
	{
#if __SKIA__
		if (_owner is RichEditBox richEditBox)
		{
			richEditBox.ShowAccessibilityContextMenu(_start, _end);
			return;
		}
#endif

		_ownerPeer.ShowContextMenu();
	}

	public IRawElementProviderSimple[] GetChildren()
	{
#if __SKIA__
		return _ownerPeer is RichEditBoxAutomationPeer peer
			? peer.GetTextObjectChildren(_start, _end)
			: Array.Empty<IRawElementProviderSimple>();
#else
		return Array.Empty<IRawElementProviderSimple>();
#endif
	}
}
