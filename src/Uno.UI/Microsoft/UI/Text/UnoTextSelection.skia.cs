#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific functional Text Object Model selection over the RichEditBox plain-text buffer.
	//
	// Inherits the functional plain-text range behavior from UnoTextRange and adds the selection-only
	// surface (TypeText, Options, Type, horizontal Move*/HomeKey/EndKey plus layout-aware vertical
	// MoveUp/MoveDown and per-line Home/End). This is a programmatic selection: it edits and navigates
	// the buffer and drives rendering, and mirrors its changes onto the interactive caret.
	internal sealed class UnoTextSelection : UnoTextRange, global::Microsoft.UI.Text.ITextSelection
	{
		private global::Microsoft.UI.Text.SelectionOptions _options;

		internal UnoTextSelection(RichEditTextDocument document)
			: base(document, 0, 0)
		{
		}

		public global::Microsoft.UI.Text.SelectionOptions Options
		{
			get => _options;
			set
			{
				var startActiveChanged = _start != _end
					&& _options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive)
					!= value.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);
				_options = value;
				if (startActiveChanged)
				{
					_document.NotifySelectionChanged();
				}
			}
		}

		public global::Microsoft.UI.Text.SelectionType Type
			=> _start == _end
				? global::Microsoft.UI.Text.SelectionType.InsertionPoint
				: global::Microsoft.UI.Text.SelectionType.Normal;

		public void TypeText(string value)
		{
			var text = _document.CoerceTypedText(value ?? string.Empty);
			var replaceEnd = _end;
			if (_start == _end && _options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.Overtype))
			{
				replaceEnd = Math.Min(_document.TextLength, _start + text.Length);
			}

			var insertedLength = _document.ReplaceRange(_start, replaceEnd, text, this);
			_start = _end = _start + insertedLength;
			OnRangeChanged();
		}

		// Programmatic Selection.Copy/Cut/Paste route through the owning control so the RichEditBox
		// CopyingToClipboard / CuttingToClipboard / Paste events are raised — matching WinUI, where these
		// fire for the selection but NOT for a plain (non-selection) range, which keeps the silent base
		// behavior inherited from UnoTextRange.
		public override void Copy() => _document.CopySelectionToClipboardViaControl(this);

		public override void Cut() => _document.CutSelectionToClipboardViaControl(this);

		public override void Paste(int format)
		{
			if (_document.TryBeginSelectionPasteViaControl())
			{
				base.Paste(format);
			}
		}

		internal void SetRangeAfterTextMutation(int start, int end)
		{
			_start = start;
			_end = end;
			Normalize();
			OnRangeChanged();
		}

		internal void SetRangeInternal(int start, int end, bool selectionEndsAtTheStart)
		{
			base.SetRangeInternal(start, end);
			SetStartActive(selectionEndsAtTheStart);
		}

		public int MoveLeft(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
		{
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				return 0;
			}

			if (extend)
			{
				var old = IsStartActive ? _start : _end;
				SetActivePosition(old - count, extend: true);
				OnRangeChanged();
				return Math.Abs((IsStartActive ? _start : _end) - old);
			}

			// Non-extending left move collapses to the active (left) end. Per the TOM MoveLeft contract,
			// collapsing a nondegenerate selection to its left edge already counts as the first unit, so
			// only Count-1 further units are moved; a degenerate caret moves the full Count.
			if (_start != _end && count > 0)
			{
				var originalStart = _start;
				var originalEnd = _end;
				var originalActive = IsStartActive ? originalStart : originalEnd;
				var edge = _start;
				var target = Math.Clamp(edge - (count - 1), 0, _document.TextLength);
				_start = _end = target;
				OnRangeChanged();
				if (_start == _end)
				{
					return _start <= edge && (_start != originalStart || _end != originalEnd)
						? (edge - _start) + 1
						: 0;
				}

				return Math.Abs((IsStartActive ? _start : _end) - originalActive);
			}

			_end = _start;
			var previous = _start;
			_start = _end = Math.Clamp(_start - count, 0, _document.TextLength);
			OnRangeChanged();
			return Math.Abs(_start - previous);
		}

		public int MoveRight(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
		{
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				return 0;
			}

			if (extend)
			{
				var old = IsStartActive ? _start : _end;
				SetActivePosition(old + count, extend: true);
				OnRangeChanged();
				return Math.Abs((IsStartActive ? _start : _end) - old);
			}

			// Non-extending right move collapses to the active (right) end. Per the TOM MoveRight contract,
			// collapsing a nondegenerate selection to its right edge already counts as the first unit, so
			// only Count-1 further units are moved; a degenerate caret moves the full Count.
			if (_start != _end && count > 0)
			{
				var originalStart = _start;
				var originalEnd = _end;
				var originalActive = IsStartActive ? originalStart : originalEnd;
				var edge = _end;
				var target = Math.Clamp(edge + (count - 1), 0, _document.TextLength);
				_start = _end = target;
				OnRangeChanged();
				if (_start == _end)
				{
					return _end >= edge && (_start != originalStart || _end != originalEnd)
						? (_end - edge) + 1
						: 0;
				}

				return Math.Abs((IsStartActive ? _start : _end) - originalActive);
			}

			_start = _end;
			var previous = _end;
			_start = _end = Math.Clamp(_end + count, 0, _document.TextLength);
			OnRangeChanged();
			return Math.Abs(_end - previous);
		}

		public int HomeKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				var current = IsStartActive ? _start : _end;
				if (!_document.TryGetLineBounds(current, out var lineStart, out _, out _, out _))
				{
					return 0;
				}

				SetActivePosition(lineStart, extend);
				OnRangeChanged();
				return Math.Abs((IsStartActive ? _start : _end) - current);
			}

			// Only the Story unit (document start) is otherwise supported.
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				return 0;
			}

			var old = IsStartActive ? _start : _end;
			SetActivePosition(0, extend);
			OnRangeChanged();
			return Math.Abs((IsStartActive ? _start : _end) - old);
		}

		public int EndKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				var current = IsStartActive ? _start : _end;
				if (!_document.TryGetLineBounds(current, out _, out var lineEnd, out _, out _))
				{
					return 0;
				}

				SetActivePosition(lineEnd, extend);
				OnRangeChanged();
				return Math.Abs((IsStartActive ? _start : _end) - current);
			}

			// Only the Story unit (document end) is otherwise supported.
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				return 0;
			}

			var length = _document.TextLength;
			var old = IsStartActive ? _start : _end;
			SetActivePosition(length, extend);
			OnRangeChanged();
			return Math.Abs((IsStartActive ? _start : _end) - old);
		}

		public int MoveUp(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(unit, up: true, count, extend);

		public int MoveDown(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(unit, up: false, count, extend);

		// Vertical caret movement over the shared layout, preserving the sticky horizontal offset.
		private int MoveVertical(global::Microsoft.UI.Text.TextRangeUnit unit, bool up, int count, bool extend)
		{
			if (count <= 0)
			{
				return 0;
			}

			var unitsMoved = 0;
			var current = IsStartActive ? _start : _end;
			if (!extend && _start != _end)
			{
				current = up ? _start : _end;
				SetActivePosition(current, extend: false);
				unitsMoved = 1;
				count--;
				if (count == 0)
				{
					OnRangeChanged();
					return unitsMoved;
				}
			}

			var target = current;
			var moved = false;
			var movedAfterCollapse = 0;
			switch (unit)
			{
				case global::Microsoft.UI.Text.TextRangeUnit.Screen:
					moved = _document.TryGetPageTarget(current, up, count, out target, out movedAfterCollapse);
					break;
				case global::Microsoft.UI.Text.TextRangeUnit.Window:
					moved = _document.TryGetVisibleRange(out var windowStart, out var windowEnd);
					target = up ? windowStart : windowEnd;
					moved &= up ? target < current : target > current;
					movedAfterCollapse = moved ? 1 : 0;
					break;
				case global::Microsoft.UI.Text.TextRangeUnit.Paragraph:
					moved = TryGetParagraphTarget(current, up, count, out target, out movedAfterCollapse);
					break;
				case global::Microsoft.UI.Text.TextRangeUnit.Line:
					moved = _document.TryGetVerticalTarget(current, up, count, out target, out movedAfterCollapse);
					break;
				default:
					return 0;
			}
			if (!moved || target == current)
			{
				if (unitsMoved != 0)
				{
					OnRangeChanged();
				}

				return unitsMoved;
			}

			SetActivePosition(target, extend);
			OnRangeChanged();
			return unitsMoved + movedAfterCollapse;
		}

		private bool TryGetParagraphTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			target = position;
			unitsMoved = 0;
			var chunks = global::Microsoft.UI.Text.TextUnitNavigation.GetParagraphChunks(GetStoryText());
			if (chunks.Count == 0)
			{
				return false;
			}

			var boundaries = new int[chunks.Count + 1];
			for (var i = 0; i < chunks.Count; i++)
			{
				boundaries[i] = chunks[i].start;
			}

			boundaries[chunks.Count] = _document.TextLength;
			target = MoveByBoundaries(boundaries, position, up ? -count : count, out var signedUnitsMoved);
			unitsMoved = Math.Abs(signedUnitsMoved);
			return unitsMoved != 0;
		}

		private bool IsStartActive
			=> _start != _end && _options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);

		private void SetActivePosition(int position, bool extend)
		{
			position = Math.Clamp(position, 0, _document.TextLength);
			if (!extend)
			{
				_start = _end = position;
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.StartActive;
				return;
			}

			var anchor = IsStartActive ? _end : _start;
			if (position < anchor)
			{
				_start = position;
				_end = anchor;
				_options |= global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}
			else
			{
				_start = anchor;
				_end = position;
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}
		}

		private void SetStartActive(bool value)
		{
			if (value && _start != _end)
			{
				_options |= global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}
			else
			{
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}
		}

		// Sync the owning control's interactive caret/selection when this programmatic selection changes.
		private protected override void OnRangeChanged()
		{
			if (_start == _end)
			{
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}

			_document.NotifySelectionChanged();
		}
	}
}
