#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VideoDeviceCharacteristic 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllStreamsIndependent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PreviewRecordStreamsIdentical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PreviewPhotoStreamsIdentical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RecordPhotoStreamsIdentical,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllStreamsIdentical,
		#endif
	}
	#endif
}
