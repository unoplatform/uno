#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox Text Object Model document for Skia.
	//
	// This increment provides a working plain-text core: SetText/GetText round-trip, a functional
	// Text Object Model surface (GetRange/Selection returning UnoTextRange/UnoTextSelection) that
	// navigates and edits the plain-text buffer and drives the owning RichEditBox's shared rendering,
	// and a functional snapshot-based undo/redo stack (CanUndo/CanRedo/Undo/Redo/UndoLimit).
	//
	// TODO Uno: The following are subsequent increments and remain [NotImplemented] in the generated
	// stub for now: character/paragraph default formats, undo grouping/batching (Batch/
	// ApplyDisplayUpdates), RTF and stream load/save, embedded images and MathML. Rich runs and
	// character/paragraph formatting on ranges also arrive with the rich-content model.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private readonly List<string> _undoStack = new();
		private readonly List<string> _redoStack = new();
		private string _plainText = string.Empty;
		private UnoTextSelection? _selection;
		private int _undoLimit = 100;

		internal RichEditTextDocument(RichEditBox owner)
		{
			_owner = owner;
		}

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
		/// <paramref name="replacement"/> and re-renders. Used by <see cref="UnoTextRange"/> editing.
		/// </summary>
		internal void ReplaceRange(int start, int end, string replacement)
		{
			var text = _plainText;
			start = Math.Clamp(start, 0, text.Length);
			end = Math.Clamp(end, start, text.Length);
			var newText = text.Substring(0, start) + (replacement ?? string.Empty) + text.Substring(end);
			RecordAndApply(newText);
		}

		/// <summary>
		/// Applies <paramref name="newText"/> as the buffer content, recording an undo entry (unless the
		/// content is unchanged or undo is disabled) and re-rendering the owning control.
		/// </summary>
		private void RecordAndApply(string newText)
		{
			if (string.Equals(newText, _plainText, StringComparison.Ordinal))
			{
				return;
			}

			if (_undoLimit != 0)
			{
				_undoStack.Add(_plainText);
				TrimUndoStack();
				_redoStack.Clear();
			}

			_plainText = newText;
			_owner.OnDocumentTextChanged();
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
		/// Sets the text in this document to the specified plain text.
		/// </summary>
		public void SetText(global::Microsoft.UI.Text.TextSetOptions options, string value)
		{
			// TODO Uno: Honor FormatRtf and the remaining TextSetOptions once rich content is supported.
			RecordAndApply(value ?? string.Empty);
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
			_undoStack.Add(_plainText);
			_plainText = next;
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
			_redoStack.Add(_plainText);
			_plainText = previous;
			_owner.OnDocumentTextChanged();
		}
	}
}
