#nullable enable

using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;

namespace Microsoft.UI.Text
{
	// Clipboard primitives for the functional Text Object Model, used by the range-level
	// ITextRange.Copy/Cut/Paste. These live on the document (next to the CanCopy/CanPaste availability
	// queries) so all clipboard access stays in one place, while UnoTextRange owns the range-position
	// mutation.
	//
	// Plain text always goes to the OS clipboard. Character formatting is preserved via an app-private,
	// in-process payload (see SerializeFormatRuns): a default copy (ClipboardCopyFormat.AllFormats)
	// stashes the copied span's format runs and the next matching paste re-applies them, while
	// ClipboardCopyFormat.PlainText clears the stash so formatting is dropped. This round-trips within
	// the app (across RichEditBox instances); a cross-process RTF payload is a documented follow-up.
	public partial class RichEditTextDocument
	{
		// In-process rich payload shared across all RichEditBox instances: the exact plain text that was
		// copied and the serialized format runs for it. A paste re-applies the runs only when the inserted
		// text matches _richClipboardText exactly, so it never applies stale formatting to unrelated text.
		private static string? _richClipboardText;
		private static string? _richClipboardRuns;

		/// <summary>
		/// Copies the plain text spanning <paramref name="start"/>..<paramref name="end"/> to the OS
		/// clipboard. A degenerate (empty) span copies nothing. When the owner's ClipboardCopyFormat is
		/// AllFormats (the default) the span's character formatting is stashed for a later paste to
		/// re-apply; PlainText drops (clears) it.
		/// </summary>
		internal void CopyToClipboard(int start, int end)
		{
			var text = GetTextInRange(start, end);
			if (text.Length == 0)
			{
				return;
			}

			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			Clipboard.SetContent(dataPackage);

			if (_owner.ClipboardCopyFormat == global::Microsoft.UI.Xaml.Controls.RichEditClipboardFormat.PlainText)
			{
				_richClipboardText = null;
				_richClipboardRuns = null;
			}
			else
			{
				_richClipboardText = text;
				_richClipboardRuns = SerializeFormatRuns(start, end);
			}
		}

		/// <summary>
		/// After a plain-text paste has inserted <paramref name="insertedText"/> at <paramref name="start"/>,
		/// re-applies the stashed character formatting when the inserted text matches the copied text
		/// exactly. Returns whether formatting was applied. Callers wrap this and the preceding
		/// ReplaceRange in a single undo group so a rich paste is one undoable action.
		/// </summary>
		internal bool TryApplyRichClipboard(int start, string insertedText)
		{
			if (_richClipboardRuns is null || _richClipboardText is null)
			{
				return false;
			}

			if (!string.Equals(_richClipboardText, insertedText, StringComparison.Ordinal))
			{
				return false;
			}

			return ApplySerializedFormatRuns(start, _richClipboardRuns, insertedText.Length);
		}

		/// <summary>
		/// Reads plain text from the OS clipboard and replaces the current span of
		/// <paramref name="operationRange"/>, invoking <paramref name="onPasted"/> with the caret
		/// position after the inserted text. Unlike WinUI's synchronous RichEdit paste, the OS clipboard
		/// read is asynchronous on Uno, so this completes on a later dispatcher turn (matching the
		/// control-level Ctrl+V paste). When a matching rich payload is present the character formatting
		/// is preserved, as one undoable action.
		/// </summary>
		internal void BeginPastePlainText(UnoTextRange operationRange, Action<int> onPasted, bool requireEditable)
		{
			_ = _owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
			{
				var content = Clipboard.GetContent();
				if (!content.Contains(StandardDataFormats.Text))
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

				if (string.IsNullOrEmpty(clipboardText))
				{
					return;
				}

				if (requireEditable && IsOwnerReadOnly)
				{
					return;
				}

				// RichEditBox is multiline and normalizes newlines to \r like WinUI.
				var normalized = _owner.CoerceCasing(clipboardText.Replace("\r\n", "\r").Replace('\n', '\r'));
				var start = operationRange.StartPosition;
				var end = operationRange.EndPosition;

				BeginUndoGroup();
				BatchDisplayUpdates();
				try
				{
					var insertedLength = ReplaceRange(start, end, normalized, operationRange);
					var insertedText = normalized.Substring(0, insertedLength);
					TryApplyRichClipboard(start, insertedText);
					onPasted(start + insertedLength);
				}
				finally
				{
					try
					{
						EndUndoGroup();
					}
					finally
					{
						ApplyDisplayUpdates();
					}
				}
			});
		}
	}
}
