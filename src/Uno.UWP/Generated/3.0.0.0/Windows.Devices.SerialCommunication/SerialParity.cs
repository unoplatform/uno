#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SerialParity 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Odd,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Even,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mark,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Space,
		#endif
	}
	#endif
}
