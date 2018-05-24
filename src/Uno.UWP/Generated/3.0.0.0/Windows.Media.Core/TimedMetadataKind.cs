#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TimedMetadataKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Caption,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Chapter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Data,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Description,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Subtitle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImageSubtitle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Speech,
		#endif
	}
	#endif
}
