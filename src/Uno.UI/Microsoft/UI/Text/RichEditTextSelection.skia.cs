#if __SKIA__
using System;

namespace Microsoft.UI.Text
{
	/// <summary>
	/// Provides the Skia implementation of <see cref="ITextSelection"/> for the RichEditBox document model.
	/// </summary>
	/// <remarks>
	/// Diverges from WinUI: WinUI uses the native RichEdit ITextSelection2 COM interface.
	/// Uno implements a managed selection that extends RichEditTextRange with selection-specific behavior.
	/// </remarks>
	internal class RichEditTextSelection : ITextSelection
	{
		private readonly RichEditTextDocument _document;
		private int _startPosition;
		private int _endPosition;

		internal RichEditTextSelection(RichEditTextDocument document)
		{
			_document = document ?? throw new ArgumentNullException(nameof(document));
		}

		// ===== ITextSelection properties =====

		public SelectionOptions Options { get; set; } = SelectionOptions.StartActive;

		public SelectionType Type =>
			_startPosition == _endPosition ? SelectionType.InsertionPoint : SelectionType.Normal;

		// ===== ITextRange properties (inherited by ITextSelection) =====

		public char Character
		{
			get => _startPosition >= 0 && _startPosition < _document.TextLength
				? _document.TextBuffer[_startPosition] : '\0';
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

		public int Length => Math.Abs(_endPosition - _startPosition);

		public string Link { get; set; } = string.Empty;

		public ITextParagraphFormat ParagraphFormat
		{
			get => new TextParagraphFormat();
			set { }
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
			get => _document.GetTextInRange(
				Math.Min(_startPosition, _endPosition),
				Math.Max(_startPosition, _endPosition));
			set => SetText(TextSetOptions.None, value ?? string.Empty);
		}

		// ===== ITextSelection methods =====

		public int EndKey(TextRangeUnit unit, bool extend)
		{
			int newPos;
			switch (unit)
			{
				case TextRangeUnit.Line:
				case TextRangeUnit.Paragraph:
					var bounds = _document.GetParagraphBounds(_endPosition);
					newPos = bounds.end;
					break;
				case TextRangeUnit.Story:
					newPos = _document.TextLength;
					break;
				default:
					newPos = _endPosition;
					break;
			}

			var moved = newPos - _endPosition;
			_endPosition = newPos;
			if (!extend)
			{
				_startPosition = _endPosition;
			}

			return moved;
		}

		public int HomeKey(TextRangeUnit unit, bool extend)
		{
			int newPos;
			switch (unit)
			{
				case TextRangeUnit.Line:
				case TextRangeUnit.Paragraph:
					var bounds = _document.GetParagraphBounds(_startPosition);
					newPos = bounds.start;
					break;
				case TextRangeUnit.Story:
					newPos = 0;
					break;
				default:
					newPos = _startPosition;
					break;
			}

			var moved = _startPosition - newPos;
			_startPosition = newPos;
			if (!extend)
			{
				_endPosition = _startPosition;
			}

			return moved;
		}

		public int MoveDown(TextRangeUnit unit, int count, bool extend)
		{
			// Simplified: move to next paragraph
			var position = _endPosition;
			var moved = 0;
			var text = _document.TextBuffer;

			for (int i = 0; i < count; i++)
			{
				var next = position;
				while (next < text.Length && text[next] != '\r')
				{
					next++;
				}

				if (next < text.Length)
				{
					next++; // Skip \r
					position = next;
					moved++;
				}
				else
				{
					break;
				}
			}

			_endPosition = position;
			if (!extend)
			{
				_startPosition = _endPosition;
			}

			return moved;
		}

		public int MoveLeft(TextRangeUnit unit, int count, bool extend)
		{
			var position = extend ? _endPosition : Math.Min(_startPosition, _endPosition);
			var moved = 0;

			for (int i = 0; i < count; i++)
			{
				if (unit == TextRangeUnit.Character)
				{
					if (position > 0)
					{
						position--;
						moved++;
					}
				}
				else if (unit == TextRangeUnit.Word)
				{
					var newPos = GetWordStart(position);
					if (newPos < position)
					{
						position = newPos;
						moved++;
					}
				}
			}

			if (extend)
			{
				_endPosition = position;
			}
			else
			{
				_startPosition = position;
				_endPosition = position;
			}

			return moved;
		}

		public int MoveRight(TextRangeUnit unit, int count, bool extend)
		{
			var position = extend ? _endPosition : Math.Max(_startPosition, _endPosition);
			var moved = 0;

			for (int i = 0; i < count; i++)
			{
				if (unit == TextRangeUnit.Character)
				{
					if (position < _document.TextLength)
					{
						position++;
						moved++;
					}
				}
				else if (unit == TextRangeUnit.Word)
				{
					var newPos = GetWordEnd(position);
					if (newPos > position)
					{
						position = newPos;
						moved++;
					}
				}
			}

			if (extend)
			{
				_endPosition = position;
			}
			else
			{
				_startPosition = position;
				_endPosition = position;
			}

			return moved;
		}

		public int MoveUp(TextRangeUnit unit, int count, bool extend)
		{
			// Simplified: move to previous paragraph
			var position = _startPosition;
			var moved = 0;
			var text = _document.TextBuffer;

			for (int i = 0; i < count; i++)
			{
				if (position <= 0)
				{
					break;
				}

				var prev = position - 1;
				if (prev > 0 && text[prev] == '\r')
				{
					prev--;
				}

				while (prev > 0 && text[prev - 1] != '\r')
				{
					prev--;
				}

				position = prev;
				moved++;
			}

			_startPosition = position;
			if (!extend)
			{
				_endPosition = _startPosition;
			}

			return moved;
		}

		public void TypeText(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return;
			}

			// Replace selection with typed text
			var start = Math.Min(_startPosition, _endPosition);
			var end = Math.Max(_startPosition, _endPosition);
			var length = end - start;

			_document.ReplaceText(start, length, value);
			_startPosition = start + value.Length;
			_endPosition = _startPosition;
		}

		// ===== ITextRange methods =====

		public bool CanPaste(int format) => true;

		public void ChangeCase(LetterCase value)
		{
			var start = Math.Min(_startPosition, _endPosition);
			var end = Math.Max(_startPosition, _endPosition);
			var text = _document.GetTextInRange(start, end);
			var newText = value switch
			{
				LetterCase.Lower => text.ToLowerInvariant(),
				LetterCase.Upper => text.ToUpperInvariant(),
				_ => text
			};
			_document.ReplaceText(start, end - start, newText);
		}

		public void Collapse(bool value)
		{
			if (value)
			{
				_startPosition = _endPosition;
			}
			else
			{
				_endPosition = _startPosition;
			}
		}

		public void Copy() { }

		public void Cut() { }

		public int Delete(TextRangeUnit unit, int count)
		{
			var start = Math.Min(_startPosition, _endPosition);
			var end = Math.Max(_startPosition, _endPosition);

			if (start != end)
			{
				var len = end - start;
				_document.DeleteText(start, len);
				_startPosition = start;
				_endPosition = start;
				return len;
			}

			if (count > 0 && start < _document.TextLength)
			{
				_document.DeleteText(start, 1);
				return 1;
			}

			if (count < 0 && start > 0)
			{
				_document.DeleteText(start - 1, 1);
				_startPosition = start - 1;
				_endPosition = _startPosition;
				return 1;
			}

			return 0;
		}

		public int EndOf(TextRangeUnit unit, bool extend)
		{
			var bounds = _document.GetParagraphBounds(_endPosition);
			var newEnd = unit switch
			{
				TextRangeUnit.Line or TextRangeUnit.Paragraph => bounds.end,
				TextRangeUnit.Story => _document.TextLength,
				_ => _endPosition
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
			var oldLen = Math.Abs(_endPosition - _startPosition);
			switch (unit)
			{
				case TextRangeUnit.Word:
					_startPosition = GetWordStart(Math.Min(_startPosition, _endPosition));
					_endPosition = GetWordEnd(Math.Max(_startPosition, _endPosition));
					break;
				case TextRangeUnit.Line:
				case TextRangeUnit.Paragraph:
					var bounds = _document.GetParagraphBounds(Math.Min(_startPosition, _endPosition));
					_startPosition = bounds.start;
					_endPosition = bounds.end;
					break;
				case TextRangeUnit.Story:
					_startPosition = 0;
					_endPosition = _document.TextLength;
					break;
			}

			return Math.Abs(_endPosition - _startPosition) - oldLen;
		}

		public int FindText(string value, int scanLength, FindOptions options)
		{
			return 0; // Simplified
		}

		public void GetCharacterUtf32(out uint value, int offset)
		{
			var pos = _startPosition + offset;
			value = pos >= 0 && pos < _document.TextLength
				? (uint)_document.TextBuffer[pos] : 0u;
		}

		public ITextRange GetClone()
		{
			return _document.GetRange(_startPosition, _endPosition);
		}

		public int GetIndex(TextRangeUnit unit) => _startPosition;

		public void GetPoint(HorizontalCharacterAlignment horizontalAlign, VerticalCharacterAlignment verticalAlign, PointOptions options, out global::Windows.Foundation.Point point)
		{
			point = default;
		}

		public void GetRect(PointOptions options, out global::Windows.Foundation.Rect rect, out int hit)
		{
			rect = default;
			hit = 0;
		}

		public void GetText(TextGetOptions options, out string value)
		{
			var start = Math.Min(_startPosition, _endPosition);
			var end = Math.Max(_startPosition, _endPosition);
			value = _document.GetTextInRange(start, end);
			if ((options & TextGetOptions.UseCrlf) != 0)
			{
				value = value.Replace("\r", "\r\n");
			}
		}

		public void GetTextViaStream(TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) { }

		public bool InRange(ITextRange range) =>
			range != null && range.StartPosition >= Math.Min(_startPosition, _endPosition) && range.EndPosition <= Math.Max(_startPosition, _endPosition);

		public void InsertImage(int width, int height, int ascent, VerticalCharacterAlignment verticalAlign, string alternateText, global::Windows.Storage.Streams.IRandomAccessStream value) { }

		public bool InStory(ITextRange range) => range != null;

		public bool IsEqual(ITextRange range) =>
			range != null && range.StartPosition == _startPosition && range.EndPosition == _endPosition;

		public int Move(TextRangeUnit unit, int count) => 0;

		public int MoveEnd(TextRangeUnit unit, int count) => 0;

		public int MoveStart(TextRangeUnit unit, int count) => 0;

		public void Paste(int format) { }

		public void ScrollIntoView(PointOptions value) { }

		public void MatchSelection() { }

		public void SetIndex(TextRangeUnit unit, int index, bool extend) { }

		public void SetPoint(global::Windows.Foundation.Point point, PointOptions options, bool extend) { }

		public void SetRange(int startPosition, int endPosition)
		{
			_startPosition = Math.Max(0, Math.Min(startPosition, _document.TextLength));
			_endPosition = Math.Max(0, Math.Min(endPosition, _document.TextLength));
		}

		public void SetText(TextSetOptions options, string value)
		{
			var text = (value ?? string.Empty).Replace("\r\n", "\r").Replace("\n", "\r");
			var start = Math.Min(_startPosition, _endPosition);
			var end = Math.Max(_startPosition, _endPosition);
			_document.ReplaceText(start, end - start, text);
			_startPosition = start + text.Length;
			_endPosition = _startPosition;
		}

		public void SetTextViaStream(TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) { }

		public int StartOf(TextRangeUnit unit, bool extend)
		{
			var bounds = _document.GetParagraphBounds(_startPosition);
			var newStart = unit switch
			{
				TextRangeUnit.Line or TextRangeUnit.Paragraph => bounds.start,
				TextRangeUnit.Story => 0,
				_ => _startPosition
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

		private int GetWordStart(int position)
		{
			var text = _document.TextBuffer;
			if (position <= 0)
			{
				return 0;
			}

			var pos = Math.Min(position - 1, text.Length - 1);
			while (pos > 0 && char.IsWhiteSpace(text[pos]))
			{
				pos--;
			}

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
			while (pos < text.Length && !char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			while (pos < text.Length && char.IsWhiteSpace(text[pos]))
			{
				pos++;
			}

			return pos;
		}
	}
}
#endif
