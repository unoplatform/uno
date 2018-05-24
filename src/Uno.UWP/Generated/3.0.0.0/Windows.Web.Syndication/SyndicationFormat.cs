#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Syndication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SyndicationFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Atom10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rss20,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rss10,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rss092,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rss091,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Atom03,
		#endif
	}
	#endif
}
