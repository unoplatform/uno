#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox Text Object Model document for Skia.
	//
	// This first increment provides a working plain-text core: SetText/GetText round-trip and drive
	// the owning RichEditBox's shared rendering surface.
	//
	// TODO Uno: The following are subsequent increments and remain [NotImplemented] in the generated
	// stub for now: Selection/GetRange (ITextRange/ITextSelection), character/paragraph default
	// formats, undo/redo history and grouping, batching (Batch/ApplyDisplayUpdates), RTF and stream
	// load/save, and MathML.
	public partial class RichEditTextDocument
	{
		private readonly RichEditBox _owner;
		private string _plainText = string.Empty;

		internal RichEditTextDocument(RichEditBox owner)
		{
			_owner = owner;
		}

		/// <summary>The current plain-text content of the document.</summary>
		internal string PlainText => _plainText;

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
