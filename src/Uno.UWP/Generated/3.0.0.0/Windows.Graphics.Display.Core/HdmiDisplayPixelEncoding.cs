#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HdmiDisplayPixelEncoding 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rgb444,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ycc444,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ycc422,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ycc420,
		#endif
	}
	#endif
}
