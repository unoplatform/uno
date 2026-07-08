#nullable enable

using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;

namespace Microsoft.UI.Text
{
	// Plain-text clipboard primitives for the functional Text Object Model, used by the range-level
	// ITextRange.Copy/Cut/Paste. These live on the document (next to the CanCopy/CanPaste availability
	// queries) so all clipboard access stays in one place, while UnoTextRange owns the range-position
	// mutation.
	//
	// TODO Uno: rich/RTF clipboard payloads are a follow-up — only the plain-text format is honored.
	public partial class RichEditTextDocument
	{
		/// <summary>
		/// Copies the plain text spanning <paramref name="start"/>..<paramref name="end"/> to the OS
		/// clipboard. A degenerate (empty) span copies nothing.
		/// </summary>
		internal void CopyPlainTextToClipboard(int start, int end)
		{
			var text = GetTextInRange(start, end);
			if (text.Length == 0)
			{
				return;
			}

			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);
		}

		/// <summary>
		/// Reads plain text from the OS clipboard and replaces the <paramref name="start"/>..
		/// <paramref name="end"/> span with it, invoking <paramref name="onPasted"/> with the caret
		/// position after the inserted text. Unlike WinUI's synchronous RichEdit paste, the OS clipboard
		/// read is asynchronous on Uno, so this completes on a later dispatcher turn (matching the
		/// control-level Ctrl+V paste).
		/// </summary>
		internal void BeginPastePlainText(int start, int end, Action<int> onPasted)
		{
			_ = _owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				var content = Clipboard.GetContent();
				if (!content.Contains(StandardDataFormats.Text))
				{
					return;
				}

				try
				{
					var clipboardText = await content.GetTextAsync();
					if (string.IsNullOrEmpty(clipboardText))
					{
						return;
					}

					// RichEditBox is multiline and normalizes newlines to \r like WinUI.
					var normalized = clipboardText.Replace("\r\n", "\r").Replace('\n', '\r');
					ReplaceRange(start, end, normalized);
					onPasted(start + normalized.Length);
				}
				catch (InvalidOperationException)
				{
					// The clipboard content may have changed or become unavailable; ignore like TextBox.
				}
			});
		}
	}
}
