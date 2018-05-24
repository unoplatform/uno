#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TabAlignment 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Left,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Center,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Right,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Decimal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bar,
		#endif
	}
	#endif
}
