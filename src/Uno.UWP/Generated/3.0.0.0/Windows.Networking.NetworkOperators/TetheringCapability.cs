#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TetheringCapability 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Enabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByGroupPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByHardwareLimitation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByOperator,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledBySku,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByRequiredAppNotInstalled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledDueToUnknownCause,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledBySystemCapability,
		#endif
	}
	#endif
}
