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
	// Inline images participate in layout and rendering. Remaining rich object formats require further
	// shared-layout support.
	internal class UnoTextRange : global::Microsoft.UI.Text.ITextRange
	{
		private protected readonly RichEditTextDocument _document;
		private int _startValue;
		private int _endValue;
		private long _rangeEditGeneration;
		private TrackedTextRangeRegistration? _trackingRegistration;
		private bool _isApplyingRangeEdits;
		private global::Microsoft.UI.Text.RangeGravity _gravity = global::Microsoft.UI.Text.RangeGravity.UIBehavior;

		private protected int _start
		{
			get
			{
				EnsureCurrent();
				return _startValue;
			}
			set
			{
				EnsureCurrent();
				_startValue = value;
			}
		}

		private protected int _end
		{
			get
			{
				EnsureCurrent();
				return _endValue;
			}
			set
			{
				EnsureCurrent();
				_endValue = value;
			}
		}

		internal UnoTextRange(RichEditTextDocument document, int startPosition, int endPosition)
		{
			_document = document;
			_rangeEditGeneration = document.RangeEditGeneration;
			_startValue = startPosition;
			_endValue = endPosition;
			Normalize();
			_document.TrackRange(this);
		}

		internal long RangeEditGeneration => _rangeEditGeneration;

		internal TrackedTextRangeRegistration? TrackingRegistration => _trackingRegistration;

		internal void SetTrackingRegistration(TrackedTextRangeRegistration registration)
			=> _trackingRegistration = registration;

		internal void SetRangeEditGeneration(long generation) => _rangeEditGeneration = generation;

		private void EnsureCurrent()
		{
			if (!_isApplyingRangeEdits)
			{
				_document.EnsureRangeCurrent(this);
			}
		}

		internal void RebaseAfterEdit(int editStart, int editEnd, int insertLength, int documentLength, long generation)
		{
			_isApplyingRangeEdits = true;
			try
			{
				var oldDocumentLength = documentLength - insertLength + (editEnd - editStart);
				var oldStart = _startValue;
				var oldEnd = _endValue;
				var includedFinalEop = _endValue == oldDocumentLength + 1;
				var fullyCovered = !includedFinalEop
					&& (oldStart == oldEnd
						? editStart <= oldStart && oldStart < editEnd
						: editStart < oldStart && oldEnd < editEnd);
				if (fullyCovered)
				{
					_startValue = _endValue = editStart;
				}
				else
				{
					_startValue = includedFinalEop && oldStart == oldDocumentLength && editStart == oldDocumentLength
						? oldDocumentLength
						: RebasePosition(oldStart, editStart, editEnd, insertLength, isStartEndpoint: true);
					_endValue = includedFinalEop
						? documentLength + 1
						: RebasePosition(oldEnd, editStart, editEnd, insertLength, isStartEndpoint: false);
				}
				_startValue = Math.Clamp(_startValue, 0, documentLength);
				_endValue = Math.Clamp(_endValue, 0, documentLength + 1);
				if (_startValue > _endValue)
				{
					(_startValue, _endValue) = (_endValue, _startValue);
				}

				_rangeEditGeneration = generation;
				if (_startValue != oldStart || _endValue != oldEnd)
				{
					OnRebasedAfterEdit();
				}
			}
			finally
			{
				_isApplyingRangeEdits = false;
			}
		}

		private static int RebasePosition(int position, int editStart, int editEnd, int insertLength, bool isStartEndpoint)
		{
			var removeLength = editEnd - editStart;
			if (removeLength == 0)
			{
				if (position < editStart)
				{
					return position;
				}

				if (position > editStart)
				{
					return position + insertLength;
				}

				return editStart;
			}

			if (position <= editStart)
			{
				return position;
			}

			if (position >= editEnd)
			{
				return position + insertLength - removeLength;
			}

			return isStartEndpoint ? editStart + insertLength : editStart;
		}

		internal bool UsesForwardCharacterFormatting
			=> _gravity is global::Microsoft.UI.Text.RangeGravity.Forward or global::Microsoft.UI.Text.RangeGravity.Inward;

		private protected void Normalize()
		{
			if (_start > _end)
			{
				(_start, _end) = (_end, _start);
			}

			var length = _document.TextLength;
			if (_start == _end)
			{
				_start = _end = Math.Clamp(_start, 0, length);
				return;
			}

			_start = Math.Clamp(_start, 0, length);
			_end = Math.Clamp(_end, 0, length + 1);
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

		private void FinalizeSelectionHistoryIfNeeded()
		{
			if (this is UnoTextSelection)
			{
				_document.FinalizeHistorySelection();
			}
		}

		private protected virtual void OnRebasedAfterEdit()
		{
		}

		public int StartPosition
		{
			get => _start;
			set
			{
				if (value > _document.TextLength)
				{
					_start = _end = _document.TextLength;
					OnRangeChanged();
					return;
				}

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
				_end = Math.Clamp(value, 0, _document.StoryLength);
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

		public int StoryLength => _document.StoryLength;

		public string Text
		{
			get => _document.GetTextInRange(_start, _end);
			set
			{
				var replacement = value ?? string.Empty;
				var insertedLength = _document.ReplaceRange(
					_start,
					_end,
					replacement,
					this,
					forceHistory: true);
				_end = _start + insertedLength;
				OnRangeChanged();
				FinalizeSelectionHistoryIfNeeded();
			}
		}

		public char Character
		{
			get
			{
				var length = _document.TextLength;
				if (_start < length)
				{
					return _document.GetCharacterAt(_start);
				}

				// The end-of-story is conventionally represented by a carriage return in the TOM.
				return '\r';
			}
			set
			{
				var length = _document.TextLength;
				if (_start < length)
				{
					_document.ReplaceRange(
						_start,
						_start + 1,
						value.ToString(),
						this,
						forceHistory: true);
				}
				else
				{
					var includedFinalEop = _end > length;
					_document.ReplaceRange(
						_start,
						_start,
						value.ToString(),
						this,
						forceHistory: true);
					if (includedFinalEop)
					{
						_end = _document.StoryLength;
					}
				}

				OnRangeChanged();
				FinalizeSelectionHistoryIfNeeded();
			}
		}

		public virtual void SetRange(int startPosition, int endPosition)
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

			Normalize();
			OnRangeChanged();
		}

		public void GetText(global::Microsoft.UI.Text.TextGetOptions options, out string value)
		{
			value = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf)
				? RichTextRtfCodec.Write(_document.CaptureFragment(_start, _end, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)))
				: _document.GetTextInRange(_start, _end, options);
		}

		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			var insertedLength = options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf)
				? _document.ReplaceRangeWithFragment(
					_start,
					_end,
					string.IsNullOrEmpty(value)
						? RichTextFragment.Empty()
						: RichTextRtfCodec.Read(
							value,
							_document.GetSetTextImportCharacterLimit(_start, _end, options),
							_document.ShouldTruncateSetTextImportAtLimit(_start, _end, options)),
					this,
					unhide: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unhide),
					unlink: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unlink),
					forceHistory: true,
					checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit))
				: _document.ReplaceRange(
					_start,
					_end,
					value ?? string.Empty,
					this,
					options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unlink),
					options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unhide),
					forceHistory: true,
					checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit),
					unicodeBidi: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.UnicodeBidi));
			_end = _start + insertedLength;
			OnRangeChanged();
			FinalizeSelectionHistoryIfNeeded();
		}

		public global::Microsoft.UI.Text.ITextRange GetClone()
		{
			var clone = new UnoTextRange(_document, _start, _end)
			{
				_gravity = _gravity,
			};
			return clone;
		}

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

			var textLength = _document.TextLength;
			var comparison = options.HasFlag(global::Microsoft.UI.Text.FindOptions.Case)
				? StringComparison.Ordinal
				: StringComparison.OrdinalIgnoreCase;
			var matchWholeWord = options.HasFlag(global::Microsoft.UI.Text.FindOptions.Word);
			var textElementBoundaries = matchWholeWord ? _document.TextElementBoundaries : (TextElementBoundaryView?)null;
			var originalStart = Math.Clamp(_start, 0, textLength);
			var originalEnd = Math.Clamp(_end, originalStart, textLength);
			var currentRangeMatches = originalEnd - originalStart == value.Length
				&& IsFindMatch(_document, value, originalStart, comparison, textElementBoundaries);

			int searchStart;
			int searchEnd;
			if (scanLength > 0)
			{
				searchStart = currentRangeMatches ? originalStart + 1 : originalStart;
				searchEnd = (int)Math.Min(textLength, (long)originalStart + scanLength);
			}
			else if (scanLength < 0)
			{
				searchStart = (int)Math.Max(0, (long)originalEnd + scanLength);
				searchEnd = currentRangeMatches ? originalEnd - 1 : originalEnd;
			}
			else
			{
				searchStart = originalStart;
				searchEnd = originalEnd;
			}

			var match = scanLength < 0
				? FindBackward(_document, value, searchStart, searchEnd, comparison, textElementBoundaries)
				: FindForward(_document, value, searchStart, searchEnd, comparison, textElementBoundaries);
			if (match < 0)
			{
				return 0;
			}

			_start = match;
			_end = match + value.Length;
			OnRangeChanged();
			return value.Length;
		}

		private static int FindForward(RichEditTextDocument document, string value, int searchStart, int searchEnd, StringComparison comparison, TextElementBoundaryView? textElementBoundaries)
		{
			var lastCandidate = searchEnd - value.Length;
			var candidate = searchStart;
			while (candidate <= lastCandidate)
			{
				var match = document.IndexOfText(value, candidate, searchEnd - candidate, comparison);
				if (match < 0 || match > lastCandidate)
				{
					return -1;
				}

				if (IsFindMatch(document, value, match, comparison, textElementBoundaries))
				{
					return match;
				}

				candidate = match + 1;
			}

			return -1;
		}

		private static int FindBackward(RichEditTextDocument document, string value, int searchStart, int searchEnd, StringComparison comparison, TextElementBoundaryView? textElementBoundaries)
		{
			var lastCandidate = searchEnd - value.Length;
			var candidate = searchStart;
			var lastMatch = -1;
			while (candidate <= lastCandidate)
			{
				var match = document.IndexOfText(value, candidate, searchEnd - candidate, comparison);
				if (match < 0 || match > lastCandidate)
				{
					break;
				}

				if (IsFindMatch(document, value, match, comparison, textElementBoundaries))
				{
					lastMatch = match;
				}

				candidate = match + 1;
			}

			return lastMatch;
		}

		private static bool IsFindMatch(RichEditTextDocument document, string value, int start, StringComparison comparison, TextElementBoundaryView? textElementBoundaries)
		{
			if (start < 0 || start > document.TextLength - value.Length
				|| !document.TextRangeEquals(start, value, comparison))
			{
				return false;
			}

			if (textElementBoundaries is null)
			{
				return true;
			}

			var end = start + value.Length;
			return IsFindWordBoundary(document, start, textElementBoundaries.Value)
				&& IsFindWordBoundary(document, end, textElementBoundaries.Value);
		}

		private static bool IsFindWordBoundary(RichEditTextDocument document, int position, TextElementBoundaryView textElementBoundaries)
		{
			var boundaryIndex = textElementBoundaries.BinarySearch(position);
			if (boundaryIndex < 0)
			{
				return false;
			}

			if (position == 0 || position == document.TextLength)
			{
				return true;
			}

			return GetFindCharacterClass(document, textElementBoundaries[boundaryIndex - 1])
				!= GetFindCharacterClass(document, textElementBoundaries[boundaryIndex]);
		}

		private static int GetFindCharacterClass(RichEditTextDocument document, int start)
		{
			if (!document.TryGetRuneAt(start, out var value))
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
			var descriptor = global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptorForDelete(unit);
			if (_start != _end)
			{
				if (_start == _document.TextLength)
				{
					_end = _start;
					OnRangeChanged();
					return 0;
				}

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
					FinalizeSelectionHistoryIfNeeded();
					var deletedUnits = 1 + Math.Abs(additionalDeleted);
					return count < 0 ? -deletedUnits : deletedUnits;
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

			if (descriptor.Kind == global::Microsoft.UI.Text.TextRangeUnitProviderKind.UnsupportedOperation)
			{
				return 0;
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
					FinalizeSelectionHistoryIfNeeded();
				}

				return count > 0 ? deleted : -deleted;
			}

			// Word / Paragraph / Line: delete |count| units in the logical direction (like CTRL+DELETE /
			// CTRL+BACKSPACE), returning the number of units removed (TOM ITextRange::Delete pDelta).
			var boundaries = GetUnitBoundarySet(unit);
			if (boundaries is null)
			{
				return 0;
			}

			var target = boundaries.Move(_start, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return 0;
			}
			target = Math.Clamp(target, 0, length);

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
				FinalizeSelectionHistoryIfNeeded();
			}

			return unitsMoved;
		}

		public void ChangeCase(global::Microsoft.UI.Text.LetterCase value)
		{
			var text = _document.GetTextInRange(_start, _end);
			if (text.Length == 0)
			{
				return;
			}

			var changed = _document.ChangeCaseText(text, value);
			var start = _start;
			var insertedLength = _document.ReplaceRange(start, _end, changed, this);
			_start = start;
			_end = start + insertedLength;
			OnRangeChanged();
			FinalizeSelectionHistoryIfNeeded();
		}

		public int Move(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var descriptor = global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptor(unit);
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
					if (_start == length && _end == length + 1)
					{
						_end = _start;
						OnRangeChanged();
						return -1;
					}

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
				var caretPosition = _start;
				var caretTarget = (int)Math.Clamp((long)caretPosition + count, 0, length);
				var moved = caretTarget - caretPosition;
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

			if (unit is global::Microsoft.UI.Text.TextRangeUnit.Screen or global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				return MoveLayoutUnit(unit, count);
			}

			var direction = Math.Sign(count);
			var position = direction > 0 ? _end : _start;
			var boundaries = GetUnitBoundarySet(unit);
			var unitsMoved = 0;
			var collapsedRange = _start != _end;
			if (collapsedRange)
			{
				_start = _end = Math.Min(position, length);

				if (descriptor.IsSparse && direction < 0 && boundaries?.TryGetSpanEndingAt(position, out var precedingSpan) == true)
				{
					_start = _end = Math.Min(precedingSpan.Start, length);
					OnRangeChanged();
					return direction;
				}
				if (descriptor.IsSparse && direction > 0 && boundaries?.TryGetSpanContainingForward(position, out var followingSpan) == true)
				{
					_start = _end = Math.Min(followingSpan.End, length);
					OnRangeChanged();
					return direction;
				}

				var collapseConsumesUnit = descriptor.MoveCollapseConsumesUnit
					|| boundaries?.HasStartAt(position) == true;
				if (collapseConsumesUnit)
				{
					unitsMoved = direction;
					if (Math.Abs((long)count) == 1)
					{
						OnRangeChanged();
						return unitsMoved;
					}
				}
			}

			if (boundaries is not null)
			{
				var remaining = count - unitsMoved;
				var destination = boundaries.Move(position, remaining, out var moved);
				var destinationPosition = Math.Min(destination, length);
				if (moved != 0 && destinationPosition != position)
				{
					_start = _end = destinationPosition;
					unitsMoved += moved;
				}
			}

			if (unitsMoved == 0 && collapsedRange)
			{
				unitsMoved = direction;
			}

			if (unitsMoved != 0)
			{
				OnRangeChanged();
			}
			return unitsMoved;
		}

		// The ascending unit-boundary positions for <paramref name="unit"/> — every unit start plus the
		// end of the story — used by Delete/MoveStart/MoveEnd. Word/Paragraph are text-based; Line is
		// geometry-based (wrap-aware) and requires a laid-out view. Returns null when the unit is not
		// boundary-navigable here (Character/Story are handled directly) or the layout is unavailable.
		private protected global::Microsoft.UI.Text.TextRangeUnitBoundarySet? GetUnitBoundarySet(
			global::Microsoft.UI.Text.TextRangeUnit unit,
			bool allowUnavailableWindow = false)
		{
			var boundaries = _document.GetUnitBoundaries(unit);
			if (boundaries is null
				&& !allowUnavailableWindow
				&& unit == global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				throw new NotImplementedException();
			}
			return boundaries;
		}

		private protected int[]? GetUnitBoundaries(global::Microsoft.UI.Text.TextRangeUnit unit)
			=> GetUnitBoundarySet(unit)?.GetMovementBoundaries();

		// Moves <paramref name="position"/> by <paramref name="count"/> unit boundaries along
		// <paramref name="boundaries"/> and reports the signed number of units actually crossed. Mirrors
		// the boundary math used by Move so all unit navigation stays consistent.
		private protected static int MoveByBoundaries(int[] boundaries, int position, int count, out int unitsMoved)
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

		private int MoveLayoutUnit(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Window
				&& _document.GetVisibleUnitBoundaries() is null)
			{
				throw new NotImplementedException();
			}

			var direction = Math.Sign(count);
			var position = direction > 0 ? _end : _start;
			var unitsMoved = 0;
			if (_start != _end)
			{
				_start = _end = position;
				unitsMoved = 1;
				if (Math.Abs(count) == 1)
				{
					OnRangeChanged();
					return direction;
				}
			}

			var remaining = Math.Abs(count) - unitsMoved;
			var moved = unit == global::Microsoft.UI.Text.TextRangeUnit.Screen
				? _document.TryGetRangePageTarget(position, direction < 0, remaining, out var target, out var actual)
				: TryGetWindowTarget(position, direction, out target, out actual);
			if (moved)
			{
				_start = _end = target;
				unitsMoved += actual;
			}

			if (unitsMoved != 0)
			{
				OnRangeChanged();
			}

			return direction * unitsMoved;
		}

		private bool TryGetWindowTarget(int position, int direction, out int target, out int unitsMoved)
		{
			target = position;
			unitsMoved = 0;
			if (!_document.TryGetVisibleRange(out var visibleStart, out var visibleEnd))
			{
				return false;
			}

			target = direction > 0 ? visibleEnd : visibleStart;
			if (direction > 0 && target <= position || direction < 0 && target >= position)
			{
				target = position;
				return false;
			}

			unitsMoved = target == position ? 0 : 1;
			return unitsMoved != 0;
		}

		private int MoveLayoutEndpoint(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool moveStart)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Window
				&& _document.GetVisibleUnitBoundaries() is null)
			{
				throw new NotImplementedException();
			}

			var direction = Math.Sign(count);
			var position = moveStart ? _start : _end;
			var moved = unit == global::Microsoft.UI.Text.TextRangeUnit.Screen
				? _document.TryGetRangePageTarget(position, direction < 0, Math.Abs(count), out var target, out var actual)
				: TryGetWindowTarget(position, direction, out target, out actual);
			if (!moved)
			{
				return 0;
			}

			if (moveStart)
			{
				_start = target;
				if (_start > _end)
				{
					_end = _start;
				}
			}
			else
			{
				_end = target;
				if (_end < _start)
				{
					_start = _end;
				}
			}

			OnRangeChanged();
			return direction * actual;
		}


		public int MoveStart(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			var descriptor = global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptor(unit);
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

			if (unit is global::Microsoft.UI.Text.TextRangeUnit.Screen or global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				return MoveLayoutEndpoint(unit, count, moveStart: true);
			}

			var boundaries = GetUnitBoundarySet(unit);
			if (boundaries is null)
			{
				return 0;
			}

			if (descriptor.IsEffectUnit && count > 0 && boundaries.HasStartAt(_start))
			{
				return 1;
			}

			var target = boundaries.Move(_start, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return descriptor.IsEffectUnit && count > 0 ? 1 : 0;
			}

			var oldStart = _start;
			_start = Math.Clamp(target, 0, length);
			if (_start == oldStart)
			{
				return 0;
			}
			if (_start > _end)
			{
				_end = _start;
			}

			OnRangeChanged();
			return unitsMoved;
		}

		public int MoveEnd(global::Microsoft.UI.Text.TextRangeUnit unit, int count)
		{
			global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptor(unit);
			var length = _document.TextLength;

			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				var oldChar = _end;
				_end = (int)Math.Clamp((long)_end + count, 0, _document.StoryLength);
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

			if (unit is global::Microsoft.UI.Text.TextRangeUnit.Screen or global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				return MoveLayoutEndpoint(unit, count, moveStart: false);
			}

			var boundaries = GetUnitBoundarySet(unit);
			if (boundaries is null)
			{
				return 0;
			}

			var target = boundaries.Move(_end, count, out var unitsMoved);
			if (unitsMoved == 0)
			{
				return 0;
			}

			var oldEnd = _end;
			_end = Math.Clamp(target, 0, _document.StoryLength);
			if (_end == oldEnd)
			{
				return 0;
			}
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
				var format = _document.GetFormatOverRange(_start, _end, _gravity);
				format.Bind(this);
				return format;
			}
			set
			{
				if (value is UnoTextCharacterFormat format)
				{
					_document.SetFormatOverRange(_start, _end, format, _gravity);
				}
			}
		}

		// Applies a bound character format to this range's current extent. Called by
		// UnoTextCharacterFormat's property setters so `range.CharacterFormat.Bold = On` takes effect.
		internal void ApplyCharacterFormat(UnoTextCharacterFormat format)
			=> _document.SetFormatOverRange(_start, _end, format, _gravity);

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

		// --- Rich transfer, gravity, clipboard, geometry, streams, and embedded images ---

		public global::Microsoft.UI.Text.ITextRange FormattedText
		{
			get => GetClone();
			set
			{
				if (value is null)
				{
					throw new ArgumentNullException(nameof(value));
				}

				if (value is UnoTextRange source)
				{
					var fragment = source._document.CaptureFragment(source._start, source._end);
					var insertedLength = _document.ReplaceRangeWithFragment(
						_start,
						_end,
						fragment,
						this,
						forceHistory: true);
					_end = _start + insertedLength;
				}
				else
				{
					var insertedLength = _document.ReplaceRange(
						_start,
						_end,
						value.Text ?? string.Empty,
						this,
						forceHistory: true);
					_end = _start + insertedLength;
				}

				OnRangeChanged();
				FinalizeSelectionHistoryIfNeeded();
			}
		}

		public global::Microsoft.UI.Text.RangeGravity Gravity
		{
			get => _gravity;
			set
			{
				_gravity = value switch
				{
					global::Microsoft.UI.Text.RangeGravity.UIBehavior => value,
					global::Microsoft.UI.Text.RangeGravity.Backward => value,
					global::Microsoft.UI.Text.RangeGravity.Forward => value,
					global::Microsoft.UI.Text.RangeGravity.Inward => value,
					global::Microsoft.UI.Text.RangeGravity.Outward => global::Microsoft.UI.Text.RangeGravity.UIBehavior,
					_ => throw new ArgumentException("The range gravity is invalid.", nameof(value)),
				};
			}
		}

		public string Link
		{
			get
			{
				var link = _document.GetLink(_start, _end, out var linkStart, out var linkEnd);
				if (link.Length > 0)
				{
					_start = linkStart;
					_end = linkEnd;
					OnRangeChanged();
				}

				return link;
			}
			set => _document.SetLink(_start, _end, value);
		}

		public bool CanPaste(int format) => _document.CanPaste(format);

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

			if (_document.IsRangeProtected(_start, _end))
			{
				throw new UnauthorizedAccessException("The text range contains protected text.");
			}

			_document.CopyToClipboard(_start, _end);
			_document.ReplaceRange(_start, _end, string.Empty, this);
			_end = _start;
			OnRangeChanged();
		}

		public virtual void Paste(int format)
		{
			if (_document.IsRangeProtected(_start, _end, UsesForwardCharacterFormatting))
			{
				throw new UnauthorizedAccessException("The text range contains protected text.");
			}

			// The OS clipboard read is async on Uno, so unlike WinUI's synchronous paste this replaces the
			// range on a later dispatcher turn. The operation range remains live while the requested native
			// clipboard format is retrieved.
			var operationRange = new UnoTextRange(_document, _start, _end)
			{
				Gravity = _gravity,
			};
			_document.BeginPasteFromClipboard(operationRange, caret =>
			{
				_start = _end = caret;
				OnRangeChanged();
			}, requireEditable: this is UnoTextSelection, format);
		}

		// --- Text-based unit navigation (Word/Paragraph/Story) — functional over the plain-text buffer ---

		public int EndOf(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			var boundaries = _document.GetUnitBoundaries(unit);
			if (boundaries is null || boundaries.Count == 0)
			{
				return 0;
			}

			var probe = _end > _start ? _end - 1 : _end;
			var index = boundaries.FindContaining(probe);
			if (index < 0)
			{
				return 0;
			}
			var target = boundaries[index].OperationEnd;

			if (!extend && target > _document.TextLength)
			{
				target = _document.TextLength;
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
			var boundaries = _document.GetUnitBoundaries(unit);
			if (boundaries is null || boundaries.Count == 0)
			{
				return 0;
			}
			var index = boundaries.FindContaining(_start);
			if (index < 0)
			{
				return 0;
			}
			var target = boundaries[index].Start;

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
			var boundaries = GetUnitBoundarySet(unit, allowUnavailableWindow: true);
			if (boundaries is null || boundaries.Count == 0)
			{
				return 0;
			}

			var startIndex = boundaries.FindContaining(_start);
			if (startIndex < 0)
			{
				return 0;
			}
			var probeEnd = _end > _start ? _end - 1 : _end;
			var endIndex = boundaries.FindContaining(probeEnd);
			if (endIndex < 0)
			{
				endIndex = startIndex;
			}

			var oldStart = _start;
			var oldEnd = _end;
			var originalLength = _end - _start;
			_start = boundaries[startIndex].Start;
			_end = boundaries[endIndex].OperationEnd;
			OnRangeChanged();
			return oldEnd == _end && oldStart != _start
				? _start - oldStart
				: (_end - _start) - originalLength;
		}

		public int GetIndex(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			var descriptor = global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptor(unit);
			var units = GetUnitBoundarySet(unit);
			if (descriptor.Kind == global::Microsoft.UI.Text.TextRangeUnitProviderKind.ContentLink)
			{
				return 1;
			}
			if (descriptor.IsEffectUnit)
			{
				return units?.GetLeadingCompletedIndex(_start) ?? 0;
			}
			if (units is null || units.Count == 0)
			{
				return 0;
			}
			if (descriptor.Kind == global::Microsoft.UI.Text.TextRangeUnitProviderKind.Object)
			{
				return units.CountEndingAtOrBefore(_start);
			}
			var index = units.FindContaining(_start);
			return index < 0 ? 0 : index + 1;
		}

		public void SetIndex(global::Microsoft.UI.Text.TextRangeUnit unit, int index, bool extend)
		{
			var descriptor = global::Microsoft.UI.Text.TextRangeUnitBoundaryProvider.GetDescriptor(unit);
			var units = GetUnitBoundarySet(unit);
			if (descriptor.IsEffectUnit || descriptor.Kind == global::Microsoft.UI.Text.TextRangeUnitProviderKind.ContentLink)
			{
				SetSingleUnitIndex(index, extend);
				return;
			}
			if (units is null || units.Count == 0)
			{
				if (descriptor.Kind == global::Microsoft.UI.Text.TextRangeUnitProviderKind.Object)
				{
					SetSingleUnitIndex(index, extend);
				}
				return;
			}
			var indexedUnit = units[GetUnitIndex(index, units.Count)];

			if (extend)
			{
				_end = indexedUnit.End;
				if (_end < _start)
				{
					_start = _end;
				}
			}
			else
			{
				_start = _end = indexedUnit.Start;
			}

			OnRangeChanged();
		}

		private void SetSingleUnitIndex(int index, bool extend)
		{
			ValidateUnitIndex(index, 1);
			var position = index > 0 ? 0 : _document.TextLength;
			if (extend)
			{
				_end = position;
				if (_end < _start)
				{
					_start = _end;
				}
			}
			else
			{
				_start = _end = position;
			}

			OnRangeChanged();
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
			var position = (long)_end + offset;
			if (position < 0 || position >= _document.TextLength)
			{
				// Out of range yields 0 (WinUI reports the null character past the story end).
				value = 0;
				return;
			}

			var index = (int)position;
			if (char.IsLowSurrogate(_document.GetCharacterAt(index))
				&& index > 0
				&& char.IsHighSurrogate(_document.GetCharacterAt(index - 1)))
			{
				if (offset > 0)
				{
					index++;
					if (index >= _document.TextLength)
					{
						value = 0;
						return;
					}
				}
				else
				{
					index--;
				}
			}

			// Combine a surrogate pair into a single UTF-32 code point; otherwise the char is the value.
			var character = _document.GetCharacterAt(index);
			value = char.IsHighSurrogate(character)
				&& index + 1 < _document.TextLength
				&& char.IsLowSurrogate(_document.GetCharacterAt(index + 1))
					? (uint)char.ConvertToUtf32(character, _document.GetCharacterAt(index + 1))
					: character;
		}

		public void GetPoint(global::Microsoft.UI.Text.HorizontalCharacterAlignment horizontalAlign, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Point point)
		{
			// The point is taken at the range's start when PointOptions.Start is set, otherwise at its
			// (active) end, mirroring WinUI's tomStart/tomEnd anchoring.
			point = default;
			_document.TryScrollRangeIntoView(_start, _end, options);
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
				global::Microsoft.UI.Text.VerticalCharacterAlignment.Baseline when _document.TryGetIndexBaseline(anchor, options, out var baseline) => baseline,
				_ => rect.Y + rect.Height,
			};
			point = new global::Windows.Foundation.Point(x, y);
		}

		public void GetRect(global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect, out int hit)
		{
			_document.TryScrollRangeIntoView(_start, _end, options);
			_document.TryGetRangeGeometry(
				_start,
				_end,
				options,
				this is UnoTextSelection,
				out var result);
			rect = result.Rect;
			hit = result.NativeHit;
		}

		public void ScrollIntoView(global::Microsoft.UI.Text.PointOptions value)
			=> _document.TryScrollRangeIntoView(_start, _end, value);

		public void SetPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, bool extend)
		{
			const global::Microsoft.UI.Text.PointOptions invalidOptions =
				global::Microsoft.UI.Text.PointOptions.AllowOffClient
				| global::Microsoft.UI.Text.PointOptions.NoHorizontalScroll
				| global::Microsoft.UI.Text.PointOptions.NoVerticalScroll;
			if ((options & invalidOptions) != 0)
			{
				throw new ArgumentException(nameof(options));
			}

			if (!_document.TryGetIndexFromPoint(point, options, out var index))
			{
				return;
			}

			if (extend)
			{
				if (options.HasFlag(global::Microsoft.UI.Text.PointOptions.Start))
				{
					_start = index;
					if (_start > _end)
					{
						_end = _start;
					}
				}
				else
				{
					_start = _end = index;
				}

				OnRangeChanged();
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

		public void GetTextViaStream(global::Microsoft.UI.Text.TextGetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value)
			=> _document.GetRangeTextViaStream(_start, _end, options, value);

		public void SetTextViaStream(global::Microsoft.UI.Text.TextSetOptions options, global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			if (!options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf))
			{
				var plainInsertedLength = _document.ReplaceRange(
					_start,
					_end,
					_document.ReadRangeTextViaStream(value),
					this,
					options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unlink),
					options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unhide),
					forceHistory: true,
					checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit),
					unicodeBidi: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.UnicodeBidi));
				_start = _end = _start + plainInsertedLength;
				OnRangeChanged();
				FinalizeSelectionHistoryIfNeeded();
				return;
			}

			var fragment = _document.ReadRangeRtfViaStream(
				value,
				_document.GetSetTextImportCharacterLimit(_start, _end, options),
				_document.ShouldTruncateSetTextImportAtLimit(_start, _end, options));
			var insertedLength = _document.ReplaceRangeWithFragment(
				_start,
				_end,
				fragment,
				this,
				unhide: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unhide),
				unlink: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.Unlink),
				forceHistory: true,
				checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit));
			_start = _end = _start + insertedLength;
			OnRangeChanged();
			FinalizeSelectionHistoryIfNeeded();
		}

		public void InsertImage(int width, int height, int ascent, global::Microsoft.UI.Text.VerticalCharacterAlignment verticalAlign, string alternateText, global::Windows.Storage.Streams.IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			if (width < 0 || height < 0 || ascent < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions and ascent cannot be negative.");
			}

			var image = InlineImageState.CreateFromStream(value, width, height, ascent, verticalAlign, alternateText);
			var fragment = _document.CreateInlineImageFragment(_start, image);
			var insertedLength = _document.ReplaceRangeWithFragment(_start, _end, fragment, this);
			_end = _start + insertedLength;
			OnRangeChanged();
			FinalizeSelectionHistoryIfNeeded();
		}
	}
}
