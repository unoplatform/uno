#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Management.Workplace
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MessagingSyncPolicy 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disallowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Required,
		#endif
	}
	#endif
}
