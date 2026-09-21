#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Xaml.Controls
{
	// Clipboard mutations flow through the document so formatting, tracked ranges, and undo stay aligned.
	partial class RichEditBox
	{
		private long _nativeClipboardOperationVersion;
		private WeakReference<NativeClipboardOperation>? _pendingNativeClipboardOperation;

		internal sealed class NativeClipboardOperation
		{
			internal NativeClipboardOperation(RichEditBox owner, XamlRoot root, bool isCut, string text, string? rtf)
			{
				Owner = owner;
				Root = root;
				Document = owner.Document;
				IsCut = isCut;
				Text = text;
				Rtf = rtf;
				Selection = owner._selection;
				TextVersion = Document.TextVersion;
				SelectionVersion = Document.SelectionChangeVersion;
				CharacterFormatVersion = Document.CharacterFormatVersion;
				ParagraphFormatVersion = Document.ParagraphFormatVersion;
				AutomationVersion = Document.AutomationVersion;
				CopyFormat = owner.ClipboardCopyFormat;
			}

			internal string Text { get; }
			internal string? Rtf { get; }
			internal RichEditBox Owner { get; }
			internal XamlRoot Root { get; }
			internal global::Microsoft.UI.Text.RichEditTextDocument Document { get; }
			internal bool IsCut { get; }
			internal (int start, int length, bool selectionEndsAtTheStart) Selection { get; }
			internal long TextVersion { get; }
			internal long SelectionVersion { get; }
			internal long CharacterFormatVersion { get; }
			internal long ParagraphFormatVersion { get; }
			internal long AutomationVersion { get; }
			internal RichEditClipboardFormat CopyFormat { get; }
		}

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
			var textLength = GetPlainTextLength();
			start = Math.Clamp(_selection.start, 0, textLength);
			var length = Math.Clamp(_selection.length, 0, textLength - start);
			end = start + length;
			return length > 0;
		}

		private void CopySelectionToClipboardCore(int start, int end)
		{
			// AllFormats also publishes standard RTF through the document's clipboard package.
			Document.CopyToClipboard(start, end);
		}

		/// <summary>Captures a native clipboard payload without changing the document or writing to the OS clipboard.</summary>
		internal NativeClipboardOperation? PrepareNativeClipboard(bool isCut)
		{
			InvalidateNativeClipboardOperation();
			var operationVersion = _nativeClipboardOperationVersion;
			if (XamlRoot is not { } root
				|| !CanUseNativeClipboard(root)
				|| isCut && IsReadOnly
				|| !TryGetInteractiveSelectionSpan(out _, out _))
			{
				return null;
			}

			if (isCut ? RaiseCuttingToClipboardIsHandled() : RaiseCopyingToClipboardIsHandled())
			{
				return null;
			}

			if (operationVersion != _nativeClipboardOperationVersion
				|| !CanUseNativeClipboard(root)
				|| isCut && IsReadOnly
				|| !TryGetInteractiveSelectionSpan(out var start, out var end)
				|| isCut && Document.IsRangeProtected(start, end))
			{
				return null;
			}

			var package = Document.CreateClipboardDataPackage(start, end);
			if (package is null)
			{
				return null;
			}
			var text = Document.GetTextInRange(start, end);
			// Reuse the managed copy path's format/size eligibility; native gestures need its
			// snapshot serialized synchronously rather than a deferred DataPackage provider.
			var rtf = package.GetView().Contains(StandardDataFormats.Rtf)
				? global::Microsoft.UI.Text.RichTextRtfCodec.Write(Document.CaptureFragment(start, end))
				: null;
			var operation = new NativeClipboardOperation(this, root, isCut, text, rtf);
			_pendingNativeClipboardOperation = new WeakReference<NativeClipboardOperation>(operation);
			return operation;
		}

		/// <summary>Completes a prepared operation after the native clipboard accepted every advertised format.</summary>
		internal bool CommitNativeClipboard(NativeClipboardOperation operation)
		{
			if (operation is null
				|| _pendingNativeClipboardOperation is not { } pendingReference
				|| !pendingReference.TryGetTarget(out var pending)
				|| !ReferenceEquals(pending, operation))
			{
				return false;
			}

			InvalidateNativeClipboardOperation();
			if (!ReferenceEquals(operation.Owner, this)
				|| !ReferenceEquals(operation.Document, Document)
				|| !CanUseNativeClipboard(operation.Root)
				|| operation.IsCut && IsReadOnly
				|| operation.TextVersion != Document.TextVersion
				|| operation.SelectionVersion != Document.SelectionChangeVersion
				|| operation.CharacterFormatVersion != Document.CharacterFormatVersion
				|| operation.ParagraphFormatVersion != Document.ParagraphFormatVersion
				|| operation.AutomationVersion != Document.AutomationVersion
				|| operation.CopyFormat != ClipboardCopyFormat
				|| operation.Selection != _selection
				|| !TryGetInteractiveSelectionSpan(out var start, out var end)
				|| operation.IsCut && Document.IsRangeProtected(start, end))
			{
				return false;
			}

			if (operation.IsCut)
			{
				DeleteSelectionForCut(start, end);
			}
			return true;
		}

		private bool CanUseNativeClipboard(XamlRoot root)
			=> IsLoaded
				&& IsEnabled
				&& ReferenceEquals(XamlRoot, root)
				&& ReferenceEquals(FocusManager.GetFocusedElement(root), this)
				&& _selectionSyncDeferralDepth == 0
				&& !_isProcessingSelectionChanging;

		private void InvalidateNativeClipboardOperation()
		{
			_pendingNativeClipboardOperation = null;
			_nativeClipboardOperationVersion++;
		}

		internal void CopyTomSelectionToClipboard(global::Microsoft.UI.Text.UnoTextSelection selection)
		{
			var textLength = GetPlainTextLength();
			var start = Math.Clamp(selection.StartPosition, 0, textLength);
			var end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start == end || RaiseCopyingToClipboardIsHandled())
			{
				return;
			}

			textLength = GetPlainTextLength();
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
			if (!IsEnabled || IsReadOnly)
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
			DeleteSelectionForCut(start, end);
		}

		private void DeleteSelectionForCut(int start, int end)
		{
			var selectionBeforeMutation = _selection;
			var selectionVersionBeforeMutation = Document.SelectionChangeVersion;
			RunWithDeferredSelectionSync(() => Document.ReplaceRange(start, end, string.Empty));
			if (Document.SelectionChangeVersion == selectionVersionBeforeMutation
				&& _selection == selectionBeforeMutation)
			{
				SetInteractiveSelection(start, 0);
			}
			Document.FinalizeHistorySelection();
		}

		internal void CutTomSelectionToClipboard(global::Microsoft.UI.Text.UnoTextSelection selection)
		{
			if (IsReadOnly)
			{
				return;
			}

			var textLength = GetPlainTextLength();
			var start = Math.Clamp(selection.StartPosition, 0, textLength);
			var end = Math.Clamp(selection.EndPosition, start, textLength);
			if (start == end || RaiseCuttingToClipboardIsHandled())
			{
				return;
			}

			textLength = GetPlainTextLength();
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
			Document.FinalizeHistorySelection();
		}

		internal bool TryBeginTomSelectionPaste() => !IsReadOnly && !RaisePasteIsHandled();

		/// <summary>
		/// Pastes plain text from the OS clipboard, replacing the current selection. Raises
		/// <see cref="Paste"/> first; a handler may suppress the default paste.
		/// </summary>
		internal async void PasteFromClipboard()
		{
			try
			{
				if (!IsEnabled || IsReadOnly)
				{
					return;
				}

				if (RaisePasteIsHandled())
				{
					return;
				}

				var content = Clipboard.GetContent();
				var textLength = GetPlainTextLength();
				var start = Math.Clamp(_selection.start, 0, textLength);
				var end = Math.Clamp(start + _selection.length, start, textLength);
				var operationRange = Document.GetRange(start, end);
				var operation = Document.BeginPasteOperation();
				var retrieval = Document.ReadClipboardContentAsync(
					content,
					operationRange,
					cancellationToken: operation.CancellationToken);
				await PasteFromClipboardAsync(retrieval, operationRange, operation);
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is not null)
			{
				throw;
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error("RichEditBox interactive paste failed.", error);
				}
			}
		}

		/// <summary>
		/// Pastes text supplied by a native input surface. Browser clipboard events already provide the
		/// text synchronously, so routing through the OS clipboard again would lose the user gesture that
		/// grants clipboard access.
		/// </summary>
		internal void PasteFromClipboard(string clipboardText)
		{
			if (!IsEnabled || IsReadOnly || RaisePasteIsHandled())
			{
				return;
			}

			var textLength = GetPlainTextLength();
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(start + _selection.length, start, textLength);
			Document.BeginPasteOperation();
			PasteClipboardContent(fragment: null, clipboardText, Document.GetRange(start, end));
		}

		internal int GetClipboardPasteSourceLimit()
		{
			var textLength = GetPlainTextLength();
			var start = Math.Clamp(_selection.start, 0, textLength);
			var end = Math.Clamp(start + _selection.length, start, textLength);
			var outputLimit = Document.GetClipboardImportCharacterLimit(start, end);
			return outputLimit >= (int.MaxValue - 2) / 2
				? int.MaxValue
				: outputLimit * 2 + 2;
		}

		private async Task PasteFromClipboardAsync(
			Task<(global::Microsoft.UI.Text.RichTextFragment? Fragment, string? Text)> retrieval,
			global::Microsoft.UI.Text.ITextRange operationRange,
			global::Microsoft.UI.Text.RichEditTextDocument.PasteOperation operation)
		{
			var (fragment, clipboardText) = await retrieval;

			await Document.TryCommitLatestPasteAsync(
				operation,
				() => PasteClipboardContent(fragment, clipboardText, operationRange));
		}

		private void PasteClipboardContent(
			global::Microsoft.UI.Text.RichTextFragment? fragment,
			string? clipboardText,
			global::Microsoft.UI.Text.ITextRange operationRange)
		{
			if (!IsEnabled || IsReadOnly || (fragment is null && string.IsNullOrEmpty(clipboardText)))
			{
				return;
			}

			var textLength = GetPlainTextLength();
			var start = Math.Clamp(operationRange.StartPosition, 0, textLength);
			var end = Math.Clamp(operationRange.EndPosition, start, textLength);
			if (Document.IsRangeProtected(start, end))
			{
				return;
			}

			var insertedLength = 0;
			RunWithDeferredSelectionSync(() =>
			{
				if (fragment is not null
					&& (CharacterCasing == CharacterCasing.Normal || global::Microsoft.UI.Text.RichEditTextDocument.IsImageOnlyFragment(fragment)))
				{
					insertedLength = Document.ReplaceRangeWithFragment(start, end, fragment, sourceRange: null);
				}
				else if (!string.IsNullOrEmpty(clipboardText ?? fragment?.Text))
				{
					var sourceText = clipboardText ?? fragment!.Text;
					var normalized = Document.NormalizeImportedPlainText(sourceText, start, end);
					insertedLength = Document.ReplaceRange(start, end, normalized);
				}
			});

			SetInteractiveSelection(start + insertedLength, 0);
			Document.FinalizeHistorySelection();
		}

	}
}
