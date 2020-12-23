using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool DataTransferManager.IsSupported() is not implemented in Uno.");
		}

		public static void ShowShareUI()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataTransferManager", "void DataTransferManager.ShowShareUI()");
		}

		public static DataTransferManager GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member DataTransferManager DataTransferManager.GetForCurrentView() is not implemented in Uno.");
		}

		public event TypedEventHandler<DataTransferManager, DataRequestedEventArgs> DataRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataTransferManager", "event TypedEventHandler<DataTransferManager, DataRequestedEventArgs> DataTransferManager.DataRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.DataTransfer.DataTransferManager", "event TypedEventHandler<DataTransferManager, DataRequestedEventArgs> DataTransferManager.DataRequested");
			}
		}
	}
}
