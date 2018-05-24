#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Streaming.Adaptive
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AdaptiveMediaSourceDiagnosticType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManifestUnchangedUponReload,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManifestMismatchUponReload,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManifestSignaledEndOfLiveEventUponReload,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MediaSegmentSkipped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceTimedOut,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceParsingError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BitrateDisabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FatalMediaSourceError,
		#endif
	}
	#endif
}
