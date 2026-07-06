#nullable enable

using System;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox Text Object Model document for Skia.
	//
	// This increment provides a working plain-text core: SetText/GetText round-trip, plus a functional
	// Text Object Model surface (GetRange/Selection returning UnoTextRange/UnoTextSelection) that
	// navigates and edits the plain-text buffer and drives the owning RichEditBox's shared rendering.
	//
	// TODO Uno: The following are subsequent increments and remain [NotImplemented] in the generated
	// stub for now: character/paragraph default formats, undo/redo history and grouping, batching
	// (Batch/ApplyDisplayUpdates), RTF and stream load/save, embedded images and MathML. Rich runs and
	// character/paragraph formatting on ranges also arrive with the rich-content model.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private string _plainText = string.Empty;
		private UnoTextSelection? _selection;

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
			_plainText = text.Substring(0, start) + (replacement ?? string.Empty) + text.Substring(end);
			_owner.OnDocumentTextChanged();
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
			_plainText = value ?? string.Empty;
			_owner.OnDocumentTextChanged();
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
		/// Gets a value that indicates whether the most recent undo action can be redone.
		/// </summary>
		// TODO Uno: Wire to the shared editing history once the engine is extracted.
		public bool CanRedo() => false;

		/// <summary>
		/// Gets a value that indicates whether the most recent action can be undone.
		/// </summary>
		// TODO Uno: Wire to the shared editing history once the engine is extracted.
		public bool CanUndo() => false;

		/// <summary>
		/// Redoes the most recent undo action.
		/// </summary>
		// TODO Uno: Wire to the shared editing history once the engine is extracted.
		public void Redo()
		{
		}

		/// <summary>
		/// Undoes the most recent undo action.
		/// </summary>
		// TODO Uno: Wire to the shared editing history once the engine is extracted.
		public void Undo()
		{
		}
	}
}
