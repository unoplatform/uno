#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox Text Object Model document for Skia.
	//
	// This increment provides a working text core: SetText/GetText round-trip, a functional Text
	// Object Model surface (GetRange/Selection returning UnoTextRange/UnoTextSelection) that navigates
	// and edits the buffer and drives the owning RichEditBox's shared rendering, a functional
	// character-formatting run model (see RichEditTextDocument.Formatting.skia.cs and
	// UnoTextRange.CharacterFormat), a functional paragraph-formatting run model (see
	// RichEditTextDocument.ParagraphFormatting.skia.cs and UnoTextRange.ParagraphFormat), a
	// snapshot-based undo/redo stack over text and both formatting models
	// (CanUndo/CanRedo/Undo/Redo/UndoLimit) with grouping (BeginUndoGroup/EndUndoGroup), and display
	// batching (BatchDisplayUpdates/ApplyDisplayUpdates).
	//
	// TODO Uno: The following are subsequent increments and remain [NotImplemented] in the generated
	// stub for now: character/paragraph default formats, RTF and stream load/save, embedded images
	// and MathML. Paragraph formatting round-trips through the model but is not rendered (the shared
	// DisplayBlock is a single TextBlock) — see RichEditTextDocument.ParagraphFormatting.skia.cs.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private readonly List<Snapshot> _undoStack = new();
		private readonly List<Snapshot> _redoStack = new();
		private string _plainText = string.Empty;
		private UnoTextSelection? _selection;
		private int _undoLimit = 100;

		// Undo grouping: while a group is open, individual edits do not each push an undo entry.
		// Instead, a single snapshot captured when the outermost group opened is committed on close,
		// so BeginUndoGroup/EndUndoGroup collapse a run of edits into one undoable action.
		private int _undoGroupDepth;
		private Snapshot? _undoGroupSnapshot;

		// Display batching: while batched, render requests are coalesced and applied once the
		// outermost ApplyDisplayUpdates balances the matching BatchDisplayUpdates.
		private int _batchDepth;
		private bool _pendingRender;

		internal RichEditTextDocument(RichEditBox owner)
		{
			_owner = owner;
		}

		// A point-in-time copy of the document used for undo/redo: the plain text plus deep clones of
		// the character-formatting runs and paragraph-formatting runs.
		private sealed record Snapshot(string Text, List<FormatRun> Runs, List<ParagraphRun> ParagraphRuns);

		/// <summary>The current plain-text content of the document.</summary>
		internal string PlainText => _plainText;

		/// <summary>The number of characters in the plain-text buffer.</summary>
		internal int TextLength => _plainText.Length;

		/// <summary>Returns the substring of the plain-text buffer between two clamped positions.</summary>
		internal string GetTextInRange(int start, int end)
		{
			start = Math.Clamp(start, 0, _plainText.Length);
			end = Math.Clamp(end, start, _plainText.Length);
			return _plainText.Substring(start, end - start);
		}

		/// <summary>
		/// Replaces the plain-text between <paramref name="start"/> and <paramref name="end"/> with
		/// <paramref name="replacement"/>, splices the formatting runs accordingly and re-renders. Used
		/// by <see cref="UnoTextRange"/> editing.
		/// </summary>
		internal void ReplaceRange(int start, int end, string replacement)
		{
			MutateWithUndo(() =>
			{
				var text = _plainText;
				start = Math.Clamp(start, 0, text.Length);
				end = Math.Clamp(end, start, text.Length);
				var insert = replacement ?? string.Empty;

				// Keep the run model aligned with the pre-edit text, then splice it in lock-step with the
				// text edit so inserted characters inherit the neighbouring formatting.
				SyncRunsToLength(text.Length);
				SyncParagraphRunsToLength(text.Length);
				SpliceRuns(start, end - start, insert.Length);
				SpliceParagraphRuns(start, end - start, insert.Length);
				_plainText = text.Substring(0, start) + insert + text.Substring(end);
			});

			// The pending caret format (if any) has now been consumed by the splice above, or the caret
			// context has changed by an edit that didn't consume it; either way it no longer applies.
			ClearPendingCaretFormat();
		}

		/// <summary>
		/// Runs a buffer/formatting mutation, capturing a before-snapshot for undo (unless nothing
		/// changed or undo is disabled), clearing the redo stack and re-rendering the owning control.
		/// </summary>
		private void MutateWithUndo(Action mutate)
		{
			var before = CaptureSnapshot();
			mutate();

			if (string.Equals(_plainText, before.Text, StringComparison.Ordinal)
				&& RunsEqual(_runs, before.Runs)
				&& ParagraphRunsEqual(_paragraphRuns, before.ParagraphRuns))
			{
				return;
			}

			if (_undoLimit != 0)
			{
				if (_undoGroupDepth > 0)
				{
					// Inside an open undo group: the group's single snapshot (captured at
					// BeginUndoGroup) is committed on close; each edit still invalidates redo.
					_redoStack.Clear();
				}
				else
				{
					_undoStack.Add(before);
					TrimUndoStack();
					_redoStack.Clear();
				}
			}

			RequestRender();
		}

		// Trigger a re-render of the shared DisplayBlock, deferring while display updates are batched.
		private void RequestRender()
		{
			if (_batchDepth > 0)
			{
				_pendingRender = true;
				return;
			}

			_owner.OnDocumentTextChanged();
		}

		private Snapshot CaptureSnapshot() => new(_plainText, CloneRuns(_runs), CloneParagraphRuns(_paragraphRuns));

		private void RestoreSnapshot(Snapshot snapshot)
		{
			_plainText = snapshot.Text;
			_runs = CloneRuns(snapshot.Runs);
			_paragraphRuns = CloneParagraphRuns(snapshot.ParagraphRuns);
		}

		private void TrimUndoStack()
		{
			while (_undoLimit > 0 && _undoStack.Count > _undoLimit)
			{
				_undoStack.RemoveAt(0);
			}
		}

		/// <summary>
		/// Gets a text range for the specified range of text positions.
		/// </summary>
		public global::Microsoft.UI.Text.ITextRange GetRange(int startPosition, int endPosition)
			=> new UnoTextRange(this, startPosition, endPosition);

		/// <summary>
		/// Gets a degenerate text range at the character position nearest the specified point.
		/// </summary>
		public global::Microsoft.UI.Text.ITextRange GetRangeFromPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options)
		{
			if (_owner.TryGetIndexFromPoint(point, options, out var index))
			{
				return GetRange(index, index);
			}

			return GetRange(0, 0);
		}

		/// <summary>
		/// Gets the current text selection as an <see cref="ITextSelection"/>.
		/// </summary>
		public global::Microsoft.UI.Text.ITextSelection Selection => _selection ??= new UnoTextSelection(this);

		/// <summary>
		/// Mirrors the owning control's interactive caret/selection into <see cref="Selection"/> without
		/// triggering the drag semantics of the public position setters. Used by the interactive editor.
		/// </summary>
		internal void SetSelectionRangeInternal(int start, int end)
		{
			// Moving the caret away from a pending insertion-point format discards it.
			ClearPendingCaretFormatIfMoved(start, end);
			((UnoTextRange)Selection).SetRangeInternal(start, end);
		}

		/// <summary>
		/// Raised by <see cref="UnoTextSelection"/> when the programmatic selection changes through the
		/// public API, so the owning control can sync its interactive caret/selection and re-render.
		/// This is the reverse of <see cref="SetSelectionRangeInternal"/> and is not called by it.
		/// </summary>
		internal void NotifySelectionChanged()
		{
			// A programmatic selection move away from a pending insertion-point format discards it.
			if (_selection is { } selection)
			{
				ClearPendingCaretFormatIfMoved(selection.StartPosition, selection.EndPosition);
			}

			_owner.OnTomSelectionChanged();
		}

		// Programmatic Selection.Copy/Cut/Paste (ITextSelection) route here so the owning control raises
		// its CopyingToClipboard / CuttingToClipboard / Paste events. The interactive selection is first
		// synced from the TOM selection (non-focus-gated) because these operate on Document.Selection even
		// when the control isn't focused (the WinUI conformance tests never focus it).
		internal void CopySelectionToClipboardViaControl()
		{
			_owner.SyncInteractiveSelectionFromTomSelection();
			_owner.CopySelectionToClipboard();
		}

		internal void CutSelectionToClipboardViaControl()
		{
			_owner.SyncInteractiveSelectionFromTomSelection();
			_owner.CutSelectionToClipboard();
		}

		internal void PasteFromClipboardViaControl()
		{
			_owner.SyncInteractiveSelectionFromTomSelection();
			_owner.PasteFromClipboard();
		}

		// --- Geometry-backed line navigation (delegates to the owning control's DisplayBlock layout) ---

		internal bool TryGetLineBounds(int position, out int lineStart, out int lineEnd, out int lineIndex, out bool isLast)
			=> _owner.TryGetLineBounds(position, out lineStart, out lineEnd, out lineIndex, out isLast);

		internal int GetLineCount() => _owner.GetLineCountForTom();

		internal bool TryGetVerticalTarget(int position, bool up, int count, out int target)
			=> _owner.TryGetVerticalTarget(position, up, count, out target);

		// --- Geometry-backed coordinate mapping (delegates to the owning control's DisplayBlock layout) ---

		internal bool TryGetIndexRect(int index, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetIndexRect(index, options, out rect);

		internal bool TryGetRangeRect(int start, int end, global::Microsoft.UI.Text.PointOptions options, out global::Windows.Foundation.Rect rect)
			=> _owner.TryGetRangeRect(start, end, options, out rect);

		internal bool TryGetIndexFromPoint(global::Windows.Foundation.Point point, global::Microsoft.UI.Text.PointOptions options, out int index)
			=> _owner.TryGetIndexFromPoint(point, options, out index);

		internal bool TryScrollRangeIntoView(int start, int end)
			=> _owner.TryScrollRangeIntoView(start, end);

		/// <summary>
		/// Sets the text in this document to the specified plain text.
		/// </summary>
		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			// TODO Uno: Honor FormatRtf and the remaining TextSetOptions once rich content is supported.
			var text = value ?? string.Empty;

			// WinUI clamps a programmatic SetText to the control's MaxLength (SetTextAdheresToMaxLength).
			var maxLength = _owner.MaxLength;
			if (maxLength > 0 && text.Length > maxLength)
			{
				text = text.Substring(0, maxLength);
			}

			MutateWithUndo(() =>
			{
				_plainText = text;
				ResetRuns(text.Length);
				ResetParagraphRuns(text.Length);
			});
		}

		/// <summary>
		/// Gets the text in this document as plain text.
		/// </summary>
		public void GetText(global::Microsoft.UI.Text.TextGetOptions options, out string value)
		{
			// TODO Uno: Honor FormatRtf and the remaining TextGetOptions once rich content is supported.
			value = _plainText;
		}

		/// <summary>
		/// Gets or sets the maximum number of actions that can be undone. Setting the limit to 0
		/// disables the undo/redo history.
		/// </summary>
		public uint UndoLimit
		{
			get => (uint)_undoLimit;
			set
			{
				_undoLimit = (int)Math.Min(value, int.MaxValue);
				if (_undoLimit == 0)
				{
					_undoStack.Clear();
					_redoStack.Clear();
				}
				else
				{
					TrimUndoStack();
				}
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the most recent undo action can be redone.
		/// </summary>
		public bool CanRedo() => _redoStack.Count > 0;

		/// <summary>
		/// Gets a value that indicates whether the most recent action can be undone.
		/// </summary>
		public bool CanUndo() => _undoStack.Count > 0;

		/// <summary>
		/// Redoes the most recent undo action.
		/// </summary>
		public void Redo()
		{
			if (_redoStack.Count == 0)
			{
				return;
			}

			var index = _redoStack.Count - 1;
			var next = _redoStack[index];
			_redoStack.RemoveAt(index);
			_undoStack.Add(CaptureSnapshot());
			RestoreSnapshot(next);
			RequestRender();
		}

		/// <summary>
		/// Undoes the most recent action.
		/// </summary>
		public void Undo()
		{
			if (_undoStack.Count == 0)
			{
				return;
			}

			var index = _undoStack.Count - 1;
			var previous = _undoStack[index];
			_undoStack.RemoveAt(index);
			_redoStack.Add(CaptureSnapshot());
			RestoreSnapshot(previous);
			RequestRender();
		}

		/// <summary>
		/// Opens an undo group. Edits made until the matching <see cref="EndUndoGroup"/> are
		/// coalesced into a single undoable action. Groups may be nested.
		/// </summary>
		public void BeginUndoGroup()
		{
			if (_undoGroupDepth == 0)
			{
				_undoGroupSnapshot = CaptureSnapshot();
			}

			_undoGroupDepth++;
		}

		/// <summary>
		/// Closes the undo group opened by <see cref="BeginUndoGroup"/>. When the outermost group
		/// closes, the changes made since it opened are committed as a single undo entry.
		/// </summary>
		public void EndUndoGroup()
		{
			if (_undoGroupDepth == 0)
			{
				// Unbalanced EndUndoGroup: ignore, matching WinUI's tolerance of extra calls.
				return;
			}

			_undoGroupDepth--;
			if (_undoGroupDepth > 0)
			{
				return;
			}

			var groupStart = _undoGroupSnapshot;
			_undoGroupSnapshot = null;
			if (groupStart is null || _undoLimit == 0)
			{
				return;
			}

			if (string.Equals(_plainText, groupStart.Text, StringComparison.Ordinal)
				&& RunsEqual(_runs, groupStart.Runs)
				&& ParagraphRunsEqual(_paragraphRuns, groupStart.ParagraphRuns))
			{
				// The group made no net change; nothing to record.
				return;
			}

			_undoStack.Add(groupStart);
			TrimUndoStack();
			_redoStack.Clear();
		}

		/// <summary>
		/// Pauses rendering of the document until the matching <see cref="ApplyDisplayUpdates"/> is
		/// called. Calls may be nested. Returns the current nesting count.
		/// </summary>
		public int BatchDisplayUpdates() => ++_batchDepth;

		/// <summary>
		/// Resumes rendering paused by <see cref="BatchDisplayUpdates"/>, applying any pending update
		/// once the outermost batch closes. Returns the remaining nesting count.
		/// </summary>
		public int ApplyDisplayUpdates()
		{
			if (_batchDepth > 0)
			{
				_batchDepth--;
			}

			if (_batchDepth == 0 && _pendingRender)
			{
				_pendingRender = false;
				_owner.OnDocumentTextChanged();
			}

			return _batchDepth;
		}
	}
}
