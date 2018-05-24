#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ProtectionCapabilityResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Maybe,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Probably,
		#endif
	}
	#endif
}
