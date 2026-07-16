#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Core;

namespace Microsoft.UI.Text
{
	// Clipboard primitives for the functional Text Object Model, used by the range-level
	// ITextRange.Copy/Cut/Paste. These live on the document (next to the CanCopy/CanPaste availability
	// queries) so all clipboard access stays in one place, while UnoTextRange owns the range-position
	// mutation.
	//
	// Plain text always goes to the OS clipboard. AllFormats additionally publishes standard RTF so
	// formatting and links survive process boundaries.
	public partial class RichEditTextDocument
	{
		/// <summary>
		/// Copies the plain text spanning <paramref name="start"/>..<paramref name="end"/> to the OS
		/// clipboard. A degenerate (empty) span copies nothing. When the owner's ClipboardCopyFormat is
		/// AllFormats (the default), standard RTF is included for a later paste to restore formatting.
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
			if (_owner.ClipboardCopyFormat != global::Microsoft.UI.Xaml.Controls.RichEditClipboardFormat.PlainText)
			{
				try
				{
					dataPackage.SetRtf(RichTextRtfCodec.Write(CaptureFragment(start, end)));
				}
				catch (ArgumentException)
				{
				}
			}

			Clipboard.SetContent(dataPackage);
		}

		/// <summary>
		/// Reads plain text from the OS clipboard and replaces the current span of
		/// <paramref name="operationRange"/>, invoking <paramref name="onPasted"/> with the caret
		/// position after the inserted text. Unlike WinUI's synchronous RichEdit paste, the OS clipboard
		/// read is asynchronous on Uno, so this completes on a later dispatcher turn (matching the
		/// control-level Ctrl+V paste). When a matching rich payload is present the character formatting
		/// is preserved, as one undoable action.
		/// </summary>
		internal void BeginPasteFromClipboard(UnoTextRange operationRange, Action<int> onPasted, bool requireEditable)
		{
			var content = Clipboard.GetContent();
			_ = _owner.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_ = PasteFromClipboardAsync(content, operationRange, onPasted, requireEditable);
			});
		}

		private async Task PasteFromClipboardAsync(DataPackageView content, UnoTextRange operationRange, Action<int> onPasted, bool requireEditable)
		{
			try
			{
				if (!content.Contains(StandardDataFormats.Rtf) && !content.Contains(StandardDataFormats.Text))
				{
					return;
				}

				RichTextFragment? fragment = null;
				string? clipboardText = null;
				if (content.Contains(StandardDataFormats.Rtf))
				{
					try
					{
						fragment = RichTextRtfCodec.Read(
							await content.GetRtfAsync(),
							GetImportCharacterLimit(operationRange.StartPosition, operationRange.EndPosition));
					}
					catch (Exception error) when (error is InvalidOperationException or InvalidCastException or ArgumentException)
					{
					}
				}

				if (content.Contains(StandardDataFormats.Text))
				{
					try
					{
						clipboardText = await content.GetTextAsync();
					}
					catch (Exception error) when (error is InvalidOperationException or InvalidCastException)
					{
					}
				}

				if (fragment is null && string.IsNullOrEmpty(clipboardText))
				{
					return;
				}

				if (requireEditable && IsOwnerReadOnly)
				{
					return;
				}

				// RichEditBox is multiline and normalizes newlines to \r like WinUI.
				var start = operationRange.StartPosition;
				var end = operationRange.EndPosition;
				if (IsRangeProtected(start, end, operationRange.UsesForwardCharacterFormatting))
				{
					return;
				}

				BeginUndoGroup();
				BatchDisplayUpdates();
				try
				{
					int insertedLength;
					if (fragment is not null && _owner.CharacterCasing == global::Microsoft.UI.Xaml.Controls.CharacterCasing.Normal)
					{
						insertedLength = ReplaceRangeWithFragment(start, end, fragment, operationRange);
					}
					else
					{
						var sourceText = clipboardText ?? fragment!.Text;
						var normalized = _owner.CoerceCasing(sourceText.Replace("\r\n", "\r").Replace('\n', '\r'));
						insertedLength = ReplaceRange(start, end, normalized, operationRange);
					}

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
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("RichEditBox TOM paste failed.", error);
				}
			}
		}
	}
}
