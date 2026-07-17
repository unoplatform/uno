#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
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
			if (Document.IsRangeProtected(start, end))
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
			if (Document.IsRangeProtected(start, end))
			{
				throw new UnauthorizedAccessException("The text range contains protected text.");
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

			var content = Clipboard.GetContent();
			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(start + _selection.length, start, textLength);
			var operationRange = Document.GetRange(start, end);
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				_ = PasteFromClipboardAsync(content, operationRange);
			});
		}

		/// <summary>
		/// Pastes text supplied by a native input surface. Browser clipboard events already provide the
		/// text synchronously, so routing through the OS clipboard again would lose the user gesture that
		/// grants clipboard access.
		/// </summary>
		internal void PasteFromClipboard(string clipboardText)
		{
			if (IsReadOnly || RaisePasteIsHandled())
			{
				return;
			}

			var textLength = GetPlainTextContent().Length;
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(start + _selection.length, start, textLength);
			PasteClipboardContent(fragment: null, clipboardText, Document.GetRange(start, end));
		}

		private async Task PasteFromClipboardAsync(DataPackageView content, global::Microsoft.UI.Text.ITextRange operationRange)
		{
			try
			{
				if (!content.AvailableFormats.Contains(StandardDataFormats.Rtf)
					&& !content.AvailableFormats.Contains(StandardDataFormats.Text))
				{
					return;
				}

				global::Microsoft.UI.Text.RichTextFragment? fragment = null;
				string? clipboardText = null;
				if (content.AvailableFormats.Contains(StandardDataFormats.Rtf))
				{
					try
					{
						fragment = global::Microsoft.UI.Text.RichTextRtfCodec.Read(
							await content.GetRtfAsync(),
							Document.GetImportCharacterLimit(operationRange.StartPosition, operationRange.EndPosition));
					}
					catch (Exception error) when (error is InvalidOperationException or InvalidCastException or ArgumentException)
					{
					}
				}

				if (content.AvailableFormats.Contains(StandardDataFormats.Text))
				{
					try
					{
						clipboardText = await content.GetTextAsync();
					}
					catch (Exception error) when (error is InvalidOperationException or InvalidCastException)
					{
					}
				}

				PasteClipboardContent(fragment, clipboardText, operationRange);
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("RichEditBox interactive paste failed.", error);
				}
			}
		}

		private void PasteClipboardContent(
			global::Microsoft.UI.Text.RichTextFragment? fragment,
			string? clipboardText,
			global::Microsoft.UI.Text.ITextRange operationRange)
		{
			if (IsReadOnly)
			{
				return;
			}

			var text = GetPlainTextContent();
			var start = Math.Clamp(operationRange.StartPosition, 0, text.Length);
			var end = Math.Clamp(operationRange.EndPosition, start, text.Length);
			if (Document.IsRangeProtected(start, end))
			{
				return;
			}

			var insertedLength = 0;
			RunWithDeferredSelectionSync(() =>
			{
				if (fragment is not null && CharacterCasing == CharacterCasing.Normal)
				{
					insertedLength = Document.ReplaceRangeWithFragment(start, end, fragment, sourceRange: null);
				}
				else if (!string.IsNullOrEmpty(clipboardText ?? fragment?.Text))
				{
					var sourceText = clipboardText ?? fragment!.Text;
					var normalized = CoerceCasing(sourceText.Replace("\r\n", "\r").Replace('\n', '\r'));
					insertedLength = Document.ReplaceRange(start, end, normalized);
				}
			});

			SetInteractiveSelection(start + insertedLength, 0);
		}

	}
}
