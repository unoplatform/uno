#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaFrameReaderAcquisitionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Realtime,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Buffered,
		#endif
	}
	#endif
}
