#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.HumanInterfaceDevice
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HidCollectionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Physical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Application,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Logical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Report,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NamedArray,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsageSwitch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UsageModifier,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
