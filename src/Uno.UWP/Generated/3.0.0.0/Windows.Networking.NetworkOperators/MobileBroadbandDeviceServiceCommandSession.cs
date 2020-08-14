#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandDeviceServiceCommandSession 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceCommandResult> SendQueryCommandAsync( uint commandId,  global::Windows.Storage.Streams.IBuffer data)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandDeviceServiceCommandResult> MobileBroadbandDeviceServiceCommandSession.SendQueryCommandAsync(uint commandId, IBuffer data) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceCommandResult> SendSetCommandAsync( uint commandId,  global::Windows.Storage.Streams.IBuffer data)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandDeviceServiceCommandResult> MobileBroadbandDeviceServiceCommandSession.SendSetCommandAsync(uint commandId, IBuffer data) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CloseSession()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.NetworkOperators.MobileBroadbandDeviceServiceCommandSession", "void MobileBroadbandDeviceServiceCommandSession.CloseSession()");
		}
		#endif
	}
}
