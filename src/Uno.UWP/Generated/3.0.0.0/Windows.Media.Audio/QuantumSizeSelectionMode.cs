#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum QuantumSizeSelectionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemDefault,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LowestLatency,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClosestToDesired,
		#endif
	}
	#endif
}
