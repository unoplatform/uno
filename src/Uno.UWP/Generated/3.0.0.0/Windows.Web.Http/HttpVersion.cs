#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HttpVersion 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Http10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Http11,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Http20,
		#endif
	}
	#endif
}
