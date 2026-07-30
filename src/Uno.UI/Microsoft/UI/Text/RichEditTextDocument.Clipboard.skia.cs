#nullable enable

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.DataTransfer;

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
		private const string RtfWithoutObjectsFormat = "Rich Text Format Without Objects";
		private int _clipboardRtfGenerationCount;
		private long _pasteOperationGeneration;
		private readonly SemaphoreSlim _pasteCommitGate = new(1, 1);
		private CancellationTokenSource? _pasteOperationCancellation;

		internal readonly record struct PasteOperation(long Generation, CancellationToken CancellationToken);

		internal int ClipboardRtfGenerationCount => _clipboardRtfGenerationCount;

		internal void ResetClipboardDiagnosticsForTesting() => _clipboardRtfGenerationCount = 0;

		/// <summary>
		/// Copies the plain text spanning <paramref name="start"/>..<paramref name="end"/> to the OS
		/// clipboard. A degenerate (empty) span copies nothing. When the owner's ClipboardCopyFormat is
		/// AllFormats (the default), standard RTF is included for a later paste to restore formatting.
		/// </summary>
		internal void CopyToClipboard(int start, int end)
		{
			var dataPackage = CreateClipboardDataPackage(start, end);
			if (dataPackage is null)
			{
				return;
			}

			Clipboard.SetContent(dataPackage);
		}

		internal DataPackage? CreateClipboardDataPackage(int start, int end)
		{
			var text = GetTextInRange(start, end);
			if (text.Length == 0)
			{
				return null;
			}

			var dataPackage = new DataPackage();
			dataPackage.SetText(text);
			if (_owner.ClipboardCopyFormat != global::Microsoft.UI.Xaml.Controls.RichEditClipboardFormat.PlainText
				&& CanPossiblyEncodeRtf(start, end))
			{
				var fragment = CaptureFragment(start, end, text);
				dataPackage.SetDataProvider(StandardDataFormats.Rtf, request =>
				{
					try
					{
						_clipboardRtfGenerationCount++;
						request.SetData(RichTextRtfCodec.Write(fragment));
					}
					catch (ArgumentException)
					{
						request.SetData(string.Empty);
					}
				});
			}

			return dataPackage;
		}

		/// <summary>
		/// Reads plain text from the OS clipboard and replaces the current span of
		/// <paramref name="operationRange"/>, invoking <paramref name="onPasted"/> with the caret
		/// position after the inserted text. Unlike WinUI's synchronous RichEdit paste, the OS clipboard
		/// read is asynchronous on Uno, so this completes on a later dispatcher turn (matching the
		/// control-level Ctrl+V paste). When a matching rich payload is present the character formatting
		/// is preserved, as one undoable action.
		/// </summary>
		internal async void BeginPasteFromClipboard(
			UnoTextRange operationRange,
			Action<int> onPasted,
			bool requireEditable,
			int format)
		{
			try
			{
				await BeginPasteFromClipboardCoreAsync(
					Clipboard.GetContent(),
					operationRange,
					onPasted,
					requireEditable,
					format);
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception error) when (FindFatalException(error) is not null)
			{
				throw;
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("RichEditBox TOM paste failed.", error);
				}
			}
		}

		internal async void BeginPasteFromClipboard(
			DataPackageView content,
			UnoTextRange operationRange,
			Action<int> onPasted,
			bool requireEditable,
			int format)
		{
			try
			{
				await BeginPasteFromClipboardCoreAsync(
					content,
					operationRange,
					onPasted,
					requireEditable,
					format);
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception error) when (FindFatalException(error) is not null)
			{
				throw;
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("RichEditBox TOM paste failed.", error);
				}
			}
		}

		private async Task BeginPasteFromClipboardCoreAsync(
			DataPackageView content,
			UnoTextRange operationRange,
			Action<int> onPasted,
			bool requireEditable,
			int format)
		{
			var operation = BeginPasteOperation();
			var retrieval = ReadClipboardContentAsync(content, operationRange, format, operation.CancellationToken);
			await PasteFromClipboardAsync(
				retrieval,
				operationRange,
				onPasted,
				requireEditable,
				operation);
		}

		internal PasteOperation BeginPasteOperation()
		{
			var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			var previous = Interlocked.Exchange(ref _pasteOperationCancellation, cancellation);
			previous?.Cancel();
			previous?.Dispose();
			return new PasteOperation(
				Interlocked.Increment(ref _pasteOperationGeneration),
				cancellation.Token);
		}

		internal async Task<bool> TryCommitLatestPasteAsync(PasteOperation operation, Action commit)
		{
			await _pasteCommitGate.WaitAsync(operation.CancellationToken);
			try
			{
				operation.CancellationToken.ThrowIfCancellationRequested();
				if (operation.Generation != Volatile.Read(ref _pasteOperationGeneration))
				{
					return false;
				}

				commit();
				return true;
			}
			finally
			{
				_pasteCommitGate.Release();
			}
		}

		private async Task PasteFromClipboardAsync(
			Task<(RichTextFragment? Fragment, string? Text)> retrieval,
			UnoTextRange operationRange,
			Action<int> onPasted,
			bool requireEditable,
			PasteOperation operation)
		{
			var (fragment, clipboardText) = await retrieval;

			if (fragment is null && string.IsNullOrEmpty(clipboardText))
			{
				return;
			}

			await TryCommitLatestPasteAsync(operation, () =>
			{
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
					if (fragment is not null
						&& (_owner.CharacterCasing == global::Microsoft.UI.Xaml.Controls.CharacterCasing.Normal || IsImageOnlyFragment(fragment)))
					{
						insertedLength = ReplaceRangeWithFragment(start, end, fragment, operationRange);
					}
					else
					{
						var sourceText = clipboardText ?? fragment!.Text;
						var normalized = NormalizeImportedPlainText(sourceText, start, end);
						insertedLength = ReplaceRange(start, end, normalized, operationRange);
					}

					onPasted(start + insertedLength);
					FinalizeHistorySelection();
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

		internal async Task<(RichTextFragment? Fragment, string? Text)> ReadClipboardContentAsync(
			DataPackageView content,
			global::Microsoft.UI.Text.ITextRange operationRange,
			int format = TomClipboardFormat.Best,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var requested = TomClipboardFormat.Resolve(format);
			if (requested == ClipboardRepresentation.None)
			{
				return (null, null);
			}

			if (requested is ClipboardRepresentation.Best or ClipboardRepresentation.Rtf)
			{
				var fragment = await TryReadClipboardRtfAsync(
					content,
					StandardDataFormats.Rtf,
					operationRange,
					cancellationToken);
				if (fragment is not null)
				{
					return (fragment, null);
				}
			}

			if (requested is ClipboardRepresentation.Best or ClipboardRepresentation.RtfWithoutObjects)
			{
				var fragment = await TryReadClipboardRtfAsync(
					content,
					RtfWithoutObjectsFormat,
					operationRange,
					cancellationToken);
				if (fragment is not null)
				{
					return (fragment, null);
				}
			}

			if ((requested is ClipboardRepresentation.Best or ClipboardRepresentation.Text)
				&& content.Contains(StandardDataFormats.Text))
			{
				try
				{
					return (null, await content.GetTextAsync().AsTask(cancellationToken));
				}
				catch (Exception error)
				{
					LogRecoverableClipboardFailure(StandardDataFormats.Text, error);
					HandleClipboardRepresentationFailure(error);
				}
			}

			if ((requested is ClipboardRepresentation.Best or ClipboardRepresentation.Bitmap)
				&& content.Contains(StandardDataFormats.Bitmap))
			{
				try
				{
					var reference = await content.GetBitmapAsync().AsTask(cancellationToken);
					if (reference is null)
					{
						return (null, null);
					}

					using var stream = await reference.OpenReadAsync().AsTask(cancellationToken);
					cancellationToken.ThrowIfCancellationRequested();
					var image = InlineImageState.CreateFromStream(
						stream,
						width: null,
						height: null,
						ascent: null,
						global::Microsoft.UI.Text.VerticalCharacterAlignment.Baseline,
						alternateText: string.Empty);
					return (
						CreateInlineImageFragment(operationRange.StartPosition, image),
						null);
				}
				catch (Exception error)
				{
					LogRecoverableClipboardFailure(StandardDataFormats.Bitmap, error);
					HandleClipboardRepresentationFailure(error);
				}
			}

			return (null, null);
		}

		private async Task<RichTextFragment?> TryReadClipboardRtfAsync(
			DataPackageView content,
			string format,
			global::Microsoft.UI.Text.ITextRange operationRange,
			CancellationToken cancellationToken)
		{
			if (!content.Contains(format))
			{
				return null;
			}

			try
			{
				var rtf = string.Equals(format, StandardDataFormats.Rtf, StringComparison.Ordinal)
					? await content.GetRtfAsync().AsTask(cancellationToken)
					: await content.GetDataAsync(format).AsTask(cancellationToken) as string
						?? throw new InvalidCastException("The clipboard RTF representation is not text.");
				cancellationToken.ThrowIfCancellationRequested();
				var start = operationRange.StartPosition;
				var end = operationRange.EndPosition;
				return SanitizeClipboardFragment(
					RichTextRtfCodec.Read(
						rtf,
						GetClipboardImportCharacterLimit(start, end),
						ShouldTruncateClipboardImportAtLimit(start, end)));
			}
			catch (Exception error)
			{
				LogRecoverableClipboardFailure(format, error);
				HandleClipboardRepresentationFailure(error);
				return null;
			}
		}

		private void LogRecoverableClipboardFailure(string format, Exception error)
		{
			if (IsRecoverableClipboardRepresentationFailure(error)
				&& FindException<UnauthorizedAccessException>(error) is null
				&& FindException<SecurityException>(error) is null
				&& this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to read the '{format}' clipboard representation.", error);
			}
		}

		private static void HandleClipboardRepresentationFailure(Exception error)
		{
			if (FindException<OperationCanceledException>(error) is { } cancellation)
			{
				ExceptionDispatchInfo.Capture(cancellation).Throw();
			}
			if (FindFatalException(error) is { } fatal)
			{
				ExceptionDispatchInfo.Capture(fatal).Throw();
			}
			if (IsRecoverableClipboardRepresentationFailure(error))
			{
				return;
			}

			var propagated = error is InvalidOperationException { InnerException: { } inner }
				? inner
				: error;
			ExceptionDispatchInfo.Capture(propagated).Throw();
		}

		private static bool IsRecoverableClipboardRepresentationFailure(Exception error)
		{
			if (error is AggregateException aggregate)
			{
				if (aggregate.InnerExceptions.Count == 0)
				{
					return false;
				}
				foreach (var aggregateInner in aggregate.InnerExceptions)
				{
					if (!IsRecoverableClipboardRepresentationFailure(aggregateInner))
					{
						return false;
					}
				}
				return true;
			}
			if (error is InvalidOperationException { InnerException: { } inner })
			{
				return IsRecoverableClipboardRepresentationFailure(inner);
			}

			return error is InvalidOperationException
				or InvalidCastException
				or ArgumentException
				or IOException
				or InvalidDataException
				or NotSupportedException
				or UnauthorizedAccessException
				or SecurityException
				or COMException
				or ObjectDisposedException;
		}

		private static TException? FindException<TException>(Exception error)
			where TException : Exception
		{
			if (error is TException match)
			{
				return match;
			}
			if (error is AggregateException aggregate)
			{
				foreach (var inner in aggregate.InnerExceptions)
				{
					if (FindException<TException>(inner) is { } nested)
					{
						return nested;
					}
				}
				return null;
			}
			return error.InnerException is { } innerException
				? FindException<TException>(innerException)
				: null;
		}

		internal static Exception? FindFatalException(Exception error)
		{
			if (error is OutOfMemoryException
				or StackOverflowException
				or AccessViolationException
				or AppDomainUnloadedException
				or BadImageFormatException
				or CannotUnloadAppDomainException)
			{
				return error;
			}
			if (error is AggregateException aggregate)
			{
				foreach (var inner in aggregate.InnerExceptions)
				{
					if (FindFatalException(inner) is { } fatal)
					{
						return fatal;
					}
				}
				return null;
			}
			return error.InnerException is { } innerException
				? FindFatalException(innerException)
				: null;
		}

		internal static bool IsImageOnlyFragment(RichTextFragment fragment)
			=> fragment.Text == "\ufffc"
				&& fragment.CharacterRuns.Count == 1
				&& fragment.CharacterRuns[0].Format.InlineImage is not null;

		internal static RichTextFragment SanitizeClipboardFragment(RichTextFragment fragment)
		{
			foreach (var run in fragment.CharacterRuns)
			{
				if (run.Format.ProtectedText)
				{
					return fragment.TransformCharacterFormats(state => state.ProtectedText = false);
				}
			}

			return fragment;
		}
	}

	internal enum ClipboardRepresentation
	{
		None,
		Best,
		Text,
		Rtf,
		RtfWithoutObjects,
		Bitmap,
	}

	internal static class TomClipboardFormat
	{
		internal const int Best = 0;
		internal const int Text = 1;
		internal const int Bitmap = 2;
		internal const int OemText = 7;
		internal const int Dib = 8;
		internal const int UnicodeText = 13;
		internal const int DibV5 = 17;
		private const int NonWindowsRtf = int.MinValue;
		private const int NonWindowsRtfWithoutObjects = int.MinValue + 1;
		private static readonly Lazy<int> _rtf = new(() => Register("Rich Text Format", NonWindowsRtf));
		private static readonly Lazy<int> _rtfWithoutObjects = new(() => Register(
			"Rich Text Format Without Objects",
			NonWindowsRtfWithoutObjects));

		internal static int Rtf => _rtf.Value;

		internal static int RtfWithoutObjects => _rtfWithoutObjects.Value;

		internal static ClipboardRepresentation Resolve(int format)
		{
			if (format == Best)
			{
				return ClipboardRepresentation.Best;
			}
			if (format is Text or UnicodeText)
			{
				return ClipboardRepresentation.Text;
			}
			if (format is Dib or DibV5)
			{
				return ClipboardRepresentation.Bitmap;
			}
			if (format == _rtf.Value && format != 0)
			{
				return ClipboardRepresentation.Rtf;
			}
			if (format == _rtfWithoutObjects.Value && format != 0)
			{
				return ClipboardRepresentation.RtfWithoutObjects;
			}
			return ClipboardRepresentation.None;
		}

		internal static bool IsAvailable(DataPackageView content, int format)
			=> Resolve(format) switch
			{
				ClipboardRepresentation.Best => content.Contains(StandardDataFormats.Rtf)
					|| content.Contains("Rich Text Format Without Objects")
					|| content.Contains(StandardDataFormats.Text)
					|| content.Contains(StandardDataFormats.Bitmap),
				ClipboardRepresentation.Text => content.Contains(StandardDataFormats.Text),
				ClipboardRepresentation.Rtf => content.Contains(StandardDataFormats.Rtf),
				ClipboardRepresentation.RtfWithoutObjects => content.Contains("Rich Text Format Without Objects"),
				ClipboardRepresentation.Bitmap => content.Contains(StandardDataFormats.Bitmap),
				_ => false,
			};

		private static int Register(string format, int nonWindowsFormat)
			=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? unchecked((int)RegisterClipboardFormat(format))
				: nonWindowsFormat;

		[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint RegisterClipboardFormat(string format);
	}
}
