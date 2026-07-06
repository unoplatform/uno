#nullable enable

using System;

namespace Microsoft.UI.Text
{
	// Uno-specific functional Text Object Model selection over the RichEditBox plain-text buffer.
	//
	// Inherits the functional plain-text range behavior from UnoTextRange and adds the selection-only
	// surface (TypeText, Options, Type, horizontal Move*/HomeKey/EndKey). This is a programmatic
	// selection: it edits and navigates the buffer and drives rendering, but is not yet bound to an
	// interactive caret (that arrives with the shared editing engine extraction).
	//
	// TODO Uno: Vertical navigation (MoveUp/MoveDown) and per-line Home/End need the layout-aware
	// engine and are treated as no-ops for now.
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
				return Math.Abs(_start - old);
			}

			// Non-extending left move collapses to the start then moves the caret left.
			_end = _start;
			var previous = _start;
			_start = _end = Math.Clamp(_start - count, 0, _document.TextLength);
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
				return Math.Abs(_end - old);
			}

			// Non-extending right move collapses to the end then moves the caret right.
			_start = _end;
			var previous = _end;
			_start = _end = Math.Clamp(_end + count, 0, _document.TextLength);
			return Math.Abs(_end - previous);
		}

		public int HomeKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			// Only the Story unit (document start) is supported without a line-aware engine.
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

			return Math.Abs(old - _start);
		}

		public int EndKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
		{
			// Only the Story unit (document end) is supported without a line-aware engine.
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

			return Math.Abs(length - old);
		}

		// TODO Uno: Vertical caret navigation requires the layout-aware editing engine.
		public int MoveUp(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend) => 0;

		public int MoveDown(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend) => 0;
	}
}
