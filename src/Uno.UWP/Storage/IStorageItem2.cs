using Windows.Foundation;

namespace Windows.Storage
{
	public partial interface IStorageItem2 : IStorageItem
	{
		IAsyncOperation<StorageFolder?> GetParentAsync();
		bool IsEqual(IStorageItem item);
	}
}
