#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ZoomUnit 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoAmount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LargeDecrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallDecrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LargeIncrement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallIncrement,
		#endif
	}
	#endif
}
