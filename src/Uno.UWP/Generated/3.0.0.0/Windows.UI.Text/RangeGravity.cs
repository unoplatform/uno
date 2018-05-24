#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum RangeGravity 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UIBehavior,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Backward,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Forward,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inward,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Outward,
		#endif
	}
	#endif
}
