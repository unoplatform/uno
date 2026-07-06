#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific functional Text Object Model range over the RichEditBox plain-text buffer.
	//
	// The plain-text navigation and editing surface (positions, Text, SetRange, Collapse, GetText/
	// SetText, FindText, Delete, ChangeCase, Move/MoveStart/MoveEnd, GetClone, InRange/InStory/IsEqual)
	// is functional and drives the shared rendering surface through the owning document. Character
	// formatting (CharacterFormat) is functional over the document's run model.
	//
	// TODO Uno: Paragraph formatting (ParagraphFormat/FormattedText/Gravity/Link), clipboard (Copy/Cut/
	// Paste/CanPaste), geometry (GetPoint/GetRect/ScrollIntoView/SetPoint), unit-based navigation beyond
	// Character (Expand/StartOf/EndOf/GetIndex/SetIndex), streams and embedded images arrive with the
	// rich-content model and the shared editing engine.
	internal class UnoTextRange : global::Microsoft.UI.Text.ITextRange
	{
		private protected readonly RichEditTextDocument _document;
		private protected int _start;
		private protected int _end;

		internal UnoTextRange(RichEditTextDocument document, int startPosition, int endPosition)
		{
			_document = document;
			_start = startPosition;
			_end = endPosition;
			Normalize();
		}

		private protected void Normalize()
		{
			var length = _document.TextLength;
			_start = Math.Clamp(_start, 0, length);
			_end = Math.Clamp(_end, 0, length);
			if (_start > _end)
			{
				(_start, _end) = (_end, _start);
			}
		}

		/// <summary>
		/// Called after a public API mutation changes this range's positions. The base range does not
		/// react; <see cref="UnoTextSelection"/> overrides this to sync the owning control's interactive
		/// caret/selection (the reverse of <see cref="SetRangeInternal"/>, which is the control pushing in
		/// and therefore does NOT call this).
		/// </summary>
		private protected virtual void OnRangeChanged()
		{
		}

		private static Exception NotImplemented(string member)
			=> new NotImplementedException($"Microsoft.UI.Text range member '{member}' is not yet implemented on Uno. TODO Uno: arrives with the rich-content model / shared editing engine.");

		public int StartPosition
		{
			get => _start;
			set
			{
				_start = Math.Clamp(value, 0, _document.TextLength);
				// WinUI: moving StartPosition past EndPosition drags EndPosition with it.
				if (_start > _end)
				{
					_end = _start;
				}

				OnRangeChanged();
			}
		}

		public int EndPosition
		{
			get => _end;
			set
			{
				_end = Math.Clamp(value, 0, _document.TextLength);
				// WinUI: moving EndPosition before StartPosition drags StartPosition with it.
				if (_end < _start)
				{
					_start = _end;
				}

				OnRangeChanged();
			}
		}

		/// <summary>
		/// Sets this range's positions directly, bypassing the WinUI "drag the other end" semantics of
		/// the public <see cref="StartPosition"/>/<see cref="EndPosition"/> setters. Used to mirror the
		/// interactive caret/selection into the Text Object Model without side effects.
		/// </summary>
		internal void SetRangeInternal(int start, int end)
		{
			_start = start;
			_end = end;
			Normalize();
		}

		public int Length => _end - _start;

		public int StoryLength => _document.TextLength + 1;

		public string Text
		{
			get => _document.GetTextInRange(_start, _end);
			set
			{
				var replacement = value ?? string.Empty;
				_document.ReplaceRange(_start, _end, replacement);
				_end = _start + replacement.Length;
				OnRangeChanged();
			}
		}

		public char Character
		{
			get
			{
				var length = _document.TextLength;
				if (_start < length)
				{
					return _document.GetTextInRange(_start, _start + 1)[0];
				}

				// The end-of-story is conventionally represented by a carriage return in the TOM.
				return '\r';
			}
			// TODO Uno: Character setter mutates a single code point in place; needs the rich model.
			set => throw NotImplemented("Character.set");
		}

		public void SetRange(int startPosition, int endPosition)
		{
			_start = startPosition;
			_end = endPosition;
			Normalize();
			OnRangeChanged();
		}

		public void Collapse(bool value)
		{
			// value == true collapses to the start position, false to the end position.
			if (value)
			{
				_end = _start;
			}
			else
			{
				_start = _end;
			}

			OnRangeChanged();
		}

		public void GetText(global::Microsoft.UI.Text.TextGetOptions options, out string value)
		{
			// TODO Uno: Honor the rich TextGetOptions once formatted content is supported.
			value = _document.GetTextInRange(_start, _end);
		}

		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			// TODO Uno: Honor the rich TextSetOptions once formatted content is supported.
			var replacement = value ?? string.Empty;
			_document.ReplaceRange(_start, _end, replacement);
			_end = _start + replacement.Length;
			OnRangeChanged();
		}

		public global::Microsoft.UI.Text.ITextRange GetClone()
			=> new UnoTextRange(_document, _start, _end);

		public bool InRange(global::Microsoft.UI.Text.ITextRange range)
			=> range is not null && _start >= range.StartPosition && _end <= range.EndPosition;

		public bool InStory(global::Microsoft.UI.Text.ITextRange range)
			=> range is UnoTextRange other && ReferenceEquals(other._document, _document);

		public bool IsEqual(global::Microsoft.UI.Text.ITextRange range)
			=> range is UnoTextRange other
				&& ReferenceEquals(other._document, _document)
				&& other._start == _start
				&& other._end == _end;

		public int FindText(string value, int scanLength, global::Microsoft.UI.Text.FindOptions options)
		{
			if (string.IsNullOrEmpty(value))
			{
				return 0;
			}

			var text = _document.GetTextInRange(0, _document.TextLength);
			var comparison = options.HasFlag(global::Microsoft.UI.Text.FindOptions.Case)
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;

			var startIndex = Math.Clamp(_start, 0, text.Length);
			var available = text.Length - startIndex;
			var span = scanLength <= 0 ? available : Math.Min(scanLength, available);
			if (span <= 0)
			{
				return 0;
			}

			// TODO Uno: FindOptions.Word (whole-word matching) is treated as a substring search for now.
			var index = text.IndexOf(value, startIndex, span, comparison);
			if (index < 0)
			{
				return 0;
			}

			_start = index;
			_end = index + value.Length;
			OnRangeChanged();
			return value.Length;
		}

		public int Delete(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var length = _document.TextLength;

			if (_start != _end)
			{
				// A non-degenerate range deletes its own content regardless of unit/count.
				var removed = _end - _start;
				_document.ReplaceRange(_start, _end, string.Empty);
				_end = _start;
				OnRangeChanged();
				return removed;
			}

			if (count == 0 || unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// TODO Uno: units other than Character are not yet supported for degenerate ranges.
				return 0;
			}

			int deleteStart, deleteEnd;
			if (count > 0)
			{
				deleteStart = _start;
				deleteEnd = Math.Clamp(_start + count, 0, length);
			}
			else
			{
				deleteEnd = _start;
				deleteStart = Math.Clamp(_start + count, 0, length);
			}

			var deleted = deleteEnd - deleteStart;
			_document.ReplaceRange(deleteStart, deleteEnd, string.Empty);
			_start = _end = deleteStart;
			OnRangeChanged();
			return deleted;
		}

		public void ChangeCase(global::Microsoft.UI.Text.LetterCase value)
		{
			var text = _document.GetTextInRange(_start, _end);
			if (text.Length == 0)
			{
				return;
			}

			var changed = value == global::Microsoft.UI.Text.LetterCase.Upper
				? text.ToUpperInvariant()
				: text.ToLowerInvariant();
			_document.ReplaceRange(_start, _end, changed);
		}

		public int Move(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var length = _document.TextLength;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// A non-degenerate range collapses toward the direction of travel before moving.
				if (_start != _end)
				{
					if (count >= 0)
					{
						_start = _end;
					}
					else
					{
						_end = _start;
					}
				}

				var position = count >= 0 ? _end : _start;
				var target = Math.Clamp(position + count, 0, length);
				var moved = target - position;
				_start = _end = target;
				OnRangeChanged();
				return moved;
			}

			if (count == 0)
			{
				// WinUI: Move with count 0 collapses the range to its start and reports no movement.
				if (_start != _end)
				{
					_end = _start;
					OnRangeChanged();
				}

				return 0;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				var ip = count > 0 ? length : 0;
				_start = _end = ip;
				OnRangeChanged();
				return count > 0 ? 1 : -1;
			}

			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
			if (chunks is null || chunks.Count == 0)
			{
				// TODO Uno: Line/Sentence/Screen units require the layout-aware editing engine.
				return 0;
			}

			// Unit boundaries are the chunk starts plus the end-of-story position.
			var boundaries = new int[chunks.Count + 1];
			for (var i = 0; i < chunks.Count; i++)
			{
				boundaries[i] = chunks[i].start;
			}

			boundaries[chunks.Count] = length;

			// Collapse a non-degenerate range toward the direction of travel first.
			var insertion = count > 0 ? _end : _start;

			int movedUnits;
			int destination;
			if (count > 0)
			{
				var index = -1;
				for (var i = 0; i < boundaries.Length; i++)
				{
					if (boundaries[i] > insertion)
					{
						index = i;
						break;
					}
				}

				if (index < 0)
				{
					return 0;
				}

				var targetIndex = Math.Min(index + (count - 1), boundaries.Length - 1);
				destination = boundaries[targetIndex];
				movedUnits = targetIndex - index + 1;
			}
			else
			{
				var index = -1;
				for (var i = boundaries.Length - 1; i >= 0; i--)
				{
					if (boundaries[i] < insertion)
					{
						index = i;
						break;
					}
				}

				if (index < 0)
				{
					return 0;
				}

				var targetIndex = Math.Max(index - (-count - 1), 0);
				destination = boundaries[targetIndex];
				movedUnits = -(index - targetIndex + 1);
			}

			_start = _end = destination;
			OnRangeChanged();
			return movedUnits;
		}

		// The full story text; the basis for text-based (non-geometry) unit navigation.
		private protected string GetStoryText() => _document.GetTextInRange(0, _document.TextLength);

		public int MoveStart(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				return 0;
			}

			var length = _document.TextLength;
			var old = _start;
			_start = Math.Clamp(_start + count, 0, length);
			if (_start > _end)
			{
				_end = _start;
			}

			OnRangeChanged();
			return _start - old;
		}

		public int MoveEnd(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				return 0;
			}

			var length = _document.TextLength;
			var old = _end;
			_end = Math.Clamp(_end + count, 0, length);
			if (_end < _start)
			{
				_start = _end;
			}

			OnRangeChanged();
			return _end - old;
		}

		// --- Character formatting (functional over the document run model) ---

		public global::Microsoft.UI.Text.ITextCharacterFormat CharacterFormat
		{
			get
			{
				var format = _document.GetFormatOverRange(_start, _end);
				format.Bind(this);
				return format;
			}
			set
			{
				if (value is UnoTextCharacterFormat format)
				{
					_document.SetFormatOverRange(_start, _end, format);
				}
			}
		}

		// Applies a bound character format to this range's current extent. Called by
		// UnoTextCharacterFormat's property setters so `range.CharacterFormat.Bold = On` takes effect.
		internal void ApplyCharacterFormat(UnoTextCharacterFormat format)
			=> _document.SetFormatOverRange(_start, _end, format);

		// --- Deferred surface (paragraph formatting, clipboard, geometry, streams, embedded images) ---

		public global::Microsoft.UI.Text.ITextParagraphFormat ParagraphFormat
		{
			get => throw NotImplemented("ParagraphFormat.get");
			set => throw NotImplemented("ParagraphFormat.set");
		}

		public global::Microsoft.UI.Text.ITextRange FormattedText
		{
			get => throw NotImplemented("FormattedText.get");
			set => throw NotImplemented("FormattedText.set");
		}

		public global::Microsoft.UI.Text.RangeGravity Gravity
		{
			get => throw NotImplemented("Gravity.get");
			set => throw NotImplemented("Gravity.set");
		}

		public string Link
		{
			get => throw NotImplemented("Link.get");
			set => throw NotImplemented("Link.set");
		}

		public bool CanPaste(int format) => false;

		public void Copy() => throw NotImplemented("Copy");

		public void Cut() => throw NotImplemented("Cut");

		public void Paste(int format) => throw NotImplemented("Paste");

		// --- Text-based unit navigation (Word/Paragraph/Story) — functional over the plain-text buffer ---

		public int EndOf(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			int target;
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				target = _document.TextLength;
			}
			else if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// The end of a single character is one position forward from a degenerate range.
				target = Math.Min(_end + (_start == _end ? 1 : 0), _document.TextLength);
			}
			else
			{
				var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
				if (chunks is null || chunks.Count == 0)
				{
					return 0;
				}

				// Probe the unit the range end sits within (end-1 for a non-degenerate range so a
				// selection ending on a boundary is not pulled into the following unit).
				var probe = _end > _start ? _end - 1 : _end;
				var chunk = chunks[global::Microsoft.UI.Text.TextUnitNavigation.FindChunkIndex(chunks, probe)];
				target = chunk.start + chunk.length;
			}

			var old = _end;
			_end = target;
			if (!extend)
			{
				_start = target;
			}
			else if (_start > _end)
			{
				_start = _end;
			}

			OnRangeChanged();
			return _end - old;
		}

		public int StartOf(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			int target;
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				target = 0;
			}
			else if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// The start of a character is the range's own start; nothing to move.
				target = _start;
			}
			else
			{
				var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
				if (chunks is null || chunks.Count == 0)
				{
					return 0;
				}

				target = chunks[global::Microsoft.UI.Text.TextUnitNavigation.FindChunkIndex(chunks, _start)].start;
			}

			var old = _start;
			_start = target;
			if (!extend)
			{
				_end = target;
			}
			else if (_end < _start)
			{
				_end = _start;
			}

			OnRangeChanged();
			return _start - old;
		}

		public int Expand(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			var originalLength = _end - _start;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				_start = 0;
				_end = _document.TextLength;
				OnRangeChanged();
				return (_end - _start) - originalLength;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// Expanding a caret by one character selects the following character.
				if (_start == _end && _start < _document.TextLength)
				{
					_end = _start + 1;
					OnRangeChanged();
					return 1;
				}

				return 0;
			}

			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
			if (chunks is null || chunks.Count == 0)
			{
				return 0;
			}

			var startChunk = chunks[global::Microsoft.UI.Text.TextUnitNavigation.FindChunkIndex(chunks, _start)];
			var probeEnd = _end > _start ? _end - 1 : _end;
			var endChunk = chunks[global::Microsoft.UI.Text.TextUnitNavigation.FindChunkIndex(chunks, probeEnd)];
			_start = startChunk.start;
			_end = endChunk.start + endChunk.length;
			OnRangeChanged();
			return (_end - _start) - originalLength;
		}

		public int GetIndex(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				return 1;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				// 1-based character index of the range start; 0 when past the end of the story.
				return _start >= _document.TextLength && _document.TextLength > 0 ? 0 : _start + 1;
			}

			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
			if (chunks is null || chunks.Count == 0)
			{
				return 0;
			}

			// TODO Uno: WinUI returns 0 when the range start is past the last unit; the exact
			// end-of-story boundary behaviour still needs runtime verification against WinUI.
			return global::Microsoft.UI.Text.TextUnitNavigation.FindChunkIndex(chunks, _start) + 1;
		}

		public void SetIndex(global::Microsoft.UI.Text.TextRangeUnit unit, int index, bool extend)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				var length = _document.TextLength;
				var target = index >= 0 ? index - 1 : length + index;
				target = Math.Clamp(target, 0, length);
				_start = target;
				if (!extend)
				{
					_end = target;
				}
				else if (_end < _start)
				{
					_end = _start;
				}

				OnRangeChanged();
				return;
			}

			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
			if (chunks is null || chunks.Count == 0)
			{
				return;
			}

			// A 1-based index; a negative index counts back from the last unit.
			var chunkIndex = index >= 0 ? index - 1 : chunks.Count + index;
			chunkIndex = Math.Clamp(chunkIndex, 0, chunks.Count - 1);
			var chunk = chunks[chunkIndex];
			_start = chunk.start;
			if (!extend)
			{
				_end = chunk.start + chunk.length;
			}
			else if (_end < _start)
			{
				_end = _start;
			}

			OnRangeChanged();
		}

		public void GetCharacterUtf32(out uint value, int offset) => throw NotImplemented("GetCharacterUtf32");

		public void GetPoint(global::Microsoft.UI.Text.HorizontalCharacterAlignment horizontalAlign, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Point point) => throw NotImplemented("GetPoint");

		public void GetRect(global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect, out int hit) => throw NotImplemented("GetRect");

		public void ScrollIntoView(global::Microsoft.UI.Text.PointOptions value) => throw NotImplemented("ScrollIntoView");

		public void SetPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, bool extend) => throw NotImplemented("SetPoint");

		public void MatchSelection() => throw NotImplemented("MatchSelection");

		public void GetTextViaStream(global::Microsoft.UI.Text.TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("GetTextViaStream");

		public void SetTextViaStream(global::Microsoft.UI.Text.TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("SetTextViaStream");

		public void InsertImage(int width, int height, int ascent, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, string alternateText, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("InsertImage");
	}
}
