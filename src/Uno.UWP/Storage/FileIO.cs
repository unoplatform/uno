using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial class FileIO
	{

		private static async Task AppendWriteTextAsync(global::Windows.Storage.IStorageFile file, string contents, global::Windows.Storage.Streams.UnicodeEncoding encoding, bool append)
		{
			if (file is null)
			{
				throw new NullReferenceException("StorageFile cannot be null");
			}

			global::System.Text.Encoding encodingForWriter = global::System.Text.Encoding.UTF8;
			switch (encoding)
			{
				case global::Windows.Storage.Streams.UnicodeEncoding.Utf8:
					encodingForWriter = global::System.Text.Encoding.UTF8;
					break;
				case global::Windows.Storage.Streams.UnicodeEncoding.Utf16LE:
					encodingForWriter = global::System.Text.Encoding.Unicode;
					break;
				case global::Windows.Storage.Streams.UnicodeEncoding.Utf16BE:
					encodingForWriter = global::System.Text.Encoding.BigEndianUnicode;
					break;
			}

			using (Stream fileStream = await file.OpenStreamForWriteAsync())
			{
				if (append)
				{
					fileStream.Seek(0, SeekOrigin.End);
				}

				using (StreamWriter streamWriter = new StreamWriter(fileStream,encodingForWriter))
				{
					await streamWriter.WriteAsync(contents);
				}
			}
		}
		
		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( global::Windows.Storage.IStorageFile file,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, true) as global::Windows.Foundation.IAsyncAction;

		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents,  global::Windows.Storage.Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, false) as global::Windows.Foundation.IAsyncAction;

		private static global::Windows.Foundation.IAsyncAction AppendWriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents, bool append)
		{
			global::Windows.Storage.Streams.UnicodeEncoding unicodeEncoding = global::Windows.Storage.Streams.UnicodeEncoding.Utf8;

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
								unicodeEncoding = global::Windows.Storage.Streams.UnicodeEncoding.Utf16BE;
								break;
							case 0xff:
								unicodeEncoding = global::Windows.Storage.Streams.UnicodeEncoding.Utf16LE;
								break;
						}
					}
					catch
					{
					   // probably file is empty
					}
					
				}
			}

			AppendWriteTextAsync( file,  contents, unicodeEncoding, append);
		}
	
		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents) => AppendWriteTextAsync(file, contents, false);

		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( global::Windows.Storage.IStorageFile file,  string contents) => AppendWriteTextAsync(file, contents, true);
	}
}
