using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public  partial interface IRandomAccessStreamReference 
	{
		IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync();
	}
}
