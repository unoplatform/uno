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

		public static global::Windows.Foundation.IAsyncAction WriteTextAsync( global::Windows.Storage.IStorageFile file,  string contents)
		{
        Stream fileStream = await file.OpenStreamForWriteAsync();
        StreamWriter streamWriter = new StreamWriter(fileStream);
        streamWriter.Write(contents); 
        streamWriter.Dispose();
        fileStream.Dispose();
		}

		public static global::Windows.Foundation.IAsyncAction AppendTextAsync( global::Windows.Storage.IStorageFile file,  string contents)
		{
        Stream fileStream = await file.OpenStreamForWriteAsync();
        fileStream.Seek(0, SeekOrigin.End);
        StreamWriter streamWriter = new StreamWriter(fileStream);
        streamWriter.Write(contents); 
        streamWriter.Dispose();
        fileStream.Dispose();
		}
