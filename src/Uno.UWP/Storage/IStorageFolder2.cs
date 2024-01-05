using Windows.Foundation;

namespace Windows.Storage
{
	public partial interface IStorageFolder2
	{
		IAsyncOperation<IStorageItem?> TryGetItemAsync(string name);
	}
}
