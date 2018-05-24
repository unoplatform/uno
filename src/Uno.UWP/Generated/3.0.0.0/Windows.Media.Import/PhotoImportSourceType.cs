#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Import
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhotoImportSourceType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Generic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Camera,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MediaPlayer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Phone,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Video,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalInfoManager,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioRecorder,
		#endif
	}
	#endif
}
