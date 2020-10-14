#nullable enable

using System;
using System.Collections.Generic;
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
			ReadTextTaskAsync(absolutePath).AsAsyncOperation();

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding and returns text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a text string.</returns>
		public static IAsyncOperation<string> ReadTextAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			ReadTextTaskAsync(absolutePath, encoding).AsAsyncOperation();

		/// <summary>
		/// Writes text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="contents">The text to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(string absolutePath, string contents) =>
			WriteTextTaskAsync(absolutePath, contents).AsAsyncAction();

		/// <summary>
		/// Writes text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="contents">The text to write.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			WriteTextTaskAsync(absolutePath, contents, encoding).AsAsyncAction();

		/// <summary>
		/// Appends text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(string absolutePath, string contents) =>
			AppendTextTaskAsync(absolutePath, contents).AsAsyncAction();

		/// <summary>
		/// Appends text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="contents">The text to append.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			AppendTextTaskAsync(absolutePath, contents, encoding).AsAsyncAction();

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// and returns lines of text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list (type IList)
		/// of lines of text. Each line of text in the list is represented by a String object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath) =>
			ReadLinesTaskAsync(absolutePath).AsAsyncOperation();

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding and returns lines of text.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>When this method completes successfully, it returns the contents of the file as a list (type IList)
		/// of lines of text. Each line of text in the list is represented by a String object.</returns>
		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			ReadLinesTaskAsync(absolutePath, encoding).AsAsyncOperation();

		/// <summary>
		/// Writes lines of text to the file at the specified path or Uniform Resource Identifier (URI)
		/// using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			WriteLinesTaskAsync(absolutePath, lines).AsAsyncAction();

		/// <summary>
		/// Writes lines of text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="lines">The list of text strings to write as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			WriteLinesTaskAsync(absolutePath, lines, encoding).AsAsyncAction();

		/// <summary>
		/// Appends lines of text to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			AppendLinesTaskAsync(absolutePath, lines).AsAsyncAction();

		/// <summary>
		/// Appends lines of text to the file at the specified path or
		/// Uniform Resource Identifier (URI) using the specified character encoding.
		/// </summary>
		/// <param name="absolutePath">The path of the file that the lines are appended to.</param>
		/// <param name="lines">The list of text strings to append as lines.</param>
		/// <param name="encoding">The character encoding of the file.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AppendLinesTaskAsync(absolutePath, lines, encoding).AsAsyncAction();

		/// <summary>
		/// Reads the contents of the file at the specified path or Uniform Resource Identifier (URI) and returns a buffer.
		/// </summary>
		/// <param name="absolutePath">The path of the file to read.</param>
		/// <returns>When this method completes, it returns an object (type IBuffer) that represents the contents of the file.</returns>
		public static IAsyncOperation<IBuffer> ReadBufferAsync(string absolutePath) =>
			ReadBufferTaskAsync(absolutePath).AsAsyncOperation();

		/// <summary>
		/// Writes data from a buffer to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="buffer">The buffer that contains the data to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBufferAsync(string absolutePath, IBuffer buffer) =>
			WriteBufferTaskAsync(absolutePath, buffer).AsAsyncAction();

		/// <summary>
		/// Writes a single byte of data to the file at the specified path or Uniform Resource Identifier (URI).
		/// </summary>
		/// <param name="absolutePath">The path of the file to write.</param>
		/// <param name="buffer">The buffer that contains the data to write.</param>
		/// <returns>No object or value is returned when this method completes.</returns>
		public static IAsyncAction WriteBytesAsync(string absolutePath, byte[] buffer) =>
			WriteBytesTaskAsync(absolutePath, buffer).AsAsyncAction();

		private static async Task<string> ReadTextTaskAsync(string path, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				return await FileIO.ReadTextAsync(file).AsTask();
			}
			return await FileIO.ReadTextAsync(file, encoding.Value).AsTask();
		}

		private static async Task<IList<string>> ReadLinesTaskAsync(string path, UwpUnicodeEncoding? encoding = null)
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
			return await FileIO.ReadLinesAsync(file, encoding.Value).AsTask();
		}

		private static async Task WriteLinesTaskAsync(string path, IEnumerable<string> lines, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteLinesAsync(file, lines).AsTask();
			}
			else
			{
				await FileIO.WriteLinesAsync(file, lines, encoding.Value).AsTask();
			}
		}

		private static async Task AppendLinesTaskAsync(string path, IEnumerable<string> lines, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteLinesTaskAsync(file, lines, append: true, recognizeEncoding: false);
			}
			else
			{
				await FileIO.WriteLinesTaskAsync(file, lines, append: true, recognizeEncoding: false, encoding.Value);
			}
		}

		private static async Task WriteTextTaskAsync(string path, string contents, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteTextAsync(file, contents).AsTask();
			}
			else
			{
				await FileIO.WriteTextAsync(file, contents, encoding.Value).AsTask();
			}
		}

		private static async Task AppendTextTaskAsync(string path, string contents, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				await FileIO.WriteTextTaskAsync(file, contents, append: true, recognizeEncoding: false);
			}
			else
			{
				await FileIO.WriteTextTaskAsync(file, contents, append: true, recognizeEncoding: false, encoding.Value);
			}
		}

		private static async Task WriteBytesTaskAsync(string path, byte[] buffer)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteBytesAsync(file, buffer).AsTask();
		}

		private static async Task<IBuffer> ReadBufferTaskAsync(string path)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			return await FileIO.ReadBufferAsync(file).AsTask();
		}

		private static async Task WriteBufferTaskAsync(string path, IBuffer buffer)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			await FileIO.WriteBufferAsync(file, buffer).AsTask();
		}
	}
}
