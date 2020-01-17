using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Storage;

namespace Windows.Storage
{
	public partial class FileIO
	{

		private static async Task AppendWriteTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding, bool append)
		{
			if (file is null)
			{
				throw new NullReferenceException("StorageFile cannot be null");
			}

			Encoding encodingForWriter = Encoding.UTF8;
			switch (encoding)
			{
				case Streams.UnicodeEncoding.Utf8:
					encodingForWriter = Encoding.UTF8;
					break;
				case Streams.UnicodeEncoding.Utf16LE:
					encodingForWriter = Encoding.Unicode;
					break;
				case Streams.UnicodeEncoding.Utf16BE:
					encodingForWriter = Encoding.BigEndianUnicode;
					break;
			}

			using (Stream fileStream = await file.OpenStreamForWriteAsync())
			{
				if (append)
				{
					fileStream.Seek(0, SeekOrigin.End);
				}

				using (StreamWriter streamWriter = new StreamWriter(fileStream, encodingForWriter))
				{
					await streamWriter.WriteAsync(contents);
				}
			}
		}

		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, true) as IAsyncAction;

		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, false) as IAsyncAction;

		private static async Task AppendWriteTextAsync(IStorageFile file, string contents, bool append)
		{
			Streams.UnicodeEncoding unicodeEncoding = Streams.UnicodeEncoding.Utf8;

			using (Stream fileReadStream = await file.OpenStreamForReadAsync())
			{
				using (BinaryReader binReader = new BinaryReader(fileReadStream))
				{
					try
					{
						byte firstByte = binReader.ReadByte();
						switch (firstByte)
						{
							case 0xfe:
								unicodeEncoding = Streams.UnicodeEncoding.Utf16BE;
								break;
							case 0xff:
								unicodeEncoding = Streams.UnicodeEncoding.Utf16LE;
								break;
						}
					}
					catch
					{
						// probably file is empty
					}

				}
			}

			AppendWriteTextAsync(file, contents, unicodeEncoding, append);
		}

		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents) => AppendWriteTextAsync(file, contents, false) as IAsyncAction;

		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents) => AppendWriteTextAsync(file, contents, true) as IAsyncAction;
	}
}
