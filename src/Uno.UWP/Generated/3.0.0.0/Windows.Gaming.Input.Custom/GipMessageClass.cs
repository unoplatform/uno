#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GipMessageClass 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Command,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LowLatency,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StandardLatency,
		#endif
	}
	#endif
}
