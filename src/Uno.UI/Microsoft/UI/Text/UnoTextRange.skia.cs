#nullable enable

using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Text
{
	// Uno-specific functional Text Object Model range over the RichEditBox plain-text buffer.
	//
	// The plain-text navigation and editing surface (positions, Text, SetRange, Collapse, GetText/
	// SetText, FindText, Delete, ChangeCase, Move/MoveStart/MoveEnd, GetClone, InRange/InStory/IsEqual,
	// GetCharacterUtf32, MatchSelection) is functional and drives the shared rendering surface through
	// the owning document. Character formatting (CharacterFormat) is functional over the document's run
	// model. Plain-text clipboard (Copy/Cut/Paste/CanPaste) is functional.
	//
	// TODO Uno: FormattedText/Gravity/Link, rich/RTF clipboard payloads, streams and embedded images
	// arrive with the rich-content model and the shared editing engine.
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
				set
				{
					// ITextRange::SetChar sets the character at the range's starting position, leaving the range
					// length unchanged. At end-of-story there is no character to overwrite, so the value is inserted.
					var length = _document.TextLength;
					if (_start < length)
					{
						_document.ReplaceRange(_start, _start + 1, value.ToString());
					}
					else
					{
						_document.ReplaceRange(_start, _start, value.ToString());
					}

					OnRangeChanged();
				}
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

			// TODO Uno: FindOptions.Word (whole-word matching) is treated as a substring search for now.
			if (scanLength < 0)
			{
				// A negative scanLength searches backward: match the occurrence closest to the range start
				// within the |scanLength| characters preceding it (per the ITextRange::FindText contract).
				var windowStart = Math.Max(0, startIndex + scanLength);
				var windowLength = startIndex - windowStart;
				if (windowLength < value.Length)
				{
					return 0;
				}

				var relative = text.Substring(windowStart, windowLength).LastIndexOf(value, comparison);
				if (relative < 0)
				{
					return 0;
				}

				_start = windowStart + relative;
				_end = _start + value.Length;
				OnRangeChanged();
				return value.Length;
			}

			var available = text.Length - startIndex;
			var span = scanLength == 0 ? available : Math.Min(scanLength, available);
			if (span <= 0)
			{
				return 0;
			}

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

			if (count == 0)
			{
				// TOM ITextRange::Delete: with Count == 0 a degenerate range deletes nothing.
				return 0;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
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

			// Word / Paragraph / Line: delete |count| units in the logical direction (like CTRL+DELETE /
			// CTRL+BACKSPACE), returning the number of units removed (TOM ITextRange::Delete pDelta).
			var boundaries = GetUnitBoundaries(unit);
			if (boundaries is null)
			{
				// Story and unsupported units, or a Line delete before the view is laid out.
				return 0;
			}

			var target = MoveByBoundaries(boundaries, _start, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return 0;
			}

			var rangeStart = Math.Min(_start, target);
			var rangeEnd = Math.Max(_start, target);
			if (rangeEnd <= rangeStart)
			{
				return 0;
			}

			_document.ReplaceRange(rangeStart, rangeEnd, string.Empty);
			_start = _end = rangeStart;
			OnRangeChanged();
			return Math.Abs(unitsMoved);
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

			// TOM ITextRange::Move: "If Count is zero, the range is unchanged." This holds for every unit,
			// including a non-degenerate range (which must NOT collapse).
			if (count == 0)
			{
				return 0;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				if (_start != _end)
				{
					// Collapsing a non-degenerate range toward the direction of travel counts as the first
					// unit moved (TOM), so only Count-1 further characters are traversed from the far edge.
					if (count > 0)
					{
						var edge = _end;
						var target = Math.Clamp(edge + (count - 1), 0, length);
						_start = _end = target;
						OnRangeChanged();
						return (target - edge) + 1;
					}
					else
					{
						var edge = _start;
						var target = Math.Clamp(edge + (count + 1), 0, length);
						_start = _end = target;
						OnRangeChanged();
						return (target - edge) - 1;
					}
				}

				// Degenerate caret: move the full Count.
				var position = _start;
				var caretTarget = Math.Clamp(position + count, 0, length);
				var moved = caretTarget - position;
				_start = _end = caretTarget;
				OnRangeChanged();
				return moved;
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

		// The ascending unit-boundary positions for <paramref name="unit"/> — every unit start plus the
		// end of the story — used by Delete/MoveStart/MoveEnd. Word/Paragraph are text-based; Line is
		// geometry-based (wrap-aware) and requires a laid-out view. Returns null when the unit is not
		// boundary-navigable here (Character/Story are handled directly) or the layout is unavailable.
		private int[]? GetUnitBoundaries(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				return GetLineBoundaries();
			}

			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(GetStoryText(), unit);
			if (chunks is null || chunks.Count == 0)
			{
				return null;
			}

			var boundaries = new int[chunks.Count + 1];
			for (var i = 0; i < chunks.Count; i++)
			{
				boundaries[i] = chunks[i].start;
			}

			boundaries[chunks.Count] = _document.TextLength;
			return boundaries;
		}

		// The ascending start positions of every visual line plus the end of the story, or null when the
		// view is not yet laid out. Lines are contiguous, so the next line starts where the current line's
		// content ends (stepping over a hard carriage return when present).
		private int[]? GetLineBoundaries()
		{
			var text = GetStoryText();
			var length = text.Length;
			var boundaries = new List<int>();
			var probe = 0;
			var guard = 0;
			while (guard++ <= length + 1)
			{
				if (!_document.TryGetLineBounds(probe, out var lineStart, out var lineEnd, out _, out var isLast))
				{
					return null;
				}

				if (boundaries.Count == 0 || boundaries[boundaries.Count - 1] != lineStart)
				{
					boundaries.Add(lineStart);
				}

				if (isLast)
				{
					break;
				}

				// lineEnd stops before a trailing '\r'; the next visual line starts just past it.
				var next = lineEnd + (lineEnd < length && text[lineEnd] == '\r' ? 1 : 0);
				if (next <= probe)
				{
					break;
				}

				probe = next;
			}

			if (boundaries.Count == 0)
			{
				return null;
			}

			if (boundaries[boundaries.Count - 1] != length)
			{
				boundaries.Add(length);
			}

			return boundaries.ToArray();
		}

		// Moves <paramref name="position"/> by <paramref name="count"/> unit boundaries along
		// <paramref name="boundaries"/> and reports the signed number of units actually crossed. Mirrors
		// the boundary math used by Move so all unit navigation stays consistent.
		private static int MoveByBoundaries(int[] boundaries, int position, int count, out int unitsMoved)
		{
			if (count > 0)
			{
				var index = -1;
				for (var i = 0; i < boundaries.Length; i++)
				{
					if (boundaries[i] > position)
					{
						index = i;
						break;
					}
				}

				if (index < 0)
				{
					unitsMoved = 0;
					return position;
				}

				var targetIndex = Math.Min(index + (count - 1), boundaries.Length - 1);
				unitsMoved = targetIndex - index + 1;
				return boundaries[targetIndex];
			}
			else
			{
				var index = -1;
				for (var i = boundaries.Length - 1; i >= 0; i--)
				{
					if (boundaries[i] < position)
					{
						index = i;
						break;
					}
				}

				if (index < 0)
				{
					unitsMoved = 0;
					return position;
				}

				var targetIndex = Math.Max(index - (-count - 1), 0);
				unitsMoved = -(index - targetIndex + 1);
				return boundaries[targetIndex];
			}
		}


		public int MoveStart(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var length = _document.TextLength;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				var oldChar = _start;
				_start = Math.Clamp(_start + count, 0, length);
				if (_start > _end)
				{
					_end = _start;
				}

				OnRangeChanged();
				return _start - oldChar;
			}

			if (count == 0)
			{
				return 0;
			}

			var boundaries = GetUnitBoundaries(unit);
			if (boundaries is null)
			{
				return 0;
			}

			var target = MoveByBoundaries(boundaries, _start, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return 0;
			}

			_start = Math.Clamp(target, 0, length);
			if (_start > _end)
			{
				_end = _start;
			}

			OnRangeChanged();
			return unitsMoved;
		}

		public int MoveEnd(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var length = _document.TextLength;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				var oldChar = _end;
				_end = Math.Clamp(_end + count, 0, length);
				if (_end < _start)
				{
					_start = _end;
				}

				OnRangeChanged();
				return _end - oldChar;
			}

			if (count == 0)
			{
				return 0;
			}

			var boundaries = GetUnitBoundaries(unit);
			if (boundaries is null)
			{
				return 0;
			}

			var target = MoveByBoundaries(boundaries, _end, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return 0;
			}

			_end = Math.Clamp(target, 0, length);
			if (_end < _start)
			{
				_start = _end;
			}

			OnRangeChanged();
			return unitsMoved;
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

		// --- Paragraph formatting (functional over the document paragraph run model) ---

		public global::Microsoft.UI.Text.ITextParagraphFormat ParagraphFormat
		{
			get
			{
				var format = _document.GetParagraphFormatOverRange(_start, _end);
				format.Bind(this);
				return format;
			}
			set
			{
				if (value is UnoTextParagraphFormat format)
				{
					_document.SetParagraphFormatOverRange(_start, _end, format);
				}
			}
		}

		// Applies a bound paragraph format to the paragraphs touched by this range. Called by
		// UnoTextParagraphFormat's setters so `range.ParagraphFormat.Alignment = Center` takes effect.
		internal void ApplyParagraphFormat(UnoTextParagraphFormat format)
			=> _document.SetParagraphFormatOverRange(_start, _end, format);

		// --- Deferred surface (clipboard, geometry, streams, embedded images) ---

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

		public bool CanPaste(int format) => _document.CanPaste();

		public virtual void Copy()
		{
			// Plain text is written to the OS clipboard; when ClipboardCopyFormat is AllFormats the
			// span's character formatting is preserved for a matching paste via an in-process payload.
			if (_start != _end)
			{
				_document.CopyToClipboard(_start, _end);
			}
		}

		public virtual void Cut()
		{
			if (_start == _end)
			{
				return;
			}

			_document.CopyToClipboard(_start, _end);
			_document.ReplaceRange(_start, _end, string.Empty);
			_end = _start;
			OnRangeChanged();
		}

		public virtual void Paste(int format)
		{
			// The OS clipboard read is async on Uno, so unlike WinUI's synchronous paste this replaces the
			// range on a later dispatcher turn. A matching in-process rich payload restores formatting; the
			// 'format' argument (a RichEdit clipboard-format id) is not used to select an OS payload.
			var start = _start;
			var end = _end;
			_document.BeginPastePlainText(start, end, caret =>
			{
				_start = _end = caret;
				OnRangeChanged();
			});
		}

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
			else if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				// The visual line end is geometry-based (wrap-aware); probe end-1 for a non-degenerate range.
				var probe = _end > _start ? _end - 1 : _end;
				if (!_document.TryGetLineBounds(probe, out _, out var lineEnd, out _, out _))
				{
					return 0;
				}

				target = lineEnd;
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
			else if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				if (!_document.TryGetLineBounds(_start, out var lineStart, out _, out _, out _))
				{
					return 0;
				}

				target = lineStart;
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

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				if (!_document.TryGetLineBounds(_start, out var lineStart, out _, out _, out _))
				{
					return 0;
				}

				var probe = _end > _start ? _end - 1 : _end;
				_document.TryGetLineBounds(probe, out _, out var lineEnd, out _, out _);
				_start = lineStart;
				_end = Math.Max(lineEnd, lineStart);
				OnRangeChanged();
				return (_end - _start) - originalLength;
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

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				return _document.TryGetLineBounds(_start, out _, out _, out var lineIndex, out _) ? lineIndex + 1 : 0;
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

		public void GetCharacterUtf32(out uint value, int offset)
		{
			var text = _document.GetTextInRange(0, _document.TextLength);
			var position = _start + offset;
			if (position < 0 || position >= text.Length)
			{
				// Out of range yields 0 (WinUI reports the null character past the story end).
				value = 0;
				return;
			}

			// Combine a surrogate pair into a single UTF-32 code point; otherwise the char is the value.
			value = char.IsHighSurrogate(text[position]) && position + 1 < text.Length && char.IsLowSurrogate(text[position + 1])
				? (uint)char.ConvertToUtf32(text[position], text[position + 1])
				: text[position];
		}

		public void GetPoint(global::Microsoft.UI.Text.HorizontalCharacterAlignment horizontalAlign, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Point point)
		{
			// The point is taken at the range's start when PointOptions.Start is set, otherwise at its
			// (active) end, mirroring WinUI's tomStart/tomEnd anchoring.
			point = default;
			var anchor = options.HasFlag(global::Microsoft.UI.Text.PointOptions.Start) ? _start : _end;
			if (!_document.TryGetIndexRect(anchor, options, out var rect))
			{
				return;
			}

			var x = horizontalAlign switch
			{
				global::Microsoft.UI.Text.HorizontalCharacterAlignment.Right => rect.X + rect.Width,
				global::Microsoft.UI.Text.HorizontalCharacterAlignment.Center => rect.X + (rect.Width / 2),
				_ => rect.X,
			};
			var y = verticalAlign switch
			{
				global::Microsoft.UI.Text.VerticalCharacterAlignment.Top => rect.Y,
				// TODO Uno: Baseline is approximated as the line bottom (no per-line baseline metric here).
				_ => rect.Y + rect.Height,
			};
			point = new global::Windows.Foundation.Point(x, y);
		}

		public void GetRect(global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect, out int hit)
		{
			// TODO Uno: 'hit' (WinUI reports a RichEdit hit-test/clip indicator) is reported as 0. The
			// rect is exact for degenerate and single-line ranges and the endpoint bounding box for
			// multi-line ranges.
			hit = 0;
			_document.TryGetRangeRect(_start, _end, options, out rect);
		}

		public void ScrollIntoView(global::Microsoft.UI.Text.PointOptions value)
			=> _document.TryScrollRangeIntoView(_start, _end);

		public void SetPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, bool extend)
		{
			if (!_document.TryGetIndexFromPoint(point, options, out var index))
			{
				return;
			}

			if (extend)
			{
				// Extend the range to the hit position, keeping the current anchor.
				SetRange(_start, index);
			}
			else
			{
				SetRange(index, index);
			}
		}

		public void MatchSelection()
		{
			// WinUI sets the ACTIVE SELECTION to match this range's positions (and raises SelectionChanged),
			// despite the API docs' inverted wording. Verified against the shipping product's own tests:
			// RichEditBoxTOMTests moves the selection onto this range, and TextEditingTests requires a single
			// SelectionChanged. This is the reverse of copying the selection into this range.
			_document.Selection.SetRange(_start, _end);
		}

		public void GetTextViaStream(global::Microsoft.UI.Text.TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("GetTextViaStream");

		public void SetTextViaStream(global::Microsoft.UI.Text.TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("SetTextViaStream");

		public void InsertImage(int width, int height, int ascent, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, string alternateText, global::Windows.Storage.Streams.IRandomAccessStream value) => throw NotImplemented("InsertImage");
	}
}
