using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using UwpUnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;
using UwpBuffer = Windows.Storage.Streams.Buffer;

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

		private static async Task WriteTextTaskAsync(string path, string contents, bool append, UwpUnicodeEncoding? encoding = null)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var file = await StorageFile.GetFileFromPathAsync(path);
			if (encoding == null)
			{
				return await FileIO.WriteTextAsync(file, contents, append).AsTask();
			}
			return await FileIO.WriteTextAsync(file, contents, append, encoding).AsTask();
		}

		private static async Task WriteBytesTaskAsync(IStorageFile file, byte[] buffer)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			using var fs = File.Create(file.Path);
			await fs.WriteAsync(buffer, 0, buffer.Length);
		}

		private static async Task<IBuffer> ReadBufferTaskAsync(IStorageFile file)
		{
			using var fs = File.OpenRead(file.Path);
			var bytes = await fs.ReadBytesAsync();
			return new UwpBuffer(bytes);
		}

		private static async Task WriteBufferTaskAsync(IStorageFile file, IBuffer buffer)
		{
			if (!(buffer is UwpBuffer inMemoryBuffer))
			{
				throw new NotSupportedException("The current implementation can only write a UwpBuffer");
			}

			await WriteBytesTaskAsync(file, inMemoryBuffer.Data);
		}

		private static async Task<Encoding> GetEncodingFromFileAsync(IStorageFile file)
		{
			if (File.Exists(file.Path))
			{
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
			}
			return Encoding.UTF8;
		}

		private static string ConvertLinesToString(IEnumerable<string> lines) => string.Join(Environment.NewLine, lines);

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
