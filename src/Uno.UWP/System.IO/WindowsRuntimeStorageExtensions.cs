#nullable enable

using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace System.IO
{
	public static partial class WindowsRuntimeStorageExtensions
	{
		public static async Task<Stream> OpenStreamForReadAsync(this IStorageFile windowsRuntimeFile)
		{
			if (windowsRuntimeFile is StorageFile file)
			{
				return await file.OpenStream(CancellationToken.None, FileAccessMode.Read, StorageOpenOptions.AllowReadersAndWriters);
			}
			else
			{
				var raStream = await windowsRuntimeFile.OpenReadAsync();
				return raStream.AsStreamForRead();
			}
		}

		public static async Task<Stream> OpenStreamForReadAsync(this IStorageFolder rootDirectory, string relativePath)
		{
			var file = await rootDirectory.GetFileAsync(relativePath);
			return await file.OpenStreamForReadAsync();
		}

		public static async Task<Stream> OpenStreamForWriteAsync(this IStorageFile windowsRuntimeFile)
		{
			if (windowsRuntimeFile is StorageFile file)
			{
				return await file.OpenStream(CancellationToken.None, FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
			}
			else
			{
				var raStream = await windowsRuntimeFile.OpenAsync(FileAccessMode.ReadWrite);
				return raStream.AsStreamForWrite();
			}
		}

		public static async Task<Stream> OpenStreamForWriteAsync(this IStorageFolder rootDirectory, string relativePath, CreationCollisionOption creationCollisionOption)
		{
			var file = await rootDirectory.CreateFileAsync(relativePath, creationCollisionOption);
			return await file.OpenStreamForWriteAsync();
		}
	}
}
