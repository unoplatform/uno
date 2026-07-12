#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
			_document.TrackRange(this);
		}

		internal void RebaseAfterEdit(int editStart, int editEnd, int insertLength, int documentLength)
		{
			_start = RebasePosition(_start, editStart, editEnd, insertLength);
			_end = RebasePosition(_end, editStart, editEnd, insertLength);
			_start = Math.Clamp(_start, 0, documentLength);
			_end = Math.Clamp(_end, 0, documentLength);
			if (_start > _end)
			{
				(_start, _end) = (_end, _start);
			}
		}

		private static int RebasePosition(int position, int editStart, int editEnd, int insertLength)
		{
			if (position < editStart)
			{
				return position;
			}

			if (position > editEnd || (editStart == editEnd && position == editEnd))
			{
				return position + insertLength - (editEnd - editStart);
			}

			return editStart + insertLength;
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
				var insertedLength = _document.ReplaceRange(_start, _end, replacement, this);
				_end = _start + insertedLength;
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
					_document.ReplaceRange(_start, _start + 1, value.ToString(), this);
				}
				else
				{
					_document.ReplaceRange(_start, _start, value.ToString(), this);
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
			value = _document.GetTextInRange(_start, _end, options);
		}

		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			// TODO Uno: Honor the rich TextSetOptions once formatted content is supported.
			var replacement = value ?? string.Empty;
			var insertedLength = _document.ReplaceRange(_start, _end, replacement, this);
			_end = _start + insertedLength;
			OnRangeChanged();
		}

		public global::Microsoft.UI.Text.ITextRange GetClone()
			=> new UnoTextRange(_document, _start, _end);

		public bool InRange(global::Microsoft.UI.Text.ITextRange range)
			=> range is UnoTextRange other
				&& ReferenceEquals(other._document, _document)
				&& _start >= other.StartPosition
				&& _end <= other.EndPosition;

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
			var matchWholeWord = options.HasFlag(global::Microsoft.UI.Text.FindOptions.Word);
			var textElementBoundaries = matchWholeWord ? TextUnitNavigation.GetTextElementBoundaries(text) : null;
			var originalStart = Math.Clamp(_start, 0, text.Length);
			var originalEnd = Math.Clamp(_end, originalStart, text.Length);
			var currentRangeMatches = originalEnd - originalStart == value.Length
				&& IsFindMatch(text, value, originalStart, comparison, textElementBoundaries);

			int searchStart;
			int searchEnd;
			if (scanLength > 0)
			{
				searchStart = currentRangeMatches ? originalStart + 1 : originalStart;
				searchEnd = (int)Math.Min(text.Length, (long)originalStart + scanLength);
			}
			else if (scanLength < 0)
			{
				searchStart = (int)Math.Max(0, (long)originalEnd + scanLength);
				searchEnd = currentRangeMatches ? originalEnd - 1 : originalEnd;
			}
			else if (originalStart == originalEnd)
			{
				searchStart = originalEnd;
				searchEnd = text.Length;
			}
			else
			{
				searchStart = originalStart;
				searchEnd = originalEnd;
			}

			var match = scanLength < 0
				? FindBackward(text, value, searchStart, searchEnd, comparison, textElementBoundaries)
				: FindForward(text, value, searchStart, searchEnd, comparison, textElementBoundaries);
			if (match < 0)
			{
				return 0;
			}

			_start = match;
			_end = match + value.Length;
			OnRangeChanged();
			return value.Length;
		}

		private static int FindForward(string text, string value, int searchStart, int searchEnd, StringComparison comparison, int[]? textElementBoundaries)
		{
			var lastCandidate = searchEnd - value.Length;
			var candidate = searchStart;
			while (candidate <= lastCandidate)
			{
				var match = text.IndexOf(value, candidate, searchEnd - candidate, comparison);
				if (match < 0 || match > lastCandidate)
				{
					return -1;
				}

				if (IsFindMatch(text, value, match, comparison, textElementBoundaries))
				{
					return match;
				}

				candidate = match + 1;
			}

			return -1;
		}

		private static int FindBackward(string text, string value, int searchStart, int searchEnd, StringComparison comparison, int[]? textElementBoundaries)
		{
			var lastCandidate = searchEnd - value.Length;
			var candidate = searchStart;
			var lastMatch = -1;
			while (candidate <= lastCandidate)
			{
				var match = text.IndexOf(value, candidate, searchEnd - candidate, comparison);
				if (match < 0 || match > lastCandidate)
				{
					break;
				}

				if (IsFindMatch(text, value, match, comparison, textElementBoundaries))
				{
					lastMatch = match;
				}

				candidate = match + 1;
			}

			return lastMatch;
		}

		private static bool IsFindMatch(string text, string value, int start, StringComparison comparison, int[]? textElementBoundaries)
		{
			if (start < 0 || start > text.Length - value.Length
				|| !text.AsSpan(start, value.Length).Equals(value.AsSpan(), comparison))
			{
				return false;
			}

			if (textElementBoundaries is null)
			{
				return true;
			}

			var end = start + value.Length;
			return IsFindWordBoundary(text, start, textElementBoundaries)
				&& IsFindWordBoundary(text, end, textElementBoundaries);
		}

		private static bool IsFindWordBoundary(string text, int position, int[] textElementBoundaries)
		{
			var boundaryIndex = Array.BinarySearch(textElementBoundaries, position);
			if (boundaryIndex < 0)
			{
				return false;
			}

			if (position == 0 || position == text.Length)
			{
				return true;
			}

			return GetFindCharacterClass(text, textElementBoundaries[boundaryIndex - 1])
				!= GetFindCharacterClass(text, textElementBoundaries[boundaryIndex]);
		}

		private static int GetFindCharacterClass(string text, int start)
		{
			if (!Rune.TryGetRuneAt(text, start, out var value))
			{
				return 2;
			}

			if (Rune.IsLetterOrDigit(value))
			{
				return 0;
			}

			var category = Rune.GetUnicodeCategory(value);
			if (category is UnicodeCategory.NonSpacingMark
				or UnicodeCategory.SpacingCombiningMark
				or UnicodeCategory.EnclosingMark
				or UnicodeCategory.ConnectorPunctuation)
			{
				return 0;
			}

			return Rune.IsWhiteSpace(value) ? 1 : 2;
		}

		public int Delete(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			if (_start != _end)
			{
				_document.BeginUndoGroup();
				_document.BatchDisplayUpdates();
				try
				{
					_document.ReplaceRange(_start, _end, string.Empty, this);
					_end = _start;

					var additionalRequested = Math.Abs((long)count) - 1;
					var additionalCount = additionalRequested <= 0
						? 0
						: (int)Math.Min(additionalRequested, _document.TextLength);
					var additionalDeleted = additionalCount == 0
						? 0
						: DeleteCollapsed(unit, count > 0 ? additionalCount : -additionalCount, notify: false);
					OnRangeChanged();
					return 1 + additionalDeleted;
				}
				finally
				{
					try
					{
						_document.EndUndoGroup();
					}
					finally
					{
						_document.ApplyDisplayUpdates();
					}
				}
			}

			return DeleteCollapsed(unit, count, notify: true);
		}

		private int DeleteCollapsed(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool notify)
		{
			var length = _document.TextLength;

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
					deleteEnd = (int)Math.Clamp((long)_start + count, 0, length);
				}
				else
				{
					deleteEnd = _start;
					deleteStart = (int)Math.Clamp((long)_start + count, 0, length);
				}

				var deleted = deleteEnd - deleteStart;
				_document.ReplaceRange(deleteStart, deleteEnd, string.Empty, this);
				_start = _end = deleteStart;
				if (notify)
				{
					OnRangeChanged();
				}

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

			_document.ReplaceRange(rangeStart, rangeEnd, string.Empty, this);
			_start = _end = rangeStart;
			if (notify)
			{
				OnRangeChanged();
			}

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
			_document.ReplaceRange(_start, _end, changed, this);
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
						var target = (int)Math.Clamp((long)edge + count - 1, 0, length);
						_start = _end = target;
						OnRangeChanged();
						return (target - edge) + 1;
					}
					else
					{
						var edge = _start;
						var target = (int)Math.Clamp((long)edge + count + 1, 0, length);
						_start = _end = target;
						OnRangeChanged();
						return (target - edge) - 1;
					}
				}

				// Degenerate caret: move the full Count.
				var position = _start;
				var caretTarget = (int)Math.Clamp((long)position + count, 0, length);
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

				var targetIndex = (int)Math.Min((long)index + count - 1, boundaries.Length - 1);
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

				var targetIndex = (int)Math.Max((long)index - (-(long)count - 1), 0);
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

				var targetIndex = (int)Math.Min((long)index + count - 1, boundaries.Length - 1);
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

				var targetIndex = (int)Math.Max((long)index - (-(long)count - 1), 0);
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
				_start = (int)Math.Clamp((long)_start + count, 0, length);
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
				_end = (int)Math.Clamp((long)_end + count, 0, length);
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
			_document.ReplaceRange(_start, _end, string.Empty, this);
			_end = _start;
			OnRangeChanged();
		}

		public virtual void Paste(int format)
		{
			// The OS clipboard read is async on Uno, so unlike WinUI's synchronous paste this replaces the
			// range on a later dispatcher turn. A matching in-process rich payload restores formatting; the
			// 'format' argument (a RichEdit clipboard-format id) is not used to select an OS payload.
			var operationRange = new UnoTextRange(_document, _start, _end);
			_document.BeginPastePlainText(operationRange, caret =>
			{
				_start = _end = caret;
				OnRangeChanged();
			}, requireEditable: this is UnoTextSelection);
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
				return _start + 1;
			}

			var units = GetIndexedUnitRanges(unit);
			if (units is null || units.Count == 0)
			{
				return 0;
			}

			for (var i = 0; i < units.Count; i++)
			{
				var current = units[i];
				if (_start < current.end || (current.start == current.end && _start == current.start))
				{
					return i + 1;
				}
			}

			return units.Count;
		}

		public void SetIndex(global::Microsoft.UI.Text.TextRangeUnit unit, int index, bool extend)
		{
			if (!TryGetIndexedUnit(unit, index, out var unitStart, out var unitEnd))
			{
				return;
			}

			if (extend)
			{
				_end = unitEnd;
				if (_end < _start)
				{
					_start = _end;
				}
			}
			else
			{
				_start = _end = unitStart;
			}

			OnRangeChanged();
		}

		private bool TryGetIndexedUnit(global::Microsoft.UI.Text.TextRangeUnit unit, int index, out int unitStart, out int unitEnd)
		{
			unitStart = 0;
			unitEnd = 0;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				ValidateUnitIndex(index, 1);
				unitEnd = _document.TextLength;
				return true;
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				var length = _document.TextLength;
				unitStart = GetUnitIndex(index, length + 1);
				unitEnd = Math.Min(unitStart + 1, length);
				return true;
			}

			var units = GetIndexedUnitRanges(unit);
			if (units is null || units.Count == 0)
			{
				return false;
			}

			var indexedUnit = units[GetUnitIndex(index, units.Count)];
			unitStart = indexedUnit.start;
			unitEnd = indexedUnit.end;
			return true;
		}

		private List<(int start, int end)>? GetIndexedUnitRanges(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				return GetLineRanges();
			}

			var text = GetStoryText();
			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetChunks(text, unit);
			if (chunks is null)
			{
				return null;
			}

			var units = new List<(int start, int end)>(chunks.Count + 1);
			foreach (var chunk in chunks)
			{
				units.Add((chunk.start, chunk.start + chunk.length));
			}

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Word
				|| (unit == global::Microsoft.UI.Text.TextRangeUnit.Paragraph
					&& (text.Length == 0 || text[text.Length - 1] is '\r' or '\n')))
			{
				units.Add((text.Length, text.Length));
			}

			return units;
		}

		private List<(int start, int end)>? GetLineRanges()
		{
			var text = GetStoryText();
			var starts = new List<int>();
			var probe = 0;
			var guard = 0;
			while (guard++ <= text.Length + 1)
			{
				if (!_document.TryGetLineBounds(probe, out var lineStart, out var lineEnd, out _, out var isLast))
				{
					return null;
				}

				if (starts.Count == 0 || starts[starts.Count - 1] != lineStart)
				{
					starts.Add(lineStart);
				}

				if (isLast)
				{
					break;
				}

				var next = lineEnd + (lineEnd < text.Length && text[lineEnd] == '\r' ? 1 : 0);
				if (next <= probe)
				{
					break;
				}

				probe = next;
			}

			if (starts.Count == 0)
			{
				return null;
			}

			var lines = new List<(int start, int end)>(starts.Count);
			for (var i = 0; i < starts.Count; i++)
			{
				lines.Add((starts[i], i + 1 < starts.Count ? starts[i + 1] : text.Length));
			}

			return lines;
		}

		private static int GetUnitIndex(int index, int unitCount)
		{
			ValidateUnitIndex(index, unitCount);
			return index > 0 ? index - 1 : unitCount + index;
		}

		private static void ValidateUnitIndex(int index, int unitCount)
		{
			var zeroBasedIndex = index > 0
				? (long)index - 1
				: index < 0
					? (long)unitCount + index
					: -1;
			if (zeroBasedIndex < 0 || zeroBasedIndex >= unitCount)
			{
				throw new ArgumentException("The index does not identify a unit in this story.", nameof(index));
			}
		}

		public void GetCharacterUtf32(out uint value, int offset)
		{
			var text = _document.GetTextInRange(0, _document.TextLength);
			var position = (long)_end + offset;
			if (position < 0 || position >= text.Length)
			{
				// Out of range yields 0 (WinUI reports the null character past the story end).
				value = 0;
				return;
			}

			var index = (int)position;
			if (char.IsLowSurrogate(text[index]) && index > 0 && char.IsHighSurrogate(text[index - 1]))
			{
				index--;
			}

			// Combine a surrogate pair into a single UTF-32 code point; otherwise the char is the value.
			value = char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])
				? (uint)char.ConvertToUtf32(text[index], text[index + 1])
				: text[index];
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
