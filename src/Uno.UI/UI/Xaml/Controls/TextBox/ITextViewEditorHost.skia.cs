#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Narrow contract the shared <see cref="TextViewEditor"/> uses to read and mutate the hosting
	/// control's editing state, so the keyboard navigation/edit logic can be shared between
	/// <see cref="TextBox"/> and RichEditBox without duplicating it.
	/// </summary>
	internal interface ITextViewEditorHost
	{
		/// <summary>The shared render companion whose DisplayBlock geometry drives caret navigation.</summary>
		TextBoxView TextBoxView { get; }

		/// <summary>The current plain-text content being edited.</summary>
		string Text { get; }

		/// <summary>Whether a pointer is currently captured (editing is suppressed while true).</summary>
		bool HasPointerCapture { get; }

		/// <summary>The logical horizontal caret offset preserved across vertical (up/down) navigation.</summary>
		float CaretXOffset { get; }

		/// <summary>Starts or ends the current "typing run" used for undo grouping.</summary>
		void TrySetCurrentlyTyping(bool value);

		/// <summary>Records a whole-text replacement in the host's undo history.</summary>
		void CommitReplace(string oldText, string newText, int caret);

		/// <summary>Records a deletion in the host's undo history.</summary>
		void CommitDelete(string oldText, string newText, int selectionStart, int selectionLength);
	}
}
