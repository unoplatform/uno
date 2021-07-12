using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.Threading;
using Uno.Extensions;
using UwpUnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;
using UwpBuffer = Windows.Storage.Streams.Buffer;
using Uno.Storage.Internal;

namespace Windows.Storage
{
	/// <summary>
	/// Provides helper methods for reading and writing files that
	/// are represented by objects of type <see cref="IStorageFile" />.
	/// </summary>
	public static partial class FileIO
	{
		/// <summary>
		/// Reads the contents of the specified file and returns text.
		/// </summary>
		/// <param name="file">The file to read.</param>
		/// <returns>When this method completes successfully, it returns the contents
		/// of the file as a text string.</returns>
		public static IAsyncOperation<string> ReadTextAsync(IStorageFile file) =>
			AsyncOperation.FromTask(cancellationToken => ReadTextTaskAsync(file, cancellationToken));

		/// <summary>
		/// Reads the contents of the specified file using the specified character
		/// encoding and returns text.
		/// </summary>
		/// <param name="file">The file to read.</param>
		/// <param name="encoding">The character encoding to use.</param>
		/// <returns>When this method completes successfully, it returns the contents
		/// of the file as a text string.</returns>
		public static IAsyncOperation<string> ReadTextAsync(IStorageFile file, UwpUnicodeEncoding encoding) =>
			AsyncOperation.FromTask(cancellationToken => ReadTextTaskAsync(file, cancellationToken, encoding));

		/// <summary>
		/// Reads the contents of the specified file and returns lines of text.
		/// </summary>
		/// <param name="file">The file to read.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list
		/// (type <see cref="IList{T}" />) of lines of text. Each line of text in the list is represented
		/// by a <see cref="string"/> object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(IStorageFile file) =>
			AsyncOperation.FromTask(cancellationToken => ReadLinesTaskAsync(file, cancellationToken));

		/// <summary>
		/// Reads the contents of the specified file using the specified character encoding and returns lines of text.
		/// </summary>
		/// <param name="file">The file to read.</param>
		/// <param name="encoding">The character encoding to use.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list
		/// (type <see cref="IList{T}" />) of lines of text. Each line of text in the list is represented
		/// by a <see cref="string"/> object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(IStorageFile file, UwpUnicodeEncoding encoding) =>
			AsyncOperation.FromTask(cancellationToken => ReadLinesTaskAsync(file, cancellationToken, encoding));

		/// <summary>
		/// Writes text to the specified file.
		/// </summary>
		/// <param name="file">The file that the text is written to.</param>
		/// <param name="contents">The text to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(file, contents, append: false, recognizeEncoding: true, cancellationToken));

		/// <summary>
		/// Writes text to the specified file using the specified character encoding.
		/// </summary>
		/// <param name="file">The file that the text is written to.</param>
		/// <param name="contents">The text to write.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(file, contents, append: false, recognizeEncoding: true, cancellationToken, encoding));

		/// <summary>
		/// Writes lines of text to the specified file.
		/// </summary>
		/// <param name="file">The file that the lines are written to.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(IStorageFile file, IEnumerable<string> lines) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(file, lines, append: false, recognizeEncoding: true, cancellationToken));

		/// <summary>
		/// Writes lines of text to the specified file using the specified character encoding.
		/// </summary>
		/// <param name="file">The file that the lines are written to.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(IStorageFile file, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(file, lines, append: false, recognizeEncoding: true, cancellationToken, encoding));

		/// <summary>
		/// Appends text to the specified file.
		/// </summary>
		/// <param name="file">The file that the text is appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(file, contents, append: true, recognizeEncoding: true, cancellationToken));

		/// <summary>
		/// Appends text to the specified file using the specified character encoding.
		/// </summary>
		/// <param name="file">The file that the text is appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(file, contents, append: true, recognizeEncoding: true, cancellationToken, encoding));

		/// <summary>
		/// Appends lines of text to the specified file.
		/// </summary>
		/// <param name="file">The file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(IStorageFile file, IEnumerable<string> lines) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(file, lines, append: true, recognizeEncoding: true, cancellationToken));

		/// <summary>
		/// Appends lines of text to the specified file using the specified character encoding.
		/// </summary>
		/// <param name="file">The file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(IStorageFile file, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(file, lines, append: true, recognizeEncoding: true, cancellationToken, encoding));


		/// <summary>
		/// Writes an array of bytes of data to the specified file.
		/// </summary>
		/// <param name="file">The file that the byte is written to.</param>
		/// <param name="buffer">The array of bytes to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBytesAsync(IStorageFile file, byte[] buffer) =>
			AsyncAction.FromTask(cancellationToken => WriteBytesTaskAsync(file, buffer, cancellationToken));

		/// <summary>
		/// Reads the contents of the specified file and returns a buffer.
		/// </summary>
		/// <param name="file">The file to read.</param>
		/// <returns>When this method completes, it returns an object
		/// (type <see cref="IBuffer" />) that represents the contents of the file.</returns>
		public static IAsyncOperation<IBuffer> ReadBufferAsync(IStorageFile file) =>
			AsyncOperation.FromTask(cancellationToken => ReadBufferTaskAsync(file, cancellationToken));

		/// <summary>
		/// Writes data from a buffer to the specified file.
		/// </summary>
		/// <param name="file">The file that the buffer of data is written to.</param>
		/// <param name="buffer">The buffer that contains the data to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBufferAsync(IStorageFile file, IBuffer buffer) =>
			AsyncAction.FromTask(cancellationToken => WriteBufferTaskAsync(file, buffer, cancellationToken));

		private static async Task<string> ReadTextTaskAsync(IStorageFile file, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			Encoding systemEncoding;
			if (encoding == null)
			{
				systemEncoding = await GetEncodingFromFileAsync(file);
			}
			else
			{
				systemEncoding = UwpEncodingToSystemEncoding(encoding.Value);
			}

			using Stream fileStream = await file.OpenStreamForReadAsync();
			using StreamReader streamReader = new StreamReader(fileStream, systemEncoding);

			return await streamReader.ReadToEndAsync();
		}

		private static async Task<IList<string>> ReadLinesTaskAsync(IStorageFile file, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			Encoding systemEncoding;
			if (encoding == null)
			{
				systemEncoding = await GetEncodingFromFileAsync(file);
			}
			else
			{
				systemEncoding = UwpEncodingToSystemEncoding(encoding.Value);
			}

			cancellationToken.ThrowIfCancellationRequested();

			using Stream fileStream = await file.OpenStreamForReadAsync();
			using StreamReader streamReader = new StreamReader(fileStream, systemEncoding);

			var lines = new List<string>();
			string line;
			while ((line = await streamReader.ReadLineAsync()) != null)
			{
				lines.Add(line);

				cancellationToken.ThrowIfCancellationRequested();
			}

			return lines;
		}

		internal static Task WriteLinesTaskAsync(IStorageFile file, IEnumerable<string> lines, bool append, bool recognizeEncoding, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null) =>
			WriteTextTaskAsync(file, ConvertLinesToString(lines), append, recognizeEncoding, cancellationToken, encoding);

		internal static async Task WriteTextTaskAsync(IStorageFile file, string contents, bool append, bool recognizeEncoding, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			Encoding systemEncoding;
			if (encoding == null)
			{
				if (recognizeEncoding)
				{
					systemEncoding = await GetEncodingFromFileAsync(file);
				}
				else
				{
					systemEncoding = Encoding.Default;
				}
			}
			else
			{
				systemEncoding = UwpEncodingToSystemEncoding(encoding.Value);
			}

			using var fileStream = await file.OpenStreamForWriteAsync();

			if (append)
			{
				fileStream.Seek(0, SeekOrigin.End);
			}
			else
			{
				// reset file content, as UWP does
				fileStream.SetLength(0);
			}

			using StreamWriter streamWriter = new StreamWriter(fileStream, systemEncoding);

			// Configure autoflush, for it seems FlushAsync does not flush properly
			// on .NET 6 preview 1 (WebAssembly). Failure in runtime tests otherwise.
			streamWriter.AutoFlush = true;

			await streamWriter.WriteAsync(contents);
			await streamWriter.FlushAsync();
		}

		private static async Task WriteBytesTaskAsync(IStorageFile file, byte[] buffer, CancellationToken cancellationToken)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			using var fs = await file.OpenStreamForWriteAsync();
			await fs.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
			await fs.FlushAsync();
		}

		private static async Task<IBuffer> ReadBufferTaskAsync(IStorageFile file, CancellationToken cancellationToken)
		{
			using var fs = await file.OpenStreamForReadAsync();

			cancellationToken.ThrowIfCancellationRequested();

			var bytes = await fs.ReadBytesAsync();

			return new UwpBuffer(bytes);
		}

		private static async Task WriteBufferTaskAsync(IStorageFile file, IBuffer buffer, CancellationToken cancellationToken)
		{
			var data = UwpBuffer.Cast(buffer).GetSegment();
			await WriteBytesTaskAsync(file, data.Array!, cancellationToken);
		}

		private static async Task<Encoding> GetEncodingFromFileAsync(IStorageFile file)
		{
			// If the file has a local path, try to not create it
			if (file is StorageFile storageFile &&
				storageFile.Provider == StorageProviders.Local &&
				file.Path is { } path &&
				!string.IsNullOrWhiteSpace(path) &&
				!File.Exists(path))
			{
				return Encoding.UTF8;
			}

			using Stream fileStream = await file.OpenStreamForReadAsync();
			var bytes = new byte[2];
			if (await fileStream.ReadAsync(bytes, 0, bytes.Length) == 2)
			{
				if (bytes[0] == 0xff && bytes[1] == 0xfe)
				{
					return Encoding.Unicode;
				}
				else if (bytes[0] == 0xfe && bytes[1] == 0xff)
				{
					return Encoding.BigEndianUnicode;
				}
			}

			return Encoding.UTF8;
		}

		private static string ConvertLinesToString(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines) + Environment.NewLine;

		private static Encoding UwpEncodingToSystemEncoding(UwpUnicodeEncoding encoding)
		{
			switch (encoding)
			{
				case UwpUnicodeEncoding.Utf8:
					return Encoding.UTF8;
				case UwpUnicodeEncoding.Utf16LE:
					return Encoding.Unicode;
				case UwpUnicodeEncoding.Utf16BE:
					return Encoding.BigEndianUnicode;
			}

			return Encoding.UTF8;
		}
	}
}
