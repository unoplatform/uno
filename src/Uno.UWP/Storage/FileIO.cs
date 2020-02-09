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
using System.Collections.Generic;

namespace Windows.Storage
{
	public partial class FileIO
	{

		private static async Task AppendWriteTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding, bool append)
		{
			if (file is null)
			{
				// UWP throws NullReferenceException
				// but it was choosen to throw ArgumentNullException , as it is more appriopriate in this caase
				throw new ArgumentNullException("StorageFile cannot be null");
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

			Stream fileStream = null;
			try
			{
				fileStream = await file.OpenStreamForWriteAsync();
				if (append)
				{
					fileStream.Seek(0, SeekOrigin.End);
				}
				else
				{
					// reset file content, as UWP does
					fileStream.SetLength(0);
				}

				using (StreamWriter streamWriter = new StreamWriter(fileStream, encodingForWriter))
				{
					await streamWriter.WriteAsync(contents);
				}
			}
			finally
			{
				if(fileStream != null)
				{
					fileStream.Dispose();
				}
			}
		}

		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, true).AsAsyncAction();

		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents, Streams.UnicodeEncoding encoding) => AppendWriteTextAsync(file, contents, encoding, false).AsAsyncAction();

		private static string ConvertLinesToString(IEnumerable<string> lines)
		{
			StringBuilder output = new StringBuilder(); 
			foreach (string line in lines)
			{
				output.Append(line + Environment.NewLine);
			}

			return output.ToString();
		}

		public static IAsyncAction WriteLinesAsync(IStorageFile file, IEnumerable<string> lines, Streams.UnicodeEncoding encoding)
			=> WriteTextAsync(file, ConvertLinesToString(lines), encoding);

		public static IAsyncAction AppendLinesAsync(IStorageFile file, IEnumerable<string> lines, Streams.UnicodeEncoding encoding)
			=> AppendTextAsync(file, ConvertLinesToString(lines), encoding);


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

		public static IAsyncAction WriteTextAsync(IStorageFile file, string contents) => AppendWriteTextAsync(file, contents, false).AsAsyncAction();

		public static IAsyncAction AppendTextAsync(IStorageFile file, string contents) => AppendWriteTextAsync(file, contents, true).AsAsyncAction();

		public static IAsyncAction WriteLinesAsync(IStorageFile file, IEnumerable<string> lines)
			=> WriteTextAsync(file, ConvertLinesToString(lines));
		public static IAsyncAction AppendLinesAsync(IStorageFile file, IEnumerable<string> lines)
			=> AppendTextAsync(file, ConvertLinesToString(lines));


		private static async Task<string> ReadTextTaskAsync(IStorageFile file, Streams.UnicodeEncoding encoding)
		{
			if (file is null)
			{
				// UWP throws NullReferenceException
				// but it was choosen to throw ArgumentNullException , as it is more appriopriate in this caase
				throw new ArgumentNullException("StorageFile cannot be null");
			}

			Encoding encodingForReader = Encoding.UTF8;
			switch (encoding)
			{
				case Streams.UnicodeEncoding.Utf8:
					encodingForReader = Encoding.UTF8;
					break;
				case Streams.UnicodeEncoding.Utf16LE:
					encodingForReader = Encoding.Unicode;
					break;
				case Streams.UnicodeEncoding.Utf16BE:
					encodingForReader = Encoding.BigEndianUnicode;
					break;
			}

			string output = "";
			using (Stream fileStream = await file.OpenStreamForReadAsync())
			{
				using (StreamReader streamReader = new StreamReader(fileStream, encodingForReader))
				{
					output = streamReader.ReadToEnd();
				}
			}
			return output;
		}

		public static IAsyncOperation<string> ReadTextAsync(IStorageFile file, Streams.UnicodeEncoding encoding)
			=> ReadTextTaskAsync(file, encoding).AsAsyncOperation<string>();

		private static async Task<string> ReadTextTaskAsync(IStorageFile file)
		{
			if (file is null)
			{
				// UWP throws NullReferenceException
				// but it was choosen to throw ArgumentNullException , as it is more appriopriate in this caase
				throw new ArgumentNullException("StorageFile cannot be null");
			}

			string output = "";
			Stream fileStream = null;
			try
            {
				fileStream = await file.OpenStreamForReadAsync();
				using (StreamReader streamReader = new StreamReader(fileStream))
				{
					output = streamReader.ReadToEnd();
				}
			}
			finally
			{
				if(fileStream != null)
                {
					fileStream.Dispose();
				}
			}
			return output;
		}

		public static IAsyncOperation<string> ReadTextAsync(IStorageFile file)
			=> ReadTextTaskAsync(file).AsAsyncOperation<string>();

		private static async Task<IList<string>> ReadLinesTaskAsync(IStorageFile file, Streams.UnicodeEncoding encoding)
		{
			string output = await ReadTextTaskAsync(file, encoding);
			return output.Split(Environment.NewLine);
		}

		public static IAsyncOperation<IList<string>> ReadLinesAsync(IStorageFile file, Streams.UnicodeEncoding encoding)
			=> ReadLinesTaskAsync(file, encoding).AsAsyncOperation<IList<string>>();
		private static async Task<IList<string>> ReadLinesTaskAsync(IStorageFile file)
		{
			string output = await ReadTextTaskAsync(file);
			return output.Split(Environment.NewLine);
		}

		public static IAsyncOperation<IList<string>> ReadLinesAsync(IStorageFile file)
			=> ReadLinesTaskAsync(file).AsAsyncOperation<IList<string>>();


	}
}
