using Windows.Foundation;
using Windows.Storage.Streams;

namespace Windows.Storage
{
	public partial interface IStorageFile2
	{
		IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options);

		IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options);
	}
}
