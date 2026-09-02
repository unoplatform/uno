#nullable enable

using System;
using System.Globalization;

namespace Microsoft.UI.Text
{
	internal sealed class UnoTextSelection : UnoTextRange, global::Microsoft.UI.Text.ITextSelection
	{
		private const global::Microsoft.UI.Text.SelectionOptions WritableOptions =
			global::Microsoft.UI.Text.SelectionOptions.StartActive
			| global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine
			| global::Microsoft.UI.Text.SelectionOptions.Overtype;

		private global::Microsoft.UI.Text.SelectionOptions _options;
		private int _lastNotifiedStart;
		private int _lastNotifiedEnd;
		private double? _desiredX;
		private bool _preserveDesiredX;

		internal UnoTextSelection(RichEditTextDocument document)
			: base(document, 0, 0)
		{
		}

		public global::Microsoft.UI.Text.SelectionOptions Options
		{
			get
			{
				var options = (_options & WritableOptions) | global::Microsoft.UI.Text.SelectionOptions.Replace;
				if (_start == _end)
				{
					options |= global::Microsoft.UI.Text.SelectionOptions.StartActive;
				}
				return options;
			}
			set
			{
				var wasStartActive = IsStartActive;
				_options = value & WritableOptions;
				_desiredX = null;
				if (wasStartActive != IsStartActive)
				{
					_document.NotifySelectionDirectionChanged();
				}
			}
		}

		public global::Microsoft.UI.Text.SelectionType Type
			=> _start == _end
				? global::Microsoft.UI.Text.SelectionType.InsertionPoint
				: _document.IsInlineObjectRange(_start, _end)
					? global::Microsoft.UI.Text.SelectionType.InlineShape
					: global::Microsoft.UI.Text.SelectionType.Normal;

		public void TypeText(string value)
		{
			var text = _document.CoerceTypedText(value ?? string.Empty);
			var replaceEnd = _end;
			if (_start == _end
				&& _options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.Overtype)
				&& text.Length > 0)
			{
				var clusterCount = StringInfo.ParseCombiningCharacters(text).Length;
				var clusters = GetUnitBoundarySet(global::Microsoft.UI.Text.TextRangeUnit.Cluster);
				if (clusters is not null)
				{
					replaceEnd = Math.Min(
						_document.TextLength,
						clusters.Move(_start, clusterCount, out _));
				}
			}

			var insertedLength = _document.ReplaceRange(
				_start,
				replaceEnd,
				text,
				this,
				historyKind: TextHistoryKind.Typing);
			_start = _end = _start + insertedLength;
			SetAtEndOfLine(text.Length > 0 && _document.IsVisualLineEnd(_end));
			OnRangeChanged();
			_document.FinalizeHistorySelection();
		}

		public override void Copy() => _document.CopySelectionToClipboardViaControl(this);

		public override void Cut() => _document.CutSelectionToClipboardViaControl(this);

		public override void Paste(int format)
		{
			if (_document.TryBeginSelectionPasteViaControl())
			{
				base.Paste(format);
			}
		}

		public override void SetRange(int startPosition, int endPosition)
		{
			var wasStartActive = IsStartActive;
			var startActive = startPosition > endPosition;
			_start = Math.Min(startPosition, endPosition);
			_end = Math.Max(startPosition, endPosition);
			Normalize();
			SetStartActive(startActive);
			SetAtEndOfLine(_document.IsVisualLineEnd(Math.Min(GetActivePosition(), _document.TextLength)));
			var directionChanged = wasStartActive != IsStartActive;
			var positionsChanged = _start != _lastNotifiedStart || _end != _lastNotifiedEnd;
			OnRangeChanged();
			if (!positionsChanged && directionChanged)
			{
				_document.NotifySelectionDirectionChanged();
			}
		}

		internal void SetRangeAfterTextMutation(int start, int end)
		{
			_start = start;
			_end = end;
			Normalize();
			SetStartActive(false);
			SetAtEndOfLine(_document.IsVisualLineEnd(Math.Min(_end, _document.TextLength)));
			OnRangeChanged();
		}

		internal void SetRangeInternal(int start, int end, bool selectionEndsAtTheStart)
		{
			base.SetRangeInternal(start, end);
			SetStartActive(selectionEndsAtTheStart);
			_lastNotifiedStart = _start;
			_lastNotifiedEnd = _end;
			_desiredX = null;
		}

		public int MoveLeft(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveHorizontal(unit, count, extend, baseDirection: -1);

		public int MoveRight(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveHorizontal(unit, count, extend, baseDirection: 1);

		private int MoveHorizontal(
			global::Microsoft.UI.Text.TextRangeUnit unit,
			int count,
			bool extend,
			int baseDirection)
		{
			ValidateHorizontalUnit(unit);
			if (count == 0)
			{
				return 0;
			}

			var boundaries = GetUnitBoundarySet(unit);
			if (boundaries is null)
			{
				return 0;
			}

			var countSign = count < 0 ? -1 : 1;
			var direction = baseDirection * countSign;
			var requested = count == int.MinValue ? int.MaxValue : Math.Abs(count);
			var oldStart = _start;
			var oldEnd = _end;
			var unitsMoved = 0;
			var target = GetActivePosition();

			if (extend)
			{
				target = boundaries.Move(target, direction * requested, out var moved);
				unitsMoved = Math.Abs(moved);
				SetActivePosition(target, extend: true);
			}
			else if (_start != _end)
			{
				target = direction < 0 ? _start : _end;
				var collapseConsumesUnit = unit is global::Microsoft.UI.Text.TextRangeUnit.Character
					or global::Microsoft.UI.Text.TextRangeUnit.Cluster
					|| unit == global::Microsoft.UI.Text.TextRangeUnit.Word && boundaries.HasStartAt(target);
				var remaining = requested - (collapseConsumesUnit ? 1 : 0);
				unitsMoved = collapseConsumesUnit ? 1 : 0;
				if (remaining > 0)
				{
					target = boundaries.Move(target, direction * remaining, out var moved);
					unitsMoved += Math.Abs(moved);
				}

				_start = _end = Math.Clamp(target, 0, _document.TextLength);
			}
			else
			{
				target = boundaries.Move(target, direction * requested, out var moved);
				unitsMoved = Math.Abs(moved);
				_start = _end = Math.Clamp(target, 0, _document.TextLength);
			}

			SetAtEndOfLine(direction > 0 && _document.IsVisualLineEnd(Math.Min(target, _document.TextLength)));
			OnRangeChanged();
			return _start == oldStart && _end == oldEnd ? 0 : countSign * unitsMoved;
		}

		public int HomeKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
			=> MoveToHorizontalBoundary(unit, extend, home: true);

		public int EndKey(global::Microsoft.UI.Text.TextRangeUnit unit, bool extend)
			=> MoveToHorizontalBoundary(unit, extend, home: false);

		private int MoveToHorizontalBoundary(
			global::Microsoft.UI.Text.TextRangeUnit unit,
			bool extend,
			bool home)
		{
			ValidateHomeEndUnit(unit);
			var current = GetActivePosition();
			int target;
			if (unit == global::Microsoft.UI.Text.TextRangeUnit.Line)
			{
				if (!_document.TryGetLineBounds(
					current,
					_options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine),
					out var lineStart,
					out var lineEnd,
					out _,
					out _))
				{
					return 0;
				}
				target = home ? lineStart : lineEnd;
			}
			else
			{
				target = home ? 0 : _document.TextLength;
			}

			SetActivePosition(target, extend);
			SetAtEndOfLine(!home && unit == global::Microsoft.UI.Text.TextRangeUnit.Line);
			OnRangeChanged();
			return GetActivePosition() - current;
		}

		public int MoveUp(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(unit, up: true, count, extend);

		public int MoveDown(global::Microsoft.UI.Text.TextRangeUnit unit, int count, bool extend)
			=> MoveVertical(unit, up: false, count, extend);

		private int MoveVertical(global::Microsoft.UI.Text.TextRangeUnit unit, bool up, int count, bool extend)
		{
			ValidateVerticalUnit(unit);
			if (count == 0)
			{
				return 0;
			}

			var countSign = count < 0 ? -1 : 1;
			var effectiveUp = count < 0 ? !up : up;
			var requested = count == int.MinValue ? int.MaxValue : Math.Abs(count);
			var navigationCount = Math.Min(requested, Math.Max(1, _document.TextLength + 1));
			var current = unit == global::Microsoft.UI.Text.TextRangeUnit.Paragraph && _start != _end
				? effectiveUp ? _start : _end
				: GetActivePosition();
			var oldStart = _start;
			var oldEnd = _end;
			var target = current;
			var unitsMoved = 0;
			var targetAtEndOfLine = false;
			var moved = unit switch
			{
				global::Microsoft.UI.Text.TextRangeUnit.Paragraph =>
					TryGetParagraphTarget(current, effectiveUp, requested, out target, out unitsMoved),
				global::Microsoft.UI.Text.TextRangeUnit.Line =>
					_document.TryGetVerticalTarget(
						current,
						effectiveUp,
						navigationCount,
						_options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine),
						ref _desiredX,
						out target,
						out unitsMoved,
						out targetAtEndOfLine),
				global::Microsoft.UI.Text.TextRangeUnit.Screen =>
					_document.TryGetPageTarget(
						current,
						effectiveUp,
						navigationCount,
						_options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine),
						ref _desiredX,
						out target,
						out unitsMoved,
						out targetAtEndOfLine),
				global::Microsoft.UI.Text.TextRangeUnit.Window =>
					TryGetWindowTarget(current, effectiveUp, out target, out unitsMoved, out targetAtEndOfLine),
				_ => false,
			};
			if (!moved || target == current)
			{
				return 0;
			}

			if (extend
				&& !effectiveUp
				&& target == _document.TextLength
				&& unit is global::Microsoft.UI.Text.TextRangeUnit.Screen or global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				target = _document.StoryLength;
				targetAtEndOfLine = unit == global::Microsoft.UI.Text.TextRangeUnit.Window;
			}

			SetActivePosition(target, extend);
			SetAtEndOfLine(targetAtEndOfLine);
			_preserveDesiredX = true;
			try
			{
				OnRangeChanged();
			}
			finally
			{
				_preserveDesiredX = false;
			}
			return _start == oldStart && _end == oldEnd ? 0 : countSign * unitsMoved;
		}

		private bool TryGetParagraphTarget(int position, bool up, int count, out int target, out int unitsMoved)
		{
			target = position;
			unitsMoved = 0;
			var paragraphs = GetUnitBoundarySet(global::Microsoft.UI.Text.TextRangeUnit.Paragraph);
			if (paragraphs is null)
			{
				return false;
			}

			target = paragraphs.Move(position, up ? -count : count, out var moved);
			unitsMoved = Math.Abs(moved);
			return unitsMoved != 0;
		}

		private bool TryGetWindowTarget(
			int position,
			bool up,
			out int target,
			out int unitsMoved,
			out bool targetAtEndOfLine)
		{
			target = position;
			unitsMoved = 0;
			targetAtEndOfLine = false;
			if (!_document.TryGetVisibleRange(out var windowStart, out var windowEnd))
			{
				return false;
			}

			target = up ? windowStart : windowEnd;
			if (up ? target >= position : target <= position)
			{
				return false;
			}

			unitsMoved = 1;
			targetAtEndOfLine = !up && _document.IsVisualLineEnd(target);
			return true;
		}

		private static void ValidateHorizontalUnit(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit is not global::Microsoft.UI.Text.TextRangeUnit.Character
				and not global::Microsoft.UI.Text.TextRangeUnit.Word
				and not global::Microsoft.UI.Text.TextRangeUnit.Cluster)
			{
				throw new ArgumentException("The text range unit is invalid for horizontal selection movement.", nameof(unit));
			}
		}

		private static void ValidateHomeEndUnit(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit is not global::Microsoft.UI.Text.TextRangeUnit.Line
				and not global::Microsoft.UI.Text.TextRangeUnit.Story)
			{
				throw new ArgumentException("The text range unit is invalid for HomeKey or EndKey.", nameof(unit));
			}
		}

		private static void ValidateVerticalUnit(global::Microsoft.UI.Text.TextRangeUnit unit)
		{
			if (unit is not global::Microsoft.UI.Text.TextRangeUnit.Paragraph
				and not global::Microsoft.UI.Text.TextRangeUnit.Line
				and not global::Microsoft.UI.Text.TextRangeUnit.Screen
				and not global::Microsoft.UI.Text.TextRangeUnit.Window)
			{
				throw new ArgumentException("The text range unit is invalid for vertical selection movement.", nameof(unit));
			}
		}

		private bool IsStartActive
			=> _start != _end && _options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive);

		private int GetActivePosition() => IsStartActive ? _start : _end;

		private void SetActivePosition(int position, bool extend)
		{
			position = Math.Clamp(position, 0, extend ? _document.StoryLength : _document.TextLength);
			if (!extend)
			{
				_start = _end = Math.Min(position, _document.TextLength);
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.StartActive;
				return;
			}

			var anchor = IsStartActive ? _end : _start;
			if (position < anchor)
			{
				_start = Math.Min(position, _document.TextLength);
				_end = anchor;
				_options |= global::Microsoft.UI.Text.SelectionOptions.StartActive;
			}
			else
			{
				_start = Math.Min(anchor, _document.TextLength);
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

		private void SetAtEndOfLine(bool value)
		{
			if (value)
			{
				_options |= global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine;
			}
			else
			{
				_options &= ~global::Microsoft.UI.Text.SelectionOptions.AtEndOfLine;
			}
		}

		private protected override void OnRangeChanged()
		{
			if (_document.TextLength == 0 && _end > 0)
			{
				_start = _end = 0;
			}

			if (!_preserveDesiredX)
			{
				_desiredX = null;
			}

			if (_start == _lastNotifiedStart && _end == _lastNotifiedEnd)
			{
				return;
			}

			_lastNotifiedStart = _start;
			_lastNotifiedEnd = _end;
			_document.NotifySelectionChanged();
		}

		private protected override void OnRebasedAfterEdit()
			=> _desiredX = null;
	}
}
