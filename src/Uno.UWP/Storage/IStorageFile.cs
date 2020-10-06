using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public  partial interface IStorageFile
	{
		string ContentType { get; }
		string FileType { get; }

		IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode);
		IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync();
		IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder);
		IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName);
		IAsyncOperation<StorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
		IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace);
		IAsyncAction MoveAsync(IStorageFolder destinationFolder);
		IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName);
		IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option);
		IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace);
	}
}
