#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Uno.Foundation.Logging;
using Windows.Storage.Streams;

namespace Microsoft.UI.Text
{
	public partial class RichEditTextDocument
	{
		private const int HardMaxStreamCharacters = RichTextRtfCodec.MaxRtfInputLength;
		private const int MaxPlainStreamBytes = HardMaxStreamCharacters * 4;
		private const int MaxStreamRollbackBytes = RichTextRtfCodec.MaxRtfOutputLength;
		private const string StreamRollbackFailureDataKey = "Uno.RichEditTextDocument.StreamRollbackFailure";

		/// <summary>Replaces the document with plain text or RTF read from a random-access stream.</summary>
		public void LoadFromStream(global::Microsoft.UI.Text.TextSetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			if (options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf))
			{
				ThrowIfNotEditable(0, _textBuffer.Length);
				var bytes = ReadStreamBytes(value, RichTextRtfCodec.MaxRtfInputLength);
				MathDocument? mathDocument = null;
				RichTextFragment fragment;
				if (bytes.Length == 0)
				{
					fragment = RichTextFragment.Empty();
				}
				else if (IsMathMode
					&& RichTextRtfCodec.TryReadMath(
						bytes,
						DefaultFormatState(),
						DefaultParagraphState(),
						GetSetTextImportCharacterLimit(0, _textBuffer.Length, options),
						ShouldTruncateSetTextImportAtLimit(0, _textBuffer.Length, options),
						out mathDocument,
						out var mathFragment))
				{
					fragment = mathFragment;
				}
				else
				{
					fragment = RichTextRtfCodec.Read(
						bytes,
						GetSetTextImportCharacterLimit(0, _textBuffer.Length, options),
						ShouldTruncateSetTextImportAtLimit(0, _textBuffer.Length, options));
				}
				fragment = ApplyRtfSetOptions(fragment, options);
				SetDocumentFragment(
					fragment,
					mathDocument,
					forceHistory: true,
					checkTextLimit: options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.CheckTextLimit));
			}
			else
			{
				SetText(options, DecodePlainText(ReadStreamBytes(value, MaxPlainStreamBytes)));
			}
		}

		/// <summary>Writes the document as plain text or RTF to a random-access stream.</summary>
		public void SaveToStream(global::Microsoft.UI.Text.TextGetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var isRtf = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf);
			byte[] bytes;
			if (isRtf)
			{
				var content = IsMathMode
					? RichTextRtfCodec.WriteMath(_mathDocument ?? MathDocument.FromPlainText(PlainText))
					: RichTextRtfCodec.Write(CaptureFragment(
						0,
						_textBuffer.Length,
						options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)));
				bytes = EncodeRtfStream(
					content,
					appendNullTerminator: _textBuffer.Length == 0);
			}
			else
			{
				GetText(options, out var content);
				bytes = EncodePlainText(content);
			}
			WriteStreamBytes(value, bytes);
		}

		internal void GetRangeTextViaStream(
			int start,
			int end,
			global::Microsoft.UI.Text.TextGetOptions options,
			IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var isRtf = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf);
			if (isRtf && start == end)
			{
				return;
			}

			var bytes = isRtf
				? EncodeRtfStream(RichTextRtfCodec.Write(CaptureFragment(
					start,
					end,
					options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden))),
					appendNullTerminator: false)
				: EncodePlainText(GetTextInRange(start, end, options));
			WriteStreamBytes(value, bytes);
		}

		internal string ReadRangeTextViaStream(IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return DecodePlainText(ReadStreamBytes(value, MaxPlainStreamBytes));
		}

		internal RichTextFragment ReadRangeRtfViaStream(
			IRandomAccessStream value,
			int maxCharacters,
			bool truncateAtLimit)
		{
			ArgumentNullException.ThrowIfNull(value);
			var bytes = ReadStreamBytes(value, RichTextRtfCodec.MaxRtfInputLength);
			return bytes.Length == 0
				? RichTextFragment.Empty()
				: RichTextRtfCodec.Read(bytes, maxCharacters, truncateAtLimit);
		}

		private static string DecodePlainText(byte[] bytes)
		{
			if (bytes.Length == 0)
			{
				return string.Empty;
			}

			Encoding encoding;
			var offset = 0;
			if (bytes.AsSpan().StartsWith(Encoding.UTF32.GetPreamble()))
			{
				encoding = new UTF32Encoding(false, true, true);
				offset = Encoding.UTF32.GetPreamble().Length;
			}
			else if (bytes.AsSpan().StartsWith(new byte[] { 0, 0, 0xfe, 0xff }))
			{
				encoding = new UTF32Encoding(true, true, true);
				offset = 4;
			}
			else if (bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()))
			{
				encoding = new UTF8Encoding(false, true);
				offset = Encoding.UTF8.GetPreamble().Length;
			}
			else if (bytes.AsSpan().StartsWith(Encoding.Unicode.GetPreamble()))
			{
				encoding = new global::System.Text.UnicodeEncoding(false, true, true);
				offset = Encoding.Unicode.GetPreamble().Length;
			}
			else if (bytes.AsSpan().StartsWith(Encoding.BigEndianUnicode.GetPreamble()))
			{
				encoding = new global::System.Text.UnicodeEncoding(true, true, true);
				offset = Encoding.BigEndianUnicode.GetPreamble().Length;
			}
			else
			{
				encoding = new global::System.Text.UnicodeEncoding(false, false, true);
			}

			try
			{
				var text = encoding.GetString(bytes, offset, bytes.Length - offset);
				if (text.Length > HardMaxStreamCharacters)
				{
					throw new ArgumentException("The text stream is too large.", nameof(bytes));
				}
				return text;
			}
			catch (DecoderFallbackException error)
			{
				throw new ArgumentException("The text stream encoding is invalid.", nameof(bytes), error);
			}
		}

		private static byte[] EncodePlainText(string content)
		{
			var bytes = new global::System.Text.UnicodeEncoding(false, false, true).GetBytes(content);
			if (bytes.Length > MaxPlainStreamBytes)
			{
				throw new ArgumentException("The text stream output is too large.", nameof(content));
			}
			return bytes;
		}

		private static byte[] EncodeRtfStream(string content, bool appendNullTerminator)
		{
			var byteCount = Encoding.ASCII.GetByteCount(content);
			var terminatorLength = 2 + (appendNullTerminator ? 1 : 0);
			if (byteCount > RichTextRtfCodec.MaxRtfOutputLength - terminatorLength)
			{
				throw new ArgumentException("The RTF stream output is too large.", nameof(content));
			}

			var bytes = new byte[byteCount + terminatorLength];
			Encoding.ASCII.GetBytes(content, 0, content.Length, bytes, 0);
			bytes[byteCount] = (byte)'\r';
			bytes[byteCount + 1] = (byte)'\n';
			return bytes;
		}

		private static byte[] ReadStreamBytes(IRandomAccessStream value, int maxBytes)
		{
			if (!value.CanRead)
			{
				throw new NotSupportedException("The stream is not readable.");
			}

			var originalPosition = value.Position;
			try
			{
				if (value.Size > (ulong)maxBytes)
				{
					throw new ArgumentException("The text stream is too large.", nameof(value));
				}

				value.Seek(0);
				var stream = value.AsStreamForRead();
				using var buffer = new MemoryStream((int)Math.Min(value.Size, (ulong)int.MaxValue));
				var chunk = new byte[4096];
				while (true)
				{
					var read = stream.Read(chunk, 0, chunk.Length);
					if (read == 0)
					{
						break;
					}
					if (buffer.Length > maxBytes - read)
					{
						throw new ArgumentException("The text stream is too large.", nameof(value));
					}
					buffer.Write(chunk, 0, read);
				}
				return buffer.ToArray();
			}
			finally
			{
				value.Seek(originalPosition);
			}
		}

		private static void WriteStreamBytes(IRandomAccessStream value, byte[] bytes)
		{
			if (!value.CanWrite)
			{
				throw new NotSupportedException("The stream is not writable.");
			}

			var originalPosition = value.Position;
			var originalSize = value.Size;
			byte[]? originalBytes = null;
			if (value.CanRead && originalSize <= MaxStreamRollbackBytes)
			{
				originalBytes = ReadStreamBytes(value, MaxStreamRollbackBytes);
			}

			var mutationStarted = false;
			try
			{
				value.Seek(0);
				var stream = value.AsStreamForWrite();
				mutationStarted = true;
				stream.Write(bytes, 0, bytes.Length);
				stream.Flush();
				value.Size = (ulong)bytes.Length;
				value.Seek(originalPosition);
			}
			catch (Exception error) when (!IsFatalException(error))
			{
				var recoveryErrors = new List<Exception>();
				if (mutationStarted)
				{
					if (originalBytes is null)
					{
						recoveryErrors.Add(new IOException(
							"The stream does not support bounded rollback; its contents may be partially modified."));
					}
					else if (TryRestoreStream(value, originalBytes, originalSize) is { } rollbackError)
					{
						recoveryErrors.Add(rollbackError);
					}
				}

				if (TryRestoreStreamPosition(value, originalPosition) is { } positionError)
				{
					recoveryErrors.Add(positionError);
				}

				if (recoveryErrors.Count != 0)
				{
					AttachStreamRollbackFailure(error, recoveryErrors);
				}

				throw;
			}
		}

		private static void AttachStreamRollbackFailure(Exception error, List<Exception> recoveryErrors)
		{
			try
			{
				error.Data[StreamRollbackFailureDataKey] = recoveryErrors.Count == 1
					? recoveryErrors[0]
					: new AggregateException("Multiple stream rollback operations failed.", recoveryErrors);
			}
			catch (Exception attachmentError) when (!IsFatalException(attachmentError))
			{
				try
				{
					if (typeof(RichEditTextDocument).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(RichEditTextDocument).Log().Warn(
							"Failed to attach stream rollback diagnostics to the original exception.",
							attachmentError);
					}
				}
				catch (Exception loggingError) when (!IsFatalException(loggingError))
				{
					// Rollback diagnostics must never replace the original stream failure.
				}
			}
		}

		private static Exception? TryRestoreStream(IRandomAccessStream value, byte[] originalBytes, ulong originalSize)
		{
			try
			{
				RestoreStream(value, originalBytes, originalSize);
				return null;
			}
			catch (Exception error) when (!IsFatalException(error))
			{
				return error;
			}
		}

		private static Exception? TryRestoreStreamPosition(IRandomAccessStream value, ulong originalPosition)
		{
			try
			{
				value.Seek(originalPosition);
				return null;
			}
			catch (Exception error) when (!IsFatalException(error))
			{
				return error;
			}
		}

		private static bool IsFatalException(Exception error)
			=> FindFatalException(error) is not null;

		private static void RestoreStream(IRandomAccessStream value, byte[]? originalBytes, ulong originalSize)
		{
			if (originalBytes is null)
			{
				return;
			}

			value.Seek(0);
			var stream = value.AsStreamForWrite();
			stream.Write(originalBytes, 0, originalBytes.Length);
			stream.Flush();
			value.Size = originalSize;
		}
	}
}
