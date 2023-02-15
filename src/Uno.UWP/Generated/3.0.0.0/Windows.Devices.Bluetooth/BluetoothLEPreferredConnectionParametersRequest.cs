#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEPreferredConnectionParametersRequest : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothLEPreferredConnectionParametersRequestStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEPreferredConnectionParametersRequestStatus BluetoothLEPreferredConnectionParametersRequest.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothLEPreferredConnectionParametersRequestStatus%20BluetoothLEPreferredConnectionParametersRequest.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothLEPreferredConnectionParametersRequest.Status.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.BluetoothLEPreferredConnectionParametersRequest", "void BluetoothLEPreferredConnectionParametersRequest.Dispose()");
		}
		#endif
		// Processing: System.IDisposable
	}
}
