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
	// TODO Uno: Emit and consume a standard RTF clipboard payload so rich formatting survives outside
	// the current process.
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
			if (!TryGetInteractiveSelectionSpan(out _, out _))
			{
				return;
			}

			if (RaiseCopyingToClipboardIsHandled())
			{
				return;
			}

			if (TryGetInteractiveSelectionSpan(out var start, out var end))
			{
				CopySelectionToClipboardCore(start, end);
			}
		}

		private bool TryGetInteractiveSelectionSpan(out int start, out int end)
		{
			var text = GetPlainTextContent();
			start = Math.Clamp(_selection.start, 0, text.Length);
			var length = Math.Clamp(_selection.length, 0, text.Length - start);
			end = start + length;
			return length > 0;
		}

		private void CopySelectionToClipboardCore(int start, int end)
		{
			// Routes through the document so plain text goes to the OS clipboard and, when
			// ClipboardCopyFormat is AllFormats, the selection's character formatting is stashed
			// for a matching paste to restore.
			Document.CopyToClipboard(start, end);
		}

		internal void CopyTomSelectionToClipboard(global::Microsoft.UI.Text.UnoTextSelection selection)
		{
			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(selection.StartPosition, 0, textLength);
			var end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start == end || RaiseCopyingToClipboardIsHandled())
			{
				return;
			}

			textLength = GetPlainTextContent().Length;
			start = Math.Clamp(selection.StartPosition, 0, textLength);
			end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start != end)
			{
				Document.CopyToClipboard(start, end);
			}
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

			if (!TryGetInteractiveSelectionSpan(out _, out _))
			{
				return;
			}

			if (RaiseCuttingToClipboardIsHandled())
			{
				return;
			}

			if (!TryGetInteractiveSelectionSpan(out var start, out var end))
			{
				return;
			}

			// Raw copy (does not re-raise CopyingToClipboard — WinUI raises CuttingToClipboard for a cut).
			CopySelectionToClipboardCore(start, end);
			RunWithDeferredSelectionSync(() => Document.ReplaceRange(start, end, string.Empty));
			SetInteractiveSelection(start, 0);
		}

		internal void CutTomSelectionToClipboard(global::Microsoft.UI.Text.UnoTextSelection selection)
		{
			if (IsReadOnly)
			{
				return;
			}

			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(selection.StartPosition, 0, textLength);
			var end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start == end || RaiseCuttingToClipboardIsHandled())
			{
				return;
			}

			textLength = GetPlainTextContent().Length;
			start = Math.Clamp(selection.StartPosition, 0, textLength);
			end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start == end)
			{
				return;
			}

			Document.CopyToClipboard(start, end);
			Document.ReplaceRange(start, end, string.Empty, selection);
			selection.SetRangeAfterTextMutation(start, start);
		}

		internal bool TryBeginTomSelectionPaste() => !IsReadOnly && !RaisePasteIsHandled();

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

				string clipboardText;
				try
				{
					clipboardText = await content.GetTextAsync();
				}
				catch (InvalidOperationException)
				{
					// The clipboard content may have changed or become unavailable; ignore like TextBox.
					return;
				}

				PasteTextInteractive(clipboardText);
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
			RunWithDeferredSelectionSync(() =>
			{
				Document.BeginUndoGroup();
				Document.BatchDisplayUpdates();
				try
				{
					Document.ReplaceRange(start, start + length, normalized);
					Document.TryApplyRichClipboard(start, normalized);
				}
				finally
				{
					try
					{
						Document.EndUndoGroup();
					}
					finally
					{
						Document.ApplyDisplayUpdates();
					}
				}
			});

			SetInteractiveSelection(start + normalized.Length, 0);
		}
	}
}
