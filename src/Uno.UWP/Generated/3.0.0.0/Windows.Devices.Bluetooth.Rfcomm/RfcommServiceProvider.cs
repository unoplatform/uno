#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Rfcomm
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RfcommServiceProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<uint, global::Windows.Storage.Streams.IBuffer> SdpRawAttributes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<uint, IBuffer> RfcommServiceProvider.SdpRawAttributes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Rfcomm.RfcommServiceId ServiceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member RfcommServiceId RfcommServiceProvider.ServiceId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider.ServiceId.get
		// Forced skipping of method Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider.SdpRawAttributes.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartAdvertising( global::Windows.Networking.Sockets.StreamSocketListener listener)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider", "void RfcommServiceProvider.StartAdvertising(StreamSocketListener listener)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopAdvertising()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider", "void RfcommServiceProvider.StopAdvertising()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartAdvertising( global::Windows.Networking.Sockets.StreamSocketListener listener,  bool radioDiscoverable)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider", "void RfcommServiceProvider.StartAdvertising(StreamSocketListener listener, bool radioDiscoverable)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider> CreateAsync( global::Windows.Devices.Bluetooth.Rfcomm.RfcommServiceId serviceId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<RfcommServiceProvider> RfcommServiceProvider.CreateAsync(RfcommServiceId serviceId) is not implemented in Uno.");
		}
		#endif
	}
}
