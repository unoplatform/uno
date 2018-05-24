#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HdmiDisplayColorSpace 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RgbLimited,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RgbFull,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BT2020,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BT709,
		#endif
	}
	#endif
}
