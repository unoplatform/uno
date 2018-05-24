#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Streaming.Adaptive
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AdaptiveMediaSourceCreationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManifestDownloadFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManifestParseFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedManifestContentType,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedManifestVersion,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedManifestProfile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
	}
	#endif
}
