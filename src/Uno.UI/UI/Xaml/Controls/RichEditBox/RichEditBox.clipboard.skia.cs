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
		/// Copies the current selection to the OS clipboard as plain text. Raises
		/// <see cref="CopyingToClipboard"/> first; a handler may suppress the default copy.
		/// </summary>
		internal void CopySelectionToClipboard()
		{
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

			var dataPackage = new DataPackage();
			dataPackage.SetText(text.Substring(start, length));
			Clipboard.SetContent(dataPackage);
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

			Document.ReplaceRange(start, start + length, normalized);
			SetInteractiveSelection(start + normalized.Length, 0);
		}
	}
}
