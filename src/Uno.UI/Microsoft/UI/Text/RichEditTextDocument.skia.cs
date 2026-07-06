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
	// UnoTextRange.CharacterFormat), and a snapshot-based undo/redo stack over both text and formatting
	// (CanUndo/CanRedo/Undo/Redo/UndoLimit).
	//
	// TODO Uno: The following are subsequent increments and remain [NotImplemented] in the generated
	// stub for now: character/paragraph default formats, paragraph formatting, undo grouping/batching
	// (Batch/ApplyDisplayUpdates), RTF and stream load/save, embedded images and MathML.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private readonly List<Snapshot> _undoStack = new();
		private readonly List<Snapshot> _redoStack = new();
		private string _plainText = string.Empty;
		private UnoTextSelection? _selection;
		private int _undoLimit = 100;

		internal RichEditTextDocument(RichEditBox owner)
		{
			_owner = owner;
		}

		// A point-in-time copy of the document used for undo/redo: the plain text plus a deep clone of
		// the formatting runs.
		private sealed record Snapshot(string Text, List<FormatRun> Runs);

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
				SpliceRuns(start, end - start, insert.Length);
				_plainText = text.Substring(0, start) + insert + text.Substring(end);
			});
		}

		/// <summary>
		/// Runs a buffer/formatting mutation, capturing a before-snapshot for undo (unless nothing
		/// changed or undo is disabled), clearing the redo stack and re-rendering the owning control.
		/// </summary>
		private void MutateWithUndo(Action mutate)
		{
			var before = CaptureSnapshot();
			mutate();

			if (string.Equals(_plainText, before.Text, StringComparison.Ordinal) && RunsEqual(_runs, before.Runs))
			{
				return;
			}

			if (_undoLimit != 0)
			{
				_undoStack.Add(before);
				TrimUndoStack();
				_redoStack.Clear();
			}

			_owner.OnDocumentTextChanged();
		}

		private Snapshot CaptureSnapshot() => new(_plainText, CloneRuns(_runs));

		private void RestoreSnapshot(Snapshot snapshot)
		{
			_plainText = snapshot.Text;
			_runs = CloneRuns(snapshot.Runs);
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
		/// Gets the current text selection as an <see cref="ITextSelection"/>.
		/// </summary>
		// TODO Uno: This is a programmatic selection over the plain-text buffer; it is not yet wired to
		// an interactive caret/selection (which arrives with the shared editing engine extraction).
		public global::Microsoft.UI.Text.ITextSelection Selection => _selection ??= new UnoTextSelection(this);

		/// <summary>
		/// Mirrors the owning control's interactive caret/selection into <see cref="Selection"/> without
		/// triggering the drag semantics of the public position setters. Used by the interactive editor.
		/// </summary>
		internal void SetSelectionRangeInternal(int start, int end)
			=> ((UnoTextRange)Selection).SetRangeInternal(start, end);

		/// <summary>
		/// Sets the text in this document to the specified plain text.
		/// </summary>
		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			// TODO Uno: Honor FormatRtf and the remaining TextSetOptions once rich content is supported.
			var text = value ?? string.Empty;
			MutateWithUndo(() =>
			{
				_plainText = text;
				ResetRuns(text.Length);
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
			_owner.OnDocumentTextChanged();
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
			_owner.OnDocumentTextChanged();
		}
	}
}
