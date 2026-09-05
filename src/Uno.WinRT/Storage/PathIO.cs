#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using UwpUnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Windows.Storage
{
	/// <summary>
	/// Provides helper methods for reading and writing a file using
	/// the absolute path or Uniform Resource Identifier (URI) of the file.
	/// </summary>
	public static partial class PathIO
	{
		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI) and returns text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a text string.</returns>
		public static IAsyncOperation<string> ReadTextAsync(string absolutePath) =>
			AsyncOperation.FromTask(cancellationToken => ReadTextTaskAsync(absolutePath, cancellationToken));

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding and returns text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a text string.</returns>
		public static IAsyncOperation<string> ReadTextAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			AsyncOperation.FromTask(cancellationToken => ReadTextTaskAsync(absolutePath, cancellationToken, encoding));

		/// <summary>
		/// Writes text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="contents">The text to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(string absolutePath, string contents) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(absolutePath, contents, cancellationToken));

		/// <summary>
		/// Writes text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="contents">The text to write.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteTextTaskAsync(absolutePath, contents, cancellationToken, encoding));

		/// <summary>
		/// Appends text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(string absolutePath, string contents) =>
			AsyncAction.FromTask(cancellationToken => AppendTextTaskAsync(absolutePath, contents, cancellationToken));

		/// <summary>
		/// Appends text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => AppendTextTaskAsync(absolutePath, contents, cancellationToken, encoding));

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// and returns lines of text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list (type IList)
		/// of lines of text. Each line of text in the list is represented by a String object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath) =>
			AsyncOperation.FromTask(cancellationToken => ReadLinesTaskAsync(absolutePath, cancellationToken));

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding and returns lines of text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list (type IList)
		/// of lines of text. Each line of text in the list is represented by a String object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			AsyncOperation.FromTask(cancellationToken => ReadLinesTaskAsync(absolutePath, cancellationToken, encoding));

		/// <summary>
		/// Writes lines of text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(absolutePath, lines, cancellationToken));

		/// <summary>
		/// Writes lines of text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => WriteLinesTaskAsync(absolutePath, lines, cancellationToken, encoding));

		/// <summary>
		/// Appends lines of text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			AsyncAction.FromTask(cancellationToken => AppendLinesTaskAsync(absolutePath, lines, cancellationToken));

		/// <summary>
		/// Appends lines of text to the file at the specified path or
		/// Uniform Resource Identifier (URI) using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AsyncAction.FromTask(cancellationToken => AppendLinesTaskAsync(absolutePath, lines, cancellationToken, encoding));

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI) and returns a buffer.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <returns>When this method completes, it returns an object (type IBuffer) that represents the contents of the file.</returns>
		public static IAsyncOperation<IBuffer> ReadBufferAsync(string absolutePath) =>
			AsyncOperation.FromTask(cancellationToken => ReadBufferTaskAsync(absolutePath, cancellationToken));

		/// <summary>
		/// Writes data from a buffer to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="buffer">The buffer that contains the data to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBufferAsync(string absolutePath, IBuffer buffer) =>
			AsyncAction.FromTask(cancellationToken => WriteBufferTaskAsync(absolutePath, buffer, cancellationToken));

		/// <summary>
		/// Writes a single byte of data to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="buffer">The buffer that contains the data to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBytesAsync(string absolutePath, byte[] buffer) =>
			AsyncAction.FromTask(cancellationToken => WriteBytesTaskAsync(absolutePath, buffer, cancellationToken));

		private static async Task<string> ReadTextTaskAsync(
			string path,
			CancellationToken cancellationToken,
			UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				return await FileIO.ReadTextAsync(file).AsTask(cancellationToken);
			}
			return await FileIO
				.ReadTextAsync(file, encoding.Value)
				.AsTask(cancellationToken);
		}

		private static async Task<IList<string>> ReadLinesTaskAsync(
			string path,
			CancellationToken cancellationToken,
			UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				return await FileIO.ReadLinesAsync(file).AsTask();
			}
			return await FileIO
				.ReadLinesAsync(file, encoding.Value)
				.AsTask(cancellationToken);
		}

		private static async Task WriteLinesTaskAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO
					.WriteLinesAsync(file, lines)
					.AsTask(cancellationToken);
			}
			else
			{
				await FileIO
					.WriteLinesAsync(file, lines, encoding.Value)
					.AsTask(cancellationToken);
			}
		}

		private static async Task AppendLinesTaskAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO
					.WriteLinesTaskAsync(
						file,
						lines,
						append: true,
						recognizeEncoding: false,
						cancellationToken);
			}
			else
			{
				await FileIO
					.WriteLinesTaskAsync(
						file,
						lines,
						append: true,
						recognizeEncoding: false,
						cancellationToken,
						encoding.Value);
			}
		}

		private static async Task WriteTextTaskAsync(string path, string contents, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteTextAsync(file, contents).AsTask(cancellationToken);
			}
			else
			{
				await FileIO.WriteTextAsync(file, contents, encoding.Value).AsTask(cancellationToken);
			}
		}

		private static async Task AppendTextTaskAsync(string path, string contents, CancellationToken cancellationToken, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteTextTaskAsync(
					file,
					contents,
					append: true,
					recognizeEncoding: false,
					cancellationToken);
			}
			else
			{
				await FileIO.WriteTextTaskAsync(
					file,
					contents,
					append: true,
					recognizeEncoding: false,
					cancellationToken,
					encoding.Value);
			}
		}

		private static async Task WriteBytesTaskAsync(string path, byte[] buffer, CancellationToken cancellationToken)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteBytesAsync(file, buffer).AsTask(cancellationToken);
		}

		private static async Task<IBuffer> ReadBufferTaskAsync(string path, CancellationToken cancellationToken)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			return await FileIO.ReadBufferAsync(file).AsTask(cancellationToken);
		}

		private static async Task WriteBufferTaskAsync(string path, IBuffer buffer, CancellationToken cancellationToken)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteBufferAsync(file, buffer).AsTask(cancellationToken);
		}
	}
}
