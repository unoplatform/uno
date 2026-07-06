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
			set => _options = value;
		}

		public global::Microsoft.UI.Text.SelectionType Type
			=> _start == _end
				? global::Microsoft.UI.Text.SelectionType.InsertionPoint
				: global::Microsoft.UI.Text.SelectionType.Normal;

		public void TypeText(string value)
		{
			var text = value ?? string.Empty;
			_document.ReplaceRange(_start, _end, text);
			_start = _end = _start + text.Length;
			OnRangeChanged();
		}

		public int MoveLeft(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
		{
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Character)
			{
				return 0;
			}

			if (extend)
			{
				var old = _start;
				_start = Math.Clamp(_start - count, 0, _document.TextLength);
				OnRangeChanged();
				return Math.Abs(_start - old);
			}

			// Non-extending left move collapses to the start then moves the caret left.
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
				var old = _end;
				_end = Math.Clamp(_end + count, 0, _document.TextLength);
				OnRangeChanged();
				return Math.Abs(_end - old);
			}

			// Non-extending right move collapses to the end then moves the caret right.
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
				// The caret (range end) determines the current visual line.
				if (!_document.TryGetLineBounds(_end, out var lineStart, out _, out _, out _))
				{
					return 0;
				}

				var oldStart = _start;
				_start = lineStart;
				if (!extend)
				{
					_end = lineStart;
				}
				else if (_end < _start)
				{
					(_start, _end) = (_end, _start);
				}

				OnRangeChanged();
				return Math.Abs(oldStart - _start);
			}

			// Only the Story unit (document start) is otherwise supported.
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				return 0;
			}

			var old = _start;
			_start = 0;
			if (!extend)
			{
				_end = 0;
			}

			OnRangeChanged();
			return Math.Abs(old - _start);
		}

		public int EndKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				if (!_document.TryGetLineBounds(_end, out _, out var lineEnd, out _, out _))
				{
					return 0;
				}

				var oldEnd = _end;
				_end = lineEnd;
				if (!extend)
				{
					_start = lineEnd;
				}
				else if (_start > _end)
				{
					(_start, _end) = (_end, _start);
				}

				OnRangeChanged();
				return Math.Abs(_end - oldEnd);
			}

			// Only the Story unit (document end) is otherwise supported.
			if (unit != global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				return 0;
			}

			var length = _document.TextLength;
			var old = _end;
			_end = length;
			if (!extend)
			{
				_start = length;
			}

			OnRangeChanged();
			return Math.Abs(length - old);
		}

		public int MoveUp(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(up: true, count, extend);

		public int MoveDown(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(up: false, count, extend);

		// Vertical caret movement over the shared layout, preserving the sticky horizontal offset.
		// TODO Uno: honor Window/Screen page units and report the exact number of lines moved.
		private int MoveVertical(bool up, int count, bool extend)
		{
			if (count <= 0)
			{
				return 0;
			}

			// The caret sits at the range end; move it up/down from there.
			if (!_document.TryGetVerticalTarget(_end, up, count, out var target) || target == _end)
			{
				return 0;
			}

			if (extend)
			{
				_end = target;
				if (_end < _start)
				{
					(_start, _end) = (_end, _start);
				}
			}
			else
			{
				_start = _end = target;
			}

			OnRangeChanged();
			return count;
		}

		// Sync the owning control's interactive caret/selection when this programmatic selection changes.
		private protected override void OnRangeChanged() => _document.NotifySelectionChanged();
	}
}
