#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DisplayMonitorConnectionKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Internal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wireless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Virtual,
		#endif
	}
	#endif
}
