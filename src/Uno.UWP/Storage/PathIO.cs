#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using UwpUnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace Windows.Storage
{
	public partial class PathIO
	{
		public static IAsyncOperation<string> ReadTextAsync(string absolutePath) =>
			ReadTextTaskAsync(absolutePath).AsAsyncOperation();

		public static IAsyncOperation<string> ReadTextAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			ReadTextTaskAsync(absolutePath, encoding).AsAsyncOperation();

		public static IAsyncAction WriteTextAsync(string absolutePath, string contents) =>
			WriteTextTaskAsync(absolutePath, contents).AsAsyncAction();

		public static IAsyncAction WriteTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			WriteTextTaskAsync(absolutePath, contents, encoding).AsAsyncAction();

		public static IAsyncAction AppendTextAsync(string absolutePath, string contents) =>
			AppendTextTaskAsync(absolutePath, contents).AsAsyncAction();

		public static IAsyncAction AppendTextAsync(string absolutePath, string contents, UwpUnicodeEncoding encoding) =>
			AppendTextTaskAsync(absolutePath, contents, encoding).AsAsyncAction();

		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath) =>
			ReadLinesTaskAsync(absolutePath).AsAsyncOperation();

		public static IAsyncOperation<IList<string>> ReadLinesAsync(string absolutePath, UwpUnicodeEncoding encoding) =>
			ReadLinesTaskAsync(absolutePath, encoding).AsAsyncOperation();

		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			WriteLinesTaskAsync(absolutePath, lines).AsAsyncAction();

		public static IAsyncAction WriteLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			WriteLinesTaskAsync(absolutePath, lines, encoding).AsAsyncAction();

		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines) =>
			AppendLinesTaskAsync(absolutePath, lines).AsAsyncAction();

		public static IAsyncAction AppendLinesAsync(string absolutePath, IEnumerable<string> lines, UwpUnicodeEncoding encoding) =>
			AppendLinesTaskAsync(absolutePath, lines, encoding).AsAsyncAction();

		public static IAsyncOperation<IBuffer> ReadBufferAsync(string absolutePath) =>
			ReadBufferTaskAsync(absolutePath).AsAsyncOperation();

		public static IAsyncAction WriteBufferAsync(string absolutePath, IBuffer buffer) =>
			WriteBufferTaskAsync(absolutePath, buffer).AsAsyncAction();

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
