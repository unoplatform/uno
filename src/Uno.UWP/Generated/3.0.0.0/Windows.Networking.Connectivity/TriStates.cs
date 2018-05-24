#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TriStates 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoNotCare,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		No,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Yes,
		#endif
	}
	#endif
}
