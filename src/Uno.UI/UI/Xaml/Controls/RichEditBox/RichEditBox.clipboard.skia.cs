#nullable enable

using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls
{
	// Clipboard (Copy/Cut/Paste) for the functional RichEditBox on Skia.
	//
	// Mirrors TextBox's clipboard behavior against the interactive selection (_selection). Copy/Cut are
	// synchronous; Paste is dispatched because the OS clipboard read (DataPackageView.GetTextAsync) is
	// async on Uno — this matches TextBox.PasteFromClipboard. All mutations flow through
	// Document.ReplaceRange so the character-format run model is preserved and undo is recorded.
	//
	// TODO Uno: rich (RTF) clipboard payloads and the TOM-level ITextRange.Copy/Cut/Paste(format) are
	// separate follow-ups; this slice provides the plain-text control-level clipboard users invoke via
	// Ctrl+C/X/V.
	partial class RichEditBox
	{
		/// <summary>
		/// Copies the current selection to the OS clipboard as plain text. When there is a non-empty
		/// selection, raises <see cref="CopyingToClipboard"/> first (a handler may suppress the default
		/// copy). An empty selection is a no-op and raises no event — matching CutSelectionToClipboard
		/// and TextBox.CopySelectionToClipboard.
		/// </summary>
		internal void CopySelectionToClipboard()
		{
			var text = GetPlainTextContent();
			var start = Math.Clamp(_selection.start, 0, text.Length);
			var length = Math.Clamp(_selection.length, 0, text.Length - start);
			if (length <= 0)
			{
				return;
			}

			if (RaiseCopyingToClipboardIsHandled())
			{
				return;
			}

			CopySelectionToClipboardCore();
		}

		private void CopySelectionToClipboardCore()
		{
			var text = GetPlainTextContent();
			var start = Math.Clamp(_selection.start, 0, text.Length);
			var length = Math.Clamp(_selection.length, 0, text.Length - start);
			if (length <= 0)
			{
				return;
			}

			// Routes through the document so plain text goes to the OS clipboard and, when
			// ClipboardCopyFormat is AllFormats, the selection's character formatting is stashed
			// for a matching paste to restore.
			Document.CopyToClipboard(start, start + length);
		}

		/// <summary>
		/// Moves the current selection to the OS clipboard and removes it from the document. Raises
		/// <see cref="CuttingToClipboard"/> first; a handler may suppress the default cut.
		/// </summary>
		internal void CutSelectionToClipboard()
		{
			if (IsReadOnly)
			{
				return;
			}

			var text = GetPlainTextContent();
			var start = Math.Clamp(_selection.start, 0, text.Length);
			var length = Math.Clamp(_selection.length, 0, text.Length - start);
			if (length <= 0)
			{
				return;
			}

			if (RaiseCuttingToClipboardIsHandled())
			{
				return;
			}

			// Raw copy (does not re-raise CopyingToClipboard — WinUI raises CuttingToClipboard for a cut).
			CopySelectionToClipboardCore();
			Document.ReplaceRange(start, start + length, string.Empty);
			SetInteractiveSelection(start, 0);
		}

		/// <summary>
		/// Pastes plain text from the OS clipboard, replacing the current selection. Raises
		/// <see cref="Paste"/> first; a handler may suppress the default paste.
		/// </summary>
		internal void PasteFromClipboard()
		{
			if (IsReadOnly)
			{
				return;
			}

			if (RaisePasteIsHandled())
			{
				return;
			}

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				var content = Clipboard.GetContent();
				if (!content.AvailableFormats.Contains(StandardDataFormats.Text))
				{
					return;
				}

				try
				{
					var clipboardText = await content.GetTextAsync();
					PasteTextInteractive(clipboardText);
				}
				catch (InvalidOperationException)
				{
					// The clipboard content may have changed or become unavailable; ignore like TextBox.
				}
			});
		}

		private void PasteTextInteractive(string? clipboardText)
		{
			if (IsReadOnly || string.IsNullOrEmpty(clipboardText))
			{
				return;
			}

			// RichEditBox is multiline and normalizes newlines to \r like WinUI.
			var normalized = clipboardText.Replace("\r\n", "\r").Replace('\n', '\r');

			var text = GetPlainTextContent();
			var start = Math.Clamp(_selection.start, 0, text.Length);
			var length = Math.Clamp(_selection.length, 0, text.Length - start);

			// Route pasted text through CharacterCasing, then clamp to MaxLength (accounting for the
			// selection being replaced) so a paste can't push the content past the limit.
			normalized = ClampInsertToMaxLength(CoerceCasing(normalized), text.Length, start, start + length);
			if (normalized.Length == 0 && length == 0)
			{
				return;
			}

			// Insert plus restore-formatting are one undoable action. Rich formatting is only re-applied
			// when the inserted text matches the copied text exactly, so casing/MaxLength changes above
			// (which alter the text) naturally fall back to a plain paste.
			Document.BeginUndoGroup();
			try
			{
				Document.ReplaceRange(start, start + length, normalized);
				Document.TryApplyRichClipboard(start, normalized);
			}
			finally
			{
				Document.EndUndoGroup();
			}

			SetInteractiveSelection(start + normalized.Length, 0);
		}
	}
}
