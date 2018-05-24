#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HttpCompletionOption 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResponseContentRead,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResponseHeadersRead,
		#endif
	}
	#endif
}
