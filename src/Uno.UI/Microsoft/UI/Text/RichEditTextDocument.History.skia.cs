#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Text
{
	internal enum TextHistoryKind
	{
		None,
		Typing,
		Backspace,
		Delete,
	}

	internal readonly record struct ImageHistoryDiagnostics(
		int References,
		long EncodedBytes,
		long DecodedBytes,
		int DecodedCaches);

	public partial class RichEditTextDocument
	{
		private const long MaxUndoCost = 4 * 1024 * 1024;

		private readonly List<HistoryEntry> _undoStack = new();
		private readonly List<HistoryEntry> _redoStack = new();
		private int _undoLimit;
		private bool _undoEnabled = true;
		private bool _undoGrouping;
		private bool _undoGroupOverBudget;
		private HistoryEntry? _undoGroupEntry;
		private HistoryEntry? _lastRecordedEntry;
		private HistoryEntry? _coalescingEntry;
		private bool _isRestoringHistory;

		private readonly record struct TextEdit(int Start, int RemoveLength, int InsertLength);

		private readonly record struct HistoryRange(int Start, int End);

		private readonly record struct SelectionState(int Start, int End, bool IsStartActive);

		private sealed record RunDelta(
			int Start,
			int BeforeLength,
			int AfterLength,
			List<FormatRun> Before,
			List<FormatRun> After);

		private sealed record ParagraphRunDelta(
			int Start,
			int BeforeLength,
			int AfterLength,
			List<ParagraphRun> Before,
			List<ParagraphRun> After);

		private sealed record HistoryOperation(
			TextEdit? TextEdit,
			string BeforeText,
			string AfterText,
			RunDelta? CharacterRuns,
			ParagraphRunDelta? ParagraphRuns,
			ParagraphFormatState BeforeTerminalParagraph,
			ParagraphFormatState AfterTerminalParagraph,
			MathDocument? BeforeMathDocument,
			MathDocument? AfterMathDocument,
			bool BeforeMathMLUnavailable,
			bool AfterMathMLUnavailable);

		private sealed class HistoryEntry
		{
			public HistoryEntry(TextHistoryKind kind, SelectionState beforeSelection)
			{
				Kind = kind;
				BeforeSelection = beforeSelection;
				AfterSelection = beforeSelection;
			}

			public TextHistoryKind Kind { get; }

			public List<HistoryOperation> Operations { get; } = new();

			public SelectionState BeforeSelection { get; }

			public SelectionState AfterSelection { get; set; }

			public bool RestoresSelection { get; set; }

			public long Cost { get; set; }
		}

		internal int UndoEntryCount => _undoStack.Count;

		internal long UndoHistoryCost
		{
			get
			{
				long cost = 0;
				foreach (var entry in _undoStack)
				{
					cost += entry.Cost;
				}
				return cost;
			}
		}

		internal int UndoRetainedTextLength
		{
			get
			{
				var length = 0;
				foreach (var entry in _undoStack)
				{
					foreach (var operation in entry.Operations)
					{
						length = checked(length + operation.BeforeText.Length + operation.AfterText.Length);
					}
				}
				return length;
			}
		}

		internal int UndoRetainedRunCount
		{
			get
			{
				var count = 0;
				foreach (var entry in _undoStack)
				{
					foreach (var operation in entry.Operations)
					{
						count = checked(count
							+ (operation.CharacterRuns?.Before.Count ?? 0)
							+ (operation.CharacterRuns?.After.Count ?? 0)
							+ (operation.ParagraphRuns?.Before.Count ?? 0)
							+ (operation.ParagraphRuns?.After.Count ?? 0));
					}
				}
				return count;
			}
		}

		internal ImageHistoryDiagnostics UndoImageDiagnostics
		{
			get
			{
				var references = 0;
				long encodedBytes = 0;
				long decodedBytes = 0;
				var decodedCaches = 0;
				foreach (var entry in _undoStack)
				{
					foreach (var operation in entry.Operations)
					{
						Include(operation.CharacterRuns?.Before);
						Include(operation.CharacterRuns?.After);
					}
				}

				return new(references, encodedBytes, decodedBytes, decodedCaches);

				void Include(IReadOnlyList<FormatRun>? runs)
				{
					if (runs is null)
					{
						return;
					}

					foreach (var run in runs)
					{
						if (run.Format.InlineImage is not { } image)
						{
							continue;
						}

						references++;
						encodedBytes += image.EncodedLength;
						decodedBytes += image.DecodedByteLength;
						if (image.HasDecodedImage)
						{
							decodedCaches++;
						}
					}
				}
			}
		}

		private bool MutateWithUndo(
			Action mutate,
			Action? onChanged = null,
			TextEdit? textEdit = null,
			HistoryRange? characterRange = null,
			HistoryRange? paragraphRange = null,
			TextHistoryKind historyKind = TextHistoryKind.None,
			bool forceHistory = false)
		{
			if (_isRestoringHistory)
			{
				mutate();
				onChanged?.Invoke();
				return true;
			}

			if (!_undoGrouping)
			{
				_lastRecordedEntry = null;
			}

			var beforeSelection = CaptureSelectionState();
			var beforeMath = _mathDocument;
			var beforeMathMLUnavailable = _mathMLUnavailable;
			var beforeTerminal = _terminalParagraphFormat.Clone();
			var beforeText = string.Empty;
			var beforeCharacterRuns = new List<FormatRun>();
			var beforeParagraphRuns = new List<ParagraphRun>();
			var characterStart = 0;
			var characterBeforeLength = 0;
			var paragraphStart = 0;
			var paragraphBeforeLength = 0;

			if (textEdit is { } edit)
			{
				var editEnd = checked(edit.Start + edit.RemoveLength);
				beforeText = _textBuffer.Slice(edit.Start, edit.RemoveLength);
				characterStart = edit.Start;
				characterBeforeLength = edit.RemoveLength;
				beforeCharacterRuns = CaptureCharacterRunSlice(edit.Start, editEnd);
				var paragraphScope = GetHistoryParagraphScope(_textBuffer, edit.Start, editEnd);
				paragraphStart = paragraphScope.Start;
				paragraphBeforeLength = paragraphScope.End - paragraphScope.Start;
				beforeParagraphRuns = CaptureParagraphRunSlice(paragraphScope.Start, paragraphScope.End);
			}
			else
			{
				if (characterRange is { } characters)
				{
					characterStart = characters.Start;
					characterBeforeLength = characters.End - characters.Start;
					beforeCharacterRuns = CaptureCharacterRunSlice(characters.Start, characters.End);
				}
				if (paragraphRange is { } paragraphs)
				{
					paragraphStart = paragraphs.Start;
					paragraphBeforeLength = paragraphs.End - paragraphs.Start;
					beforeParagraphRuns = CaptureParagraphRunSlice(paragraphs.Start, paragraphs.End);
				}
			}
			mutate();

			var afterText = string.Empty;
			var afterCharacterRuns = new List<FormatRun>();
			var characterAfterLength = characterBeforeLength;
			var afterParagraphRuns = new List<ParagraphRun>();
			var paragraphAfterLength = paragraphBeforeLength;
			if (textEdit is { } appliedEdit)
			{
				afterText = _textBuffer.Slice(appliedEdit.Start, appliedEdit.InsertLength);
				characterAfterLength = appliedEdit.InsertLength;
				afterCharacterRuns = CaptureCharacterRunSlice(appliedEdit.Start, appliedEdit.Start + appliedEdit.InsertLength);
				var paragraphScope = GetHistoryParagraphScope(
					_textBuffer,
					appliedEdit.Start,
					appliedEdit.Start + appliedEdit.InsertLength);
				paragraphAfterLength = Math.Max(0, paragraphScope.End - paragraphStart);
				afterParagraphRuns = CaptureParagraphRunSlice(
					paragraphStart,
					paragraphStart + paragraphAfterLength);
			}
			else
			{
				if (characterRange is { } characters)
				{
					afterCharacterRuns = CaptureCharacterRunSlice(characters.Start, characters.End);
				}
				if (paragraphRange is { } paragraphs)
				{
					afterParagraphRuns = CaptureParagraphRunSlice(paragraphs.Start, paragraphs.End);
				}
			}

			var textChanged = textEdit is { } && !string.Equals(beforeText, afterText, StringComparison.Ordinal);
			var runsChanged = !RunsEqual(beforeCharacterRuns, afterCharacterRuns);
			var paragraphRunsChanged = !ParagraphRunsEqual(beforeParagraphRuns, afterParagraphRuns)
				|| !beforeTerminal.Equals(_terminalParagraphFormat);
			if (!textChanged
				&& (runsChanged || paragraphRunsChanged)
				&& ReferenceEquals(_mathDocument, beforeMath))
			{
				_mathDocument = null;
				_mathMLUnavailable = false;
			}

			var mathChanged = !ReferenceEquals(_mathDocument, beforeMath)
				|| _mathMLUnavailable != beforeMathMLUnavailable;
			var documentChanged = textChanged || runsChanged || paragraphRunsChanged || mathChanged;
			if (documentChanged)
			{
				if (runsChanged)
				{
					_characterFormatVersion++;
				}
				if (paragraphRunsChanged)
				{
					_paragraphFormatVersion++;
				}
				if (textChanged || runsChanged || paragraphRunsChanged)
				{
					var oldDocumentLength = textEdit is { } changedEdit
						? _textBuffer.Length - changedEdit.InsertLength + changedEdit.RemoveLength
						: _textBuffer.Length;
					var oldStart = int.MaxValue;
					var oldEnd = 0;
					var newStart = int.MaxValue;
					var newEnd = 0;

					void Include(int start, int beforeLength, int afterLength)
					{
						oldStart = Math.Min(oldStart, start);
						oldEnd = Math.Max(oldEnd, start + beforeLength);
						newStart = Math.Min(newStart, start);
						newEnd = Math.Max(newEnd, start + afterLength);
					}

					if (textEdit is { } renderTextEdit && textChanged)
					{
						Include(renderTextEdit.Start, renderTextEdit.RemoveLength, renderTextEdit.InsertLength);
					}
					if (runsChanged)
					{
						Include(characterStart, characterBeforeLength, characterAfterLength);
					}
					var paragraphSemanticsChanged = paragraphRunsChanged
						&& (textEdit is null
							|| ContainsParagraphBreak(beforeText)
							|| ContainsParagraphBreak(afterText)
							|| !beforeTerminal.Equals(_terminalParagraphFormat));
					if (paragraphRunsChanged && paragraphSemanticsChanged)
					{
						Include(paragraphStart, paragraphBeforeLength, paragraphAfterLength);
					}
					RecordRenderInvalidation(
						oldStart == int.MaxValue ? 0 : oldStart,
						oldEnd,
						newStart == int.MaxValue ? 0 : newStart,
						newEnd,
						paragraphSemanticsChanged,
						oldStart == 0
							&& oldEnd >= oldDocumentLength
							&& newStart == 0
							&& newEnd >= _textBuffer.Length);
				}
				onChanged?.Invoke();
			}
			var afterSelection = CaptureSelectionState();
			var selectionChanged = !beforeSelection.Equals(afterSelection);
			if (!documentChanged && !selectionChanged && !forceHistory)
			{
				return false;
			}

			var operation = new HistoryOperation(
				textEdit,
				beforeText,
				afterText,
				textEdit is not null || characterRange is not null
					? new RunDelta(
						characterStart,
						characterBeforeLength,
						characterAfterLength,
						beforeCharacterRuns,
						afterCharacterRuns)
					: null,
				textEdit is not null || paragraphRange is not null
					? new ParagraphRunDelta(
						paragraphStart,
						paragraphBeforeLength,
						paragraphAfterLength,
						beforeParagraphRuns,
						afterParagraphRuns)
					: null,
				beforeTerminal,
				_terminalParagraphFormat.Clone(),
				beforeMath,
				_mathDocument,
				beforeMathMLUnavailable,
				_mathMLUnavailable);

			if (_undoEnabled)
			{
				RecordHistoryOperation(operation, historyKind, beforeSelection, afterSelection, textEdit is not null || selectionChanged);
			}

			if (documentChanged)
			{
				RequestRender(textChanged);
			}
			return documentChanged;
		}

		internal void FinalizeHistorySelection()
		{
			var entry = _undoGrouping ? _undoGroupEntry : _lastRecordedEntry;
			if (entry is null)
			{
				return;
			}

			entry.AfterSelection = CaptureSelectionState();
			entry.RestoresSelection = true;
			_coalescingEntry = entry.Kind == TextHistoryKind.None ? null : entry;
		}

		internal void BreakHistoryCoalescing() => _coalescingEntry = null;

		private void RecordHistoryOperation(
			HistoryOperation operation,
			TextHistoryKind historyKind,
			SelectionState beforeSelection,
			SelectionState afterSelection,
			bool restoresSelection)
		{
			var operationCost = EstimateOperationCost(operation);
			if (_undoGrouping)
			{
				_coalescingEntry = null;
				if (_undoGroupOverBudget)
				{
					_redoStack.Clear();
					return;
				}

				var group = _undoGroupEntry ??= new HistoryEntry(TextHistoryKind.None, beforeSelection);
				if (group.Cost > MaxUndoCost - operationCost)
				{
					group.Operations.Clear();
					group.Cost = 0;
					_undoGroupEntry = null;
					_lastRecordedEntry = null;
					_undoGroupOverBudget = true;
					_redoStack.Clear();
					return;
				}

				group.Operations.Add(operation);
				group.AfterSelection = afterSelection;
				group.RestoresSelection |= restoresSelection;
				group.Cost += operationCost;
				_lastRecordedEntry = group;
				_redoStack.Clear();
				return;
			}

			if (historyKind != TextHistoryKind.None
				&& _undoStack.Count > 0
				&& ReferenceEquals(_undoStack[^1], _coalescingEntry)
				&& _undoStack[^1].Kind == historyKind)
			{
				var previous = _undoStack[^1];
				previous.Operations.Add(operation);
				previous.AfterSelection = afterSelection;
				previous.RestoresSelection |= restoresSelection;
				previous.Cost += operationCost;
				_lastRecordedEntry = previous;
				_coalescingEntry = previous;
			}
			else
			{
				var entry = new HistoryEntry(historyKind, beforeSelection)
				{
					AfterSelection = afterSelection,
					RestoresSelection = restoresSelection,
					Cost = operationCost,
				};
				entry.Operations.Add(operation);
				_undoStack.Add(entry);
				_lastRecordedEntry = entry;
				_coalescingEntry = historyKind == TextHistoryKind.None ? null : entry;
			}

			TrimUndoStack();
			_redoStack.Clear();
		}

		private void ApplyHistoryEntry(HistoryEntry entry, bool undo)
		{
			_isRestoringHistory = true;
			try
			{
				if (undo)
				{
					for (var i = entry.Operations.Count - 1; i >= 0; i--)
					{
						ApplyHistoryOperation(entry.Operations[i], undo: true);
					}
				}
				else
				{
					for (var i = 0; i < entry.Operations.Count; i++)
					{
						ApplyHistoryOperation(entry.Operations[i], undo: false);
					}
				}

				if (entry.RestoresSelection)
				{
					var selection = undo ? entry.BeforeSelection : entry.AfterSelection;
					((UnoTextSelection)Selection).SetRangeInternal(
						selection.Start,
						selection.End,
						selection.IsStartActive);
				}
			}
			finally
			{
				_isRestoringHistory = false;
			}

			RequestRender(entry.Operations.Exists(static operation =>
				operation.TextEdit is not null
				&& !string.Equals(operation.BeforeText, operation.AfterText, StringComparison.Ordinal)));
		}

		private void ApplyHistoryOperation(HistoryOperation operation, bool undo)
		{
			var oldDocumentLength = _textBuffer.Length;
			var oldTerminalParagraph = _terminalParagraphFormat;
			var oldStart = int.MaxValue;
			var oldEnd = 0;
			var newStart = int.MaxValue;
			var newEnd = 0;

			void Include(int start, int beforeLength, int afterLength)
			{
				oldStart = Math.Min(oldStart, start);
				oldEnd = Math.Max(oldEnd, start + beforeLength);
				newStart = Math.Min(newStart, start);
				newEnd = Math.Max(newEnd, start + afterLength);
			}

			if (operation.CharacterRuns is { } character)
			{
				var removeLength = undo ? character.AfterLength : character.BeforeLength;
				var target = undo ? character.Before : character.After;
				ReplaceRuns(character.Start, character.Start + removeLength, target);
				_characterFormatVersion++;
				Include(character.Start, removeLength, undo ? character.BeforeLength : character.AfterLength);
			}

			var beforeOperationText = undo ? operation.AfterText : operation.BeforeText;
			var afterOperationText = undo ? operation.BeforeText : operation.AfterText;
			var paragraphSemanticsChanged = operation.ParagraphRuns is not null
				&& (operation.TextEdit is null
					|| ContainsParagraphBreak(beforeOperationText)
					|| ContainsParagraphBreak(afterOperationText)
					|| !oldTerminalParagraph.Equals(undo
						? operation.BeforeTerminalParagraph
						: operation.AfterTerminalParagraph));

			if (operation.ParagraphRuns is { } paragraph)
			{
				var removeLength = undo ? paragraph.AfterLength : paragraph.BeforeLength;
				var target = undo ? paragraph.Before : paragraph.After;
				ReplaceParagraphRuns(paragraph.Start, paragraph.Start + removeLength, target);
				_paragraphFormatVersion++;
				if (paragraphSemanticsChanged)
				{
					Include(paragraph.Start, removeLength, undo ? paragraph.BeforeLength : paragraph.AfterLength);
				}
			}

			if (operation.TextEdit is { } edit)
			{
				var sourceLength = undo ? edit.InsertLength : edit.RemoveLength;
				var targetText = undo ? operation.BeforeText : operation.AfterText;
				_textBuffer.Replace(edit.Start, sourceLength, targetText);
				RebaseRanges(
					edit.Start,
					edit.Start + sourceLength,
					targetText.Length,
					(UnoTextSelection)Selection);
				Include(edit.Start, sourceLength, targetText.Length);
			}

			_terminalParagraphFormat = (undo
				? operation.BeforeTerminalParagraph
				: operation.AfterTerminalParagraph).Clone();
			_mathDocument = undo ? operation.BeforeMathDocument : operation.AfterMathDocument;
			_mathMLUnavailable = undo
				? operation.BeforeMathMLUnavailable
				: operation.AfterMathMLUnavailable;
			if (oldStart != int.MaxValue)
			{
				RecordRenderInvalidation(
					oldStart,
					oldEnd,
					newStart,
					newEnd,
					paragraphSemanticsChanged,
					oldStart == 0
						&& oldEnd >= oldDocumentLength
						&& newStart == 0
						&& newEnd >= _textBuffer.Length);
			}
		}

		private List<FormatRun> CaptureCharacterRunSlice(int start, int end)
		{
			SyncRunsToLength(_textBuffer.Length);
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			var result = new List<FormatRun>();
			if (start == end)
			{
				return result;
			}

			var cursor = _runs.GetCursor(FindRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				AppendRun(
					result,
					Math.Min(end, runEnd) - Math.Max(start, runStart),
					cursor.Current.Format);
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}
			return result;
		}

		private List<ParagraphRun> CaptureParagraphRunSlice(int start, int end)
		{
			SyncParagraphRunsToLength(_textBuffer.Length);
			start = Math.Clamp(start, 0, _textBuffer.Length);
			end = Math.Clamp(end, start, _textBuffer.Length);
			var result = new List<ParagraphRun>();
			if (start == end)
			{
				return result;
			}

			var cursor = _paragraphRuns.GetCursor(FindParagraphRunIndex(start));
			while (cursor.IsValid)
			{
				var runStart = cursor.Start;
				var runEnd = cursor.End;
				AppendParagraphRun(
					result,
					Math.Min(end, runEnd) - Math.Max(start, runStart),
					cursor.Current.Format);
				if (runEnd >= end)
				{
					break;
				}
				cursor.MoveNext();
			}
			return result;
		}

		private static HistoryRange GetHistoryParagraphScope(TextStoryBuffer text, int start, int end)
		{
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, start, text.Length);
			var scopeStartProbe = start > 0 ? start - 1 : 0;
			var precedingBreak = text.LastIndexOf('\r', 0, scopeStartProbe);
			var scopeStart = precedingBreak + 1;
			if (scopeStart > 0)
			{
				var previousBreak = text.LastIndexOf('\r', 0, scopeStart - 1);
				scopeStart = previousBreak + 1;
			}

			var scopeEnd = Math.Min(text.Length, Math.Max(end, start) + (end < text.Length ? 1 : 0));
			if (scopeEnd < text.Length && text[scopeEnd - 1] != '\r')
			{
				var nextBreak = text.IndexOf('\r', scopeEnd - 1, text.Length - scopeEnd + 1);
				scopeEnd = nextBreak < 0 ? text.Length : nextBreak + 1;
			}
			if (scopeEnd < text.Length)
			{
				scopeEnd++;
				if (scopeEnd < text.Length && text[scopeEnd - 1] != '\r')
				{
					var nextBreak = text.IndexOf('\r', scopeEnd - 1, text.Length - scopeEnd + 1);
					scopeEnd = nextBreak < 0 ? text.Length : nextBreak + 1;
				}
			}

			return new HistoryRange(scopeStart, scopeEnd);
		}

		private SelectionState CaptureSelectionState()
		{
			var selection = (UnoTextSelection)Selection;
			return new SelectionState(
				selection.StartPosition,
				selection.EndPosition,
				selection.StartPosition != selection.EndPosition
					&& selection.Options.HasFlag(global::Microsoft.UI.Text.SelectionOptions.StartActive));
		}

		private static long EstimateOperationCost(HistoryOperation operation)
		{
			long cost = (operation.BeforeText.Length + operation.AfterText.Length) * sizeof(char);
			cost += EstimateRunsCost(operation.CharacterRuns?.Before);
			cost += EstimateRunsCost(operation.CharacterRuns?.After);
			cost += EstimateParagraphRunsCost(operation.ParagraphRuns?.Before);
			cost += EstimateParagraphRunsCost(operation.ParagraphRuns?.After);
			cost += EstimateParagraphFormatCost(operation.BeforeTerminalParagraph);
			cost += EstimateParagraphFormatCost(operation.AfterTerminalParagraph);
			if (!ReferenceEquals(operation.BeforeMathDocument, operation.AfterMathDocument))
			{
				cost += operation.BeforeMathDocument?.CanonicalMathML.Length * sizeof(char) ?? 0;
				cost += operation.AfterMathDocument?.CanonicalMathML.Length * sizeof(char) ?? 0;
			}
			return cost;
		}

		private static long EstimateRunsCost(IReadOnlyList<FormatRun>? runs)
		{
			if (runs is null)
			{
				return 0;
			}

			long cost = 0;
			foreach (var run in runs)
			{
				cost += 128;
				if (run.Format.InlineImage is { } image)
				{
					cost += image.EncodedLength;
					cost += image.DecodedByteLength;
				}
			}
			return cost;
		}

		private static long EstimateParagraphRunsCost(IReadOnlyList<ParagraphRun>? runs)
		{
			if (runs is null)
			{
				return 0;
			}

			long cost = 0;
			foreach (var run in runs)
			{
				cost += EstimateParagraphFormatCost(run.Format);
			}
			return cost;
		}

		private static long EstimateParagraphFormatCost(ParagraphFormatState format)
			=> 128 + format.Tabs.Count * 16L;

		private void TrimUndoStack()
		{
			long cost = 0;
			for (var i = _undoStack.Count - 1; i >= 0; i--)
			{
				cost += _undoStack[i].Cost;
				if (cost > MaxUndoCost)
				{
					_undoStack.RemoveRange(0, i + 1);
					break;
				}
			}

			while (_undoLimit > 0 && _undoStack.Count > _undoLimit)
			{
				_undoStack.RemoveAt(0);
			}

			if (_lastRecordedEntry is not null && !_undoStack.Contains(_lastRecordedEntry))
			{
				_lastRecordedEntry = null;
				_coalescingEntry = null;
			}
		}

		/// <summary>
		/// Gets or sets the maximum number of actions that can be undone. The native default reports
		/// zero while history remains enabled; assigning zero clears and disables history.
		/// </summary>
		public uint UndoLimit
		{
			get => (uint)_undoLimit;
			set
			{
				_undoLimit = (int)Math.Min(value, int.MaxValue);
				_undoEnabled = value != 0;
				if (!_undoEnabled)
				{
					ClearUndoRedoHistory();
				}
				else
				{
					TrimUndoStack();
				}
			}
		}

		/// <summary>Gets a value that indicates whether the most recent undo action can be redone.</summary>
		public bool CanRedo() => _redoStack.Count > 0;

		/// <summary>Gets a value that indicates whether the most recent action can be undone.</summary>
		public bool CanUndo() => _undoStack.Count > 0;

		/// <summary>Redoes the most recent undo action.</summary>
		public void Redo()
		{
			ThrowIfNotEditable(0, _textBuffer.Length);
			if (_redoStack.Count == 0)
			{
				return;
			}

			var index = _redoStack.Count - 1;
			var entry = _redoStack[index];
			_redoStack.RemoveAt(index);
			ApplyHistoryEntry(entry, undo: false);
			_undoStack.Add(entry);
			TrimUndoStack();
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}

		/// <summary>Undoes the most recent action.</summary>
		public void Undo()
		{
			ThrowIfNotEditable(0, _textBuffer.Length);
			if (_undoStack.Count == 0)
			{
				return;
			}

			var index = _undoStack.Count - 1;
			var entry = _undoStack[index];
			_undoStack.RemoveAt(index);
			ApplyHistoryEntry(entry, undo: true);
			_redoStack.Add(entry);
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}

		/// <summary>Turns on undo grouping until the next <see cref="EndUndoGroup"/> call.</summary>
		public void BeginUndoGroup()
		{
			if (_undoGrouping)
			{
				return;
			}

			_undoGrouping = true;
			_undoGroupOverBudget = false;
			_undoGroupEntry = null;
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}

		/// <summary>Turns off undo grouping and commits the collected edits as one action.</summary>
		public void EndUndoGroup()
		{
			if (!_undoGrouping)
			{
				return;
			}

			_undoGrouping = false;
			if (_undoGroupEntry is { Operations.Count: > 0 } group && _undoEnabled)
			{
				_undoStack.Add(group);
				TrimUndoStack();
			}
			_undoGroupEntry = null;
			_undoGroupOverBudget = false;
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}

		internal void DiscardUndoGroup()
		{
			_undoGrouping = false;
			_undoGroupOverBudget = false;
			_undoGroupEntry = null;
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}

		/// <summary>Clears the undo and redo history and terminates any open undo group.</summary>
		public void ClearUndoRedoHistory()
		{
			_undoStack.Clear();
			_redoStack.Clear();
			_undoGrouping = false;
			_undoGroupOverBudget = false;
			_undoGroupEntry = null;
			_lastRecordedEntry = null;
			_coalescingEntry = null;
		}
	}
}
