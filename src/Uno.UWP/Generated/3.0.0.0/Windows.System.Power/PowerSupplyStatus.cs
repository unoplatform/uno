#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Power
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PowerSupplyStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inadequate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Adequate,
		#endif
	}
	#endif
}
