#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SmartCards
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmartCardPinCharacterPolicyOption 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequireAtLeastOne,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disallow,
		#endif
	}
	#endif
}
