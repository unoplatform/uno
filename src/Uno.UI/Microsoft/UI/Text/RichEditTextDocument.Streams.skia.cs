#nullable enable

using System;
using System.IO;
using System.Text;
using Windows.Storage.Streams;

namespace Microsoft.UI.Text
{
	public partial class RichEditTextDocument
	{
		private const int MaxStreamBytes = 4 * 1024 * 1024;
		private const int MaxStreamCharacters = 262_144;

		/// <summary>Replaces the document with plain text or RTF read from a random-access stream.</summary>
		public void LoadFromStream(global::Microsoft.UI.Text.TextSetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var content = ReadStreamText(value, options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf));
			SetText(options, content);
		}

		/// <summary>Writes the document as plain text or RTF to a random-access stream.</summary>
		public void SaveToStream(global::Microsoft.UI.Text.TextGetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var isRtf = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf);
			var content = isRtf
				? RichTextRtfCodec.Write(CaptureFragment(0, _plainText.Length, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)), MaxStreamBytes)
				: GetTextInRange(0, _plainText.Length, options);
			WriteStreamText(value, content, isRtf);
		}

		internal void GetRangeTextViaStream(int start, int end, global::Microsoft.UI.Text.TextGetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			var content = options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf)
				? RichTextRtfCodec.Write(CaptureFragment(start, end, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.NoHidden)), MaxStreamBytes)
				: GetTextInRange(start, end, options);
			WriteStreamText(value, content, options.HasFlag(global::Microsoft.UI.Text.TextGetOptions.FormatRtf));
		}

		internal string ReadRangeTextViaStream(global::Microsoft.UI.Text.TextSetOptions options, IRandomAccessStream value)
		{
			ArgumentNullException.ThrowIfNull(value);
			return ReadStreamText(value, options.HasFlag(global::Microsoft.UI.Text.TextSetOptions.FormatRtf));
		}

		private static string ReadStreamText(IRandomAccessStream value, bool isRtf)
		{
			if (!value.CanRead)
			{
				throw new NotSupportedException("The stream is not readable.");
			}

			value.Seek(0);
			var stream = value.AsStream();
			if (stream.CanSeek && stream.Length > MaxStreamBytes)
			{
				throw new ArgumentException("The text stream is too large.", nameof(value));
			}

			using var reader = new StreamReader(stream, isRtf ? Encoding.ASCII : new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
			var buffer = new char[4096];
			var builder = new StringBuilder();
			while (true)
			{
				var read = reader.Read(buffer, 0, buffer.Length);
				if (read == 0)
				{
					break;
				}

				builder.Append(buffer, 0, read);
				if (builder.Length > MaxStreamCharacters)
				{
					throw new ArgumentException("The text stream is too large.", nameof(value));
				}
			}

			return builder.ToString();
		}

		private static void WriteStreamText(IRandomAccessStream value, string content, bool isRtf)
		{
			if (!value.CanWrite)
			{
				throw new NotSupportedException("The stream is not writable.");
			}

			if (content.Length > MaxStreamBytes)
			{
				throw new ArgumentException("The text stream output is too large.", nameof(content));
			}

			value.Size = 0;
			value.Seek(0);
			var stream = value.AsStream();
			using var writer = new StreamWriter(
				stream,
				isRtf ? Encoding.ASCII : new UTF8Encoding(false),
				bufferSize: 4096,
				leaveOpen: true);
			writer.Write(content);
			writer.Flush();
			stream.Flush();
		}
	}
}
