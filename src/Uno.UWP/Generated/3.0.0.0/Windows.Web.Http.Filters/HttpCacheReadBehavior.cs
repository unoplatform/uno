#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Http.Filters
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum HttpCacheReadBehavior 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MostRecent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlyFromCache,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoCache,
		#endif
	}
	#endif
}
