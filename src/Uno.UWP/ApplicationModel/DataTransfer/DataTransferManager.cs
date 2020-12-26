#if __WASM__
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private DataTransferManager()
		{
		}

		public event TypedEventHandler<DataTransferManager, DataRequestedEventArgs> DataRequested;
	}
}
#endif
