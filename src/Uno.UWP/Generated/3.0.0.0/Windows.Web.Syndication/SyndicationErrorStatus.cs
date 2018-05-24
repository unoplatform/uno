#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.Syndication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SyndicationErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MissingRequiredElement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MissingRequiredAttribute,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidXml,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnexpectedContent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedFormat,
		#endif
	}
	#endif
}
