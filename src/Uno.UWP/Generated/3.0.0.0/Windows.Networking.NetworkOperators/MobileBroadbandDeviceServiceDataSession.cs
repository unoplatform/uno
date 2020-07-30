#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandDeviceServiceDataSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction WriteDataAsync( global::Windows.Storage.Streams.IBuffer value)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MobileBroadbandDeviceServiceDataSession.WriteDataAsync(IBuffer value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CloseSession()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession", "void MobileBroadbandDeviceServiceDataSession.CloseSession()");
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession.DataReceived.add
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession.DataReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession, global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataReceivedEventArgs> DataReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession", "event TypedEventHandler<MobileBroadbandDeviceServiceDataSession, MobileBroadbandDeviceServiceDataReceivedEventArgs> MobileBroadbandDeviceServiceDataSession.DataReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceDataSession", "event TypedEventHandler<MobileBroadbandDeviceServiceDataSession, MobileBroadbandDeviceServiceDataReceivedEventArgs> MobileBroadbandDeviceServiceDataSession.DataReceived");
			}
		}
		#endif
	}
}
