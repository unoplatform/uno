#nullable enable

using System;
using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Text
{
	// Uno-specific functional implementation of the RichEditBox document-level option knobs and the
	// clipboard-availability queries for Skia.
	//
	// CanCopy/CanPaste are genuinely functional. The remaining options drive caret or shared text
	// layout behavior and are intentionally excluded from undo snapshots.
	public partial class RichEditTextDocument
	{
		private global::Microsoft.UI.Text.CaretType _caretType = global::Microsoft.UI.Text.CaretType.Normal;
		private float _defaultTabStop = 36f; // WinUI's default tab stop is 0.5" == 36pt.
		private bool _alignmentIncludesTrailingWhitespace;
		private bool _ignoreTrailingCharacterSpacing;

		/// <summary>Gets or sets the caret type.</summary>
		public global::Microsoft.UI.Text.CaretType CaretType
		{
			get => _caretType;
			set
			{
				if (!Enum.IsDefined(value))
				{
					throw new ArgumentException("The caret type is not defined.", nameof(value));
				}

				if (_caretType != value)
				{
					_caretType = value;
					_owner.OnDocumentCaretTypeChanged();
				}
			}
		}

		/// <summary>
		/// Gets or sets the default tab stop, in points.
		/// </summary>
		public float DefaultTabStop
		{
			get => _defaultTabStop;
			set
			{
				if (!_defaultTabStop.Equals(value))
				{
					_defaultTabStop = value;
					RequestRender(isContentChanging: false);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether paragraph alignment includes trailing whitespace.
		/// </summary>
		public bool AlignmentIncludesTrailingWhitespace
		{
			get => _alignmentIncludesTrailingWhitespace;
			set
			{
				if (_alignmentIncludesTrailingWhitespace != value)
				{
					_alignmentIncludesTrailingWhitespace = value;
					RequestRender(isContentChanging: false);
				}
			}
		}

		/// <summary>
		/// Gets or sets whether trailing character spacing is ignored.
		/// </summary>
		public bool IgnoreTrailingCharacterSpacing
		{
			get => _ignoreTrailingCharacterSpacing;
			set
			{
				if (_ignoreTrailingCharacterSpacing != value)
				{
					_ignoreTrailingCharacterSpacing = value;
					RequestRender(isContentChanging: false);
				}
			}
		}

		/// <summary>
		/// Returns whether the current selection can be copied to the clipboard, i.e. the selection is
		/// non-degenerate.
		/// </summary>
		public bool CanCopy() => Selection.StartPosition != Selection.EndPosition;

		/// <summary>
		/// Returns whether the clipboard currently holds text or RTF content that can be pasted.
		/// </summary>
		public bool CanPaste()
		{
			try
			{
				var content = Clipboard.GetContent();
				return content.Contains(StandardDataFormats.Rtf) || content.Contains(StandardDataFormats.Text);
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
			_undoGroupTextEdits = null;
		}
	}
}
