#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FormatEffect 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Off,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		On,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Toggle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
	}
	#endif
}
