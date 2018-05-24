#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UiccAccessCondition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlwaysAllowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pin4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Administrative5,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Administrative6,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NeverAllowed,
		#endif
	}
	#endif
}
