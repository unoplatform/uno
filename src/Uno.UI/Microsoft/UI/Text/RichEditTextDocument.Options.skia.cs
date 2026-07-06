#nullable enable

using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox document-level option knobs and the
	// clipboard-availability queries for Skia.
	//
	// CanCopy/CanPaste are genuinely functional. The four option knobs (CaretType, DefaultTabStop,
	// AlignmentIncludesTrailingWhitespace, IgnoreTrailingCharacterSpacing) round-trip faithfully but
	// are document-level configuration that the shared single-TextBlock DisplayBlock does not yet act
	// on when rendering (see the class summary in RichEditTextDocument.skia.cs). They are
	// intentionally excluded from undo snapshots — they are settings, not content.
	public partial class RichEditTextDocument
	{
		private global::Microsoft.UI.Text.CaretType _caretType = global::Microsoft.UI.Text.CaretType.Normal;
		private float _defaultTabStop = 36f; // WinUI's default tab stop is 0.5" == 36pt.
		private bool _alignmentIncludesTrailingWhitespace;
		private bool _ignoreTrailingCharacterSpacing;

		/// <summary>Gets or sets the caret type. Round-trips; not yet reflected in rendering.</summary>
		public global::Microsoft.UI.Text.CaretType CaretType
		{
			get => _caretType;
			set => _caretType = value;
		}

		/// <summary>
		/// Gets or sets the default tab stop, in points. Round-trips; not yet reflected in rendering.
		/// </summary>
		public float DefaultTabStop
		{
			get => _defaultTabStop;
			set => _defaultTabStop = value;
		}

		/// <summary>
		/// Gets or sets whether paragraph alignment includes trailing whitespace. Round-trips; not yet
		/// reflected in rendering.
		/// </summary>
		public bool AlignmentIncludesTrailingWhitespace
		{
			get => _alignmentIncludesTrailingWhitespace;
			set => _alignmentIncludesTrailingWhitespace = value;
		}

		/// <summary>
		/// Gets or sets whether trailing character spacing is ignored. Round-trips; not yet reflected
		/// in rendering.
		/// </summary>
		public bool IgnoreTrailingCharacterSpacing
		{
			get => _ignoreTrailingCharacterSpacing;
			set => _ignoreTrailingCharacterSpacing = value;
		}

		/// <summary>
		/// Returns whether the current selection can be copied to the clipboard, i.e. the selection is
		/// non-degenerate.
		/// </summary>
		public bool CanCopy() => Selection.StartPosition != Selection.EndPosition;

		/// <summary>
		/// Returns whether the clipboard currently holds content that can be pasted as text.
		/// </summary>
		public bool CanPaste()
		{
			try
			{
				return Clipboard.GetContent().Contains(StandardDataFormats.Text);
			}
			catch
			{
				// Clipboard access can fail transiently (e.g. locked by another process); treat as
				// "nothing to paste" rather than surfacing the failure.
				return false;
			}
		}

		/// <summary>Clears the undo and redo history and discards any open undo group.</summary>
		public void ClearUndoRedoHistory()
		{
			_undoStack.Clear();
			_redoStack.Clear();
			_undoGroupDepth = 0;
			_undoGroupSnapshot = null;
		}
	}
}
