#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiDirectServiceSessionErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ok,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disassociated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LocalClose,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteClose,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoResponseFromRemote,
		#endif
	}
	#endif
}
