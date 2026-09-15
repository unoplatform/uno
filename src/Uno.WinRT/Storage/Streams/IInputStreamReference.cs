using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial interface IInputStreamReference
	{
		IAsyncOperation<IInputStream> OpenSequentialReadAsync();
	}
}
