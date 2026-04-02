#if __SKIA__
using System;

namespace Microsoft.UI.Text
{
	/// <summary>
	/// Provides the Skia implementation of the <see cref="RichEditTextRange"/> text range.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native RichEdit ITextRange2 COM interface.
	/// Uno implements a managed text range that operates on the RichEditTextDocument's flat text buffer.
	/// </remarks>
	public partial class RichEditTextRange
	{
		private readonly RichEditTextDocument _document;
		private int _startPosition;
		private int _endPosition;

		internal RichEditTextRange(RichEditTextDocument document, int startPosition, int endPosition)
		{
			_document = document ?? throw new ArgumentNullException(nameof(document));
			_startPosition = Math.Max(0, startPosition);
			_endPosition = Math.Max(_startPosition, endPosition);
			ClampPositions();
		}

		// ===== ITextRange properties =====

		public char Character
		{
			get
			{
				if (_startPosition >= 0 && _startPosition < _document.TextLength)
				{
					return _document.TextBuffer[_startPosition];
				}

				return '\0';
			}
			set
			{
				if (_startPosition >= 0 && _startPosition < _document.TextLength)
				{
					_document.ReplaceText(_startPosition, 1, value.ToString());
				}
			}
		}

		public ITextCharacterFormat CharacterFormat
		{
			get => _document.GetFormatForRange(_startPosition, _endPosition);
			set
			{
				if (value != null && _startPosition < _endPosition)
				{
					_document.AddFormatSpan(_startPosition, _endPosition, value);
				}
			}
		}

		public int EndPosition
		{
			get => _endPosition;
			set
			{
				_endPosition = Math.Max(0, Math.Min(value, _document.TextLength));
				if (_endPosition < _startPosition)
				{
					_startPosition = _endPosition;
				}
			}
		}

		public ITextRange FormattedText
		{
			get => _document.GetRange(_startPosition, _endPosition);
			set
			{
				if (value != null)
				{
					value.GetText(TextGetOptions.None, out var text);
					SetText(TextSetOptions.None, text);
					CharacterFormat = value.CharacterFormat;
				}
			}
		}

		public RangeGravity Gravity { get; set; } = RangeGravity.Forward;

		public int Length => _endPosition - _startPosition;

		public string Link { get; set; } = string.Empty;

		public ITextParagraphFormat ParagraphFormat
		{
			get => new TextParagraphFormat(); // TODO: Uno - Implement paragraph format per range
			set { } // TODO: Uno - Implement paragraph format setting
		}

		public int StartPosition
		{
			get => _startPosition;
			set
			{
				_startPosition = Math.Max(0, Math.Min(value, _document.TextLength));
				if (_startPosition > _endPosition)
				{
					_endPosition = _startPosition;
				}
			}
		}

		public int StoryLength => _document.TextLength;

		public string Text
		{
			get => _document.GetTextInRange(_startPosition, _endPosition);
			set => SetText(TextSetOptions.None, value ?? string.Empty);
		}

		// ===== ITextRange methods =====

		public bool CanPaste(int format) => true;

		public void ChangeCase(LetterCase value)
		{
			var text = Text;
			var newText = value switch
			{
				LetterCase.Lower => text.ToLowerInvariant(),
				LetterCase.Upper => text.ToUpperInvariant(),
				_ => text
			};
			_document.ReplaceText(_startPosition, _endPosition - _startPosition, newText);
			_endPosition = _startPosition + newText.Length;
		}

		public void Collapse(bool value)
		{
			if (value)
			{
				// Collapse to end
				_startPosition = _endPosition;
			}
			else
			{
				// Collapse to start
				_endPosition = _startPosition;
			}
		}

		public void Copy()
		{
			// Handled at the RichEditBox level
		}

		public void Cut()
		{
			// Handled at the RichEditBox level
		}

		public int Delete(TextRangeUnit unit, int count)
		{
			if (count == 0)
			{
				return 0;
			}

			if (_startPosition != _endPosition)
			{
				var length = _endPosition - _startPosition;
				_document.DeleteText(_startPosition, length);
				_endPosition = _startPosition;
				return length;
			}

			if (count > 0 && _startPosition < _document.TextLength)
			{
				_document.DeleteText(_startPosition, 1);
				return 1;
			}

			if (count < 0 && _startPosition > 0)
			{
				_startPosition--;
				_document.DeleteText(_startPosition, 1);
				_endPosition = _startPosition;
				return 1;
			}

			return 0;
		}

		public int EndOf(TextRangeUnit unit, bool extend)
		{
			var newEnd = unit switch
			{
				TextRangeUnit.Line or TextRangeUnit.Paragraph =>
					GetParagraphEnd(_endPosition),
				TextRangeUnit.Story =>
					_document.TextLength,
				TextRangeUnit.Word =>
					GetWordEnd(_endPosition),
				_ =>
					Math.Min(_endPosition + 1, _document.TextLength)
			};

			var moved = newEnd - _endPosition;
			_endPosition = newEnd;

			if (!extend)
			{
				_startPosition = _endPosition;
			}

			return moved;
		}

		public int Expand(TextRangeUnit unit)
		{
			var oldLength = _endPosition - _startPosition;

			switch (unit)
			{
				case TextRangeUnit.Word:
					_startPosition = GetWordStart(_startPosition);
					_endPosition = GetWordEnd(_endPosition);
					break;
				case TextRangeUnit.Line:
				case TextRangeUnit.Paragraph:
					var bounds = _document.GetParagraphBounds(_startPosition);
					_startPosition = bounds.start;
					_endPosition = bounds.end;
					break;
				case TextRangeUnit.Story:
					_startPosition = 0;
					_endPosition = _document.TextLength;
					break;
				default:
					break;
			}

			return (_endPosition - _startPosition) - oldLength;
		}

		public int FindText(string value, int scanLength, FindOptions options)
		{
			if (string.IsNullOrEmpty(value))
			{
				return 0;
			}

			var text = _document.TextBuffer;
			var comparison = (options & FindOptions.Case) != 0
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;

			int searchStart;
			int searchEnd;

			if (scanLength > 0)
			{
				searchStart = _endPosition;
				searchEnd = Math.Min(searchStart + scanLength, text.Length);
			}
			else if (scanLength < 0)
			{
				searchEnd = _startPosition;
				searchStart = Math.Max(searchEnd + scanLength, 0);
			}
			else
			{
				searchStart = 0;
				searchEnd = text.Length;
			}

			var searchRegion = text.Substring(searchStart, searchEnd - searchStart);
			var index = searchRegion.IndexOf(value, comparison);

			if (index >= 0)
			{
				_startPosition = searchStart + index;
				_endPosition = _startPosition + value.Length;
				return value.Length;
			}

			return 0;
		}

		public void GetCharacterUtf32(out uint value, int offset)
		{
			var pos = _startPosition + offset;
			if (pos >= 0 && pos < _document.TextLength)
			{
				value = (uint)_document.TextBuffer[pos];
			}
			else
			{
				value = 0;
			}
		}

		public ITextRange GetClone()
		{
			return new RichEditTextRange(_document, _startPosition, _endPosition);
		}

		public int GetIndex(TextRangeUnit unit)
		{
			return unit switch
			{
				TextRangeUnit.Paragraph => GetParagraphIndex(_startPosition),
				TextRangeUnit.Line => GetParagraphIndex(_startPosition), // Simplified: line = paragraph
				_ => _startPosition
			};
		}

		public void GetPoint(HorizontalCharacterAlignment horizontalAlign, VerticalCharacterAlignment verticalAlign, PointOptions options, out global::Windows.Foundation.Point point)
		{
			// TODO: Uno - Implement coordinate mapping
			point = default;
		}

		public void GetRect(PointOptions options, out global::Windows.Foundation.Rect rect, out int hit)
		{
			// TODO: Uno - Implement coordinate mapping
			rect = default;
			hit = 0;
		}

		public void GetText(TextGetOptions options, out string value)
		{
			value = _document.GetTextInRange(_startPosition, _endPosition);
			if ((options & TextGetOptions.UseCrlf) != 0)
			{
				value = value.Replace("\r", "\r\n");
			}
		}

		public void GetTextViaStream(TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			// TODO: Uno - Implement stream-based text access
		}

		public bool InRange(ITextRange range)
		{
			return range != null
				&& range.StartPosition >= _startPosition
				&& range.EndPosition <= _endPosition;
		}

		public void InsertImage(int width, int height, int ascent, VerticalCharacterAlignment verticalAlign, string alternateText, global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			// TODO: Uno - Implement image insertion
		}

		public bool InStory(ITextRange range)
		{
			return range != null; // We only have one story
		}

		public bool IsEqual(ITextRange range)
		{
			return range != null
				&& range.StartPosition == _startPosition
				&& range.EndPosition == _endPosition;
		}

		public int Move(TextRangeUnit unit, int count)
		{
			if (count == 0)
			{
				return 0;
			}

			var moved = 0;
			var position = count > 0 ? _endPosition : _startPosition;

			for (int i = 0; i < Math.Abs(count); i++)
			{
				var next = MoveByUnit(position, unit, count > 0 ? 1 : -1);
				if (next == position)
				{
					break;
				}

				position = next;
				moved += count > 0 ? 1 : -1;
			}

			_startPosition = position;
			_endPosition = position;
			return moved;
		}

		public int MoveEnd(TextRangeUnit unit, int count)
		{
			if (count == 0)
			{
				return 0;
			}

			var moved = 0;
			var position = _endPosition;

			for (int i = 0; i < Math.Abs(count); i++)
			{
				var next = MoveByUnit(position, unit, count > 0 ? 1 : -1);
				if (next == position)
				{
					break;
				}

				position = next;
				moved += count > 0 ? 1 : -1;
			}

			_endPosition = position;
			if (_endPosition < _startPosition)
			{
				_startPosition = _endPosition;
			}

			return moved;
		}

		public int MoveStart(TextRangeUnit unit, int count)
		{
			if (count == 0)
			{
				return 0;
			}

			var moved = 0;
			var position = _startPosition;

			for (int i = 0; i < Math.Abs(count); i++)
			{
				var next = MoveByUnit(position, unit, count > 0 ? 1 : -1);
				if (next == position)
				{
					break;
				}

				position = next;
				moved += count > 0 ? 1 : -1;
			}

			_startPosition = position;
			if (_startPosition > _endPosition)
			{
				_endPosition = _startPosition;
			}

			return moved;
		}

		public void Paste(int format)
		{
			// Handled at the RichEditBox level
		}

		public void ScrollIntoView(PointOptions value)
		{
			// Handled at the RichEditBox level
		}

		public void MatchSelection()
		{
			var sel = _document.Selection;
			if (sel != null)
			{
				_startPosition = sel.StartPosition;
				_endPosition = sel.EndPosition;
			}
		}

		public void SetIndex(TextRangeUnit unit, int index, bool extend)
		{
			// TODO: Uno - Implement index-based positioning
		}

		public void SetPoint(global::Windows.Foundation.Point point, PointOptions options, bool extend)
		{
			// TODO: Uno - Implement point-based positioning
		}

		public void SetRange(int startPosition, int endPosition)
		{
			_startPosition = Math.Max(0, Math.Min(startPosition, _document.TextLength));
			_endPosition = Math.Max(0, Math.Min(endPosition, _document.TextLength));
			if (_endPosition < _startPosition)
			{
				(_startPosition, _endPosition) = (_endPosition, _startPosition);
			}
		}

		public void SetText(TextSetOptions options, string value)
		{
			var text = value ?? string.Empty;

			// Normalize line endings
			text = text.Replace("\r\n", "\r").Replace("\n", "\r");

			_document.ReplaceText(_startPosition, _endPosition - _startPosition, text);
			_endPosition = _startPosition + text.Length;
		}

		public void SetTextViaStream(TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			// TODO: Uno - Implement stream-based text setting
		}

		public int StartOf(TextRangeUnit unit, bool extend)
		{
			var newStart = unit switch
			{
				TextRangeUnit.Line or TextRangeUnit.Paragraph =>
					GetParagraphStart(_startPosition),
				TextRangeUnit.Story =>
					0,
				TextRangeUnit.Word =>
					GetWordStart(_startPosition),
				_ =>
					Math.Max(_startPosition - 1, 0)
			};

			var moved = _startPosition - newStart;
			_startPosition = newStart;

			if (!extend)
			{
				_endPosition = _startPosition;
			}

			return moved;
		}

		// ===== Private helpers =====

		private void ClampPositions()
		{
			var len = _document.TextLength;
			_startPosition = Math.Max(0, Math.Min(_startPosition, len));
			_endPosition = Math.Max(0, Math.Min(_endPosition, len));
			if (_endPosition < _startPosition)
			{
				(_startPosition, _endPosition) = (_endPosition, _startPosition);
			}
		}

		private int MoveByUnit(int position, TextRangeUnit unit, int direction)
		{
			return unit switch
			{
				TextRangeUnit.Character =>
					Math.Max(0, Math.Min(position + direction, _document.TextLength)),
				TextRangeUnit.Word =>
					direction > 0 ? GetWordEnd(position) : GetWordStart(position),
				TextRangeUnit.Line or TextRangeUnit.Paragraph =>
					direction > 0 ? GetNextParagraphStart(position) : GetPreviousParagraphStart(position),
				_ =>
					Math.Max(0, Math.Min(position + direction, _document.TextLength))
			};
		}

		private int GetWordStart(int position)
		{
			var text = _document.TextBuffer;
			if (position <= 0)
			{
				return 0;
			}

			position = Math.Min(position, text.Length);
			var pos = position - 1;

			// Skip whitespace backwards
			while (pos > 0 && char.IsWhiteSpace(text[pos]))
			{
				pos--;
			}

			// Skip word characters backwards
			while (pos > 0 && !char.IsWhiteSpace(text[pos - 1]))
			{
				pos--;
			}

			return pos;
		}

		private int GetWordEnd(int position)
		{
			var text = _document.TextBuffer;
			if (position >= text.Length)
			{
				return text.Length;
			}

			var pos = position;

			// Skip current word characters
			while (pos < text.Length && !char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			// Skip whitespace
			while (pos < text.Length && char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			return pos;
		}

		private int GetParagraphStart(int position)
		{
			var bounds = _document.GetParagraphBounds(position);
			return bounds.start;
		}

		private int GetParagraphEnd(int position)
		{
			var bounds = _document.GetParagraphBounds(position);
			return bounds.end;
		}

		private int GetNextParagraphStart(int position)
		{
			var text = _document.TextBuffer;
			var pos = position;
			while (pos < text.Length && text[pos] != '\r')
			{
				pos++;
			}

			if (pos < text.Length)
			{
				pos++; // Skip the \r
			}

			return pos;
		}

		private int GetPreviousParagraphStart(int position)
		{
			var text = _document.TextBuffer;
			if (position <= 0)
			{
				return 0;
			}

			var pos = position - 1;

			// Skip current paragraph break if at one
			if (pos > 0 && text[pos] == '\r')
			{
				pos--;
			}

			// Find previous paragraph break
			while (pos > 0 && text[pos - 1] != '\r')
			{
				pos--;
			}

			return pos;
		}

		private int GetParagraphIndex(int position)
		{
			var text = _document.TextBuffer;
			var index = 0;
			for (int i = 0; i < position && i < text.Length; i++)
			{
				if (text[i] == '\r')
				{
					index++;
				}
			}

			return index;
		}
	}
}
#endif
